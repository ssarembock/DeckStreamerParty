using DeckParty.EditorTools;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using UnityEditor;
using UnityEngine;

namespace DeckParty.Juice.EditorTools
{
    /// <summary>
    /// Generates one Feel feedback prefab per polish moment (Assets/DeckParty/FX/Feedbacks/FX_&lt;Cue&gt;.prefab) and wires it
    /// into the FX Library. Every feedback targets its parent (automated target acquisition), runs on unscaled time and
    /// respects the intensity Deck Party passes in. Open a prefab to tweak it with Feel's inspector; Build Everything never
    /// overwrites your tweaks (Deck Party > FX > Rebuild FX does).
    /// </summary>
    [InitializeOnLoad]
    static class FeelFxBuilder
    {
        /// <summary>
        /// Bump when a recipe below is fixed: the cues in <see cref="s_Revised"/> are then regenerated once by the next
        /// Build Everything / Rebuild, even though prefabs are normally never overwritten.
        /// Revision 1: Power Used ended 20 degrees rotated (an additive spin that never came back), which left the shop's
        /// streamer card tilted for the rest of the match, on every player's board.
        /// </summary>
        const int RecipeRevision = 1;
        static readonly FxCue[] s_Revised = { FxCue.PowerUsed };

        static FeelFxBuilder()
        {
            FxSetup.FeelBuilder = Build;
        }

        static void Build(FxLibrary library, bool overwrite)
        {
            bool revise = library.recipeRevision < RecipeRevision;
            foreach (var entry in library.cues)
            {
                if (entry == null) continue;
                string path = $"{FxSetup.Folder}/Feedbacks/FX_{entry.cue}.prefab";
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                bool stale = revise && System.Array.IndexOf(s_Revised, entry.cue) >= 0;
                if (existing != null && !overwrite && !stale)
                {
                    if (entry.feedback == null) entry.feedback = existing;
                    continue;
                }

                var go = new GameObject("FX_" + entry.cue);
                var player = go.AddComponent<MMF_Player>();
                player.InitializationMode = MMFeedbacks.InitializationModes.Script;
                player.ForceTimescaleMode = true;
                player.ForcedTimescaleMode = TimescaleModes.Unscaled;
                Compose(player, entry.cue);
                entry.feedback = PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
            }
            library.recipeRevision = RecipeRevision;
            EditorUtility.SetDirty(library);
            Debug.Log("[DeckParty] Feel feedback prefabs are in " + FxSetup.Folder + "/Feedbacks.");
        }

        // ------------------------------------------------------------------ recipes

