using FfalconXR.InputModule; // XRGraphicRaycaster (ARDK UnityXRSDKCore.dll)
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.Zones
{
    /// <summary>
    /// Minimal debug HUD following RayNeo display specs:
    /// black (transparent) background, bold text >= 16px, cyan/green palette,
    /// toast in the top area that hides after ~2.5 s, anchor menu just above the center dot.
    /// Builds its own head-locked (0DoF) canvas in Configure(). Updates text at ~5 Hz to save power.
    /// </summary>
    public class ZoneDebugHUD : MonoBehaviour
    {
        [SerializeField] private float toastSeconds = 2.5f;

        // ---- Layout (RayNeo design specs) ----
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
        private const int MenuFontSize = 24;
        // Gap between the center gaze dot and the bottom of the anchor menu.
        private const float MenuGapPx = 30f;

        private static readonly Color Cyan = new Color32(0x4D, 0xE2, 0xFF, 0xFF);
        private static readonly Color Green = new Color32(0x44, 0xFF, 0x66, 0xFF);
        private static readonly Color Purple = new Color32(0x85, 0x66, 0xFF, 0xFF);
        private const string CyanHex = "#4DE2FF";

        private ZoneDetector detector;
        private ModeManager modes;
        private SlamSessionController slam;
        private AnchorPlacementInput input;
        private ZoneAnchorStore store;

        private Text modeText;    // top-left, second row
        private Text toastText;   // top-center
        private Text menuText;    // center, above the gaze dot; empty when the menu is closed
        private Text statusText;  // bottom

        private float toastHideTime;
        private float nextRefresh;

        /// <summary>
        /// Runtime wiring, used by AnchorZoneBootstrap. Call before the component is enabled.
        /// Also builds the HUD canvas as a child of the head camera.
        /// </summary>
        public void Configure(ZoneDetector detector, ModeManager modes, SlamSessionController slam,
            AnchorPlacementInput input, ZoneAnchorStore store, Transform head)
        {
            this.detector = detector;
            this.modes = modes;
            this.slam = slam;
            this.input = input;
            this.store = store;
            BuildCanvas(head);
        }

        private void OnEnable()
        {
            input.OnToast += ShowToast;
            input.OnMenuChanged += RefreshMenu;
            modes.OnModeChanged += HandleModeChanged;
        }

        private void OnDisable()
        {
            input.OnToast -= ShowToast;
            input.OnMenuChanged -= RefreshMenu;
            modes.OnModeChanged -= HandleModeChanged;
        }

        private void Start()
        {
            toastText.text = "";
            RefreshMenu();
            HandleModeChanged(AppMode.Default, modes.CurrentMode);
        }

        private void Update()
        {
            if (toastText.text.Length > 0 && Time.unscaledTime > toastHideTime) toastText.text = "";

            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.2f;

            // s = raw Algorithm.GetSlamStatus(): 0 init, 1 success, 2 fail, -1 not polled (Editor)
            string track = (slam.IsTracking ? "TRACKING" : "NO TRACKING") +
                           (slam.RawStatus >= 0 ? $" s{slam.RawStatus}" : "");
            string dist = float.IsInfinity(detector.NearestDistance) ? "-" : $"{detector.NearestDistance:F1}m";
            string dwell = detector.CandidateAnchor != null
                ? $"  entering {detector.CandidateAnchor.zoneType} {detector.CandidateProgress * 100f:F0}%"
                : "";
            statusText.text = $"{track} | anchors {store.Anchors.Count} | near {dist}\n" +
                              (input.IsMenuOpen ? "Swipe: choose  Tap: confirm" : "Tap: set anchor  3x tap: remove") +
                              dwell;
        }

        private void HandleModeChanged(AppMode prev, AppMode mode)
        {
            modeText.text = mode == AppMode.Default ? "" : $"{mode.ToString().ToUpper()} MODE";
            modeText.color = mode == AppMode.Work ? Cyan : mode == AppMode.Bedroom ? Purple : Green;
            if (mode != AppMode.Default) ShowToast($"Entered {mode} zone");
            else if (prev != AppMode.Default) ShowToast($"Left {prev} zone");
        }

        // Trigger: AnchorPlacementInput.OnMenuChanged (open / close / selection moved).
        // Selected option = cyan with brackets, others = the text's base color (green).
        private void RefreshMenu()
        {
            if (!input.IsMenuOpen) { menuText.text = ""; return; }

            var sb = new System.Text.StringBuilder("<size=20>SET ANCHOR</size>\n");
            foreach (var type in AnchorPlacementInput.MenuOrder)
            {
                string name = type.ToString().ToUpper();
                sb.Append(type == input.SelectedType ? $"<color={CyanHex}>[ {name} ]</color>" : name);
                sb.Append("    ");
            }
            menuText.text = sb.ToString().TrimEnd();
        }

        private void ShowToast(string msg)
        {
            toastText.text = msg;
            toastText.color = Cyan;
            toastHideTime = Time.unscaledTime + toastSeconds;
        }

        // ---------------------------------------------------------------- canvas

        private void BuildCanvas(Transform head)
        {
            var go = new GameObject("Zone HUD Canvas", typeof(RectTransform));
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) go.layer = uiLayer;
            var canvasRt = (RectTransform)go.transform;
            canvasRt.SetParent(head, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = head.GetComponent<Camera>();
            // RayNeo replacement for GraphicRaycaster. No plain GraphicRaycaster is added.
            go.AddComponent<XRGraphicRaycaster>();

            // Visible diagonal at HudDistanceM = 2 * d * tan(FOV/2); divide by the canvas diagonal in px.
            float diagPx = Mathf.Sqrt(CanvasWidthPx * CanvasWidthPx + CanvasHeightPx * CanvasHeightPx);
            float metersPerPx = 2f * HudDistanceM * Mathf.Tan(DisplayFovDeg * 0.5f * Mathf.Deg2Rad) / diagPx;
            canvasRt.sizeDelta = new Vector2(CanvasWidthPx, CanvasHeightPx);
            canvasRt.localPosition = new Vector3(0f, 0f, HudDistanceM);
            canvasRt.localRotation = Quaternion.identity;
            canvasRt.localScale = Vector3.one * metersPerPx;

            float innerWidth = CanvasWidthPx - 2 * MarginPx;
            // top zone = toasts
            toastText = CreateText(canvasRt, "ToastText", ToastFontSize, Cyan, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -MarginPx), new Vector2(innerWidth, 32f));
            // top-left, second row (first row belongs to toasts)
            modeText = CreateText(canvasRt, "ModeText", ModeFontSize, Cyan, TextAnchor.UpperLeft,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(MarginPx, -(MarginPx + 40f)), new Vector2(innerWidth, 44f));
            // center zone, bottom edge MenuGapPx above the gaze dot so the dot stays visible
            menuText = CreateText(canvasRt, "MenuText", MenuFontSize, Green, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0f, MenuGapPx), new Vector2(innerWidth, 70f));
            // bottom zone = hints, two lines
            statusText = CreateText(canvasRt, "StatusText", StatusFontSize, Green, TextAnchor.LowerLeft,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(MarginPx, MarginPx), new Vector2(innerWidth, 52f));
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
            text.supportRichText = true;              // menu uses <color>/<size>
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;               // display only, keeps UI raycasts cheap
            text.text = "";
            return text;
        }
    }
}
