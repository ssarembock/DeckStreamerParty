using System;
using DeckParty.Core;

namespace DeckParty
{
    /// <summary>Turns engine events into Stream Log lines (keys "log.*").</summary>
    public static class EventText
    {
        /// <summary>Multiplayer: turns a player index from a rival-power event into a (coloured) name.</summary>
        public static Func<int, string> PlayerName;

        /// <summary>Set by the game screen in multiplayer: Bots only travel to other players, so single player says nothing about them.</summary>
        public static bool Multiplayer;

        /// <param name="owner">Whose stream this is when it isn't yours (multiplayer watching); null for your own.</param>
        public static string Describe(GameEvent e, PlayerState state, string owner = null)
        {
            string viewer = e.GuestId >= 0 ? GuestVisuals.NameTag(e.Guest) : string.Empty;
            bool mine = owner == null;

            switch (e.Type)
            {
                case GameEventType.NightStarted:
                    return GuestVisuals.Colored(Loc.Get("log.night", "night", e.A), UIPalette.Purple) + " " +
                           (mine ? Loc.Get("log.live_you") : Loc.Get("log.live_other", "player", owner));

                case GameEventType.GuestArrived:
                    if (e.A < 0) return Loc.Get("log.joined", "viewer", viewer);
                    return Loc.Get("log.joined_via", "viewer", viewer, "source", Source(e.A, state));

                case GameEventType.GuestRemoved:
                    switch ((LeaveReason)e.B)
                    {
                        case LeaveReason.SentOutside: return Loc.Get("log.sent_back", "viewer", viewer);
                        case LeaveReason.Replaced: return Loc.Get("log.replaced", "viewer", viewer);
                        case LeaveReason.Followed: return Loc.Get("log.followed", "viewer", viewer);
                        case LeaveReason.Gone: return mine ? Loc.Get("log.gone_you", "viewer", viewer) : Loc.Get("log.gone_other", "viewer", viewer, "player", owner);
                        default: return e.A >= 0 ? Loc.Get("log.removed_by", "viewer", viewer, "source", Source(e.A, state)) : Loc.Get("log.removed", "viewer", viewer);
                    }

                case GameEventType.GuestRejected:
                    return Loc.Get("log.denied", "viewer", viewer);

                case GameEventType.GuestPeeked:
                    return Loc.Get("log.peeked", "source", Source(e.A, state), "viewer", viewer);

                case GameEventType.GuestScored:
                    if (e.C != 0) return null; // the end-of-stream breakdown is summarised separately
                    return Loc.Get("log.scores_now", "viewer", viewer, "values", Values(e.A, e.B));

                case GameEventType.AbilityUsed:
                {
                    string action = GuestVisuals.Colored(GuestVisuals.ActionName(e.Guest), UIPalette.Purple);
                    string choice = GuestVisuals.OptionLabel(e.Guest, e.C);
                    return choice.Length > 0
                        ? Loc.Get("log.action_used_choice", ("viewer", viewer), ("action", action), ("choice", choice))
                        : Loc.Get("log.action_used", "viewer", viewer, "action", action);
                }

                case GameEventType.EveryoneOutside:
                    return Loc.Get("log.everyone_out", "viewer", viewer, "count", e.A);

                case GameEventType.PoliceRaid:
                    return GuestVisuals.Colored(Loc.Get("log.platform_ban", "risk", e.A), UIPalette.Red);

                case GameEventType.FireMarshal:
                    return GuestVisuals.Colored(Loc.Get("log.server_down", "filled", e.A, "cap", e.B), UIPalette.Orange);

                case GameEventType.PartyEnded:
                    return Loc.Get("log.stream_ended", "values", Values(e.A, e.B), "diamonds", Terms.DiamondCount(e.C));

                case GameEventType.CashShortfall:
                    return GuestVisuals.Colored(Loc.Get("log.shortfall", "cash", e.A, "penalty", e.B), UIPalette.Red);

                case GameEventType.GuestBlamed:
                    return Loc.Get("log.banned", "viewer", viewer);

                case GameEventType.GuestPurchased:
                    return mine ? Loc.Get("log.purchased_you", "viewer", viewer) : Loc.Get("log.purchased_other", "viewer", viewer, "player", owner);

                case GameEventType.HouseExpanded:
                    return Loc.Get("log.sub_cap_raised", "cap", e.A);

                case GameEventType.Victory:
                    return GuestVisuals.Colored(mine
                        ? Loc.Get("log.victory_you", "count", GameRules.DiamondsToWin)
                        : Loc.Get("log.victory_other", "count", GameRules.DiamondsToWin, "player", owner), UIPalette.Gold);

                case GameEventType.Defeat:
                    return GuestVisuals.Colored(Loc.Get("log.defeat"), UIPalette.TextDim);

                case GameEventType.GuestGained:
                    return Loc.Get("log.gained", "viewer", viewer, "values", Values(e.A, e.B));

                case GameEventType.PartyPotGained:
                    return Loc.Get("log.pot", "viewer", viewer, "values", Values(e.A, e.B));

                case GameEventType.TroubleAdded:
                    return Loc.Get("log.risk_added", "viewer", viewer, "risk", GuestVisuals.RiskText(e.A));

                case GameEventType.TroubleRemoved:
                    return Loc.Get("log.risk_removed", "viewer", viewer);

                case GameEventType.ThisIsFine:
                    return GuestVisuals.Colored(Loc.Get("log.this_is_fine", "viewer", viewer, "count", e.A), UIPalette.Orange);

                case GameEventType.Infected:
                    return Loc.Get("log.infected", "viewer", viewer);

                case GameEventType.Attached:
                    return Loc.Get("log.attached", "viewer", viewer, "source", Source(e.A, state));

                case GameEventType.StatsCopied:
                    return Loc.Get("log.copied", ("viewer", viewer), ("source", Source(e.A, state)), ("values", Values(e.B, e.C)));

                case GameEventType.TemporaryCopy:
                    return Loc.Get("log.temp_copy", "viewer", viewer);

                case GameEventType.ShieldBlocked:
                    return Loc.Get("log.shield", "viewer", viewer);

                case GameEventType.PermanentChange:
                    if (e.A == 0) return Loc.Get("log.even_match", "viewer", viewer);
                    return Loc.Get(e.A > 0 ? "log.permanent_gain" : "log.permanent_loss",
                        ("viewer", viewer), ("amount", Math.Abs(e.A)), ("total", (e.B >= 0 ? "+" : "") + e.B));

                case GameEventType.Struck:
                    return Loc.Get("log.struck", "viewer", viewer, "count", e.A);

                case GameEventType.Transformed:
                    return Loc.Get("log.rickrolled", "viewer", viewer, "other", GuestVisuals.NameTag((GuestType)e.A));

                case GameEventType.PositionsSwapped:
                    return Loc.Get("log.swapped", "a", Source(e.A, state), "b", Source(e.B, state));

                case GameEventType.PrintedExchanged:
                    return Loc.Get("log.printed_swapped", "a", Source(e.A, state), "b", Source(e.B, state));

                case GameEventType.MoodChosen:
                    return Loc.Get("log.mood", "viewer", viewer, "mood", GuestVisuals.MoodName((PepeMood)e.A));

                case GameEventType.Invested:
                    return Loc.Get("log.invested", "values", Values(e.A, e.B));

                case GameEventType.InvestmentPaid:
                    return GuestVisuals.Colored(Loc.Get("log.investment_paid", "values", Values(e.A, e.B)), UIPalette.Green);

                case GameEventType.MovedToBack:
                    return Loc.Get("log.moved_back", "viewer", viewer);

                case GameEventType.ImmunityGranted:
                    return Loc.Get("log.immune", "viewer", viewer);

                case GameEventType.Converted:
                    return Loc.Get(e.A == 1 ? "log.convert_cash" : "log.convert_subs", "viewer", viewer);

                case GameEventType.Zeroed:
                    return Loc.Get("log.zeroed", "viewer", viewer);

                case GameEventType.Doubled:
                    return Loc.Get(e.A == 1 ? "log.doubled_subs" : "log.doubled_cash", "viewer", viewer);

                case GameEventType.AttractionRevealed:
                    return Loc.Get("log.attraction", "viewer", viewer, "count", e.A);

                case GameEventType.AttractionResolved:
                    return e.A < 0 ? Loc.Get("log.attraction_skip") : Loc.Get("log.attraction_swap", "a", Source(e.A, state), "b", Source(e.B, state));

                case GameEventType.TemporaryCopiesLeft:
                    return Loc.Get("log.temp_left", "count", e.A);

                // --- audience classics ---
                case GameEventType.ScoredNow:
                    return Loc.Get("log.scored_now", ("viewer", viewer), ("source", Source(e.C, state)), ("values", Values(e.A, e.B)));

                case GameEventType.InterviewGiven:
                    return GuestVisuals.Colored(Loc.Get("log.interview", "viewer", viewer, "cash", GuestVisuals.CashText(e.A)), UIPalette.Green);

                case GameEventType.CashBoosted:
                    return Loc.Get("log.cash_boosted", "viewer", viewer, "total", GuestVisuals.CashText(e.B));

                case GameEventType.MoodFlipped:
                    return e.A != 0
                        ? GuestVisuals.Colored(Loc.Get("log.hate_watching", "viewer", viewer), UIPalette.Red)
                        : Loc.Get("log.just_watching", "viewer", viewer);

                // --- streamer powers ---
                case GameEventType.PowerUsed:
                {
                    var streamer = (StreamerType)e.A;
                    return GuestVisuals.Colored(Loc.Get("log.power_used", "streamer", StreamerVisuals.Name(streamer), "power", StreamerVisuals.PowerName(streamer)), UIPalette.Gold)
                           + CostSuffix(e.B);
                }
                case GameEventType.HeatApplied:
                    return GuestVisuals.Colored(Loc.Get("log.heat_applied", "risk", GuestVisuals.RiskText(e.A)), UIPalette.Red);
                case GameEventType.ShieldApplied:
                    return GuestVisuals.Colored(Loc.Get("log.shield_applied", "count", e.A), UIPalette.Teal);
                case GameEventType.GiveawayActive:
                    return GuestVisuals.Colored(Loc.Get("log.giveaway", "count", e.A), UIPalette.Gold);
                case GameEventType.PalCaught:
                    return Loc.Get("log.pal_caught", "viewer", viewer);
                case GameEventType.SoftLanding:
                    return GuestVisuals.Colored(Loc.Get("log.soft_landing", "value", e.A), UIPalette.Pink);
                case GameEventType.StreakBonus:
                    return GuestVisuals.Colored(Loc.Get("log.streak", "count", e.B, "bonus", e.A), UIPalette.Teal);
                case GameEventType.FreeActionUsed:
                    return Loc.Get("log.free_action", "viewer", viewer);
                case GameEventType.DoubleMoveActive:
                    return GuestVisuals.Colored(Loc.Get("log.double_move"), UIPalette.Purple);
                case GameEventType.XRayActive:
                    return GuestVisuals.Colored(Loc.Get("log.xray", "count", e.A), UIPalette.Teal);
                case GameEventType.RaidReceived:
                    return GuestVisuals.Colored(Loc.Get("log.raid_received", "raider", Player(e.B), "count", e.A), UIPalette.Red);
                case GameEventType.TaxPaid:
                    return GuestVisuals.Colored(Loc.Get("log.tax_paid", "taxer", Player(e.B), "amount", e.A), UIPalette.Red);
                case GameEventType.TaxReceived:
                    return GuestVisuals.Colored(Loc.Get("log.tax_received", "payer", Player(e.B), "amount", e.A), UIPalette.Green);

                // --- communities ---
                case GameEventType.CommunityBonus:
                {
                    var community = (Community)e.A;
                    string tag = GameIcons.Tag(community) + " ";
                    switch (community)
                    {
                        case Community.Staff:
                            return tag + Loc.Get("log.staff_free", "viewer", viewer, "cash", GuestVisuals.CashText(-e.B));
                        case Community.Content:
                            return tag + Loc.Get("log.content_encore", "viewer", viewer);
                        case Community.FinanceBros:
                            return tag + Loc.Get("log.finance_double", "viewer", viewer, "cash", GuestVisuals.CashText(e.B));
                        case Community.Doomsdayers:
                            return tag + GuestVisuals.Colored(Loc.Get("log.doomed", "viewer", viewer), UIPalette.Red);
                        case Community.ArmchairActivists:
                            return tag + GuestVisuals.Colored(Loc.Get("log.all_talk", "viewer", viewer), UIPalette.TextDim);
                        case Community.Clout:
                            return tag + Loc.Get("log.clout", "viewer", viewer, "subs", GuestVisuals.PopText(e.B));
                    }
                    return null;
                }
                case GameEventType.MemeticsRepeat:
                    return GameIcons.Tag(Community.MemeticsDivision) + " " + GuestVisuals.Colored(Loc.Get("log.memetics_repeat", "value", e.A), CommunityVisuals.Color(Community.MemeticsDivision));
                case GameEventType.BotsSent:
                    if (!Multiplayer) return null;
                    return GameIcons.Tag(Community.Bots) + " " + Loc.Get(mine ? "log.bots_sent_you" : "log.bots_sent_other", "count", e.A, "player", owner ?? string.Empty);
                case GameEventType.BotsArrived:
                    return GameIcons.Tag(Community.Bots) + " " + GuestVisuals.Colored(Loc.Get("log.bots_arrived", "count", e.A), UIPalette.TextDim);
                case GameEventType.CommunityReward:
                {
                    var community = (Community)e.A;
                    string name = GuestVisuals.Colored(CommunityVisuals.Name(community), CommunityVisuals.Color(community));
                    return GameIcons.Tag(community) + " " + Loc.Get("log.community_reward", "community", name, "values", Amounts(e.B, e.C));
                }
                case GameEventType.ServerRestored:
                    return GameIcons.Tag(Community.Techspertise) + " " + GuestVisuals.Colored(Loc.Get("log.server_restored", "value", e.A), UIPalette.Teal);
                case GameEventType.TrollsPaid:
                    return GameIcons.Tag(Community.Trolls) + " " + GuestVisuals.Colored(Loc.Get("log.trolls_paid", "values", Amounts(e.A, e.B)), UIPalette.Orange);
                case GameEventType.DoomApplied:
                    return GameIcons.Tag(Community.Doomsdayers) + " " + GuestVisuals.Colored(Loc.Get("log.doom_applied", "value", e.B), UIPalette.Red);
            }
            return null;
        }

        static string CostSuffix(int cash) =>
            cash > 0 ? "  " + GuestVisuals.Colored("(-" + cash + " " + GameIcons.Tag(ResourceIcon.Cash) + ")", UIPalette.TextDim) : string.Empty;

        static string Player(int index) => PlayerName != null ? PlayerName(index) : Loc.Get("common.rival");

        static string Source(int guestId, PlayerState state)
        {
            var g = state?.Find(guestId);
            return g != null ? GuestVisuals.NameTag(g.Kind) : Loc.Get("common.someone");
        }

        static string Values(int pop, int cash) => Amounts(pop, cash);

        /// <summary>Subs and Cash as icon amounts ("+3 [subs] +1 [cash]"), skipping a zero.</summary>
        public static string Amounts(int pop, int cash)
        {
            if (cash == 0) return GuestVisuals.PopText(pop);
            if (pop == 0) return GuestVisuals.CashText(cash);
            return GuestVisuals.PopText(pop) + " " + GuestVisuals.CashText(cash);
        }
    }
}