        static void Compose(MMF_Player p, FxCue cue)
        {
            switch (cue)
            {
                case FxCue.ViewerJoined:
                    Pop(p, 0.35f, 0.35f, 1.12f);
                    Squash(p, 0.18f, 0.28f);
                    break;
                case FxCue.DiamondJoined:
                    Pop(p, 0.2f, 0.45f, 1.22f);
                    Wobble(p, 7f, 0.5f, 0.1f);
                    Squash(p, 0.22f, 0.42f);
                    break;
                case FxCue.CardHover:
                    Punch(p, 0.06f, 0.18f);
                    break;
                case FxCue.ButtonPress:
                    Punch(p, -0.08f, 0.16f);
                    break;
                case FxCue.ButtonDenied:
                    Shake(p, 9f, 0f, 0.25f);
                    break;
                case FxCue.AbilityUsed:
                    Squash(p, 0.25f, 0f);
                    Punch(p, 0.18f, 0.3f, 0.12f);
                    break;
                case FxCue.ImpactRecruit:
                    Pop(p, 0.6f, 0.3f, 1.1f);
                    break;
                case FxCue.ImpactRemove:
                    Shake(p, 12f, 4f, 0.32f);
                    Wobble(p, 9f, 0.32f);
                    break;
                case FxCue.ImpactModify:
                    Punch(p, 0.16f, 0.3f);
                    break;
                case FxCue.ImpactShuffle:
                    Spin(p, 360f, 0.45f);
                    break;
                case FxCue.ImpactScout:
                    Punch(p, 0.1f, 0.25f);
                    Wobble(p, 4f, 0.3f);
                    break;
                case FxCue.ImpactEconomy:
                    Punch(p, 0.2f, 0.3f);
                    break;
                case FxCue.CounterPunch:
                case FxCue.Purchase:
                    Punch(p, 0.22f, 0.25f);
                    break;
                case FxCue.RiskPip:
                    Punch(p, 0.35f, 0.3f);
                    Wobble(p, 10f, 0.35f);
                    break;
                case FxCue.SubCapFull:
                    Shake(p, 6f, 0f, 0.3f);
                    break;
                case FxCue.DiamondTracker:
                    Punch(p, 0.4f, 0.4f);
                    break;
                case FxCue.PlatformBan:
                    Shake(p, 26f, 18f, 0.55f);
                    break;
                case FxCue.ServerDown:
                    Shake(p, 18f, 10f, 0.45f);
                    Wobble(p, 3f, 0.45f);
                    break;
                case FxCue.ScoreTick:
                    Punch(p, 0.2f, 0.22f);
                    break;
                case FxCue.Victory:
                    Pop(p, 0.5f, 0.5f, 1.2f);
                    Wobble(p, 5f, 0.6f, 0.2f);
                    break;
                case FxCue.Defeat:
                    Punch(p, -0.1f, 0.8f);
                    break;
                case FxCue.TurnBanner:
                case FxCue.BannerSlam:
                    Slam(p, 1.6f, 0.28f);
                    break;
                case FxCue.Stamp:
                    Slam(p, 2.2f, 0.16f);
                    break;
                case FxCue.BoardSwitch:
                    Pop(p, 0.94f, 0.3f, 1.01f);
                    Shake(p, 18f, 0f, 0.3f);
                    break;
                case FxCue.ToastIn:
                case FxCue.MenuIn:
                    Pop(p, 0.85f, 0.25f, 1.04f);
                    break;
                case FxCue.StreamerAnxious:
                    Shake(p, 6f, 2f, 0.3f);
                    Wobble(p, 4f, 0.3f);
                    break;
                case FxCue.PowerUsed:
                    // Everything here must come back to rest: the streamer card is one object shown on every player's board.
                    Pop(p, 0.7f, 0.35f, 1.15f);
                    Wobble(p, 8f, 0.45f, 0.05f);
                    break;
                case FxCue.ScreenShake:
                    Shake(p, 16f, 12f, 0.35f);
                    break;
                case FxCue.SelectConfirm:
                    Punch(p, 0.3f, 0.35f);
                    Wobble(p, 6f, 0.4f);
                    break;
                default:
                    Punch(p, 0.15f, 0.28f);
                    break;
            }
        }

        // ------------------------------------------------------------------ building blocks

        static T Add<T>(MMF_Player p, T feedback, string label, float delay = 0f) where T : MMF_Feedback
        {
            feedback.Label = label;
            feedback.Timing = new MMFeedbackTiming { TimescaleMode = TimescaleModes.Unscaled, InitialDelay = delay };
            feedback.AutomatedTargetAcquisition = new MMFeedbackTargetAcquisition { Mode = MMFeedbackTargetAcquisition.Modes.Parent };
            p.AddFeedback(feedback);
            return feedback;
        }

        static AnimationCurve Curve(params float[] keys)
        {
            var frames = new Keyframe[keys.Length / 2];
            for (int i = 0; i < frames.Length; i++) frames[i] = new Keyframe(keys[i * 2], keys[i * 2 + 1]);
            var curve = new AnimationCurve(frames);
            for (int i = 0; i < curve.length; i++) curve.SmoothTangents(i, 0f);
            return curve;
        }

        static readonly AnimationCurve s_ShakeCurve = Curve(0f, 0f, 0.1f, 1f, 0.25f, -0.9f, 0.4f, 0.7f, 0.55f, -0.5f, 0.7f, 0.3f, 0.85f, -0.15f, 1f, 0f);
        static readonly AnimationCurve s_ShakeCurveB = Curve(0f, 0f, 0.12f, -0.8f, 0.3f, 1f, 0.45f, -0.6f, 0.62f, 0.5f, 0.8f, -0.25f, 1f, 0f);

        /// <summary>Scale punch: grows by <paramref name="amount"/> and springs back.</summary>
        static void Punch(MMF_Player p, float amount, float duration, float delay = 0f)
        {
            var f = Add(p, new MMF_Scale(), "Punch", delay);
            f.Mode = MMF_Scale.Modes.Additive;
            f.AnimateScaleDuration = duration;
            f.UniformScaling = true;
            f.RemapCurveZero = 0f;
            f.RemapCurveOne = amount;
            f.AnimateScaleTweenX = new MMTweenType(Curve(0f, 0f, 0.3f, 1f, 0.65f, -0.25f, 1f, 0f));
        }

