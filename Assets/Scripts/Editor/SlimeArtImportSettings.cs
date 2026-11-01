#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FeelinSlimy.Editor
{
    public class SlimeArtImportSettings : AssetPostprocessor
    {
        private const string SlimeArtFolder = SlimeArtWiring.ArtFolder + "/";
        private const int PixelsPerUnit = 64;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SlimeArtFolder)) return;
            Configure((TextureImporter)assetImporter);
        }

        public static void Configure(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }

        public static bool IsConfigured(TextureImporter importer) =>
            importer.textureType == TextureImporterType.Sprite
            && importer.spriteImportMode == SpriteImportMode.Single
            && Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit)
            && importer.filterMode == FilterMode.Point
            && !importer.mipmapEnabled
            && importer.textureCompression == TextureImporterCompression.Uncompressed;

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (imported.Concat(deleted).Any(path => path.StartsWith(SlimeArtFolder) && path.EndsWith(".png")))
            {
                SlimeArtWiring.ScheduleRewire();
            }
        }
    }
}
#endif
