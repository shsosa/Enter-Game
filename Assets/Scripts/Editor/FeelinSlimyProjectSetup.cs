#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FeelinSlimy.Editor
{
    [InitializeOnLoad]
    public static class FeelinSlimyProjectSetup
    {
        private const string SetupMarkerPath = "Assets/Prefabs/SlimePlayer.prefab";

        static FeelinSlimyProjectSetup()
        {
            EditorApplication.delayCall += AutoInitializeIfNeeded;
        }

        private static void AutoInitializeIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            bool missingPrefabs = !File.Exists(SetupMarkerPath);
            bool missingRoomInActiveScene = Object.FindFirstObjectByType<SlimePlayerController>() == null;

            if (missingPrefabs || missingRoomInActiveScene)
            {
                BuildProjectAssetsAndScenes();
            }
        }

        [MenuItem("Tools/Feelin' Slimy/Rebuild Test Room, Prefabs & Starter Copy")]
        public static void BuildProjectAssetsAndScenes()
        {
            EnsureFolders();

            Sprite calmSprite = SaveSpriteAsset("Assets/Art/Slime_Calm.png", ProceduralVisuals.GetSlimeSprite(false).texture, 64);
            Sprite angrySprite = SaveSpriteAsset("Assets/Art/Slime_Angry.png", ProceduralVisuals.GetSlimeSprite(true).texture, 64);
            Sprite creatureSprite = SaveSpriteAsset("Assets/Art/EmotionCreature_AngerSpark.png", ProceduralVisuals.GetAngerCreatureSprite().texture, 48);
            Sprite enemySprite = SaveSpriteAsset("Assets/Art/LargeEnemy_GloomBrute.png", ProceduralVisuals.GetLargeEnemySprite().texture, 64);
            Sprite platformSprite = SaveSpriteAsset("Assets/Art/RoomPlatform_Block.png", ProceduralVisuals.GetPlatformSprite().texture, 32);
            Sprite exitSprite = SaveSpriteAsset("Assets/Art/ExitGoal_Portal.png", ProceduralVisuals.GetExitPortalSprite().texture, 64);

            CreatePrefabs(calmSprite, angrySprite, creatureSprite, enemySprite, platformSprite, exitSprite);

            FeelinSlimyRoomBuilder.BuildCompleteTestRoom(
                calmSprite,
                angrySprite,
                creatureSprite,
                enemySprite,
                platformSprite,
                exitSprite
            );

            Scene activeScene = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                if (!File.Exists("Assets/Scenes/BeginnerStarterScene.unity"))
                {
                    AssetDatabase.CopyAsset(activeScene.path, "Assets/Scenes/BeginnerStarterScene.unity");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Feelin' Slimy] Test Room, Art Sprites, Prefabs, and BeginnerStarterScene are ready!");
        }

        private static void EnsureFolders()
        {
            string[] folders = { "Scenes", "Scripts", "Prefabs", "Art", "Audio", "UI" };
            foreach (string folder in folders)
            {
                string path = "Assets/" + folder;
                if (!AssetDatabase.IsValidFolder(path))
                {
                    AssetDatabase.CreateFolder("Assets", folder);
                }
            }
        }

        private static Sprite SaveSpriteAsset(string assetPath, Texture2D sourceTex, int pixelsPerUnit)
        {
            if (!File.Exists(assetPath) && sourceTex != null)
            {
                byte[] pngBytes = sourceTex.EncodeToPNG();
                File.WriteAllBytes(assetPath, pngBytes);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = pixelsPerUnit;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void CreatePrefabs(
            Sprite calmSprite,
            Sprite angrySprite,
            Sprite creatureSprite,
            Sprite enemySprite,
            Sprite platformSprite,
            Sprite exitSprite)
        {
            SlimePlayerController tempPlayer = FeelinSlimyRoomBuilder.CreateSlimePlayer(Vector3.zero, calmSprite, angrySprite);
            PrefabUtility.SaveAsPrefabAsset(tempPlayer.gameObject, "Assets/Prefabs/SlimePlayer.prefab");
            Object.DestroyImmediate(tempPlayer.gameObject);

            EmotionCreature tempCreature = FeelinSlimyRoomBuilder.CreateEmotionCreature(Vector3.zero, creatureSprite);
            PrefabUtility.SaveAsPrefabAsset(tempCreature.gameObject, "Assets/Prefabs/AngerCreature.prefab");
            Object.DestroyImmediate(tempCreature.gameObject);

            LargeEnemy tempEnemy = FeelinSlimyRoomBuilder.CreateLargeEnemy(Vector3.zero, enemySprite);
            PrefabUtility.SaveAsPrefabAsset(tempEnemy.gameObject, "Assets/Prefabs/LargeEnemy.prefab");
            Object.DestroyImmediate(tempEnemy.gameObject);

            ExitGoal tempExit = FeelinSlimyRoomBuilder.CreateExitGoal(Vector3.zero, exitSprite);
            PrefabUtility.SaveAsPrefabAsset(tempExit.gameObject, "Assets/Prefabs/ExitGoal.prefab");
            Object.DestroyImmediate(tempExit.gameObject);

            RoomPlatform tempPlat = FeelinSlimyRoomBuilder.CreatePlatform(null, "RoomPlatform", Vector3.zero, new Vector3(4f, 0.7f, 1f), new Color(0.32f, 0.44f, 0.56f, 1f), platformSprite);
            PrefabUtility.SaveAsPrefabAsset(tempPlat.gameObject, "Assets/Prefabs/RoomPlatform.prefab");
            Object.DestroyImmediate(tempPlat.gameObject);
        }
    }
}
#endif
