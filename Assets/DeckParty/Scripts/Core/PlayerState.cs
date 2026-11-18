using System;
using System.Collections.Generic;

namespace DeckParty.Core
{
    public enum GamePhase : byte
    {
        Party = 0,      // Doors are open.
        RaidBlame = 1,  // The stream got deplatformed; choose who gets the Temporary Ban.
        Planning = 2,   // Spend Cash and Influence, then start the next night.
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

    /// <summary>
    /// The rules a season is played with (2026-11-16, additionalgamemodes.md). Saved and sent over the network: only append.
    /// </summary>
    public enum GameMode : byte
    {
        /// <summary>The usual rules: 4 Trophy viewers in one scored stream wins.</summary>
        Classic = 0,
        /// <summary>
        /// Endless: no win. Trophy viewers in a scored stream bank Trophies, which buy extra streams and extra viewers for the
        /// shop. The run lasts as long as the streams do.
        /// </summary>
        Endless = 1,
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
        public int BonusCash;        // Permanent Cash change (Chad vs Virgin, Tung Tung, Bad Luck Brian).
        public int BonusInfluence;       // Permanent Influence change (Affiliate's Checkout Code, Gen Z with Job's entrances).
        public int TrolledCount;    // Times Trollface has trolled this guest.
        public int StrikeCount;     // Tung Tung Tung Sahur strikes so far (0-2).
        public bool Temporary;      // Ugandan Knuckles copy or a rival's Bot: vanishes when the party ends.
        public bool Wolf;           // Hate-Watcher: hate-watching (risky) since their last entrance. Flips on every entrance.
        public int ActionUses;      // Times this card's action has been used, ever (Junior PR Manager sends one more viewer each time).
        /// <summary>Teams (2v2, co-op): which team member's Audience this card is in (0 or 1). Always 0 for a solo season.</summary>
        public byte Owner;

        // Tonight only (cleared when the party ends)
        public int PartyCash;               // Cash this guest gained tonight.
        public int PartyInfluence;              // Influence this guest gained tonight.
        public int PartyTrouble;           // Extra Deplatform Risk added tonight.
        public bool TroubleCleared;        // Printed Deplatform Risk removed for the rest of the party.
        public bool CashMuted;             // Billionaire Benefactor's Softball Interview: earns 0 Cash tonight.
        public byte Disguise = NoDisguise; // Rickroll: the character this guest is pretending to be.
        public bool HasCopy;               // NPC Wojak copied stats.
        public int CopyCash;
        public int CopyInfluence;
        public bool HasOverride;           // Ballerina Cappuccina exchanged printed Cash.
        public int PrintedOverride;
        public bool Infected;              // Skibidi Toilet.
        public bool Zeroed;                // Drakeposting: earns nothing tonight.
        public byte DoubleMode;            // Drakeposting: 1 doubles printed Cash, 2 doubles printed Influence.
        public byte ConvertMode;           // Confused Math Lady: 1 Cash to Influence, 2 Influence to Cash.
        public bool Immune;                // Roll Safe.
        public int AttachedTo = -1;        // Overly Attached Girlfriend's partner (or AttachPending).
        public PepeMood Mood;
        public int Counter;                // Nyan chain / Bongo beat / Pop Cat action count.
        public int Gained;                 // Dancing Baby combo Cash so far.
        public bool ShieldUsed;            // Bad Luck Brian's once-per-party shield.
        public bool HypeSeat;              // Hype Men: sits in a spare seat past the Sub Cap (a Hype Man brought them in).

        /// <summary>The character this guest acts as tonight (differs from <see cref="Type"/> while Rickrolled).</summary>
        public GuestType Kind => Disguise != NoDisguise ? (GuestType)Disguise : Type;

        /// <summary>Rules for how this guest behaves right now (follows a Rickroll disguise).</summary>
        public GuestInfo Info => GuestCatalog.Get(Kind);

        /// <summary>The card's real identity, used for ownership, shop limits and refunds.</summary>
        public GuestInfo BaseInfo => GuestCatalog.Get(Type);

