using System;
using System.Collections.Generic;
using System.IO;

namespace DeckParty.Core
{
    /// <summary>
    /// Compact binary format for PlayerState (+ optional event list). Used for save files and for the
    /// host's per-player snapshots.
    /// </summary>
    public static class StateSerializer
    {
        /// <summary>
        /// v2: meme roster, per-guest party modifiers, ordered waiting line. v3: streamers and their powers.
        /// v4: audience classics (permanent Cash, Hate-Watcher mood, Softball Interview) and the shared shop.
        /// </summary>
        const byte FormatVersion = 4;
        const byte OldestReadableVersion = 2;
        const int MaxListLength = 4096;

        public static byte[] Serialize(PlayerState s, IReadOnlyList<GameEvent> events = null)
        {
            using (var stream = new MemoryStream(1024))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(FormatVersion);

                w.Write(s.ScenarioId);
                w.Write(s.Seed);
                w.Write(s.TotalNights);
                w.Write((ushort)s.Shop.Count);
                for (int i = 0; i < s.Shop.Count; i++) w.Write((byte)s.Shop[i]);

                w.Write(s.Night);
                w.Write(s.Pop);
                w.Write(s.Cash);
                w.Write(s.Capacity);
                w.Write(s.Expansions);

                w.Write((ushort)s.Rolodex.Count);
                for (int i = 0; i < s.Rolodex.Count; i++) WriteGuest(w, s.Rolodex[i]);
                w.Write(s.NextInstanceId);
                w.Write(s.BannedNextParty);
                w.Write(s.BannedThisParty);

                w.Write((byte)s.Phase);
                WriteIds(w, s.House);
                WriteIds(w, s.Pool);
                WriteIds(w, s.Excluded);
                WriteIds(w, s.UsedAbilities);
                w.Write(s.PeekedId);
                w.Write(s.PartyPop);
                w.Write(s.PartyCash);
                w.Write(s.InvestPop);
                w.Write(s.InvestCash);
                w.Write((byte)s.LastActionCategory);
                w.Write(s.LastRandomArrival);
                w.Write(s.AttractionActor);
                WriteIds(w, s.AttractionCandidates);

                w.Write((byte)s.LastOutcome);
                w.Write(s.LastPop);
                w.Write(s.LastCash);
                w.Write(s.LastDiamonds);
                w.Write(s.LastPenalty);
                w.Write(s.LastBlamedId);
                WriteIds(w, s.RaidSuspects);

                w.Write(s.BestDiamonds);
                w.Write(s.TotalPopEarned);
                w.Write(s.TotalCashEarned);
                w.Write(s.SuccessfulParties);
                w.Write(s.Raids);
                w.Write(s.FireMarshals);
                w.Write(s.WinNight);
                w.Write(s.RngState);

                w.Write((byte)s.Streamer);
                w.Write((short)s.PowerUses);
                w.Write((byte)s.PowerCooldown);
                w.Write(s.PowerArmed);
                w.Write((byte)s.IncomingHeat);
                w.Write((short)s.Streak);
                w.Write((byte)s.PartyHeat);
                w.Write((byte)s.PartyShield);
                w.Write((byte)s.PartyViewerBonus);
                w.Write((byte)s.PartyFreeActions);
                w.Write(s.PartyXRay);

                // v4
                w.Write(s.PartyInterviewUsed);
                w.Write((ushort)s.RivalTaken.Length);
                for (int i = 0; i < s.RivalTaken.Length; i++) w.Write((byte)Math.Min(255, Math.Max(0, s.RivalTaken[i])));
                for (int i = 0; i < s.Rolodex.Count; i++) WriteGuestV4(w, s.Rolodex[i]);

                int eventCount = events != null ? events.Count : 0;
                w.Write((ushort)eventCount);
                for (int i = 0; i < eventCount; i++)
                {
                    var e = events[i];
                    w.Write((byte)e.Type);
                    w.Write(e.GuestId);
                    w.Write((byte)e.Guest);
                    w.Write(e.A);
                    w.Write(e.B);
                    w.Write(e.C);
                }

                w.Flush();
                return stream.ToArray();
            }
        }

