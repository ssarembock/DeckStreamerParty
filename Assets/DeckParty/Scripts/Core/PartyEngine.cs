using System;
using System.Collections.Generic;

namespace DeckParty.Core
{
    /// <summary>
    /// The authoritative Deck Party rules. Pure and deterministic: given the same state and command it always
    /// produces the same result, which is what lets the host simulate every player's game and stream snapshots
    /// to clients. All randomness comes from the RNG state stored in <see cref="PlayerState"/>.
    /// </summary>
    public static partial class PartyEngine
    {
        const int InfectionCash = 2;

        /// <summary>The Audience every season starts with. These copies don't come out of the shared shop.</summary>
        static readonly (GuestType type, int count)[] s_StartingAudience =
        {
            (GuestType.LoyalFan, 4),
            (GuestType.Fish, 2),
            (GuestType.IneffectiveTroll, 5),
        };

        // ================================================================== setup

        /// <param name="mode">Classic (win with <paramref name="trophiesToWin"/> Trophies) or Endless (last as many streams as you can).</param>
        /// <param name="members">1, or 2 for a team (2v2, co-op): each member gets the starting Audience and their own Sub Cap.</param>
        /// <param name="trophiesToWin">Trophies in one scored stream that win: 4 in single player, 3 in competitive multiplayer.</param>
        /// <param name="partnerStreamer">Teams: member 1's own streamer (member 0 plays <paramref name="streamer"/>). Never the same one.</param>
        public static PlayerState CreateGame(int scenarioId, uint seed, int nights = GameRules.DefaultNights, StreamerType streamer = StreamerType.None,
            GameMode mode = GameMode.Classic, int members = 1, int trophiesToWin = GameRules.DiamondsToWin,
            StreamerType partnerStreamer = StreamerType.None)
        {
            if (!StreamerCatalog.IsValid(streamer)) streamer = StreamerType.None;
            if (!StreamerCatalog.IsValid(partnerStreamer) || partnerStreamer == streamer) partnerStreamer = StreamerType.None;
            members = Math.Max(1, Math.Min(GameRules.TeamSize, members));
            var s = new PlayerState
            {
                ScenarioId = scenarioId,
                Seed = seed,
                TotalNights = Math.Max(1, nights),
                Capacity = GameRules.StartingCapacity,
                PartnerCapacity = GameRules.StartingCapacity,
                RngState = DeterministicRandom.Seed(seed),
                Night = 1,
                Streamer = streamer,
                Mode = mode,
                Members = members,
                TrophiesToWin = Math.Max(1, trophiesToWin),
                PartnerStreamer = members > 1 ? partnerStreamer : StreamerType.None,
            };
            s.Shop.AddRange(ScenarioCatalog.BuildShop(scenarioId, seed));

            for (int member = 0; member < members; member++)
                foreach (var (type, count) in s_StartingAudience)
                    for (int i = 0; i < count; i++) AddGuest(s, type, false, member);

            BeginParty(s, null);
            return s;
        }

        /// <summary>Trophies in one scored stream that win this season (4 in single player, 3 in competitive multiplayer).</summary>
        public static int TrophiesToWin(PlayerState s) => s != null && s.TrophiesToWin > 0 ? s.TrophiesToWin : GameRules.DiamondsToWin;

        /// <summary>
        /// The highest Sub Cap one player's chat can reach in this season: 40 solo (4 rows of 10), 50 a player in a team (5 rows
        /// of 10 on each side; 2026-11-18 UI updates). The Hype Men's spare seats come on top, in one extra row.
        /// </summary>
        public static int MaxCapacity(PlayerState s) => GameRules.MaxCapacityFor(s != null && s.IsTeam);

        /// <summary>How many copies of this type every season starts with.</summary>
        public static int StartingCopies(GuestType type)
        {
            foreach (var (startType, count) in s_StartingAudience)
                if (startType == type) return count;
            return 0;
        }

        static GuestInstance AddGuest(PlayerState s, GuestType type, bool temporary = false, int owner = 0)
        {
            var g = new GuestInstance { Id = s.NextInstanceId++, Type = type, Temporary = temporary, Owner = (byte)(s.IsTeam ? Math.Max(0, Math.Min(1, owner)) : 0) };
            s.Rolodex.Add(g);
            return g;
        }

        // ================================================================== command dispatch

        public static CommandResult Execute(PlayerState s, GameCommand cmd, List<GameEvent> events)
        {
            if (s == null) return CommandResult.Fail(Err.Of("err.no_game"));
            if (s.IsGameOver) return CommandResult.Fail(Err.Of("err.season_over"));

            // Teams: the member sending it (the host fills it in from the sender). A solo season only has member 0.
            int member = s.IsTeam ? Math.Max(-1, Math.Min(1, cmd.Member)) : 0;
            if (member < 0 && cmd.Type != CommandType.EndParty) member = 0;

            switch (cmd.Type)
            {
                case CommandType.OpenDoor: return OpenDoor(s, member, events);
                case CommandType.UseAbility:
                {
                    var result = UseAbility(s, cmd, member, events);
                    if (result.Ok) RefreshFreeAction(s, cmd.Actor, events);
                    return result;
                }
                case CommandType.AdmitPeeked: return AdmitPeeked(s, events);
                case CommandType.RejectPeeked: return RejectPeeked(s, events);
                case CommandType.ResolveAttraction: return ResolveAttraction(s, cmd.TargetA, cmd.TargetB, events);
                case CommandType.EndParty: return EndParty(s, member, events);
                case CommandType.BlameGuest: return Blame(s, cmd.TargetA, events);
                case CommandType.BuyGuest: return Buy(s, cmd.Guest, member, events);
                case CommandType.ExpandHouse: return Expand(s, member, events);
                case CommandType.StartNextNight: return StartNextNight(s, events);
                case CommandType.UseStreamerPower: return UsePower(s, member, events);
                case CommandType.RemoveGuest: return Dismiss(s, cmd.TargetA, member, events);
                case CommandType.BuyExtraStream: return BuyExtraStream(s, events);
                case CommandType.BuyShopViewer: return BuyShopViewer(s, cmd.Guest, events);
                default: return CommandResult.Fail(Err.Of("err.unknown_command"));
            }
        }

        // ================================================================== queries: Deplatform Risk

        /// <summary>Whether the card itself carries Deplatform Risk (a Hate-Watcher only while hate-watching).</summary>
        public static bool PrintedTrouble(GuestInstance g)
        {
            if (g == null) return false;
            return g.Info.Trouble || (g.Kind == GuestType.HateWatcher && g.Wolf);
        }

        /// <summary>Whether a guest waiting in the queue would bring Deplatform Risk in with them (a Hate-Watcher flips on entry).</summary>
        public static bool WouldArriveRisky(GuestInstance g)
        {
            if (g == null || g.TroubleCleared) return false;
            return g.Info.Trouble || (g.Kind == GuestType.HateWatcher && !g.Wolf);
        }

        /// <summary>How much Deplatform Risk this guest adds right now.</summary>
        public static int GuestTrouble(PlayerState s, GuestInstance g)
        {
            if (g == null) return 0;
            int printed = !g.TroubleCleared && PrintedTrouble(g) ? 1 : 0;
            return printed + g.PartyTrouble;
        }

        public static bool HasTroubleProperty(PlayerState s, GuestInstance g) => GuestTrouble(s, g) > 0;

        /// <summary>Deplatform Risk from everyone in chat, plus heat from rivals' raids.</summary>
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

        /// <summary>Whether this guest is cancelling Deplatform Risk right now (Cheems only does with a Doge).</summary>
        public static bool CancelsTroubleNow(PlayerState s, GuestInstance g)
        {
            if (g == null) return false;
            if (g.Info.CancelsTrouble) return true;
            return g.Kind == GuestType.Cheems && s.CountInHouse(GuestType.DegenDog) > 0;
        }

        /// <summary>Deplatform Risk cancelled by viewers, the Core Fanbase (1 per 3 points) and Shinobi's Smoke Bomb.</summary>
        public static int CancelCount(PlayerState s)
        {
            int count = s.PartyShield + FanbaseCancels(s);
            for (int i = 0; i < s.House.Count; i++)
                if (CancelsTroubleNow(s, s.Find(s.House[i]))) count++;
            return count;
        }

        /// <summary>Deplatform Risk the Core Fanbase cancels right now.</summary>
        public static int FanbaseCancels(PlayerState s) => Communities.CoreFanbaseCancels(CommunityScore(s, Community.CoreFanbase));

        public static int NetTrouble(PlayerState s) => Math.Max(0, TroubleTotal(s) - CancelCount(s));

        /// <summary>
        /// Net Deplatform Risk that gets this stream deplatformed: 3, or 4 for a team (2v2, co-op Endless), whose two players
        /// share one Risk count across both sides of the chat.
        /// </summary>
        public static int RiskLimit(PlayerState s) => s != null && s.IsTeam ? GameRules.TeamRiskLimit : GameRules.RaidTroubleThreshold;

        // ================================================================== queries: communities

        /// <summary>
        /// How many <b>different</b> viewers in chat belong to each community right now (indexed by <see cref="Community"/>).
        /// Two copies of the same card count once; a Rickrolled viewer counts as the character they've become.
        /// </summary>
        public static int[] CommunityMembers(PlayerState s)
        {
            var members = new int[Communities.Count];
            if (s == null || s.House.Count == 0) return members;
            var seen = new bool[GuestCatalog.Count];
            for (int i = 0; i < s.House.Count; i++)
            {
                var g = s.Find(s.House[i]);
                if (g == null) continue;
                int kind = (int)g.Kind;
                if (kind < 0 || kind >= seen.Length || seen[kind]) continue;
                seen[kind] = true;
                int mask = (int)GuestCatalog.CommunitiesOf(g.Kind);
                for (int c = 0; c < Communities.Count; c++)
                    if ((mask & (1 << c)) != 0) members[c]++;
            }
            return members;
        }

        /// <summary>
        /// Each community's score right now (indexed by <see cref="Community"/>): its different viewers in chat
        /// (<see cref="CommunityMembers"/>) minus one, as a lone viewer is not a community, so it scores 0. The Boomers have
        /// levels instead: 2 different Boomers for level 1, 4 for level 2.
        /// </summary>
        public static int[] CommunityScores(PlayerState s)
        {
            var scores = CommunityMembers(s);
            for (int c = 0; c < scores.Length; c++) scores[c] = Communities.PointsFromMembers((Community)c, scores[c]);
            return scores;
        }

        public static int CommunityScore(PlayerState s, Community community) => CommunityScores(s)[(int)community];

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

        /// <summary>The guest's printed Cash tonight (after Ballerina swaps, NPC copies and permanent changes).</summary>
        public static int PrintedCash(GuestInstance g)
        {
            if (g.HasOverride) return g.PrintedOverride;
            int baseCash = g.HasCopy ? g.CopyCash : g.Info.Cash;
            return baseCash + g.BonusCash;
        }

        /// <summary>The guest's printed Influence tonight (after NPC copies and permanent changes such as Checkout Code).</summary>
        public static int PrintedInfluence(GuestInstance g) => (g.HasCopy ? g.CopyInfluence : g.Info.Influence) + g.BonusInfluence;

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
            /// <summary>Group-scoring cards in chat (Dancing Jen, Crypto Enthusiast) and the first copy of each (index), or -1.</summary>
            public int Jens, Cryptos, JenLead = -1, CryptoLead = -1;
            /// <summary>Different communities among the viewers in chat other than Doges.</summary>
            public int CommunityCount;
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
        public static void ScoreGuest(PlayerState s, GuestInstance g, out int cash, out int influence)
        {
            int index = s.House.IndexOf(g.Id);
            if (index < 0)
            {
                cash = PrintedCash(g);
                influence = PrintedInfluence(g);
                return;
            }
            ScoreHouse(s, out var cashes, out var influences);
            cash = cashes[index];
            influence = influences[index];
        }

