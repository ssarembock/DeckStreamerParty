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
        public static readonly string[] Labels = { "Take Turns", "Synchronized Nights", "Race (own pace)" };

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
                case MatchMode.Turns: return "take turns";
                case MatchMode.Race: return "race";
                default: return "synchronized nights";
            }
        }
    }

    public struct MatchSettings : INetworkSerializable, IEquatable<MatchSettings>
    {
        public int ScenarioId;
        public byte Nights;
        public MatchMode Mode;

        public static MatchSettings Default => new MatchSettings
        {
            ScenarioId = 0,
            Nights = GameRules.DefaultNights,
            Mode = MatchMode.Turns,
        };

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ScenarioId);
            serializer.SerializeValue(ref Nights);
            serializer.SerializeValue(ref Mode);
        }

        public bool Equals(MatchSettings other) => ScenarioId == other.ScenarioId && Nights == other.Nights && Mode == other.Mode;
        public override bool Equals(object obj) => obj is MatchSettings other && Equals(other);
        public override int GetHashCode() => (ScenarioId * 397) ^ (Nights * 31) ^ (int)Mode;
    }

    /// <summary>A lobby member plus a live summary of their season (what opponents see).</summary>
    public struct NetPlayer : INetworkSerializable, IEquatable<NetPlayer>
    {
        public ulong ClientId;
        public FixedString64Bytes Name;
        public byte ColorIndex;
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

        public static NetPlayer Create(ulong clientId, string name, byte colorIndex)
        {
            return new NetPlayer
            {
                ClientId = clientId,
                Name = NetStrings.Fs64(name),
                ColorIndex = colorIndex,
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
        }

        public bool Equals(NetPlayer o)
        {
            return ClientId == o.ClientId && Name.Equals(o.Name) && ColorIndex == o.ColorIndex && LobbyReady == o.LobbyReady &&
                   Connected == o.Connected && ReadyNextNight == o.ReadyNextNight && Night == o.Night && Phase == o.Phase &&
                   Pop == o.Pop && Cash == o.Cash && Capacity == o.Capacity && Guests == o.Guests && DiamondsOwned == o.DiamondsOwned &&
                   DiamondsInHouse == o.DiamondsInHouse && BestDiamonds == o.BestDiamonds && Trouble == o.Trouble && TotalPop == o.TotalPop &&
                   WinNight == o.WinNight && Rank == o.Rank && LastOutcome == o.LastOutcome;
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
        public string Text;
        public int ColorIndex;
        public bool IsSystem;
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
