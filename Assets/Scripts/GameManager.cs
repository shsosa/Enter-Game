using UnityEngine;
using UnityEngine.InputSystem;

namespace FeelinSlimy
{
    public class GameManager : MonoBehaviour
    {
        public enum GameState
        {
            Playing,
            Victory,
            Failure
        }

        public static GameManager Instance { get; private set; }

        [Header("Scene References")]
        [Tooltip("Reference to the Slime player in the test room.")]
        public SlimePlayerController player;
        [Tooltip("Reference to the small Anger Emotion Creature in the test room.")]
        public EmotionCreature emotionCreature;
        [Tooltip("Reference to the dangerous Large Enemy in the test room.")]
        public LargeEnemy largeEnemy;
        [Tooltip("Reference to the Exit Portal at the end of the test room.")]
        public ExitGoal exitGoal;

        [Header("Camera Settings")]
        [Tooltip("Whether the camera smoothly tracks the Slime horizontally.")]
        public bool smoothFollowPlayer = true;
        [Tooltip("Minimum and maximum X coordinates for the camera in the test room.")]
        public Vector2 cameraBoundsX = new Vector2(-2.5f, 4.5f);

        public GameState CurrentState { get; private set; } = GameState.Playing;
        public bool IsEnemyDefeated => largeEnemy != null && largeEnemy.IsDefeated;

        private string _resultMessage = "";
        private string _hintMessage = "Explore the room! Climb the left platforms to reach the small Anger Spark.";
        private float _hintTimer = 8f;

        private Camera _mainCamera;
        private Vector3 _cameraBasePos;
        private float _shakeTimer;
        private float _shakeIntensity;

        private GUIStyle _panelStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _bannerTitleStyle;
        private GUIStyle _bannerSubStyle;
        private Texture2D _darkTex;
        private Texture2D _accentTex;
        private Texture2D _heartFullTex;
        private Texture2D _heartEmptyTex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            AutoWireReferences();
            ConfigureCamera();
        }

        public void AutoWireReferences()
        {
            if (player == null) player = FindFirstObjectByType<SlimePlayerController>();
            if (emotionCreature == null) emotionCreature = FindFirstObjectByType<EmotionCreature>();
            if (largeEnemy == null) largeEnemy = FindFirstObjectByType<LargeEnemy>();
            if (exitGoal == null) exitGoal = FindFirstObjectByType<ExitGoal>();

            if (player != null && player.targetCreature == null)
            {
                player.targetCreature = emotionCreature;
            }
        }

