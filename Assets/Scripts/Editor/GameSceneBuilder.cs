using System.IO;
using System.Linq;
using RayneoIdle.Game;
using RayneoIdle.GameInput;
using RayneoIdle.Onboarding;
using RayneoIdle.Todo;
using RayneoIdle.UI;
using RayneoIdle.Zones;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RayneoIdle.EditorTools
{
    /// <summary>
    /// Builds the device game scene Assets/Scenes/Game.unity and puts it FIRST in Build Settings.
    /// Contents:
    ///   - RayNeo "XR Plugin" prefab (head camera, tag MainCamera)
    ///   - "Zones": AnchorZoneBootstrap with withDebugUi OFF (SLAM + store + detector + ModeManager only)
    ///   - "Game": GameSession, DemoMode, ZoneTodoService, InputAdapter, TreasureMapView, MainHud, PanelHost,
    ///     OnboardingController, EditorHeadLook (Editor only: Q / E look forward / right)
    /// Re-run after adding a game component, instead of editing the scene by hand (one owner per scene).
    /// Start: Tools > RayneoIdle > Build Game Scene, or the file Temp/build-game-scene.request.
    /// </summary>
    [InitializeOnLoad]
    public static class GameSceneBuilder
    {
        private const string LogTag = "[GameSceneBuilder]";
        public const string ScenePath = "Assets/Scenes/Game.unity";
        private const string XrPluginPrefab = "Packages/com.unity.xr.rayneo.openxr/SDK/Runtime/Resources/Prefab/XR Plugin.prefab";
        private const string RequestFile = "Temp/build-game-scene.request";
        private const string ResultFile = "Temp/build-game-scene.result";

        private static double nextPoll;

        static GameSceneBuilder()
        {
            EditorApplication.update += Poll;
        }

        // Play check: Temp/play-game-scene.request opens Game.unity, plays PlayCheckSeconds, then stops.
        // Look for exceptions in the Console / Editor.log between "[GameSceneBuilder] play check start/end".
        private const string PlayRequestFile = "Temp/play-game-scene.request";
        private const string PlayResultFile = "Temp/play-game-scene.result";
        private const float PlayCheckSeconds = 8f;
        private const string KeyPlayUntil = "RayneoIdle.GameScene.PlayUntil";
        private const string KeyPrevScene = "RayneoIdle.GameScene.PrevScene";

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 1.0;

            float until = SessionState.GetFloat(KeyPlayUntil, 0);
            if (until > 0)
            {
                if (EditorApplication.isPlaying && EditorApplication.timeSinceStartup >= until) EditorApplication.ExitPlaymode();
                else if (!EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.timeSinceStartup >= until)
                {
                    SessionState.SetFloat(KeyPlayUntil, 0);
                    Debug.Log($"{LogTag} play check end");
                    File.WriteAllText(PlayResultFile, "done");
                    string prev = SessionState.GetString(KeyPrevScene, "");
                    if (!string.IsNullOrEmpty(prev)) EditorSceneManager.OpenScene(prev, OpenSceneMode.Single);
                }
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (File.Exists(RequestFile)) { File.Delete(RequestFile); Build(); }
            else if (File.Exists(PlayRequestFile)) { File.Delete(PlayRequestFile); PlayCheck(); }
        }

        [MenuItem("Tools/RayneoIdle/Play Check Game Scene")]
        private static void PlayCheck()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError($"{LogTag} the open scene has unsaved changes, save it and run again");
                return;
            }
            SessionState.SetString(KeyPrevScene, SceneManager.GetActiveScene().path);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SessionState.SetFloat(KeyPlayUntil, (float)EditorApplication.timeSinceStartup + PlayCheckSeconds);
            Debug.Log($"{LogTag} play check start");
            EditorApplication.EnterPlaymode();
        }

        [MenuItem("Tools/RayneoIdle/Build Game Scene")]
        public static void Build()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError($"{LogTag} the open scene has unsaved changes, save it and run again");
                return;
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrPluginPrefab);
            if (prefab == null)
            {
                Debug.LogError($"{LogTag} XR Plugin prefab not found at {XrPluginPrefab}");
                return;
            }
            string prevScene = SceneManager.GetActiveScene().path;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Head camera rig. RayNeo spec: solid black clear (= transparent), no skybox.
            var xr = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            xr.name = "XR Plugin";
            RenderSettings.skybox = null;

            // Zone system without its debug UI: the game's HUD / input / onboarding drive anchors.
            var zones = new GameObject("Zones");
            var boot = zones.AddComponent<AnchorZoneBootstrap>();
            var so = new SerializedObject(boot);
            var debugUi = so.FindProperty("withDebugUi");
            if (debugUi != null) { debugUi.boolValue = false; so.ApplyModifiedPropertiesWithoutUndo(); }
            else Debug.LogError($"{LogTag} AnchorZoneBootstrap.withDebugUi not found");

            var game = new GameObject("Game");
            game.AddComponent<GameSession>();
            game.AddComponent<DemoMode>();
            game.AddComponent<ZoneTodoService>();
            game.AddComponent<InputAdapter>();
            game.AddComponent<TreasureMapView>();
            game.AddComponent<MainHud>();
            game.AddComponent<PanelHost>();
            game.AddComponent<OnboardingController>();
            game.AddComponent<EditorHeadLook>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError($"{LogTag} could not save {ScenePath}");
                return;
            }

            // Game scene first in Build Settings; keep the test scenes after it.
            var list = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
            AssetDatabase.SaveAssets();

            Debug.Log($"{LogTag} built {ScenePath}; build order: {string.Join(", ", EditorBuildSettings.scenes.Select(s => Path.GetFileNameWithoutExtension(s.path)))}");
            File.WriteAllText(ResultFile, "done");

            if (!string.IsNullOrEmpty(prevScene) && prevScene != ScenePath) EditorSceneManager.OpenScene(prevScene, OpenSceneMode.Single);
        }
    }
}
