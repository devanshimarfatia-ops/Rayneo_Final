using System.IO;
using RayneoIdle.Game;
using RayneoIdle.Todo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RayneoIdle.EditorTools
{
    /// <summary>
    /// Runs the SmokeTest in a temporary scene, in two Play sessions (phase 1, then a relaunch check).
    /// Start: menu Tools > RayneoIdle > Run Smoke Test, or create the file Temp/smoke-test.request
    /// in the project folder (polled once per second while the Editor is idle; lets scripts start a run).
    /// The open scene must be saved first (it is replaced by a temporary scene, and reopened at the end).
    /// Results: "[SmokeTest]" lines in the Console / Editor.log; summary also in Temp/smoke-test.result.
    /// Also: Temp/hud-snapshot.request renders HUD screenshots (UI/HudSnapshot), and
    /// Temp/refresh.request runs AssetDatabase.Refresh (re-import + compile without focusing the Editor).
    /// </summary>
    [InitializeOnLoad]
    public static class SmokeTestLauncher
    {
        private const string LogTag = "[SmokeTest]";
        private const string RequestFile = "Temp/smoke-test.request";
        private const string ResultFile = "Temp/smoke-test.result";
        private const string RefreshRequestFile = "Temp/refresh.request";

        // SessionState survives the domain reload of entering / leaving Play mode.
        private const string KeyState = "RayneoIdle.SmokeTest.State";
        private const string KeyPrevScene = "RayneoIdle.SmokeTest.PrevScene";
        private const string KeyNextAt = "RayneoIdle.SmokeTest.NextAt";

        // State machine: 0 idle, 1 phase 1 running, 2 waiting before phase 2, 3 phase 2 running.
        private const int Idle = 0, Phase1 = 1, WaitPhase2 = 2, Phase2 = 3;

        /// <summary>Real seconds between phase 1 exit and phase 2 start (the "time away" being settled).</summary>
        private const float RelaunchDelaySeconds = 5f;

        private static double nextPoll;

        static SmokeTestLauncher()
        {
            EditorApplication.update += Poll;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Tools/RayneoIdle/Run Smoke Test")]
        private static void MenuRun() => Begin();

        [MenuItem("Tools/RayneoIdle/HUD Snapshot")]
        private static void MenuSnapshot() => BeginSnapshot();

        // HUD snapshot: one Play session that renders Temp/hud_*.png (see UI/HudSnapshot).
        private const string SnapshotRequestFile = "Temp/hud-snapshot.request";
        private const string SnapshotResultFile = "Temp/hud-snapshot.result";
        private const int Snapshot = 4;

        [MenuItem("Tools/RayneoIdle/Onboarding Test")]
        private static void MenuOnboarding() => BeginSnapshot(onboarding: true);

        // Onboarding test: same game scene + OnboardingController, driven by Onboarding/OnboardingTest.
        private const string OnboardingRequestFile = "Temp/onboarding-test.request";

        /// <summary>Builds the full game UI scene (no anchors) and runs HudSnapshot or OnboardingTest.</summary>
        private static void BeginSnapshot(bool onboarding = false)
        {
            if (!CanStart()) return;
            if (File.Exists(SnapshotResultFile)) File.Delete(SnapshotResultFile);
            SessionState.SetString(KeyPrevScene, SceneManager.GetActiveScene().path);
            State = Snapshot;

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var headGo = new GameObject("Head", typeof(Camera));
            headGo.tag = "MainCamera";
            var go = new GameObject(onboarding ? "OnboardingTest" : "HudSnapshot");
            go.AddComponent<GameSession>();
            go.AddComponent<DemoMode>();
            go.AddComponent<ZoneTodoService>();
            go.AddComponent<GameInput.InputAdapter>();
            go.AddComponent<UI.TreasureMapView>();
            go.AddComponent<UI.MainHud>();
            go.AddComponent<UI.PanelHost>();
            if (onboarding)
            {
                go.AddComponent<Onboarding.OnboardingController>();
                go.AddComponent<Onboarding.OnboardingTest>();
            }
            else go.AddComponent<UI.HudSnapshot>();
            Debug.Log($"{LogTag} starting {(onboarding ? "onboarding test" : "HUD snapshot")}");
            EditorApplication.EnterPlaymode();
        }

        private static bool CanStart()
        {
            if (State != Idle || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning($"{LogTag} already running or in Play mode");
                return false;
            }
            if (SceneManager.GetActiveScene().isDirty)
            {
                Debug.LogError($"{LogTag} the open scene has unsaved changes, save it and run again");
                return false;
            }
            return true;
        }

        private static void RestoreScene()
        {
            string prev = SessionState.GetString(KeyPrevScene, "");
            if (!string.IsNullOrEmpty(prev)) EditorSceneManager.OpenScene(prev, OpenSceneMode.Single);
            else EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        private static int State
        {
            get => SessionState.GetInt(KeyState, Idle);
            set => SessionState.SetInt(KeyState, value);
        }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;

            // Trigger: Temp/refresh.request -> re-import changed files and recompile, without focusing the Editor.
            if (File.Exists(RefreshRequestFile))
            {
                File.Delete(RefreshRequestFile);
                Debug.Log($"{LogTag} refresh requested");
                AssetDatabase.Refresh();
                return;
            }

            if (State == Idle && File.Exists(RequestFile))
            {
                File.Delete(RequestFile);
                Begin();
            }
            else if (State == Idle && File.Exists(SnapshotRequestFile))
            {
                File.Delete(SnapshotRequestFile);
                BeginSnapshot();
            }
            else if (State == Idle && File.Exists(OnboardingRequestFile))
            {
                File.Delete(OnboardingRequestFile);
                BeginSnapshot(onboarding: true); // result file: Temp/hud-snapshot.result
            }
            else if (State == WaitPhase2 && EditorApplication.timeSinceStartup >= SessionState.GetFloat(KeyNextAt, 0))
            {
                State = Phase2;
                StartPhase(2);
            }
        }

        private static void Begin()
        {
            if (State != Idle || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning($"{LogTag} already running or in Play mode");
                return;
            }
            var active = SceneManager.GetActiveScene();
            if (active.isDirty)
            {
                Debug.LogError($"{LogTag} the open scene has unsaved changes, save it and run again");
                return;
            }
            if (File.Exists(ResultFile)) File.Delete(ResultFile);
            SessionState.SetString(KeyPrevScene, active.path);
            State = Phase1;
            StartPhase(1);
        }

        private static void StartPhase(int phase)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("SmokeTest");
            go.AddComponent<GameSession>();
            go.AddComponent<DemoMode>();
            go.AddComponent<ZoneTodoService>();
            go.AddComponent<SmokeTest>().phase = phase;
            Debug.Log($"{LogTag} starting phase {phase}");
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode) return;

            if (State == Snapshot)
            {
                State = Idle;
                File.WriteAllText(SnapshotResultFile, "done");
                RestoreScene();
                Debug.Log($"{LogTag} HUD snapshot finished, scene restored");
            }
            else if (State == Phase1)
            {
                State = WaitPhase2;
                SessionState.SetFloat(KeyNextAt, (float)EditorApplication.timeSinceStartup + RelaunchDelaySeconds);
                Debug.Log($"{LogTag} phase 1 exited, relaunch in {RelaunchDelaySeconds} s");
            }
            else if (State == Phase2)
            {
                State = Idle;
                File.WriteAllText(ResultFile, "done");
                RestoreScene();
                Debug.Log($"{LogTag} finished, scene restored");
            }
        }
    }
}
