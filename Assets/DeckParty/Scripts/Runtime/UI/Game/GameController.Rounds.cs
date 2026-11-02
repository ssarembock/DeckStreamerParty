using System.Collections.Generic;
using System.Text;
using DeckParty.Core;
using DeckParty.Networking;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Free For All: the shop gate (Go to the Shop, the 3-2-1 countdown, first come first served) and the round summary. In
    /// this mode every stream runs at once and nobody can watch anyone else, so the summary is how you see what the others did.
    /// </summary>
    public sealed partial class GameController
    {
        /// <summary>The summary's lines: night, then player, then what happened (the latest two nights are kept).</summary>
        readonly SortedDictionary<int, Dictionary<ulong, List<string>>> m_Rounds = new SortedDictionary<int, Dictionary<ulong, List<string>>>();

        bool FreeForAll => m_Net != null && m_Net.FreeForAll;

        /// <summary>Free For All: the shop hasn't opened for everyone yet.</summary>
        bool ShopGated => FreeForAll && m_Net.Stage != RoundStage.Shopping;

        // ------------------------------------------------------------------ the shop gate

        void OnStageChanged(RoundStage stage)
        {
            if (planning.gate != null) planning.gate.ClearCountdown();
            if (stage == RoundStage.Shopping && m_Net.Phase == MatchPhase.InGame && State != null && State.Phase == GamePhase.Planning)
            {
                banner.Show(Loc.Get("game.shop_open"), Loc.Get("game.shop_open_sub"), UIPalette.Gold, 1.2f);
                Fx.Play(FxCue.TurnBanner, banner.transform);
                AudioService.Play(Sfx.Notify);
            }
            RenderAll();
        }

        /// <summary>After your stream: you're ready for the shop (or not, while the others are still streaming).</summary>
        void ToggleShopReady()
        {
            var s = State;
            if (m_Net == null || s == null || s.Phase != GamePhase.Planning || !ViewingOwn || m_Net.Stage != RoundStage.Streaming) return;
            m_Net.TryGetLocalPlayer(out var me);
            m_Net.SetReadyForShop(!me.ReadyForShop);
            AudioService.Play(Sfx.Click);
        }

        /// <summary>Runs the 3-2-1 on the gate while the countdown is on.</summary>
        void UpdateShopGate()
        {
            if (!FreeForAll || m_Net.Stage != RoundStage.Countdown || planning.gate == null || !planning.gate.IsShowing) return;
            if (!planning.gate.SetCountdown(m_Net.ShopCountdownLeft)) return;
            AudioService.PlayPitched(Sfx.Notify, 1.2f, 0.7f);
            UpdateViewBar();
        }

        void FillShopGate(ref PlanningContext ctx)
        {
            ctx.FreeForAll = FreeForAll;
            if (!FreeForAll) return;
            ctx.Stage = m_Net.Stage;
            ctx.ShopGated = m_Net.Stage != RoundStage.Shopping;
            if (m_Net.TryGetLocalPlayer(out var me)) ctx.LocalReadyForShop = me.ReadyForShop;

            var streaming = new List<string>();
            var notReady = new List<string>();
            foreach (var p in m_Net.PlayersSnapshot())
            {
                if (p.ClientId == m_Net.LocalClientId || !p.Connected || p.Finished) continue;
                if (p.GamePhase != GamePhase.Planning) streaming.Add(p.DisplayName);
                else if (!p.ReadyForShop) notReady.Add(p.DisplayName);
            }
            ctx.StillStreaming = string.Join(", ", streaming);
            ctx.NotReadyForShop = string.Join(", ", notReady);
        }

        /// <summary>The bar above the board in Free For All: where the round is, and the round summary button.</summary>
        bool UpdateFreeForAllBar()
        {
            if (!FreeForAll || m_ViewBar == null || m_Net.Phase != MatchPhase.InGame) return false;
            string text;
            Color accent;
            switch (m_Net.Stage)
            {
                case RoundStage.Countdown:
                    text = Loc.Get("bar.ffa_countdown", "seconds", Mathf.Max(1, Mathf.CeilToInt(m_Net.ShopCountdownLeft)));
                    accent = UIPalette.Gold;
                    break;
                case RoundStage.Shopping:
                    text = Loc.Get("bar.ffa_shopping");
                    accent = UIPalette.Gold;
                    break;
                default:
                {
                    int waiting = 0;
                    foreach (var p in m_Net.PlayersSnapshot())
                        if (p.Connected && !p.Finished && !(p.GamePhase == GamePhase.Planning && p.ReadyForShop)) waiting++;
                    text = waiting > 0 ? Loc.Get("bar.ffa_streaming", "count", waiting) : Loc.Get("bar.ffa_all_ready");
                    accent = UIPalette.Teal;
                    break;
                }
            }
            m_ViewBar.Set(text, accent, Loc.Get("bar.round_summary"), ToggleRoundSummary);
            return true;
        }

        // ------------------------------------------------------------------ the round summary

        void ToggleRoundSummary()
        {
            if (roundSummary != null) roundSummary.Toggle(BuildRoundSummary);
        }

        /// <summary>Keeps the events of a player's snapshot that matter for the round summary.</summary>
        void RecordRound(ulong owner, PlayerState s, IReadOnlyList<GameEvent> events)
        {
            if (m_Net == null || s == null || events == null || events.Count == 0) return;
            string who = owner == LocalId ? null : NameOf(owner);
            bool added = false;
            foreach (var e in events)
            {
                if (!InRoundSummary(e.Type)) continue;
                string line = EventText.Describe(e, s, who);
                if (string.IsNullOrEmpty(line)) continue;
                if (!m_Rounds.TryGetValue(s.Night, out var round)) m_Rounds[s.Night] = round = new Dictionary<ulong, List<string>>();
                if (!round.TryGetValue(owner, out var lines)) round[owner] = lines = new List<string>();
                lines.Add(line);
                added = true;
            }
            while (m_Rounds.Count > 2)
            {
                foreach (var oldest in m_Rounds.Keys)
                {
                    m_Rounds.Remove(oldest);
                    break;
                }
            }
            if (added && roundSummary != null) roundSummary.Refresh();
        }

        /// <summary>What a rival would want to know: results, payouts, recruits, upgrades, powers and effects on the next streams.</summary>
        static bool InRoundSummary(GameEventType type)
        {
            switch (type)
            {
                case GameEventType.PartyEnded:
                case GameEventType.PoliceRaid:
                case GameEventType.FireMarshal:
                case GameEventType.ServerRestored:
                case GameEventType.CashShortfall:
                case GameEventType.GuestBlamed:
                case GameEventType.InvestmentPaid:
                case GameEventType.TrollsPaid:
                case GameEventType.CommunityReward:
                case GameEventType.MemeticsRepeat:
                case GameEventType.BotsSent:
                case GameEventType.BotsArrived:
                case GameEventType.DoomApplied:
                case GameEventType.StreakBonus:
                case GameEventType.SoftLanding:
                case GameEventType.GuestPurchased:
                case GameEventType.PalCaught:
                case GameEventType.HouseExpanded:
                case GameEventType.PowerUsed:
                case GameEventType.RaidReceived:
                case GameEventType.TaxPaid:
                case GameEventType.TaxReceived:
                case GameEventType.HeatApplied:
                case GameEventType.ShieldApplied:
                case GameEventType.GiveawayActive:
                case GameEventType.DoubleMoveActive:
                case GameEventType.XRayActive:
                case GameEventType.Victory:
                case GameEventType.Defeat:
                    return true;
                default:
                    return false;
            }
        }

        string BuildRoundSummary()
        {
            var sb = new StringBuilder();
            var players = m_Net != null ? m_Net.PlayersSnapshot() : new List<NetPlayer>();
            if (m_Rounds.Count == 0) sb.Append(GuestVisuals.Colored(Loc.Get("summary.empty"), UIPalette.TextDim)).Append("\n\n");

            var nights = new List<int>(m_Rounds.Keys);
            for (int n = nights.Count - 1; n >= 0; n--)
            {
                var round = m_Rounds[nights[n]];
                sb.Append("<size=125%><b>").Append(GuestVisuals.Colored(Loc.Get("summary.night", "night", nights[n]), UIPalette.Purple)).Append("</b></size>\n");
                foreach (var p in players)
                {
                    if (!round.TryGetValue(p.ClientId, out var lines) || lines.Count == 0) continue;
                    sb.Append("<b>").Append(ColoredName(p.ClientId)).Append("</b>\n");
                    foreach (var line in lines) sb.Append("    • ").Append(line).Append('\n');
                }
                sb.Append('\n');
            }

            // Effects already waiting for the next streams.
            sb.Append("<size=115%><b>").Append(Loc.Get("summary.next")).Append("</b></size>\n");
            bool any = false;
            foreach (var p in players)
            {
                var s = m_Net.GetState(p.ClientId);
                if (s == null || !p.Connected || s.IsGameOver) continue;
                var parts = new List<string>();
                if (s.IncomingHeat > 0) parts.Add(Loc.Get("summary.next_heat", "risk", GuestVisuals.RiskText(Mathf.Min(StreamerCatalog.MaxRaidHeat, s.IncomingHeat))));
                if (s.IncomingDoom > 0) parts.Add(GameIcons.Tag(Community.Doomsdayers) + " " + Loc.Get("summary.next_doom", "value", Communities.DoomChance(s.IncomingDoom)));
                if (s.IncomingBots > 0) parts.Add(GameIcons.Tag(Community.Bots) + " " + Loc.Get("summary.next_bots", "count", Mathf.Min(Communities.BotsMaxIncoming, s.IncomingBots)));
                if (s.PowerArmed) parts.Add(Loc.Get("summary.next_power", "power", StreamerVisuals.PowerName(s.Streamer)));
                if (parts.Count == 0) continue;
                any = true;
                sb.Append("    • ").Append(ColoredName(p.ClientId)).Append(": ").Append(string.Join(", ", parts)).Append('\n');
            }
            if (!any) sb.Append("    ").Append(GuestVisuals.Colored(Loc.Get("summary.next_none"), UIPalette.TextDim)).Append('\n');
            return sb.ToString();
        }
    }
}
