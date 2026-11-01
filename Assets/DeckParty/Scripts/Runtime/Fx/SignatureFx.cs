using System.Collections.Generic;
using DeckParty.Core;
using DeckParty.UI;
using UnityEngine;

namespace DeckParty
{
    /// <summary>
    /// Signature effects (nice-to-have N1) and comic stamps (N13): every viewer action and arrival special gets its own
    /// shader pulse on the target, an optional shake and a localized stamp ("stamp.&lt;GuestType&gt;" keys).
    /// Edit the table below to retune a character; switch the features off in the FX Library.
    /// </summary>
    public static class SignatureFx
    {
        public struct Signature
        {
            public CardPulse? Target;
            public CardPulse? Actor;
            public float Shake;
            public Color Color;
        }

        static Signature S(CardPulse? target, CardPulse? actor, float shake, Color color) =>
            new Signature { Target = target, Actor = actor, Shake = shake, Color = color };

        static readonly Dictionary<GuestType, Signature> s_Table = new Dictionary<GuestType, Signature>
        {
            { GuestType.Producer, S(CardPulse.GlowIn, CardPulse.Shine, 0f, UIPalette.Teal) },
            { GuestType.ChatAdmin, S(CardPulse.Burn, null, 6f, UIPalette.Red) },
            { GuestType.JuniorPrManager, S(null, CardPulse.Shine, 4f, UIPalette.Gold) },
            { GuestType.PrManager, S(null, CardPulse.Shine, 4f, UIPalette.Gold) },
            { GuestType.CybersecurityEngineer, S(null, CardPulse.Hologram, 0f, UIPalette.Teal) },
            { GuestType.SocialMediaManager, S(null, CardPulse.Hologram, 0f, UIPalette.Blue) },
            { GuestType.CryptoGuy, S(CardPulse.Shine, CardPulse.Glitch, 2f, UIPalette.Gold) },
            { GuestType.ChatArbitrator, S(null, CardPulse.Frozen, 0f, UIPalette.Teal) },
            { GuestType.BotDetector, S(CardPulse.Glitch, null, 6f, UIPalette.Red) },
            { GuestType.BillionaireBenefactor, S(null, CardPulse.Shine, 0f, UIPalette.Green) },
            { GuestType.TechAdmin, S(CardPulse.Burn, null, 6f, UIPalette.Red) },
            { GuestType.Affiliate, S(CardPulse.Shine, null, 0f, UIPalette.Green) },
            { GuestType.Trollface, S(CardPulse.Glitch, null, 3f, UIPalette.Pink) },
            { GuestType.Pepe, S(null, CardPulse.Poison, 0f, UIPalette.Green) },
            { GuestType.GrumpyCat, S(null, CardPulse.Frozen, 0f, UIPalette.Orange) },
            { GuestType.StonksGuy, S(null, CardPulse.Shine, 0f, UIPalette.Green) },
            { GuestType.ChadVsVirgin, S(CardPulse.Vibrate, CardPulse.Shine, 3f, UIPalette.Teal) },
            { GuestType.KeyboardCat, S(CardPulse.Burn, null, 2f, UIPalette.Orange) },
            { GuestType.DisasterGirl, S(CardPulse.Burn, null, 3f, UIPalette.Orange) },
            { GuestType.SuccessKid, S(CardPulse.Shine, CardPulse.Shine, 0f, UIPalette.Gold) },
            { GuestType.TungTungTungSahur, S(CardPulse.Vibrate, null, 8f, UIPalette.Orange) },
            { GuestType.TralaleroTralala, S(CardPulse.Hologram, null, 2f, UIPalette.Blue) },
            { GuestType.BombardiroCrocodilo, S(CardPulse.Burn, null, 12f, UIPalette.Red) },
            { GuestType.BallerinaCappuccina, S(CardPulse.Shine, null, 0f, UIPalette.Pink) },
            { GuestType.Karen, S(CardPulse.Burn, null, 4f, UIPalette.Red) },
            { GuestType.Rickroll, S(CardPulse.Glitch, null, 2f, UIPalette.Purple) },
            { GuestType.Drakeposting, S(CardPulse.Shine, null, 0f, UIPalette.Gold) },
            { GuestType.ConfusedMathLady, S(CardPulse.Hologram, null, 0f, UIPalette.Purple) },
            { GuestType.SideEyeChloe, S(null, CardPulse.Hologram, 0f, UIPalette.TextDim) },
            { GuestType.RollSafe, S(CardPulse.Frozen, null, 0f, UIPalette.Teal) },
            { GuestType.PlatformOwner, S(CardPulse.Burn, null, 10f, UIPalette.Red) },
            { GuestType.SurpriseGuest, S(CardPulse.GlowIn, CardPulse.Shine, 0f, UIPalette.Gold) },
            // Arrival specials (no action button)
            { GuestType.GenerousDonator, S(CardPulse.Shine, CardPulse.Shine, 0f, UIPalette.Gold) },
            { GuestType.HateWatcher, S(null, CardPulse.Burn, 2f, UIPalette.Red) },
            { GuestType.SkibidiToilet, S(CardPulse.Poison, null, 2f, UIPalette.Green) },
            { GuestType.NpcWojak, S(null, CardPulse.Hologram, 0f, UIPalette.TextDim) },
            { GuestType.OverlyAttachedGirlfriend, S(CardPulse.Shine, null, 0f, UIPalette.Pink) },
            { GuestType.UgandanKnuckles, S(null, CardPulse.Hologram, 0f, UIPalette.Red) },
            { GuestType.DistractedBoyfriend, S(null, CardPulse.Shine, 0f, UIPalette.Pink) },
            { GuestType.ThisIsFineDog, S(null, CardPulse.Burn, 6f, UIPalette.Orange) },
        };

