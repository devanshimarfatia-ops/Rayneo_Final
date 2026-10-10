using System.Collections.Generic;
using System.IO;
using System.Linq;
using FfalconXR.InputModule; // XRGraphicRaycaster (ARDK UnityXRSDKCore.dll)
using RayNeo;                // HeadTrackedPoseDriver
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RayneoIdle.Zones.EditorTools
{
    /// <summary>
    /// Menu: Rayneo > Setup Anchor Zone Scene
    /// Builds Assets/Scenes/AnchorZoneTest.unity from scratch:
    ///   - XR Plugin prefab (replaces Main Camera), camera clear = solid black, no skybox
    ///   - ZoneSystem with all AnchorZone components, references wired
    ///   - head-locked World Space HUD canvas (640x480 reference) with XRGraphicRaycaster
    ///   - scene saved and put first in Build Settings
    /// Re-running it regenerates the scene (asks before overwriting).
    /// </summary>
    public static class AnchorZoneSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/AnchorZoneTest.unity";

        // XR Plugin prefab inside the RayNeo ARDK package. The GUID is used first so the
        // lookup still works if the package is referenced from a different folder.
        private const string XRPluginPrefabGuid = "f82accd910f54e44cb54d6e87ad284d6";
        private const string XRPluginPrefabPath =
            "Packages/com.unity.xr.rayneo.openxr/SDK/Runtime/Resources/Prefab/XR Plugin.prefab";

        // ---- HUD layout (RayNeo design specs) ----
        // Canvas reference 640x480 px covering a 30 deg FOV (treated as the diagonal).
        private const float CanvasWidthPx = 640f;
        private const float CanvasHeightPx = 480f;
        private const float DisplayFovDeg = 30f;
        // HUD distance in front of the eyes. Scale is derived so the 640x480 canvas fills the FOV.
        private const float HudDistanceM = 2f;
        // Black safety margin, spec says 16-30 px.
        private const float MarginPx = 24f;
        // Font sizes: title 28-40, body 20-24, aux 16-18 (min 16).
        private const int ModeFontSize = 32;
        private const int ToastFontSize = 22;
        private const int StatusFontSize = 18;

        private static readonly Color Black = new Color(0f, 0f, 0f, 0f); // #000000 = transparent on the glasses
        private static readonly Color Cyan = new Color32(0x4D, 0xE2, 0xFF, 0xFF);
        private static readonly Color Green = new Color32(0x44, 0xFF, 0x66, 0xFF);

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

            // ---- Camera rig ----
            var xrPlugin = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            xrPlugin.name = "XR Plugin";
            var driver = xrPlugin.GetComponentInChildren<HeadTrackedPoseDriver>(true);
            var cam = driver != null ? driver.GetComponent<Camera>() : xrPlugin.GetComponentInChildren<Camera>(true);
            if (cam == null)
            {
                Debug.LogError("[AnchorZoneSetup] No Camera found under the XR Plugin prefab.");
                return;
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Black;
            cam.gameObject.AddComponent<EditorHeadMover>(); // Editor-only WASD walking, no-op on device

            // ---- ZoneSystem ----
            var zoneSystem = new GameObject("ZoneSystem");
            var slam = zoneSystem.AddComponent<SlamSessionController>();
            var store = zoneSystem.AddComponent<ZoneAnchorStore>();
            var detector = zoneSystem.AddComponent<ZoneDetector>();
            var modes = zoneSystem.AddComponent<ModeManager>();
            var input = zoneSystem.AddComponent<AnchorPlacementInput>();
            var hud = zoneSystem.AddComponent<ZoneDebugHUD>();

            Wire(detector, ("store", store), ("slam", slam), ("head", cam.transform));
            Wire(modes, ("detector", detector));
            Wire(input, ("store", store), ("detector", detector), ("slam", slam));

            // ---- HUD canvas ----
            var canvas = CreateHudCanvas(cam.transform);
            var modeText = CreateText(canvas, "ModeText", ModeFontSize, Cyan, TextAnchor.UpperLeft,
                // top-left, second row (first row belongs to toasts)
                anchor: new Vector2(0f, 1f), pivot: new Vector2(0f, 1f),
                pos: new Vector2(MarginPx, -(MarginPx + 40f)), size: new Vector2(CanvasWidthPx - 2 * MarginPx, 44f));
            var toastText = CreateText(canvas, "ToastText", ToastFontSize, Cyan, TextAnchor.UpperCenter,
                // top zone = toasts
                anchor: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
                pos: new Vector2(0f, -MarginPx), size: new Vector2(CanvasWidthPx - 2 * MarginPx, 32f));
            var statusText = CreateText(canvas, "StatusText", StatusFontSize, Green, TextAnchor.LowerLeft,
                // bottom zone = hints, two lines
                anchor: new Vector2(0f, 0f), pivot: new Vector2(0f, 0f),
                pos: new Vector2(MarginPx, MarginPx), size: new Vector2(CanvasWidthPx - 2 * MarginPx, 52f));

            Wire(hud, ("detector", detector), ("modes", modes), ("slam", slam), ("input", input),
                ("store", store), ("modeText", modeText), ("statusText", statusText), ("toastText", toastText));

            // ---- Save + Build Settings ----
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("[AnchorZoneSetup] Failed to save " + ScenePath);
                return;
            }
            AddToBuildSettingsFirst(ScenePath);

            Selection.activeGameObject = zoneSystem;
            Debug.Log("[AnchorZoneSetup] Created " + ScenePath + " and set it as the first scene in Build Settings.");
        }

        private static GameObject LoadXRPluginPrefab()
        {
            string path = AssetDatabase.GUIDToAssetPath(XRPluginPrefabGuid);
            var prefab = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return prefab != null ? prefab : AssetDatabase.LoadAssetAtPath<GameObject>(XRPluginPrefabPath);
        }

        /// <summary>Head-locked (0DoF) World Space canvas sized so 640x480 px fills the display FOV.</summary>
        private static RectTransform CreateHudCanvas(Transform head)
        {
            var go = new GameObject("HUD Canvas", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(head, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = head.GetComponent<Camera>();
            // RayNeo replacement for GraphicRaycaster. No plain GraphicRaycaster is added.
            go.AddComponent<XRGraphicRaycaster>();

            // Visible diagonal at HudDistanceM = 2 * d * tan(FOV/2); divide by the canvas diagonal in px.
            float diagPx = Mathf.Sqrt(CanvasWidthPx * CanvasWidthPx + CanvasHeightPx * CanvasHeightPx);
            float metersPerPx = 2f * HudDistanceM * Mathf.Tan(DisplayFovDeg * 0.5f * Mathf.Deg2Rad) / diagPx;

            rt.sizeDelta = new Vector2(CanvasWidthPx, CanvasHeightPx);
            rt.localPosition = new Vector3(0f, 0f, HudDistanceM);
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one * metersPerPx;
            return rt;
        }

        private static Text CreateText(RectTransform parent, string name, int fontSize, Color color, TextAnchor align,
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;          // spec: Bold/Medium only
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = align;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;               // display only, keeps UI raycasts cheap
            text.text = "";
            return text;
        }

        /// <summary>Assigns private [SerializeField] references by field name.</summary>
        private static void Wire(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs)
            {
                var prop = so.FindProperty(field);
                if (prop == null)
                {
                    Debug.LogError($"[AnchorZoneSetup] {target.GetType().Name} has no field '{field}'");
                    continue;
                }
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
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
