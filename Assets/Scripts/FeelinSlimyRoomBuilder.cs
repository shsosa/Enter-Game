using UnityEngine;

namespace FeelinSlimy
{
    public static class FeelinSlimyRoomBuilder
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureRoomAtRuntime()
        {
            if (Object.FindFirstObjectByType<SlimePlayerController>() != null)
            {
                return;
            }

            BuildCompleteTestRoom();
        }

        public static GameManager BuildCompleteTestRoom(
            Sprite calmSlimeSprite = null,
            Sprite angrySlimeSprite = null,
            Sprite creatureSprite = null,
            Sprite enemySprite = null,
            Sprite platformSprite = null,
            Sprite exitSprite = null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.orthographic = true;
                mainCam.orthographicSize = 6.2f;
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0.11f, 0.14f, 0.21f, 1f);
                mainCam.transform.position = new Vector3(0f, 0.8f, -10f);
                mainCam.transform.rotation = Quaternion.identity;
            }

            RetroSoundEffects sfx = Object.FindFirstObjectByType<RetroSoundEffects>();
            if (sfx == null)
            {
                GameObject audioObj = new GameObject("AudioManager_SFX");
                sfx = audioObj.AddComponent<RetroSoundEffects>();
            }

            GameObject roomRoot = GameObject.Find("TestRoom_Environment");
            if (roomRoot != null)
            {
                Object.DestroyImmediate(roomRoot);
            }
            roomRoot = new GameObject("TestRoom_Environment");

            Color groundCol = new Color(0.24f, 0.31f, 0.42f, 1f);
            Color platformCol = new Color(0.32f, 0.44f, 0.56f, 1f);
            Color obstacleCol = new Color(0.42f, 0.34f, 0.46f, 1f);

            CreatePlatform(roomRoot.transform, "MainGround_Floor", new Vector3(0f, -4.5f, 0f), new Vector3(24f, 1.4f, 1f), groundCol, platformSprite);
            CreatePlatform(roomRoot.transform, "LeftBoundary_Wall", new Vector3(-11.5f, 1.0f, 0f), new Vector3(1.2f, 12.5f, 1f), groundCol, platformSprite);
            CreatePlatform(roomRoot.transform, "RightBoundary_Wall", new Vector3(11.5f, 1.0f, 0f), new Vector3(1.2f, 12.5f, 1f), groundCol, platformSprite);
            CreatePlatform(roomRoot.transform, "CenterDivider_Obstacle", new Vector3(-2.8f, -2.9f, 0f), new Vector3(1.4f, 1.8f, 1f), obstacleCol, platformSprite);
            CreatePlatform(roomRoot.transform, "SteppingPlatform_Low", new Vector3(-5.2f, -1.5f, 0f), new Vector3(3.2f, 0.65f, 1f), platformCol, platformSprite);
            CreatePlatform(roomRoot.transform, "EmotionPlatform_UpperLeft", new Vector3(-8.2f, 0.9f, 0f), new Vector3(4.2f, 0.65f, 1f), platformCol, platformSprite);
            CreatePlatform(roomRoot.transform, "VantagePlatform_StompDrop", new Vector3(1.6f, 0.6f, 0f), new Vector3(3.8f, 0.65f, 1f), platformCol, platformSprite);

            SlimePlayerController existingPlayer = Object.FindFirstObjectByType<SlimePlayerController>();
            if (existingPlayer != null) Object.DestroyImmediate(existingPlayer.gameObject);
            SlimePlayerController player = CreateSlimePlayer(new Vector3(-8.2f, -2.8f, 0f), calmSlimeSprite, angrySlimeSprite);

            EmotionCreature existingCreature = Object.FindFirstObjectByType<EmotionCreature>();
            if (existingCreature != null) Object.DestroyImmediate(existingCreature.gameObject);
            EmotionCreature creature = CreateEmotionCreature(new Vector3(-8.2f, 1.95f, 0f), creatureSprite);

            LargeEnemy existingEnemy = Object.FindFirstObjectByType<LargeEnemy>();
            if (existingEnemy != null) Object.DestroyImmediate(existingEnemy.gameObject);
            LargeEnemy enemy = CreateLargeEnemy(new Vector3(2.4f, -2.75f, 0f), enemySprite);

            ExitGoal existingExit = Object.FindFirstObjectByType<ExitGoal>();
            if (existingExit != null) Object.DestroyImmediate(existingExit.gameObject);
            ExitGoal exit = CreateExitGoal(new Vector3(9.2f, -2.65f, 0f), exitSprite);

            player.targetCreature = creature;

            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gm = gmObj.AddComponent<GameManager>();
            }

            gm.player = player;
            gm.emotionCreature = creature;
            gm.largeEnemy = enemy;
            gm.exitGoal = exit;

            return gm;
        }

        public static RoomPlatform CreatePlatform(Transform parent, string name, Vector3 position, Vector3 scale, Color color, Sprite customSprite = null)
        {
            GameObject obj = new GameObject(name);
            if (parent != null) obj.transform.SetParent(parent, false);
            obj.transform.position = position;
            obj.transform.localScale = scale;

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = customSprite != null ? customSprite : ProceduralVisuals.GetPlatformSprite();
            sr.color = color;

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            RoomPlatform rp = obj.AddComponent<RoomPlatform>();
            rp.platformColor = color;
            rp.customSprite = customSprite;
            rp.ApplyVisuals();
            return rp;
        }

        public static SlimePlayerController CreateSlimePlayer(Vector3 position, Sprite calmSprite = null, Sprite angrySprite = null)
        {
            GameObject obj = new GameObject("SlimePlayer");
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(1.15f, 1.15f, 1f);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = calmSprite != null ? calmSprite : ProceduralVisuals.GetSlimeSprite(false);
            sr.sortingOrder = 10;

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.82f, 0.78f);
            col.offset = new Vector2(0f, -0.04f);

            Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.2f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            SlimePlayerController player = obj.AddComponent<SlimePlayerController>();
            player.customCalmSprite = calmSprite;
            player.customAngrySprite = angrySprite;
            player.ApplyVisualState();
            return player;
        }

        public static EmotionCreature CreateEmotionCreature(Vector3 position, Sprite customSprite = null)
        {
            GameObject obj = new GameObject("EmotionCreature_AngerSpark");
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(0.95f, 0.95f, 1f);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = customSprite != null ? customSprite : ProceduralVisuals.GetAngerCreatureSprite();
            sr.sortingOrder = 5;

            CircleCollider2D col = obj.AddComponent<CircleCollider2D>();
            col.radius = 0.42f;
            col.isTrigger = true;

            EmotionCreature creature = obj.AddComponent<EmotionCreature>();
            creature.customSprite = customSprite;
            return creature;
        }

        public static LargeEnemy CreateLargeEnemy(Vector3 position, Sprite customSprite = null)
        {
            GameObject obj = new GameObject("LargeEnemy_GloomBrute");
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(2.1f, 2.1f, 1f);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = customSprite != null ? customSprite : ProceduralVisuals.GetLargeEnemySprite();
            sr.sortingOrder = 6;

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.84f, 0.82f);
            col.offset = new Vector2(0f, -0.03f);

            Rigidbody2D rb = obj.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.useFullKinematicContacts = true;

            LargeEnemy enemy = obj.AddComponent<LargeEnemy>();
            enemy.customSprite = customSprite;
            return enemy;
        }

        public static ExitGoal CreateExitGoal(Vector3 position, Sprite customSprite = null)
        {
            GameObject obj = new GameObject("ExitGoal_Portal");
            obj.transform.position = position;
            obj.transform.localScale = new Vector3(1.6f, 2.1f, 1f);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = customSprite != null ? customSprite : ProceduralVisuals.GetExitPortalSprite();
            sr.sortingOrder = 3;

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.75f, 0.85f);
            col.isTrigger = true;

            ExitGoal exit = obj.AddComponent<ExitGoal>();
            exit.customSprite = customSprite;
            return exit;
        }
    }
}