        public static bool TryGet(GuestType type, out Signature signature) => s_Table.TryGetValue(type, out signature);

        /// <summary>Plays the signature for one engine event on the board being shown.</summary>
        public static void OnEvent(GameEvent e, HouseView house, EffectsView effects)
        {
            var fx = FxLibrary.Current;
            if (!fx.signatureEffects && !fx.comicStamps) return;

            GuestType? actorType = null;
            int actorId = -1;
            var targets = new List<int>(3);
            switch (e.Type)
            {
                case GameEventType.AbilityUsed:
                    actorType = e.Guest;
                    actorId = e.GuestId;
                    if (e.A >= 0) targets.Add(e.A);
                    if (e.B >= 0) targets.Add(e.B);
                    break;
                case GameEventType.Infected:
                    actorType = GuestType.SkibidiToilet;
                    targets.Add(e.GuestId);
                    break;
                case GameEventType.StatsCopied:
                case GameEventType.Attached:
                case GameEventType.AttractionRevealed:
                case GameEventType.ThisIsFine:
                    actorType = e.Guest;
                    actorId = e.GuestId;
                    if (e.Type == GameEventType.Attached || e.Type == GameEventType.StatsCopied) targets.Add(e.A);
                    break;
                case GameEventType.TemporaryCopy:
                    actorType = GuestType.UgandanKnuckles;
                    break;
                case GameEventType.MoodFlipped:
                    // Only the switch into hate-watching gets a signature.
                    if (e.A == 0) return;
                    actorType = e.Guest;
                    actorId = e.GuestId;
                    break;
                case GameEventType.ScoredNow:
                {
                    // Generous Donator scoring the viewer it brought in (Crypto Guy's own action already played).
                    var source = e.C >= 0 ? house.FindCard(e.C) : null;
                    if (source == null || source.ShownType != GuestType.GenerousDonator) return;
                    actorType = GuestType.GenerousDonator;
                    actorId = e.C;
                    targets.Add(e.GuestId);
                    break;
                }
                default:
                    return;
            }
            if (actorType == null || !TryGet(actorType.Value, out var sig)) return;

            var actorCard = actorId >= 0 ? house.FindCard(actorId) : null;
            if (fx.signatureEffects)
            {
                if (sig.Actor.HasValue && actorCard != null && actorCard.Fx != null) actorCard.Fx.Pulse(sig.Actor.Value);
                if (sig.Target.HasValue)
                    foreach (var id in targets)
                    {
                        var card = house.FindCard(id);
                        if (card != null && card.Fx != null) card.Fx.Pulse(sig.Target.Value);
                    }
                if (sig.Shake > 0f) Fx.Shake(sig.Shake, 0.3f);
            }

            if (fx.comicStamps && effects != null)
            {
                string key = $"stamp.{actorType.Value}";
                if (!Loc.Has(key)) return;
                var anchor = targets.Count > 0 ? house.FindCard(targets[0]) : actorCard;
                Vector3 at = anchor != null ? anchor.AnimRoot.position : house.transform.position;
                effects.Stamp(Loc.Get(key), at, sig.Color);
            }
        }
    }
}
