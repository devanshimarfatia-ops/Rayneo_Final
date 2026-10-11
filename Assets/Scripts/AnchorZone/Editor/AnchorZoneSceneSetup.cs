using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RayneoIdle.Zones.EditorTools
{
    /// <summary>
    /// Optional shortcut. The normal workflow is to add AnchorZoneBootstrap to an existing scene.
    ///
    /// Menu: Rayneo > Setup Anchor Zone Scene
    /// Creates Assets/Scenes/AnchorZoneTest.unity with only the XR Plugin prefab and one
    /// GameObject carrying AnchorZoneBootstrap (which builds everything else at runtime),
    /// then puts the scene first in Build Settings. Asks before overwriting.
    /// </summary>
    public static class AnchorZoneSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/AnchorZoneTest.unity";

        // XR Plugin prefab inside the RayNeo ARDK package. The GUID is used first so the
        // lookup still works if the package is referenced from a different folder.
        private const string XRPluginPrefabGuid = "f82accd910f54e44cb54d6e87ad284d6";
        private const string XRPluginPrefabPath =
            "Packages/com.unity.xr.rayneo.openxr/SDK/Runtime/Resources/Prefab/XR Plugin.prefab";

        [MenuItem("Rayneo/Setup Anchor Zone Scene")]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Setup Anchor Zone Scene",
                    ScenePath + " already exists. Regenerate it? Manual changes in that scene will be lost.",
                    "Regenerate", "Cancel"))
                return;

            var prefab = LoadXRPluginPrefab();
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Setup Anchor Zone Scene",
                    "Could not find the 'XR Plugin' prefab from the RayNeo ARDK package.", "OK");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // No skybox: anything not black is visible light on the glasses.
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;

            var xrPlugin = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            xrPlugin.name = "XR Plugin";

            var zones = new GameObject("AnchorZones");
            zones.AddComponent<AnchorZoneBootstrap>();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("[AnchorZoneSetup] Failed to save " + ScenePath);
                return;
            }
            AddToBuildSettingsFirst(ScenePath);

            Selection.activeGameObject = zones;
            Debug.Log("[AnchorZoneSetup] Created " + ScenePath + " and set it as the first scene in Build Settings.");
        }

        private static GameObject LoadXRPluginPrefab()
        {
            string path = AssetDatabase.GUIDToAssetPath(XRPluginPrefabGuid);
            var prefab = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null ? prefab : AssetDatabase.LoadAssetAtPath<GameObject>(XRPluginPrefabPath);
        }

        /// <summary>Puts the scene at index 0 (launch scene) and keeps the existing entries after it.</summary>
        private static void AddToBuildSettingsFirst(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes.Where(s => s.path != path));
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
