using System;
using System.Collections.Generic;

namespace DeckParty.Core
{
    /// <summary>
    /// The authoritative Deck Party rules. Pure and deterministic: given the same state and command it always
    /// produces the same result, which is what lets the host simulate every player's game and stream snapshots
    /// to clients. All randomness comes from the RNG state stored in <see cref="PlayerState"/>.
    /// </summary>
    public static class PartyEngine
    {
        const int InfectionPop = 2;

        // ================================================================== setup

        public static PlayerState CreateGame(int scenarioId, uint seed, int nights = GameRules.DefaultNights)
        {
            var s = new PlayerState
            {
                ScenarioId = scenarioId,
                Seed = seed,
                TotalNights = Math.Max(1, nights),
                Capacity = GameRules.StartingCapacity,
                RngState = DeterministicRandom.Seed(seed),
                Night = 1,
            };
            s.Shop.AddRange(ScenarioCatalog.BuildShop(scenarioId, seed));

            for (int i = 0; i < 4; i++) AddGuest(s, GuestType.LoyalFan);
            for (int i = 0; i < 2; i++) AddGuest(s, GuestType.Whale);
            for (int i = 0; i < 4; i++) AddGuest(s, GuestType.BotFarm);

            BeginParty(s, null);
            return s;
        }

        static GuestInstance AddGuest(PlayerState s, GuestType type, bool temporary = false)
        {
            var g = new GuestInstance { Id = s.NextInstanceId++, Type = type, Temporary = temporary };
            s.Rolodex.Add(g);
            return g;
        }

        // ================================================================== command dispatch

        public static CommandResult Execute(PlayerState s, GameCommand cmd, List<GameEvent> events)
        {
            if (s == null) return CommandResult.Fail("No game in progress.");
            if (s.IsGameOver) return CommandResult.Fail("The season is over.");

            switch (cmd.Type)
            {
                case CommandType.OpenDoor: return OpenDoor(s, events);
                case CommandType.UseAbility: return UseAbility(s, cmd, events);
                case CommandType.AdmitPeeked: return AdmitPeeked(s, events);
                case CommandType.RejectPeeked: return RejectPeeked(s, events);
                case CommandType.ResolveAttraction: return ResolveAttraction(s, cmd.TargetA, cmd.TargetB, events);
                case CommandType.EndParty: return EndParty(s, events);
                case CommandType.BlameGuest: return Blame(s, cmd.TargetA, events);
                case CommandType.BuyGuest: return Buy(s, cmd.Guest, events);
                case CommandType.ExpandHouse: return Expand(s, events);
                case CommandType.StartNextNight: return StartNextNight(s, events);
                default: return CommandResult.Fail("Unknown command.");
            }
        }

        // ================================================================== queries: Swatting Risk

        /// <summary>How much Swatting Risk this guest adds right now.</summary>
        public static int GuestTrouble(PlayerState s, GuestInstance g)
        {
            if (g == null) return 0;
            int printed = !g.TroubleCleared && g.Info.Trouble ? 1 : 0;
            return printed + g.PartyTrouble;
        }

        public static bool HasTroubleProperty(PlayerState s, GuestInstance g) => GuestTrouble(s, g) > 0;

        public static int TroubleTotal(PlayerState s)
        {
            int total = 0;
            for (int i = 0; i < s.House.Count; i++) total += GuestTrouble(s, s.Find(s.House[i]));
            return total;
        }

        public static int TroublemakerCount(PlayerState s)
        {
            int count = 0;
            for (int i = 0; i < s.House.Count; i++)
                if (HasTroubleProperty(s, s.Find(s.House[i]))) count++;
            return count;
        }

        /// <summary>Whether this guest is cancelling Swatting Risk right now (Cheems only does with a Doge).</summary>
        public static bool CancelsTroubleNow(PlayerState s, GuestInstance g)
        {
            if (g == null) return false;
            if (g.Info.CancelsTrouble) return true;
            return g.Kind == GuestType.Cheems && s.CountInHouse(GuestType.Doge) > 0;
        }

        public static int CancelCount(PlayerState s)
        {
            int count = 0;
            for (int i = 0; i < s.House.Count; i++)
                if (CancelsTroubleNow(s, s.Find(s.House[i]))) count++;
            return count;
        }

        public static int NetTrouble(PlayerState s) => Math.Max(0, TroubleTotal(s) - CancelCount(s));

        public static int DiamondsPresent(PlayerState s)
        {
            int count = 0;
            for (int i = 0; i < s.House.Count; i++)
            {
                var g = s.Find(s.House[i]);
                if (g != null && g.Info.IsDiamond) count++;
            }
            return count;
        }

        // ================================================================== queries: scoring

        /// <summary>The guest's printed Subs tonight (after Ballerina swaps, NPC copies and permanent changes).</summary>
        public static int PrintedPop(GuestInstance g)
        {
            if (g.HasOverride) return g.PrintedOverride;
            int basePop = g.HasCopy ? g.CopyPop : g.Info.Pop;
            return basePop + g.BonusPop;
        }

        public static int PrintedCash(GuestInstance g) => g.HasCopy ? g.CopyCash : g.Info.Cash;

        /// <summary>Overly Attached Girlfriend attached to this guest makes them impossible to remove.</summary>
        public static bool IsProtected(PlayerState s, GuestInstance g)
        {
            if (g == null) return false;
            for (int i = 0; i < s.House.Count; i++)
            {
                var other = s.Find(s.House[i]);
                if (other != null && other != g && other.Kind == GuestType.OverlyAttachedGirlfriend && other.AttachedTo == g.Id) return true;
            }
            return false;
        }

        /// <summary>The guest this Overly Attached Girlfriend is attached to, if they're still here.</summary>
        public static GuestInstance AttachedPartner(PlayerState s, GuestInstance oag)
        {
            if (oag == null || oag.AttachedTo < 0 || !s.House.Contains(oag.AttachedTo)) return null;
            return s.Find(oag.AttachedTo);
        }

        sealed class ScoreContext
        {
            public int Doges, Cheems, Gigachads, Sigmas, Knuckles, Harolds;
            public int Families;
            public int BigPrinted;
            public int NetTrouble;
            public int Empty;
            public int Guests;
            public int ChungusBonus;
            public int Sufferers;
            public bool[] Suffering;
        }

        /// <summary>What this guest would score if scored right now.</summary>
        public static void ScoreGuest(PlayerState s, GuestInstance g, out int pop, out int cash)
        {
            int index = s.House.IndexOf(g.Id);
            if (index < 0)
            {
                pop = PrintedPop(g);
                cash = PrintedCash(g);
                return;
            }
            ScoreHouse(s, out var pops, out var cashes);
            pop = pops[index];
            cash = cashes[index];
        }

