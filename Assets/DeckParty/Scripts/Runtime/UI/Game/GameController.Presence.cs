using System.Collections.Generic;
using DeckParty.Core;
using DeckParty.Networking;
using UnityEngine;

namespace DeckParty.UI
{
    /// <summary>
    /// Multiplayer presence: the active player's game reports what they point at and choose (never game state), the host
    /// relays it, and everyone watching sees it in that player's colour. Plus emotes, rival-power callouts and the turn timer.
    /// Updates can lag up to a second without harm (the game is turn-based); the state itself always comes from the host.
    /// </summary>
    public sealed partial class GameController
    {
        const float PresenceInterval = 0.12f;
        const float CursorInterval = 0.2f;

        readonly Dictionary<ulong, PresenceMessage> m_Presence = new Dictionary<ulong, PresenceMessage>();
        PresenceMessage m_LastSent = PresenceMessage.None;
        float m_LastSentAt;
        float m_LastCursorAt;
        int m_HoverCard = -1;
        int m_HoverShop = -1;
        PresenceButton m_HoverButton;
        int m_HoverPicker = -1;
        int m_ChoosingActor = -1;
        int m_PickingActor = -1;
        bool m_ChoosingRival;

        void HookPresenceSources()
        {
            house.CardHovered += (card, on) => m_HoverCard = on && !card.IsEmpty ? card.GuestId : (m_HoverCard == card.GuestId ? -1 : m_HoverCard);
            planning.ShopHovered += type => m_HoverShop = type.HasValue ? (int)type.Value : -1;
            picker.CardHovered += id => m_HoverPicker = id;

            void Button(Component target, PresenceButton id)
            {
                var relay = HoverRelay.On(target);
                if (relay != null) relay.Hovered += on => m_HoverButton = on ? id : (m_HoverButton == id ? PresenceButton.None : m_HoverButton);
            }
            Button(partyControls.doorButton, PresenceButton.AddViewer);
            Button(partyControls.endButton, PresenceButton.EndStream);
            Button(partyControls.admitButton, PresenceButton.Approve);
            Button(partyControls.rejectButton, PresenceButton.Deny);
            Button(planning.expandButton, PresenceButton.RaiseSubCap);
            Button(planning.nextNightButton, PresenceButton.EndTurn);
            Button(planning.rolodexButton, PresenceButton.Audience);
            if (planning.power != null && planning.power.useButton != null) Button(planning.power.useButton, PresenceButton.Power);
        }

        /// <summary>What the local player is doing right now, as a presence message.</summary>
        PresenceMessage ComposePresence()
        {
            var msg = PresenceMessage.None;
            var s = State;
            if (s == null) return msg;

            if (m_TargetActor >= 0)
            {
                msg.Kind = PresenceKind.Targeting;
                msg.A = m_TargetActor;
                msg.B = m_HoverCard;
                msg.Pick0 = m_Picks.Count > 0 ? m_Picks[0] : -1;
                msg.Pick1 = m_Picks.Count > 1 ? m_Picks[1] : -1;
                msg.Pick2 = m_Picks.Count > 2 ? m_Picks[2] : -1;
            }
            else if (m_AttractionChoice >= 0)
            {
                msg.Kind = PresenceKind.Targeting;
                msg.A = s.AttractionActor;
                msg.B = m_HoverCard;
                msg.Pick0 = m_AttractionChoice;
            }
            else if (picker.IsOpen)
            {
                msg.Kind = PresenceKind.Picking;
                msg.A = m_PickingActor;
                msg.B = m_HoverPicker;
            }
            else if (m_ChoosingRival)
            {
                msg.Kind = PresenceKind.ChoosingRival;
            }
            else if (m_ChoosingActor >= 0 && ModalDialog.IsOpen)
            {
                msg.Kind = PresenceKind.Choosing;
                msg.A = m_ChoosingActor;
            }
            else if (m_HoverButton != PresenceButton.None)
            {
                msg.Kind = PresenceKind.HoverButton;
                msg.A = (int)m_HoverButton;
            }
            else if (s.Phase == GamePhase.Planning && m_HoverShop >= 0)
            {
                msg.Kind = PresenceKind.HoverShop;
                msg.A = m_HoverShop;
            }
            else if (s.Phase == GamePhase.Party && m_HoverCard >= 0)
            {
                msg.Kind = PresenceKind.HoverCard;
                msg.A = m_HoverCard;
            }
            else if (s.Phase == GamePhase.Planning)
            {
                msg.Kind = PresenceKind.Browsing;
            }

            if (FxLibrary.Current.ghostCursor)
            {
                PresenceView.BoardCursor(BoardRect, InputHelper.PointerPosition, out var x, out var y);
                msg.CursorX = x;
                msg.CursorY = y;
            }
            return msg;
        }

