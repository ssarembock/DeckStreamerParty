namespace DeckParty.Core
{
    public enum CommandType : byte
    {
        OpenDoor = 0,
        UseAbility = 1,
        AdmitPeeked = 2,
        RejectPeeked = 3,
        EndParty = 4,
        BlameGuest = 5,
        BuyGuest = 6,
        ExpandHouse = 7,
        StartNextNight = 8,
        ResolveAttraction = 9,
        /// <summary>Use your streamer's active power (shop phase only). TargetA = the rival's player index for rival powers, -1 for automatic.</summary>
        UseStreamerPower = 10,
    }

    /// <summary>A player's intent. Identical for local play and network play.</summary>
    public struct GameCommand
    {
        public CommandType Type;
        public int Actor;
        public int TargetA;
        public int TargetB;
        public int TargetC;
        /// <summary>Which option was chosen for actions that offer a choice (Pepe's mood, Stonks' investment...).</summary>
        public int Option;
        public GuestType Guest;

        static GameCommand Simple(CommandType type) =>
            new GameCommand { Type = type, Actor = -1, TargetA = -1, TargetB = -1, TargetC = -1 };

        public static GameCommand OpenDoor() => Simple(CommandType.OpenDoor);
        public static GameCommand EndParty() => Simple(CommandType.EndParty);
        public static GameCommand AdmitPeeked() => Simple(CommandType.AdmitPeeked);
        public static GameCommand RejectPeeked() => Simple(CommandType.RejectPeeked);
        public static GameCommand ExpandHouse() => Simple(CommandType.ExpandHouse);
        public static GameCommand StartNextNight() => Simple(CommandType.StartNextNight);

        public static GameCommand UsePower(int rivalIndex = -1)
        {
            var command = Simple(CommandType.UseStreamerPower);
            command.TargetA = rivalIndex;
            return command;
        }

        public static GameCommand UseAbility(int actor, int targetA = -1, int targetB = -1, int targetC = -1, int option = 0) =>
            new GameCommand { Type = CommandType.UseAbility, Actor = actor, TargetA = targetA, TargetB = targetB, TargetC = targetC, Option = option };

        public static GameCommand Blame(int guestId)
        {
            var command = Simple(CommandType.BlameGuest);
            command.TargetA = guestId;
            return command;
        }

        public static GameCommand Buy(GuestType type)
        {
            var command = Simple(CommandType.BuyGuest);
            command.Guest = type;
            return command;
        }

        /// <summary>Distracted Boyfriend's choice: swap <paramref name="replaced"/> for <paramref name="chosen"/>, or pass -1 for both to skip.</summary>
        public static GameCommand ResolveAttraction(int replaced, int chosen)
        {
            var command = Simple(CommandType.ResolveAttraction);
            command.TargetA = replaced;
            command.TargetB = chosen;
            return command;
        }
    }

    public readonly struct CommandResult
    {
        public readonly bool Ok;
        /// <summary>A localization key with optional arguments (see <see cref="Err"/>), never display text.</summary>
        public readonly string Error;

        CommandResult(bool ok, string error)
        {
            Ok = ok;
            Error = error;
        }

        public static CommandResult Success => new CommandResult(true, null);
        public static CommandResult Fail(string error) => new CommandResult(false, error);
    }

    /// <summary>
    /// Builds rule-engine messages as localization keys plus arguments: "key|arg|arg". The rules never hold display
    /// text, so every player reads rejections in their own language. Arguments that start with "@g" are a
    /// viewer type id (shown as the viewer's localized name); "@a" is a viewer type whose action name is shown.
    /// </summary>
    public static class Err
    {
        public const char Separator = '|';

        public static string Of(string key) => key;
        public static string Of(string key, object a) => key + Separator + a;
        public static string Of(string key, object a, object b) => key + Separator + a + Separator + b;

        public static string Guest(GuestType type) => "@g" + (int)type;
        public static string Action(GuestType type) => "@a" + (int)type;
    }
}
