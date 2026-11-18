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
        /// v4: audience classics (permanent Influence, Hate-Watcher mood, Softball Interview) and the shared shop.
        /// v5: rival Doomsdayers. v6: Bots in flight. v7: V5 communities (extra action uses, shop perks, Mods shield, Saboteurs)
        /// and each card's action use count. v8: Remove a Viewer purchases. v9: the match's player count (shared shop stack size).
        /// v10: what the new streamer actives armed for this stream (extra Sub Cap, Bark at Chat, Soft Landing).
        /// v11: who sits in a Hype Men spare seat (2026-11-16).
        /// v12: game modes and teams (Endless Trophies and purchases, team members, their Sub Caps, whose card is whose).
        /// v13: each team member's own Remove a Viewer price (2026-11-17).
        /// v14: Subathon and Double or Nothing armed for this stream (the two streamers added 2026-11-18).
        /// v15: the season's Trophy goal (3 in competitive multiplayer). v16: each team member's own streamer, active uses,
        /// cooldown and armed power (2026-11-18).
        /// </summary>
        const byte FormatVersion = 16;
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
                w.Write(s.Cash);
                w.Write(s.Influence);
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
                w.Write(s.PartyCash);
                w.Write(s.PartyInfluence);
                w.Write(s.InvestCash);
                w.Write(s.InvestInfluence);
                w.Write((byte)s.LastActionCategory);
                w.Write(s.LastRandomArrival);
                w.Write(s.AttractionActor);
                WriteIds(w, s.AttractionCandidates);

                w.Write((byte)s.LastOutcome);
                w.Write(s.LastCash);
                w.Write(s.LastInfluence);
                w.Write(s.LastDiamonds);
                w.Write(s.LastPenalty);
                w.Write(s.LastBlamedId);
                WriteIds(w, s.RaidSuspects);

                w.Write(s.BestDiamonds);
                w.Write(s.TotalCashEarned);
                w.Write(s.TotalInfluenceEarned);
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

                // v5: rival Doomsdayers
                w.Write((byte)Math.Min(255, Math.Max(0, s.IncomingDoom)));
                w.Write((byte)Math.Min(255, Math.Max(0, s.PartyDoom)));
                w.Write((byte)Math.Min(255, Math.Max(0, s.DoomOut)));

                // v6: Bots
                w.Write((byte)Math.Min(255, Math.Max(0, s.IncomingBots)));
                w.Write((byte)Math.Min(255, Math.Max(0, s.BotsOut)));

                // v7: V5 communities
                w.Write((byte)Math.Min(255, Math.Max(0, s.PartyExtraEvictions)));
                w.Write((byte)Math.Min(255, Math.Max(0, s.PartyExtraRecruits)));
                w.Write((byte)Math.Min(255, Math.Max(0, s.PartyExtraPeeks)));
                w.Write((byte)Math.Min(255, Math.Max(0, s.ExpansionDiscount)));
                w.Write((byte)Math.Min(255, Math.Max(0, s.RecruitDiscount)));
                w.Write((byte)Math.Min(255, Math.Max(0, s.IncomingSabotage)));
                w.Write((byte)Math.Min(255, Math.Max(0, s.SabotageOut)));
                w.Write(s.ModsShieldNext);
                for (int i = 0; i < s.Rolodex.Count; i++) w.Write((short)Math.Min(short.MaxValue, Math.Max(0, s.Rolodex[i].ActionUses)));

                // v8: Remove a Viewer
                w.Write((short)Math.Min(short.MaxValue, Math.Max(0, s.Removals)));

                // v9: players in the match (sizes the shared shop's stacks)
                w.Write((byte)Math.Min(255, Math.Max(1, s.MatchPlayers)));

                // v10: streamer actives that replaced the passives (Three-Hour Episode, Bark at Chat, Soft Landing)
                w.Write((byte)Math.Min(255, Math.Max(0, s.PartySeats)));
                w.Write((byte)Math.Min(255, Math.Max(0, s.PartyBark)));
                w.Write(s.PartySoftLanding);

                // v11: Hype Men spare seats (one flag per Audience card, same order as the Audience)
                for (int i = 0; i < s.Rolodex.Count; i++) w.Write(s.Rolodex[i].HypeSeat);

                // v12: game modes (Endless) and teams (2v2, co-op)
                w.Write((byte)s.Mode);
                w.Write(s.Trophies);
                w.Write((short)Math.Min(short.MaxValue, Math.Max(0, s.StreamsBought)));
                w.Write((short)Math.Min(short.MaxValue, Math.Max(0, s.ShopViewersBought)));
                w.Write((byte)Math.Min(255, Math.Max(1, s.Members)));
                w.Write(s.PartnerCapacity);
                w.Write(s.PartnerExpansions);
                w.Write((byte)Math.Min(255, Math.Max(0, s.StreamDone)));
                for (int i = 0; i < s.Rolodex.Count; i++) w.Write(s.Rolodex[i].Owner);

                // v13: the second team member's removals (their own Remove a Viewer price)
                w.Write((short)Math.Min(short.MaxValue, Math.Max(0, s.PartnerRemovals)));

                // v14: Kai Sonnet's Subathon and Train Wrex's Double or Nothing (armed for this stream)
                w.Write(s.PartySubathon);
                w.Write(s.PartyDoubleDown);

                // v15: the season's Trophy goal (3 in competitive multiplayer since 2026-11-18)
                w.Write((byte)Math.Min(255, Math.Max(1, s.TrophiesToWin)));

                // v16: the second team member's streamer and its active (teams play two streamers since 2026-11-18)
                w.Write((byte)s.PartnerStreamer);
                w.Write((short)Math.Min(short.MaxValue, Math.Max(0, s.PartnerPowerUses)));
                w.Write((byte)Math.Min(255, Math.Max(0, s.PartnerPowerCooldown)));
                w.Write(s.PartnerPowerArmed);

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
                s.Cash = r.ReadInt32();
                s.Influence = r.ReadInt32();
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
                s.PartyCash = r.ReadInt32();
                s.PartyInfluence = r.ReadInt32();
                s.InvestCash = r.ReadInt32();
                s.InvestInfluence = r.ReadInt32();
                s.LastActionCategory = (ActionCategory)r.ReadByte();
                s.LastRandomArrival = r.ReadInt32();
                s.AttractionActor = r.ReadInt32();
                ReadIds(r, s.AttractionCandidates);

                s.LastOutcome = (PartyOutcome)r.ReadByte();
                s.LastCash = r.ReadInt32();
                s.LastInfluence = r.ReadInt32();
                s.LastDiamonds = r.ReadInt32();
                s.LastPenalty = r.ReadInt32();
                s.LastBlamedId = r.ReadInt32();
                ReadIds(r, s.RaidSuspects);

                s.BestDiamonds = r.ReadInt32();
                s.TotalCashEarned = r.ReadInt32();
                s.TotalInfluenceEarned = r.ReadInt32();
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

                // v4 and older saves predate communities: no Doomsdayers in flight.
                if (version >= 5)
                {
                    s.IncomingDoom = r.ReadByte();
                    s.PartyDoom = r.ReadByte();
                    s.DoomOut = r.ReadByte();
                }

                // v5 and older saves predate the Bots community.
                if (version >= 6)
                {
                    s.IncomingBots = r.ReadByte();
                    s.BotsOut = r.ReadByte();
                }

                // v6 and older saves predate the V5 communities.
                if (version >= 7)
                {
                    s.PartyExtraEvictions = r.ReadByte();
                    s.PartyExtraRecruits = r.ReadByte();
                    s.PartyExtraPeeks = r.ReadByte();
                    s.ExpansionDiscount = r.ReadByte();
                    s.RecruitDiscount = r.ReadByte();
                    s.IncomingSabotage = r.ReadByte();
                    s.SabotageOut = r.ReadByte();
                    s.ModsShieldNext = r.ReadBoolean();
                    for (int i = 0; i < s.Rolodex.Count; i++) s.Rolodex[i].ActionUses = r.ReadInt16();
                }

                // v7 and older saves predate Remove a Viewer.
                if (version >= 8)
                {
                    s.Removals = r.ReadInt16();
                }

                // v8 and older saves are single-player seasons: 1 player (the old fixed stack of 4).
                if (version >= 9)
                {
                    s.MatchPlayers = Math.Max(1, (int)r.ReadByte());
                }

                // v9 and older saves predate the new streamer actives (nothing armed for this stream).
                if (version >= 10)
                {
                    s.PartySeats = r.ReadByte();
                    s.PartyBark = r.ReadByte();
                    s.PartySoftLanding = r.ReadBoolean();
                }

                // v10 and older saves predate the Hype Men spare seats (nobody sits in one).
                if (version >= 11)
                {
                    for (int i = 0; i < s.Rolodex.Count; i++) s.Rolodex[i].HypeSeat = r.ReadBoolean();
                }

                // v11 and older saves are classic solo seasons.
                if (version >= 12)
                {
                    s.Mode = (GameMode)r.ReadByte();
                    s.Trophies = r.ReadInt32();
                    s.StreamsBought = r.ReadInt16();
                    s.ShopViewersBought = r.ReadInt16();
                    s.Members = Math.Max(1, Math.Min(GameRules.TeamSize, (int)r.ReadByte()));
                    s.PartnerCapacity = r.ReadInt32();
                    s.PartnerExpansions = r.ReadInt32();
                    s.StreamDone = r.ReadByte();
                    for (int i = 0; i < s.Rolodex.Count; i++) s.Rolodex[i].Owner = r.ReadByte();
                }

                // v12 and older: the team shared one removal count (it stays with member 0).
                if (version >= 13) s.PartnerRemovals = r.ReadInt16();

                // v13 and older saves predate Subathon and Double or Nothing (nothing armed).
                if (version >= 14)
                {
                    s.PartySubathon = r.ReadBoolean();
                    s.PartyDoubleDown = r.ReadBoolean();
                }

                // v14 and older: every season won with 4 Trophies.
                s.TrophiesToWin = version >= 15 ? r.ReadByte() : GameRules.DiamondsToWin;

                // v15 and older: a team played one streamer (member 1 had none of their own).
                if (version >= 16)
                {
                    var partner = (StreamerType)r.ReadByte();
                    s.PartnerStreamer = StreamerCatalog.IsValid(partner) ? partner : StreamerType.None;
                    s.PartnerPowerUses = r.ReadInt16();
                    s.PartnerPowerCooldown = r.ReadByte();
                    s.PartnerPowerArmed = r.ReadBoolean();
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
            w.Write((short)g.BonusCash);
            w.Write((byte)g.TrolledCount);
            w.Write((byte)g.StrikeCount);
            w.Write(g.Temporary);

            w.Write((short)g.PartyCash);
            w.Write((short)g.PartyInfluence);
            w.Write((short)g.PartyTrouble);
            w.Write(g.TroubleCleared);
            w.Write(g.Disguise);
            w.Write(g.HasCopy);
            w.Write((short)g.CopyCash);
            w.Write((short)g.CopyInfluence);
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
                BonusCash = r.ReadInt16(),
                TrolledCount = r.ReadByte(),
                StrikeCount = r.ReadByte(),
                Temporary = r.ReadBoolean(),

                PartyCash = r.ReadInt16(),
                PartyInfluence = r.ReadInt16(),
                PartyTrouble = r.ReadInt16(),
                TroubleCleared = r.ReadBoolean(),
                Disguise = r.ReadByte(),
                HasCopy = r.ReadBoolean(),
                CopyCash = r.ReadInt16(),
                CopyInfluence = r.ReadInt16(),
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
            w.Write((short)g.BonusInfluence);
            w.Write(g.Wolf);
            w.Write(g.CashMuted);
        }

        static void ReadGuestV4(BinaryReader r, GuestInstance g)
        {
            g.BonusInfluence = r.ReadInt16();
            g.Wolf = r.ReadBoolean();
            g.CashMuted = r.ReadBoolean();
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
