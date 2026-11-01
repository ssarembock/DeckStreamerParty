using System;
using DeckParty.Core;
using Unity.Collections;
using Unity.Netcode;

namespace DeckParty.Networking
{
    public enum MatchPhase : byte
    {
        Lobby = 0,
        InGame = 1,
        Results = 2,
    }

    public enum MatchMode : byte
    {
        /// <summary>Everyone plays the same night; the next night starts when all players are ready.</summary>
        SyncNights = 0,
        /// <summary>Everyone plays at their own pace; the first 4-Diamond party wins instantly.</summary>
        Race = 1,
        /// <summary>Each night players throw their parties one after another while everyone else watches.</summary>
        Turns = 2,
    }

    /// <summary>Lobby order and names of the match modes.</summary>
    public static class MatchModes
    {
        public static readonly MatchMode[] Order = { MatchMode.Turns, MatchMode.SyncNights, MatchMode.Race };

        public static string[] Labels => new[] { Loc.Get("mode.turns"), Loc.Get("mode.sync"), Loc.Get("mode.race") };

        public static int IndexOf(MatchMode mode)
        {
            int index = Array.IndexOf(Order, mode);
            return index >= 0 ? index : 0;
        }

        public static MatchMode At(int index) => Order[Math.Max(0, Math.Min(Order.Length - 1, index))];

        public static string ShortName(MatchMode mode)
        {
            switch (mode)
            {
                case MatchMode.Turns: return Loc.Get("mode.turns_short");
                case MatchMode.Race: return Loc.Get("mode.race_short");
                default: return Loc.Get("mode.sync_short");
            }
        }
    }

    /// <summary>Lobby choices for the Take Turns timer: Off, Hard (30 s), Medium (60 s), Easy (120 s) and Custom.</summary>
    public static class TurnTimers
    {
        public static readonly int[] Presets = { 0, 30, 60, 120 };
        public const int CustomIndex = 4;
        public const int MinCustom = 10;
        public const int MaxCustom = 600;
        /// <summary>The countdown turns red and ticks for the last few seconds.</summary>
        public const int WarningSeconds = 10;

        public static string[] Labels => new[]
        {
            Loc.Get("timer.off"), Loc.Get("timer.hard", "seconds", 30), Loc.Get("timer.medium", "seconds", 60),
            Loc.Get("timer.easy", "seconds", 120), Loc.Get("timer.custom"),
        };

        public static int IndexOf(int seconds)
        {
            int index = Array.IndexOf(Presets, seconds);
            return index >= 0 ? index : CustomIndex;
        }

        public static string Describe(int seconds) => seconds <= 0 ? Loc.Get("timer.off") : Loc.Get("timer.seconds", "seconds", seconds);
    }

    public struct MatchSettings : INetworkSerializable, IEquatable<MatchSettings>
    {
        public int ScenarioId;
        public byte Nights;
        public MatchMode Mode;
        /// <summary>Take Turns: seconds per turn (0 = no timer).</summary>
        public ushort TurnSeconds;