        RectTransform BoardRect => partyRoot.transform.parent as RectTransform;

        void UpdatePresence()
        {
            if (m_Net == null || !m_Net.IsSpawned || m_Net.Phase != MatchPhase.InGame) return;
            // Only the player who can act has intent worth showing (in Take Turns, the active player).
            if (!CanAct)
            {
                if (m_LastSent.Kind != PresenceKind.None) Send(PresenceMessage.None);
                return;
            }

            var msg = ComposePresence();
            float now = Time.unscaledTime;
            bool intentChanged = !msg.SameIntent(m_LastSent);
            bool cursorMoved = msg.CursorX != m_LastSent.CursorX || msg.CursorY != m_LastSent.CursorY;
            if (intentChanged && now - m_LastSentAt >= PresenceInterval) Send(msg);
            else if (!intentChanged && cursorMoved && now - m_LastCursorAt >= CursorInterval) Send(msg);
        }

        void Send(PresenceMessage msg)
        {
            m_LastSent = msg;
            m_LastSentAt = m_LastCursorAt = Time.unscaledTime;
            m_Net.SendPresence(msg);
        }

        void OnPresenceReceived(ulong sender, PresenceMessage msg)
        {
            m_Presence[sender] = msg;
            if (sender == m_ViewId && sender != LocalId) RenderPresence(sender, msg);
            if (opponents != null && opponents.gameObject.activeInHierarchy)
            {
                var row = opponents.RowFor(sender);
                if (row != null) row.SetPresence(PresenceLine(sender));
            }
        }

        void RenderPresence(ulong owner, PresenceMessage msg)
        {
            if (presence == null || owner == LocalId) return;
            presence.Render(msg, ColorOf(owner), NameOf(owner),
                id =>
                {
                    var card = house.FindCard(id);
                    if (card == null && picker.IsOpen) card = picker.FindCard(id);
                    return card != null ? card.AnimRoot : null;
                },
                type =>
                {
                    var item = planning.gameObject.activeInHierarchy ? planning.FindItem(type) : null;
                    return item != null ? (RectTransform)item.transform : null;
                },
                PresenceButtonRect, BoardRect);
        }

        RectTransform PresenceButtonRect(PresenceButton button)
        {
            switch (button)
            {
                case PresenceButton.AddViewer: return (RectTransform)partyControls.doorButton.transform;
                case PresenceButton.EndStream: return (RectTransform)partyControls.endButton.transform;
                case PresenceButton.Approve: return (RectTransform)partyControls.admitButton.transform;
                case PresenceButton.Deny: return (RectTransform)partyControls.rejectButton.transform;
                case PresenceButton.RaiseSubCap: return planning.ExpandButtonRect;
                case PresenceButton.EndTurn: return planning.NextNightButtonRect;
                case PresenceButton.Audience: return planning.RolodexButtonRect;
                case PresenceButton.Power: return planning.PowerButtonRect;
                default: return null;
            }
        }