        public static PlayerState Deserialize(byte[] data, List<GameEvent> eventsOut = null)
        {
            if (data == null || data.Length == 0) throw new ArgumentException("Empty state data.");

            using (var stream = new MemoryStream(data, false))
            using (var r = new BinaryReader(stream))
            {
                byte version = r.ReadByte();
                if (version < OldestReadableVersion || version > FormatVersion)
                    throw new InvalidDataException($"Unsupported state format v{version} (expected v{OldestReadableVersion}-v{FormatVersion}).");

                var s = new PlayerState();
                s.ScenarioId = r.ReadInt32();
                s.Seed = r.ReadUInt32();
                s.TotalNights = r.ReadInt32();
                int shopCount = ReadCount(r);
                for (int i = 0; i < shopCount; i++) s.Shop.Add(ReadType(r));

                s.Night = r.ReadInt32();
                s.Pop = r.ReadInt32();
                s.Cash = r.ReadInt32();
                s.Capacity = r.ReadInt32();
                s.Expansions = r.ReadInt32();

                int rolodexCount = ReadCount(r);
                for (int i = 0; i < rolodexCount; i++) s.Rolodex.Add(ReadGuest(r));
                s.NextInstanceId = r.ReadInt32();
                s.BannedNextParty = r.ReadInt32();
                s.BannedThisParty = r.ReadInt32();

                s.Phase = (GamePhase)r.ReadByte();
                ReadIds(r, s.House);
                ReadIds(r, s.Pool);
                ReadIds(r, s.Excluded);
                ReadIds(r, s.UsedAbilities);
                s.PeekedId = r.ReadInt32();
                s.PartyPop = r.ReadInt32();
                s.PartyCash = r.ReadInt32();
                s.InvestPop = r.ReadInt32();
                s.InvestCash = r.ReadInt32();
                s.LastActionCategory = (ActionCategory)r.ReadByte();
                s.LastRandomArrival = r.ReadInt32();
                s.AttractionActor = r.ReadInt32();
                ReadIds(r, s.AttractionCandidates);

                s.LastOutcome = (PartyOutcome)r.ReadByte();
                s.LastPop = r.ReadInt32();
                s.LastCash = r.ReadInt32();
                s.LastDiamonds = r.ReadInt32();
                s.LastPenalty = r.ReadInt32();
                s.LastBlamedId = r.ReadInt32();
                ReadIds(r, s.RaidSuspects);

                s.BestDiamonds = r.ReadInt32();
                s.TotalPopEarned = r.ReadInt32();
                s.TotalCashEarned = r.ReadInt32();
                s.SuccessfulParties = r.ReadInt32();
                s.Raids = r.ReadInt32();
                s.FireMarshals = r.ReadInt32();
                s.WinNight = r.ReadInt32();
                s.RngState = r.ReadUInt64();

                // v2 saves predate streamers: they load as "no streamer".
                if (version >= 3)
                {
                    var streamer = (StreamerType)r.ReadByte();
                    s.Streamer = StreamerCatalog.IsValid(streamer) ? streamer : StreamerType.None;
                    s.PowerUses = r.ReadInt16();
                    s.PowerCooldown = r.ReadByte();
                    s.PowerArmed = r.ReadBoolean();
                    s.IncomingHeat = r.ReadByte();
                    s.Streak = r.ReadInt16();
                    s.PartyHeat = r.ReadByte();
                    s.PartyShield = r.ReadByte();
                    s.PartyViewerBonus = r.ReadByte();
                    s.PartyFreeActions = r.ReadByte();
                    s.PartyXRay = r.ReadBoolean();
                }

                // v3 and older saves predate the audience classics and the shared shop: everything starts at zero.
                if (version >= 4)
                {
                    s.PartyInterviewUsed = r.ReadBoolean();
                    int types = ReadCount(r);
                    for (int i = 0; i < types; i++)
                    {
                        int taken = r.ReadByte();
                        if (i < s.RivalTaken.Length) s.RivalTaken[i] = taken;
                    }
                    for (int i = 0; i < s.Rolodex.Count; i++) ReadGuestV4(r, s.Rolodex[i]);
                }

                int eventCount = ReadCount(r);
                for (int i = 0; i < eventCount; i++)
                {
                    var e = new GameEvent
                    {
                        Type = (GameEventType)r.ReadByte(),
                        GuestId = r.ReadInt32(),
                        Guest = (GuestType)r.ReadByte(),
                        A = r.ReadInt32(),
                        B = r.ReadInt32(),
                        C = r.ReadInt32(),
                    };
                    eventsOut?.Add(e);
                }

                return s;
            }
        }

