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
        /// <summary>True while the events of a stream whose scoring sequence just played are processed (its chips already flew).</summary>
        bool m_AfterSequence;
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
                            Fx.Play(FxCue.DiamondTracker, hud.PlaysAnchor);
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
                    case GameEventType.ScoredNow:
                    {
                        // Banked mid-stream (Crypto Guy, Generous Donator): the take flies straight to the wallet.
                        m_Pulses.Add(e.GuestId);
                        var card = house.FindCard(e.GuestId);
                        if (card != null)
                        {
                            FlyResources(card.AnimRoot.position, e.A, e.B, false, null);
                        }
                        Fx.Play(FxCue.CounterPunch, hud.popLabel.transform, 1.2f);
                        Consider(Sfx.Coin, 4);
                        break;
                    }
                    case GameEventType.InterviewGiven:
                    case GameEventType.CashBoosted:
                        m_Pulses.Add(e.GuestId);
                        if (e.Type == GameEventType.InterviewGiven) Fx.Play(FxCue.CounterPunch, hud.cashLabel.transform, 1.2f);
                        Consider(Sfx.Coin, 3);
                        break;
                    case GameEventType.MoodFlipped:
                        m_Pulses.Add(e.GuestId);
                        if (e.A != 0) Fx.Play(FxCue.RiskPip, hud.RiskAnchor);
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
                        Fx.Play(FxCue.RiskPip, hud.RiskAnchor);
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
                    {
                        string sub = who == null ? Loc.Get("game.banned_sub") : Loc.Get("game.banned_sub_other", "player", who);
                        // Your Deplatform image when there is one, otherwise the text banner.
                        if (outcomeSplash == null || !outcomeSplash.Show(GameDatabase.Instance.deplatformImage, Terms.Raid + "\n<size=55%>" + sub + "</size>", UIPalette.Red))
                            banner.Show(Terms.Raid, sub, UIPalette.Red, 1.6f);
                        Deplatformed();
                        if (facecam != null) facecam.React(CharacterState.Hit);
                        Consider(Sfx.Raid, 9);
                        break;
                    }
                    case GameEventType.FireMarshal:
                    {
                        string sub = Loc.Get("game.server_down_sub", "filled", e.A, "cap", e.B);
                        if (outcomeSplash == null || !outcomeSplash.Show(GameDatabase.Instance.serverDownImage, Terms.Overload + "\n<size=55%>" + sub + "</size>", UIPalette.Orange))
                            banner.Show(Terms.Overload, sub, UIPalette.Orange, 1.6f);
                        ServerDown();
                        if (facecam != null) facecam.React(CharacterState.Hit);
                        Consider(Sfx.FireMarshal, 9);
                        break;
                    }
                    // --- communities ---
                    case GameEventType.CommunityBonus:
                        if (e.C == 0) m_Pulses.Add(e.GuestId);
                        break;
                    case GameEventType.ServerRestored:
                        // Shown before the scoring sequence (TryStartScoring); without it, a quick banner and flash.
                        if (!FxLibrary.Current.scoringSequence) banner.Show(Loc.Get("game.server_restored"), Loc.Get("game.server_restored_sub", "value", e.A), UIPalette.Teal, 1.6f);
                        effects.Flash(UIPalette.Teal);
                        Consider(Sfx.PartySuccess, 8);
                        break;
                    case GameEventType.CommunityReward:
                        // Boomer / Millennial / Zoomer payouts fly from their chip during the scoring sequence; without it, float up.
                        if (!m_AfterSequence)
                        {
                            var community = (Community)e.A;
                            effects.FloatText(GameIcons.Tag(community) + " " + EventText.Amounts(e.B, e.C), new Vector2(0f, 10f - 40f * (e.A % 3)));
                        }
                        Consider(Sfx.Coin, 5);
                        break;
                    case GameEventType.TrollsPaid:
                        effects.FloatText(GuestVisuals.Colored(Loc.Get("game.trolls_paid"), UIPalette.Orange) + "  " + EventText.Amounts(e.A, e.B), new Vector2(0f, -40f));
                        Fx.Play(FxCue.CounterPunch, hud.popLabel.transform, 1.2f);
                        Consider(Sfx.Coin, 7);
                        break;
                    case GameEventType.DoomApplied:
                        ToastView.Show(Loc.Get(who == null ? "game.doom_you" : "game.doom_other", "value", e.B, "player", who ?? string.Empty), ToastKind.Error);
                        effects.Flash(UIPalette.Red);
                        Consider(Sfx.Raid, 5);
                        break;
                    case GameEventType.MemeticsRepeat:
                        // Announced mid-sequence (ScoringSequence); without the sequence, a banner.
                        if (!m_AfterSequence)
                            banner.Show(Loc.Get("game.memetics_repeat"), Loc.Get("game.memetics_repeat_sub", "value", e.A), CommunityVisuals.Color(Community.MemeticsDivision), 1.4f);
                        Consider(Sfx.PartySuccess, 8);
                        break;
                    case GameEventType.BotsArrived:
                        ToastView.Show(GameIcons.Tag(Community.Bots) + " " + Loc.Get(who == null ? "game.bots_you" : "game.bots_other", "count", e.A, "player", who ?? string.Empty), ToastKind.Warning);
                        Consider(Sfx.Notify, 4);
                        break;
                    case GameEventType.BotsSent:
                        if (m_Net != null && who == null)
                            effects.FloatText(GameIcons.Tag(Community.Bots) + " " + GuestVisuals.Colored(Loc.Get("game.bots_sent", "count", e.A), CommunityVisuals.Color(Community.Bots)), new Vector2(0f, -90f), 40f);
                        break;

                    // --- V5 communities ---
                    case GameEventType.ActionRefreshed:
                    {
                        // Gatekeepers / Marketers / Voyeurs: the card's action lights up again.
                        var community = (Community)e.A;
                        m_Pulses.Add(e.GuestId);
                        effects.FloatText(GameIcons.Tag(community) + " " + GuestVisuals.Colored(Loc.Get("game.action_again"), CommunityVisuals.Color(community)), new Vector2(0f, -60f), 36f);
                        Consider(Sfx.Ability, 4);
                        break;
                    }
                    case GameEventType.SentBack:
                        m_Pulses.Add(e.GuestId);
                        Consider(Sfx.Boot, 4);
                        break;
                    case GameEventType.GenZGrowth:
                        if (who == null)
                            ToastView.Show(GameIcons.Tag(Community.GenZ) + " " + Loc.Get("game.genz_growth", "viewer", GuestVisuals.NameTag(e.Guest), "values", EventText.Amounts(e.A, e.B)), ToastKind.Success);
                        Consider(Sfx.Coin, 3);
                        break;
                    case GameEventType.ModsShield:
                        if (who == null)
                            effects.FloatText(GameIcons.Tag(Community.Mods) + " " + GuestVisuals.Colored(Loc.Get(e.A == 2 ? "game.mods_applied" : "game.mods_ready"), CommunityVisuals.Color(Community.Mods)), new Vector2(0f, -120f), 40f);
                        Consider(Sfx.Notify, 3);
                        break;
                    case GameEventType.BoomerRetired:
                        // Satire, not subtle: a Boomer leaves the Audience for good.
                        if (who == null)
                        {
                            ToastView.Show(GameIcons.Tag(Community.Boomer) + " " + Loc.Get("game.boomer_retired", "viewer", GuestVisuals.NameTag(e.Guest)), ToastKind.Warning);
                            effects.Flash(CommunityVisuals.Color(Community.Boomer));
                        }
                        Consider(Sfx.Defeat, 6);
                        break;
                    case GameEventType.Inheritance:
                        if (who == null)
                        {
                            ToastView.Show(GameIcons.Tag(Community.Millennial) + " " + Loc.Get("game.inheritance", "viewer", GuestVisuals.NameTag(e.Guest), "cash", GuestVisuals.CashText(e.A)), ToastKind.Success);
                            effects.FloatText(GameIcons.Tag(Community.Millennial) + " " + GuestVisuals.CashText(e.A), new Vector2(0f, -40f), 40f);
                        }
                        Consider(Sfx.Coin, 6);
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
                    case GameEventType.GuestDismissed:
                        if (who == null) ToastView.Show(Loc.Get("game.removed_you", "viewer", GuestVisuals.NameTag(e.Guest)), ToastKind.Info);
                        Consider(Sfx.Boot, 4);
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
                        Fx.Play(FxCue.RiskPip, hud.RiskAnchor, 1.5f);
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

        /// <summary>C11: Deplatformed! A strong shake, the strobing lights, a red flash and every card glitching.</summary>
        void Deplatformed()
        {
            Fx.Play(FxCue.Deplatform, Fx.ShakeRoot != null ? Fx.ShakeRoot : partyRoot.transform);
            Fx.Shake(24f, 0.5f);
            effects.Flash(UIPalette.Red);
            background.PoliceLights();
            AudioService.Duck(1.6f);
            foreach (var card in house.Cards)
                if (card.gameObject.activeSelf && card.Fx != null) card.Fx.Pulse(CardPulse.Glitch);
        }

        /// <summary>C12: Server Down (chat over the Sub Cap): cards vibrate and pixelate.</summary>
        void ServerDown()
        {
            Fx.Play(FxCue.ServerDown, Fx.ShakeRoot != null ? Fx.ShakeRoot : partyRoot.transform);
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
            var item = planning.FindItem(type);
            var from = item != null ? item.PortraitRect.position : planning.transform.position;
            // No names on screen: a "+1" flies to the Audience button (the bought viewer's name is in the tooltip and the log).
            effects.FlyChip(GuestVisuals.Colored("+1", GuestVisuals.Tint(type)), from, planning.RolodexButtonRect.position);
            if (item != null) Fx.Play(FxCue.Purchase, item.AnimRoot);
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
            bool restored = false;
            int restoreChance = 0;
            foreach (var e in events)
            {
                if (e.Type == GameEventType.GuestScored && e.C != 0) scored.Add(e);
                if (e.Type == GameEventType.PartyEnded) ended = true;
                if (e.Type == GameEventType.ServerRestored)
                {
                    restored = true;
                    restoreChance = e.A;
                }
            }
            if (!ended || scored.Count == 0) return false;
            // Techspertise saved a downed server: say so before the chat scores.
            if (restored) banner.Show(Loc.Get("game.server_restored"), Loc.Get("game.server_restored_sub", "value", restoreChance), UIPalette.Teal, 1.4f);
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
            int i = 0;
            // In engine order: each viewer's take, then the stream-wide community rewards, and the same again if the Memetics
            // Division made the stream score twice.
            foreach (var e in events)
            {
                if (m_SkipScoring) break;
                if (e.Type == GameEventType.MemeticsRepeat)
                {
                    banner.Show(Loc.Get("game.memetics_repeat"), Loc.Get("game.memetics_repeat_sub", "value", e.A), CommunityVisuals.Color(Community.MemeticsDivision), 1.2f);
                    Fx.Play(FxCue.BannerSlam, banner.transform);
                    effects.Flash(CommunityVisuals.Color(Community.MemeticsDivision));
                    AudioService.Play(Sfx.Notify);
                    yield return Wait(0.7f * AppSettings.AnimationTimeScale);
                    continue;
                }
                if (e.Type == GameEventType.CommunityReward)
                {
                    // Boomer, Millennial and Zoomer fly from their Community Beats chips.
                    var community = (Community)e.A;
                    var from = communityBeats != null ? communityBeats.ChipPosition(community) : null;
                    FlyResources(from ?? partyRoot.transform.position, e.B, e.C, false, GameIcons.Tag(community));
                    AudioService.PlayPitched(Sfx.Coin, 1.3f, 0.8f);
                    yield return Wait(step);
                    continue;
                }
                if (e.Type != GameEventType.GuestScored || e.C == 0) continue;

                var card = house.FindCard(e.GuestId);
                if (card != null)
                {
                    if (card.Portrait == null || !card.Portrait.Play(CharacterState.Score)) Fx.Play(FxCue.ScoreTick, card.AnimRoot);
                    else Fx.Play(FxCue.ScoreTick, card.AnimRoot, 0.6f);
                    if (card.Fx != null) card.Fx.Pulse(CardPulse.Shine, 0.7f);
                    // Each resource flies to its own icon in the menu bar: Clout to Clout, Cash to Cash, a Trophy to the Trophies.
                    // Community bonuses this viewer rolled in this scoring (twice, free, Cash doubled...) ride along on the first chip.
                    string tags = CommunityTags(events, e.GuestId, e.C == 2 ? 2 : 0);
                    var scorer = State != null ? View?.Find(e.GuestId) : null;
                    bool trophy = scorer != null ? scorer.Info.IsDiamond : GuestCatalog.Get(e.Guest).IsDiamond;
                    FlyResources(card.AnimRoot.position, e.A, e.B, trophy && e.C != 2, tags.Length > 0 ? tags : null);
                    if (tags.Length > 0 && card.Fx != null) card.Fx.Pulse(CardPulse.Shine);
                }
                AudioService.PlayPitched(Sfx.Coin, 1f + i * 0.06f, 0.8f);
                i++;
                yield return Wait(step);
            }

            if (!m_SkipScoring) yield return Wait(0.35f * AppSettings.AnimationTimeScale);

            m_Scoring = false;
            ApplyVenue(false);
            m_AfterSequence = true;
            try { ProcessEvents(after, events, ViewingOwn ? null : NameOf(m_ViewId)); }
            finally { m_AfterSequence = false; }
            // When the stream summary is about to open it says this (with the numbers), so the banner would only repeat it.
            if (!StreamRecapPending)
            {
                banner.Show(Loc.Get("result.success"), Loc.Get("game.scored_total", "subs", GuestVisuals.PopText(after.LastPop), "cash", GuestVisuals.CashText(after.LastCash)), UIPalette.Gold, 1.4f);
                Fx.Play(FxCue.BannerSlam, banner.transform);
            }
            RenderAll();
        }

        /// <summary>
        /// Sends a take up to the menu bar, each resource to its own icon: Clout to the Clout icon, Cash to the Cash icon and a
        /// Trophy (a Trophy viewer scoring) to the Trophy pips. <paramref name="prefix"/> (community tags or icon) rides on the
        /// first chip. Zero amounts don't fly; with nothing at all, a "+0" goes to Clout so the viewer still visibly scores.
        /// </summary>
        void FlyResources(Vector3 from, int clout, int cash, bool trophy, string prefix)
        {
            var cloutTarget = hud.popIcon != null ? hud.popIcon.transform.position : hud.popLabel.transform.position;
            var cashTarget = hud.cashIcon != null ? hud.cashIcon.transform.position : hud.cashLabel.transform.position;
            var trophyTarget = hud.starsMeter != null ? hud.starsMeter.position : cloutTarget;
            string lead = string.IsNullOrEmpty(prefix) ? string.Empty : prefix + "  ";
            bool any = false;
            // Fanned slightly so chips from the same viewer don't stack on top of each other.
            if (clout != 0 || (cash == 0 && !trophy))
            {
                effects.FlyChip(lead + GuestVisuals.PopText(clout), from, cloutTarget);
                lead = string.Empty;
                any = true;
            }
            if (cash != 0)
            {
                effects.FlyChip(lead + GuestVisuals.CashText(cash), from + new Vector3(0f, any ? -34f : 0f, 0f), cashTarget, any ? 0.06f : 0f);
                lead = string.Empty;
                any = true;
            }
            if (trophy)
                effects.FlyChip(lead + "+1 " + GameIcons.Tag(ResourceIcon.Plays), from + new Vector3(0f, any ? 34f : 0f, 0f), trophyTarget, any ? 0.12f : 0f);
        }

        /// <summary>
        /// The community bonus tags for one viewer's end-of-stream score ("[Content] x2", "[Staff] FREE", "[Clout] +1"...).
        /// <paramref name="pass"/>: 0 for the stream's scoring, 2 for the Memetics Division repeat.
        /// </summary>
        static string CommunityTags(IReadOnlyList<GameEvent> events, int guestId, int pass)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var e in events)
            {
                if (e.Type != GameEventType.CommunityBonus || e.GuestId != guestId || e.C != pass) continue;
                var community = (Community)e.A;
                string label;
                switch (community)
                {
                    case Community.CoreFanbase:
                    case Community.Content: label = Loc.Get("score.tag.encore"); break;
                    case Community.Staff: label = Loc.Get("score.tag.free"); break;
                    case Community.FinanceBros: label = Loc.Get("score.tag.cash_double"); break;
                    case Community.Doomsdayers: label = Loc.Get("score.tag.doomed"); break;
                    case Community.ArmchairActivists: label = Loc.Get("score.tag.all_talk"); break;
                    case Community.Clout: label = "+" + e.B; break;
                    default: continue;
                }
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(GameIcons.Tag(community)).Append(GuestVisuals.Colored(label, CommunityVisuals.Color(community)));
            }
            return sb.ToString();
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
