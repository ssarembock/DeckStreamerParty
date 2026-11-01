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

        /// <summary>The Audience every season starts with. These copies don't come out of the shared shop.</summary>
        static readonly (GuestType type, int count)[] s_StartingAudience =
        {
            (GuestType.LoyalFan, 4),
            (GuestType.Whale, 2),
            (GuestType.IneffectiveTroll, 4),
        };

        // ================================================================== setup

        public static PlayerState CreateGame(int scenarioId, uint seed, int nights = GameRules.DefaultNights, StreamerType streamer = StreamerType.None)
        {
            if (!StreamerCatalog.IsValid(streamer)) streamer = StreamerType.None;
            var s = new PlayerState
            {
                ScenarioId = scenarioId,
                Seed = seed,
                TotalNights = Math.Max(1, nights),
                Capacity = GameRules.StartingCapacity,
                RngState = DeterministicRandom.Seed(seed),
                Night = 1,
                Streamer = streamer,
            };
            // Bro Jogan's Three-Hour Episode: one extra seat from the start.
            if (streamer == StreamerType.BroJogan) s.Capacity += StreamerCatalog.BigStudioCapacity;
            s.Shop.AddRange(ScenarioCatalog.BuildShop(scenarioId, seed));

            foreach (var (type, count) in s_StartingAudience)
                for (int i = 0; i < count; i++) AddGuest(s, type);

            BeginParty(s, null);
            return s;
        }

        /// <summary>How many copies of this type every season starts with.</summary>
        public static int StartingCopies(GuestType type)
        {
            foreach (var (startType, count) in s_StartingAudience)
                if (startType == type) return count;
            return 0;
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
            if (s == null) return CommandResult.Fail(Err.Of("err.no_game"));
            if (s.IsGameOver) return CommandResult.Fail(Err.Of("err.season_over"));

            switch (cmd.Type)
            {
                case CommandType.OpenDoor: return OpenDoor(s, events);
                case CommandType.UseAbility:
                {
                    var result = UseAbility(s, cmd, events);
                    if (result.Ok) RefreshFreeAction(s, cmd.Actor, events);
                    return result;
                }
                case CommandType.AdmitPeeked: return AdmitPeeked(s, events);
                case CommandType.RejectPeeked: return RejectPeeked(s, events);
                case CommandType.ResolveAttraction: return ResolveAttraction(s, cmd.TargetA, cmd.TargetB, events);
                case CommandType.EndParty: return EndParty(s, events);
                case CommandType.BlameGuest: return Blame(s, cmd.TargetA, events);
                case CommandType.BuyGuest: return Buy(s, cmd.Guest, events);
                case CommandType.ExpandHouse: return Expand(s, events);
                case CommandType.StartNextNight: return StartNextNight(s, events);
                case CommandType.UseStreamerPower: return UsePower(s, events);
                default: return CommandResult.Fail(Err.Of("err.unknown_command"));
            }
        }

        // ================================================================== queries: Platform Ban Risk

        /// <summary>Whether the card itself carries Platform Ban Risk (a Hate-Watcher only while hate-watching).</summary>
        public static bool PrintedTrouble(GuestInstance g)
        {
            if (g == null) return false;
            return g.Info.Trouble || (g.Kind == GuestType.HateWatcher && g.Wolf);
        }

        /// <summary>Whether a guest waiting in the queue would bring Platform Ban Risk in with them (a Hate-Watcher flips on entry).</summary>
        public static bool WouldArriveRisky(GuestInstance g)
        {
            if (g == null || g.TroubleCleared) return false;
            return g.Info.Trouble || (g.Kind == GuestType.HateWatcher && !g.Wolf);
        }

        /// <summary>How much Platform Ban Risk this guest adds right now.</summary>
        public static int GuestTrouble(PlayerState s, GuestInstance g)
        {
            if (g == null) return 0;
            int printed = !g.TroubleCleared && PrintedTrouble(g) ? 1 : 0;
            return printed + g.PartyTrouble;
        }

        public static bool HasTroubleProperty(PlayerState s, GuestInstance g) => GuestTrouble(s, g) > 0;

        /// <summary>Platform Ban Risk from everyone in chat, plus heat from rivals' raids.</summary>
        public static int TroubleTotal(PlayerState s)
        {
            int total = s.PartyHeat;
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

        /// <summary>Whether this guest is cancelling Platform Ban Risk right now (Cheems only does with a Doge).</summary>
        public static bool CancelsTroubleNow(PlayerState s, GuestInstance g)
        {
            if (g == null) return false;
            if (g.Info.CancelsTrouble) return true;
            return g.Kind == GuestType.Cheems && s.CountInHouse(GuestType.Doge) > 0;
        }

        /// <summary>Platform Ban Risk cancelled by viewers, plus Shinobi's Smoke Bomb.</summary>
        public static int CancelCount(PlayerState s)
        {
            int count = s.PartyShield;
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

        /// <summary>The guest's printed Cash tonight (after NPC copies and permanent changes such as Checkout Code).</summary>
        public static int PrintedCash(GuestInstance g) => (g.HasCopy ? g.CopyCash : g.Info.Cash) + g.BonusCash;

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
            public int Doges, Cheems, Gigachads, Sigmas, Knuckles, Harolds, LoyalFans;
            public int Families;
            public int BigPrinted;
            public int NetTrouble;
            public int Troublemakers;
            public int Empty;
            public bool Full;
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
                pop += s.PartyViewerBonus;
                if (s.Streamer == StreamerType.IRunFast && GuestTrouble(s, g) > 0) pop += 1;
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

                // Softball Interview: everyone who was here earns no Subs tonight.
                if (g.SubsMuted) pop = 0;

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
                Troublemakers = TroublemakerCount(s),
                Empty = s.EmptySpaces,
                Full = s.HouseFull,
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
                    case GuestType.LoyalFan: ctx.LoyalFans++; break;
                }
                // Undecided (TBD) groups don't count as a group yet.
                if (g.Kind != GuestType.Doge && g.Info.Category != GuestCategory.Tbd) familyMask |= 1 << (int)g.Info.Category;
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

                // --- Audience classics ---
                case GuestType.ComedianWithAPodcast:
                    if (ctx.Full) pop += GameRules.ComedianFullBonus;
                    break;
                case GuestType.Gatekeeper:
                    pop += ctx.Empty;
                    break;
                case GuestType.BingeBunny:
                    pop += ctx.LoyalFans;
                    break;
                case GuestType.MasterOfCurrency:
                    cash += GameRules.PerRiskyViewerBonus * ctx.Troublemakers;
                    break;
                case GuestType.ExecutivePr:
                    pop += GameRules.PerRiskyViewerBonus * ctx.Troublemakers;
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

        public static int ExpansionCost(PlayerState s) =>
            GameRules.ExpansionCost(s.Expansions) + (s.Streamer == StreamerType.BroJogan ? StreamerCatalog.BigStudioUpgradeTax : 0);

        // ================================================================== queries: what can be done

        public static string GetDoorBlocker(PlayerState s)
        {
            if (s.Phase != GamePhase.Party) return Err.Of("err.stream_over");
            if (s.PeekedId >= 0) return Err.Of("err.decide_waiting");
            if (s.AttractionPending) return Err.Of("err.decide_attraction");
            if (s.HouseFull) return Err.Of("err.sub_cap");
            if (s.Pool.Count == 0) return Err.Of("err.queue_empty");
            return null;
        }

        public static string GetEndPartyBlocker(PlayerState s)
        {
            if (s.Phase != GamePhase.Party) return Err.Of("err.stream_over");
            if (s.PeekedId >= 0) return Err.Of("err.decide_waiting");
            if (s.AttractionPending) return Err.Of("err.decide_attraction");
            return null;
        }

        /// <summary>Null when the guest's action can be started (targets are validated separately).</summary>
        public static string GetAbilityBlocker(PlayerState s, int actorId)
        {
            if (s.Phase != GamePhase.Party) return Err.Of("err.stream_over");
            if (s.PeekedId >= 0) return Err.Of("err.decide_waiting");
            if (s.AttractionPending) return Err.Of("err.decide_attraction");
            if (!s.House.Contains(actorId)) return Err.Of("err.not_in_chat");

            var actor = s.Find(actorId);
            if (actor == null) return Err.Of("err.unknown_viewer");
            var info = actor.Info;
            if (!info.HasAbility) return Err.Of("err.no_action", Err.Guest(actor.Kind));
            if (s.UsedAbilities.Contains(actorId)) return Err.Of("err.action_used", Err.Action(actor.Kind));

            switch (info.Ability)
            {
                case AbilityKind.Fetch:
                    if (s.Pool.Count == 0) return Err.Of("err.queue_empty");
                    if (s.HouseFull) return Err.Of("err.sub_cap");
                    break;
                case AbilityKind.Inspect:
                    if (s.Pool.Count < 2) return Err.Of("err.needs_two_queued");
                    break;
                case AbilityKind.SideEye:
                    if (s.Pool.Count < 2) return Err.Of("err.needs_two_queued");
                    break;
                case AbilityKind.Invest:
                    if (s.Pop < GameRules.StonksInvestPop && s.Cash < GameRules.StonksInvestCash) return Err.Of("err.nothing_to_invest");
                    break;
                case AbilityKind.Reroll:
                    if (!CanReroll(s) && s.Pool.Count == 0) return Err.Of("err.nothing_to_reroll");
                    break;
                case AbilityKind.Arson:
                    if (WouldRaidWithExtraTrouble(s)) return Err.Of("err.would_ban");
                    break;
                case AbilityKind.Peek:
                    if (s.Pool.Count == 0) return Err.Of("err.queue_empty");
                    break;
                case AbilityKind.Calm:
                    if (TroublemakerCount(s) == 0) return Err.Of("err.no_risky");
                    break;
                case AbilityKind.Interview:
                    if (s.PartyInterviewUsed) return Err.Of("err.interview_used");
                    break;
            }

            GuestInfo.TargetCount(info.Ability, out int min, out _);
            if (info.Target != TargetMode.WaitingGuest && min > 0 && CountValidTargets(s, actorId) < min)
                return min == 1 ? Err.Of("err.no_target") : Err.Of("err.needs_targets", min);
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
                case AbilityKind.Photograph:
                    return !self;
                case AbilityKind.Style:
                    return PrintedCash(target) < GameRules.PermanentCashCap;
                case AbilityKind.BootPair:
                {
                    if (IsProtected(s, target)) return false;
                    if (chosen == null || chosen.Count == 0) return HasPairPartner(s, candidateId);
                    return chosen.Count == 1 && AreNeighbours(s, chosen[0], candidateId);
                }
                default:
                    return false;
            }
        }

        /// <summary>Bot Detector: two guests sit next to each other when they're in consecutive chat spots.</summary>
        public static bool AreNeighbours(PlayerState s, int a, int b)
        {
            int ia = s.House.IndexOf(a);
            int ib = s.House.IndexOf(b);
            return ia >= 0 && ib >= 0 && Math.Abs(ia - ib) == 1;
        }

        /// <summary>Whether a guest has a neighbour Bot Detector could remove together with them.</summary>
        static bool HasPairPartner(PlayerState s, int id)
        {
            int index = s.House.IndexOf(id);
            for (int offset = -1; offset <= 1; offset += 2)
            {
                int j = index + offset;
                if (j < 0 || j >= s.House.Count) continue;
                var neighbour = s.Find(s.House[j]);
                if (neighbour != null && !neighbour.Immune && !IsProtected(s, neighbour)) return true;
            }
            return false;
        }

        public static string ValidateCommand(PlayerState s, GameCommand cmd)
        {
            var actor = s.Find(cmd.Actor);
            if (actor == null) return Err.Of("err.unknown_viewer");
            var info = actor.Info;
            var kind = info.Ability;

            var options = info.Options;
            if (options != null && (cmd.Option < 0 || cmd.Option >= options.Length)) return Err.Of("err.pick_option");

            switch (kind)
            {
                case AbilityKind.Invest:
                    if (cmd.Option == 0 && s.Pop < GameRules.StonksInvestPop) return Err.Of("err.needs_gifted_subs", GameRules.StonksInvestPop);
                    if (cmd.Option == 1 && s.Cash < GameRules.StonksInvestCash) return Err.Of("err.needs_cash", GameRules.StonksInvestCash);
                    return null;
                case AbilityKind.Reroll:
                    if (cmd.Option == 0 && !CanReroll(s)) return Err.Of("err.no_reroll");
                    if (cmd.Option == 1 && s.Pool.Count == 0) return Err.Of("err.queue_empty");
                    return null;
                case AbilityKind.Inspect:
                {
                    int looked = Math.Min(GameRules.GrumpyInspectCount, s.Pool.Count);
                    int rejected = s.Pool.IndexOf(cmd.TargetA);
                    if (rejected < 0 || rejected >= looked) return Err.Of("err.pick_reject");
                    if (cmd.TargetB >= 0)
                    {
                        int first = s.Pool.IndexOf(cmd.TargetB);
                        if (first < 0 || first >= looked || cmd.TargetB == cmd.TargetA) return Err.Of("err.pick_next");
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
                return min == max ? Err.Of("err.pick_exact", min) : Err.Of("err.pick_range", min, max);

            var chosen = new List<int>(3);
            foreach (var id in picks)
            {
                if (!IsValidTarget(s, cmd.Actor, id, chosen)) return Err.Of("err.invalid_target");
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
            if (s.Phase != GamePhase.Planning) return Err.Of("err.recruit_between");
            if (!s.Shop.Contains(type)) return Err.Of("err.not_in_set");
            var info = GuestCatalog.Get(type);
            if (!info.Purchasable) return Err.Of("err.cant_buy", Err.Guest(type));
            if (s.CountOwned(type) >= info.MaxCopies) return Err.Of("err.max_copies", info.MaxCopies, Err.Guest(type));
            if (SharedLeft(s, type) <= 0) return Err.Of("err.sold_out", Err.Guest(type));
            if (s.Pop < info.Cost) return Err.Of("err.needs_gifted_subs", info.Cost);
            return null;
        }

        // ------------------------------------------------------------------ the shared shop

        /// <summary>Copies of a type this player took from the shop (everything beyond the starting Audience).</summary>
        public static int TakenFromShop(PlayerState s, GuestType type) => Math.Max(0, s.CountOwned(type) - StartingCopies(type));

        /// <summary>
        /// Copies still in the shop for every player (the shop's stock is shared by the whole match). Diamonds are
        /// unlimited (<see cref="int.MaxValue"/>).
        /// </summary>
        public static int SharedLeft(PlayerState s, GuestType type)
        {
            if (GuestCatalog.Get(type).IsDiamond) return int.MaxValue;
            int rivals = (int)type < s.RivalTaken.Length ? s.RivalTaken[(int)type] : 0;
            return Math.Max(0, GameRules.SharedStockPerGuest - TakenFromShop(s, type) - rivals);
        }

        /// <summary>
        /// How many more copies this player can buy: the smaller of their own copy limit and the shared stock.
        /// <see cref="int.MaxValue"/> means unlimited (Diamonds).
        /// </summary>
        public static int ShopLeft(PlayerState s, GuestType type)
        {
            var info = GuestCatalog.Get(type);
            if (!info.Purchasable) return 0;
            if (info.IsDiamond) return int.MaxValue;
            return Math.Max(0, Math.Min(info.MaxCopies - s.CountOwned(type), SharedLeft(s, type)));
        }

        public static string GetExpandBlocker(PlayerState s)
        {
            if (s.Phase != GamePhase.Planning) return Err.Of("err.upgrade_between");
            if (s.Capacity >= GameRules.MaxCapacity) return Err.Of("err.sub_cap_maxed");
            int cost = ExpansionCost(s);
            if (s.Cash < cost) return Err.Of("err.needs_cash", cost);
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
            ApplyStreamerEffects(s, ev);
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
            s.PartyHeat = 0;
            s.PartyShield = 0;
            s.PartyViewerBonus = 0;
            s.PartyFreeActions = 0;
            s.PartyXRay = false;
            s.PartyInterviewUsed = false;

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
            if (s.Phase != GamePhase.Party || s.PeekedId < 0) return CommandResult.Fail(Err.Of("err.nobody_waiting"));
            if (s.HouseFull) return CommandResult.Fail(Err.Of("err.cap_deny"));
            int id = s.PeekedId;
            s.PeekedId = -1;
            Arrive(s, id, -1, -1, false, ev);
            return CommandResult.Success;
        }

        static CommandResult RejectPeeked(PlayerState s, List<GameEvent> ev)
        {
            if (s.Phase != GamePhase.Party || s.PeekedId < 0) return CommandResult.Fail(Err.Of("err.nobody_waiting"));
            int id = s.PeekedId;
            s.PeekedId = -1;
            s.Pool.Remove(id);
            s.Excluded.Add(id);
            Emit(ev, GameEventType.GuestRejected, s.Find(id));
            return CommandResult.Success;
        }

        // ================================================================== arrivals

        /// <summary>
        /// Moves a guest into the party and resolves entrance effects, reactions, overcrowding (Server Down), Platform Ban
        /// Risk and chained arrivals. Returns false when the party was shut down as a consequence.
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

            // Generous Donator: brings in the next viewer and scores them once right away.
            if (g.Kind == GuestType.GenerousDonator && s.Pool.Count > 0)
            {
                int greeted = s.Pool[0];
                if (!Arrive(s, greeted, -1, id, true, ev)) return false;
                var guest = s.Find(greeted);
                if (guest != null && s.House.Contains(greeted)) ScoreNow(s, guest, id, ev);
            }
            return s.Phase == GamePhase.Party;
        }

        /// <summary>
        /// Scores a guest mid-stream (Crypto Guy, Generous Donator) and banks it immediately, so it's kept even if the
        /// stream ends badly. Fees the bank can't cover cost Gifted Subs, as at the end of a stream.
        /// </summary>
        static void ScoreNow(PlayerState s, GuestInstance g, int sourceId, List<GameEvent> ev)
        {
            ScoreGuest(s, g, out int pop, out int cash);
            Emit(ev, GameEventType.ScoredNow, g, pop, cash, sourceId);
            Bank(s, pop, cash, ev);
        }

        /// <summary>Adds Subs and Cash straight to the wallet. A Cash debt the bank can't pay becomes a Gifted Subs penalty.</summary>
        static void Bank(PlayerState s, int pop, int cash, List<GameEvent> ev)
        {
            s.Pop = Math.Max(0, s.Pop + pop);
            s.TotalPopEarned += Math.Max(0, pop);
            s.Cash += cash;
            s.TotalCashEarned += Math.Max(0, cash);
            if (s.Cash >= 0) return;
            int unpaid = -s.Cash;
            int penalty = unpaid * GameRules.CashShortfallPopPenalty;
            s.Cash = 0;
            s.Pop = Math.Max(0, s.Pop - penalty);
            Emit(ev, GameEventType.CashShortfall, null, unpaid, penalty);
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

                case GuestType.HateWatcher:
                    // Hate-watching on the first entrance, harmless on the next, and so on (remembered between streams).
                    g.Wolf = !g.Wolf;
                    Emit(ev, GameEventType.MoodFlipped, g, g.Wolf ? 1 : 0);
                    break;

                case GuestType.GenZWithJob:
                    // Secure The Bag: +1 Cash for good on every entrance, up to the cap, even if the stream fails later.
                    if (PrintedCash(g) < GameRules.PermanentCashCap)
                    {
                        g.BonusCash++;
                        Emit(ev, GameEventType.CashBoosted, g, 1, g.BonusCash);
                    }
                    break;
            }
        }

        /// <summary>Everyone already at the party responds to a newcomer.</summary>
        static void ReactToArrival(PlayerState s, GuestInstance newcomer, List<GameEvent> ev)
        {
            bool troubleArrival = PrintedTrouble(newcomer) && !newcomer.TroubleCleared;
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
            if (s.Phase != GamePhase.Party || !s.AttractionPending) return CommandResult.Fail(Err.Of("err.nothing_to_decide"));

            bool skip = replacedId < 0 || chosenId < 0;
            if (!skip)
            {
                if (!s.AttractionCandidates.Contains(chosenId)) return CommandResult.Fail(Err.Of("err.pick_revealed"));
                if (!IsValidAttractionTarget(s, replacedId)) return CommandResult.Fail(Err.Of("err.cant_replace"));
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

        /// <summary>Clears a guest's Platform Ban Risk for the rest of the party. Returns how much was removed.</summary>
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

        /// <summary>Platform Ban: too much net Platform Ban Risk ends the stream with no payout (internally a "raid").</summary>
        static bool CheckRaid(PlayerState s, List<GameEvent> ev)
        {
            if (s.Phase != GamePhase.Party) return false;
            if (NetTrouble(s) < GameRules.RaidTroubleThreshold) return false;

            // This Is Fine Dog: the first time it would happen, the ban is called off.
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
            SoftLanding(s, ev);
            s.Streak = 0;
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

        /// <summary>Server Down: chat went over the Sub Cap, so the stream ends with no payout (internally the "fire marshal").</summary>
        static void ShutDownFireMarshal(PlayerState s, List<GameEvent> ev)
        {
            int occupancy = s.Occupancy;
            s.FireMarshals++;
            s.Streak = 0;
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

                // --- Audience classics ---

                case AbilityKind.Peek:
                {
                    int peeked = s.Pool[0];
                    s.Pool.RemoveAt(0);
                    s.PeekedId = peeked;
                    Emit(ev, GameEventType.GuestPeeked, s.Find(peeked), actorId);
                    break;
                }

                case AbilityKind.Photograph:
                    ScoreNow(s, s.Find(a), actorId, ev);
                    break;

                case AbilityKind.Calm:
                    foreach (var id in new List<int>(s.House))
                    {
                        var g = s.Find(id);
                        if (g != null && !g.Immune && HasTroubleProperty(s, g)) ClearTrouble(s, g, ev);
                    }
                    break;

                case AbilityKind.BootPair:
                    RemoveFromParty(s, a, LeaveReason.Evicted, actorId, true, ev);
                    if (s.Phase == GamePhase.Party) RemoveFromParty(s, b, LeaveReason.Evicted, actorId, true, ev);
                    CheckRaid(s, ev);
                    break;

                case AbilityKind.Interview:
                {
                    s.PartyInterviewUsed = true;
                    int muted = 0;
                    foreach (var id in s.House)
                    {
                        var g = s.Find(id);
                        if (g == null || g.Immune) continue;
                        g.SubsMuted = true;
                        muted++;
                    }
                    // The stream's Subs pot drops to 0 as well.
                    s.PartyPop = 0;
                    int billionaires = s.CountInHouse(GuestType.BillionaireBenefactor);
                    Emit(ev, GameEventType.InterviewGiven, actor, billionaires, muted);
                    Bank(s, 0, billionaires, ev);
                    break;
                }

                case AbilityKind.Style:
                {
                    var target = s.Find(a);
                    target.BonusCash++;
                    Emit(ev, GameEventType.CashBoosted, target, 1, target.BonusCash);
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

            s.Streak++;
            if (s.Streamer == StreamerType.ManimMakethMan)
            {
                int bonus = Math.Min(StreamerCatalog.StreakMaxBonus, s.Streak);
                pop += bonus;
                Emit(ev, GameEventType.StreakBonus, null, bonus, s.Streak);
            }

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
            if (s.Phase != GamePhase.RaidBlame) return CommandResult.Fail(Err.Of("err.nobody_to_ban"));
            if (!s.RaidSuspects.Contains(guestId) || s.Find(guestId) == null) return CommandResult.Fail(Err.Of("err.not_banned"));

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
            if (!GuestCatalog.IsValid(type)) return CommandResult.Fail(Err.Of("err.unknown_viewer"));
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
            if (s.Phase != GamePhase.Planning) return CommandResult.Fail(Err.Of("err.finish_stream"));
            if (s.IsFinalNight) return CommandResult.Fail(Err.Of("err.final_night"));
            s.Night++;
            BeginParty(s, ev);
            return CommandResult.Success;
        }

        // ================================================================== streamer powers

        /// <summary>Null when the streamer's active power can be used now. Rival powers also need a rival to hit.</summary>
        public static string GetPowerBlocker(PlayerState s, bool rivalsAvailable = false)
        {
            var info = s.StreamerInfo;
            if (s.Streamer == StreamerType.None || info.Kind == PowerKind.None) return Err.Of("err.power_none");
            if (info.IsPassive) return Err.Of("err.power_passive");
            if (s.IsGameOver) return Err.Of("err.season_over");
            if (s.Phase != GamePhase.Planning) return Err.Of("err.power_shop_only");
            if (info.NeedsRival && !rivalsAvailable) return Err.Of("err.power_needs_rival");
            if (s.PowerCooldown > 0) return Err.Of("err.power_cooldown", s.PowerCooldown);
            if (info.Kind == PowerKind.NextStream)
            {
                if (s.PowerArmed) return Err.Of("err.power_armed");
                if (s.IsFinalNight) return Err.Of("err.power_no_next");
            }
            if (s.Streamer == StreamerType.Palmane && PalSphereChoices(s).Count == 0) return Err.Of("err.power_no_pals");

            int cost = StreamerCatalog.Cost(s);
            if (s.Cash < cost) return Err.Of("err.needs_cash", cost);
            return null;
        }

        /// <summary>Null when <paramref name="target"/> can be hit by the actor's rival power.</summary>
        public static string GetRivalTargetBlocker(PlayerState actor, PlayerState target)
        {
            if (target == null || target == actor || target.IsGameOver) return Err.Of("err.power_no_rival");
            if (actor.Streamer == StreamerType.EthicalFboi && target.Cash <= 0) return Err.Of("err.power_rival_broke");
            return null;
        }

        /// <summary>Viewer types Palmane's Pal Sphere could catch from the shop.</summary>
        public static List<GuestType> PalSphereChoices(PlayerState s)
        {
            var result = new List<GuestType>();
            foreach (var type in s.Shop)
            {
                var info = GuestCatalog.Get(type);
                if (!info.Purchasable || info.IsDiamond || info.Cost > StreamerCatalog.PalSphereMaxCost) continue;
                // A caught viewer comes out of the shared shop like a bought one.
                if (ShopLeft(s, type) <= 0 || result.Contains(type)) continue;
                result.Add(type);
            }
            return result;
        }

        static CommandResult UsePower(PlayerState s, List<GameEvent> ev)
        {
            var blocker = GetPowerBlocker(s);
            if (blocker != null) return CommandResult.Fail(blocker);

            PayForPower(s, ev);
            if (s.StreamerInfo.Kind == PowerKind.NextStream)
            {
                s.PowerArmed = true;
            }
            else if (s.Streamer == StreamerType.Palmane)
            {
                var choices = PalSphereChoices(s);
                var type = choices[DeterministicRandom.Range(ref s.RngState, choices.Count)];
                var g = AddGuest(s, type);
                Emit(ev, GameEventType.PalCaught, g);
            }
            return CommandResult.Success;
        }

        /// <summary>
        /// A rival power (Fate's Raid, Ethical F**kboi's Wealth Tax). The host calls this with both players' states, since
        /// the rules engine only ever sees one player at a time. Indexes are the players' positions, used for the log.
        /// </summary>
        public static CommandResult UseRivalPower(PlayerState actor, int actorIndex, PlayerState target, int targetIndex,
            List<GameEvent> actorEvents, List<GameEvent> targetEvents)
        {
            if (actor == null) return CommandResult.Fail(Err.Of("err.no_game"));
            var blocker = GetPowerBlocker(actor, true) ?? GetRivalTargetBlocker(actor, target);
            if (blocker != null) return CommandResult.Fail(blocker);

            PayForPower(actor, actorEvents);
            switch (actor.Streamer)
            {
                case StreamerType.Fate:
                    target.IncomingHeat += StreamerCatalog.RaidHeat;
                    Emit(targetEvents, GameEventType.RaidReceived, null, StreamerCatalog.RaidHeat, actorIndex);
                    break;
                case StreamerType.EthicalFboi:
                {
                    int amount = Math.Min(StreamerCatalog.WealthTaxAmount, target.Cash);
                    target.Cash -= amount;
                    actor.Cash += amount;
                    Emit(targetEvents, GameEventType.TaxPaid, null, amount, actorIndex);
                    Emit(actorEvents, GameEventType.TaxReceived, null, amount, targetIndex);
                    break;
                }
            }
            return CommandResult.Success;
        }

        /// <summary>Actives are paid in Cash; every use makes the next one cost more (see <see cref="StreamerCatalog.Cost"/>).</summary>
        static void PayForPower(PlayerState s, List<GameEvent> ev)
        {
            int cost = StreamerCatalog.Cost(s);
            s.Cash -= cost;
            s.PowerUses++;
            int cooldown = s.StreamerInfo.Cooldown;
            // Stored one higher: it counts down as the next stream starts, so "2" means two turns without it.
            s.PowerCooldown = cooldown > 0 ? cooldown + 1 : 0;
            Emit(ev, GameEventType.PowerUsed, null, (int)s.Streamer, cost);
        }

        /// <summary>Applies armed powers and rivals' raids as a stream starts, and ticks the cooldown.</summary>
        static void ApplyStreamerEffects(PlayerState s, List<GameEvent> ev)
        {
            if (s.PowerCooldown > 0) s.PowerCooldown--;

            if (s.IncomingHeat > 0)
            {
                s.PartyHeat = Math.Min(StreamerCatalog.MaxRaidHeat, s.IncomingHeat);
                s.IncomingHeat = 0;
                Emit(ev, GameEventType.HeatApplied, null, s.PartyHeat);
            }

            if (!s.PowerArmed) return;
            s.PowerArmed = false;
            switch (s.Streamer)
            {
                case StreamerType.Shinobi:
                    s.PartyShield = StreamerCatalog.SmokeBombShield;
                    Emit(ev, GameEventType.ShieldApplied, null, s.PartyShield);
                    break;
                case StreamerType.MrShark:
                    s.PartyViewerBonus = StreamerCatalog.GiveawayBonus;
                    Emit(ev, GameEventType.GiveawayActive, null, s.PartyViewerBonus);
                    break;
                case StreamerType.StrategicAbomination:
                    s.PartyFreeActions = 1;
                    Emit(ev, GameEventType.DoubleMoveActive, null, s.PartyFreeActions);
                    break;
                case StreamerType.RudeRadiologist:
                    s.PartyXRay = true;
                    Emit(ev, GameEventType.XRayActive, null, StreamerCatalog.XRayCount);
                    break;
            }
        }

        /// <summary>Strategic Abomination's Double Move: the first action used this stream can be used again.</summary>
        static void RefreshFreeAction(PlayerState s, int actorId, List<GameEvent> ev)
        {
            if (s.PartyFreeActions <= 0 || s.Phase != GamePhase.Party || !s.UsedAbilities.Remove(actorId)) return;
            s.PartyFreeActions--;
            Emit(ev, GameEventType.FreeActionUsed, s.Find(actorId));
        }

        /// <summary>Soft Hard Body Man: a stream that gets a Platform Ban still banks half its Subs.</summary>
        static void SoftLanding(PlayerState s, List<GameEvent> ev)
        {
            if (s.Streamer != StreamerType.SoftHardBodyMan) return;
            ProjectScore(s, out int pop, out _);
            int banked = Math.Max(0, pop) / 2;
            if (banked <= 0) return;
            s.Pop += banked;
            s.TotalPopEarned += banked;
            Emit(ev, GameEventType.SoftLanding, null, banked);
        }

        /// <summary>The next viewers in the queue that X-Ray Vision reveals this stream (empty without it).</summary>
        public static List<int> XRayQueue(PlayerState s)
        {
            var result = new List<int>();
            if (!s.PartyXRay || s.Phase != GamePhase.Party) return result;
            for (int i = 0; i < s.Pool.Count && i < StreamerCatalog.XRayCount; i++) result.Add(s.Pool[i]);
            return result;
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
