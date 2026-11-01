#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FeelinSlimy.Editor
{
    public class LevelSettingsWindow : EditorWindow
    {
        private const string LastAssetPrefKey = "FeelinSlimy.LevelSettingsWindow.LastAsset";
        private const string LiveApplyPrefKey = "FeelinSlimy.LevelSettingsWindow.LiveApply";
        private const string TabPrefKey = "FeelinSlimy.LevelSettingsWindow.Tab";
        private const string LevelsFolder = "Assets/Levels";
        private const string LayoutProp = "layout";

        private class Section
        {
            public readonly string Title;
            public readonly string Description;
            public readonly string[] Props;

            public Section(string title, string description, params string[] props)
            {
                Title = title;
                Description = description;
                Props = props;
            }
        }

        private static readonly Section[] Sections =
        {
            new Section("Level Info", "Name and designer notes for this level variation.", "levelName", "description"),
            new Section("Movement", "Run speed, acceleration, jump strength and ground detection of the Slime.", "playerMovement"),
            new Section("Slime Trail & Walls", "Residue trail speed boost, wall climbing, wall sliding and wall jumps.", "slimeTrail"),
            new Section("Jump Physics", "Gravity curves for jumps and falls, plus the jelly squash & stretch feel.", "jumpPhysics"),
            new Section("Absorption", "How the Slime pulls and absorbs emotion creatures (Hold E).", "absorption"),
            new Section("Anger Stomp", "The ground-pound ability unlocked by absorbing Anger.", "angerStomp"),
            new Section("Health", "Hearts, invulnerability after damage and the fall-out limit.", "playerHealth"),
            new Section("Player Visuals", "Slime colors and optional custom sprites.", "playerVisuals"),
            new Section("Enemy", "Patrol, chase ability, contact attack and weaknesses. Applies to every Large Enemy in the scene.", "enemy"),
            new Section("Emotion Creature", "Identity, look and wandering motion of the small emotion creature.", "creature"),
            new Section("Exit Portal", "Look of the exit and whether the enemy must be defeated first.", "exit"),
            new Section("Camera", "Zoom, height, follow bounds and background color.", "cameraSettings"),
            new Section("Room Layout", "Spawn points and platforms. Drag the handles in the Scene view, then press Apply Layout.", LayoutProp),
        };

        private LevelSettings _settings;
        private SerializedObject _so;
        private int _tab;
        private bool _liveApply;
        private Vector2 _sidebarScroll;
        private Vector2 _contentScroll;
        private GUIStyle _tabStyle;
        private GUIStyle _titleStyle;

        [MenuItem("Tools/Feelin' Slimy/Level Settings Editor")]
        private static void OpenFromMenu()
        {
            Open();
        }

        public static LevelSettingsWindow Open()
        {
            LevelSettingsWindow window = GetWindow<LevelSettingsWindow>();
            window.titleContent = new GUIContent("Level Settings", EditorGUIUtility.IconContent("ScriptableObject Icon").image);
            window.minSize = new Vector2(620f, 420f);
            window.Show();
            return window;
        }

        public static void Open(LevelSettings settings)
        {
            Open().SetSettings(settings);
        }

        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceID) is LevelSettings settings)
            {
                Open(settings);
                return true;
            }
            return false;
        }

        private void OnEnable()
        {
            _liveApply = EditorPrefs.GetBool(LiveApplyPrefKey, true);
            _tab = Mathf.Clamp(EditorPrefs.GetInt(TabPrefKey, 0), 0, Sections.Length - 1);

            if (_settings == null)
            {
                string path = AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString(LastAssetPrefKey, ""));
                LevelSettings last = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<LevelSettings>(path);
                GameManager gm = Object.FindFirstObjectByType<GameManager>();
                SetSettings(last != null ? last : gm != null ? gm.levelSettings : null);
            }

            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            _so?.Update();
            Repaint();
        }

        private void SetSettings(LevelSettings settings)
        {
            _settings = settings;
            _so = settings != null ? new SerializedObject(settings) : null;
            if (settings != null)
            {
                EditorPrefs.SetString(LastAssetPrefKey, AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(settings)));
            }
            SceneView.RepaintAll();
            Repaint();
        }

        private void InitStyles()
        {
            if (_tabStyle != null) return;
            _tabStyle = new GUIStyle(EditorStyles.toolbarButton)
            {
                alignment = TextAnchor.MiddleLeft,
                fixedHeight = 26f,
                fontSize = 12,
                padding = new RectOffset(10, 6, 2, 2)
            };
            _titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 };
        }

        private void OnGUI()
        {
            InitStyles();
            DrawToolbar();

            if (_settings == null)
            {
                DrawEmptyState();
                return;
            }

            if (_so == null || _so.targetObject != _settings) _so = new SerializedObject(_settings);
            _so.Update();

            EditorGUILayout.BeginHorizontal();
            DrawSidebar();
            DrawContent();
            EditorGUILayout.EndHorizontal();

            DrawFooter();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            LevelSettings picked = (LevelSettings)EditorGUILayout.ObjectField(_settings, typeof(LevelSettings), false, GUILayout.MinWidth(200f));
            if (EditorGUI.EndChangeCheck()) SetSettings(picked);

            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(45f)))
            {
                LevelSettings created = CreateAsset(false);
                if (created != null) SetSettings(created);
            }
            if (GUILayout.Button("New From Scene", EditorStyles.toolbarButton, GUILayout.Width(105f)))
            {
                LevelSettings created = CreateAsset(true);
                if (created != null) SetSettings(created);
            }
            using (new EditorGUI.DisabledScope(_settings == null))
            {
                if (GUILayout.Button("Duplicate", EditorStyles.toolbarButton, GUILayout.Width(70f))) DuplicateAsset();
                if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(40f))) EditorGUIUtility.PingObject(_settings);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.HelpBox(
                "No Level Settings asset selected.\n\n" +
                "A Level Settings asset stores every tunable value of a level: Slime movement, jump physics, slime trail & walls, " +
                "absorption, Anger Stomp, health, enemy abilities, creature, exit, camera and room layout.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Create From Current Scene", GUILayout.Width(220f), GUILayout.Height(32f)))
            {
                LevelSettings created = CreateAsset(true);
                if (created != null) SetSettings(created);
            }
            if (GUILayout.Button("Create With Defaults", GUILayout.Width(180f), GUILayout.Height(32f)))
            {
                LevelSettings created = CreateAsset(false);
                if (created != null) SetSettings(created);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        private void DrawSidebar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(180f));
            _sidebarScroll = EditorGUILayout.BeginScrollView(_sidebarScroll);
            for (int i = 0; i < Sections.Length; i++)
            {
                bool selected = GUILayout.Toggle(_tab == i, Sections[i].Title, _tabStyle);
                if (selected && _tab != i)
                {
                    _tab = i;
                    EditorPrefs.SetInt(TabPrefKey, _tab);
                    GUI.FocusControl(null);
                    SceneView.RepaintAll();
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawContent()
        {
            Section section = Sections[_tab];

            EditorGUILayout.BeginVertical();
            _contentScroll = EditorGUILayout.BeginScrollView(_contentScroll);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(section.Title, _titleStyle, GUILayout.Height(22f));
            GUILayout.FlexibleSpace();
            if (section.Props.Length == 1 && GUILayout.Button("Reset Section", GUILayout.Width(110f)))
            {
                ResetSection(section);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField(section.Description, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space(6f);

            float prevLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Mathf.Max(180f, position.width * 0.3f);

            if (section.Props.Length == 1)
            {
                DrawChildren(_so.FindProperty(section.Props[0]));
            }
            else
            {
                foreach (string propName in section.Props) EditorGUILayout.PropertyField(_so.FindProperty(propName), true);
            }

            EditorGUIUtility.labelWidth = prevLabelWidth;
            if (_so.ApplyModifiedProperties() && _liveApply && !Application.isPlaying)
            {
                ApplyToScene(false, false);
            }

            if (section.Props[0] == LayoutProp) DrawLayoutTools();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private static void DrawChildren(SerializedProperty prop)
        {
            if (prop == null) return;
            SerializedProperty it = prop.Copy();
            SerializedProperty end = prop.GetEndProperty();
            bool enterChildren = true;
            while (it.NextVisible(enterChildren) && !SerializedProperty.EqualContents(it, end))
            {
                EditorGUILayout.PropertyField(it, true);
                enterChildren = false;
            }
        }

        private void DrawLayoutTools()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(
                "Layout changes are not applied live. Use the Scene view handles (shown while this tab is open) to move spawn points " +
                "and platforms, then press Apply Layout. Apply Layout destroys and rebuilds every Room Platform in the scene (undoable).",
                MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Layout Now", GUILayout.Height(26f)))
            {
                ApplyToScene(true, true);
            }
            if (GUILayout.Button("Capture Layout From Scene", GUILayout.Height(26f)))
            {
                ModifySettings("Capture Layout", s => s.layout.CaptureFromScene(
                    Object.FindFirstObjectByType<SlimePlayerController>(FindObjectsInactive.Include),
                    Object.FindFirstObjectByType<LargeEnemy>(FindObjectsInactive.Include),
                    Object.FindFirstObjectByType<EmotionCreature>(FindObjectsInactive.Include),
                    Object.FindFirstObjectByType<ExitGoal>(FindObjectsInactive.Include)));
            }
            if (GUILayout.Button("Default Room Platforms", GUILayout.Height(26f)))
            {
                ModifySettings("Default Room Platforms", s => s.layout.platforms = RoomLayoutSettings.CreateDefaultPlatforms());
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GameManager gm = Object.FindFirstObjectByType<GameManager>();
            if (gm == null)
            {
                EditorGUILayout.LabelField("No GameManager in the open scene. Settings can still be applied directly to scene objects.", EditorStyles.wordWrappedMiniLabel);
            }
            else if (gm.levelSettings == _settings)
            {
                EditorGUILayout.LabelField(Application.isPlaying
                    ? "PLAY MODE: the scene GameManager uses this asset - edits apply live and are saved to the asset."
                    : "The scene GameManager uses this asset - it is applied automatically when entering Play Mode.",
                    EditorStyles.wordWrappedMiniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("The scene GameManager is not using this asset. Press 'Use In GameManager' to apply it on Play.", EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUI.BeginChangeCheck();
                _liveApply = GUILayout.Toggle(_liveApply, new GUIContent("Live Apply", "Edit Mode: push every change straight onto the scene objects (layout excluded)."), GUILayout.Width(85f));
                if (EditorGUI.EndChangeCheck()) EditorPrefs.SetBool(LiveApplyPrefKey, _liveApply);
            }

            if (GUILayout.Button(new GUIContent("Apply To Scene", "Copy all settings onto the scene objects. Includes the layout when 'Apply Layout' is enabled in Room Layout."), GUILayout.Height(28f)))
            {
                ApplyToScene(true, true);
            }
            if (GUILayout.Button(new GUIContent("Capture From Scene", "Overwrite this asset with the values currently on the scene objects."), GUILayout.Height(28f)))
            {
                ModifySettings("Capture Level Settings", s => s.CaptureFromScene());
            }
            using (new EditorGUI.DisabledScope(gm == null || gm.levelSettings == _settings))
            {
                if (GUILayout.Button(new GUIContent("Use In GameManager", "Assign this asset to the scene GameManager."), GUILayout.Height(28f)))
                {
                    Undo.RecordObject(gm, "Assign Level Settings");
                    gm.levelSettings = _settings;
                    if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void ModifySettings(string undoName, Action<LevelSettings> change)
        {
            Undo.RecordObject(_settings, undoName);
            change(_settings);
            EditorUtility.SetDirty(_settings);
            _so.Update();
            _settings.NotifyChanged();
            GUI.FocusControl(null);
            SceneView.RepaintAll();
        }

        private void ResetSection(Section section)
        {
            FieldInfo field = typeof(LevelSettings).GetField(section.Props[0]);
            if (field == null) return;
            ModifySettings("Reset " + section.Title, s => field.SetValue(s, Activator.CreateInstance(field.FieldType)));
            if (_liveApply && !Application.isPlaying && section.Props[0] != LayoutProp) ApplyToScene(false, false);
        }

        private void ApplyToScene(bool includeLayout, bool newUndoGroup)
        {
            if (Application.isPlaying)
            {
                _settings.ApplyToScene(includeLayout);
                return;
            }

            if (newUndoGroup) Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Level Settings");

            HashSet<GameObject> targets = new HashSet<GameObject>();
            foreach (SlimePlayerController c in LevelSettings.FindAll<SlimePlayerController>()) targets.Add(c.gameObject);
            foreach (LargeEnemy c in LevelSettings.FindAll<LargeEnemy>()) targets.Add(c.gameObject);
            foreach (EmotionCreature c in LevelSettings.FindAll<EmotionCreature>()) targets.Add(c.gameObject);
            foreach (ExitGoal c in LevelSettings.FindAll<ExitGoal>()) targets.Add(c.gameObject);
            foreach (GameManager c in LevelSettings.FindAll<GameManager>()) targets.Add(c.gameObject);
            if (Camera.main != null) targets.Add(Camera.main.gameObject);
            foreach (GameObject go in targets) Undo.RegisterFullObjectHierarchyUndo(go, "Apply Level Settings");

            _settings.ApplyToScene(includeLayout, DestroyWithUndo, go => Undo.RegisterCreatedObjectUndo(go, "Create Platform"));

            Undo.CollapseUndoOperations(group);
            EditorSceneManager.MarkAllScenesDirty();
            SceneView.RepaintAll();
        }

        private static void DestroyWithUndo(GameObject go)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(go) && !PrefabUtility.IsOutermostPrefabInstanceRoot(go))
            {
                Undo.RecordObject(go, "Hide Platform");
                go.SetActive(false);
                return;
            }
            Undo.DestroyObjectImmediate(go);
        }

        private static LevelSettings CreateAsset(bool captureScene)
        {
            if (!AssetDatabase.IsValidFolder(LevelsFolder)) AssetDatabase.CreateFolder("Assets", "Levels");
            string path = EditorUtility.SaveFilePanelInProject("Create Level Settings", "LevelSettings", "asset",
                "Choose where to save the new Level Settings asset.", LevelsFolder);
            if (string.IsNullOrEmpty(path)) return null;

            LevelSettings asset = CreateInstance<LevelSettings>();
            asset.levelName = Path.GetFileNameWithoutExtension(path);
            if (captureScene) asset.CaptureFromScene();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            return asset;
        }

        private void DuplicateAsset()
        {
            string source = AssetDatabase.GetAssetPath(_settings);
            string target = AssetDatabase.GenerateUniqueAssetPath(source);
            if (!AssetDatabase.CopyAsset(source, target)) return;
            LevelSettings copy = AssetDatabase.LoadAssetAtPath<LevelSettings>(target);
            copy.levelName = Path.GetFileNameWithoutExtension(target);
            EditorUtility.SetDirty(copy);
            AssetDatabase.SaveAssets();
            SetSettings(copy);
        }

        private void OnSceneGUI(SceneView view)
        {
            if (_settings == null || Sections[_tab].Props[0] != LayoutProp) return;

            RoomLayoutSettings layout = _settings.layout;
            EditorGUI.BeginChangeCheck();

            Vector2 player = PointHandle(layout.playerSpawn, "Player Spawn", new Color(0.3f, 0.9f, 1f));
            Vector2 creature = PointHandle(layout.creaturePosition, "Emotion Creature", new Color(1f, 0.5f, 0.2f));
            Vector2 enemy = PointHandle(layout.enemyPosition, "Large Enemy", new Color(0.8f, 0.3f, 1f));
            Vector2 exit = PointHandle(layout.exitPosition, "Exit Portal", new Color(0.3f, 1f, 0.6f));

            Handles.color = new Color(0.8f, 0.3f, 1f, 0.6f);
            Handles.DrawLine(enemy + Vector2.left * _settings.enemy.patrolDistance, enemy + Vector2.right * _settings.enemy.patrolDistance);

            Vector2[] platformPositions = new Vector2[layout.platforms.Count];
            for (int i = 0; i < layout.platforms.Count; i++)
            {
                PlatformLayout p = layout.platforms[i];
                Rect rect = new Rect(p.position - p.size * 0.5f, p.size);
                Handles.DrawSolidRectangleWithOutline(rect, new Color(p.color.r, p.color.g, p.color.b, 0.12f), new Color(1f, 1f, 1f, 0.75f));
                platformPositions[i] = PointHandle(p.position, p.name, Color.white, 0.08f);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_settings, "Move Layout Handle");
                layout.playerSpawn = player;
                layout.creaturePosition = creature;
                layout.enemyPosition = enemy;
                layout.exitPosition = exit;
                for (int i = 0; i < platformPositions.Length; i++) layout.platforms[i].position = platformPositions[i];
                EditorUtility.SetDirty(_settings);
                _so?.Update();
                Repaint();
            }
        }

        private static Vector2 PointHandle(Vector2 pos, string label, Color color, float sizeScale = 0.12f)
        {
            Handles.color = color;
            float size = HandleUtility.GetHandleSize(pos) * sizeScale;
            Vector3 moved = Handles.FreeMoveHandle(pos, size, Vector3.one * 0.25f, Handles.CircleHandleCap);
            Handles.Label((Vector3)pos + Vector3.up * size * 2.2f, label, EditorStyles.whiteMiniLabel);
            return moved;
        }
    }

    [CustomEditor(typeof(LevelSettings))]
    public class LevelSettingsInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open in Level Settings Editor", GUILayout.Height(30f)))
            {
                LevelSettingsWindow.Open((LevelSettings)target);
            }
            EditorGUILayout.Space();
            DrawDefaultInspector();
        }
    }
}
#endif