        /// <summary>The live status line under a player in the Players panel ("choosing Trollface's target..."), or null.</summary>
        string PresenceLine(ulong clientId)
        {
            // Free For All: nobody watches anyone else, so nothing about what they're doing right now.
            if (clientId == LocalId || FreeForAll || !m_Presence.TryGetValue(clientId, out var msg)) return null;
            var s = m_Net != null ? m_Net.GetState(clientId) : null;
            if (s == null) return null;
            string Viewer(int id) => s.Find(id) is GuestInstance g ? GuestVisuals.DisplayName(g.Kind) : Loc.Get("common.someone");
            const string dots = "...";

            switch (msg.Kind)
            {
                case PresenceKind.HoverCard: return Loc.Get("presence.eyeing", "viewer", Viewer(msg.A));
                case PresenceKind.HoverShop:
                    return GuestCatalog.IsValid((GuestType)msg.A) ? Loc.Get("presence.considering", "viewer", GuestVisuals.DisplayName((GuestType)msg.A)) : null;
                case PresenceKind.Targeting: return Loc.Get("presence.targeting", "viewer", Viewer(msg.A)) + dots;
                case PresenceKind.Choosing: return Loc.Get("presence.choosing", "viewer", Viewer(msg.A)) + dots;
                case PresenceKind.Picking: return Loc.Get("presence.picking") + dots;
                case PresenceKind.Browsing: return Loc.Get("presence.browsing") + dots;
                case PresenceKind.ChoosingRival: return Loc.Get("presence.rival") + dots;
                case PresenceKind.HoverButton:
                    switch ((PresenceButton)msg.A)
                    {
                        case PresenceButton.AddViewer: return Loc.Get("presence.add_viewer") + dots;
                        case PresenceButton.EndStream: return Loc.Get("presence.end_stream") + dots;
                        case PresenceButton.Approve:
                        case PresenceButton.Deny: return Loc.Get("presence.deciding") + dots;
                        case PresenceButton.RaiseSubCap: return Loc.Get("presence.sub_cap") + dots;
                        case PresenceButton.EndTurn: return Loc.Get("presence.end_turn") + dots;
                        case PresenceButton.Power: return Loc.Get("presence.power") + dots;
                        case PresenceButton.Audience: return Loc.Get("presence.audience") + dots;
                    }
                    break;
            }
            return null;
        }

        /// <summary>Every player sees the emote pop up from the sender's avatar in the Players panel.</summary>
        void OnEmoteReceived(ulong sender, int index)
        {
            if (emotes == null || !FxLibrary.Current.emotes) return;
            var row = opponents != null && opponents.gameObject.activeInHierarchy ? opponents.RowFor(sender) : null;
            emotes.Show(NameOf(sender), ColorOf(sender), index, row != null ? row.AvatarRect : null);
        }

        void OnRivalPowerUsed(ulong actor, ulong target, StreamerType streamer)
        {
            // Everyone sees the hit; the target gets the big callout.
            string power = StreamerVisuals.PowerName(streamer);
            if (target == LocalId)
            {
                banner.Show(Loc.Get(streamer == StreamerType.Fate ? "power.raided_title" : "power.taxed_title"),
                    Loc.Get("power.hit_sub", "player", NameOf(actor), "power", power), StreamerVisuals.Color(streamer), 1.6f);
                Fx.Shake(10f, 0.35f);
                AudioService.Play(Sfx.Raid, 0.6f);
            }
            else
            {
                ToastView.Show(Loc.Get("power.used_toast", "player", NameOf(actor), "power", power), ToastKind.Info);
            }
        }

        void UpdateTurnTimer()
        {
            if (turnTimer == null) return;
            if (m_Net == null || !m_Net.IsSpawned || !TakingTurns || m_Net.Phase != MatchPhase.InGame)
            {
                turnTimer.Render(-1f, null, false);
                return;
            }
            ulong active = m_Net.ActivePlayer;
            turnTimer.Render(active == DeckPartyNetSession.NoPlayer ? -1f : m_Net.TurnSecondsLeft, NameOf(active), active == LocalId);
        }
    }
}