        static void WriteGuest(BinaryWriter w, GuestInstance g)
        {
            w.Write(g.Id);
            w.Write((byte)g.Type);
            w.Write((short)g.BonusPop);
            w.Write((byte)g.TrolledCount);
            w.Write((byte)g.StrikeCount);
            w.Write(g.Temporary);

            w.Write((short)g.PartyPop);
            w.Write((short)g.PartyCash);
            w.Write((short)g.PartyTrouble);
            w.Write(g.TroubleCleared);
            w.Write(g.Disguise);
            w.Write(g.HasCopy);
            w.Write((short)g.CopyPop);
            w.Write((short)g.CopyCash);
            w.Write(g.HasOverride);
            w.Write((short)g.PrintedOverride);
            w.Write(g.Infected);
            w.Write(g.Zeroed);
            w.Write(g.DoubleMode);
            w.Write(g.ConvertMode);
            w.Write(g.Immune);
            w.Write(g.AttachedTo);
            w.Write((byte)g.Mood);
            w.Write((short)g.Counter);
            w.Write((short)g.Gained);
            w.Write(g.ShieldUsed);
        }

        static GuestInstance ReadGuest(BinaryReader r)
        {
            var g = new GuestInstance
            {
                Id = r.ReadInt32(),
                Type = ReadType(r),
                BonusPop = r.ReadInt16(),
                TrolledCount = r.ReadByte(),
                StrikeCount = r.ReadByte(),
                Temporary = r.ReadBoolean(),

                PartyPop = r.ReadInt16(),
                PartyCash = r.ReadInt16(),
                PartyTrouble = r.ReadInt16(),
                TroubleCleared = r.ReadBoolean(),
                Disguise = r.ReadByte(),
                HasCopy = r.ReadBoolean(),
                CopyPop = r.ReadInt16(),
                CopyCash = r.ReadInt16(),
                HasOverride = r.ReadBoolean(),
                PrintedOverride = r.ReadInt16(),
                Infected = r.ReadBoolean(),
                Zeroed = r.ReadBoolean(),
                DoubleMode = r.ReadByte(),
                ConvertMode = r.ReadByte(),
                Immune = r.ReadBoolean(),
                AttachedTo = r.ReadInt32(),
                Mood = (PepeMood)r.ReadByte(),
                Counter = r.ReadInt16(),
                Gained = r.ReadInt16(),
                ShieldUsed = r.ReadBoolean(),
            };
            if (g.Disguise != GuestInstance.NoDisguise && !GuestCatalog.IsValid((GuestType)g.Disguise))
                throw new InvalidDataException($"Unknown disguise {g.Disguise} in state data.");
            return g;
        }

        /// <summary>Per-guest fields added in v4, written after the v3 block (same guest order as the Audience).</summary>
        static void WriteGuestV4(BinaryWriter w, GuestInstance g)
        {
            w.Write((short)g.BonusCash);
            w.Write(g.Wolf);
            w.Write(g.SubsMuted);
        }

        static void ReadGuestV4(BinaryReader r, GuestInstance g)
        {
            g.BonusCash = r.ReadInt16();
            g.Wolf = r.ReadBoolean();
            g.SubsMuted = r.ReadBoolean();
        }

        static GuestType ReadType(BinaryReader r)
        {
            var type = (GuestType)r.ReadByte();
            if (!GuestCatalog.IsValid(type)) throw new InvalidDataException($"Unknown guest type {(int)type} in state data.");
            return type;
        }

        static void WriteIds(BinaryWriter w, List<int> ids)
        {
            w.Write((ushort)ids.Count);
            for (int i = 0; i < ids.Count; i++) w.Write(ids[i]);
        }

        static void ReadIds(BinaryReader r, List<int> into)
        {
            int count = ReadCount(r);
            for (int i = 0; i < count; i++) into.Add(r.ReadInt32());
        }

        static int ReadCount(BinaryReader r)
        {
            int count = r.ReadUInt16();
            if (count > MaxListLength) throw new InvalidDataException("Corrupt state data (list too long).");
            return count;
        }
    }
}
