using System;
using System.Collections.Generic;

namespace DeckParty.Core
{
    public enum GamePhase : byte
    {
        Party = 0,      // Doors are open.
        RaidBlame = 1,  // The stream got deplatformed; choose who gets the Temporary Ban.
        Planning = 2,   // Spend Gifted Subs and Cash, then start the next night.
        Victory = 3,
        Defeat = 4,
    }

    public enum PartyOutcome : byte
    {
        None = 0,
        Success = 1,
        /// <summary>Deplatformed: too much Deplatform Risk.</summary>
        Raid = 2,
        /// <summary>Server Down: chat went over the Sub Cap.</summary>
        FireMarshal = 3,
    }

    public enum PepeMood : byte
    {
        None = 0,
        Happy = 1,
        Sad = 2,
        Angry = 3,
    }

    /// <summary>One specific guest card in a Squad. Permanent upgrades and tonight's modifiers live here.</summary>
    public sealed class GuestInstance
    {
        public const byte NoDisguise = 255;
        public const int AttachPending = -2;

        public int Id;
        public GuestType Type;

        // Permanent
        public int BonusPop;        // Permanent Subs change (Chad vs Virgin, Tung Tung, Bad Luck Brian).
        public int BonusCash;       // Permanent Cash change (Affiliate's Checkout Code, Gen Z with Job's entrances).
        public int TrolledCount;    // Times Trollface has trolled this guest.
        public int StrikeCount;     // Tung Tung Tung Sahur strikes so far (0-2).
        public bool Temporary;      // Ugandan Knuckles copy or a rival's Bot: vanishes when the party ends.
        public bool Wolf;           // Hate-Watcher: hate-watching (risky) since their last entrance. Flips on every entrance.
        public int ActionUses;      // Times this card's action has been used, ever (Junior PR Manager sends one more viewer each time).

        // Tonight only (cleared when the party ends)
        public int PartyPop;               // Subs this guest gained tonight.
        public int PartyCash;              // Cash this guest gained tonight.
        public int PartyTrouble;           // Extra Deplatform Risk added tonight.
        public bool TroubleCleared;        // Printed Deplatform Risk removed for the rest of the party.
        public bool SubsMuted;             // Billionaire Benefactor's Softball Interview: earns 0 Subs tonight.
        public byte Disguise = NoDisguise; // Rickroll: the character this guest is pretending to be.
        public bool HasCopy;               // NPC Wojak copied stats.
        public int CopyPop;
        public int CopyCash;
        public bool HasOverride;           // Ballerina Cappuccina exchanged printed Subs.
        public int PrintedOverride;
        public bool Infected;              // Skibidi Toilet.
        public bool Zeroed;                // Drakeposting: earns nothing tonight.
        public byte DoubleMode;            // Drakeposting: 1 doubles printed Subs, 2 doubles printed Cash.
        public byte ConvertMode;           // Confused Math Lady: 1 Subs to Cash, 2 Cash to Subs.
        public bool Immune;                // Roll Safe.
        public int AttachedTo = -1;        // Overly Attached Girlfriend's partner (or AttachPending).
        public PepeMood Mood;
        public int Counter;                // Nyan chain / Bongo beat / Pop Cat action count.
        public int Gained;                 // Dancing Baby combo Subs so far.
        public bool ShieldUsed;            // Bad Luck Brian's once-per-party shield.

        /// <summary>The character this guest acts as tonight (differs from <see cref="Type"/> while Rickrolled).</summary>
        public GuestType Kind => Disguise != NoDisguise ? (GuestType)Disguise : Type;

        /// <summary>Rules for how this guest behaves right now (follows a Rickroll disguise).</summary>
        public GuestInfo Info => GuestCatalog.Get(Kind);

        /// <summary>The card's real identity, used for ownership, shop limits and refunds.</summary>
        public GuestInfo BaseInfo => GuestCatalog.Get(Type);

        public bool IsDisguised => Disguise != NoDisguise;