        public bool IsDisguised => Disguise != NoDisguise;

        public void ResetParty()
        {
            PartyCash = 0;
            PartyInfluence = 0;
            PartyTrouble = 0;
            TroubleCleared = false;
            CashMuted = false;
            Disguise = NoDisguise;
            HasCopy = false;
            CopyCash = 0;
            CopyInfluence = 0;
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
            HypeSeat = false;
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
        /// <summary>
        /// Trophies needed in one scored stream to win this season: 4 in single player, 3 in competitive multiplayer (online
        /// and couch, 2026-11-18). Read it through <see cref="PartyEngine.TrophiesToWin"/>.
        /// </summary>
        public int TrophiesToWin = GameRules.DiamondsToWin;
        public readonly List<GuestType> Shop = new List<GuestType>();

        // Economy
        public int Night;
        public int Cash;
        public int Influence;
        public int Capacity = GameRules.StartingCapacity;
        public int Expansions;
        /// <summary>Viewers removed for good with Remove a Viewer (each one makes the next cost 1 Influence more).</summary>
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
        public int PartyCash;
        public int PartyInfluence;
        public int InvestCash;
        public int InvestInfluence;
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

        /// <summary>
        /// Per guest type: permanent cards that left this season's Audience for good (Remove a Viewer, a retired Boomer, the
        /// This Is Fine Dog). They are out of the game: they still count as taken from the shop, so they never go back to its
        /// stock (2026-11-18; state format v17).
        /// </summary>
        public readonly int[] GoneForGood = new int[GuestCatalog.Count];

        /// <summary>
        /// Players in the match (1 in single player). Sets the size of the shared shop's stacks (<see cref="GameRules.SharedStock"/>);
        /// the host sets it when the match starts.
        /// </summary>
        public int MatchPlayers = 1;

        // Last party summary
        public PartyOutcome LastOutcome;
        public int LastCash;
        public int LastInfluence;
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
        /// <summary>Mods (the Gatekeepers until 2026-11-16): extra eviction-action uses already taken this stream.</summary>
        public int PartyExtraEvictions;
        /// <summary>Marketers: extra recruit-action uses already taken this stream.</summary>
        public int PartyExtraRecruits;
        /// <summary>Voyeurs: extra peek-action uses already taken this stream.</summary>
        public int PartyExtraPeeks;
        /// <summary>
        /// Boomers: Influence off every Sub Cap upgrade and every use of the streamer's active in this shop (set when a stream
        /// is scored, gone when the next starts).
        /// </summary>
        public int ExpansionDiscount;
        /// <summary>Shop Control: Cash off your first recruit in this shop (set when a stream is scored, gone when the next starts).</summary>
        public int RecruitDiscount;
        /// <summary>Rival Saboteurs: Cash added to your next recruit (cleared by that purchase).</summary>
        public int IncomingSabotage;
        /// <summary>Saboteurs points your last scored stream earned, waiting for the host to pass them to your rivals.</summary>
        public int SabotageOut;
        /// <summary>The Mods' old rule (retired 2026-11-16): your next stream starts with 1 Deplatform Risk cancelled. Never set now.</summary>
        public bool ModsShieldNext;

        // Current stream: streamer power effects
        public int PartyHeat;          // Deplatform Risk from rivals' raids.
        public int PartyShield;        // Deplatform Risk ignored (Smoke Bomb).
        public int PartyViewerBonus;   // Extra Cash per viewer (Giveaway).
        public int PartyFreeActions;   // Viewer actions that refresh after use (Double Move).
        public bool PartyXRay;         // The next viewers in the queue are visible (X-Ray Vision).
        public int PartySeats;         // Extra Sub Cap for this stream only (Three-Hour Episode); taken back off Capacity when it ends.
        public int PartyBark;          // Extra Cash for each viewer carrying Risk (Bark at Chat).
        public bool PartySoftLanding;  // A deplatformed stream still banks half its Cash (Soft Landing).
        public bool PartySubathon;     // The first viewer who would crash the server goes back to the queue instead (Subathon).
        public bool PartyDoubleDown;   // Scored: Cash again (capped); failed: lose Influence (Double or Nothing).

        // Game mode (2026-11-16)
        public GameMode Mode;
        /// <summary>Endless: Trophies banked (one for every Trophy viewer in a scored stream), spent on streams and shop viewers.</summary>
        public int Trophies;
        /// <summary>Endless: extra streams bought (each one costs 1 Trophy more).</summary>
        public int StreamsBought;
        /// <summary>Endless: viewers added to the shop (each one costs 1 Trophy more).</summary>
        public int ShopViewersBought;

        // Teams (2026-11-16): two players share one season (resources, the stream, the shop) but keep their own Audience
        // and Sub Cap. Member 0 uses Capacity / Expansions, member 1 PartnerCapacity / PartnerExpansions.
        /// <summary>Players sharing this season: 1, or 2 for a team (2v2, co-op).</summary>
        public int Members = 1;
        public int PartnerCapacity = GameRules.StartingCapacity;
        public int PartnerExpansions;
        /// <summary>Teams: viewers member 1 has removed (each player has their own Audience, so their own removal price).</summary>
        public int PartnerRemovals;
        /// <summary>Teams: one bit per member who has ended their side of this stream. It's scored once every member has.</summary>
        public int StreamDone;

        // Teams (2026-11-18): each member plays their own streamer, with their own active, price, cooldown and armed power.
        // Member 0 uses Streamer / PowerUses / PowerCooldown / PowerArmed, member 1 these. A team's two streamers are never
        // the same one (the host keeps them apart), so a streamer type names the member it belongs to (MemberWithStreamer).
        public StreamerType PartnerStreamer;
        public int PartnerPowerUses;
        public int PartnerPowerCooldown;
        public bool PartnerPowerArmed;

        // Stats
        public int BestDiamonds;
        public int TotalCashEarned;
        public int TotalInfluenceEarned;
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

        /// <summary>Every seat in chat is taken (a team: both members' sides are full).</summary>
        public bool HouseFull => IsTeam ? SideFull(0) && SideFull(1) : Occupancy >= Capacity;

        /// <summary>Empty seats in chat (a team: both sides together, since the two streams score as one).</summary>
        public int EmptySpaces => IsTeam
            ? Math.Max(0, Capacity - OccupancyOf(0)) + Math.Max(0, PartnerCapacity - OccupancyOf(1))
            : Math.Max(0, Capacity - Occupancy);

        // ------------------------------------------------------------------ modes and teams

        public bool IsEndless => Mode == GameMode.Endless;
        public bool IsTeam => Members > 1;

        /// <summary>A member's Sub Cap (member 0, or a solo season: <see cref="Capacity"/>).</summary>
        public int CapacityOf(int member) => IsTeam && member == 1 ? PartnerCapacity : Capacity;

        /// <summary>Sub Cap upgrades a member has bought.</summary>
        public int ExpansionsOf(int member) => IsTeam && member == 1 ? PartnerExpansions : Expansions;

        /// <summary>Viewers a member has removed with Remove a Viewer (sets their next removal price).</summary>
        public int RemovalsOf(int member) => IsTeam && member == 1 ? PartnerRemovals : Removals;

        /// <summary>Both sides' Sub Caps together (a solo season: <see cref="Capacity"/>).</summary>
        public int TotalCapacity => IsTeam ? Capacity + PartnerCapacity : Capacity;

        /// <summary>Seats a member's viewers take in chat (a solo season: <see cref="Occupancy"/>).</summary>
        public int OccupancyOf(int member)
        {
            if (!IsTeam) return Occupancy;
            int total = 0;
            for (int i = 0; i < House.Count; i++)
            {
                var g = Find(House[i]);
                if (g != null && g.Owner == member) total += g.Info.Size;
            }
            return total;
        }

        /// <summary>A member's side of the chat is full.</summary>
        public bool SideFull(int member) => OccupancyOf(member) >= CapacityOf(member);

        /// <summary>Teams: this member has ended their side of the stream.</summary>
        public bool StreamDoneFor(int member) => IsTeam && (StreamDone & (1 << member)) != 0;

        /// <summary>The next viewer in this member's queue (a solo season: the whole queue), or -1.</summary>
        public int NextInPool(int member)
        {
            for (int i = 0; i < Pool.Count; i++)
            {
                if (!IsTeam) return Pool[i];
                var g = Find(Pool[i]);
                if (g != null && g.Owner == member) return Pool[i];
            }
            return -1;
        }

        /// <summary>Viewers waiting in this member's queue (a solo season: the whole queue).</summary>
        public int PoolCountOf(int member)
        {
            if (!IsTeam) return Pool.Count;
            int count = 0;
            for (int i = 0; i < Pool.Count; i++)
            {
                var g = Find(Pool[i]);
                if (g != null && g.Owner == member) count++;
            }
            return count;
        }

        /// <summary>Permanent cards of this type in a member's Audience (a solo season: the whole Audience).</summary>
        public int CountOwned(GuestType type, int member)
        {
            if (!IsTeam) return CountOwned(type);
            int count = 0;
            for (int i = 0; i < Rolodex.Count; i++)
                if (Rolodex[i].Type == type && !Rolodex[i].Temporary && Rolodex[i].Owner == member) count++;
            return count;
        }

        /// <summary>Permanent cards in a member's Audience (a solo season: the whole Audience).</summary>
        public int PermanentCountOf(int member)
        {
            if (!IsTeam) return PermanentCount;
            int count = 0;
            for (int i = 0; i < Rolodex.Count; i++)
                if (!Rolodex[i].Temporary && Rolodex[i].Owner == member) count++;
            return count;
        }

        /// <summary>Whether a card belongs to this member (a solo season: always).</summary>
        public bool Owns(GuestInstance g, int member) => g != null && (!IsTeam || g.Owner == member);
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

        // ------------------------------------------------------------------ streamers per member (teams, 2026-11-18)

        /// <summary>A member's streamer (member 0, or a solo season: <see cref="Streamer"/>).</summary>
        public StreamerType StreamerOf(int member) => IsTeam && member == 1 ? PartnerStreamer : Streamer;

        public StreamerInfo StreamerInfoOf(int member) => StreamerCatalog.Get(StreamerOf(member));

        /// <summary>Times a member has used their streamer's active (sets its next price).</summary>
        public int PowerUsesOf(int member) => IsTeam && member == 1 ? PartnerPowerUses : PowerUses;

        /// <summary>Streams until a member's active can be used again.</summary>
        public int PowerCooldownOf(int member) => IsTeam && member == 1 ? PartnerPowerCooldown : PowerCooldown;

        /// <summary>A member's next-stream active is bought and waiting for the next stream.</summary>
        public bool PowerArmedOf(int member) => IsTeam && member == 1 ? PartnerPowerArmed : PowerArmed;

        /// <summary>
        /// The member playing this streamer: 0 (always 0 in a solo season playing it), 1, or -1 when nobody in this season plays
        /// it. Side effects of an active (Giveaway, Bark at Chat, X-Ray Vision...) apply to that member's viewers.
        /// </summary>
        public int MemberWithStreamer(StreamerType type)
        {
            if (type == StreamerType.None) return -1;
            if (Streamer == type) return 0;
            return IsTeam && PartnerStreamer == type ? 1 : -1;
        }

        /// <summary>Whether this viewer is on the side of the member playing <paramref name="type"/> (a solo season: whenever it's yours).</summary>
        public bool OnSideOf(GuestInstance g, StreamerType type)
        {
            int member = MemberWithStreamer(type);
            if (member < 0 || g == null) return false;
            return !IsTeam || g.Owner == member;
        }

        public PlayerState Clone() => StateSerializer.Deserialize(StateSerializer.Serialize(this));
    }
}