        /// <summary>Scale pop-in from <paramref name="from"/> with an overshoot to <paramref name="overshoot"/>.</summary>
        static void Pop(MMF_Player p, float from, float duration, float overshoot)
        {
            var f = Add(p, new MMF_Scale(), "Pop In");
            f.Mode = MMF_Scale.Modes.Absolute;
            f.AnimateScaleDuration = duration;
            f.UniformScaling = true;
            f.RemapCurveZero = from;
            f.RemapCurveOne = 1f;
            float peak = (overshoot - from) / Mathf.Max(0.001f, 1f - from);
            f.AnimateScaleTweenX = new MMTweenType(Curve(0f, 0f, 0.6f, peak, 1f, 1f));
        }

        /// <summary>Slams in from a larger size (banners, stamps).</summary>
        static void Slam(MMF_Player p, float from, float duration)
        {
            var f = Add(p, new MMF_Scale(), "Slam");
            f.Mode = MMF_Scale.Modes.Absolute;
            f.AnimateScaleDuration = duration;
            f.UniformScaling = true;
            f.RemapCurveZero = from;
            f.RemapCurveOne = 1f;
            f.AnimateScaleTweenX = new MMTweenType(Curve(0f, 0f, 0.7f, 1.05f, 1f, 1f));
        }

        /// <summary>Squash and stretch (landing / wind-up).</summary>
        static void Squash(MMF_Player p, float duration, float delay)
        {
            var f = Add(p, new MMF_SquashAndStretch(), "Squash", delay);
            f.Mode = MMF_SquashAndStretch.Modes.Absolute;
            f.Axis = MMF_SquashAndStretch.PossibleAxis.YtoX;
            f.AnimateScaleDuration = duration;
            f.RemapCurveZero = 1f;
            f.RemapCurveOne = 1.18f;
            f.AnimateCurve = Curve(0f, 0f, 0.25f, -0.6f, 0.6f, 0.4f, 1f, 0f);
        }

        /// <summary>Position shake in local space (X and optionally Y), in pixels.</summary>
        static void Shake(MMF_Player p, float x, float y, float duration)
        {
            var f = Add(p, new MMF_Position(), "Shake");
            f.Mode = MMF_Position.Modes.AlongCurve;
            f.Space = MMF_Position.Spaces.Local;
            f.AnimatePositionDuration = duration;
            f.RelativePosition = true;
            f.RemapCurveZero = 0f;
            f.RemapCurveOne = x;
            f.AnimateX = x > 0f;
            f.AnimatePositionTweenX = new MMTweenType(s_ShakeCurve);
            f.AnimateY = y > 0f;
            f.AnimatePositionTweenY = new MMTweenType(s_ShakeCurveB);
            if (y > 0f && x > 0f)
            {
                // One remap range drives both axes: scale the Y curve so its amplitude is y.
                float ratio = y / x;
                var scaled = new AnimationCurve();
                foreach (var k in s_ShakeCurveB.keys) scaled.AddKey(new Keyframe(k.time, k.value * ratio, k.inTangent * ratio, k.outTangent * ratio));
                f.AnimatePositionTweenY = new MMTweenType(scaled);
            }
            f.AnimateZ = false;
        }

        /// <summary>Rotation wobble around Z, in degrees.</summary>
        static void Wobble(MMF_Player p, float degrees, float duration, float delay = 0f)
        {
            var f = Add(p, new MMF_Rotation(), "Wobble", delay);
            f.Mode = MMF_Rotation.Modes.Additive;
            f.RotationSpace = Space.Self;
            f.AnimateRotationDuration = duration;
            f.RemapCurveZero = 0f;
            f.RemapCurveOne = degrees;
            f.AnimateX = false;
            f.AnimateY = false;
            f.AnimateZ = true;
            f.AnimateRotationTweenZ = new MMTweenType(s_ShakeCurve);
        }

        /// <summary>A full (or partial) spin around Z.</summary>
        static void Spin(MMF_Player p, float degrees, float duration)
        {
            var f = Add(p, new MMF_Rotation(), "Spin");
            f.Mode = MMF_Rotation.Modes.Additive;
            f.RotationSpace = Space.Self;
            f.AnimateRotationDuration = duration;
            f.RemapCurveZero = 0f;
            f.RemapCurveOne = degrees;
            f.AnimateX = false;
            f.AnimateY = false;
            f.AnimateZ = true;
            f.AnimateRotationTweenZ = new MMTweenType(Curve(0f, 0f, 1f, 1f));
        }
    }
}
