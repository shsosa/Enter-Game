#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FeelinSlimy.Editor
{
    [InitializeOnLoad]
    public static class SlimeArtWiring
    {
        public const string ArtFolder = "Assets/Art/Slime";
        private const string ManifestPath = ArtFolder + "/slime_animations.json";
        private const string PlayerPrefabPath = "Assets/Prefabs/SlimePlayer.prefab";

        [Serializable]
        private class Manifest
        {
            public ManifestClip[] animations = new ManifestClip[0];
        }

        [Serializable]
        private class ManifestClip
        {
            public string name = "";
            public float fps = 12f;
            public bool loop = true;
        }

        static SlimeArtWiring()
        {
            EditorApplication.delayCall += AutoWireIfNeeded;
            EditorSceneManager.sceneOpened += (_, _) => AutoWireIfNeeded();
        }

        private static bool ArtExists => File.Exists(ManifestPath);

        private static void AutoWireIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !ArtExists) return;

            bool needsWiring = UnityEngine.Object.FindObjectsByType<SlimePlayerController>(FindObjectsSortMode.None)
                .Any(p => !p.TryGetComponent(out SlimeSpriteAnimator _));
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            needsWiring |= prefab != null && !prefab.TryGetComponent(out SlimeSpriteAnimator _);

            if (needsWiring) WireAll();
        }

        public static void ScheduleRewire()
        {
            EditorApplication.delayCall -= RewireIfIdle;
            EditorApplication.delayCall += RewireIfIdle;
        }

        private static void RewireIfIdle()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && ArtExists) WireAll();
        }

        [MenuItem("Tools/Feelin' Slimy/Wire Animated Slime Sprites To Player")]
        public static void WireAll()
        {
            if (!ArtExists)
            {
                Debug.LogWarning("[Feelin' Slimy] No slime art found. Run: python Tools/SlimeArt/slime_pipeline.py");
                return;
            }

            EnsureImportSettings();
            Manifest manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));

            if (File.Exists(PlayerPrefabPath))
            {
                GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
                if (root.TryGetComponent(out SlimePlayerController prefabPlayer)) Wire(prefabPlayer, manifest);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                PrefabUtility.UnloadPrefabContents(root);
            }

            foreach (SlimePlayerController player in UnityEngine.Object.FindObjectsByType<SlimePlayerController>(FindObjectsSortMode.None))
            {
                Wire(player, manifest);
                EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
            }

            Sprite calmIdle = LoadFrames("Calm", "calm", "idle").FirstOrDefault();
            Sprite angryIdle = LoadFrames("Angry", "angry", "idle").FirstOrDefault();
            foreach (string guid in AssetDatabase.FindAssets("t:LevelSettings"))
            {
                LevelSettings settings = AssetDatabase.LoadAssetAtPath<LevelSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (settings == null) continue;
                settings.playerVisuals.customCalmSprite = calmIdle;
                settings.playerVisuals.customAngrySprite = angryIdle;
                EditorUtility.SetDirty(settings);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Feelin' Slimy] Animated Slime sprites wired to the player (scene + prefab).");
        }

        private static void Wire(SlimePlayerController player, Manifest manifest)
        {
            if (!player.TryGetComponent(out SlimeSpriteAnimator animator))
            {
                animator = player.gameObject.AddComponent<SlimeSpriteAnimator>();
            }

            Fill(animator.calm, "Calm", "calm", manifest);
            Fill(animator.angry, "Angry", "angry", manifest);

            player.tintSpriteWithSlimeColor = false;
            if (!animator.calm.idle.IsEmpty) player.customCalmSprite = animator.calm.idle.frames[0];
            if (!animator.angry.idle.IsEmpty) player.customAngrySprite = animator.angry.idle.frames[0];
            player.ApplyVisualState();

            EditorUtility.SetDirty(animator);
            EditorUtility.SetDirty(player);
        }

        private static void Fill(SlimeAnimationSet set, string folder, string palette, Manifest manifest)
        {
            foreach (SlimeAnim anim in Enum.GetValues(typeof(SlimeAnim)))
            {
                string clipName = ToSnakeCase(anim.ToString());
                SlimeClip clip = set.Get(anim);
                clip.frames = LoadFrames(folder, palette, clipName);

                ManifestClip info = manifest.animations.FirstOrDefault(c => c.name == clipName);
                if (info != null)
                {
                    clip.fps = info.fps;
                    clip.loop = info.loop;
                }
            }
        }

        private static Sprite[] LoadFrames(string folder, string palette, string clipName)
        {
            string dir = $"{ArtFolder}/{folder}";
            if (!Directory.Exists(dir)) return Array.Empty<Sprite>();

            Regex pattern = new Regex($"^slime_{palette}_{clipName}_\\d+\\.png$");
            return Directory.GetFiles(dir, "*.png")
                .Select(Path.GetFileName)
                .Where(file => pattern.IsMatch(file))
                .OrderBy(file => file, StringComparer.Ordinal)
                .Select(file => AssetDatabase.LoadAssetAtPath<Sprite>($"{dir}/{file}"))
                .Where(sprite => sprite != null)
                .ToArray();
        }

        private static void EnsureImportSettings()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(path) is TextureImporter importer && !SlimeArtImportSettings.IsConfigured(importer))
                {
                    SlimeArtImportSettings.Configure(importer);
                    importer.SaveAndReimport();
                }
            }
        }

        private static string ToSnakeCase(string name) => Regex.Replace(name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant();
    }
}
#endif
