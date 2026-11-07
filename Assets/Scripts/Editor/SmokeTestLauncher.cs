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
    /// </summary>
    [InitializeOnLoad]
    public static class SmokeTestLauncher
    {
        private const string LogTag = "[SmokeTest]";
        private const string RequestFile = "Temp/smoke-test.request";
        private const string ResultFile = "Temp/smoke-test.result";

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

            if (State == Idle && File.Exists(RequestFile))
            {
                File.Delete(RequestFile);
                Begin();
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

            if (State == Phase1)
            {
                State = WaitPhase2;
                SessionState.SetFloat(KeyNextAt, (float)EditorApplication.timeSinceStartup + RelaunchDelaySeconds);
                Debug.Log($"{LogTag} phase 1 exited, relaunch in {RelaunchDelaySeconds} s");
            }
            else if (State == Phase2)
            {
                State = Idle;
                File.WriteAllText(ResultFile, "done");
                string prev = SessionState.GetString(KeyPrevScene, "");
                if (!string.IsNullOrEmpty(prev)) EditorSceneManager.OpenScene(prev, OpenSceneMode.Single);
                else EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                Debug.Log($"{LogTag} finished, scene restored");
            }
        }
    }
}
