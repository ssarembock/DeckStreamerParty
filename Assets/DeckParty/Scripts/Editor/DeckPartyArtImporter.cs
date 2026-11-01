using System;
using System.IO;
using System.Linq;
using UnityEditor;

namespace DeckParty.EditorTools
{
    /// <summary>
    /// Makes adding art a drag-and-drop job. Images dropped into Assets/DeckParty/Art/Characters, Streamers,
    /// Backgrounds or UI are imported as sprites (first import only; your own Inspector changes are kept afterwards),
    /// then the auto-assigner hooks them up to guests, streamers and venues by file name.
    /// </summary>
    public sealed class DeckPartyArtImporter : AssetPostprocessor
    {
        const string AutoAssignKey = "DeckParty.AutoAssignArt";
        const string MenuPath = "Deck Party/Art/Auto-Assign When Images Are Imported";
        static readonly string[] s_ImageExtensions = { ".png", ".jpg", ".jpeg", ".psd", ".tga", ".tif", ".tiff", ".bmp", ".gif", ".exr" };
        static bool s_Pending;

        static bool AutoAssign
        {
            get => EditorPrefs.GetBool(AutoAssignKey, true);
            set => EditorPrefs.SetBool(AutoAssignKey, value);
        }

        [MenuItem(MenuPath, priority = 40)]
        static void ToggleAutoAssign() => AutoAssign = !AutoAssign;

        [MenuItem(MenuPath, true)]
        static bool ToggleAutoAssignValidate()
        {
            Menu.SetChecked(MenuPath, AutoAssign);
            return true;
        }

        static bool IsArt(string path)
        {
            path = path.Replace('\\', '/');
            bool inFolder = path.StartsWith(DeckPartyPaths.ArtCharacters + "/", StringComparison.OrdinalIgnoreCase) ||
                            path.StartsWith(DeckPartyPaths.ArtStreamers + "/", StringComparison.OrdinalIgnoreCase) ||
                            path.StartsWith(DeckPartyPaths.ArtBackgrounds + "/", StringComparison.OrdinalIgnoreCase) ||
                            path.StartsWith(DeckPartyPaths.ArtUI + "/", StringComparison.OrdinalIgnoreCase);
            return inFolder && s_ImageExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());
        }

        void OnPreprocessTexture()
        {
            if (!IsArt(assetPath) || !assetImporter.importSettingsMissing) return;

            var importer = (TextureImporter)assetImporter;
            bool background = assetPath.Replace('\\', '/').StartsWith(DeckPartyPaths.ArtBackgrounds + "/", StringComparison.OrdinalIgnoreCase);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.maxTextureSize = background ? 4096 : 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (!AutoAssign || s_Pending) return;
            if (!imported.Concat(moved).Any(IsArt)) return;

            s_Pending = true;
            EditorApplication.delayCall += () =>
            {
                s_Pending = false;
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                SpriteAutoAssigner.AssignByName(true);
            };
        }
    }
}
