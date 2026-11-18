using System.Collections.Generic;
using System.Text;
using DeckParty.Core;
using DeckParty.Networking;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// The quick summaries (<see cref="RecapView"/>): after a stream ("Great stream!" with what it earned) and, in multiplayer,
    /// after the shop (who recruited what, Sub Cap raises, actives used, resource changes). One column per player:
    ///  - Single player: your stream.
    ///  - Take Turns: the stream (then the shop) of the player whose turn it is, for everyone watching.
    ///  - Free For All: every player's stream on the gate over the shop once the last stream of the round is over, and every player's shop once the
    ///    next round starts.
    /// Built from each player's state changes (open information) and events, so it needs nothing extra from the network.
    /// </summary>
    public sealed partial class GameController
    {
        [Header("Summaries")]
        [Tooltip("The summary pop-up after each stream and (multiplayer) after each shop.")]
        [SerializeField] internal RecapView recap;
        [Tooltip("Multiplayer: the shop summary panel at the bottom of the left column (fades after 10 seconds).")]
        [SerializeField] internal ShopRecapPanelView shopRecapPanel;

        /// <summary>The numbers a summary compares (states are mutated in place, so these are copied).</summary>
        struct RecapSnap
        {
            public GamePhase Phase;
            public int Night;
            public int Cash;
            public int Influence;
            public int Capacity;
            public int PowerUses;
            public StreamerType Streamer;
            /// <summary>Teams: member 1's own streamer and active uses (None / 0 for a solo season).</summary>
            public StreamerType Partner;
            public int PartnerPowerUses;
            /// <summary>Viewer cards in the Audience (a team: both players' together).</summary>
            public int Audience;

            public static RecapSnap Of(PlayerState s) => new RecapSnap
            {
                Phase = s.Phase, Night = s.Night, Cash = s.Cash, Influence = s.Influence, Capacity = s.TotalCapacity, PowerUses = s.PowerUses, Streamer = s.Streamer,
                Partner = s.IsTeam ? s.PartnerStreamer : StreamerType.None, PartnerPowerUses = s.IsTeam ? s.PartnerPowerUses : 0,
                Audience = s.PermanentCount,
            };

            /// <summary>Actives used by everyone in this season (a team: both players).</summary>
            public int AllPowerUses => PowerUses + PartnerPowerUses;
        }

        /// <summary>
        /// Teams (2026-11-18): one summary per team, filed under its member 0, whoever of the two acted, so the column shows
        /// both players. Everyone else: their own id.
        /// </summary>
        ulong RecapKey(ulong clientId)
        {
            if (m_Net == null || !m_Net.IsTeamMode || !m_Net.TryGetPlayer(clientId, out var player)) return clientId;
            foreach (var mate in m_Net.TeamPlayers(player.Team))
                if (mate.Member == 0) return mate.ClientId;
            return clientId;
        }

        /// <summary>
        /// Multiplayer (online and couch, 2026-11-18): notices sit at the bottom of the left column, where the shop summary
        /// appears (and just above it while it shows), instead of over the menu bar.
        /// </summary>
        void DockNotices()
        {
            var column = opponents != null ? opponents.transform.parent as RectTransform : null;
            ToastView.DockTo(column, shopRecapPanel != null ? (RectTransform)shopRecapPanel.transform : null);
        }

        /// <summary>The summaries to show, one per player or (teams) one per team, in lobby order.</summary>
        List<ulong> RecapUnits()
        {
            var units = new List<ulong>();
            foreach (var p in m_Net.PlayersSnapshot())
            {
                ulong key = RecapKey(p.ClientId);
                if (!units.Contains(key)) units.Add(key);
            }
            return units;
        }

        /// <summary>Teams: the two players of the team filed under <paramref name="key"/>, member 0 first.</summary>
        bool TeamPair(ulong key, out NetPlayer first, out NetPlayer second)
        {
            first = default;
            second = default;
            if (m_Net == null || !m_Net.IsTeamMode || !m_Net.TryGetPlayer(key, out var player)) return false;
            int found = 0;
            foreach (var mate in m_Net.TeamPlayers(player.Team))
            {
                if (mate.Member == 0 && (found & 1) == 0) { first = mate; found |= 1; }
                else if ((found & 2) == 0) { second = mate; found |= 2; }
            }
            return found == 3;
        }

        sealed class StreamRecord
        {
            public int Night;
            public bool Done;
            public RecapSnap Start;
            public RecapSnap End;
            public PartyOutcome Outcome;
            public int Plays;
            public int StreakBonus;
            public bool Restored;
            public int BotsSent;
            public readonly List<GuestType> Retired = new List<GuestType>();
            public readonly List<GuestType> Heirs = new List<GuestType>();
            /// <summary>The Cash each heir inherited (Millennials: +1 to +3 since 2026-11-18), in the order of <see cref="Heirs"/>.</summary>
            public readonly List<int> HeirAmounts = new List<int>();
            public bool ModsReady;
        }

        sealed class ShopRecord
        {
            public int Night;
            public bool Done;
            public RecapSnap Start;
            public RecapSnap End;
            public readonly List<GuestType> Recruited = new List<GuestType>();
            public readonly List<GuestType> Removed = new List<GuestType>();
            public int PowersUsed;
        }

        sealed class PlayerRecap
        {
            public RecapSnap Last;
            public StreamRecord Stream;
            public ShopRecord Shop;
        }

        enum RecapKind { Stream, Shop }

        sealed class PendingRecap
        {
            public RecapKind Kind;
            public int Night;
            public readonly List<ulong> Owners = new List<ulong>();
            public float OpenAt;
        }

        readonly Dictionary<ulong, PlayerRecap> m_Recaps = new Dictionary<ulong, PlayerRecap>();
        readonly List<PendingRecap> m_PendingRecaps = new List<PendingRecap>();
        /// <summary>Free For All: the round whose summary is already queued (so it opens once).</summary>
        int m_RecapStreamNight = -1;
        int m_RecapShopNight = -1;
        ulong m_RecapActive = DeckPartyNetSession.NoPlayer;

        bool RecapOpen => recap != null && recap.IsOpen;

        /// <summary>True when a stream summary is about to open (the "Great stream!" banner would only repeat it).</summary>
        bool StreamRecapPending
        {
            get
            {
                foreach (var p in m_PendingRecaps)
                    if (p.Kind == RecapKind.Stream) return true;
                return false;
            }
        }

        MatchMode RecapMode => m_Net != null ? m_Net.Settings.Mode : MatchMode.Turns;

        // ------------------------------------------------------------------ tracking

        /// <summary>Follows one player's state: notes where each stream and shop started and what happened in them.</summary>
        void TrackRecap(ulong owner, PlayerState s, IReadOnlyList<GameEvent> events)
        {
            if (recap == null || s == null) return;
            var now = RecapSnap.Of(s);
            if (!m_Recaps.TryGetValue(owner, out var r))
            {
                r = new PlayerRecap { Last = now };
                m_Recaps[owner] = r;
                if (now.Phase == GamePhase.Party) r.Stream = new StreamRecord { Night = now.Night, Start = now };
                else if (now.Phase == GamePhase.Planning) r.Shop = new ShopRecord { Night = now.Night, Start = now };
                return;
            }

            bool wasPlanning = r.Last.Phase == GamePhase.Planning;
            bool isPlanning = now.Phase == GamePhase.Planning;

            // Left the shop: the next stream began (Take Turns already closed the shop when the turn passed on).
            if (wasPlanning && now.Phase == GamePhase.Party)
            {
                if (r.Shop != null && !r.Shop.Done) FinishShop(owner, r, r.Last);
                r.Stream = new StreamRecord { Night = now.Night, Start = r.Last };
            }

            if (events != null)
            {
                foreach (var e in events)
                {
                    switch (e.Type)
                    {
                        case GameEventType.GuestPurchased:
                        case GameEventType.PalCaught:
                            if (r.Shop != null && !r.Shop.Done) r.Shop.Recruited.Add(e.Guest);
                            break;
                        case GameEventType.GuestDismissed:
                            if (r.Shop != null && !r.Shop.Done) r.Shop.Removed.Add(e.Guest);
                            break;
                        case GameEventType.PowerUsed:
                            if (r.Shop != null && !r.Shop.Done && isPlanning) r.Shop.PowersUsed++;
                            break;
                        case GameEventType.StreakBonus:
                            if (r.Stream != null && !r.Stream.Done) r.Stream.StreakBonus = e.B;
                            break;
                        case GameEventType.ServerRestored:
                            if (r.Stream != null && !r.Stream.Done) r.Stream.Restored = true;
                            break;
                        case GameEventType.BotsSent:
                            if (r.Stream != null && !r.Stream.Done) r.Stream.BotsSent += e.A;
                            break;
                        case GameEventType.BoomerRetired:
                            if (r.Stream != null && !r.Stream.Done) r.Stream.Retired.Add(e.Guest);
                            break;
                        case GameEventType.Inheritance:
                            if (r.Stream != null && !r.Stream.Done)
                            {
                                r.Stream.Heirs.Add(e.Guest);
                                r.Stream.HeirAmounts.Add(e.A);
                            }
                            break;
                        case GameEventType.ModsShield:
                            if (r.Stream != null && !r.Stream.Done && e.A == 1) r.Stream.ModsReady = true;
                            break;
                    }
                }
            }

            // The stream is over (scored, deplatformed and blamed, or Server Down): the shop starts.
            if (!wasPlanning && isPlanning && r.Stream != null && !r.Stream.Done)
            {
                var stream = r.Stream;
                stream.Done = true;
                stream.End = now;
                stream.Outcome = s.LastOutcome;
                stream.Plays = s.LastDiamonds;
                r.Shop = new ShopRecord { Night = now.Night, Start = now };
                OnStreamRecorded(owner, stream.Night);
            }
            else if (!wasPlanning && (now.Phase == GamePhase.Victory || now.Phase == GamePhase.Defeat) && r.Stream != null)
            {
                // The season is over: the game over screen sums it up.
                r.Stream.Done = true;
                r.Stream.End = now;
            }

            r.Last = now;
        }

        void FinishShop(ulong owner, PlayerRecap r, RecapSnap end)
        {
            var shop = r.Shop;
            shop.Done = true;
            shop.End = end;
            OnShopRecorded(owner, shop.Night);
        }

        /// <summary>Take Turns: a turn ends when the next player's starts. The player who just finished has left the shop.</summary>
        void TrackRecapTurn(ulong active)
        {
            ulong previous = m_RecapActive;
            m_RecapActive = active;
            if (!TakingTurns || previous == DeckPartyNetSession.NoPlayer || previous == active) return;
            if (!m_Recaps.TryGetValue(previous, out var r) || r.Shop == null || r.Shop.Done) return;
            var s = m_Net.GetState(previous);
            if (s == null || s.IsGameOver) return;
            FinishShop(previous, r, RecapSnap.Of(s));
        }

        // ------------------------------------------------------------------ when to show

        void OnStreamRecorded(ulong owner, int night)
        {
            if (m_Net == null)
            {
                QueueRecap(RecapKind.Stream, night, owner);
                return;
            }
            // Team modes play Free For All rounds.
            switch (MatchModes.UsesRounds(RecapMode) ? MatchMode.SyncNights : RecapMode)
            {
                case MatchMode.Turns:
                    if (owner == m_Net.ActivePlayer) QueueRecap(RecapKind.Stream, night, owner);
                    break;
                case MatchMode.SyncNights:
                    // Free For All: no pop-up; every player's stream summary appears on the gate over the shop once the
                    // last stream of the round is over (GameController.Rounds: RoundStreamEntries).
                    if (View != null && View.Phase == GamePhase.Planning && !m_Scoring) planning.Render(View, PlanningCtx());
                    break;
                default:
                    // Race: everyone plays their own season at their own pace, so only your own stream.
                    if (owner == LocalId) QueueRecap(RecapKind.Stream, night, owner);
                    break;
            }
        }

        void OnShopRecorded(ulong owner, int night)
        {
            if (m_Net == null) return;
            if (RecapMode == MatchMode.Turns) QueueRecap(RecapKind.Shop, night, owner);
            else if (MatchModes.UsesRounds(RecapMode)) CheckRoundRecap(RecapKind.Shop, night);
        }

        /// <summary>Free For All: once every player still in the season has finished this half of the round, show them all.</summary>
        void CheckRoundRecap(RecapKind kind, int night)
        {
            if (kind == RecapKind.Stream && m_RecapStreamNight == night) return;
            if (kind == RecapKind.Shop && m_RecapShopNight == night) return;

            // One summary per player, or per team (both teammates' moves are filed under the team).
            var owners = new List<ulong>();
            foreach (var key in RecapUnits())
            {
                if (!m_Net.TryGetPlayer(key, out var p) || !UnitConnected(key)) continue;
                bool has = m_Recaps.TryGetValue(key, out var r);
                bool done = has && (kind == RecapKind.Stream
                    ? r.Stream != null && r.Stream.Night == night && r.Stream.Done
                    : r.Shop != null && r.Shop.Night == night && r.Shop.Done);
                if (done)
                {
                    owners.Add(key);
                    continue;
                }
                // Out of the season (won, lost or deplatformed for good): nobody waits for them.
                var s = m_Net.GetState(key);
                if (p.Finished || (s != null && s.IsGameOver)) continue;
                return;
            }
            if (owners.Count == 0) return;
            if (kind == RecapKind.Stream) m_RecapStreamNight = night;
            else m_RecapShopNight = night;
            QueueRecap(kind, night, owners.ToArray());
        }

        /// <summary>Whether anyone in this summary's player or team is still connected.</summary>
        bool UnitConnected(ulong key)
        {
            foreach (var p in m_Net.PlayersSnapshot())
                if (p.Connected && RecapKey(p.ClientId) == key) return true;
            return false;
        }

        void QueueRecap(RecapKind kind, int night, params ulong[] owners)
        {
            if (recap == null || m_GameOverScheduled || m_GameOverShown) return;
            var pending = new PendingRecap { Kind = kind, Night = night, OpenAt = Time.unscaledTime + 0.45f };
            pending.Owners.AddRange(owners);
            m_PendingRecaps.Add(pending);
        }

        /// <summary>Opens the next summary once nothing else is on screen (scoring, the Deplatform image, dialogs...).</summary>
        void UpdateRecap()
        {
            if (recap == null || m_PendingRecaps.Count == 0) return;
            if (m_GameOverScheduled || m_GameOverShown || (gameOver != null && gameOver.IsOpen))
            {
                m_PendingRecaps.Clear();
                return;
            }
            if (RecapOpen || m_Scoring || ModalDialog.IsOpen || picker.IsOpen || pauseMenu.IsOpen || rolodex.IsOpen) return;
            if (outcomeSplash != null && outcomeSplash.IsShowing) return;
            if (roundSummary != null && roundSummary.IsOpen) return;
            var next = m_PendingRecaps[0];
            if (Time.unscaledTime < next.OpenAt) return;
            m_PendingRecaps.RemoveAt(0);
            ShowRecap(next);
        }

        void ShowRecap(PendingRecap pending)
        {
            // Multiplayer shop summaries go to the panel at the bottom of the left column (it fades after 10 s), not a pop-up.
            if (pending.Kind == RecapKind.Shop && shopRecapPanel != null)
            {
                var shops = new List<RecapEntry>();
                foreach (var owner in pending.Owners)
                    if (m_Recaps.TryGetValue(owner, out var shopRecord) && CompactShopEntry(owner, shopRecord.Shop) is RecapEntry shop) shops.Add(shop);
                var state = View ?? State;
                string when = state != null ? Loc.Get("game.night_of", "night", pending.Night, "total", state.TotalNights) : null;
                if (shops.Count > 0) shopRecapPanel.Show(Loc.Get("recap.shop_title") + (when != null ? $"  <size=70%><color=#53535F>{when}</color></size>" : string.Empty), shops);
                return;
            }

            var entries = new List<RecapEntry>();
            PartyOutcome single = PartyOutcome.None;
            foreach (var owner in pending.Owners)
            {
                if (!m_Recaps.TryGetValue(owner, out var r)) continue;
                var entry = pending.Kind == RecapKind.Stream ? StreamEntry(owner, r.Stream) : ShopEntry(owner, r.Shop);
                if (entry == null) continue;
                entries.Add(entry);
                if (pending.Kind == RecapKind.Stream) single = r.Stream.Outcome;
            }
            if (entries.Count == 0) return;

            string title;
            Color color;
            if (pending.Kind == RecapKind.Shop)
            {
                title = Loc.Get("recap.shop_title");
                color = SiteColours.Get(SiteColourRole.Text, SiteArea.PopUps);
            }
            else if (entries.Count == 1)
            {
                // One stream: its result is the title ("Great stream!").
                title = OutcomeTitle(single, out color);
                entries[0].headline = null;
            }
            else
            {
                title = Loc.Get("recap.stream_title");
                color = SiteColours.Get(SiteColourRole.Text, SiteArea.PopUps);
            }

            var any = View ?? State;
            string subtitle = any != null ? Loc.Get("game.night_of", "night", pending.Night, "total", any.TotalNights) : null;
            recap.Show(title, color, subtitle, entries);
        }

        static string OutcomeTitle(PartyOutcome outcome, out Color color)
        {
            switch (outcome)
            {
                case PartyOutcome.Raid:
                    color = UIPalette.Red;
                    return Terms.Raid;
                case PartyOutcome.FireMarshal:
                    color = UIPalette.Orange;
                    return Loc.Get("term.overload");
                default:
                    color = UIPalette.Gold;
                    return Loc.Get("result.success");
            }
        }

        // ------------------------------------------------------------------ the columns

        /// <summary>
        /// A column's head: the player's streamer, name and streamer name. Teams (2026-11-18): both players' streamers side by
        /// side and "Ana and Bo" (member 0 first, each in their colour), whoever of them acted.
        /// </summary>
        RecapEntry BaseEntry(ulong owner, RecapSnap snap)
        {
            bool multiplayer = m_Net != null;
            var streamer = snap.Streamer;
            var entry = new RecapEntry
            {
                portrait = StreamerVisuals.Portrait(streamer),
                color = StreamerColour(streamer),
                initials = StreamerVisuals.Initials(streamer),
                streamer = StreamerVisuals.Name(streamer),
            };
            if (TeamPair(owner, out var first, out var second))
            {
                var partner = snap.Partner;
                entry.name = Loc.Get("recap.team_names", "a", GuestVisuals.Colored(NameOf(first.ClientId), ColorOf(first.ClientId)),
                    "b", GuestVisuals.Colored(NameOf(second.ClientId), ColorOf(second.ClientId)));
                entry.hasSecond = true;
                entry.portrait2 = StreamerVisuals.Portrait(partner);
                entry.color2 = StreamerColour(partner);
                entry.initials2 = StreamerVisuals.Initials(partner);
                entry.streamer = Loc.Get("recap.team_names", "a", StreamerVisuals.Name(streamer), "b", StreamerVisuals.Name(partner));
                return entry;
            }
            var name = multiplayer ? NameOf(owner) : Loc.Get("common.you");
            entry.name = multiplayer ? GuestVisuals.Colored(name, ColorOf(owner)) : name;
            return entry;
        }

        static Color StreamerColour(StreamerType streamer) => streamer == StreamerType.None ? UIPalette.Purple : StreamerVisuals.Color(streamer);

        RecapEntry StreamEntry(ulong owner, StreamRecord stream)
        {
            if (stream == null || !stream.Done) return null;
            var entry = BaseEntry(owner, stream.End);
            entry.headline = OutcomeTitle(stream.Outcome, out var color);
            entry.headline = GuestVisuals.Colored(entry.headline, color);

            var sb = new StringBuilder();
            Line(sb, GameIcons.Tag(ResourceIcon.Cash), Signed(stream.End.Cash - stream.Start.Cash), Loc.Get("term.cash_banked"));
            Line(sb, GameIcons.Tag(ResourceIcon.Influence), Signed(stream.End.Influence - stream.Start.Influence), Loc.Get("term.influence"));
            if (stream.Outcome == PartyOutcome.Success || stream.Plays > 0)
                Line(sb, GameIcons.Tag(ResourceIcon.Plays), stream.Plays.ToString(), Loc.Get("term.diamonds"));
            if (stream.StreakBonus > 0) sb.Append(Loc.Get("recap.streak", "count", stream.StreakBonus)).Append('\n');
            if (stream.Restored) sb.Append(GameIcons.Tag(Community.HypeMen)).Append(' ').Append(Loc.Get("game.server_restored")).Append('\n');
            if (stream.BotsSent > 0) sb.Append(GameIcons.Tag(Community.Bots)).Append(' ').Append(Loc.Get("recap.bots_sent", "count", stream.BotsSent)).Append('\n');
            foreach (var boomer in stream.Retired)
                sb.Append(GameIcons.Tag(Community.Boomer)).Append(' ').Append(Loc.Get("recap.boomer_retired", "viewer", GuestVisuals.NameTag(boomer))).Append('\n');
            for (int h = 0; h < stream.Heirs.Count; h++)
            {
                int amount = h < stream.HeirAmounts.Count ? stream.HeirAmounts[h] : Communities.InheritanceCash;
                sb.Append(GameIcons.Tag(Community.Millennial)).Append(' ')
                    .Append(Loc.Get("recap.inheritance", "viewer", GuestVisuals.NameTag(stream.Heirs[h]), "amount", amount)).Append('\n');
            }
            if (stream.ModsReady) sb.Append(GameIcons.Tag(Community.Mods)).Append(' ').Append(Loc.Get("game.mods_ready")).Append('\n');
            entry.body = sb.ToString().TrimEnd('\n');
            return entry;
        }

        RecapEntry ShopEntry(ulong owner, ShopRecord shop)
        {
            if (shop == null || !shop.Done) return null;
            var entry = BaseEntry(owner, shop.End);
            var sb = new StringBuilder();

            // Recruits, grouped: "+ Loyal Fan ×2".
            if (shop.Recruited.Count > 0)
            {
                sb.Append("<b>").Append(Loc.Get("recap.recruited")).Append("</b>\n");
                var counts = new Dictionary<GuestType, int>();
                var order = new List<GuestType>();
                foreach (var type in shop.Recruited)
                {
                    if (!counts.ContainsKey(type))
                    {
                        counts[type] = 0;
                        order.Add(type);
                    }
                    counts[type]++;
                }
                foreach (var type in order)
                {
                    sb.Append("+ ").Append(GuestVisuals.DisplayName(type));
                    if (counts[type] > 1) sb.Append(" ×").Append(counts[type]);
                    if (GuestCatalog.Get(type).IsDiamond) sb.Append(' ').Append(GameIcons.Tag(ResourceIcon.Plays));
                    sb.Append('\n');
                }
            }
            else
            {
                sb.Append("<color=#53535F>").Append(Loc.Get("recap.nothing_bought")).Append("</color>\n");
            }

            foreach (var removed in shop.Removed) sb.Append(Loc.Get("recap.removed", "viewer", GuestVisuals.DisplayName(removed))).Append('\n');
            int raised = shop.End.Capacity - shop.Start.Capacity;
            if (raised > 0) sb.Append(Loc.Get("recap.sub_cap", ("from", shop.Start.Capacity), ("to", shop.End.Capacity), ("count", raised))).Append('\n');
            // Each streamer's active (a team has two streamers since 2026-11-18).
            int powers = shop.End.PowerUses - shop.Start.PowerUses;
            int partnerPowers = shop.End.PartnerPowerUses - shop.Start.PartnerPowerUses;
            if (shop.End.Partner == StreamerType.None) powers = Mathf.Max(shop.PowersUsed, powers);
            if (powers > 0 && shop.End.Streamer != StreamerType.None)
                sb.Append(Loc.Get("recap.active_used", "power", StreamerVisuals.PowerName(shop.End.Streamer), "count", powers)).Append('\n');
            if (partnerPowers > 0 && shop.End.Partner != StreamerType.None)
                sb.Append(Loc.Get("recap.active_used", "power", StreamerVisuals.PowerName(shop.End.Partner), "count", partnerPowers)).Append('\n');

            Line(sb, GameIcons.Tag(ResourceIcon.Cash), Signed(shop.End.Cash - shop.Start.Cash), Loc.Get("term.cash_banked"));
            Line(sb, GameIcons.Tag(ResourceIcon.Influence), Signed(shop.End.Influence - shop.Start.Influence), Loc.Get("term.influence"));
            entry.body = sb.ToString().TrimEnd('\n');
            return entry;
        }

        /// <summary>
        /// The multiplayer shop summary panel (2026-11-18): per player or team, just their name and four icons with what
        /// changed: Audience (viewer cards gained or lost), Cash, Influence, and actives used.
        /// </summary>
        RecapEntry CompactShopEntry(ulong owner, ShopRecord shop)
        {
            if (shop == null || !shop.Done) return null;
            var entry = BaseEntry(owner, shop.End);
            entry.streamer = null;
            int actives = Mathf.Max(shop.End.Partner == StreamerType.None ? shop.PowersUsed : 0, shop.End.AllPowerUses - shop.Start.AllPowerUses);
            entry.body = CompactChange(GameIcons.AudienceTag, shop.End.Audience - shop.Start.Audience, true) + "   " +
                         CompactChange(GameIcons.Tag(ResourceIcon.Cash), shop.End.Cash - shop.Start.Cash, true) + "   " +
                         CompactChange(GameIcons.Tag(ResourceIcon.Influence), shop.End.Influence - shop.Start.Influence, true) + "   " +
                         CompactChange(GameIcons.ActiveTag, actives, false);
            return entry;
        }

        /// <summary>"[icon] +2" kept together on one line; nothing changed shows a grey 0.</summary>
        static string CompactChange(string icon, int value, bool signed)
        {
            string number = value == 0 ? "<color=#8A8A96>0</color>" : signed ? Signed(value) : value.ToString();
            return "<nobr><size=125%>" + icon + "</size> <b>" + number + "</b></nobr>";
        }

        static void Line(StringBuilder sb, string icon, string value, string label)
        {
            sb.Append("<size=120%>").Append(icon).Append("</size> <b>").Append(value).Append("</b> <size=80%><color=#53535F>")
                .Append(label).Append("</color></size>\n");
        }

        static string Signed(int value) => value > 0 ? "+" + value : value.ToString();
    }
}