        /// <summary>Scores everyone at the party in one pass (several guests depend on the others).</summary>
        public static void ScoreHouse(PlayerState s, out int[] pops, out int[] cashes)
        {
            int n = s.House.Count;
            pops = new int[n];
            cashes = new int[n];
            var guests = new GuestInstance[n];
            for (int i = 0; i < n; i++) guests[i] = s.Find(s.House[i]);

            var ctx = BuildContext(s, guests);
            int prevented = 0;
            var printed = new int[n];

            for (int i = 0; i < n; i++)
            {
                var g = guests[i];
                if (g == null) continue;

                int basePop = PrintedPop(g);
                int baseCash = PrintedCash(g);
                printed[i] = basePop;
                int pop = basePop;
                int cash = baseCash;

                ApplyPassive(g, i, ctx, ref pop, ref cash);
                pop += g.PartyPop;
                cash += g.PartyCash;
                if (g.Infected) pop += InfectionPop;

                if (g.Zeroed)
                {
                    pop = 0;
                    cash = 0;
                }
                if (g.DoubleMode == 1) pop += basePop;
                else if (g.DoubleMode == 2) cash += baseCash;

                if (pop > 0 && IsProtected(s, g)) pop *= 2;

                if (ctx.Harolds > 0 && !g.Immune && pop < basePop)
                {
                    pop = basePop;
                    prevented++;
                }

                pops[i] = pop;
                cashes[i] = cash;
            }

            if (ctx.Harolds > 0)
            {
                int bonus = Math.Min(GameRules.HaroldMaxBonus, prevented);
                for (int i = 0; i < n; i++)
                    if (guests[i] != null && guests[i].Kind == GuestType.HideThePainHarold) pops[i] += bonus;
            }

            // Confused Math Lady conversions happen last so they convert the guest's whole take.
            for (int i = 0; i < n; i++)
            {
                var g = guests[i];
                if (g == null) continue;
                if (g.ConvertMode == 1 && pops[i] > 0)
                {
                    cashes[i] += pops[i] / 2;
                    pops[i] = 0;
                }
                else if (g.ConvertMode == 2 && cashes[i] > 0)
                {
                    pops[i] += cashes[i] * 2;
                    cashes[i] = 0;
                }
            }
        }

        static ScoreContext BuildContext(PlayerState s, GuestInstance[] guests)
        {
            var ctx = new ScoreContext
            {
                NetTrouble = NetTrouble(s),
                Empty = s.EmptySpaces,
                Guests = guests.Length,
                ChungusBonus = Math.Max(0, Math.Min(GameRules.BigChungusMaxBonus, s.Capacity - GameRules.BigChungusCapacityBase)),
                Suffering = new bool[guests.Length],
            };

            int familyMask = 0;
            foreach (var g in guests)
            {
                if (g == null) continue;
                switch (g.Kind)
                {
                    case GuestType.Doge: ctx.Doges++; break;
                    case GuestType.Cheems: ctx.Cheems++; break;
                    case GuestType.Gigachad: ctx.Gigachads++; break;
                    case GuestType.SigmaMale: ctx.Sigmas++; break;
                    case GuestType.UgandanKnuckles: ctx.Knuckles++; break;
                    case GuestType.HideThePainHarold: ctx.Harolds++; break;
                }
                if (g.Kind != GuestType.Doge) familyMask |= 1 << (int)g.Info.Category;
                if (PrintedPop(g) >= 5) ctx.BigPrinted++;
            }
            for (int bit = familyMask; bit != 0; bit &= bit - 1) ctx.Families++;

            for (int i = 0; i < guests.Length; i++)
            {
                var g = guests[i];
                if (g == null) continue;
                bool penalised = (g.Kind == GuestType.Gigachad && ctx.Gigachads > 1) ||
                                 (g.Kind == GuestType.SigmaMale && ctx.Sigmas > 1) ||
                                 (g.Kind == GuestType.Cheems && ctx.Doges == 0 && ctx.NetTrouble > 0);
                bool suffering = PrintedPop(g) < 0 || PrintedCash(g) < 0 || GuestTrouble(s, g) > 0 || g.Infected || g.Zeroed || penalised;
                ctx.Suffering[i] = suffering;
                if (suffering) ctx.Sufferers++;
            }
            return ctx;
        }

        static void ApplyPassive(GuestInstance g, int index, ScoreContext ctx, ref int pop, ref int cash)
        {
            switch (g.Kind)
            {
                case GuestType.Doge:
                    pop += Math.Min(GameRules.DogeMaxBonus, ctx.Families);
                    if (ctx.Cheems > 0 && !g.Immune) pop += 2;
                    break;
                case GuestType.Gigachad:
                    if (ctx.BigPrinted == 1 && PrintedPop(g) >= 5) pop += GameRules.GigachadSoloBonus;
                    pop -= GameRules.GigachadCrowdPenalty * (ctx.Gigachads - 1);
                    break;
                case GuestType.Wojak:
                {
                    int others = ctx.Sufferers - (ctx.Suffering[index] ? 1 : 0);
                    pop += Math.Min(GameRules.WojakMaxBonus, GameRules.WojakPerSufferer * others);
                    break;
                }
                case GuestType.Cheems:
                    if (ctx.Doges > 0) pop += 2;
                    else pop -= ctx.NetTrouble;
                    break;
                case GuestType.Doomer:
                    pop += 2 * ctx.NetTrouble;
                    if (ctx.NetTrouble == 2) cash += 2;
                    break;
                case GuestType.ForeverAlone:
                    pop += ctx.Guests == 1 ? GameRules.ForeverAloneSoloBonus : ctx.Empty;
                    break;
                case GuestType.UgandanKnuckles:
                    pop += ctx.Knuckles - 1;
                    break;
                case GuestType.BigChungus:
                    pop += ctx.ChungusBonus;
                    break;
                case GuestType.SigmaMale:
                    pop += Math.Min(GameRules.SigmaMaxBonus, ctx.Empty) - (ctx.Sigmas - 1);
                    break;
                case GuestType.Pepe:
                    if (g.Mood == PepeMood.Happy) pop += 3;
                    else if (g.Mood == PepeMood.Sad) cash += 3;
                    else if (g.Mood == PepeMood.Angry) pop += 6;
                    break;
            }
        }

        /// <summary>Total Subs/Cash the party would bank if it ended successfully right now (before fee shortfalls).</summary>
        public static void ProjectScore(PlayerState s, out int pop, out int cash)
        {
            pop = s.PartyPop + s.InvestPop * GameRules.StonksMultiplier;
            cash = s.PartyCash + s.InvestCash * GameRules.StonksMultiplier;
            ScoreHouse(s, out var pops, out var cashes);
            for (int i = 0; i < pops.Length; i++)
            {
                pop += pops[i];
                cash += cashes[i];
            }
        }

        /// <summary>Gifted Subs penalty that ending now would incur because fees can't be paid.</summary>
        public static int ProjectPenalty(PlayerState s, int partyCash)
        {
            int available = partyCash + (GameRules.FeesDrawFromBank ? s.Cash : 0);
            return available < 0 ? -available * GameRules.CashShortfallPopPenalty : 0;
        }

        public static int ExpansionCost(PlayerState s) => GameRules.ExpansionCost(s.Expansions);

        // ================================================================== queries: what can be done

        public static string GetDoorBlocker(PlayerState s)
        {
            if (s.Phase != GamePhase.Party) return "The party is over.";
            if (s.PeekedId >= 0) return "Decide on the guest at the door first.";
            if (s.AttractionPending) return "Decide on the New Attraction first.";
            if (s.HouseFull) return "You've hit your Sub Cap.";
            if (s.Pool.Count == 0) return "Nobody is left waiting outside.";
            return null;
        }

        public static string GetEndPartyBlocker(PlayerState s)
        {
            if (s.Phase != GamePhase.Party) return "The party is over.";
            if (s.PeekedId >= 0) return "Decide on the guest at the door first.";
            if (s.AttractionPending) return "Decide on the New Attraction first.";
            return null;
        }

