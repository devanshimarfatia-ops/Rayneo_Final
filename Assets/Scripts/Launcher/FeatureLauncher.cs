using System;
using FfalconXR.InputModule; // XRGraphicRaycaster (ARDK UnityXRSDKCore.dll)
using RayNeo;                // SimpleTouchForLite, HeadTrackedPoseDriver
using RayneoIdle.Todo;
using RayneoIdle.Zones;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RayneoIdle.Launcher
{
    /// <summary>
    /// Plan B home screen: square tiles ANCHOR, TODO (open the existing features in one scene) and QUIT.
    /// Drop-in component: add it next to the RayNeo "XR Plugin" prefab. It needs nothing else in the scene;
    /// the feature bootstraps are created as inactive children and only built on first open.
    ///
    ///   Home view:
    ///     Swipe fwd/back -> move focus between the tiles   (Editor: Left / Right arrow)
    ///     Single tap     -> open the focused feature        (Editor: Space)
    ///                       on QUIT: close the app (Application.Quit, same as the ARDK HelloRayNeo
    ///                       sample), no long press on the temple needed. Editor: stops Play mode.
    ///     Double tap     -> not used (left to the system back / exit)
    ///   Inside a feature:
    ///     Double tap / Android back key -> back to the home screen (Editor: Escape)
    ///     Blocked while the anchor "Set anchor" menu is open: there double tap = cancel the menu.
    ///     All other input belongs to the feature itself.
    ///
    /// Only one feature has input / a visible HUD at a time. Leaving a feature disables its "front"
    /// components (input + HUD + markers); each one hides its own head-locked canvas in OnDisable.
    /// The anchor back end (SLAM, detector, ModeManager) keeps running by default, see keepAnchorRunning.
    /// </summary>
    [DisallowMultipleComponent]
    public class FeatureLauncher : MonoBehaviour
    {
        [Tooltip("Head camera. Leave empty to use the XR Plugin's HeadTrackedPoseDriver camera (or Camera.main).")]
        [SerializeField] private Transform head;

        [Header("Features (optional)")]
        [Tooltip("Leave empty: an inactive child with default tuning is created. If you assign one for custom " +
                 "tuning, its GameObject MUST start inactive, otherwise it builds itself before the launcher.")]
        [SerializeField] private AnchorZoneBootstrap anchorFeature;
        [Tooltip("Same rule as Anchor Feature: leave empty or assign an INACTIVE GameObject.")]
        [SerializeField] private VoiceTodoTestBootstrap todoFeature;

        [Header("Behaviour")]
        [Tooltip("ON: after leaving the anchor screen, SLAM + zone detection keep running (ModeManager still " +
                 "fires, anchors stay valid). OFF: the whole zone system is deactivated, which also calls " +
                 "DisableSlamHeadTracker (saves power, but the SLAM origin may reset on re-open, untested).")]
        [SerializeField] private bool keepAnchorRunning = true;
        [Tooltip("Flip if swiping forward on the temple moves the tile focus the wrong way.")]
        [SerializeField] private bool invertSwipe = false;
        [Tooltip("Swipe events fire repeatedly during one long swipe. Swipes closer together than this " +
                 "are ignored, so one swipe = one step.")]
        [SerializeField] private float swipeCooldownSeconds = 0.35f;
        [Tooltip("BACK DEBOUNCE (s): double tap and the Android back key may both arrive for one gesture; " +
                 "a second back within this time is ignored.")]
        [SerializeField] private float backDebounceSeconds = 0.6f;
        [Tooltip("Force the camera to clear to solid black (#000000 = transparent on the glasses).")]
        [SerializeField] private bool forceBlackBackground = true;

        // ---- Home screen layout, in canvas px (canvas = 640 x 480) ----
        // Changes in the Inspector apply LIVE during Play (OnValidate -> ApplyLayout). Play-mode edits are
        // lost when Play stops: right-click the component > Copy Component, stop, then Paste Component Values.
        [Header("Layout (live-editable in Play mode)")]
        [Tooltip("Side length of each square tile (px).")]
        [Range(60f, 220f)] [SerializeField] private float tileSizePx = 90f;
        [Tooltip("Horizontal gap between neighbouring tiles (px).")]
        [Range(0f, 200f)] [SerializeField] private float tileGapPx = 30f;
        [Tooltip("Distance from the canvas BOTTOM edge to the bottom of the tiles (px). " +
                 "Keep >= 24 (RayNeo black safety margin 16-30 px). About 195 = vertically centered.")]
        [Range(0f, 400f)] [SerializeField] private float tilesBottomPx = 40f;
        [Tooltip("Shift the tile row left (-) / right (+) (px).")]
        [Range(-250f, 250f)] [SerializeField] private float tilesOffsetXPx = 0f;
        [Tooltip("Show the TEST FEATURES title right above the tiles.")]
        [SerializeField] private bool showTitle = true;
        [Tooltip("Gap between the title and the top of the tiles (px).")]
        [Range(0f, 60f)] [SerializeField] private float titleGapPx = 8f;
        [Tooltip("Show the small second line inside each tile (zones / voice / exit app).")]
        [SerializeField] private bool showSubtitle = true;
        [Tooltip("Show the input hint at the bottom edge. If ON, raise Tiles Bottom so they do not overlap.")]
        [SerializeField] private bool showHint = false;
        [Tooltip("Font sizes (px). Values below 16 are raised to 16 (RayNeo spec minimum).")]
        [SerializeField] private int titleFontSize = 18;
        [SerializeField] private int tileFontSize = 18;
        [SerializeField] private int tileSubFontSize = 16;
        [SerializeField] private int hintFontSize = 16;
        [Tooltip("Frame thickness of the focused / unfocused tile (px). Values below 2 are raised to 2 (spec).")]
        [SerializeField] private float focusLinePx = 3f;
        [SerializeField] private float idleLinePx = 2f;

        // ---- Fixed canvas math (RayNeo design specs, same as ZoneDebugHUD) ----
        private const float CanvasWidthPx = 640f;
        private const float CanvasHeightPx = 480f;
        private const float DisplayFovDeg = 30f;
        private const float HudDistanceM = 2f;
        private const float MarginPx = 24f;
        private const int MinFontPx = 16;
        private const float MinLinePx = 2f;

        private static readonly Color Cyan = new Color32(0x4D, 0xE2, 0xFF, 0xFF);
        private static readonly Color Green = new Color32(0x44, 0xFF, 0x66, 0xFF);
        private static readonly Color Purple = new Color32(0x85, 0x66, 0xFF, 0xFF);

        private enum View { Home, Anchor, Todo }
        // Tile order, left to right. Add new tiles here, in BuildCanvas and in HandleTap.
        private const int TileAnchor = 0, TileTodo = 1, TileQuit = 2;
        private const int TileCount = 3;

        /// <summary>One square tile: frame of four lines + title + subtitle.</summary>
        private class Tile
        {
            public RectTransform root;
            public Image[] lines;
            public RectTransform[] lineRts;
            public Text title, sub;
        }

        private View view = View.Home;
        private int focused;
        private GameObject canvasGo;
        private Tile[] tiles;
        private Text titleText, hintText;

        // Requests from input handlers, applied in LateUpdate (after every Update this frame), so the
        // tap / key that opened a feature is never also seen by that feature's own handlers.
        private View? pendingOpen;
        private bool pendingBack;
        private bool pendingQuit;
        // Anchor menu state as of the END of the previous frame. Checked on double tap, so the result does
        // not depend on whether the launcher or AnchorPlacementInput receives the touch event first.
        private bool featureBlocksBackLastFrame;
        private float lastSwipeTime = -999f;
        private float lastBackTime = -999f;
        private bool touchpadBound;

        // Front components of each feature (found after the feature is first built).
        private GameObject anchorSystem; // "ZoneSystem" created by AnchorZoneBootstrap
        private AnchorPlacementInput anchorInput;
        private ZoneDebugHUD anchorHud;
        private ZoneAnchorMarkers anchorMarkers;
        private VoiceTodoTestPanel todoPanel;

        private void Awake()
        {
            if (head == null) head = FindHead();
            if (head == null)
            {
                Debug.LogError("[Launcher] No head camera found. Add the RayNeo 'XR Plugin' prefab to the scene.");
                enabled = false;
                return;
            }

            var cam = head.GetComponent<Camera>();
            if (forceBlackBackground && cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            }

            // Inactive children: their Awake (which builds the feature) runs only on first SetActive(true).
            if (anchorFeature == null) anchorFeature = CreateFeatureChild<AnchorZoneBootstrap>("Feature Anchor");
            if (todoFeature == null) todoFeature = CreateFeatureChild<VoiceTodoTestBootstrap>("Feature Todo");
            WarnIfActive(anchorFeature);
            WarnIfActive(todoFeature);

            BuildCanvas();
            ApplyLayout();
            Debug.Log("[Launcher] Home view ready");
        }

        private T CreateFeatureChild<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.SetActive(false);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        private static void WarnIfActive(Component feature)
        {
            if (feature != null && feature.gameObject.activeSelf)
                Debug.LogWarning($"[Launcher] {feature.gameObject.name} starts ACTIVE: it builds itself at load " +
                                 "and its input runs on the home screen. Set the GameObject inactive.");
        }

        private static Transform FindHead()
        {
            var driver = FindObjectOfType<HeadTrackedPoseDriver>();
            if (driver != null) return driver.transform;
            return Camera.main != null ? Camera.main.transform : null;
        }

        // ------------------------------------------------------------------ input

        private void OnEnable()
        {
            if (Application.isEditor) return; // Editor uses the keyboard stand-ins in Update
            SimpleTouchForLite touch;
            try { touch = SimpleTouchForLite.Instance; }
            catch (Exception e)
            {
                Debug.LogError($"[Launcher] Touchpad init failed, check RayNeoGeneralSettings touchConfigs: {e.Message}");
                return;
            }
            touch.OnSimpleTap.AddListener(HandleTap);
            touch.OnDoubleTap.AddListener(HandleDoubleTap);
            // Per-step horizontal swipe events, same as AnchorPlacementInput (the *End variants never fire).
            touch.OnSwipeRight.AddListener(HandleSwipeRight);
            touch.OnSwipeLeft.AddListener(HandleSwipeLeft);
            touchpadBound = true;
        }

        private void OnDisable()
        {
            if (!touchpadBound || !SimpleTouchForLite.SingletonExist) return;
            var touch = SimpleTouchForLite.Instance;
            touch.OnSimpleTap.RemoveListener(HandleTap);
            touch.OnDoubleTap.RemoveListener(HandleDoubleTap);
            touch.OnSwipeRight.RemoveListener(HandleSwipeRight);
            touch.OnSwipeLeft.RemoveListener(HandleSwipeLeft);
            touchpadBound = false;
        }

        private void Update()
        {
#if UNITY_EDITOR
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame) HandleTap();
                if (kb.rightArrowKey.wasPressedThisFrame) HandleSwipe(+1);
                if (kb.leftArrowKey.wasPressedThisFrame) HandleSwipe(-1);
                if (kb.escapeKey.wasPressedThisFrame) HandleDoubleTap();
            }
#else
            // Trigger: Android back key (legacy Input maps it to Escape; Active Input Handling = Both).
            // Treated like a double tap, in case the system turns the gesture into a back key.
            if (Input.GetKeyDown(KeyCode.Escape)) HandleDoubleTap();
#endif
        }

        private void LateUpdate()
        {
            if (pendingOpen.HasValue)
            {
                var target = pendingOpen.Value;
                pendingOpen = null;
                Open(target);
            }
            else if (pendingBack)
            {
                pendingBack = false;
                GoHome();
            }
            else if (pendingQuit)
            {
                pendingQuit = false;
                Quit();
            }

            // Snapshot for the next frame's double tap: true while the anchor menu is open
            // (the double tap then belongs to AnchorPlacementInput = cancel the menu).
            featureBlocksBackLastFrame = view == View.Anchor && anchorInput != null && anchorInput.IsMenuOpen;
        }

        // Trigger: SimpleTouchForLite.OnSimpleTap (or Space). Only acts on the home screen.
        private void HandleTap()
        {
            if (view != View.Home || pendingOpen.HasValue || pendingQuit) return;
            if (focused == TileQuit) pendingQuit = true;
            else pendingOpen = focused == TileAnchor ? View.Anchor : View.Todo;
        }

        // Trigger: single tap on the QUIT tile. Anchors need no extra save here (ZoneAnchorStore writes its
        // file on every change); SLAM is released by SlamSessionController.OnDisable during shutdown.
        private void Quit()
        {
            Debug.Log("[Launcher] Quit");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // Trigger: SimpleTouchForLite.OnDoubleTap, Android back key (or Escape).
        // Inside a feature = back to home, unless the anchor menu was open (then it only cancels the menu).
        // On the home screen it is ignored (left to the system).
        private void HandleDoubleTap()
        {
            if (view == View.Home || featureBlocksBackLastFrame) return;
            // Threshold: ignore a second back within backDebounceSeconds (double tap + back key for one gesture).
            if (Time.unscaledTime - lastBackTime < backDebounceSeconds) return;
            lastBackTime = Time.unscaledTime;
            pendingBack = true;
        }

        // Trigger: SimpleTouchForLite.OnSwipeRight / OnSwipeLeft (each ~200 touch px of travel).
        // Same mapping as the anchor menu (SwipeMap): forward swipe = left tile, back swipe = right tile.
        private void HandleSwipeRight(Vector2 _) => HandleSwipe(SwipeMap.OnSwipeRightStep(invertSwipe));
        private void HandleSwipeLeft(Vector2 _) => HandleSwipe(SwipeMap.OnSwipeLeftStep(invertSwipe));

        private void HandleSwipe(int dir)
        {
            if (view != View.Home) return;
            // Threshold: ignore repeats within swipeCooldownSeconds of the last accepted swipe.
            if (Time.unscaledTime - lastSwipeTime < swipeCooldownSeconds) return;
            lastSwipeTime = Time.unscaledTime;
            // Clamped, not wrapped: with two tiles a wrap makes -1 and +1 identical (any swipe just toggles),
            // and wrapping from TODO onto QUIT by accident is risky. At the left / right end the focus stays put.
            focused = Mathf.Clamp(focused + dir, 0, TileCount - 1);
            RefreshTiles();
        }

        // ------------------------------------------------------------------ screens

        private void Open(View target)
        {
            canvasGo.SetActive(false);
            view = target;

            if (target == View.Anchor)
            {
                // First open: activating the bootstrap runs its Awake, which builds the zone system.
                if (!anchorFeature.gameObject.activeSelf) anchorFeature.gameObject.SetActive(true);
                if (anchorSystem == null)
                {
                    anchorInput = anchorFeature.GetComponentInChildren<AnchorPlacementInput>(true);
                    anchorHud = anchorFeature.GetComponentInChildren<ZoneDebugHUD>(true);
                    anchorMarkers = anchorFeature.GetComponentInChildren<ZoneAnchorMarkers>(true);
                    if (anchorInput != null) anchorSystem = anchorInput.gameObject;
                }
                if (anchorSystem != null && !anchorSystem.activeSelf) anchorSystem.SetActive(true); // keepAnchorRunning OFF
                SetAnchorFront(true);
            }
            else
            {
                if (!todoFeature.gameObject.activeSelf) todoFeature.gameObject.SetActive(true);
                if (todoPanel == null) todoPanel = todoFeature.GetComponentInChildren<VoiceTodoTestPanel>(true);
                if (todoPanel != null) todoPanel.enabled = true;
            }
            Debug.Log($"[Launcher] Opened {target}");
        }

        private void GoHome()
        {
            if (view == View.Anchor)
            {
                SetAnchorFront(false);
                // keepAnchorRunning OFF: deactivate the whole system (SlamSessionController.OnDisable
                // calls DisableSlamHeadTracker).
                if (!keepAnchorRunning && anchorSystem != null) anchorSystem.SetActive(false);
            }
            else if (view == View.Todo && todoPanel != null)
            {
                todoPanel.enabled = false;
            }

            Debug.Log($"[Launcher] Back to home from {view}");
            view = View.Home;
            canvasGo.SetActive(true);
            RefreshTiles();
        }

        /// <summary>Input + HUD + world markers. Each component hides its own visuals in OnDisable.</summary>
        private void SetAnchorFront(bool on)
        {
            if (anchorInput != null) anchorInput.enabled = on;
            if (anchorHud != null) anchorHud.enabled = on;
            if (anchorMarkers != null) anchorMarkers.enabled = on;
        }

        // ------------------------------------------------------------------ view

        /// <summary>Focused tile: cyan 4 px frame + cyan title. Other tile: green 2 px frame.</summary>
        private void RefreshTiles()
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                bool f = i == focused;
                var color = f ? Cyan : Green;
                float w = Mathf.Max(MinLinePx, f ? focusLinePx : idleLinePx);
                for (int l = 0; l < 4; l++)
                {
                    tiles[i].lines[l].color = color;
                    // Lines 0,1 = top / bottom (thickness on y), 2,3 = left / right (thickness on x).
                    tiles[i].lineRts[l].sizeDelta = l < 2 ? new Vector2(0f, w) : new Vector2(w, 0f);
                }
                tiles[i].title.color = color;
            }
        }

        // ------------------------------------------------------------------ canvas

        private void BuildCanvas()
        {
            var go = new GameObject("Launcher Canvas", typeof(RectTransform));
            canvasGo = go;
            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0) go.layer = uiLayer;
            var canvasRt = (RectTransform)go.transform;
            canvasRt.SetParent(head, false);

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = head.GetComponent<Camera>();
            go.AddComponent<XRGraphicRaycaster>();

            // Visible diagonal at HudDistanceM = 2 * d * tan(FOV/2); divide by the canvas diagonal in px.
            float diagPx = Mathf.Sqrt(CanvasWidthPx * CanvasWidthPx + CanvasHeightPx * CanvasHeightPx);
            float metersPerPx = 2f * HudDistanceM * Mathf.Tan(DisplayFovDeg * 0.5f * Mathf.Deg2Rad) / diagPx;
            canvasRt.sizeDelta = new Vector2(CanvasWidthPx, CanvasHeightPx);
            canvasRt.localPosition = new Vector3(0f, 0f, HudDistanceM);
            canvasRt.localRotation = Quaternion.identity;
            canvasRt.localScale = Vector3.one * metersPerPx;

            // Positions / sizes / fonts are set in ApplyLayout, so the Inspector can change them live.
            float innerWidth = CanvasWidthPx - 2 * MarginPx;
            // Title: bottom-anchored, sits right above the tiles.
            titleText = CreateText(canvasRt, "Title", MinFontPx, Purple, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(innerWidth, 40f));
            titleText.text = "TEST FEATURES";

            tiles = new Tile[TileCount];
            tiles[TileAnchor] = CreateTile(canvasRt, "Tile Anchor", "ANCHOR", "zones");
            tiles[TileTodo] = CreateTile(canvasRt, "Tile Todo", "TODO", "voice");
            tiles[TileQuit] = CreateTile(canvasRt, "Tile Quit", "QUIT", "exit app");

            // bottom zone = hints
            hintText = CreateText(canvasRt, "HintText", MinFontPx, Green, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, MarginPx), new Vector2(innerWidth, 48f));
            hintText.text = "Swipe: select   Tap: open\nInside: 2x tap = back";
        }

        // Trigger: any Inspector change. Only re-lays out while playing (the canvas exists only at runtime).
        private void OnValidate()
        {
            if (Application.isPlaying && tiles != null) ApplyLayout();
        }

        /// <summary>
        /// Applies the Layout fields. Tiles are bottom-anchored: their bottom edge is tilesBottomPx above the
        /// canvas bottom; the title sits titleGapPx above the tiles. Fonts / lines are clamped to the spec minimum.
        /// </summary>
        private void ApplyLayout()
        {
            // Row of TileCount tiles centered on tilesOffsetXPx, one (size + gap) step apart.
            float step = tileSizePx + tileGapPx;
            float firstX = tilesOffsetXPx - step * (tiles.Length - 1) * 0.5f;
            for (int i = 0; i < tiles.Length; i++)
            {
                var t = tiles[i];
                t.root.anchoredPosition = new Vector2(firstX + i * step, tilesBottomPx);
                t.root.sizeDelta = new Vector2(tileSizePx, tileSizePx);

                t.title.fontSize = Mathf.Max(MinFontPx, tileFontSize);
                t.sub.fontSize = Mathf.Max(MinFontPx, tileSubFontSize);
                t.sub.gameObject.SetActive(showSubtitle);
                // Title centered when there is no subtitle, otherwise title above / subtitle below the middle.
                float half = (t.title.fontSize + t.sub.fontSize) * 0.3f;
                t.title.rectTransform.anchoredPosition = new Vector2(0f, showSubtitle ? half : 0f);
                t.sub.rectTransform.anchoredPosition = new Vector2(0f, -half);
                t.title.rectTransform.sizeDelta = new Vector2(tileSizePx, t.title.fontSize + 8f);
                t.sub.rectTransform.sizeDelta = new Vector2(tileSizePx, t.sub.fontSize + 8f);
            }

            titleText.gameObject.SetActive(showTitle);
            titleText.fontSize = Mathf.Max(MinFontPx, titleFontSize);
            titleText.rectTransform.anchoredPosition = new Vector2(tilesOffsetXPx, tilesBottomPx + tileSizePx + titleGapPx);

            hintText.gameObject.SetActive(showHint);
            hintText.fontSize = Mathf.Max(MinFontPx, hintFontSize);

            RefreshTiles(); // frame thickness
        }

        private static Tile CreateTile(RectTransform parent, string name, string title, string sub)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.layer = parent.gameObject.layer;
            var rt = (RectTransform)root.transform;
            rt.SetParent(parent, false);
            // Anchored to the canvas bottom-center, pivot at the tile's bottom-center (see ApplyLayout).
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);

            var tile = new Tile { root = rt, lines = new Image[4], lineRts = new RectTransform[4] };
            // Outline only, no fill (black = transparent on the glasses).
            CreateLine(rt, tile, 0, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f));
            CreateLine(rt, tile, 1, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f));
            CreateLine(rt, tile, 2, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f));
            CreateLine(rt, tile, 3, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f));

            tile.title = CreateText(rt, "Title", MinFontPx, Green, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            tile.title.text = title;
            tile.sub = CreateText(rt, "Sub", MinFontPx, Green, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            tile.sub.text = sub;
            // One line each, never wrapped, even when the tile is made very small.
            tile.title.horizontalOverflow = HorizontalWrapMode.Overflow;
            tile.sub.horizontalOverflow = HorizontalWrapMode.Overflow;
            return tile;
        }

        // Stretched along one edge of the parent; thickness is set in RefreshTiles.
        private static void CreateLine(RectTransform parent, Tile tile, int index, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(anchorMin.x == anchorMax.x ? anchorMin.x : 0.5f, anchorMin.y == anchorMax.y ? anchorMin.y : 0.5f);
            rt.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>(); // no sprite = solid quad
            img.raycastTarget = false;
            tile.lines[index] = img;
            tile.lineRts[index] = rt;
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
            text.fontStyle = FontStyle.Bold;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = align;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            text.text = "";
            return text;
        }
    }
}