        public void ResetParty()
        {
            PartyPop = 0;
            PartyCash = 0;
            PartyTrouble = 0;
            TroubleCleared = false;
            SubsMuted = false;
            Disguise = NoDisguise;
            HasCopy = false;
            CopyPop = 0;
            CopyCash = 0;
            HasOverride = false;
            PrintedOverride = 0;
            Infected = false;
            Zeroed = false;
            DoubleMode = 0;
            ConvertMode = 0;
            Immune = false;
            AttachedTo = -1;
            Mood = PepeMood.None;
            Counter = 0;
            Gained = 0;
            ShieldUsed = false;
        }
    }

    /// <summary>
    /// Complete state of one player's season. Everything needed to resume (including RNG) is here;
    /// the PartyEngine is the only thing that mutates it.
    /// </summary>
    public sealed class PlayerState
    {
        // Setup
        public int ScenarioId;
        public uint Seed;
        public int TotalNights = GameRules.DefaultNights;
        public readonly List<GuestType> Shop = new List<GuestType>();

        // Economy
        public int Night;
        public int Pop;
        public int Cash;
        public int Capacity = GameRules.StartingCapacity;
        public int Expansions;
        /// <summary>Viewers removed for good with Remove a Viewer (each one makes the next cost 1 Cash more).</summary>
        public int Removals;

        // Collection
        public readonly List<GuestInstance> Rolodex = new List<GuestInstance>();
        public int NextInstanceId = 1;
        public int BannedNextParty = -1;
        public int BannedThisParty = -1;

        // Current party
        public GamePhase Phase;
        public readonly List<int> House = new List<int>();
        /// <summary>The waiting line, in arrival order: the door always admits Pool[0].</summary>
        public readonly List<int> Pool = new List<int>();
        public readonly List<int> Excluded = new List<int>();
        public readonly List<int> UsedAbilities = new List<int>();
        public int PeekedId = -1;
        public int PartyPop;
        public int PartyCash;
        public int InvestPop;
        public int InvestCash;
        public ActionCategory LastActionCategory;
        public int LastRandomArrival = -1;
        public int AttractionActor = -1;
        public readonly List<int> AttractionCandidates = new List<int>();
        /// <summary>A Billionaire Benefactor already gave the Softball Interview this stream (once per stream, for all of them).</summary>
        public bool PartyInterviewUsed;

        // Shared shop (multiplayer)
        /// <summary>
        /// Per guest type: copies the other players in the match have taken from the shared shop (their copies beyond the
        /// starting audience). Maintained by the host and sent with every snapshot; always zero in single player.
        /// </summary>
        public readonly int[] RivalTaken = new int[GuestCatalog.Count];

        // Last party summary
        public PartyOutcome LastOutcome;
        public int LastPop;
        public int LastCash;
        public int LastDiamonds;
        public int LastPenalty;
        public int LastBlamedId = -1;
        public readonly List<int> RaidSuspects = new List<int>();

        // Streamer (the character you play as) and their power
        public StreamerType Streamer;
        public int PowerUses;
        /// <summary>Nights until the power can be used again (counts down when a stream starts).</summary>
        public int PowerCooldown;
        /// <summary>A next-stream power bought in the shop, waiting for the next stream to start.</summary>
        public bool PowerArmed;
        /// <summary>Deplatform Risk rivals' raids will add to your next stream.</summary>
        public int IncomingHeat;
        /// <summary>Successful streams in a row (Manim Maketh Man's bonus).</summary>
        public int Streak;

        // Communities (multiplayer): rival Doomsdayers
        /// <summary>Doomsdayers points a rival's stream aimed at you, applied when your next stream starts.</summary>
        public int IncomingDoom;
        /// <summary>This stream's rival Doomsdayers points: each viewer may score nothing (<see cref="Communities.DoomChance"/>).</summary>
        public int PartyDoom;
        /// <summary>Doomsdayers points your last successful stream earned, waiting for the host to pass them to your rivals.</summary>
        public int DoomOut;

        // Communities (multiplayer): Bots
        /// <summary>Bots rivals sent you: they join your Audience when your next stream starts and leave when it ends.</summary>
        public int IncomingBots;
        /// <summary>Bots your last scored stream sent, waiting for the host to pass them to the next player.</summary>
        public int BotsOut;

