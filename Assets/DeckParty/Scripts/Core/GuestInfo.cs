namespace DeckParty.Core
{
    /// <summary>
    /// Immutable rule data for one guest type. Presentation (sprites, colours, renames) lives in the
    /// Unity-side GameDatabase so the rules stay identical on every peer.
    /// </summary>
    public sealed class GuestInfo
    {
        public readonly GuestType Type;
        public readonly string Name;
        public readonly int Cost;
        public readonly bool Purchasable;
        public readonly bool IsDiamond;
        public readonly GuestCategory Category;
        public readonly int Pop;
        public readonly int Cash;
        public readonly bool Trouble;
        public readonly bool CancelsTrouble;
        public readonly int Brings;
        /// <summary>House spaces this guest takes up (Big Chungus takes 3).</summary>
        public readonly int Size;
        public readonly AbilityKind Ability;
        public readonly string AbilityName;
        public readonly string Description;

        public GuestInfo(
            GuestType type,
            string name,
            int cost,
            GuestCategory category,
            string description,
            int pop = 0,
            int cash = 0,
            bool trouble = false,
            bool diamond = false,
            bool cancels = false,
            int brings = 0,
            int size = 1,
            AbilityKind ability = AbilityKind.None,
            string abilityName = null,
            bool purchasable = true)
        {
            Type = type;
            Name = name;
            Cost = cost;
            Category = category;
            Description = description;
            Pop = pop;
            Cash = cash;
            Trouble = trouble;
            IsDiamond = diamond;
            CancelsTrouble = cancels;
            Brings = brings;
            Size = size < 1 ? 1 : size;
            Ability = ability;
            AbilityName = abilityName ?? ability.ToString();
            Purchasable = purchasable;
        }

        public bool HasAbility => Ability != AbilityKind.None;

        public int MaxCopies => IsDiamond ? int.MaxValue : GameRules.MaxCopiesPerGuest;

        public TargetMode Target => TargetFor(Ability);

        public ActionCategory ActionCategory => CategoryOf(Ability);

        public string[] Options => OptionsFor(Ability);

        public static TargetMode TargetFor(AbilityKind kind)
        {
            switch (kind)
            {
                case AbilityKind.Fetch: return TargetMode.WaitingGuest;
                case AbilityKind.Inspect: return TargetMode.Inspect;
                case AbilityKind.Boot:
                case AbilityKind.Convert:
                case AbilityKind.Immunity:
                case AbilityKind.Style: return TargetMode.AnyAttendee;
                case AbilityKind.Troll:
                case AbilityKind.PlayOff:
                case AbilityKind.Mog:
                case AbilityKind.Arson:
                case AbilityKind.Strike:
                case AbilityKind.Refund:
                case AbilityKind.Rickroll:
                case AbilityKind.Photograph: return TargetMode.OtherAttendee;
                case AbilityKind.SwapOrder:
                case AbilityKind.Pirouette:
                case AbilityKind.Drake: return TargetMode.TwoAttendees;
                case AbilityKind.Bombard: return TargetMode.UpToThree;
                case AbilityKind.BootPair: return TargetMode.AdjacentPair;
                default: return TargetMode.None;
            }
        }

        public static ActionCategory CategoryOf(AbilityKind kind)
        {
            switch (kind)
            {
                case AbilityKind.Fetch: return ActionCategory.Recruit;
                case AbilityKind.Boot:
                case AbilityKind.Troll:
                case AbilityKind.PlayOff:
                case AbilityKind.Strike:
                case AbilityKind.Bombard:
                case AbilityKind.Refund:
                case AbilityKind.BootPair: return ActionCategory.Remove;
                case AbilityKind.Mood:
                case AbilityKind.Mog:
                case AbilityKind.Arson:
                case AbilityKind.Pirouette:
                case AbilityKind.Drake:
                case AbilityKind.Convert:
                case AbilityKind.Immunity:
                case AbilityKind.Calm:
                case AbilityKind.Style: return ActionCategory.Modify;
                case AbilityKind.Reset:
                case AbilityKind.SendBack:
                case AbilityKind.SwapOrder:
                case AbilityKind.Rickroll: return ActionCategory.Shuffle;
                case AbilityKind.Inspect:
                case AbilityKind.Reroll:
                case AbilityKind.SideEye:
                case AbilityKind.Peek: return ActionCategory.Scout;
                case AbilityKind.Invest:
                case AbilityKind.Photograph:
                case AbilityKind.Interview: return ActionCategory.Economy;
                default: return ActionCategory.None;
            }
        }

        /// <summary>Choices the player makes before picking targets, or null when the action has none.</summary>
        public static string[] OptionsFor(AbilityKind kind)
        {
            switch (kind)
            {
                case AbilityKind.Mood: return new[] { "Happy", "Sad", "Angry" };
                case AbilityKind.Invest: return new[] { "Invest 2 Subs", "Invest 1 $" };
                case AbilityKind.Reroll: return new[] { "Reroll", "Peek" };
                case AbilityKind.Drake: return new[] { "Double Subs", "Double $" };
                case AbilityKind.Convert: return new[] { "Subs to $", "$ to Subs" };
                default: return null;
            }
        }

        /// <summary>How many guests the action needs picked (min, max).</summary>
        public static void TargetCount(AbilityKind kind, out int min, out int max)
        {
            switch (TargetFor(kind))
            {
                case TargetMode.AnyAttendee:
                case TargetMode.OtherAttendee:
                case TargetMode.WaitingGuest:
                    min = max = 1;
                    break;
                case TargetMode.TwoAttendees:
                case TargetMode.AdjacentPair:
                    min = max = 2;
                    break;
                case TargetMode.UpToThree:
                    min = 1;
                    max = 3;
                    break;
                default:
                    min = max = 0;
                    break;
            }
        }
    }
}
