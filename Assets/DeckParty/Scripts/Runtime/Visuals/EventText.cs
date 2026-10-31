using DeckParty.Core;

namespace DeckParty
{
    /// <summary>Turns engine events into party-log lines.</summary>
    public static class EventText
    {
        /// <param name="owner">Whose party this is when it isn't yours (multiplayer watching); null for your own.</param>
        public static string Describe(GameEvent e, PlayerState state, string owner = null)
        {
            string guest = e.GuestId >= 0 ? GuestVisuals.NameTag(e.Guest) : string.Empty;
            string their = owner == null ? "your" : owner + "'s";

            switch (e.Type)
            {
                case GameEventType.NightStarted:
                    return GuestVisuals.Colored($"- Night {e.A} -", UIPalette.Purple) + (owner == null ? " You're live." : $" {owner} is live.");

                case GameEventType.GuestArrived:
                    if (e.A < 0) return $"{guest} joins the party.";
                    return $"{guest} comes in thanks to {Source(e.A, state)}.";

                case GameEventType.GuestRemoved:
                    switch ((LeaveReason)e.B)
                    {
                        case LeaveReason.SentOutside: return $"{guest} is sent back outside.";
                        case LeaveReason.Replaced: return $"{guest} is swapped out for the night.";
                        case LeaveReason.Followed: return $"{guest} storms out after their partner.";
                        case LeaveReason.Gone: return $"{guest} leaves {their} {Terms.Squad} for good.";
                        default: return e.A >= 0 ? $"{guest} is removed from chat by {Source(e.A, state)}." : $"{guest} is removed from chat.";
                    }

                case GameEventType.GuestRejected:
                    return $"{guest} is turned away for tonight.";

                case GameEventType.GuestPeeked:
                    return $"{Source(e.A, state)} spots {guest} waiting outside.";

                case GameEventType.GuestScored:
                    if (e.C != 0) return null; // end-of-party breakdown is summarised separately
                    return $"{guest} scores {Values(e.A, e.B)} right now.";

                case GameEventType.AbilityUsed:
                {
                    var info = GuestCatalog.Get(e.Guest);
                    var options = info.Options;
                    string choice = options != null && e.C >= 0 && e.C < options.Length ? $" ({options[e.C]})" : string.Empty;
                    return $"{guest} uses {GuestVisuals.Colored(info.AbilityName, UIPalette.Purple)}{choice}.";
                }

                case GameEventType.EveryoneOutside:
                    return $"{guest} resets the stream: {e.A} guest{(e.A == 1 ? "" : "s")} sent outside!";

                case GameEventType.PoliceRaid:
                    return GuestVisuals.Colored($"{Terms.Raid} {Terms.Risk} hit {e.A}. The SWAT team ends the party. No payout tonight.", UIPalette.Red);

                case GameEventType.FireMarshal:
                    return GuestVisuals.Colored($"{Terms.Overload} {e.A} spaces filled with a {Terms.SubCap} of {e.B}. Party shut down.", UIPalette.Orange);

                case GameEventType.PartyEnded:
                    return $"Party wraps up: {Values(e.A, e.B)} with {Terms.DiamondCount(e.C)}.";

                case GameEventType.CashShortfall:
                    return GuestVisuals.Colored($"Couldn't pay {e.A} $ in fees: -{e.B} {Terms.GiftedSubs}.", UIPalette.Red);

                case GameEventType.GuestBlamed:
                    return $"{guest} gets a {Terms.Ban} and sits out the next party.";

                case GameEventType.GuestPurchased:
                    return $"{guest} joins {their} {Terms.Squad}.";

                case GameEventType.HouseExpanded:
                    return $"{Terms.SubCap} raised to {e.A}.";

                case GameEventType.Victory:
                    return GuestVisuals.Colored($"{GameRules.DiamondsToWin} {Terms.Diamonds.ToUpperInvariant()} AT ONE PARTY! {(owner == null ? "You win!" : owner + " wins!")}", UIPalette.Gold);

                case GameEventType.Defeat:
                    return GuestVisuals.Colored("The season is over.", UIPalette.TextDim);

                case GameEventType.GuestGained:
                    return $"{guest} gains {Values(e.A, e.B)}.";

                case GameEventType.PartyPotGained:
                    return $"{guest} earns {Values(e.A, e.B)} for tonight's take.";

                case GameEventType.TroubleAdded:
                    return $"{guest} adds {GuestVisuals.RiskText(e.A)}.";

                case GameEventType.TroubleRemoved:
                    return $"{guest} loses their {Terms.Risk}.";

                case GameEventType.ThisIsFine:
                    return GuestVisuals.Colored("This is fine.", UIPalette.Orange) + $" {guest} calls off the SWAT team and clears {e.A} {Terms.Risk}.";

                case GameEventType.Infected:
                    return $"{guest} catches the Skibidi infection!";

                case GameEventType.Attached:
                    return $"{guest} attaches herself to {Source(e.A, state)}.";

                case GameEventType.StatsCopied:
                    return $"{guest} copies {Source(e.A, state)}: {Values(e.B, e.C)}.";

                case GameEventType.TemporaryCopy:
                    return $"Another {guest} joins the waiting line. Follow me!";

                case GameEventType.ShieldBlocked:
                    return $"{guest}'s bad luck holds: it doesn't work, and he permanently gains +1 {Terms.Subs}.";

                case GameEventType.PermanentChange:
                    if (e.A == 0) return $"{guest} is evenly matched. Nothing changes.";
                    return $"{guest} permanently {(e.A > 0 ? "gains" : "loses")} {System.Math.Abs(e.A)} {Terms.Subs} (now {(e.B >= 0 ? "+" : "")}{e.B}).";

                case GameEventType.Struck:
                    return $"{guest} takes strike {e.A}!";

                case GameEventType.Transformed:
                    return $"{guest} gets Rickrolled into {GuestVisuals.NameTag((GuestType)e.A)}!";

                case GameEventType.PositionsSwapped:
                    return $"{Source(e.A, state)} and {Source(e.B, state)} swap places.";

                case GameEventType.PrintedExchanged:
                    return $"{Source(e.A, state)} and {Source(e.B, state)} swap their printed {Terms.Subs}.";

                case GameEventType.MoodChosen:
                    return $"{guest} is feeling {((PepeMood)e.A).ToString().ToLowerInvariant()}.";

                case GameEventType.Invested:
                    return $"Invested so far tonight: {Values(e.A, e.B)}. Triple back if the party succeeds.";

                case GameEventType.InvestmentPaid:
                    return GuestVisuals.Colored("Stonks!", UIPalette.Green) + $" The investment pays {Values(e.A, e.B)}.";

                case GameEventType.MovedToBack:
                    return $"{guest} gets the side eye and goes to the back of the line.";

                case GameEventType.ImmunityGranted:
                    return $"{guest} is now immune to actions tonight.";

                case GameEventType.Converted:
                    return e.A == 1 ? $"{guest} will turn their {Terms.Subs} into $ tonight." : $"{guest} will turn their $ into {Terms.Subs} tonight.";

                case GameEventType.Zeroed:
                    return $"{guest} earns nothing tonight.";

                case GameEventType.Doubled:
                    return $"{guest}'s printed {(e.A == 1 ? Terms.Subs : "$")} are doubled tonight.";

                case GameEventType.AttractionRevealed:
                    return $"{guest} spots {e.A} new attraction{(e.A == 1 ? "" : "s")} outside...";

                case GameEventType.AttractionResolved:
                    return e.A < 0 ? "Nobody gets replaced." : $"{Source(e.A, state)} is out, {Source(e.B, state)} is in.";

                case GameEventType.TemporaryCopiesLeft:
                    return $"{e.A} temporary Knuckles cop{(e.A == 1 ? "y" : "ies")} wander off.";
            }
            return null;
        }

        static string Source(int guestId, PlayerState state)
        {
            var g = state?.Find(guestId);
            return g != null ? GuestVisuals.NameTag(g.Kind) : "someone";
        }

        static string Values(int pop, int cash)
        {
            if (cash == 0) return GuestVisuals.PopText(pop);
            if (pop == 0) return GuestVisuals.CashText(cash);
            return GuestVisuals.PopText(pop) + " " + GuestVisuals.CashText(cash);
        }
    }
}