        /// <summary>Null when the guest's action can be started (targets are validated separately).</summary>
        public static string GetAbilityBlocker(PlayerState s, int actorId)
        {
            if (s.Phase != GamePhase.Party) return "The party is over.";
            if (s.PeekedId >= 0) return "Decide on the guest at the door first.";
            if (s.AttractionPending) return "Decide on the New Attraction first.";
            if (!s.House.Contains(actorId)) return "That guest isn't at the party.";

            var actor = s.Find(actorId);
            if (actor == null) return "Unknown guest.";
            var info = actor.Info;
            if (!info.HasAbility) return $"{info.Name} has no action.";
            if (s.UsedAbilities.Contains(actorId)) return $"{info.AbilityName} was already used tonight.";

            switch (info.Ability)
            {
                case AbilityKind.Fetch:
                    if (s.Pool.Count == 0) return "Nobody is left waiting outside.";
                    if (s.HouseFull) return "You've hit your Sub Cap.";
                    break;
                case AbilityKind.Inspect:
                    if (s.Pool.Count < 2) return "Needs at least 2 guests waiting outside.";
                    break;
                case AbilityKind.SideEye:
                    if (s.Pool.Count < 2) return "Needs at least 2 guests waiting outside.";
                    break;
                case AbilityKind.Invest:
                    if (s.Pop < GameRules.StonksInvestPop && s.Cash < GameRules.StonksInvestCash) return "You have nothing to invest.";
                    break;
                case AbilityKind.Reroll:
                    if (!CanReroll(s) && s.Pool.Count == 0) return "Nothing to reroll and nobody waiting outside.";
                    break;
                case AbilityKind.Arson:
                    if (WouldRaidWithExtraTrouble(s)) return "That would bring the SWAT team.";
                    break;
            }

            GuestInfo.TargetCount(info.Ability, out int min, out _);
            if (info.Target != TargetMode.WaitingGuest && min > 0 && CountValidTargets(s, actorId) < min)
                return min == 1 ? "No guest can be chosen right now." : $"Needs {min} guests that can be chosen.";
            return null;
        }

        static bool WouldRaidWithExtraTrouble(PlayerState s) =>
            Math.Max(0, TroubleTotal(s) + 1 - CancelCount(s)) >= GameRules.RaidTroubleThreshold;

        static int CountValidTargets(PlayerState s, int actorId)
        {
            int count = 0;
            for (int i = 0; i < s.House.Count; i++)
                if (IsValidTarget(s, actorId, s.House[i], null)) count++;
            return count;
        }

        /// <summary>
        /// Whether <paramref name="candidateId"/> is a legal next pick for the actor's action, given the guests
        /// already chosen (in order) in <paramref name="chosen"/>.
        /// </summary>
        public static bool IsValidTarget(PlayerState s, int actorId, int candidateId, IReadOnlyList<int> chosen)
        {
            var actor = s.Find(actorId);
            if (actor == null) return false;
            var kind = actor.Info.Ability;

            if (kind == AbilityKind.Fetch) return s.Pool.Contains(candidateId);
            if (!s.House.Contains(candidateId)) return false;
            if (chosen != null)
                for (int i = 0; i < chosen.Count; i++)
                    if (chosen[i] == candidateId) return false;

            var target = s.Find(candidateId);
            if (target == null || target.Immune) return false;
            bool self = candidateId == actorId;

            switch (kind)
            {
                case AbilityKind.Boot:
                    return !IsProtected(s, target);
                case AbilityKind.Troll:
                case AbilityKind.PlayOff:
                case AbilityKind.Refund:
                    return !self && !IsProtected(s, target);
                case AbilityKind.Bombard:
                    return !self && !IsProtected(s, target) && (chosen == null || chosen.Count < 3);
                case AbilityKind.Mog:
                case AbilityKind.Arson:
                    return !self;
                case AbilityKind.Strike:
                    return !self && (target.StrikeCount < 2 || !IsProtected(s, target));
                case AbilityKind.Rickroll:
                    return !self && !target.Info.IsDiamond && RickrollChoices(s, target).Count > 0;
                case AbilityKind.Convert:
                case AbilityKind.Immunity:
                    return true;
                case AbilityKind.SwapOrder:
                case AbilityKind.Pirouette:
                case AbilityKind.Drake:
                    return chosen == null || chosen.Count < 2;
                default:
                    return false;
            }
        }

        public static string ValidateCommand(PlayerState s, GameCommand cmd)
        {
            var actor = s.Find(cmd.Actor);
            if (actor == null) return "Unknown guest.";
            var info = actor.Info;
            var kind = info.Ability;

            var options = info.Options;
            if (options != null && (cmd.Option < 0 || cmd.Option >= options.Length)) return "Pick one of the options.";

            switch (kind)
            {
                case AbilityKind.Invest:
                    if (cmd.Option == 0 && s.Pop < GameRules.StonksInvestPop) return $"Needs {GameRules.StonksInvestPop} Gifted Subs.";
                    if (cmd.Option == 1 && s.Cash < GameRules.StonksInvestCash) return $"Needs {GameRules.StonksInvestCash} $.";
                    return null;
                case AbilityKind.Reroll:
                    if (cmd.Option == 0 && !CanReroll(s)) return "There's no random arrival to reroll.";
                    if (cmd.Option == 1 && s.Pool.Count == 0) return "Nobody is waiting outside.";
                    return null;
                case AbilityKind.Inspect:
                {
                    int looked = Math.Min(GameRules.GrumpyInspectCount, s.Pool.Count);
                    int rejected = s.Pool.IndexOf(cmd.TargetA);
                    if (rejected < 0 || rejected >= looked) return "Pick one of the next guests to reject.";
                    if (cmd.TargetB >= 0)
                    {
                        int first = s.Pool.IndexOf(cmd.TargetB);
                        if (first < 0 || first >= looked || cmd.TargetB == cmd.TargetA) return "Pick who comes in next.";
                    }
                    return null;
                }
            }

            GuestInfo.TargetCount(kind, out int min, out int max);
            if (max == 0) return null;

            var picks = new List<int>(3);
            if (cmd.TargetA >= 0) picks.Add(cmd.TargetA);
            if (cmd.TargetB >= 0) picks.Add(cmd.TargetB);
            if (cmd.TargetC >= 0) picks.Add(cmd.TargetC);
            if (picks.Count < min || picks.Count > max)
                return min == max ? $"Pick {min} guest{(min == 1 ? "" : "s")}." : $"Pick {min} to {max} guests.";

            var chosen = new List<int>(3);
            foreach (var id in picks)
            {
                if (!IsValidTarget(s, cmd.Actor, id, chosen)) return "That's not a valid choice.";
                chosen.Add(id);
            }
            return null;
        }

        public static bool IsValidAttractionTarget(PlayerState s, int candidateId)
        {
            if (!s.AttractionPending || candidateId == s.AttractionActor || !s.House.Contains(candidateId)) return false;
            var g = s.Find(candidateId);
            return g != null && !g.Immune && !IsProtected(s, g);
        }

        /// <summary>Success Kid can reroll the most recent random arrival while they're still here.</summary>
        public static bool CanReroll(PlayerState s)
        {
            if (s.LastRandomArrival < 0 || !s.House.Contains(s.LastRandomArrival) || s.Pool.Count == 0) return false;
            var g = s.Find(s.LastRandomArrival);
            return g != null && !g.Immune && !IsProtected(s, g);
        }

