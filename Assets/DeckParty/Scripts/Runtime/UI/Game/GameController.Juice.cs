using System.Collections;
using System.Collections.Generic;
using DeckParty.Core;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Game feel: banners, sounds, Feel feedbacks and shader pulses for engine events, the end-of-stream scoring sequence
    /// and link lines. Everything here is driven by the shared events, so watchers see exactly what the active player sees.
    /// </summary>
    public sealed partial class GameController
    {
        bool m_Scoring;
        bool m_SkipScoring;
        PlayerState m_LastLive;

        /// <summary>Banners, sounds and effects for events on the board being shown. <paramref name="who"/> is null for your own stream.</summary>
        void ProcessEvents(PlayerState s, IReadOnlyList<GameEvent> events, string who)
        {
            m_Arrivals.Clear();
            m_Pulses.Clear();
            if (events == null) return;

            Sfx sound = Sfx.Click;
            int soundPriority = -1;
            void Consider(Sfx candidate, int priority)
            {
                if (priority > soundPriority)
                {
                    sound = candidate;
                    soundPriority = priority;
                }
            }

            foreach (var e in events)
            {
                if (fakeChat != null) fakeChat.React(e.Type);
                SignatureFx.OnEvent(e, house, effects);

                switch (e.Type)
                {
                    case GameEventType.NightStarted:
                    {
                        string left = s.NightsRemaining > 0 ? Loc.Get("game.nights_left", "count", s.NightsRemaining) : Loc.Get("game.final_night");
                        banner.Show(Loc.Get("game.night_banner", "night", e.A), who == null ? left : Loc.Get("game.whose_stream", "player", who) + "  -  " + left, UIPalette.Purple, 1.0f);
                        Consider(Sfx.Notify, 1);
                        break;
                    }
                    case GameEventType.GuestArrived:
                        m_Arrivals.Add(e.GuestId);
                        if (GuestCatalog.Get(e.Guest).IsDiamond)
                        {
                            Consider(Sfx.StarArrive, 5);
                            AudioService.Duck(0.8f);
                            Fx.Play(FxCue.DiamondTracker, hud.starsLabel.transform);
                        }
                        else Consider(e.A < 0 ? Sfx.DoorKnock : Sfx.GuestArrive, 2);
                        break;
                    case GameEventType.GuestRemoved:
                    case GameEventType.GuestRejected:
                        Consider(Sfx.Boot, 3);
                        break;
                    case GameEventType.GuestPeeked:
                    case GameEventType.AttractionRevealed:
                        Consider(Sfx.Peek, 3);
                        break;
                    case GameEventType.GuestScored:
                        if (e.C == 0)
                        {
                            m_Pulses.Add(e.GuestId);
                            Consider(Sfx.Coin, 3);
                        }
                        break;
                    case GameEventType.GuestGained:
                    case GameEventType.PartyPotGained:
                        m_Pulses.Add(e.GuestId);
                        Consider(Sfx.Coin, 1);
                        break;
                    case GameEventType.AbilityUsed:
                        Consider(Sfx.Ability, 2);
                        PlayActionImpact(e);
                        break;
                    case GameEventType.TroubleAdded:
                        Fx.Play(FxCue.RiskPip, hud.troubleLabel.transform);
                        break;
                    case GameEventType.EveryoneOutside:
                    case GameEventType.Transformed:
                    case GameEventType.Struck:
                    case GameEventType.Infected:
                    case GameEventType.PositionsSwapped:
                    case GameEventType.PrintedExchanged:
                    case GameEventType.ShieldBlocked:
                    case GameEventType.PermanentChange:
                        m_Pulses.Add(e.GuestId);
                        Consider(Sfx.Ability, 3);
                        break;
                    case GameEventType.ThisIsFine:
                        banner.Show(Loc.Get("game.this_is_fine"), Loc.Get("game.this_is_fine_sub"), UIPalette.Orange, 1.6f);
                        effects.Flash(UIPalette.Orange);
                        Consider(Sfx.Ability, 8);
                        break;
                    case GameEventType.PoliceRaid:
                        banner.Show(Terms.Raid, who == null ? Loc.Get("game.swatted_sub") : Loc.Get("game.swatted_sub_other", "player", who), UIPalette.Red, 1.6f);
                        Swatted();
                        if (facecam != null) facecam.React(CharacterState.Hit);
                        Consider(Sfx.Raid, 9);
                        break;
                    case GameEventType.FireMarshal:
                        banner.Show(Terms.Overload, Loc.Get("game.overload_sub", "filled", e.A, "cap", e.B), UIPalette.Orange, 1.6f);
                        Crashed();
                        if (facecam != null) facecam.React(CharacterState.Hit);
                        Consider(Sfx.FireMarshal, 9);
                        break;
                    case GameEventType.PartyEnded:
                        effects.FloatText($"{GuestVisuals.PopText(e.A)}   {GuestVisuals.CashText(e.B)}", new Vector2(0f, 60f));
                        if (facecam != null) facecam.React(CharacterState.Celebrate);
                        Consider(Sfx.PartySuccess, 7);
                        break;
                    case GameEventType.CashShortfall:
                        if (who == null) ToastView.Show(Loc.Get("game.shortfall_you", "cash", e.A, "penalty", e.B), ToastKind.Error);
                        else ToastView.Show(Loc.Get("game.shortfall_other", ("player", who), ("cash", e.A), ("penalty", e.B)), ToastKind.Info);
                        break;
                    case GameEventType.GuestPurchased:
                        Consider(Sfx.Purchase, 4);
                        PurchaseFlourish(e.Guest);
                        break;
                    case GameEventType.PalCaught:
                        Consider(Sfx.Purchase, 5);
                        PurchaseFlourish(e.Guest);
                        break;
                    case GameEventType.HouseExpanded:
                        Consider(Sfx.Expand, 4);
                        Fx.Play(FxCue.CounterPunch, hud.capacityLabel.transform);
                        break;
                    case GameEventType.PowerUsed:
                    {
                        var streamer = (StreamerType)e.A;
                        banner.Show(StreamerVisuals.PowerName(streamer), StreamerVisuals.Name(streamer), StreamerVisuals.Color(streamer), 1.2f);
                        if (planning.power != null) Fx.Play(FxCue.PowerUsed, planning.power.transform);
                        if (facecam != null) facecam.React(CharacterState.Select);
                        Consider(Sfx.Ability, 6);
                        break;
                    }
                    case GameEventType.HeatApplied:
                        banner.Show(Loc.Get("power.raided_title"), Loc.Get("game.heat_sub", "count", e.A), UIPalette.Red, 1.4f);
                        Fx.Play(FxCue.RiskPip, hud.troubleLabel.transform, 1.5f);
                        Consider(Sfx.Raid, 6);
                        break;
                    case GameEventType.ShieldApplied:
                    case GameEventType.GiveawayActive:
                    case GameEventType.DoubleMoveActive:
                    case GameEventType.XRayActive:
                        Consider(Sfx.Notify, 4);
                        break;
                    case GameEventType.SoftLanding:
                    case GameEventType.StreakBonus:
                        Fx.Play(FxCue.CounterPunch, hud.popLabel.transform, 1.3f);
                        Consider(Sfx.Coin, 6);
                        break;
                    case GameEventType.Victory:
                        banner.Show(who == null ? Loc.Get("over.victory") : Loc.Get("over.player_wins", "player", who), Loc.Get("game.victory_sub", "count", GameRules.DiamondsToWin), UIPalette.Gold, 2.2f);
                        effects.Confetti(140);
                        effects.Flash(UIPalette.Gold);
                        effects.Sparkles(banner.transform.position, UIPalette.Gold, 30);
                        AudioService.Duck(2.5f);
                        Fx.Play(FxCue.Victory, banner.transform);
                        if (facecam != null) facecam.React(CharacterState.Celebrate);
                        Consider(Sfx.Victory, 10);
                        break;
                    case GameEventType.Defeat:
                        banner.Show(Loc.Get("over.season_over"), who == null ? Loc.Get("game.defeat_sub", "count", GameRules.DiamondsToWin) : Loc.Get("game.defeat_sub_other", "player", who), UIPalette.TextDim, 2f);
                        Fx.Play(FxCue.Defeat, banner.transform);
                        Consider(Sfx.Defeat, 10);
                        break;
                }
            }

            if (soundPriority >= 0) AudioService.Play(sound);
        }

        /// <summary>C11: SWATTED! A strong shake, the police strobe, a red flash and every card glitching.</summary>
        void Swatted()
        {
            Fx.Play(FxCue.Swatted, Fx.ShakeRoot != null ? Fx.ShakeRoot : partyRoot.transform);
            Fx.Shake(24f, 0.5f);
            effects.Flash(UIPalette.Red);
            background.PoliceLights();
            AudioService.Duck(1.6f);
            foreach (var card in house.Cards)
                if (card.gameObject.activeSelf && card.Fx != null) card.Fx.Pulse(CardPulse.Glitch);
        }

        /// <summary>C12: Sub Cap exceeded, the stream crashes: cards vibrate and pixelate.</summary>
        void Crashed()
        {
            Fx.Play(FxCue.Crashed, Fx.ShakeRoot != null ? Fx.ShakeRoot : partyRoot.transform);
            Fx.Shake(18f, 0.45f);
            effects.Flash(UIPalette.Orange);
            AudioService.Duck(1.4f);
            foreach (var card in house.Cards)
            {
                if (!card.gameObject.activeSelf || card.Fx == null) continue;
                card.Fx.Pulse(CardPulse.Vibrate);
                card.Fx.Pulse(CardPulse.Pixelate);
            }
        }

        /// <summary>C4/C5: the acting card winds up, then the targets react according to the action's family.</summary>
        void PlayActionImpact(GameEvent e)
        {
            var actor = house.FindCard(e.GuestId);
            if (actor != null) actor.PlayActive();

            FxCue cue;
            switch (GuestCatalog.Get(e.Guest).ActionCategory)
            {
                case ActionCategory.Recruit: cue = FxCue.ImpactRecruit; break;
                case ActionCategory.Remove: cue = FxCue.ImpactRemove; break;
                case ActionCategory.Shuffle: cue = FxCue.ImpactShuffle; break;
                case ActionCategory.Scout: cue = FxCue.ImpactScout; break;
                case ActionCategory.Economy: cue = FxCue.ImpactEconomy; break;
                default: cue = FxCue.ImpactModify; break;
            }
            foreach (var id in new[] { e.A, e.B })
            {
                var target = id >= 0 ? house.FindCard(id) : null;
                if (target == null) continue;
                Fx.Play(cue, target.AnimRoot);
                if (target.Portrait != null) target.Portrait.Play(CharacterState.Hit);
            }
        }

        /// <summary>A bought (or caught) viewer flies from the shop to the Audience button.</summary>
        void PurchaseFlourish(GuestType type)
        {
            if (!planning.gameObject.activeInHierarchy) return;
            var row = planning.FindRow(type);
            var from = row != null ? row.PortraitRect.position : planning.transform.position;
            effects.FlyChip(GuestVisuals.NameTag(type), from, planning.RolodexButtonRect.position);
            if (row != null) Fx.Play(FxCue.Purchase, row.transform);
            Fx.Play(FxCue.CounterPunch, planning.RolodexButtonRect, 1.2f);
        }

        void PlayCardAnimations(PlayerState s, IReadOnlyList<GameEvent> events)
        {
            float delay = 0f;
            foreach (var id in m_Arrivals)
            {
                var card = house.FindCard(id);
                if (card == null) continue;
                var g = s.Find(id);
                card.PlayArrive(delay, g != null && g.Info.IsDiamond);
                delay += 0.12f;
            }

            foreach (var id in m_Pulses)
            {
                if (id < 0 || m_Arrivals.Contains(id)) continue;
                var card = house.FindCard(id);
                if (card != null) card.PlayPulse();
            }

            if (s.Phase == GamePhase.Party) m_LastLive = s.Clone();
        }

        // ------------------------------------------------------------------ C13: the end-of-stream scoring sequence

        /// <summary>
        /// When a stream ends successfully, the chat stays on screen while each viewer (in joining order) plays Score and
        /// sends its take flying to the HUD, the pitch climbing with every viewer. Click or Space skips it.
        /// </summary>
        bool TryStartScoring(PlayerState s, IReadOnlyList<GameEvent> events)
        {
            if (!FxLibrary.Current.scoringSequence || m_LastLive == null || events == null) return false;
            var scored = new List<GameEvent>();
            bool ended = false;
            foreach (var e in events)
            {
                if (e.Type == GameEventType.GuestScored && e.C != 0) scored.Add(e);
                if (e.Type == GameEventType.PartyEnded) ended = true;
            }
            if (!ended || scored.Count == 0) return false;
            StartCoroutine(ScoringSequence(m_LastLive, s, events, scored));
            m_LastLive = null;
            return true;
        }

        IEnumerator ScoringSequence(PlayerState before, PlayerState after, IReadOnlyList<GameEvent> events, List<GameEvent> scored)
        {
            m_Scoring = true;
            m_SkipScoring = false;
            CancelTargeting(false);

            // Keep the final chat on screen.
            partyRoot.SetActive(true);
            planning.gameObject.SetActive(false);
            raidBlame.gameObject.SetActive(false);
            house.Render(before);
            partyControls.Render(before, false, false);

            float step = Mathf.Clamp(3f / scored.Count, 0.12f, 0.28f) * AppSettings.AnimationTimeScale;
            int total = 0;
            for (int i = 0; i < scored.Count && !m_SkipScoring; i++)
            {
                var e = scored[i];
                var card = house.FindCard(e.GuestId);
                total += e.A;
                if (card != null)
                {
                    if (card.Portrait == null || !card.Portrait.Play(CharacterState.Score)) Fx.Play(FxCue.ScoreTick, card.AnimRoot);
                    else Fx.Play(FxCue.ScoreTick, card.AnimRoot, 0.6f);
                    if (card.Fx != null) card.Fx.Pulse(CardPulse.Shine, 0.7f);
                    string chip = e.B != 0 ? $"{GuestVisuals.PopText(e.A)} {GuestVisuals.CashText(e.B)}" : GuestVisuals.PopText(e.A);
                    effects.FlyChip(chip, card.AnimRoot.position, hud.popLabel.transform.position);
                }
                AudioService.PlayPitched(Sfx.Coin, 1f + i * 0.06f, 0.8f);
                yield return Wait(step);
            }

            if (!m_SkipScoring) yield return Wait(0.35f * AppSettings.AnimationTimeScale);

            m_Scoring = false;
            ApplyVenue(false);
            ProcessEvents(after, events, ViewingOwn ? null : NameOf(m_ViewId));
            banner.Show(Loc.Get("result.success"), Loc.Get("game.scored_total", "subs", GuestVisuals.PopText(after.LastPop), "cash", GuestVisuals.CashText(after.LastCash)), UIPalette.Gold, 1.4f);
            Fx.Play(FxCue.BannerSlam, banner.transform);
            RenderAll();
        }

        IEnumerator Wait(float seconds)
        {
            for (float t = 0f; t < seconds && !m_SkipScoring; t += Time.unscaledDeltaTime) yield return null;
        }

        void SkipScoring() => m_SkipScoring = true;

        // ------------------------------------------------------------------ N14: link lines

        void RenderLinks(PlayerState s)
        {
            if (links == null) return;
            links.Begin();
            if (s.Phase == GamePhase.Party)
            {
                for (int i = 0; i < s.House.Count; i++)
                {
                    var g = s.Find(s.House[i]);
                    if (g == null) continue;

                    if (g.Kind == GuestType.OverlyAttachedGirlfriend)
                    {
                        var partner = PartyEngine.AttachedPartner(s, g);
                        if (partner != null) links.Link(house.FindCard(g.Id)?.AnimRoot, house.FindCard(partner.Id)?.AnimRoot, false);
                    }
                    else if (g.Kind == GuestType.NyanCat && g.Counter > 0)
                    {
                        // The current unbroken chain: the last Counter arrivals after Nyan Cat.
                        int end = s.House.Count - 1;
                        int start = Mathf.Max(i + 1, end - g.Counter + 1);
                        var previous = house.FindCard(g.Id)?.AnimRoot;
                        for (int j = start; j <= end; j++)
                        {
                            var next = house.FindCard(s.House[j])?.AnimRoot;
                            links.Link(previous, next, true);
                            previous = next;
                        }
                    }
                }
            }
            links.End();
        }
    }
}
