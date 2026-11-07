using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RayneoIdle.EditorTools
{
    /// <summary>
    /// Builds the Android APK for the glasses from the enabled Build Settings scenes (Game.unity first).
    /// Output: ../Build/RayneoIdle_Prototype.apk (working folder, outside the repo).
    /// Start: Tools > RayneoIdle > Build APK, or the file Temp/build-apk.request.
    /// Summary in the Console / Editor.log ("[DeviceBuild]") and in Temp/build-apk.result.
    /// </summary>
    [InitializeOnLoad]
    public static class DeviceBuild
    {
        private const string LogTag = "[DeviceBuild]";
        private const string RequestFile = "Temp/build-apk.request";
        private const string ResultFile = "Temp/build-apk.result";
        public const string ApkPath = "../Build/RayneoIdle_Prototype.apk";

        private static double nextPoll;

        static DeviceBuild()
        {
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (EditorApplication.timeSinceStartup < nextPoll) return;
            nextPoll = EditorApplication.timeSinceStartup + 1.0;
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (!File.Exists(RequestFile)) return;
            File.Delete(RequestFile);
            Build();
        }

        [MenuItem("Tools/RayneoIdle/Build APK")]
        public static void Build()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            {
                Finish($"FAILED: active build target is {EditorUserBuildSettings.activeBuildTarget}, switch to Android first");
                return;
            }
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ApkPath)));
            Debug.Log($"{LogTag} building {ApkPath} with {string.Join(", ", scenes)}");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = ApkPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });

            var s = report.summary;
            if (s.result == BuildResult.Succeeded)
                Finish($"SUCCEEDED {Path.GetFullPath(ApkPath)} {s.totalSize / (1024f * 1024f):F1} MB in {s.totalTime.TotalSeconds:F0} s");
            else
            {
                var errors = report.steps.SelectMany(st => st.messages).Where(m => m.type == LogType.Error || m.type == LogType.Exception)
                    .Select(m => m.content).Take(10);
                Finish($"FAILED ({s.result}, {s.totalErrors} errors)\n" + string.Join("\n", errors));
            }
        }

        private static void Finish(string message)
        {
            Debug.Log($"{LogTag} {message}");
            File.WriteAllText(ResultFile, message);
        }
    }
}