        // Communities (V5)
        /// <summary>Gatekeepers: extra eviction-action uses already taken this stream.</summary>
        public int PartyExtraEvictions;
        /// <summary>Marketers: extra recruit-action uses already taken this stream.</summary>
        public int PartyExtraRecruits;
        /// <summary>Voyeurs: extra peek-action uses already taken this stream.</summary>
        public int PartyExtraPeeks;
        /// <summary>Boomers: Cash off every Sub Cap upgrade in this shop (set when a stream is scored, gone when the next starts).</summary>
        public int ExpansionDiscount;
        /// <summary>Shop Control: Clout off your first recruit in this shop (set when a stream is scored, gone when the next starts).</summary>
        public int RecruitDiscount;
        /// <summary>Rival Saboteurs: Clout added to your next recruit (cleared by that purchase).</summary>
        public int IncomingSabotage;
        /// <summary>Saboteurs points your last scored stream earned, waiting for the host to pass them to your rivals.</summary>
        public int SabotageOut;
        /// <summary>Mods: your next stream starts with 1 Deplatform Risk cancelled.</summary>
        public bool ModsShieldNext;

        // Current stream: streamer power effects
        public int PartyHeat;          // Deplatform Risk from rivals' raids.
        public int PartyShield;        // Deplatform Risk ignored (Smoke Bomb).
        public int PartyViewerBonus;   // Extra Subs per viewer (Giveaway).
        public int PartyFreeActions;   // Viewer actions that refresh after use (Double Move).
        public bool PartyXRay;         // The next viewers in the queue are visible (X-Ray Vision).

        // Stats
        public int BestDiamonds;
        public int TotalPopEarned;
        public int TotalCashEarned;
        public int SuccessfulParties;
        public int Raids;
        public int FireMarshals;
        public int WinNight;

        // Randomness
        public ulong RngState;

        /// <summary>House spaces in use (Big Chungus takes 3).</summary>
        public int Occupancy
        {
            get
            {
                int total = 0;
                for (int i = 0; i < House.Count; i++)
                {
                    var g = Find(House[i]);
                    total += g != null ? g.Info.Size : 1;
                }
                return total;
            }
        }

        public bool HouseFull => Occupancy >= Capacity;
        public int EmptySpaces => Math.Max(0, Capacity - Occupancy);
        public bool IsGameOver => Phase == GamePhase.Victory || Phase == GamePhase.Defeat;
        public bool IsFinalNight => Night >= TotalNights;
        public int NightsRemaining => Math.Max(0, TotalNights - Night);
        public bool AttractionPending => AttractionActor >= 0;

        /// <summary>True while a peek or a New Attraction choice must be answered before anything else.</summary>
        public bool DecisionPending => PeekedId >= 0 || AttractionActor >= 0;

        public GuestInstance Find(int id)
        {
            for (int i = 0; i < Rolodex.Count; i++)
                if (Rolodex[i].Id == id) return Rolodex[i];
            return null;
        }

        /// <summary>Guests at the party acting as <paramref name="kind"/> (Rickroll disguises count).</summary>
        public int CountInHouse(GuestType kind)
        {
            int count = 0;
            for (int i = 0; i < House.Count; i++)
            {
                var g = Find(House[i]);
                if (g != null && g.Kind == kind) count++;
            }
            return count;
        }

        /// <summary>Permanent cards of this type (temporary copies don't count toward shop limits).</summary>
        public int CountOwned(GuestType type)
        {
            int count = 0;
            for (int i = 0; i < Rolodex.Count; i++)
                if (Rolodex[i].Type == type && !Rolodex[i].Temporary) count++;
            return count;
        }

        public int PermanentCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Rolodex.Count; i++)
                    if (!Rolodex[i].Temporary) count++;
                return count;
            }
        }

        public int DiamondsOwned()
        {
            int count = 0;
            for (int i = 0; i < Rolodex.Count; i++)
                if (!Rolodex[i].Temporary && Rolodex[i].BaseInfo.IsDiamond) count++;
            return count;
        }

        public StreamerInfo StreamerInfo => StreamerCatalog.Get(Streamer);

        public PlayerState Clone() => StateSerializer.Deserialize(StateSerializer.Serialize(this));
    }
}