        private void ConfigureCamera()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            _mainCamera.orthographic = true;
            _mainCamera.orthographicSize = 6.2f;
            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            _mainCamera.backgroundColor = new Color(0.11f, 0.14f, 0.21f, 1f);
            _cameraBasePos = new Vector3(0f, 0.8f, -10f);
            _mainCamera.transform.position = _cameraBasePos;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                RestartRoom();
            }

            Gamepad gp = Gamepad.current;
            if (gp != null && gp.selectButton.wasPressedThisFrame)
            {
                RestartRoom();
            }

            if (_hintTimer > 0f)
            {
                _hintTimer -= Time.deltaTime;
            }

            UpdateCamera();
        }

        private void UpdateCamera()
        {
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            if (smoothFollowPlayer && player != null)
            {
                float targetX = Mathf.Clamp(player.transform.position.x, cameraBoundsX.x, cameraBoundsX.y);
                _cameraBasePos.x = Mathf.Lerp(_cameraBasePos.x, targetX, Time.deltaTime * 5f);
            }

            Vector3 shakeOffset = Vector3.zero;
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.deltaTime;
                shakeOffset = (Vector3)(Random.insideUnitCircle * _shakeIntensity);
            }

            _mainCamera.transform.position = _cameraBasePos + shakeOffset;
        }

        public void TriggerCameraShake(float duration, float intensity)
        {
            _shakeTimer = Mathf.Max(_shakeTimer, duration);
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
        }

        public void ShowTemporaryHint(string message, float duration = 5.5f)
        {
            _hintMessage = message;
            _hintTimer = duration;
        }

        public void NotifyEnemyDefeated(LargeEnemy enemy)
        {
            ShowTemporaryHint("Gloom Brute crushed! Head right into the glowing Exit Portal!", 6f);
        }

        public void TriggerVictory()
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.Victory;
            _resultMessage = "ROOM COMPLETE! You absorbed Anger, mastered the Heavy Stomp, and reached the Exit!";

            if (RetroSoundEffects.Instance != null)
            {
                RetroSoundEffects.Instance.PlayVictory();
            }
        }

        public void TriggerFailure(string reason)
        {
            if (CurrentState != GameState.Playing) return;
            CurrentState = GameState.Failure;
            _resultMessage = reason;

            if (RetroSoundEffects.Instance != null)
            {
                RetroSoundEffects.Instance.PlayFailure();
            }
        }

        public void RestartRoom()
        {
            CurrentState = GameState.Playing;
            _resultMessage = "";
            ShowTemporaryHint("Room reset! Leave slime trails for speed & wall-climbing, absorb the Anger Spark (Hold E), and Stomp the Brute!", 5.5f);

            SlimeResiduePatch.ClearAllResidue();
            if (player != null) player.ResetPlayer();
            if (emotionCreature != null) emotionCreature.ResetCreature();
            if (largeEnemy != null) largeEnemy.ResetEnemy();
        }

        private void InitStyles()
        {
            if (_panelStyle != null) return;

            _darkTex = MakeSolidTex(new Color(0.07f, 0.09f, 0.14f, 0.86f));
            _accentTex = MakeSolidTex(new Color(1f, 0.32f, 0.18f, 0.95f));
            _heartFullTex = MakeSolidTex(new Color(1f, 0.25f, 0.35f, 1f));
            _heartEmptyTex = MakeSolidTex(new Color(0.28f, 0.3f, 0.36f, 0.85f));

            _panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _darkTex, textColor = Color.white },
                padding = new RectOffset(14, 14, 10, 10)
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.95f, 0.98f, 1f) }
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.85f, 0.9f, 0.96f) }
            };

            _bannerTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            _bannerSubStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.95f, 1f) }
            };
        }

        private static Texture2D MakeSolidTex(Color col)
        {
            Texture2D tex = new Texture2D(2, 2);
            tex.SetPixels(new[] { col, col, col, col });
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            InitStyles();
            if (player == null) return;

            GUI.Box(new Rect(16, 16, 370, 142), GUIContent.none, _panelStyle);
            GUI.Label(new Rect(30, 24, 340, 24), "FEELIN' SLIMY — Prototype Test Room", _titleStyle);

            GUI.Label(new Rect(30, 52, 70, 22), "Health:", _bodyStyle);
            for (int i = 0; i < player.maxHealth; i++)
            {
                bool filled = i < player.CurrentHealth;
                GUI.DrawTexture(new Rect(92 + i * 32, 53, 24, 20), filled ? _heartFullTex : _heartEmptyTex);
            }

            string abilityText = player.hasAngerStomp
                ? "ACTIVE: ANGER STOMP  [Press S / Down / Shift]"
                : "NONE  (Stand near Red Spark & Hold E)";
            Color prevColor = GUI.color;
            GUI.color = player.hasAngerStomp ? new Color(1f, 0.45f, 0.3f) : new Color(0.7f, 0.85f, 1f);
            GUI.Label(new Rect(30, 80, 345, 22), "Ability: " + abilityText, _bodyStyle);
            GUI.color = prevColor;

            string residueStatus = player.IsWallClimbing
                ? "WALL CLIMBING! (Jump to Wall-Launch)"
                : (player.IsOnResidueBoosted ? "SLIME BOOST ACTIVE! (Speed +48%)" : "Leaving Slime Trail (Speed & Wall Climb)");
            GUI.color = (player.IsWallClimbing || player.IsOnResidueBoosted) ? new Color(0.35f, 1f, 0.85f) : new Color(0.8f, 0.9f, 0.95f);
            GUI.Label(new Rect(30, 104, 345, 22), "Slime Trail: " + residueStatus, _bodyStyle);
            GUI.color = prevColor;

            string enemyStatus = IsEnemyDefeated ? "Defeated!" : "Patrolling Exit";
            GUI.Label(new Rect(30, 128, 345, 22), $"Gloom Brute: {enemyStatus}", _bodyStyle);

            float rightX = Screen.width - 336;
            GUI.Box(new Rect(rightX, 16, 320, 126), GUIContent.none, _panelStyle);
            GUI.Label(new Rect(rightX + 14, 24, 290, 22), "Controls", _titleStyle);
            GUI.Label(new Rect(rightX + 14, 48, 295, 86),
                "• Move: A / D or Left / Right (Fast on Residue!)\n" +
                "• Jump / Wall-Jump: Space / W / Up Arrow\n" +
                "• Wall Climb: Push into wall or W / Up on slime\n" +
                "• Absorb: Hold E  |  Stomp: S/Down/Shift  |  Reset: R",
                _bodyStyle);

            if (player.CanReachCreature && !player.hasAngerStomp && CurrentState == GameState.Playing)
            {
                float boxW = 300f;
                float boxX = (Screen.width - boxW) * 0.5f;
                GUI.Box(new Rect(boxX, Screen.height - 110f, boxW, 56f), GUIContent.none, _panelStyle);
                GUI.Label(new Rect(boxX + 14f, Screen.height - 104f, boxW - 28f, 22f),
                    "Hold [E] to Pull & Absorb Anger Spark!", _titleStyle);
                GUI.DrawTexture(new Rect(boxX + 14f, Screen.height - 74f, boxW - 28f, 12f), _heartEmptyTex);
                GUI.DrawTexture(new Rect(boxX + 14f, Screen.height - 74f, (boxW - 28f) * player.CurrentAbsorbProgress01, 12f), _accentTex);
            }
            else if (_hintTimer > 0f && CurrentState == GameState.Playing)
            {
                float boxW = 540f;
                float boxX = (Screen.width - boxW) * 0.5f;
                GUI.Box(new Rect(boxX, Screen.height - 54f, boxW, 38f), GUIContent.none, _panelStyle);
                GUI.Label(new Rect(boxX + 14f, Screen.height - 46f, boxW - 28f, 24f), _hintMessage, _bodyStyle);
            }

            if (CurrentState != GameState.Playing)
            {
                float modalW = 520f;
                float modalH = 190f;
                float modalX = (Screen.width - modalW) * 0.5f;
                float modalY = (Screen.height - modalH) * 0.5f;

                GUI.Box(new Rect(modalX, modalY, modalW, modalH), GUIContent.none, _panelStyle);
                string header = CurrentState == GameState.Victory ? "VICTORY — TEST ROOM CLEARED!" : "TRY AGAIN!";
                _bannerTitleStyle.normal.textColor = CurrentState == GameState.Victory
                    ? new Color(0.35f, 1f, 0.65f)
                    : new Color(1f, 0.38f, 0.35f);

                GUI.Label(new Rect(modalX + 20f, modalY + 22f, modalW - 40f, 36f), header, _bannerTitleStyle);
                GUI.Label(new Rect(modalX + 24f, modalY + 68f, modalW - 48f, 54f), _resultMessage, _bannerSubStyle);

                if (GUI.Button(new Rect(modalX + (modalW - 220f) * 0.5f, modalY + 132f, 220f, 38f), "Restart Room (Press R)"))
                {
                    RestartRoom();
                }
            }
        }
    }
}