        /// <summary>Scores everyone at the party in one pass (several guests depend on the others).</summary>
        public static void ScoreHouse(PlayerState s, out int[] cashes, out int[] influences)
        {
            int n = s.House.Count;
            cashes = new int[n];
            influences = new int[n];
            var guests = new GuestInstance[n];
            for (int i = 0; i < n; i++) guests[i] = s.Find(s.House[i]);

            var ctx = BuildContext(s, guests);
            int prevented = 0;
            var printed = new int[n];

            for (int i = 0; i < n; i++)
            {
                var g = guests[i];
                if (g == null) continue;

                int baseCash = PrintedCash(g);
                int baseInfluence = PrintedInfluence(g);
                printed[i] = baseCash;
                int cash = baseCash;
                int influence = baseInfluence;

                ApplyPassive(g, i, ctx, ref cash, ref influence);
                // Giveaway (Mr Shark) and Bark at Chat (IRunFast), armed for this stream. In a team they pay the viewers on
                // the side of the player whose streamer it is (each member plays their own streamer since 2026-11-18).
                if (s.PartyViewerBonus > 0 && s.OnSideOf(g, StreamerType.MrShark)) cash += s.PartyViewerBonus;
                if (s.PartyBark > 0 && GuestTrouble(s, g) > 0 && s.OnSideOf(g, StreamerType.IRunFast)) cash += s.PartyBark;
                cash += g.PartyCash;
                influence += g.PartyInfluence;
                if (g.Infected) cash += InfectionCash;

                if (g.Zeroed)
                {
                    cash = 0;
                    influence = 0;
                }
                if (g.DoubleMode == 1) cash += baseCash;
                else if (g.DoubleMode == 2) influence += baseInfluence;

                if (cash > 0 && IsProtected(s, g)) cash *= 2;

                if (ctx.Harolds > 0 && !g.Immune && cash < baseCash)
                {
                    cash = baseCash;
                    prevented++;
                }

                // Softball Interview: everyone who was here earns no Cash tonight.
                if (g.CashMuted) cash = 0;

                cashes[i] = cash;
                influences[i] = influence;
            }

            if (ctx.Harolds > 0)
            {
                int bonus = Math.Min(GameRules.HaroldMaxBonus, prevented);
                for (int i = 0; i < n; i++)
                    if (guests[i] != null && guests[i].Kind == GuestType.HideThePainHarold) cashes[i] += bonus;
            }

            // Confused Math Lady conversions happen last so they convert the guest's whole take.
            for (int i = 0; i < n; i++)
            {
                var g = guests[i];
                if (g == null) continue;
                if (g.ConvertMode == 1 && cashes[i] > 0)
                {
                    influences[i] += cashes[i] / 2;
                    cashes[i] = 0;
                }
                else if (g.ConvertMode == 2 && influences[i] > 0)
                {
                    cashes[i] += influences[i] * 2;
                    influences[i] = 0;
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
                Full = IsChatFull(s),
                Guests = guests.Length,
                ChungusBonus = Math.Max(0, Math.Min(GameRules.BigChungusMaxBonus, s.Capacity - GameRules.BigChungusCapacityBase)),
                Suffering = new bool[guests.Length],
            };

            var communities = CommunityMask.None;
            foreach (var g in guests)
            {
                if (g == null) continue;
                switch (g.Kind)
                {
                    case GuestType.DegenDog: ctx.Doges++; break;
                    case GuestType.Cheems: ctx.Cheems++; break;
                    case GuestType.Gigachad: ctx.Gigachads++; break;
                    case GuestType.SigmaMale: ctx.Sigmas++; break;
                    case GuestType.UgandanKnuckles: ctx.Knuckles++; break;
                    case GuestType.HideThePainHarold: ctx.Harolds++; break;
                    case GuestType.LoyalFan: ctx.LoyalFans++; break;
                }
                // Doge's Such Synergy counts communities (the old character families are no longer used).
                if (g.Kind != GuestType.DegenDog) communities |= GuestCatalog.CommunitiesOf(g.Kind);
                if (PrintedCash(g) >= 5) ctx.BigPrinted++;
            }
            ctx.CommunityCount = Communities.CountIn(communities);

            for (int i = 0; i < guests.Length; i++)
            {
                var g = guests[i];
                if (g == null) continue;
                if (g.Kind == GuestType.DancingJen && ctx.Jens++ == 0) ctx.JenLead = i;
                else if (g.Kind == GuestType.CryptoEnthusiast && ctx.Cryptos++ == 0) ctx.CryptoLead = i;
            }

            for (int i = 0; i < guests.Length; i++)
            {
                var g = guests[i];
                if (g == null) continue;
                bool penalised = (g.Kind == GuestType.Gigachad && ctx.Gigachads > 1) ||
                                 (g.Kind == GuestType.SigmaMale && ctx.Sigmas > 1) ||
                                 (g.Kind == GuestType.Cheems && ctx.Doges == 0 && ctx.NetTrouble > 0);
                bool suffering = PrintedCash(g) < 0 || PrintedInfluence(g) < 0 || GuestTrouble(s, g) > 0 || g.Infected || g.Zeroed || penalised;
                ctx.Suffering[i] = suffering;
                if (suffering) ctx.Sufferers++;
            }
            return ctx;
        }

        static void ApplyPassive(GuestInstance g, int index, ScoreContext ctx, ref int cash, ref int influence)
        {
            // Risky variants (Bunker Billionaire, Terminally Online...) score like the card they copy.
            switch (GuestCatalog.BehavesAs(g.Kind))
            {
                case GuestType.DegenDog:
                    cash += Math.Min(GameRules.DogeMaxBonus, ctx.CommunityCount);
                    if (ctx.Cheems > 0 && !g.Immune) cash += 2;
                    break;
                case GuestType.Gigachad:
                    if (ctx.BigPrinted == 1 && PrintedCash(g) >= 5) cash += GameRules.GigachadSoloBonus;
                    cash -= GameRules.GigachadCrowdPenalty * (ctx.Gigachads - 1);
                    break;
                case GuestType.Wojak:
                {
                    int others = ctx.Sufferers - (ctx.Suffering[index] ? 1 : 0);
                    cash += Math.Min(GameRules.WojakMaxBonus, GameRules.WojakPerSufferer * others);
                    break;
                }
                case GuestType.Cheems:
                    if (ctx.Doges > 0) cash += 2;
                    else cash -= ctx.NetTrouble;
                    break;
                case GuestType.Doomer:
                    // Doomscroll (2026-11-10: no more +2 Influence at exactly 2 Risk).
                    cash += 2 * ctx.NetTrouble;
                    break;
                case GuestType.ForeverAlone:
                    cash += ctx.Guests == 1 ? GameRules.ForeverAloneSoloBonus : ctx.Empty;
                    break;
                case GuestType.UgandanKnuckles:
                    cash += ctx.Knuckles - 1;
                    break;
                case GuestType.BigChungus:
                    cash += ctx.ChungusBonus;
                    break;
                case GuestType.SigmaMale:
                    cash += Math.Min(GameRules.SigmaMaxBonus, ctx.Empty) - (ctx.Sigmas - 1);
                    break;
                case GuestType.Pepe:
                    if (g.Mood == PepeMood.Happy) cash += 3;
                    else if (g.Mood == PepeMood.Sad) influence += 3;
                    else if (g.Mood == PepeMood.Angry) cash += 6;
                    break;

                // --- Audience classics ---
                case GuestType.PromoCodeUser:
                    if (ctx.Full) influence += GameRules.PromoCodeFullInfluence;
                    break;
                // You Shall Not Watch!: +1 per empty seat (Influence for the Influential Isolationist, Cash for the Billionaire one).
                case GuestType.InfluentialIsolationist:
                    influence += ctx.Empty;
                    break;
                case GuestType.BillionaireIsolationist:
                    cash += ctx.Empty;
                    break;
                case GuestType.BingeBunny:
                    cash += ctx.LoyalFans;
                    break;
                case GuestType.MasterOfCurrency:
                    influence += GameRules.PerRiskyViewerBonus * ctx.Troublemakers;
                    break;
                case GuestType.ExecutivePr:
                    cash += GameRules.PerRiskyViewerBonus * ctx.Troublemakers;
                    break;

                // --- Group-scoring cards: the copies in chat share one total from the table ---
                case GuestType.DancingJen:
                    cash += GroupShare(GameRules.DancingJenCash(ctx.Jens), ctx.Jens, index == ctx.JenLead);
                    break;
                case GuestType.CryptoEnthusiast:
                    influence += GroupShare(GameRules.CryptoEnthusiastInfluence(ctx.Cryptos), ctx.Cryptos, index == ctx.CryptoLead);
                    break;
            }
        }

        /// <summary>
        /// One copy's part of a group total: the total split evenly (3 Jens score 3 each = 9), the remainder on the first
        /// copy in chat (2 Crypto Enthusiasts: +1 and 0). The parts always add up to the table's total.
        /// </summary>
        static int GroupShare(int total, int copies, bool lead)
        {
            if (copies <= 0) return 0;
            int share = total / copies;
            return lead ? total - share * (copies - 1) : share;
        }

        /// <summary>
        /// Total Cash/Influence the party would bank if it ended successfully right now (before fee shortfalls), including the
        /// fixed community rewards (Staff's Influence, Troll Army's Cash) and the Affiliates' Checkout Codes. Random community
        /// rolls (Core Fanbase scoring twice) aren't included.
        /// </summary>
        public static void ProjectScore(PlayerState s, out int cash, out int influence)
        {
            cash = s.PartyCash + s.InvestCash * GameRules.StonksMultiplier;
            influence = s.PartyInfluence + s.InvestInfluence * GameRules.StonksMultiplier;
            ScoreHouse(s, out var cashes, out var influences);
            for (int i = 0; i < cashes.Length; i++)
            {
                cash += cashes[i];
                influence += influences[i];
            }
            var scores = CommunityScores(s);
            Communities.FixedRewards(scores, out int communityCash, out int communityInfluence);
            cash += communityCash + TrollArmyCash(s, scores) + LurkersCash(s, scores);
            // Double or Nothing armed: a scored stream earns its Cash again (capped).
            if (s.PartyDoubleDown) cash += Math.Min(StreamerCatalog.DoubleDownMaxBonus, Math.Max(0, cash));
            influence += communityInfluence + Communities.StaffInfluence(scores[(int)Community.Staff]) + CheckoutCodesNow(s);
        }

        /// <summary>
        /// What a Boomer's Early Retirement would bank right now (before fee shortfalls): the chat's viewers and the
        /// stream-wide community rewards (Staff, Troll Army, Lurkers). The pot, investments, Double or Nothing and Checkout
        /// Codes wait for the end of the stream, and random rolls (Core Fanbase scoring twice) aren't included.
        /// </summary>
        public static void ProjectEarlyRetirement(PlayerState s, out int cash, out int influence)
        {
            cash = 0;
            influence = 0;
            ScoreHouse(s, out var cashes, out var influences);
            for (int i = 0; i < cashes.Length; i++)
            {
                cash += cashes[i];
                influence += influences[i];
            }
            var scores = CommunityScores(s);
            cash += TrollArmyCash(s, scores) + LurkersCash(s, scores);
            influence += Communities.StaffInfluence(scores[(int)Community.Staff]);
        }

        /// <summary>The chance (%) that a Boomer's Early Retirement retires it right now, from the Boomers' points in chat.</summary>
        public static int EarlyRetirementChance(PlayerState s) => Communities.BoomerRetireChance(CommunityScore(s, Community.Boomer));

        /// <summary>Troll Army's Cash if the stream were scored now: +2 per point of Risk, cancelled Risk included.</summary>
        public static int TrollArmyCash(PlayerState s, int[] scores = null)
        {
            scores = scores ?? CommunityScores(s);
            return Communities.TrollArmyCash(scores[(int)Community.Trolls], TroubleTotal(s));
        }

        /// <summary>
        /// The Lurkers' Cash if the stream were scored now: +1 per empty space in chat per point (at most 2). The empty spaces
        /// are the ones the Isolationists count (both sides of a team's chat).
        /// </summary>
        public static int LurkersCash(PlayerState s, int[] scores = null)
        {
            scores = scores ?? CommunityScores(s);
            return Communities.LurkersCash(scores[(int)Community.Lurkers], s.EmptySpaces);
        }

        /// <summary>Influence the Affiliates' Checkout Codes would add to this stream if it were scored now (+1 each, while anyone can still grow).</summary>
        static int CheckoutCodesNow(PlayerState s)
        {
            int affiliates = s.CountInHouse(GuestType.Affiliate);
            if (affiliates == 0) return 0;
            int room = 0;
            foreach (var id in s.House)
            {
                var g = s.Find(id);
                if (g != null && !g.Temporary && !GuestCatalog.IsToken(g.Kind)) room += Math.Max(0, GameRules.PermanentInfluenceCap - PrintedInfluence(g));
            }
            return Math.Min(affiliates, room);
        }

        /// <summary>Cash penalty that ending now would incur because fees can't be paid.</summary>
        public static int ProjectPenalty(PlayerState s, int partyInfluence)
        {
            int available = partyInfluence + (GameRules.FeesDrawFromBank ? s.Influence : 0);
            return available < 0 ? -available * GameRules.InfluenceShortfallCashPenalty : 0;
        }

        /// <summary>
        /// The next Sub Cap upgrade's price, after Boomers' discount for this shop (never below 0). Teams: each member raises
        /// their own Sub Cap, so the price follows that member's upgrades.
        /// </summary>
        public static int ExpansionCost(PlayerState s, int member = 0) =>
            Math.Max(0, GameRules.ExpansionCost(s.ExpansionsOf(member)) - s.ExpansionDiscount);

        /// <summary>
        /// What recruiting this viewer costs right now: its price, less your Shop Control discount and plus rival Saboteurs'
        /// surcharge (both only on your next recruit).
        /// </summary>
        public static int RecruitCost(PlayerState s, GuestType type) =>
            Math.Max(0, GuestCatalog.Get(type).Cost - s.RecruitDiscount + s.IncomingSabotage);

        /// <summary>Whether "chat is full" bonuses apply: full, or within Full Stream's slack of full.</summary>
        public static bool IsChatFull(PlayerState s) =>
            s.HouseFull || s.EmptySpaces <= Communities.FullStreamSlack(CommunityScore(s, Community.FullStream));

        /// <summary>
        /// The community that can give this viewer's action extra uses this stream: Mods for evictions (removals and
        /// sending viewers back; the Gatekeepers had this rule until 2026-11-16), Marketers for recruits, Voyeurs for peeks.
        /// Null when the viewer isn't in that community.
        /// </summary>
        public static Community? ExtraUseCommunity(GuestInstance actor)
        {
            if (actor == null || !actor.Info.HasAbility) return null;
            var ability = actor.Info.Ability;
            Community community;
            if (ability == AbilityKind.SendBack || actor.Info.ActionCategory == ActionCategory.Remove) community = Community.Mods;
            else if (actor.Info.ActionCategory == ActionCategory.Recruit) community = Community.Marketers;
            else if (actor.Info.ActionCategory == ActionCategory.Scout) community = Community.Voyeurs;
            else return null;
            return GuestCatalog.InCommunity(actor.Kind, community) ? community : (Community?)null;
        }

        /// <summary>Extra uses of this community's actions still available this stream.</summary>
        public static int ExtraUsesLeft(PlayerState s, Community community)
        {
            int allowed = Communities.ExtraUses(community, CommunityScore(s, community));
            int used = community == Community.Mods ? s.PartyExtraEvictions
                : community == Community.Marketers ? s.PartyExtraRecruits
                : community == Community.Voyeurs ? s.PartyExtraPeeks : allowed;
            return Math.Max(0, allowed - used);
        }

        /// <summary>
        /// How many random viewers a send-back action would send right now: a Gatekeeper's (the PR Manager) 3, 6 or everyone
        /// else (<see cref="int.MaxValue"/>) by the Gatekeepers' points; otherwise the Junior PR Manager's 1, 2, 3... (one more
        /// per use of that card). A PR Manager taken out of the Gatekeepers in the Game Database sends its old 5.
        /// </summary>
        public static int SendBackCount(PlayerState s, GuestInstance actor)
        {
            if (actor == null) return 0;
            if (GuestCatalog.InCommunity(actor.Kind, Community.Gatekeepers))
                return Communities.GatekeeperSendBack(CommunityScore(s, Community.Gatekeepers));
            return actor.Kind == GuestType.PrManager ? GameRules.PrManagerSendBack : actor.ActionUses + 1;
        }

        // ================================================================== queries: Hype Men's spare seats

        /// <summary>
        /// Hype Men: spare seats past the Sub Cap right now (1 per point, at most 2), for viewers a Hype Man brings in. Since
        /// the 2026-11-18 UI updates they never count against the Sub Cap's limit (<see cref="MaxCapacity"/>, 40 or 50 a
        /// player in a team): a chat at or near it shows them in one extra row.
        /// </summary>
        public static int HypeSeats(PlayerState s)
        {
            if (s.Phase != GamePhase.Party) return 0;
            return Math.Min(Communities.HypeSeats(CommunityScore(s, Community.HypeMen)), GameRules.HypeRowSeats);
        }

        /// <summary>
        /// Whether this side of the chat (a solo season: the whole chat) can take one more spare seat: the spare seats have
        /// their own row past the Sub Cap's limit (<see cref="GameRules.HypeRowSeats"/>), so only that row can run out.
        /// </summary>
        public static bool HypeRoom(PlayerState s, int side) =>
            s.CapacityOf(side) + HypeSeated(s, s.IsTeam ? side : -1) < MaxCapacity(s) + GameRules.HypeRowSeats;

        /// <summary>Viewers in chat sitting in a Hype Men spare seat (on one member's side, or -1 for everyone).</summary>
        public static int HypeSeated(PlayerState s, int member = -1)
        {
            int count = 0;
            for (int i = 0; i < s.House.Count; i++)
            {
                var g = s.Find(s.House[i]);
                if (g != null && g.HypeSeat && (member < 0 || s.Owns(g, member))) count++;
            }
            return count;
        }

        /// <summary>Spare seats still free (only a viewer a Hype Man brings in can take one).</summary>
        public static int FreeHypeSeats(PlayerState s) => Math.Max(0, HypeSeats(s) - HypeSeated(s));

        /// <summary>Whether this viewer brings others in as a Hype Man (so they may take a spare seat).</summary>
        public static bool IsHypeMan(GuestInstance g) => g != null && GuestCatalog.InCommunity(g.Kind, Community.HypeMen);

        /// <summary>
        /// When a seat inside the Sub Cap frees up, a viewer in a spare seat moves into it (the earliest first), so spare seats
        /// only ever hold the overflow.
        /// </summary>
        static void SettleHypeSeats(PlayerState s)
        {
            for (int member = 0; member < s.Members; member++)
            {
                int seated = HypeSeated(s, member);
                for (int i = 0; i < s.House.Count && seated > 0 && s.OccupancyOf(member) - seated < s.CapacityOf(member); i++)
                {
                    var g = s.Find(s.House[i]);
                    if (g == null || !g.HypeSeat || !s.Owns(g, member)) continue;
                    g.HypeSeat = false;
                    seated--;
                }
            }
        }

        // ================================================================== queries: what can be done

        /// <summary>Why this member can't Add Viewer right now (null when they can). Teams: each member has their own queue and Sub Cap.</summary>
        public static string GetDoorBlocker(PlayerState s, int member = 0)
        {
            if (s.Phase != GamePhase.Party) return Err.Of("err.stream_over");
            if (s.StreamDoneFor(member)) return Err.Of("err.stream_ended_self");
            if (s.PeekedId >= 0) return Err.Of("err.decide_waiting");
            if (s.AttractionPending) return Err.Of("err.decide_attraction");
            if (s.SideFull(member)) return Err.Of("err.sub_cap");
            if (s.NextInPool(member) < 0) return Err.Of("err.queue_empty");
            return null;
        }

        public static string GetEndPartyBlocker(PlayerState s, int member = 0)
        {
            if (s.Phase != GamePhase.Party) return Err.Of("err.stream_over");
            if (member >= 0 && s.StreamDoneFor(member)) return Err.Of("err.stream_ended_self");
            if (s.PeekedId >= 0) return Err.Of("err.decide_waiting");
            if (s.AttractionPending) return Err.Of("err.decide_attraction");
            return null;
        }

        /// <summary>
        /// Null when the guest's action can be started (targets are validated separately). Teams: a member uses the actions
        /// of their own viewers (on anyone in chat, their partner's viewers included) until they end their side of the
        /// stream; pass -1 to skip that check.
        /// </summary>
        public static string GetAbilityBlocker(PlayerState s, int actorId, int member = -1)
        {
            if (s.Phase != GamePhase.Party) return Err.Of("err.stream_over");
            if (s.PeekedId >= 0) return Err.Of("err.decide_waiting");
            if (s.AttractionPending) return Err.Of("err.decide_attraction");
            if (!s.House.Contains(actorId)) return Err.Of("err.not_in_chat");

            var actor = s.Find(actorId);
            if (actor == null) return Err.Of("err.unknown_viewer");
            if (s.IsTeam && member >= 0)
            {
                if (!s.Owns(actor, member)) return Err.Of("err.partners_viewer");
                if (s.StreamDoneFor(member)) return Err.Of("err.stream_ended_self");
            }
            var info = actor.Info;
            if (!info.HasAbility) return Err.Of("err.no_action", Err.Guest(actor.Kind));
            if (s.UsedAbilities.Contains(actorId)) return Err.Of("err.action_used", Err.Action(actor.Kind));

            switch (info.Ability)
            {
                case AbilityKind.Fetch:
                    // From the actor's own queue (a team member's queue holds their Audience), onto their side of the chat.
                    if (s.NextInPool(actor.Owner) < 0) return Err.Of("err.queue_empty");
                    // A Hype Man (the Evangelist) may still bring someone into a free spare seat.
                    if (s.SideFull(actor.Owner) && !(IsHypeMan(actor) && FreeHypeSeats(s) > 0 && HypeRoom(s, actor.Owner))) return Err.Of("err.sub_cap");
                    break;
                case AbilityKind.Inspect:
                    if (s.Pool.Count < 2) return Err.Of("err.needs_two_queued");
                    break;
                case AbilityKind.SideEye:
                    if (s.Pool.Count < 2) return Err.Of("err.needs_two_queued");
                    break;
                case AbilityKind.Invest:
                    if (s.Cash < GameRules.StonksInvestCash && s.Influence < GameRules.StonksInvestInfluence) return Err.Of("err.nothing_to_invest");
                    break;
                case AbilityKind.Reroll:
                    if (!CanReroll(s) && s.Pool.Count == 0) return Err.Of("err.nothing_to_reroll");
                    break;
                case AbilityKind.Arson:
                    if (WouldRaidWithExtraTrouble(s)) return Err.Of("err.would_ban");
                    break;
                case AbilityKind.Peek:
                    if (s.NextInPool(actor.Owner) < 0) return Err.Of("err.queue_empty");
                    break;
                case AbilityKind.Calm:
                    if (TroublemakerCount(s) == 0) return Err.Of("err.no_risky");
                    break;
                case AbilityKind.Interview:
                    if (s.PartyInterviewUsed) return Err.Of("err.interview_used");
                    break;
                case AbilityKind.SendBack:
                    if (s.House.Count < 2) return Err.Of("err.no_target");
                    break;
            }

            GuestInfo.TargetCount(info.Ability, out int min, out _);
            if (info.Target != TargetMode.WaitingGuest && min > 0 && CountValidTargets(s, actorId) < min)
                return min == 1 ? Err.Of("err.no_target") : Err.Of("err.needs_targets", min);
            return null;
        }

        static bool WouldRaidWithExtraTrouble(PlayerState s) =>
            Math.Max(0, TroubleTotal(s) + 1 - CancelCount(s)) >= RiskLimit(s);

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

            // Fetch: from the actor's own queue (teams: each member's queue holds their Audience).
            if (kind == AbilityKind.Fetch) return s.Pool.Contains(candidateId) && s.Owns(s.Find(candidateId), actor.Owner);
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
                    return PrintedInfluence(target) < GameRules.PermanentInfluenceCap;
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

        /// <summary>
        /// Bot Detector: two guests sit next to each other when they're in consecutive chat spots. Teams: on the same side
        /// of the chat, consecutive among that side's viewers (as the split chat shows them).
        /// </summary>
        public static bool AreNeighbours(PlayerState s, int a, int b)
        {
            var seats = SideSeats(s, a);
            int ia = seats.IndexOf(a);
            int ib = seats.IndexOf(b);
            return ia >= 0 && ib >= 0 && Math.Abs(ia - ib) == 1;
        }

        /// <summary>The chat in joining order, or (teams) just the side this viewer sits on.</summary>
        static List<int> SideSeats(PlayerState s, int id)
        {
            if (!s.IsTeam) return s.House;
            var g = s.Find(id);
            var side = new List<int>(s.House.Count);
            if (g == null) return side;
            foreach (var other in s.House)
                if (s.Owns(s.Find(other), g.Owner)) side.Add(other);
            return side;
        }

        /// <summary>Whether a guest has a neighbour Bot Detector could remove together with them.</summary>
        static bool HasPairPartner(PlayerState s, int id)
        {
            var seats = SideSeats(s, id);
            int index = seats.IndexOf(id);
            for (int offset = -1; offset <= 1; offset += 2)
            {
                int j = index + offset;
                if (index < 0 || j < 0 || j >= seats.Count) continue;
                var neighbour = s.Find(seats[j]);
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
                    if (cmd.Option == 0 && s.Cash < GameRules.StonksInvestCash) return Err.Of("err.needs_cash_banked", GameRules.StonksInvestCash);
                    if (cmd.Option == 1 && s.Influence < GameRules.StonksInvestInfluence) return Err.Of("err.needs_influence", GameRules.StonksInvestInfluence);
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

        /// <summary>Why this member can't recruit the viewer (null when they can). Teams: copy limits are per member's Audience.</summary>
        public static string GetBuyBlocker(PlayerState s, GuestType type, int member = 0)
        {
            if (s.Phase != GamePhase.Planning) return Err.Of("err.recruit_between");
            if (!s.Shop.Contains(type)) return Err.Of("err.not_in_set");
            var info = GuestCatalog.Get(type);
            if (!info.Purchasable) return Err.Of("err.cant_buy", Err.Guest(type));
            if (s.CountOwned(type, member) >= info.MaxCopies) return Err.Of("err.max_copies", info.MaxCopies, Err.Guest(type));
            if (SharedLeft(s, type) <= 0) return Err.Of("err.sold_out", Err.Guest(type));
            int cost = RecruitCost(s, type);
            if (s.Cash < cost) return Err.Of("err.needs_cash_banked", cost);
            return null;
        }

        // ------------------------------------------------------------------ the shared shop

        /// <summary>
        /// Copies of a type this season took from the shop (everything beyond the starting Audience; a team counts both
        /// members' starting Audiences).
        /// </summary>
        public static int TakenFromShop(PlayerState s, GuestType type) => Math.Max(0, s.CountOwned(type) - StartingCopies(type) * Math.Max(1, s.Members));

        /// <summary>
        /// Copies still in the shop for every player (the shop's stock is shared by the whole match). Diamonds are
        /// unlimited (<see cref="int.MaxValue"/>).
        /// </summary>
        public static int SharedLeft(PlayerState s, GuestType type)
        {
            if (GuestCatalog.Get(type).IsDiamond) return int.MaxValue;
            int rivals = (int)type < s.RivalTaken.Length ? s.RivalTaken[(int)type] : 0;
            return Math.Max(0, GuestCatalog.Get(type).ShopStockFor(s.MatchPlayers) - TakenFromShop(s, type) - rivals);
        }

        /// <summary>
        /// How many more copies this player can buy: the smaller of their own copy limit and the shared stock.
        /// <see cref="int.MaxValue"/> means unlimited (Diamonds).
        /// </summary>
        public static int ShopLeft(PlayerState s, GuestType type, int member = 0)
        {
            var info = GuestCatalog.Get(type);
            if (!info.Purchasable) return 0;
            if (info.IsDiamond) return int.MaxValue;
            return Math.Max(0, Math.Min(info.MaxCopies - s.CountOwned(type, member), SharedLeft(s, type)));
        }

        /// <summary>What Remove a Viewer costs right now (Influence): 2 for the first, 1 more for each one since. Teams: per player.</summary>
        public static int RemovalCost(PlayerState s, int member = 0) => GameRules.RemovalCost(s.RemovalsOf(member));

        /// <summary>Why Remove a Viewer can't be bought right now (null when it can). Doesn't check which viewer. Teams: per member's Audience.</summary>
        public static string GetRemoveBlocker(PlayerState s, int member = 0)
        {
            if (s.Phase != GamePhase.Planning) return Err.Of("err.remove_between");
            if (s.PermanentCountOf(member) <= GameRules.MinAudience) return Err.Of("err.min_audience", GameRules.MinAudience);
            int cost = RemovalCost(s, member);
            if (s.Influence < cost) return Err.Of("err.needs_influence", cost);
            return null;
        }

        /// <summary>Remove a Viewer: pays the Influence and takes one card out of the Audience for good (it goes back to the shop's stock).</summary>
        static CommandResult Dismiss(PlayerState s, int guestId, int member, List<GameEvent> ev)
        {
            var blocker = GetRemoveBlocker(s, member);
            if (blocker != null) return CommandResult.Fail(blocker);
            var g = s.Find(guestId);
            if (g == null || g.Temporary) return CommandResult.Fail(Err.Of("err.unknown_viewer"));
            if (!s.Owns(g, member)) return CommandResult.Fail(Err.Of("err.partners_viewer"));

            int cost = RemovalCost(s, member);
            s.Influence -= cost;
            if (s.IsTeam && member == 1) s.PartnerRemovals++;
            else s.Removals++;
            s.Rolodex.Remove(g);
            if (s.BannedNextParty == guestId) s.BannedNextParty = -1;
            Emit(ev, GameEventType.GuestDismissed, g, cost);
            return CommandResult.Success;
        }

        /// <summary>Why this member can't raise their Sub Cap (null when they can).</summary>
        public static string GetExpandBlocker(PlayerState s, int member = 0)
        {
            if (s.Phase != GamePhase.Planning) return Err.Of("err.upgrade_between");
            if (s.CapacityOf(member) >= MaxCapacity(s)) return Err.Of("err.sub_cap_maxed");
            int cost = ExpansionCost(s, member);
            if (s.Influence < cost) return Err.Of("err.needs_influence", cost);
            return null;
        }

        // ================================================================== party phase: lifecycle

        static void BeginParty(PlayerState s, List<GameEvent> ev)
        {
            ClearParty(s);
            s.DoomOut = 0;
            s.BotsOut = 0;
            s.SabotageOut = 0;
            s.BannedThisParty = s.BannedNextParty;
            s.BannedNextParty = -1;
            s.RaidSuspects.Clear();

            // A rival's Bots join this stream's Audience. They're temporary, so they leave when the stream ends.
            int bots = Math.Min(Communities.BotsMaxIncoming, Math.Max(0, s.IncomingBots));
            s.IncomingBots = 0;
            for (int i = 0; i < bots; i++) AddGuest(s, GuestType.Bot, true);

            for (int i = 0; i < s.Rolodex.Count; i++)
            {
                int id = s.Rolodex[i].Id;
                if (id != s.BannedThisParty) s.Pool.Add(id);
            }
            Shuffle(s, s.Pool);

            s.Phase = GamePhase.Party;
            Emit(ev, GameEventType.NightStarted, null, s.Night);
            if (bots > 0) Emit(ev, GameEventType.BotsArrived, null, bots);
            ApplyStreamerEffects(s, ev);

            // Mods from the last stream: this one starts with 1 Deplatform Risk cancelled.
            if (s.ModsShieldNext)
            {
                s.ModsShieldNext = false;
                s.PartyShield += 1;
                Emit(ev, GameEventType.ModsShield, null, 2, 1);
            }
        }

        static void ClearParty(PlayerState s)
        {
            s.House.Clear();
            s.Pool.Clear();
            s.Excluded.Clear();
            s.UsedAbilities.Clear();
            s.PeekedId = -1;
            s.PartyCash = 0;
            s.PartyInfluence = 0;
            s.InvestCash = 0;
            s.InvestInfluence = 0;
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
            // Three-Hour Episode's extra seat was for that stream only (in a team, on the Bro Jogan player's side).
            if (s.PartySeats > 0) AddSeats(s, EpisodeMember(s), -s.PartySeats);
            s.PartySeats = 0;
            s.StreamDone = 0;
            s.PartyBark = 0;
            s.PartySoftLanding = false;
            s.PartySubathon = false;
            s.PartyDoubleDown = false;
            s.PartyInterviewUsed = false;
            s.PartyDoom = 0;
            s.PartyExtraEvictions = 0;
            s.PartyExtraRecruits = 0;
            s.PartyExtraPeeks = 0;

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

        /// <summary>Add Viewer: the next viewer in this member's queue joins their side of the chat.</summary>
        static CommandResult OpenDoor(PlayerState s, int member, List<GameEvent> ev)
        {
            var blocker = GetDoorBlocker(s, member);
            if (blocker != null) return CommandResult.Fail(blocker);
            Arrive(s, s.NextInPool(member), -1, -1, true, ev);
            return CommandResult.Success;
        }

        static CommandResult AdmitPeeked(PlayerState s, List<GameEvent> ev)
        {
            if (s.Phase != GamePhase.Party || s.PeekedId < 0) return CommandResult.Fail(Err.Of("err.nobody_waiting"));
            var peeked = s.Find(s.PeekedId);
            if (s.SideFull(peeked != null ? peeked.Owner : 0)) return CommandResult.Fail(Err.Of("err.cap_deny"));
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
        /// Moves a guest into the party and resolves entrance effects, reactions, overcrowding (Server Down), Deplatform
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

            // Server Down: this viewer's side of the chat (the whole chat for a solo season) is over its Sub Cap.
            int side = g.Owner;
            if (s.OccupancyOf(side) > s.CapacityOf(side) + HypeSeated(s, s.IsTeam ? side : -1))
            {
                // Hype Men: a viewer a Hype Man brought in takes a free spare seat instead of crashing the server.
                if (IsHypeMan(s.Find(sourceId)) && FreeHypeSeats(s) > 0 && HypeRoom(s, side))
                {
                    g.HypeSeat = true;
                    Emit(ev, GameEventType.HypeSeatTaken, g, sourceId, HypeSeats(s) - HypeSeated(s));
                }
                else if (s.PartySubathon)
                {
                    // Subathon (Kai Sonnet): the stream never ends. The viewer who would crash the server goes back to the
                    // queue instead (they can join again), once per armed stream; whoever they'd bring stays in the queue.
                    s.PartySubathon = false;
                    RemoveFromParty(s, id, LeaveReason.SentOutside, -1, false, ev, false);
                    Emit(ev, GameEventType.SubathonSaved, g);
                    return true;
                }
                else
                {
                    // The retired Techspertise may bring the server back, in which case the stream still scores.
                    if (!TryRestoreServer(s, ev)) ShutDownFireMarshal(s, ev);
                    return false;
                }
            }
            if (CheckRaid(s, ev)) return false;

            // Viewers who bring others take them from their own queue (teams: their member's queue, onto that side).
            for (int i = 0; i < g.Info.Brings; i++)
            {
                int next = s.NextInPool(g.Owner);
                if (next < 0) break;
                if (!Arrive(s, next, -1, id, true, ev)) return false;
            }

            // Generous Donator: brings in the next viewer and scores them once right away.
            if (g.Kind == GuestType.GenerousDonator && s.NextInPool(g.Owner) >= 0)
            {
                int greeted = s.NextInPool(g.Owner);
                if (!Arrive(s, greeted, -1, id, true, ev)) return false;
                var guest = s.Find(greeted);
                if (guest != null && s.House.Contains(greeted)) ScoreNow(s, guest, id, ev);
            }
            return s.Phase == GamePhase.Party;
        }

        /// <summary>
        /// Scores a guest mid-stream (Crypto Guy, Generous Donator) and banks it immediately, so it's kept even if the
        /// stream ends badly. Fees the bank can't cover cost Cash, as at the end of a stream.
        /// </summary>
        static void ScoreNow(PlayerState s, GuestInstance g, int sourceId, List<GameEvent> ev)
        {
            ScoreGuest(s, g, out int cash, out int influence);
            // The (retired) Finance Bros bonus applies to every scoring, this one included.
            var scores = CommunityScores(s);
            influence = RollInfluence(s, g, influence, Communities.FinanceDoubleChance(scores[(int)Community.FinanceBros]), ev);
            Emit(ev, GameEventType.ScoredNow, g, cash, influence, sourceId);
            Bank(s, cash, influence, ev);
        }

        /// <summary>Adds Cash and Influence straight to the wallet. A Influence debt the bank can't pay becomes a Cash penalty.</summary>
        static void Bank(PlayerState s, int cash, int influence, List<GameEvent> ev)
        {
            s.Cash = Math.Max(0, s.Cash + cash);
            s.TotalCashEarned += Math.Max(0, cash);
            s.Influence += influence;
            s.TotalInfluenceEarned += Math.Max(0, influence);
            if (s.Influence >= 0) return;
            int unpaid = -s.Influence;
            int penalty = unpaid * GameRules.InfluenceShortfallCashPenalty;
            s.Influence = 0;
            s.Cash = Math.Max(0, s.Cash - penalty);
            Emit(ev, GameEventType.InfluenceShortfall, null, unpaid, penalty);
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
                        if (best == null || PrintedCash(other) > PrintedCash(best)) best = other;
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
                    // Secure The Bag: +1 Influence for good on every entrance, up to the cap, even if the stream fails later.
                    if (PrintedInfluence(g) < GameRules.PermanentInfluenceCap)
                    {
                        g.BonusInfluence++;
                        Emit(ev, GameEventType.InfluenceBoosted, g, 1, g.BonusInfluence);
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
            npc.CopyCash = PrintedCash(previous);
            npc.CopyInfluence = PrintedInfluence(previous);
            Emit(ev, GameEventType.StatsCopied, npc, previous.Id, npc.CopyCash, npc.CopyInfluence);
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

        /// <summary>Bongo Cat's beat buffs Bongo Cat and every viewer sharing a community with it (it used to be the Animal family).</summary>
        static void BongoBeat(PlayerState s, GuestInstance bongo, List<GameEvent> ev)
        {
            var band = GuestCatalog.CommunitiesOf(bongo.Kind);
            for (int i = 0; i < s.House.Count; i++)
            {
                var g = s.Find(s.House[i]);
                if (g == null) continue;
                if (g != bongo && ((GuestCatalog.CommunitiesOf(g.Kind) & band) == 0 || g.Immune)) continue;
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
            // A freed seat inside the Sub Cap goes to a viewer in a Hype Men spare seat.
            g.HypeSeat = false;
            SettleHypeSeats(s);

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
                    // Everyone Leaves Me: +1 Cash or +1 Influence, at random.
                    if (cat == null || cat.Kind != GuestType.DistraughtCat) continue;
                    if (DeterministicRandom.Range(ref s.RngState, 2) == 0) Gain(s, cat, 1, 0, ev);
                    else Gain(s, cat, 0, 1, ev);
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
            g.BonusCash++;
            Emit(ev, GameEventType.ShieldBlocked, g, g.BonusCash);
            return true;
        }

        static void AddTrouble(PlayerState s, GuestInstance g, int amount, List<GameEvent> ev)
        {
            g.PartyTrouble += amount;
            Emit(ev, GameEventType.TroubleAdded, g, amount);
            NotifyTroubleAdded(s, g, ev);
        }

        /// <summary>Clears a guest's Deplatform Risk for the rest of the party. Returns how much was removed.</summary>
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

        static void Gain(PlayerState s, GuestInstance g, int cash, int influence, List<GameEvent> ev)
        {
            if (cash == 0 && influence == 0) return;
            g.PartyCash += cash;
            g.PartyInfluence += influence;
            Emit(ev, GameEventType.GuestGained, g, cash, influence);
        }

        static void PotGain(PlayerState s, GuestInstance source, int cash, int influence, List<GameEvent> ev)
        {
            if (cash == 0 && influence == 0) return;
            s.PartyCash += cash;
            s.PartyInfluence += influence;
            Emit(ev, GameEventType.PartyPotGained, source, cash, influence);
        }

        // ================================================================== shutdowns

        /// <summary>Deplatformed: too much net Deplatform Risk ends the stream with no payout (internally a "raid").</summary>
        static bool CheckRaid(PlayerState s, List<GameEvent> ev)
        {
            if (s.Phase != GamePhase.Party) return false;
            if (NetTrouble(s) < RiskLimit(s)) return false;

            // This Is Fine Dog: the first time it would happen, the deplatforming is called off.
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
                if (NetTrouble(s) < RiskLimit(s)) return false;
            }

            int trouble = NetTrouble(s);
            SoftLanding(s, ev);
            DoubleDownLoss(s, ev);
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

        /// <summary>
        /// The retired Techspertise: a chance (by points, max 3) that the server comes back and the stream still scores. (Hype
        /// Men had a chance too until 2026-11-16; they give spare seats now.)
        /// </summary>
        static bool TryRestoreServer(PlayerState s, List<GameEvent> ev)
        {
            var scores = CommunityScores(s);
            int chance = Communities.TechspertiseChance(scores[(int)Community.Techspertise]);
            if (!Roll(s, chance)) return false;
            Emit(ev, GameEventType.ServerRestored, null, chance, s.Occupancy, s.Capacity);
            ScoreStream(s, ev);
            return true;
        }

        /// <summary>Server Down: chat went over the Sub Cap, so the stream ends with no payout (internally the "fire marshal").</summary>
        static void ShutDownFireMarshal(PlayerState s, List<GameEvent> ev)
        {
            int occupancy = s.Occupancy;
            s.FireMarshals++;
            DoubleDownLoss(s, ev);
            s.Streak = 0;
            RecordOutcome(s, PartyOutcome.FireMarshal, 0, 0, DiamondsPresent(s), 0);
            Emit(ev, GameEventType.FireMarshal, null, occupancy, s.Capacity);
            ClearParty(s);
            AfterParty(s, ev);
        }

        // ================================================================== actions

        static CommandResult UseAbility(PlayerState s, GameCommand cmd, int member, List<GameEvent> ev)
        {
            int actorId = cmd.Actor;
            var blocker = GetAbilityBlocker(s, actorId, s.IsTeam ? member : -1);
            if (blocker != null) return CommandResult.Fail(blocker);
            var error = ValidateCommand(s, cmd);
            if (error != null) return CommandResult.Fail(error);

            var actor = s.Find(actorId);
            var kind = actor.Info.Ability;
            int a = cmd.TargetA, b = cmd.TargetB, c = cmd.TargetC;
            // Gatekeepers / Marketers / Voyeurs: decided before the action, while the actor still counts for its community.
            var extraCommunity = ExtraUseCommunity(actor);
            bool extraUse = extraCommunity.HasValue && ExtraUsesLeft(s, extraCommunity.Value) > 0;
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
                        s.Cash -= GameRules.StonksInvestCash;
                        s.InvestCash += GameRules.StonksInvestCash;
                    }
                    else
                    {
                        s.Influence -= GameRules.StonksInvestInfluence;
                        s.InvestInfluence += GameRules.StonksInvestInfluence;
                    }
                    Emit(ev, GameEventType.Invested, actor, s.InvestCash, s.InvestInfluence);
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
                    int theirs = PrintedCash(target);
                    int chads = PrintedCash(actor);
                    if (theirs < chads)
                    {
                        target.BonusCash--;
                        actor.BonusCash++;
                        Emit(ev, GameEventType.PermanentChange, target, -1, target.BonusCash);
                        Emit(ev, GameEventType.PermanentChange, actor, 1, actor.BonusCash);
                    }
                    else if (theirs > chads)
                    {
                        actor.BonusCash--;
                        Emit(ev, GameEventType.PermanentChange, actor, -1, actor.BonusCash);
                    }
                    else
                    {
                        Emit(ev, GameEventType.PermanentChange, actor, 0, actor.BonusCash);
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
                        int peeked = s.NextInPool(actor.Owner);
                        s.Pool.Remove(peeked);
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
                            actor.BonusCash++;
                            Emit(ev, GameEventType.PermanentChange, actor, 1, actor.BonusCash);
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
                    int firstPrinted = PrintedCash(first);
                    int secondPrinted = PrintedCash(second);
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
                    int peeked = s.NextInPool(actor.Owner);
                    s.Pool.Remove(peeked);
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
                        g.CashMuted = true;
                        muted++;
                    }
                    // The stream's Cash pot drops to 0 as well.
                    s.PartyCash = 0;
                    int billionaires = s.CountInHouse(GuestType.BillionaireBenefactor);
                    Emit(ev, GameEventType.InterviewGiven, actor, billionaires, muted);
                    Bank(s, 0, billionaires, ev);
                    break;
                }

                case AbilityKind.EarlyRetirement:
                    EarlyRetirement(s, actor, ev);
                    break;

                case AbilityKind.Style:
                {
                    var target = s.Find(a);
                    target.BonusInfluence++;
                    Emit(ev, GameEventType.InfluenceBoosted, target, 1, target.BonusInfluence);
                    break;
                }

                case AbilityKind.SendBack:
                {
                    // A Gatekeeper (the PR Manager): 3, 6 or everyone else, by the Gatekeepers' level (counted while the actor is
                    // still in chat). Junior PR Manager: 1, then one more every time this card uses it. Never the actor itself.
                    int count = SendBackCount(s, actor);
                    var others = new List<int>(s.House);
                    others.Remove(actorId);
                    int sent = 0;
                    for (int k = 0; k < count && others.Count > 0 && s.Phase == GamePhase.Party; k++)
                    {
                        int index = DeterministicRandom.Range(ref s.RngState, others.Count);
                        int id = others[index];
                        others.RemoveAt(index);
                        if (RemoveFromParty(s, id, LeaveReason.SentOutside, actorId, true, ev)) sent++;
                    }
                    Emit(ev, GameEventType.SentBack, actor, sent, Math.Min(count, short.MaxValue));
                    CheckRaid(s, ev);
                    break;
                }
            }

            actor.ActionUses++;

            if (extraUse && s.Phase == GamePhase.Party && s.House.Contains(actorId) && s.UsedAbilities.Remove(actorId))
            {
                var community = extraCommunity.Value;
                if (community == Community.Mods) s.PartyExtraEvictions++;
                else if (community == Community.Marketers) s.PartyExtraRecruits++;
                else s.PartyExtraPeeks++;
                Emit(ev, GameEventType.ActionRefreshed, actor, (int)community, ExtraUsesLeft(s, community));
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

        /// <summary>
        /// End the Stream. Teams: each member ends their own side; the stream is scored (as one) once every member has.
        /// Member -1 ends every side at once (the host's timeouts).
        /// </summary>
        static CommandResult EndParty(PlayerState s, int member, List<GameEvent> ev)
        {
            var blocker = GetEndPartyBlocker(s, member);
            if (blocker != null) return CommandResult.Fail(blocker);
            if (s.IsTeam)
            {
                int all = (1 << s.Members) - 1;
                s.StreamDone = member < 0 ? all : s.StreamDone | (1 << member);
                if (s.StreamDone != all)
                {
                    Emit(ev, GameEventType.MemberStreamEnded, null, member);
                    return CommandResult.Success;
                }
            }
            ScoreStream(s, ev);
            return CommandResult.Success;
        }

        /// <summary>
        /// Banks the stream: the chat scores (community bonuses rolled per viewer, then the stream-wide community rewards),
        /// maybe a second time (Memetics Division), then investments, the streak bonus, fees and the win check. Used when the
        /// stream is ended and when Techspertise brings a downed server back.
        /// </summary>
        static void ScoreStream(PlayerState s, List<GameEvent> ev)
        {
            // Affiliates' Checkout Codes first, so the +1 Influence already counts in this stream's score.
            ApplyCheckoutCodes(s, ev);
            var scores = CommunityScores(s);
            ScoreChat(s, scores, false, out int cash, out int influence, ev);

            // Memetics Division: the whole chat scores again, every roll made afresh.
            int repeat = Communities.MemeticsRepeatChance(scores[(int)Community.MemeticsDivision]);
            if (Roll(s, repeat))
            {
                Emit(ev, GameEventType.MemeticsRepeat, null, repeat);
                ScoreChat(s, scores, true, out int againCash, out int againInfluence, ev);
                cash += againCash;
                influence += againInfluence;
            }

            if (s.InvestCash > 0 || s.InvestInfluence > 0)
                Emit(ev, GameEventType.InvestmentPaid, null, s.InvestCash * GameRules.StonksMultiplier, s.InvestInfluence * GameRules.StonksMultiplier);
            cash += s.PartyCash + s.InvestCash * GameRules.StonksMultiplier;
            influence += s.PartyInfluence + s.InvestInfluence * GameRules.StonksMultiplier;
            // Double or Nothing (Train Wrex): the stream's Cash again, up to a cap.
            cash += DoubleDownBonus(s, cash, ev);

            int diamonds = DiamondsPresent(s);
            int doom = Math.Min(Communities.DoomMaxPoints, scores[(int)Community.Doomsdayers]);
            int bots = RollBots(s, scores, ev);
            int penalty = 0;
            CommunityAftermath(s, scores, ev);
            // Boomers no longer retire by chance after a scored stream (2026-11-18): their Early Retirement does it.

            // Great streams in a row: Manim Maketh Man's Beautiful Sequence cashes it in from the shop.
            s.Streak++;

            int unpaid;
            if (GameRules.FeesDrawFromBank)
            {
                s.Influence += influence;
                unpaid = s.Influence < 0 ? -s.Influence : 0;
                if (unpaid > 0) s.Influence = 0;
            }
            else
            {
                unpaid = influence < 0 ? -influence : 0;
                if (influence > 0) s.Influence += influence;
            }

            if (unpaid > 0)
            {
                penalty = unpaid * GameRules.InfluenceShortfallCashPenalty;
                Emit(ev, GameEventType.InfluenceShortfall, null, unpaid, penalty);
            }

            s.Cash = Math.Max(0, s.Cash + cash - penalty);
            s.TotalCashEarned += Math.Max(0, cash);
            s.TotalInfluenceEarned += Math.Max(0, influence);
            s.SuccessfulParties++;
            s.BestDiamonds = Math.Max(s.BestDiamonds, diamonds);
            RecordOutcome(s, PartyOutcome.Success, cash, influence, diamonds, penalty);
            Emit(ev, GameEventType.PartyEnded, null, cash, influence, diamonds);

            int temporary = 0;
            foreach (var g in s.Rolodex) if (g.Temporary && !GuestCatalog.IsToken(g.Type)) temporary++;
            ClearParty(s);
            if (temporary > 0) Emit(ev, GameEventType.TemporaryCopiesLeft, null, temporary);
            // The host hands these to the rivals' next streams (multiplayer only).
            s.DoomOut = doom;
            s.BotsOut = bots;

            if (s.IsEndless)
            {
                // Endless: no win. Every Trophy viewer in a scored stream banks a Trophy, spent on streams and shop viewers.
                if (diamonds > 0)
                {
                    s.Trophies += diamonds;
                    Emit(ev, GameEventType.TrophiesBanked, null, diamonds, s.Trophies);
                }
            }
            else if (diamonds >= TrophiesToWin(s))
            {
                s.Phase = GamePhase.Victory;
                s.WinNight = s.Night;
                Emit(ev, GameEventType.Victory, null, s.Night);
                return;
            }

            AfterParty(s, ev);
        }

        /// <summary>
        /// What a scored stream's communities leave behind: Gen-Z viewers grow for good, Boomers (and the retired Shop Control)
        /// discount the coming shop, and the retired Saboteurs aim a surcharge at the rivals (the host delivers it).
        /// </summary>
        static void CommunityAftermath(PlayerState s, int[] scores, List<GameEvent> ev)
        {
            int growth = Communities.GenZGrowth(scores[(int)Community.GenZ]);
            if (growth > 0)
            {
                foreach (var id in s.House)
                {
                    var g = s.Find(id);
                    if (g == null || g.Temporary || !GuestCatalog.InCommunity(g.Type, Community.GenZ)) continue;
                    int cash = 0, influence = 0;
                    for (int k = 0; k < growth; k++)
                    {
                        bool canCash = g.BaseInfo.Cash + g.BonusCash < GameRules.PermanentInfluenceCap;
                        bool canInfluence = g.BaseInfo.Influence + g.BonusInfluence < GameRules.PermanentInfluenceCap;
                        if (!canCash && !canInfluence) break;
                        bool pickCash = DeterministicRandom.Range(ref s.RngState, 2) == 0;
                        if (pickCash ? !canCash : !canInfluence) pickCash = !pickCash;
                        if (pickCash)
                        {
                            g.BonusCash++;
                            cash++;
                        }
                        else
                        {
                            g.BonusInfluence++;
                            influence++;
                        }
                    }
                    if (cash + influence > 0) Emit(ev, GameEventType.GenZGrowth, g, cash, influence);
                }
            }

            // The Mods' old rule (a chance to cancel 1 Risk next stream) retired on 2026-11-16: Mods give extra removal uses now.

            // The Boomers' shop discount (Sub Cap upgrades and the active) retired on 2026-11-18: their points lower the
            // chance that Early Retirement retires them instead.
            s.ExpansionDiscount = 0;
            s.RecruitDiscount = Communities.ShopControlDiscount(scores[(int)Community.ShopControl]);
            if (s.RecruitDiscount > 0) Emit(ev, GameEventType.ShopPerk, null, (int)Community.ShopControl, s.RecruitDiscount);
            s.SabotageOut = Communities.SabotageAmount(scores[(int)Community.Saboteurs]);
            if (s.SabotageOut > 0) Emit(ev, GameEventType.ShopPerk, null, (int)Community.Saboteurs, s.SabotageOut);
        }

        /// <summary>
        /// Boomers' Early Retirement (2026-11-18): the whole chat scores right now, as if the stream ended here: every viewer
        /// in joining order with the community rolls (Core Fanbase encores...), then the stream-wide community rewards
        /// (Staff, Troll Army, Lurkers). It's banked at once (fees included, as at the end of a stream), and the stream goes
        /// on, to be scored again when it ends. Trophies, the stream's pot, investments and end-of-stream effects (Gen-Z
        /// growth, streaks, a win) wait for the real end. Then the Boomer retires (leaves your Audience for good) with the
        /// Boomers' chance from before it acted (<see cref="Communities.BoomerRetireChance"/>: 100% / 50% / 25%), and a
        /// retiring Boomer leaves a Millennial an inheritance.
        /// </summary>
        static void EarlyRetirement(PlayerState s, GuestInstance boomer, List<GameEvent> ev)
        {
            var scores = CommunityScores(s);
            int retireChance = Communities.BoomerRetireChance(scores[(int)Community.Boomer]);
            int millennialPoints = scores[(int)Community.Millennial];

            ScoreChat(s, scores, false, out int cash, out int influence, ev);
            Bank(s, cash, influence, ev);
            Emit(ev, GameEventType.EarlyRetirement, boomer, cash, influence);

            if (!Roll(s, retireChance))
            {
                Emit(ev, GameEventType.BoomerStayed, boomer, retireChance);
                return;
            }
            RetireBoomer(s, boomer, millennialPoints, ev);
            // A Boomer that cancelled Risk takes its cancel with it.
            CheckRaid(s, ev);
        }

        /// <summary>
        /// A Boomer leaves chat and your Audience for good (a temporary copy just leaves). Then a random Millennial you own
        /// inherits +1 Cash for good, +1 more per Millennials point (<see cref="Communities.InheritanceAmount"/>, up to
        /// <see cref="GameRules.PermanentInfluenceCap"/> printed Cash). Without a Millennial nothing is inherited.
        /// </summary>
        static void RetireBoomer(PlayerState s, GuestInstance boomer, int millennialPoints, List<GameEvent> ev)
        {
            int id = boomer.Id;
            if (s.House.Contains(id)) RemoveFromParty(s, id, LeaveReason.Gone, -1, false, ev);
            s.UsedAbilities.Remove(id);
            s.Pool.Remove(id);
            s.Rolodex.Remove(boomer);
            if (s.BannedNextParty == id) s.BannedNextParty = -1;
            Emit(ev, GameEventType.BoomerRetired, boomer);

            var heirs = new List<GuestInstance>();
            foreach (var g in s.Rolodex)
                if (!g.Temporary && GuestCatalog.InCommunity(g.Type, Community.Millennial) && g.BaseInfo.Cash + g.BonusCash < GameRules.PermanentInfluenceCap)
                    heirs.Add(g);
            if (heirs.Count == 0) return;
            var heir = heirs[DeterministicRandom.Range(ref s.RngState, heirs.Count)];
            int amount = Math.Min(Communities.InheritanceAmount(millennialPoints), GameRules.PermanentInfluenceCap - (heir.BaseInfo.Cash + heir.BonusCash));
            heir.BonusCash += amount;
            Emit(ev, GameEventType.Inheritance, heir, amount, heir.BonusCash);
        }

        /// <summary>GuestScored C for the second scoring of a viewer who scores twice (Core Fanbase), so it plays again.</summary>
        public const int EncoreScoringTag = 3;
        /// <summary>The same inside the (retired) Memetics Division's repeat scoring.</summary>
        public const int EncoreRepeatScoringTag = 4;

        /// <summary>
        /// One scoring of the chat: every viewer in joining order (community bonuses rolled per viewer), then the stream-wide
        /// community rewards. Each viewer sends a GuestScored event (C = 1, or 2 in the Memetics Division's repeat); a viewer
        /// who scores twice sends a second one (C = <see cref="EncoreScoringTag"/> / <see cref="EncoreRepeatScoringTag"/>),
        /// so the scoring sequence plays both.
        /// </summary>
        static void ScoreChat(PlayerState s, int[] scores, bool repeat, out int cash, out int influence, List<GameEvent> ev)
        {
            ScoreHouse(s, out var cashes, out var influences);
            var encoreCashes = new int[cashes.Length];
            var encoreInfluences = new int[cashes.Length];
            var encore = new bool[cashes.Length];
            RollCommunityBonuses(s, scores, cashes, influences, encoreCashes, encoreInfluences, encore, repeat ? 2 : 0, ev);
            cash = 0;
            influence = 0;
            for (int i = 0; i < s.House.Count && i < cashes.Length; i++)
            {
                var g = s.Find(s.House[i]);
                Emit(ev, GameEventType.GuestScored, g, cashes[i], influences[i], repeat ? 2 : 1);
                cash += cashes[i];
                influence += influences[i];
                if (!encore[i]) continue;
                Emit(ev, GameEventType.GuestScored, g, encoreCashes[i], encoreInfluences[i], repeat ? EncoreRepeatScoringTag : EncoreScoringTag);
                cash += encoreCashes[i];
                influence += encoreInfluences[i];
            }
            ScoreCommunityRewards(s, scores, ref cash, ref influence, ev);
        }

        /// <summary>
        /// End-of-stream community rolls, per viewer in joining order: Core Fanbase may make one of its viewers score twice
        /// (the second scoring goes in <paramref name="encoreCashes"/> / <paramref name="encoreInfluences"/>, flagged in
        /// <paramref name="encore"/>). The retired communities' rolls are kept: rival Doomsdayers and Armchair Activists may
        /// stop a viewer scoring, Content may make a viewer outside the Core Fanbase score twice, Cash may add a Gifted Sub
        /// and Finance Bros may double a Influence reward. <paramref name="tag"/> goes in the events' C (2 = the Memetics repeat).
        /// </summary>
        static void RollCommunityBonuses(PlayerState s, int[] scores, int[] cashes, int[] influences, int[] encoreCashes, int[] encoreInfluences,
            bool[] encore, int tag, List<GameEvent> ev)
        {
            int doom = Communities.DoomChance(s.PartyDoom);
            int silence = Communities.ArmchairSilenceChance(scores[(int)Community.ArmchairActivists]);
            int contentEncore = Communities.ContentEncoreChance(scores[(int)Community.Content]);
            int fanEncore = Communities.CoreFanbaseEncoreChance(scores[(int)Community.CoreFanbase]);
            int finance = Communities.FinanceDoubleChance(scores[(int)Community.FinanceBros]);
            int clout = Communities.CloutChance(scores[(int)Community.Clout]);
            if (doom + silence + contentEncore + fanEncore + finance + clout == 0) return;

            for (int i = 0; i < s.House.Count && i < cashes.Length; i++)
            {
                var g = s.Find(s.House[i]);
                if (g == null) continue;

                if (Roll(s, doom))
                {
                    // None of this viewer's benefits, or downsides.
                    cashes[i] = 0;
                    influences[i] = 0;
                    Emit(ev, GameEventType.CommunityBonus, g, (int)Community.Doomsdayers, 0, tag);
                    continue;
                }

                // Armchair Activists: all talk. Each one rolls on its own and may score nothing.
                if (GuestCatalog.InCommunity(g.Kind, Community.ArmchairActivists) && Roll(s, silence))
                {
                    cashes[i] = 0;
                    influences[i] = 0;
                    Emit(ev, GameEventType.CommunityBonus, g, (int)Community.ArmchairActivists, 0, tag);
                    continue;
                }

                bool fan = GuestCatalog.InCommunity(g.Kind, Community.CoreFanbase);
                // Core Fanbase: the regulars may score twice (the retired Content let everyone else).
                bool twice = fan ? Roll(s, fanEncore) : Roll(s, contentEncore);
                if (twice) Emit(ev, GameEventType.CommunityBonus, g, (int)(fan ? Community.CoreFanbase : Community.Content), 0, tag);

                // Each scoring rolls on its own: Cash's extra Gifted Sub (outside the Core Fanbase; a Bot earns nothing)
                // and Finance Bros' double Influence.
                bool cloutable = !fan && !GuestCatalog.IsToken(g.Kind);
                int baseCash = cashes[i], baseInfluence = influences[i];
                ScoreOnce(s, g, baseCash, baseInfluence, cloutable, clout, finance, tag, ev, out cashes[i], out influences[i]);
                if (!twice) continue;
                encore[i] = true;
                ScoreOnce(s, g, baseCash, baseInfluence, cloutable, clout, finance, tag, ev, out encoreCashes[i], out encoreInfluences[i]);
            }
        }

        static void ScoreOnce(PlayerState s, GuestInstance g, int baseCash, int baseInfluence, bool cloutable, int cloutChance, int financeChance,
            int tag, List<GameEvent> ev, out int cash, out int influence)
        {
            int extra = cloutable && Roll(s, cloutChance) ? Communities.CloutCash : 0;
            if (extra > 0) Emit(ev, GameEventType.CommunityBonus, g, (int)Community.Clout, extra, tag);
            cash = baseCash + extra;
            influence = RollInfluence(s, g, baseInfluence, financeChance, ev, tag);
        }

        /// <summary>
        /// Bots: each Bots viewer in chat rolls (10% per point, max 40%) to send a Bot into the next player's next stream.
        /// Returns how many were sent; the host delivers them (nothing happens in single player).
        /// </summary>
        static int RollBots(PlayerState s, int[] scores, List<GameEvent> ev)
        {
            int chance = Communities.BotChance(scores[(int)Community.Bots]);
            if (chance <= 0) return 0;
            int sent = 0;
            for (int i = 0; i < s.House.Count; i++)
            {
                var g = s.Find(s.House[i]);
                if (g != null && GuestCatalog.InCommunity(g.Kind, Community.Bots) && Roll(s, chance)) sent++;
            }
            if (sent > 0) Emit(ev, GameEventType.BotsSent, null, sent, chance);
            return sent;
        }

        /// <summary>
        /// The stream-wide community rewards when a stream is scored (each flies from its Community Beats chip):
        /// Staff pays a flat +1 Influence per point (2026-11-10, it used to waive Staff fees), Troll Army +2 Cash per point of
        /// Risk in the stream, cancelled Risk included (2026-11-10, it used to pay out when deplatformed), Lurkers +1 Cash per
        /// empty space per point (2026-11-18). The retired
        /// Zoomer still rolls (+6 Cash and -3 Influence, separately), though nobody is a Zoomer any more.
        /// </summary>
        static void ScoreCommunityRewards(PlayerState s, int[] scores, ref int cash, ref int influence, List<GameEvent> ev)
        {
            int staffInfluence = Communities.StaffInfluence(scores[(int)Community.Staff]);
            if (staffInfluence > 0)
            {
                influence += staffInfluence;
                Emit(ev, GameEventType.CommunityReward, null, (int)Community.Staff, 0, staffInfluence);
            }

            int trollCash = TrollArmyCash(s, scores);
            if (trollCash > 0)
            {
                cash += trollCash;
                Emit(ev, GameEventType.CommunityReward, null, (int)Community.Trolls, trollCash, 0);
            }

            // Lurkers (2026-11-18): +1 Cash per empty space per point.
            int lurkersCash = LurkersCash(s, scores);
            if (lurkersCash > 0)
            {
                cash += lurkersCash;
                Emit(ev, GameEventType.CommunityReward, null, (int)Community.Lurkers, lurkersCash, 0);
            }

            int chance = Communities.ZoomerChance(scores[(int)Community.Zoomer]);
            if (chance <= 0) return;
            int zoomerCash = Roll(s, chance) ? Communities.ZoomerCash : 0;
            int zoomerInfluence = Roll(s, chance) ? -Communities.ZoomerInfluenceCost : 0;
            if (zoomerCash == 0 && zoomerInfluence == 0) return;
            cash += zoomerCash;
            influence += zoomerInfluence;
            Emit(ev, GameEventType.CommunityReward, null, (int)Community.Zoomer, zoomerCash, zoomerInfluence);
        }

        /// <summary>
        /// Affiliates' Checkout Code (2026-11-10, no longer an action): when the stream is scored, each Affiliate in chat gives
        /// a random viewer in chat +1 Influence for good, up to <see cref="GameRules.PermanentInfluenceCap"/>. It happens before the
        /// viewers score, so it already counts this stream.
        /// </summary>
        static void ApplyCheckoutCodes(PlayerState s, List<GameEvent> ev)
        {
            int affiliates = s.CountInHouse(GuestType.Affiliate);
            for (int k = 0; k < affiliates; k++)
            {
                var candidates = new List<GuestInstance>();
                foreach (var id in s.House)
                {
                    var g = s.Find(id);
                    if (g != null && !g.Temporary && !GuestCatalog.IsToken(g.Kind) && PrintedInfluence(g) < GameRules.PermanentInfluenceCap) candidates.Add(g);
                }
                if (candidates.Count == 0) return;
                var target = candidates[DeterministicRandom.Range(ref s.RngState, candidates.Count)];
                target.BonusInfluence++;
                Emit(ev, GameEventType.InfluenceBoosted, target, 1, target.BonusInfluence);
            }
        }

        /// <summary>One scoring of a viewer's Influence: the retired Finance Bros may double a reward.</summary>
        static int RollInfluence(PlayerState s, GuestInstance g, int influence, int financeChance, List<GameEvent> ev, int tag = 0)
        {
            if (influence > 0 && Roll(s, financeChance))
            {
                Emit(ev, GameEventType.CommunityBonus, g, (int)Community.FinanceBros, influence, tag);
                return influence * 2;
            }
            return influence;
        }

        /// <summary>A percentage roll on the season's deterministic RNG (no roll at all for 0%).</summary>
        static bool Roll(PlayerState s, int percent)
        {
            if (percent <= 0) return false;
            if (percent >= 100) return true;
            return DeterministicRandom.Range(ref s.RngState, 100) < percent;
        }

        static void RecordOutcome(PlayerState s, PartyOutcome outcome, int cash, int influence, int diamonds, int penalty)
        {
            s.LastOutcome = outcome;
            s.LastCash = cash;
            s.LastInfluence = influence;
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

        /// <summary>Recruit: paid from the (shared) Cash; the card joins this member's Audience.</summary>
        static CommandResult Buy(PlayerState s, GuestType type, int member, List<GameEvent> ev)
        {
            if (!GuestCatalog.IsValid(type)) return CommandResult.Fail(Err.Of("err.unknown_viewer"));
            var blocker = GetBuyBlocker(s, type, member);
            if (blocker != null) return CommandResult.Fail(blocker);

            // Shop Control's discount and rival Saboteurs' surcharge only touch this one recruit.
            int cost = RecruitCost(s, type);
            s.Cash -= cost;
            s.RecruitDiscount = 0;
            s.IncomingSabotage = 0;
            var g = AddGuest(s, type, false, member);
            Emit(ev, GameEventType.GuestPurchased, g, cost, member);
            return CommandResult.Success;
        }

        /// <summary>Raise the Sub Cap: paid from the (shared) Influence; raises this member's Sub Cap.</summary>
        static CommandResult Expand(PlayerState s, int member, List<GameEvent> ev)
        {
            var blocker = GetExpandBlocker(s, member);
            if (blocker != null) return CommandResult.Fail(blocker);

            int cost = ExpansionCost(s, member);
            s.Influence -= cost;
            if (s.IsTeam && member == 1)
            {
                s.PartnerCapacity++;
                s.PartnerExpansions++;
            }
            else
            {
                s.Capacity++;
                s.Expansions++;
            }
            Emit(ev, GameEventType.HouseExpanded, null, s.CapacityOf(member), cost, member);
            return CommandResult.Success;
        }

        static CommandResult StartNextNight(PlayerState s, List<GameEvent> ev)
        {
            if (s.Phase != GamePhase.Planning) return CommandResult.Fail(Err.Of("err.finish_stream"));
            if (s.IsFinalNight) return CommandResult.Fail(Err.Of("err.final_night"));
            // Boomers' and Shop Control's discounts were for this shop only.
            s.ExpansionDiscount = 0;
            s.RecruitDiscount = 0;
            s.Night++;
            BeginParty(s, ev);
            return CommandResult.Success;
        }

        // ================================================================== streamer powers

        // Teams (2026-11-18): each member plays their own streamer, so every active has its owner. Its price, cooldown and
        // armed state are that member's (PlayerState.StreamerOf / PowerUsesOf / PowerCooldownOf / PowerArmedOf), and the
        // effects that touch viewers or seats apply to that member's side of the chat:
        //  - their side: Giveaway, Bark at Chat, Double Move, X-Ray Vision, Three-Hour Episode, Pal Sphere;
        //  - the whole team: Smoke Bomb (one shared Risk count), Soft Landing, Subathon, Double or Nothing, Beautiful Sequence
        //    (the team's streak) and the rival powers.
        // A team's two streamers are never the same one, so a streamer type names its member (MemberWithStreamer).

        /// <summary>Null when a member's streamer active can be used now. Rival powers also need a rival to hit.</summary>
        /// <param name="member">Teams: whose active (0 for a solo season).</param>
        public static string GetPowerBlocker(PlayerState s, bool rivalsAvailable = false, int member = 0)
        {
            var streamer = s.StreamerOf(member);
            var info = StreamerCatalog.Get(streamer);
            if (streamer == StreamerType.None || info.Kind == PowerKind.None) return Err.Of("err.power_none");
            if (info.IsPassive) return Err.Of("err.power_passive");
            if (s.IsGameOver) return Err.Of("err.season_over");
            if (s.Phase != GamePhase.Planning) return Err.Of("err.power_shop_only");
            if (info.NeedsRival && !rivalsAvailable) return Err.Of("err.power_needs_rival");
            int cooldown = s.PowerCooldownOf(member);
            if (cooldown > 0) return Err.Of("err.power_cooldown", cooldown);
            if (info.Kind == PowerKind.NextStream)
            {
                if (s.PowerArmedOf(member)) return Err.Of("err.power_armed");
                if (s.IsFinalNight) return Err.Of("err.power_no_next");
            }
            if (streamer == StreamerType.Palmane && PalSphereChoices(s).Count == 0) return Err.Of("err.power_no_pals");
            if (streamer == StreamerType.ManimMakethMan && s.Streak <= 0) return Err.Of("err.power_no_streak");
            if (streamer == StreamerType.BroJogan && EpisodeRoom(s, member) <= 0) return Err.Of("err.sub_cap_maxed");

            int cost = StreamerCatalog.Cost(s, member);
            if (s.Influence < cost) return Err.Of("err.needs_influence", cost);
            return null;
        }

        /// <summary>Null when <paramref name="target"/> can be hit by the actor's (member's) rival power.</summary>
        public static string GetRivalTargetBlocker(PlayerState actor, PlayerState target, int actorMember = 0)
        {
            if (target == null || target == actor || target.IsGameOver) return Err.Of("err.power_no_rival");
            if (actor.StreamerOf(actorMember) == StreamerType.EthicalFboi && target.Influence <= 0) return Err.Of("err.power_rival_broke");
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

        static CommandResult UsePower(PlayerState s, int member, List<GameEvent> ev)
        {
            member = s.IsTeam ? Math.Max(0, Math.Min(1, member)) : 0;
            var blocker = GetPowerBlocker(s, false, member);
            if (blocker != null) return CommandResult.Fail(blocker);

            var streamer = s.StreamerOf(member);
            PayForPower(s, member, ev);
            if (StreamerCatalog.Get(streamer).Kind == PowerKind.NextStream)
            {
                SetPowerArmed(s, member, true);
            }
            else if (streamer == StreamerType.Palmane)
            {
                var choices = PalSphereChoices(s);
                var type = choices[DeterministicRandom.Range(ref s.RngState, choices.Count)];
                var g = AddGuest(s, type, false, member);
                Emit(ev, GameEventType.PalCaught, g);
            }
            else if (streamer == StreamerType.ManimMakethMan)
            {
                // Beautiful Sequence: +1 Cash for each great stream in a row (the streak itself carries on).
                int bonus = Math.Min(StreamerCatalog.StreakMaxBonus, s.Streak);
                s.Cash += bonus;
                s.TotalCashEarned += bonus;
                Emit(ev, GameEventType.StreakBonus, null, bonus, s.Streak);
            }
            return CommandResult.Success;
        }

        /// <summary>
        /// A rival power (Fate's Raid, Ethical F**kboi's Wealth Tax). The host calls this with both players' states, since
        /// the rules engine only ever sees one player at a time. Indexes are the players' positions, used for the log.
        /// </summary>
        /// <param name="actorMember">Teams: the member whose streamer's power it is.</param>
        public static CommandResult UseRivalPower(PlayerState actor, int actorIndex, PlayerState target, int targetIndex,
            List<GameEvent> actorEvents, List<GameEvent> targetEvents, int actorMember = 0)
        {
            if (actor == null) return CommandResult.Fail(Err.Of("err.no_game"));
            actorMember = actor.IsTeam ? Math.Max(0, Math.Min(1, actorMember)) : 0;
            var blocker = GetPowerBlocker(actor, true, actorMember) ?? GetRivalTargetBlocker(actor, target, actorMember);
            if (blocker != null) return CommandResult.Fail(blocker);

            PayForPower(actor, actorMember, actorEvents);
            switch (actor.StreamerOf(actorMember))
            {
                case StreamerType.Fate:
                    target.IncomingHeat += StreamerCatalog.RaidHeat;
                    Emit(targetEvents, GameEventType.RaidReceived, null, StreamerCatalog.RaidHeat, actorIndex);
                    break;
                case StreamerType.EthicalFboi:
                {
                    int amount = Math.Min(StreamerCatalog.WealthTaxAmount, target.Influence);
                    target.Influence -= amount;
                    actor.Influence += amount;
                    Emit(targetEvents, GameEventType.TaxPaid, null, amount, actorIndex);
                    Emit(actorEvents, GameEventType.TaxReceived, null, amount, targetIndex);
                    break;
                }
            }
            return CommandResult.Success;
        }

        /// <summary>
        /// Actives are paid in Influence (from the team's shared wallet); every use makes that member's next one cost more (see
        /// <see cref="StreamerCatalog.Cost"/>). The event says whose: A = the streamer, B = the price, C = the member.
        /// </summary>
        static void PayForPower(PlayerState s, int member, List<GameEvent> ev)
        {
            int cost = StreamerCatalog.Cost(s, member);
            s.Influence -= cost;
            int cooldown = s.StreamerInfoOf(member).Cooldown;
            // Stored one higher: it counts down as the next stream starts, so "2" means two turns without it.
            int stored = cooldown > 0 ? cooldown + 1 : 0;
            if (s.IsTeam && member == 1)
            {
                s.PartnerPowerUses++;
                s.PartnerPowerCooldown = stored;
            }
            else
            {
                s.PowerUses++;
                s.PowerCooldown = stored;
            }
            Emit(ev, GameEventType.PowerUsed, null, (int)s.StreamerOf(member), cost, member);
        }

        static void SetPowerArmed(PlayerState s, int member, bool armed)
        {
            if (s.IsTeam && member == 1) s.PartnerPowerArmed = armed;
            else s.PowerArmed = armed;
        }

        /// <summary>Applies armed powers and rivals' raids as a stream starts, and ticks the cooldowns (each member's, in a team).</summary>
        static void ApplyStreamerEffects(PlayerState s, List<GameEvent> ev)
        {
            if (s.PowerCooldown > 0) s.PowerCooldown--;
            if (s.IsTeam && s.PartnerPowerCooldown > 0) s.PartnerPowerCooldown--;

            if (s.IncomingHeat > 0)
            {
                // Raids alone never reach the limit (3 solo, 4 for a team): at most one under it.
                s.PartyHeat = Math.Min(RiskLimit(s) - 1, s.IncomingHeat);
                s.IncomingHeat = 0;
                Emit(ev, GameEventType.HeatApplied, null, s.PartyHeat);
            }

            // A rival's Doomsdayers: every viewer in this stream may score nothing.
            if (s.IncomingDoom > 0)
            {
                s.PartyDoom = Math.Min(Communities.DoomMaxPoints, s.IncomingDoom);
                s.IncomingDoom = 0;
                Emit(ev, GameEventType.DoomApplied, null, s.PartyDoom, Communities.DoomChance(s.PartyDoom));
            }

            for (int member = 0; member < (s.IsTeam ? 2 : 1); member++)
            {
                if (!s.PowerArmedOf(member)) continue;
                SetPowerArmed(s, member, false);
                ApplyArmedPower(s, s.StreamerOf(member), member, ev);
            }
        }

        /// <summary>A next-stream active bought in the last shop takes effect as the stream starts (C = the member it's for).</summary>
        static void ApplyArmedPower(PlayerState s, StreamerType streamer, int member, List<GameEvent> ev)
        {
            switch (streamer)
            {
                case StreamerType.Shinobi:
                    s.PartyShield = StreamerCatalog.SmokeBombShield;
                    Emit(ev, GameEventType.ShieldApplied, null, s.PartyShield, 0, member);
                    break;
                case StreamerType.MrShark:
                    s.PartyViewerBonus = StreamerCatalog.GiveawayBonus;
                    Emit(ev, GameEventType.GiveawayActive, null, s.PartyViewerBonus, 0, member);
                    break;
                case StreamerType.StrategicAbomination:
                    s.PartyFreeActions = 1;
                    Emit(ev, GameEventType.DoubleMoveActive, null, s.PartyFreeActions, 0, member);
                    break;
                case StreamerType.RudeRadiologist:
                    s.PartyXRay = true;
                    Emit(ev, GameEventType.XRayActive, null, StreamerCatalog.XRayCount, 0, member);
                    break;
                case StreamerType.BroJogan:
                    // Taken back off in ClearParty when the stream ends, however it ends. Never past the seat limit (30, or
                    // 40 on the Bro Jogan player's side of a team's chat; only that side gets the seat).
                    s.PartySeats = Math.Min(StreamerCatalog.EpisodeSeats, EpisodeRoom(s, member));
                    if (s.PartySeats <= 0) { s.PartySeats = 0; break; }
                    AddSeats(s, member, s.PartySeats);
                    Emit(ev, GameEventType.ExtraSeatActive, null, s.PartySeats, 0, member);
                    break;
                case StreamerType.IRunFast:
                    s.PartyBark = StreamerCatalog.BarkBonus;
                    Emit(ev, GameEventType.BarkActive, null, s.PartyBark, 0, member);
                    break;
                case StreamerType.SoftHardBodyMan:
                    s.PartySoftLanding = true;
                    Emit(ev, GameEventType.SoftLandingReady, null, 0, 0, member);
                    break;
                case StreamerType.KaiSonnet:
                    s.PartySubathon = true;
                    Emit(ev, GameEventType.SubathonReady, null, 0, 0, member);
                    break;
                case StreamerType.TrainWrex:
                    s.PartyDoubleDown = true;
                    Emit(ev, GameEventType.DoubleDownReady, null, StreamerCatalog.DoubleDownMaxBonus, StreamerCatalog.DoubleDownLoss, member);
                    break;
            }
        }

        /// <summary>Double or Nothing (Train Wrex, armed for this stream) when it's scored: the stream's Cash again, capped.</summary>
        static int DoubleDownBonus(PlayerState s, int cash, List<GameEvent> ev)
        {
            if (!s.PartyDoubleDown) return 0;
            s.PartyDoubleDown = false;
            int bonus = Math.Min(StreamerCatalog.DoubleDownMaxBonus, Math.Max(0, cash));
            Emit(ev, GameEventType.DoubleDownPaid, null, bonus);
            return bonus;
        }

        /// <summary>Double or Nothing when the stream fails (deplatformed or Server Down): Influence lost too.</summary>
        static void DoubleDownLoss(PlayerState s, List<GameEvent> ev)
        {
            if (!s.PartyDoubleDown) return;
            s.PartyDoubleDown = false;
            int lost = Math.Min(StreamerCatalog.DoubleDownLoss, Math.Max(0, s.Influence));
            s.Influence -= lost;
            Emit(ev, GameEventType.DoubleDownLost, null, lost);
        }

        /// <summary>Three-Hour Episode: Sub Cap a member's next stream can still gain before their chat reaches its seat limit.</summary>
        static int EpisodeRoom(PlayerState s, int member) =>
            MaxCapacity(s) - s.CapacityOf(member);

        /// <summary>The member whose Sub Cap holds this stream's Three-Hour Episode seat (the Bro Jogan player; 0 if nobody is).</summary>
        static int EpisodeMember(PlayerState s) => Math.Max(0, s.MemberWithStreamer(StreamerType.BroJogan));

        /// <summary>Adds (or, negative, takes back) Sub Cap on one member's side; never below 1.</summary>
        static void AddSeats(PlayerState s, int member, int seats)
        {
            if (s.IsTeam && member == 1) s.PartnerCapacity = Math.Max(1, s.PartnerCapacity + seats);
            else s.Capacity = Math.Max(1, s.Capacity + seats);
        }

        /// <summary>
        /// Strategic Abomination's Double Move: the first action used this stream can be used again (in a team, the first one
        /// used on the Strategic Abomination player's side).
        /// </summary>
        static void RefreshFreeAction(PlayerState s, int actorId, List<GameEvent> ev)
        {
            if (s.PartyFreeActions <= 0 || s.Phase != GamePhase.Party) return;
            if (!s.OnSideOf(s.Find(actorId), StreamerType.StrategicAbomination) && s.MemberWithStreamer(StreamerType.StrategicAbomination) >= 0) return;
            if (!s.UsedAbilities.Remove(actorId)) return;
            s.PartyFreeActions--;
            Emit(ev, GameEventType.FreeActionUsed, s.Find(actorId));
        }

        /// <summary>Soft Hard Body Man's Soft Landing (armed for this stream): a deplatformed stream still banks half its Cash.</summary>
        static void SoftLanding(PlayerState s, List<GameEvent> ev)
        {
            if (!s.PartySoftLanding) return;
            ProjectScore(s, out int cash, out _);
            int banked = Math.Max(0, cash) / 2;
            if (banked <= 0) return;
            s.Cash += banked;
            s.TotalCashEarned += banked;
            Emit(ev, GameEventType.SoftLanding, null, banked);
        }

        /// <summary>
        /// The next viewers in the queue that X-Ray Vision reveals this stream (empty without it). In a team only the Rude
        /// Radiologist player's queue shows.
        /// </summary>
        public static List<int> XRayQueue(PlayerState s, int member = 0)
        {
            var result = new List<int>();
            if (!s.PartyXRay || s.Phase != GamePhase.Party) return result;
            int owner = s.MemberWithStreamer(StreamerType.RudeRadiologist);
            if (s.IsTeam && owner >= 0 && owner != member) return result;
            for (int i = 0; i < s.Pool.Count && result.Count < StreamerCatalog.XRayCount; i++)
                if (s.Owns(s.Find(s.Pool[i]), member)) result.Add(s.Pool[i]);
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
