using System.IO;
using System.Linq;
using RayneoIdle.AimTest;
using RayneoIdle.Game;
using RayneoIdle.GameInput;
using RayneoIdle.Save;
using RayneoIdle.Todo;
using RayneoIdle.UI;
using RayneoIdle.Zones;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RayneoIdle.EditorTools
{
    /// <summary>
    /// Aim-to-select test scene Assets/Scenes/AimTest.unity and its own APK. The game scene and Build Settings
    /// are not touched: the APK is built from this scene only.
    /// Contents: same rig as the game scene (XR Plugin, Zones without debug UI, GameSession, ZoneTodoService,
    /// InputAdapter, TreasureMapView, MainHud, PanelHost, EditorHeadLook) plus GazeDotHider and AimTestController.
    /// No onboarding, no Demo Mode. GameSession uses SaveSystem.AimTestSlot, so the real save is never written.
    /// Start: Tools > RayneoIdle > Build Aim Test Scene / Build Aim Test APK, or the files
    /// Temp/build-aimtest-scene.request / Temp/build-aimtest-apk.request (results in Temp/*.result).
    /// The APK has the same package name as the game, so installing it replaces the game app on the glasses
    /// until the game APK is installed again (game save stays in slot 0, untouched).
    /// </summary>
    [InitializeOnLoad]
    public static class AimTestSceneBuilder
    {
        private const string LogTag = "[AimTestBuilder]";
        public const string ScenePath = "Assets/Scenes/AimTest.unity";
        public const string ApkPath = "../Build/RayneoIdle_AimTest.apk";
        private const string XrPluginPrefab = "Packages/com.unity.xr.rayneo.openxr/SDK/Runtime/Resources/Prefab/XR Plugin.prefab";
        private const string SceneRequest = "Temp/build-aimtest-scene.request";
        private const string SceneResult = "Temp/build-aimtest-scene.result";
        private const string ApkRequest = "Temp/build-aimtest-apk.request";
        private const string ApkResult = "Temp/build-aimtest-apk.result";

        private static double nextPoll;

        static AimTestSceneBuilder()
        {
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (File.Exists(SceneRequest)) { File.Delete(SceneRequest); BuildScene(); }
            else if (File.Exists(ApkRequest)) { File.Delete(ApkRequest); BuildApk(); }
        }

        [MenuItem("Tools/RayneoIdle/Build Aim Test Scene")]
        public static void BuildScene()
        {
            if (SceneManager.GetActiveScene().isDirty)
            {
                Finish(SceneResult, "FAILED: the open scene has unsaved changes, save it and run again");
                return;
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(XrPluginPrefab);
            if (prefab == null) { Finish(SceneResult, $"FAILED: XR Plugin prefab not found at {XrPluginPrefab}"); return; }
            string prevScene = SceneManager.GetActiveScene().path;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var xr = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            xr.name = "XR Plugin";
            RenderSettings.skybox = null;

            var zones = new GameObject("Zones");
            var boot = zones.AddComponent<AnchorZoneBootstrap>();
            SetField(boot, "withDebugUi", p => p.boolValue = false);

            var game = new GameObject("Game");
            var session = game.AddComponent<GameSession>();
            SetField(session, "startSlot", p => p.intValue = SaveSystem.AimTestSlot);
            game.AddComponent<ZoneTodoService>();
            game.AddComponent<InputAdapter>();
            game.AddComponent<TreasureMapView>();
            game.AddComponent<MainHud>();
            game.AddComponent<PanelHost>();
            game.AddComponent<EditorHeadLook>();
            game.AddComponent<GazeDotHider>();
            game.AddComponent<AimTestController>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) { Finish(SceneResult, $"FAILED: could not save {ScenePath}"); return; }
            AssetDatabase.SaveAssets();
            Finish(SceneResult, $"built {ScenePath} (not in Build Settings; game scene order unchanged)");

            if (!string.IsNullOrEmpty(prevScene) && prevScene != ScenePath) EditorSceneManager.OpenScene(prevScene, OpenSceneMode.Single);
        }

        [MenuItem("Tools/RayneoIdle/Build Aim Test APK")]
        public static void BuildApk()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Finish(ApkResult, $"FAILED: active build target is {EditorUserBuildSettings.activeBuildTarget}, switch to Android first");
                return;
            }
            if (!File.Exists(ScenePath)) { Finish(ApkResult, $"FAILED: {ScenePath} missing, build the scene first"); return; }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ApkPath)));
            Debug.Log($"{LogTag} building {ApkPath} from {ScenePath}");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });
            var s = report.summary;
            if (s.result == BuildResult.Succeeded)
                Finish(ApkResult, $"SUCCEEDED {Path.GetFullPath(ApkPath)} {s.totalSize / (1024f * 1024f):F1} MB in {s.totalTime.TotalSeconds:F0} s");
            else
            {
                var errors = report.steps.SelectMany(st => st.messages)
                    .Where(m => m.type == LogType.Error || m.type == LogType.Exception).Select(m => m.content).Take(10);
                Finish(ApkResult, $"FAILED ({s.result}, {s.totalErrors} errors)\n" + string.Join("\n", errors));
            }
        }

        private static void SetField(Object target, string field, System.Action<SerializedProperty> set)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"{LogTag} {target.GetType().Name}.{field} not found"); return; }
            set(p);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Finish(string resultFile, string message)
        {
            Debug.Log($"{LogTag} {message}");
            File.WriteAllText(resultFile, message);
        }
    }
}