        /// <summary>Character types Rickroll could turn <paramref name="target"/> into (from the waiting line).</summary>
        public static List<GuestType> RickrollChoices(PlayerState s, GuestInstance target)
        {
            var result = new List<GuestType>();
            for (int i = 0; i < s.Pool.Count; i++)
            {
                var g = s.Find(s.Pool[i]);
                if (g == null) continue;
                var info = g.Info;
                if (info.IsDiamond || info.Size != 1 || g.Kind == target.Kind || result.Contains(g.Kind)) continue;
                result.Add(g.Kind);
            }
            result.Sort();
            return result;
        }

        public static string GetBuyBlocker(PlayerState s, GuestType type)
        {
            if (s.Phase != GamePhase.Planning) return "You can only recruit between parties.";
            if (!s.Shop.Contains(type)) return "Not available in this character set.";
            var info = GuestCatalog.Get(type);
            if (!info.Purchasable) return $"{info.Name} can't be bought.";
            if (s.CountOwned(type) >= info.MaxCopies) return $"You already have {info.MaxCopies} {info.Name}s.";
            if (s.Pop < info.Cost) return $"Needs {info.Cost} Gifted Subs.";
            return null;
        }

        public static string GetExpandBlocker(PlayerState s)
        {
            if (s.Phase != GamePhase.Planning) return "You can only upgrade between parties.";
            if (s.Capacity >= GameRules.MaxCapacity) return "Your Sub Cap is maxed out.";
            int cost = ExpansionCost(s);
            if (s.Cash < cost) return $"Needs {cost} $.";
            return null;
        }

        // ================================================================== party phase: lifecycle

        static void BeginParty(PlayerState s, List<GameEvent> ev)
        {
            ClearParty(s);
            s.BannedThisParty = s.BannedNextParty;
            s.BannedNextParty = -1;
            s.RaidSuspects.Clear();

            for (int i = 0; i < s.Rolodex.Count; i++)
            {
                int id = s.Rolodex[i].Id;
                if (id != s.BannedThisParty) s.Pool.Add(id);
            }
            Shuffle(s, s.Pool);

            s.Phase = GamePhase.Party;
            Emit(ev, GameEventType.NightStarted, null, s.Night);
        }

        static void ClearParty(PlayerState s)
        {
            s.House.Clear();
            s.Pool.Clear();
            s.Excluded.Clear();
            s.UsedAbilities.Clear();
            s.PeekedId = -1;
            s.PartyPop = 0;
            s.PartyCash = 0;
            s.InvestPop = 0;
            s.InvestCash = 0;
            s.LastActionCategory = ActionCategory.None;
            s.LastRandomArrival = -1;
            s.AttractionActor = -1;
            s.AttractionCandidates.Clear();
            s.BannedThisParty = -1;

            foreach (var g in s.Rolodex) g.ResetParty();
            s.Rolodex.RemoveAll(g => g.Temporary);
        }

