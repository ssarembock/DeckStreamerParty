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
        /// <summary>
        /// Free For All: everyone streams at the same time (nobody can watch anyone else), then the shop opens for everyone at
        /// once after a 3-2-1 countdown, first come first served. The next night starts when all players are ready. The only
        /// mode with more than two players.
        /// </summary>
        SyncNights = 0,
        /// <summary>Everyone plays at their own pace; the first 4-Diamond party wins instantly.</summary>
        Race = 1,
        /// <summary>Each night players throw their parties one after another while everyone else watches.</summary>
        Turns = 2,
    }

    /// <summary>Free For All: where the round is. Every stream runs at once, then the shared shop opens for everyone together.</summary>
    public enum RoundStage : byte
    {
        /// <summary>Players are streaming (or have finished and are waiting for the others).</summary>
        Streaming = 0,
        /// <summary>Everyone is ready: the 3-2-1 before the shop opens.</summary>
        Countdown = 1,
        /// <summary>The shop is open for everyone (first come, first served).</summary>
        Shopping = 2,
    }

    /// <summary>Lobby order and names of the match modes.</summary>
    public static class MatchModes
    {
        /// <summary>Take Turns and Race need exactly two players; with more, Free For All is the only mode.</summary>
        public const int MaxPlayersForAnyMode = 2;

        /// <summary>Seconds of the Free For All countdown before the shop opens.</summary>
        public const float ShopCountdownSeconds = 3f;

        public static readonly MatchMode[] Order = { MatchMode.Turns, MatchMode.SyncNights, MatchMode.Race };

        /// <summary>Whether a mode can be played with this many players.</summary>
        public static bool Allowed(MatchMode mode, int players) => players <= MaxPlayersForAnyMode || mode == MatchMode.SyncNights;

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

    /// <summary>Which countdown is running in a match: none, the stream timer or the shop timer.</summary>
    public enum MatchTimer : byte
    {
        None = 0,
        Stream = 1,
        Shop = 2,
    }

    /// <summary>Lobby choices for the shop timer (the stream timer uses <see cref="TurnTimers"/>): Off, 30, 60, 90 or 120 seconds.</summary>
    public static class ShopTimers
    {
        public static readonly int[] Presets = { 0, 30, 60, 90, 120 };

        public static string[] Labels
        {
            get
            {
                var labels = new string[Presets.Length];
                for (int i = 0; i < Presets.Length; i++)
                    labels[i] = Presets[i] <= 0 ? Loc.Get("timer.off") : Loc.Get("timer.seconds_short", "seconds", Presets[i]);
                return labels;
            }
        }

        public static int IndexOf(int seconds)
        {
            int index = Array.IndexOf(Presets, seconds);
            return index >= 0 ? index : 0;
        }

        public static string Describe(int seconds) => seconds <= 0 ? Loc.Get("timer.off") : Loc.Get("timer.shop_seconds", "seconds", seconds);
    }

    public struct MatchSettings : INetworkSerializable, IEquatable<MatchSettings>
    {
        public int ScenarioId;
        public byte Nights;
        public MatchMode Mode;
        /// <summary>The stream timer: seconds for one stream (Take Turns and Free For All; 0 = no timer).</summary>
        public ushort TurnSeconds;
        /// <summary>The shop timer: seconds for one visit to the shop (Take Turns and Free For All; 0 = no timer).</summary>
        public ushort ShopSeconds;

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
            serializer.SerializeValue(ref ShopSeconds);
        }

        public bool Equals(MatchSettings other) => ScenarioId == other.ScenarioId && Nights == other.Nights && Mode == other.Mode && TurnSeconds == other.TurnSeconds && ShopSeconds == other.ShopSeconds;
        public override bool Equals(object obj) => obj is MatchSettings other && Equals(other);
        public override int GetHashCode() => (ScenarioId * 397) ^ (Nights * 31) ^ (int)Mode ^ (TurnSeconds << 8) ^ (ShopSeconds << 20);

        /// <summary>Whether the stream and shop timers apply in this mode (not in Race, where everyone plays at their own pace).</summary>
        public bool TimersApply => Mode == MatchMode.Turns || Mode == MatchMode.SyncNights;
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
        /// <summary>Free For All: finished this night's stream and clicked Go to the Shop.</summary>
        public bool ReadyForShop;

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
        /// <summary>The player's online platform id (their SteamID) for avatars and profiles; 0 off-platform.</summary>
        public ulong PlatformUserId;

        public static NetPlayer Create(ulong clientId, string name, byte colorIndex, byte streamer = 0, ulong platformUserId = 0)
        {
            return new NetPlayer
            {
                ClientId = clientId,
                Name = NetStrings.Fs64(name),
                ColorIndex = colorIndex,
                Streamer = streamer,
                Connected = true,
                Capacity = GameRules.StartingCapacity,
                PlatformUserId = platformUserId,
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
            serializer.SerializeValue(ref ReadyForShop);
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
            serializer.SerializeValue(ref PlatformUserId);
        }

        public bool Equals(NetPlayer o)
        {
            return ClientId == o.ClientId && Name.Equals(o.Name) && ColorIndex == o.ColorIndex && Streamer == o.Streamer && LobbyReady == o.LobbyReady &&
                   Connected == o.Connected && ReadyNextNight == o.ReadyNextNight && ReadyForShop == o.ReadyForShop && Night == o.Night && Phase == o.Phase &&
                   Pop == o.Pop && Cash == o.Cash && Capacity == o.Capacity && Guests == o.Guests && DiamondsOwned == o.DiamondsOwned &&
                   DiamondsInHouse == o.DiamondsInHouse && BestDiamonds == o.BestDiamonds && Trouble == o.Trouble && TotalPop == o.TotalPop &&
                   WinNight == o.WinNight && Rank == o.Rank && LastOutcome == o.LastOutcome && Watchers == o.Watchers &&
                   PlatformUserId == o.PlatformUserId;
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

    /// <summary>
    /// Emotes players can send (they pop up from the sender's avatar on every screen). The sprites in the Game Database
    /// (Multiplayer Emotes) are the emotes; until some are assigned, these text emotes stand in. Every peer runs the same
    /// build, so an index means the same emote everywhere.
    /// </summary>
    public static class Emotes
    {
        public static readonly string[] All = { "POG", "KEKW", "LUL", "F", "W", "L", "o7", "<3", "monkaS", "Copium" };
        /// <summary>Shortest time between two emotes from one player.</summary>
        public const float Cooldown = 1.5f;
        /// <summary>Anti-spam: at most <see cref="BurstCount"/> emotes per player in any <see cref="BurstWindow"/> seconds.</summary>
        public const int BurstCount = 4;
        public const float BurstWindow = 12f;
        public const int MaxSprites = 16;

        /// <summary>
        /// Whether one more emote fits the limits, given the times (seconds) of the sender's recent emotes, which it trims.
        /// The host and the sending client both check, so spam never reaches anyone.
        /// </summary>
        public static bool Allowed(System.Collections.Generic.List<float> recent, float now)
        {
            recent.RemoveAll(t => now - t > BurstWindow);
            if (recent.Count > 0 && now - recent[recent.Count - 1] < Cooldown) return false;
            return recent.Count < BurstCount;
        }

        /// <summary>Seconds until the next emote is allowed (0 = now).</summary>
        public static float WaitFor(System.Collections.Generic.List<float> recent, float now)
        {
            recent.RemoveAll(t => now - t > BurstWindow);
            float wait = recent.Count > 0 ? Cooldown - (now - recent[recent.Count - 1]) : 0f;
            if (recent.Count >= BurstCount) wait = Math.Max(wait, BurstWindow - (now - recent[0]));
            return Math.Max(0f, wait);
        }

        /// <summary>The sprite emotes, or null while the Game Database has none (the text emotes are used then).</summary>
        public static System.Collections.Generic.List<UnityEngine.Sprite> Sprites
        {
            get
            {
                var list = GameDatabase.Instance.emotes;
                return list != null && list.Count > 0 ? list : null;
            }
        }

        public static int Count
        {
            get
            {
                var sprites = Sprites;
                return sprites != null ? Math.Min(sprites.Count, MaxSprites) : All.Length;
            }
        }

        /// <summary>The sprite for an emote (null for a text emote or an empty slot).</summary>
        public static UnityEngine.Sprite SpriteAt(int index)
        {
            var sprites = Sprites;
            return sprites != null && index >= 0 && index < sprites.Count ? sprites[index] : null;
        }

        public static string TextAt(int index) => index >= 0 && index < All.Length ? All[index] : "?";
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