        public static MatchSettings Default => new MatchSettings
        {
            ScenarioId = 0,
            Nights = GameRules.DefaultNights,
            Mode = MatchMode.Turns,
            TurnSeconds = 0,
        };

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ScenarioId);
            serializer.SerializeValue(ref Nights);
            serializer.SerializeValue(ref Mode);
            serializer.SerializeValue(ref TurnSeconds);
        }

        public bool Equals(MatchSettings other) => ScenarioId == other.ScenarioId && Nights == other.Nights && Mode == other.Mode && TurnSeconds == other.TurnSeconds;
        public override bool Equals(object obj) => obj is MatchSettings other && Equals(other);
        public override int GetHashCode() => (ScenarioId * 397) ^ (Nights * 31) ^ (int)Mode ^ (TurnSeconds << 8);
    }

    /// <summary>A lobby member plus a live summary of their season (what opponents see).</summary>
    public struct NetPlayer : INetworkSerializable, IEquatable<NetPlayer>
    {
        public ulong ClientId;
        public FixedString64Bytes Name;
        public byte ColorIndex;
        /// <summary>The player's streamer (StreamerType; the Random pick is resolved by the host when the match starts).</summary>
        public byte Streamer;
        public bool LobbyReady;
        public bool Connected;
        public bool ReadyNextNight;

        public byte Night;
        public byte Phase;
        public int Pop;
        public int Cash;
        public byte Capacity;
        public byte Guests;
        public byte DiamondsOwned;
        public byte DiamondsInHouse;
        public byte BestDiamonds;
        public byte Trouble;
        public int TotalPop;
        public byte WinNight;
        public byte Rank;
        public byte LastOutcome;
        /// <summary>How many other players are looking at this player's stream right now.</summary>
        public byte Watchers;

        public static NetPlayer Create(ulong clientId, string name, byte colorIndex, byte streamer = 0)
        {
            return new NetPlayer
            {
                ClientId = clientId,
                Name = NetStrings.Fs64(name),
                ColorIndex = colorIndex,
                Streamer = streamer,
                Connected = true,
                Capacity = GameRules.StartingCapacity,
            };
        }

        public GamePhase GamePhase => (GamePhase)Phase;
        public bool Won => GamePhase == GamePhase.Victory;
        public bool Finished => GamePhase == GamePhase.Victory || GamePhase == GamePhase.Defeat;
        public string DisplayName => Name.ToString();

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref ColorIndex);
            serializer.SerializeValue(ref Streamer);
            serializer.SerializeValue(ref LobbyReady);
            serializer.SerializeValue(ref Connected);
            serializer.SerializeValue(ref ReadyNextNight);
            serializer.SerializeValue(ref Night);
            serializer.SerializeValue(ref Phase);
            serializer.SerializeValue(ref Pop);
            serializer.SerializeValue(ref Cash);
            serializer.SerializeValue(ref Capacity);
            serializer.SerializeValue(ref Guests);
            serializer.SerializeValue(ref DiamondsOwned);
            serializer.SerializeValue(ref DiamondsInHouse);
            serializer.SerializeValue(ref BestDiamonds);
            serializer.SerializeValue(ref Trouble);
            serializer.SerializeValue(ref TotalPop);
            serializer.SerializeValue(ref WinNight);
            serializer.SerializeValue(ref Rank);
            serializer.SerializeValue(ref LastOutcome);
            serializer.SerializeValue(ref Watchers);
        }

        public bool Equals(NetPlayer o)
        {
            return ClientId == o.ClientId && Name.Equals(o.Name) && ColorIndex == o.ColorIndex && Streamer == o.Streamer && LobbyReady == o.LobbyReady &&
                   Connected == o.Connected && ReadyNextNight == o.ReadyNextNight && Night == o.Night && Phase == o.Phase &&
                   Pop == o.Pop && Cash == o.Cash && Capacity == o.Capacity && Guests == o.Guests && DiamondsOwned == o.DiamondsOwned &&
                   DiamondsInHouse == o.DiamondsInHouse && BestDiamonds == o.BestDiamonds && Trouble == o.Trouble && TotalPop == o.TotalPop &&
                   WinNight == o.WinNight && Rank == o.Rank && LastOutcome == o.LastOutcome && Watchers == o.Watchers;
        }

        public override bool Equals(object obj) => obj is NetPlayer other && Equals(other);
        public override int GetHashCode() => ClientId.GetHashCode();
    }

    /// <summary>A GameCommand on the wire.</summary>
    public struct NetCommand : INetworkSerializable
    {
        public byte Type;
        public int Actor;
        public int TargetA;
        public int TargetB;
        public int TargetC;
        public byte Option;
        public byte Guest;

        public static NetCommand From(GameCommand c) => new NetCommand
        {
            Type = (byte)c.Type,
            Actor = c.Actor,
            TargetA = c.TargetA,
            TargetB = c.TargetB,
            TargetC = c.TargetC,
            Option = (byte)c.Option,
            Guest = (byte)c.Guest,
        };

        public GameCommand ToCommand() => new GameCommand
        {
            Type = (CommandType)Type,
            Actor = Actor,
            TargetA = TargetA,
            TargetB = TargetB,
            TargetC = TargetC,
            Option = Option,
            Guest = (GuestType)Guest,
        };

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Type);
            serializer.SerializeValue(ref Actor);
            serializer.SerializeValue(ref TargetA);
            serializer.SerializeValue(ref TargetB);
            serializer.SerializeValue(ref TargetC);
            serializer.SerializeValue(ref Option);
            serializer.SerializeValue(ref Guest);
        }
    }

    /// <summary>A serialized PlayerState plus the events that produced it.</summary>
    public struct StatePacket : INetworkSerializable
    {
        public byte[] Data;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            if (serializer.IsWriter && Data == null) Data = Array.Empty<byte>();
            serializer.SerializeValue(ref Data);
        }
    }

    public sealed class ChatMessage
    {
        public string Sender;
        /// <summary>Player text as typed. For system messages: a localization key with arguments (see Core.Err), shown with Loc.Error.</summary>
        public string Text;
        public int ColorIndex;
        public bool IsSystem;

        public string DisplayText => IsSystem ? Loc.Error(Text) : Text;
    }

    /// <summary>What a player is pointing at or choosing right now (multiplayer presence; never touches the game state).</summary>
    public enum PresenceKind : byte
    {
        None = 0,
        /// <summary>A = viewer id (a card in chat, the Audience, a picker).</summary>
        HoverCard = 1,
        /// <summary>A = viewer type (a shop row).</summary>
        HoverShop = 2,
        /// <summary>A = <see cref="PresenceButton"/>.</summary>
        HoverButton = 3,
        /// <summary>A = acting viewer id; Picks = targets picked so far; B = hovered target id (-1 none).</summary>
        Targeting = 4,
        /// <summary>A = acting viewer id; B = hovered option (-1 none). A choice dialog is open (Pepe's mood...).</summary>
        Choosing = 5,
        /// <summary>A = acting viewer id (or -1 for New Attraction); B = hovered viewer id. A viewer picker is open.</summary>
        Picking = 6,
        /// <summary>The shop is open and nothing in particular is hovered.</summary>
        Browsing = 7,
        /// <summary>A rival is being chosen for a streamer power.</summary>
        ChoosingRival = 8,
    }

    public enum PresenceButton : byte
    {
        None = 0,
        AddViewer = 1,
        EndStream = 2,
        Approve = 3,
        Deny = 4,
        RaiseSubCap = 5,
        EndTurn = 6,
        Power = 7,
        Audience = 8,
    }

    public struct PresenceMessage : INetworkSerializable, IEquatable<PresenceMessage>
    {
        public PresenceKind Kind;
        public int A;
        public int B;
        public int Pick0;
        public int Pick1;
        public int Pick2;
        /// <summary>Pointer position in the board's normalized space (0-1) for the ghost cursor; negative when off the board.</summary>
        public short CursorX;
        public short CursorY;

        public static PresenceMessage None => new PresenceMessage { Kind = PresenceKind.None, A = -1, B = -1, Pick0 = -1, Pick1 = -1, Pick2 = -1, CursorX = -1, CursorY = -1 };

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Kind);
            serializer.SerializeValue(ref A);
            serializer.SerializeValue(ref B);
            serializer.SerializeValue(ref Pick0);
            serializer.SerializeValue(ref Pick1);
            serializer.SerializeValue(ref Pick2);
            serializer.SerializeValue(ref CursorX);
            serializer.SerializeValue(ref CursorY);
        }

        public bool SameIntent(PresenceMessage o) => Kind == o.Kind && A == o.A && B == o.B && Pick0 == o.Pick0 && Pick1 == o.Pick1 && Pick2 == o.Pick2;
        public bool Equals(PresenceMessage o) => SameIntent(o) && CursorX == o.CursorX && CursorY == o.CursorY;
        public override bool Equals(object obj) => obj is PresenceMessage other && Equals(other);
        public override int GetHashCode() => ((int)Kind * 397) ^ A ^ (B << 4);
    }

    /// <summary>Emotes players can send (they float up on every screen). Text-only so every font can draw them.</summary>
    public static class Emotes
    {
        public static readonly string[] All = { "POG", "KEKW", "LUL", "F", "W", "L", "o7", "<3", "monkaS", "Copium" };
        public const float Cooldown = 1.2f;
    }

    public static class NetStrings
    {
        public static FixedString64Bytes Fs64(string value)
        {
            var fs = new FixedString64Bytes();
            fs.CopyFromTruncated(value ?? string.Empty);
            return fs;
        }

        public static FixedString128Bytes Fs128(string value)
        {
            var fs = new FixedString128Bytes();
            fs.CopyFromTruncated(value ?? string.Empty);
            return fs;
        }
    }
}