        static void Shuffle(PlayerState s, List<int> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = DeterministicRandom.Range(ref s.RngState, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>Puts a guest back into the waiting line at a random position.</summary>
        static void ReturnToPool(PlayerState s, int id)
        {
            int index = DeterministicRandom.Range(ref s.RngState, s.Pool.Count + 1);
            s.Pool.Insert(index, id);
        }

        static CommandResult OpenDoor(PlayerState s, List<GameEvent> ev)
        {
            var blocker = GetDoorBlocker(s);
            if (blocker != null) return CommandResult.Fail(blocker);
            Arrive(s, s.Pool[0], -1, -1, true, ev);
            return CommandResult.Success;
        }

        static CommandResult AdmitPeeked(PlayerState s, List<GameEvent> ev)
        {
            if (s.Phase != GamePhase.Party || s.PeekedId < 0) return CommandResult.Fail("Nobody is waiting at the door.");
            if (s.HouseFull) return CommandResult.Fail("You've hit your Sub Cap. You'll have to turn them away.");
            int id = s.PeekedId;
            s.PeekedId = -1;
            Arrive(s, id, -1, -1, false, ev);
            return CommandResult.Success;
        }

        static CommandResult RejectPeeked(PlayerState s, List<GameEvent> ev)
        {
            if (s.Phase != GamePhase.Party || s.PeekedId < 0) return CommandResult.Fail("Nobody is waiting at the door.");
            int id = s.PeekedId;
            s.PeekedId = -1;
            s.Pool.Remove(id);
            s.Excluded.Add(id);
            Emit(ev, GameEventType.GuestRejected, s.Find(id));
            return CommandResult.Success;
        }

        // ================================================================== arrivals

        /// <summary>
        /// Moves a guest into the party and resolves entrance effects, reactions, overcrowding, Swatting Risk and
        /// chained arrivals. Returns false when the party was shut down as a consequence.
        /// </summary>
        static bool Arrive(PlayerState s, int id, int slot, int sourceId, bool random, List<GameEvent> ev)
        {
            var g = s.Find(id);
            if (g == null || s.Phase != GamePhase.Party) return s.Phase == GamePhase.Party;

            s.Pool.Remove(id);
            if (slot >= 0 && slot <= s.House.Count) s.House.Insert(slot, id);
            else s.House.Add(id);
            if (random) s.LastRandomArrival = id;
            Emit(ev, GameEventType.GuestArrived, g, sourceId, s.House.IndexOf(id));

            OnArrival(s, g, ev);
            ReactToArrival(s, g, ev);

            if (s.Occupancy > s.Capacity)
            {
                ShutDownFireMarshal(s, ev);
                return false;
            }
            if (CheckRaid(s, ev)) return false;

            for (int i = 0; i < g.Info.Brings; i++)
            {
                if (s.Pool.Count == 0) break;
                if (!Arrive(s, s.Pool[0], -1, id, true, ev)) return false;
            }
            return s.Phase == GamePhase.Party;
        }

        /// <summary>The newcomer's own entrance effect.</summary>
        static void OnArrival(PlayerState s, GuestInstance g, List<GameEvent> ev)
        {
            switch (g.Kind)
            {
                case GuestType.NpcWojak:
                    CopyFromPredecessor(s, g, ev);
                    break;

                case GuestType.OverlyAttachedGirlfriend:
                {
                    GuestInstance best = null;
                    for (int i = 0; i < s.House.Count; i++)
                    {
                        var other = s.Find(s.House[i]);
                        if (!CanBeAttached(s, other, g)) continue;
                        if (best == null || PrintedPop(other) > PrintedPop(best)) best = other;
                    }
                    if (best != null) Attach(s, g, best, ev);
                    else g.AttachedTo = GuestInstance.AttachPending;
                    break;
                }

                case GuestType.UgandanKnuckles:
                    if (!g.Temporary)
                    {
                        var copy = AddGuest(s, GuestType.UgandanKnuckles, true);
                        ReturnToPool(s, copy.Id);
                        Emit(ev, GameEventType.TemporaryCopy, copy, g.Id);
                    }
                    break;

                case GuestType.SkibidiToilet:
                    InfectRandom(s, g.Id, ev);
                    break;

                case GuestType.DistractedBoyfriend:
                    StartAttraction(s, g, ev);
                    break;
            }
        }

        /// <summary>Everyone already at the party responds to a newcomer.</summary>
        static void ReactToArrival(PlayerState s, GuestInstance newcomer, List<GameEvent> ev)
        {
            bool troubleArrival = newcomer.Info.Trouble;
            if (troubleArrival) NotifyTroubleAdded(s, newcomer, ev);

            bool spreads = false;
            var present = new List<int>(s.House);
            foreach (var id in present)
            {
                if (id == newcomer.Id) continue;
                var h = s.Find(id);
                if (h == null || !s.House.Contains(id)) continue;

                switch (h.Kind)
                {
                    case GuestType.OverlyAttachedGirlfriend:
                        if (h.AttachedTo == GuestInstance.AttachPending && CanBeAttached(s, newcomer, h)) Attach(s, h, newcomer, ev);
                        break;

                    case GuestType.NyanCat:
                        if (troubleArrival) h.Counter = 0;
                        else
                        {
                            h.Counter++;
                            PotGain(s, h, Math.Min(GameRules.NyanMaxPerArrival, h.Counter), 0, ev);
                        }
                        break;

                    case GuestType.BongoCat:
                        h.Counter++;
                        if (h.Counter % 3 == 0) BongoBeat(s, h, ev);
                        break;

                    case GuestType.SkibidiToilet:
                        spreads = true;
                        break;
                }
                if (h.Infected) spreads = true;
            }

            if (spreads && newcomer.Kind != GuestType.SkibidiToilet) InfectRandom(s, newcomer.Id, ev);
        }

        static bool CanBeAttached(PlayerState s, GuestInstance candidate, GuestInstance oag)
        {
            if (candidate == null || candidate == oag || candidate.Immune) return false;
            if (candidate.Kind == GuestType.OverlyAttachedGirlfriend) return false;
            return !IsProtected(s, candidate);
        }

        static void Attach(PlayerState s, GuestInstance oag, GuestInstance partner, List<GameEvent> ev)
        {
            oag.AttachedTo = partner.Id;
            Emit(ev, GameEventType.Attached, oag, partner.Id);
        }

        static void CopyFromPredecessor(PlayerState s, GuestInstance npc, List<GameEvent> ev)
        {
            int index = s.House.IndexOf(npc.Id);
            var previous = index > 0 ? s.Find(s.House[index - 1]) : null;
            if (previous == null)
            {
                npc.HasCopy = false;
                return;
            }
            npc.HasCopy = true;
            npc.CopyPop = PrintedPop(previous);
            npc.CopyCash = PrintedCash(previous);
            Emit(ev, GameEventType.StatsCopied, npc, previous.Id, npc.CopyPop, npc.CopyCash);
        }

        static void InfectRandom(PlayerState s, int sourceId, List<GameEvent> ev)
        {
            var candidates = new List<GuestInstance>();
            for (int i = 0; i < s.House.Count; i++)
            {
                var g = s.Find(s.House[i]);
                if (g == null || g.Infected || g.Immune || g.Kind == GuestType.SkibidiToilet) continue;
                candidates.Add(g);
            }
            if (candidates.Count == 0) return;

            var target = candidates[DeterministicRandom.Range(ref s.RngState, candidates.Count)];
            target.Infected = true;
            Emit(ev, GameEventType.Infected, target, sourceId);
            AddTrouble(s, target, 1, ev);
        }

        static void BongoBeat(PlayerState s, GuestInstance bongo, List<GameEvent> ev)
        {
            for (int i = 0; i < s.House.Count; i++)
            {
                var g = s.Find(s.House[i]);
                if (g == null || g.Info.Category != GuestCategory.Animal) continue;
                if (g != bongo && g.Immune) continue;
                Gain(s, g, 2, 0, ev);
            }
        }

        static void StartAttraction(PlayerState s, GuestInstance boyfriend, List<GameEvent> ev)
        {
            if (s.AttractionPending || s.Pool.Count == 0) return;
            for (int k = 0; k < GameRules.AttractionRevealCount && s.Pool.Count > 0; k++)
            {
                int index = DeterministicRandom.Range(ref s.RngState, s.Pool.Count);
                s.AttractionCandidates.Add(s.Pool[index]);
                s.Pool.RemoveAt(index);
            }
            s.AttractionActor = boyfriend.Id;
            Emit(ev, GameEventType.AttractionRevealed, boyfriend, s.AttractionCandidates.Count);
        }

        static CommandResult ResolveAttraction(PlayerState s, int replacedId, int chosenId, List<GameEvent> ev)
        {
            if (s.Phase != GamePhase.Party || !s.AttractionPending) return CommandResult.Fail("There's nothing to decide.");

            bool skip = replacedId < 0 || chosenId < 0;
            if (!skip)
            {
                if (!s.AttractionCandidates.Contains(chosenId)) return CommandResult.Fail("Pick one of the revealed guests.");
                if (!IsValidAttractionTarget(s, replacedId)) return CommandResult.Fail("That guest can't be replaced.");
            }

            int actor = s.AttractionActor;
            var candidates = new List<int>(s.AttractionCandidates);
            s.AttractionActor = -1;
            s.AttractionCandidates.Clear();

            if (skip)
            {
                foreach (var id in candidates) ReturnToPool(s, id);
                Emit(ev, GameEventType.AttractionResolved, null, -1, -1);
                return CommandResult.Success;
            }

            candidates.Remove(chosenId);
            foreach (var id in candidates) ReturnToPool(s, id);
            Emit(ev, GameEventType.AttractionResolved, null, replacedId, chosenId);

            int slot = s.House.IndexOf(replacedId);
            if (RemoveFromParty(s, replacedId, LeaveReason.Replaced, actor, true, ev))
            {
                if (s.Phase == GamePhase.Party) Arrive(s, chosenId, slot, actor, false, ev);
            }
            else
            {
                ReturnToPool(s, chosenId);
            }
            return CommandResult.Success;
        }

        // ================================================================== departures

        /// <summary>
        /// Takes a guest out of the party. <paramref name="attempt"/> marks removals Bad Luck Brian can shrug off.
        /// Returns false when the guest stayed.
        /// </summary>
        static bool RemoveFromParty(PlayerState s, int id, LeaveReason reason, int actorId, bool attempt, List<GameEvent> ev, bool reactions = true)
        {
            var g = s.Find(id);
            if (g == null || !s.House.Contains(id)) return false;
            if (attempt && TryShield(g, ev)) return false;

            int trouble = GuestTrouble(s, g);
            s.House.Remove(id);
            if (s.LastRandomArrival == id) s.LastRandomArrival = -1;

            // Presence-bound effects end when a guest leaves.
            if (g.Infected)
            {
                g.Infected = false;
                g.PartyTrouble = Math.Max(0, g.PartyTrouble - 1);
            }
            g.AttachedTo = -1;
            g.HasCopy = false;
            g.Disguise = GuestInstance.NoDisguise;
            g.Counter = 0;

            if (reason == LeaveReason.SentOutside) ReturnToPool(s, id);
            else if (reason != LeaveReason.Gone) s.Excluded.Add(id);
            Emit(ev, GameEventType.GuestRemoved, g, actorId, (int)reason);

            if (reactions)
            {
                if (trouble > 0) NotifyTroubleRemoved(s, g, trouble, ev);
                for (int i = 0; i < s.House.Count; i++)
                {
                    var cat = s.Find(s.House[i]);
                    if (cat != null && cat.Kind == GuestType.CryingCat) Gain(s, cat, 2, 1, ev);
                }
            }

            // "If they are forcibly removed, she leaves too."
            var present = new List<int>(s.House);
            foreach (var otherId in present)
            {
                var other = s.Find(otherId);
                if (other == null || other.Kind != GuestType.OverlyAttachedGirlfriend || other.AttachedTo != id) continue;
                other.AttachedTo = -1;
                var follow = reason == LeaveReason.SentOutside ? LeaveReason.SentOutside : LeaveReason.Followed;
                RemoveFromParty(s, otherId, follow, -1, false, ev, reactions);
            }
            return true;
        }

        /// <summary>Bad Luck Brian: the first removal or clearing attempt each party fails.</summary>
        static bool TryShield(GuestInstance g, List<GameEvent> ev)
        {
            if (g.Kind != GuestType.BadLuckBrian || g.ShieldUsed) return false;
            g.ShieldUsed = true;
            g.BonusPop++;
            Emit(ev, GameEventType.ShieldBlocked, g, g.BonusPop);
            return true;
        }

        static void AddTrouble(PlayerState s, GuestInstance g, int amount, List<GameEvent> ev)
        {
            g.PartyTrouble += amount;
            Emit(ev, GameEventType.TroubleAdded, g, amount);
            NotifyTroubleAdded(s, g, ev);
        }

        /// <summary>Clears a guest's Swatting Risk for the rest of the party. Returns how much was removed.</summary>
        static int ClearTrouble(PlayerState s, GuestInstance g, List<GameEvent> ev)
        {
            int amount = GuestTrouble(s, g);
            if (amount <= 0) return 0;
            if (TryShield(g, ev)) return 0;
            g.TroubleCleared = true;
            g.PartyTrouble = 0;
            Emit(ev, GameEventType.TroubleRemoved, g, amount);
            NotifyTroubleRemoved(s, g, amount, ev);
            return amount;
        }

        static void NotifyTroubleAdded(PlayerState s, GuestInstance source, List<GameEvent> ev)
        {
            for (int i = 0; i < s.House.Count; i++)
            {
                var g = s.Find(s.House[i]);
                if (g != null && g != source && g.Kind == GuestType.MeGusta) Gain(s, g, 2, 0, ev);
            }
        }

        static void NotifyTroubleRemoved(PlayerState s, GuestInstance source, int amount, List<GameEvent> ev)
        {
            for (int i = 0; i < s.House.Count; i++)
            {
                var g = s.Find(s.House[i]);
                if (g != null && g != source && g.Kind == GuestType.MeGusta) Gain(s, g, 0, amount, ev);
            }
        }

        static void Gain(PlayerState s, GuestInstance g, int pop, int cash, List<GameEvent> ev)
        {
            if (pop == 0 && cash == 0) return;
            g.PartyPop += pop;
            g.PartyCash += cash;
            Emit(ev, GameEventType.GuestGained, g, pop, cash);
        }

        static void PotGain(PlayerState s, GuestInstance source, int pop, int cash, List<GameEvent> ev)
        {
            if (pop == 0 && cash == 0) return;
            s.PartyPop += pop;
            s.PartyCash += cash;
            Emit(ev, GameEventType.PartyPotGained, source, pop, cash);
        }

        // ================================================================== shutdowns

        static bool CheckRaid(PlayerState s, List<GameEvent> ev)
        {
            if (s.Phase != GamePhase.Party) return false;
            if (NetTrouble(s) < GameRules.RaidTroubleThreshold) return false;

            // This Is Fine Dog: the first time it would happen, the SWAT team is called off.
            GuestInstance dog = null;
            for (int i = 0; i < s.House.Count && dog == null; i++)
            {
                var g = s.Find(s.House[i]);
                if (g != null && g.Type == GuestType.ThisIsFineDog && !g.IsDisguised) dog = g;
            }
            if (dog != null)
            {
                int cleared = 0;
                foreach (var id in new List<int>(s.House))
                {
                    var g = s.Find(id);
                    if (g != null) cleared += ClearTrouble(s, g, ev);
                }
                Emit(ev, GameEventType.ThisIsFine, dog, cleared);
                RemoveFromParty(s, dog.Id, LeaveReason.Gone, -1, false, ev);
                s.UsedAbilities.Remove(dog.Id);
                s.Rolodex.Remove(dog);
                if (NetTrouble(s) < GameRules.RaidTroubleThreshold) return false;
            }

            int trouble = NetTrouble(s);
            s.RaidSuspects.Clear();
            foreach (var id in s.House)
            {
                var g = s.Find(id);
                if (g != null && !g.Temporary) s.RaidSuspects.Add(id);
            }
            s.Raids++;
            RecordOutcome(s, PartyOutcome.Raid, 0, 0, DiamondsPresent(s), 0);
            Emit(ev, GameEventType.PoliceRaid, null, trouble);
            ClearParty(s);

            if (s.IsFinalNight) FinishDefeat(s, ev);
            else if (s.RaidSuspects.Count == 0) s.Phase = GamePhase.Planning;
            else s.Phase = GamePhase.RaidBlame;
            return true;
        }

        static void ShutDownFireMarshal(PlayerState s, List<GameEvent> ev)
        {
            int occupancy = s.Occupancy;
            s.FireMarshals++;
            RecordOutcome(s, PartyOutcome.FireMarshal, 0, 0, DiamondsPresent(s), 0);
            Emit(ev, GameEventType.FireMarshal, null, occupancy, s.Capacity);
            ClearParty(s);
            AfterParty(s, ev);
        }

        // ================================================================== actions

        static CommandResult UseAbility(PlayerState s, GameCommand cmd, List<GameEvent> ev)
        {
            int actorId = cmd.Actor;
            var blocker = GetAbilityBlocker(s, actorId);
            if (blocker != null) return CommandResult.Fail(blocker);
            var error = ValidateCommand(s, cmd);
            if (error != null) return CommandResult.Fail(error);

            var actor = s.Find(actorId);
            var kind = actor.Info.Ability;
            int a = cmd.TargetA, b = cmd.TargetB, c = cmd.TargetC;
            s.UsedAbilities.Add(actorId);
            Emit(ev, GameEventType.AbilityUsed, actor, a, b, cmd.Option);
            OnActionUsed(s, actor, ev);

            switch (kind)
            {
                case AbilityKind.Fetch:
                    Arrive(s, a, -1, actorId, false, ev);
                    break;

                case AbilityKind.Boot:
                    RemoveFromParty(s, a, LeaveReason.Evicted, actorId, true, ev);
                    CheckRaid(s, ev);
                    break;

                case AbilityKind.Reset:
                {
                    int before = s.House.Count;
                    foreach (var id in new List<int>(s.House))
                        RemoveFromParty(s, id, LeaveReason.SentOutside, actorId, true, ev, false);
                    Emit(ev, GameEventType.EveryoneOutside, actor, before - s.House.Count);
                    CheckRaid(s, ev);
                    break;
                }

                case AbilityKind.Troll:
                {
                    var target = s.Find(a);
                    if (RemoveFromParty(s, a, LeaveReason.SentOutside, actorId, true, ev))
                    {
                        target.TrolledCount++;
                        Gain(s, actor, 2 * target.TrolledCount, 0, ev);
                    }
                    CheckRaid(s, ev);
                    break;
                }

                case AbilityKind.Mood:
                    actor.Mood = (PepeMood)(cmd.Option + 1);
                    Emit(ev, GameEventType.MoodChosen, actor, (int)actor.Mood);
                    if (actor.Mood == PepeMood.Angry)
                    {
                        AddTrouble(s, actor, 1, ev);
                        CheckRaid(s, ev);
                    }
                    break;

                case AbilityKind.Inspect:
                {
                    int looked = Math.Min(GameRules.GrumpyInspectCount, s.Pool.Count);
                    var inspected = s.Pool.GetRange(0, looked);
                    s.Pool.RemoveRange(0, looked);
                    inspected.Remove(a);
                    s.Excluded.Add(a);
                    Emit(ev, GameEventType.GuestRejected, s.Find(a));
                    if (b >= 0 && inspected.Remove(b)) inspected.Insert(0, b);
                    s.Pool.InsertRange(0, inspected);
                    break;
                }

                case AbilityKind.Invest:
                    if (cmd.Option == 0)
                    {
                        s.Pop -= GameRules.StonksInvestPop;
                        s.InvestPop += GameRules.StonksInvestPop;
                    }
                    else
                    {
                        s.Cash -= GameRules.StonksInvestCash;
                        s.InvestCash += GameRules.StonksInvestCash;
                    }
                    Emit(ev, GameEventType.Invested, actor, s.InvestPop, s.InvestCash);
                    break;

                case AbilityKind.PlayOff:
                {
                    var target = s.Find(a);
                    bool hadTrouble = HasTroubleProperty(s, target);
                    if (RemoveFromParty(s, a, LeaveReason.Evicted, actorId, true, ev))
                        Gain(s, actor, hadTrouble ? 2 : 0, hadTrouble ? 0 : 1, ev);
                    CheckRaid(s, ev);
                    break;
                }

                case AbilityKind.Mog:
                {
                    var target = s.Find(a);
                    int theirs = PrintedPop(target);
                    int chads = PrintedPop(actor);
                    if (theirs < chads)
                    {
                        target.BonusPop--;
                        actor.BonusPop++;
                        Emit(ev, GameEventType.PermanentChange, target, -1, target.BonusPop);
                        Emit(ev, GameEventType.PermanentChange, actor, 1, actor.BonusPop);
                    }
                    else if (theirs > chads)
                    {
                        actor.BonusPop--;
                        Emit(ev, GameEventType.PermanentChange, actor, -1, actor.BonusPop);
                    }
                    else
                    {
                        Emit(ev, GameEventType.PermanentChange, actor, 0, actor.BonusPop);
                    }
                    break;
                }

                case AbilityKind.Arson:
                {
                    var target = s.Find(a);
                    AddTrouble(s, target, 1, ev);
                    Gain(s, target, 3, 0, ev);
                    Gain(s, actor, 2, 0, ev);
                    CheckRaid(s, ev);
                    break;
                }

                case AbilityKind.Reroll:
                    if (cmd.Option == 0)
                    {
                        int rerolled = s.LastRandomArrival;
                        int slot = s.House.IndexOf(rerolled);
                        if (RemoveFromParty(s, rerolled, LeaveReason.SentOutside, actorId, true, ev))
                        {
                            var others = new List<int>(s.Pool);
                            others.Remove(rerolled);
                            if (others.Count > 0)
                            {
                                int next = others[DeterministicRandom.Range(ref s.RngState, others.Count)];
                                Arrive(s, next, slot, actorId, true, ev);
                            }
                        }
                        CheckRaid(s, ev);
                    }
                    else
                    {
                        int peeked = s.Pool[0];
                        s.Pool.RemoveAt(0);
                        s.PeekedId = peeked;
                        Emit(ev, GameEventType.GuestPeeked, s.Find(peeked), actorId);
                    }
                    break;

                case AbilityKind.Strike:
                {
                    var target = s.Find(a);
                    target.StrikeCount++;
                    Emit(ev, GameEventType.Struck, target, target.StrikeCount);
                    if (target.StrikeCount == 1)
                    {
                        Gain(s, target, 2, 0, ev);
                    }
                    else if (target.StrikeCount == 2)
                    {
                        ClearTrouble(s, target, ev);
                    }
                    else
                    {
                        target.StrikeCount = 0;
                        if (RemoveFromParty(s, a, LeaveReason.Evicted, actorId, true, ev))
                        {
                            actor.BonusPop++;
                            Emit(ev, GameEventType.PermanentChange, actor, 1, actor.BonusPop);
                        }
                        CheckRaid(s, ev);
                    }
                    break;
                }

                case AbilityKind.SwapOrder:
                {
                    int ia = s.House.IndexOf(a);
                    int ib = s.House.IndexOf(b);
                    (s.House[ia], s.House[ib]) = (s.House[ib], s.House[ia]);
                    Emit(ev, GameEventType.PositionsSwapped, null, a, b);
                    foreach (var id in s.House)
                    {
                        var g = s.Find(id);
                        if (g != null && g.Kind == GuestType.NpcWojak && !g.Immune) CopyFromPredecessor(s, g, ev);
                    }
                    break;
                }

                case AbilityKind.Bombard:
                {
                    var targets = new List<int>(3) { a };
                    if (b >= 0) targets.Add(b);
                    if (c >= 0) targets.Add(c);
                    foreach (var id in targets)
                    {
                        if (s.Phase != GamePhase.Party) break;
                        if (!s.House.Contains(id)) continue;
                        int slot = s.House.IndexOf(id);
                        if (!RemoveFromParty(s, id, LeaveReason.Replaced, actorId, true, ev) || s.Pool.Count == 0) continue;
                        int replacement = s.Pool[DeterministicRandom.Range(ref s.RngState, s.Pool.Count)];
                        if (!Arrive(s, replacement, slot, actorId, true, ev)) break;
                    }
                    CheckRaid(s, ev);
                    break;
                }

                case AbilityKind.Pirouette:
                {
                    var first = s.Find(a);
                    var second = s.Find(b);
                    int firstPrinted = PrintedPop(first);
                    int secondPrinted = PrintedPop(second);
                    first.HasOverride = true;
                    first.PrintedOverride = secondPrinted;
                    second.HasOverride = true;
                    second.PrintedOverride = firstPrinted;
                    Emit(ev, GameEventType.PrintedExchanged, null, a, b);
                    break;
                }

                case AbilityKind.Refund:
                {
                    var target = s.Find(a);
                    int refund = Math.Min(GameRules.KarenMaxRefund, target.BaseInfo.Cost / 2);
                    if (RemoveFromParty(s, a, LeaveReason.Evicted, actorId, true, ev))
                        PotGain(s, actor, refund, 0, ev);
                    CheckRaid(s, ev);
                    break;
                }

                case AbilityKind.Rickroll:
                {
                    var target = s.Find(a);
                    var choices = RickrollChoices(s, target);
                    var kindChosen = choices[DeterministicRandom.Range(ref s.RngState, choices.Count)];
                    Emit(ev, GameEventType.Transformed, target, (int)kindChosen);
                    target.Disguise = (byte)kindChosen;
                    target.HasCopy = false;
                    s.UsedAbilities.Remove(target.Id);
                    if (GuestCatalog.Get(kindChosen).Trouble && !target.TroubleCleared) NotifyTroubleAdded(s, target, ev);
                    CheckRaid(s, ev);
                    break;
                }

                case AbilityKind.Drake:
                {
                    var zeroed = s.Find(a);
                    var doubled = s.Find(b);
                    zeroed.Zeroed = true;
                    doubled.DoubleMode = (byte)(cmd.Option + 1);
                    Emit(ev, GameEventType.Zeroed, zeroed);
                    Emit(ev, GameEventType.Doubled, doubled, doubled.DoubleMode);
                    break;
                }

                case AbilityKind.Convert:
                {
                    var target = s.Find(a);
                    target.ConvertMode = (byte)(cmd.Option + 1);
                    Emit(ev, GameEventType.Converted, target, target.ConvertMode);
                    break;
                }

                case AbilityKind.SideEye:
                {
                    int next = s.Pool[0];
                    s.Pool.RemoveAt(0);
                    s.Pool.Add(next);
                    Emit(ev, GameEventType.MovedToBack, s.Find(next));
                    break;
                }

                case AbilityKind.Immunity:
                {
                    var target = s.Find(a);
                    target.Immune = true;
                    Emit(ev, GameEventType.ImmunityGranted, target);
                    break;
                }
            }

            return CommandResult.Success;
        }

        /// <summary>Pop Cat and Dancing Baby react to every action; the action's family is remembered.</summary>
        static void OnActionUsed(PlayerState s, GuestInstance actor, List<GameEvent> ev)
        {
            var category = actor.Info.ActionCategory;
            bool combo = category != ActionCategory.None && category == s.LastActionCategory;

            foreach (var id in new List<int>(s.House))
            {
                var g = s.Find(id);
                if (g == null) continue;
                if (g.Kind == GuestType.PopCat)
                {
                    g.Counter++;
                    Gain(s, g, 1, 0, ev);
                    if (g.Counter % 3 == 0) PotGain(s, g, 0, 1, ev);
                }
                else if (g.Kind == GuestType.DancingBaby && combo)
                {
                    int gain = Math.Min(GameRules.DancingBabyCombo, GameRules.DancingBabyMaxBonus - g.Gained);
                    if (gain <= 0) continue;
                    g.Gained += gain;
                    Gain(s, g, gain, 0, ev);
                }
            }
            s.LastActionCategory = category;
        }

        // ================================================================== ending a party

        static CommandResult EndParty(PlayerState s, List<GameEvent> ev)
        {
            var blocker = GetEndPartyBlocker(s);
            if (blocker != null) return CommandResult.Fail(blocker);

            ScoreHouse(s, out var pops, out var cashes);
            for (int i = 0; i < s.House.Count; i++)
                Emit(ev, GameEventType.GuestScored, s.Find(s.House[i]), pops[i], cashes[i], 1);

            if (s.InvestPop > 0 || s.InvestCash > 0)
                Emit(ev, GameEventType.InvestmentPaid, null, s.InvestPop * GameRules.StonksMultiplier, s.InvestCash * GameRules.StonksMultiplier);

            ProjectScore(s, out int pop, out int cash);
            int diamonds = DiamondsPresent(s);
            int penalty = 0;

            int unpaid;
            if (GameRules.FeesDrawFromBank)
            {
                s.Cash += cash;
                unpaid = s.Cash < 0 ? -s.Cash : 0;
                if (unpaid > 0) s.Cash = 0;
            }
            else
            {
                unpaid = cash < 0 ? -cash : 0;
                if (cash > 0) s.Cash += cash;
            }

            if (unpaid > 0)
            {
                penalty = unpaid * GameRules.CashShortfallPopPenalty;
                Emit(ev, GameEventType.CashShortfall, null, unpaid, penalty);
            }

            s.Pop = Math.Max(0, s.Pop + pop - penalty);
            s.TotalPopEarned += Math.Max(0, pop);
            s.TotalCashEarned += Math.Max(0, cash);
            s.SuccessfulParties++;
            s.BestDiamonds = Math.Max(s.BestDiamonds, diamonds);
            RecordOutcome(s, PartyOutcome.Success, pop, cash, diamonds, penalty);
            Emit(ev, GameEventType.PartyEnded, null, pop, cash, diamonds);

            int temporary = 0;
            foreach (var g in s.Rolodex) if (g.Temporary) temporary++;
            ClearParty(s);
            if (temporary > 0) Emit(ev, GameEventType.TemporaryCopiesLeft, null, temporary);

            if (diamonds >= GameRules.DiamondsToWin)
            {
                s.Phase = GamePhase.Victory;
                s.WinNight = s.Night;
                Emit(ev, GameEventType.Victory, null, s.Night);
                return CommandResult.Success;
            }

            AfterParty(s, ev);
            return CommandResult.Success;
        }

        static void RecordOutcome(PlayerState s, PartyOutcome outcome, int pop, int cash, int diamonds, int penalty)
        {
            s.LastOutcome = outcome;
            s.LastPop = pop;
            s.LastCash = cash;
            s.LastDiamonds = diamonds;
            s.LastPenalty = penalty;
            s.LastBlamedId = -1;
        }

        static void AfterParty(PlayerState s, List<GameEvent> ev)
        {
            if (s.IsFinalNight) FinishDefeat(s, ev);
            else s.Phase = GamePhase.Planning;
        }

        static void FinishDefeat(PlayerState s, List<GameEvent> ev)
        {
            s.Phase = GamePhase.Defeat;
            Emit(ev, GameEventType.Defeat, null);
        }

        static CommandResult Blame(PlayerState s, int guestId, List<GameEvent> ev)
        {
            if (s.Phase != GamePhase.RaidBlame) return CommandResult.Fail("There's nobody to ban.");
            if (!s.RaidSuspects.Contains(guestId) || s.Find(guestId) == null) return CommandResult.Fail("That guest wasn't at the swatted party.");

            s.BannedNextParty = guestId;
            s.LastBlamedId = guestId;
            s.RaidSuspects.Clear();
            Emit(ev, GameEventType.GuestBlamed, s.Find(guestId));
            s.Phase = GamePhase.Planning;
            return CommandResult.Success;
        }

        // ================================================================== planning phase

        static CommandResult Buy(PlayerState s, GuestType type, List<GameEvent> ev)
        {
            if (!GuestCatalog.IsValid(type)) return CommandResult.Fail("Unknown guest.");
            var blocker = GetBuyBlocker(s, type);
            if (blocker != null) return CommandResult.Fail(blocker);

            var info = GuestCatalog.Get(type);
            s.Pop -= info.Cost;
            var g = AddGuest(s, type);
            Emit(ev, GameEventType.GuestPurchased, g, info.Cost);
            return CommandResult.Success;
        }

        static CommandResult Expand(PlayerState s, List<GameEvent> ev)
        {
            var blocker = GetExpandBlocker(s);
            if (blocker != null) return CommandResult.Fail(blocker);

            int cost = ExpansionCost(s);
            s.Cash -= cost;
            s.Capacity++;
            s.Expansions++;
            Emit(ev, GameEventType.HouseExpanded, null, s.Capacity, cost);
            return CommandResult.Success;
        }

        static CommandResult StartNextNight(PlayerState s, List<GameEvent> ev)
        {
            if (s.Phase != GamePhase.Planning) return CommandResult.Fail("Finish tonight's party first.");
            if (s.IsFinalNight) return CommandResult.Fail("That was the final night.");
            s.Night++;
            BeginParty(s, ev);
            return CommandResult.Success;
        }

        // ================================================================== events

        static void Emit(List<GameEvent> ev, GameEventType type, GuestInstance g, int a = 0, int b = 0, int c = 0)
        {
            if (ev == null) return;
            ev.Add(new GameEvent
            {
                Type = type,
                GuestId = g != null ? g.Id : -1,
                Guest = g != null ? g.Kind : default,
                A = a,
                B = b,
                C = c,
            });
        }
    }
}
