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
    /// Island Gallery start flow (Figma frames 780 / 781), head-locked, built at runtime:
    ///
    ///   TITLE screen : game name + START.                       Tap -> MENU.
    ///   MENU screen  : a slowly turning 3D island (Menu Island 3D), START LEVEL 1 (greyed out until at least Min Anchors To Start
    ///                  anchors exist), and three tiles ANCHORS / TO-DOS / EXIT GAME.
    ///     START LEVEL 1 -> the Island Gallery game (GameWorld: island + To-Do card + artwork).
    ///     ANCHORS       -> the anchor-zone feature (unchanged).
    ///     TO-DOS        -> the voice to-do panel (unchanged; add / finish todos by voice).
    ///     EXIT GAME     -> close the app.
    ///
    ///   Input on TITLE / MENU:
    ///     Swipe fwd/back -> move focus (START LEVEL 1 -> ANCHORS -> TO-DOS -> EXIT)   (Editor: Left / Right)
    ///     Single tap     -> open the focused item                                     (Editor: Space)
    ///     Double tap     -> MENU: back to TITLE.  TITLE: ignored (left to the system).  (Editor: Escape)
    ///   Inside ANCHORS / TO-DOS / the level:
    ///     Double tap / Android back key -> back to MENU. Blocked while the anchor "Set anchor" menu is
    ///     open (there double tap = cancel that menu). All other input belongs to the feature.
    ///
    /// Anchors count only once the ANCHORS screen has been opened in this session (the zone system,
    /// with its saved anchors, is built on first open; SLAM positions are per session anyway).
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

        [Header("Island Gallery (START LEVEL 1)")]
        [Tooltip("The GameWorld object (island + Sphere + Gallery UI). Switched off at start and when leaving the level.")]
        [SerializeField] private GameObject islandGame;
        [Tooltip("Optional: the UIManager's GalleryGrid. Leaving the level closes the gallery, so the HUD shows next time.")]
        [SerializeField] private GalleryGrid gallery;
        [Tooltip("Optional: the UIManager's TodoList. Leave empty to find it automatically. It re-reads the voice " +
                 "todos the moment the level starts.")]
        [SerializeField] private TodoList todoList;

        [Header("Screens")]
        [SerializeField] private string gameName = "ISLAND GALLERY";
        [SerializeField] private string startLabel = "START";
        [SerializeField] private string levelLabel = "START LEVEL 1";
        [Tooltip("3D island shown on the MENU screen above START LEVEL 1. Drag the island model asset (the FBX / " +
                 "prefab from the Project window). A copy is made for the menu, fitted into the space, and turned slowly.")]
        [SerializeField] private GameObject menuIsland3D;
        [Tooltip("Degrees per second the menu island turns around its vertical axis. 0 = still.")]
        [SerializeField] private float menuIslandSpinDegPerSec = 20f;
        [Tooltip("Tilts the menu island towards you so its top is visible (degrees).")]
        [SerializeField] private float menuIslandTiltDeg = 12f;
        [Tooltip("Fallback only, used when Menu Island 3D is empty: a flat picture instead.")]
        [SerializeField] private Texture menuIslandImage;
        [Tooltip("START LEVEL 1 stays greyed out until at least this many anchors exist. 0 = always available.")]
        [SerializeField] private int minAnchorsToStart = 1;
        [SerializeField] private string lockedHint = "Set an anchor first (ANCHORS)";

        [Header("Behaviour")]
        [Tooltip("ON: after leaving the anchor screen, SLAM + zone detection keep running (ModeManager still " +
                 "fires, anchors stay valid). OFF: the whole zone system is deactivated (saves power).")]
        [SerializeField] private bool keepAnchorRunning = true;
        [Tooltip("Flip if swiping forward on the temple moves the focus the wrong way.")]
        [SerializeField] private bool invertSwipe = false;
        [Tooltip("Swipes closer together than this are ignored, so one swipe = one step.")]
        [SerializeField] private float swipeCooldownSeconds = 0.35f;
        [Tooltip("BACK DEBOUNCE (s): double tap and the Android back key may both arrive for one gesture.")]
        [SerializeField] private float backDebounceSeconds = 0.6f;
        [Tooltip("Force the camera to clear to solid black (#000000 = transparent on the glasses).")]
        [SerializeField] private bool forceBlackBackground = true;
        [Tooltip("Keep the display from timing out while the app runs (the wear sensor still turns it off).")]
        [SerializeField] private bool keepScreenOn = true;

        [Header("Look")]
        [SerializeField] private int gameNameFontSize = 40;
        [SerializeField] private int buttonFontSize = 22;
        [SerializeField] private int tileFontSize = 18;
        [SerializeField] private int hintFontSize = 16;
        [SerializeField] private float focusLinePx = 4f;
        [SerializeField] private float idleLinePx = 2f;

        // ---- Fixed canvas math (RayNeo design specs, same as ZoneDebugHUD) ----
        private const float CanvasWidthPx = 640f;
        private const float CanvasHeightPx = 480f;
        private const float DisplayFovDeg = 30f;
        private const float HudDistanceM = 2f;
        private const int MinFontPx = 16;
        private const float MinLinePx = 2f;

        private static readonly Color Cyan = new Color32(0x4D, 0xE2, 0xFF, 0xFF);
        private static readonly Color Green = new Color32(0x44, 0xFF, 0x66, 0xFF);
        private static readonly Color Purple = new Color32(0x85, 0x66, 0xFF, 0xFF);
        private static readonly Color Grey = new Color32(0x80, 0x80, 0x99, 0xFF);   // greyed-out (still visible)
        private static readonly Color Orange = new Color32(0xFF, 0xB0, 0x40, 0xFF);

        private enum View { Title, Menu, Anchor, Todo, Island }
        // MENU focus order (swipe goes through them in this order).
        private const int ItemLevel = 0, ItemAnchor = 1, ItemTodo = 2, ItemExit = 3;
        private const int ItemCount = 4;

        /// <summary>One outlined button: frame of four lines + label.</summary>
        private class Tile
        {
            public RectTransform root;
            public Image[] lines;
            public RectTransform[] lineRts;
            public Text title;
        }

        private View view = View.Title;
        private int focused = ItemAnchor;
        private bool levelWasUnlocked;
        private GameObject canvasGo, titlePanel, menuPanel;
        private Tile startTile;
        private Tile[] items;
        private Text lockedText;
        private GameObject menuIslandRoot;   // head-locked pivot of the 3D menu island
        private Transform menuIslandSpin;
        private float lockedFlashUntil;

        // Requests from input handlers, applied in LateUpdate, so the tap that opened a screen is never
        // also seen by that screen's own handlers.
        private View? pendingOpen;
        private bool pendingBack;
        private bool pendingQuit;
        private bool featureBlocksBackLastFrame;
        private float lastSwipeTime = -999f;
        private float lastBackTime = -999f;
        private bool touchpadBound;

        // Front components of each feature (found after the feature is first built).
        private GameObject anchorSystem; // "ZoneSystem" created by AnchorZoneBootstrap
        private AnchorPlacementInput anchorInput;
        private ZoneDebugHUD anchorHud;
        private ZoneAnchorMarkers anchorMarkers;
        private ZoneAnchorStore anchorStore;
        private VoiceTodoTestPanel todoPanel;

        private bool LevelUnlocked =>
            minAnchorsToStart <= 0 || (anchorStore != null && anchorStore.Anchors.Count >= minAnchorsToStart);

        private void Awake()
        {
            if (keepScreenOn) Screen.sleepTimeout = SleepTimeout.NeverSleep;
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
            if (islandGame != null) islandGame.SetActive(false); // hidden until START LEVEL 1
            WarnIfActive(anchorFeature);
            WarnIfActive(todoFeature);

            BuildCanvas();
            ShowScreen(View.Title);
            Debug.Log("[Launcher] Home view ready (title screen)");
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
            if (Input.GetKeyDown(KeyCode.Escape)) HandleDoubleTap();
#endif
            // Turn the 3D menu island.
            if (menuIslandSpin != null && view == View.Menu && menuIslandSpinDegPerSec != 0f)
                menuIslandSpin.Rotate(0f, menuIslandSpinDegPerSec * Time.unscaledDeltaTime, 0f, Space.Self);

            // The "set an anchor first" line flashes orange for a moment after a tap on the locked button.
            if (view == View.Menu && lockedText != null)
                lockedText.color = Time.unscaledTime < lockedFlashUntil ? Orange : Grey;
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
                GoBack();
            }
            else if (pendingQuit)
            {
                pendingQuit = false;
                Quit();
            }

            featureBlocksBackLastFrame = view == View.Anchor && anchorInput != null && anchorInput.IsMenuOpen;
        }

        // Trigger: SimpleTouchForLite.OnSimpleTap (or Space). Only acts on the TITLE / MENU screens.
        private void HandleTap()
        {
            if (pendingOpen.HasValue || pendingQuit) return;
            if (view == View.Title) { pendingOpen = View.Menu; return; }
            if (view != View.Menu) return;

            switch (focused)
            {
                case ItemLevel:
                    if (LevelUnlocked) pendingOpen = View.Island;
                    else lockedFlashUntil = Time.unscaledTime + 1.5f; // tells why it is greyed out
                    break;
                case ItemAnchor: pendingOpen = View.Anchor; break;
                case ItemTodo: pendingOpen = View.Todo; break;
                case ItemExit: pendingQuit = true; break;
            }
        }

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
        private void HandleDoubleTap()
        {
            if (view == View.Title || featureBlocksBackLastFrame) return;
            if (Time.unscaledTime - lastBackTime < backDebounceSeconds) return;
            lastBackTime = Time.unscaledTime;
            pendingBack = true;
        }

        private void HandleSwipeRight(Vector2 _) => HandleSwipe(SwipeMap.OnSwipeRightStep(invertSwipe));
        private void HandleSwipeLeft(Vector2 _) => HandleSwipe(SwipeMap.OnSwipeLeftStep(invertSwipe));

        private void HandleSwipe(int dir)
        {
            if (view != View.Menu) return;
            if (Time.unscaledTime - lastSwipeTime < swipeCooldownSeconds) return;
            lastSwipeTime = Time.unscaledTime;
            focused = Mathf.Clamp(focused + dir, 0, ItemCount - 1);
            RefreshMenu();
        }

        // ------------------------------------------------------------------ screens

        private void Open(View target)
        {
            if (target == View.Menu) { ShowScreen(View.Menu); return; }

            canvasGo.SetActive(false);
            if (menuIslandRoot != null) menuIslandRoot.SetActive(false);
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
                    anchorStore = anchorFeature.GetComponentInChildren<ZoneAnchorStore>(true);
                    if (anchorInput != null) anchorSystem = anchorInput.gameObject;
                }
                if (anchorSystem != null && !anchorSystem.activeSelf) anchorSystem.SetActive(true);
                SetAnchorFront(true);
            }
            else if (target == View.Todo)
            {
                if (!todoFeature.gameObject.activeSelf) todoFeature.gameObject.SetActive(true);
                if (todoPanel == null) todoPanel = todoFeature.GetComponentInChildren<VoiceTodoTestPanel>(true);
                if (todoPanel != null)
                {
                    todoPanel.ContinueEnabled = false; // the level is started from the MENU now
                    todoPanel.enabled = true;
                }
            }
            else if (target == View.Island)
            {
                if (islandGame != null) islandGame.SetActive(true);
                if (todoList == null) todoList = FindObjectOfType<TodoList>();
                if (todoList != null) todoList.ReloadFromSystem();
                else Debug.LogWarning("[Launcher] No TodoList found in the scene");
            }
            Debug.Log($"[Launcher] Opened {target}");
        }

        // Back one level: feature / level -> MENU, MENU -> TITLE.
        private void GoBack()
        {
            switch (view)
            {
                case View.Menu:
                    ShowScreen(View.Title);
                    return;
                case View.Anchor:
                    SetAnchorFront(false);
                    if (!keepAnchorRunning && anchorSystem != null) anchorSystem.SetActive(false);
                    break;
                case View.Todo:
                    if (todoPanel != null) todoPanel.enabled = false;
                    break;
                case View.Island:
                    if (gallery != null) gallery.CloseGallery(); // HUD visible again next time
                    if (islandGame != null) islandGame.SetActive(false);
                    break;
            }
            Debug.Log($"[Launcher] Back to menu from {view}");
            ShowScreen(View.Menu);
        }

        private void ShowScreen(View screen)
        {
            view = screen;
            canvasGo.SetActive(true);
            titlePanel.SetActive(screen == View.Title);
            menuPanel.SetActive(screen == View.Menu);
            if (menuIslandRoot != null) menuIslandRoot.SetActive(screen == View.Menu);
            if (screen == View.Menu)
            {
                // Level just became available (e.g. back from placing the first anchor): focus it.
                bool unlocked = LevelUnlocked;
                if (unlocked && !levelWasUnlocked) focused = ItemLevel;
                levelWasUnlocked = unlocked;
                RefreshMenu();
            }
        }

        private void SetAnchorFront(bool on)
        {
            if (anchorInput != null) anchorInput.enabled = on;
            if (anchorHud != null) anchorHud.enabled = on;
            if (anchorMarkers != null) anchorMarkers.enabled = on;
        }

        // ------------------------------------------------------------------ view

        /// <summary>Focused item: cyan thick frame. Others: green thin frame. Locked level: grey label.</summary>
        private void RefreshMenu()
        {
            bool unlocked = LevelUnlocked;
            for (int i = 0; i < items.Length; i++)
            {
                bool f = i == focused;
                Color frame = f ? Cyan : (i == ItemLevel && !unlocked ? Grey : Green);
                Color label = i == ItemLevel && !unlocked ? Grey : frame;
                SetFrame(items[i], frame, f);
                items[i].title.color = label;
            }
            items[ItemLevel].title.text = unlocked ? levelLabel : levelLabel + "  (locked)";
            lockedText.gameObject.SetActive(!unlocked);
        }

        private void SetFrame(Tile t, Color color, bool thick)
        {
            float w = Mathf.Max(MinLinePx, thick ? focusLinePx : idleLinePx);
            for (int l = 0; l < 4; l++)
            {
                t.lines[l].color = color;
                t.lineRts[l].sizeDelta = l < 2 ? new Vector2(0f, w) : new Vector2(w, 0f);
            }
        }

        // ------------------------------------------------------------------ canvas
        // Canvas 640 x 480 px, origin in the middle; everything below is placed from the center (px).

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

            // ---- TITLE (frame 780): game name + START ----
            titlePanel = CreatePanel(canvasRt, "Title Screen");
            var nameText = CreateText(titlePanel.transform as RectTransform, "Game Name", Mathf.Max(MinFontPx, gameNameFontSize),
                Purple, TextAnchor.MiddleCenter, new Vector2(0f, 70f), new Vector2(580f, 70f));
            nameText.text = gameName;
            startTile = CreateTile(titlePanel.transform as RectTransform, "Start", startLabel,
                new Vector2(0f, -30f), new Vector2(200f, 60f), Mathf.Max(MinFontPx, buttonFontSize));
            SetFrame(startTile, Cyan, true);
            startTile.title.color = Cyan;
            var titleHint = CreateText(titlePanel.transform as RectTransform, "Hint", Mathf.Max(MinFontPx, hintFontSize),
                Green, TextAnchor.MiddleCenter, new Vector2(0f, -190f), new Vector2(580f, 30f));
            titleHint.text = "Tap to start";

            // ---- MENU (frame 781): island image, START LEVEL 1, ANCHORS / TO-DOS / EXIT GAME ----
            menuPanel = CreatePanel(canvasRt, "Menu Screen");
            var menuRt = (RectTransform)menuPanel.transform;
            // Island area: 320 x 170 px, centred 110 px above the middle (ends just above START LEVEL 1).
            if (menuIsland3D != null) CreateIsland3D(new Vector2(0f, 110f), new Vector2(320f, 170f), metersPerPx);
            else CreateIslandImage(menuRt, new Vector2(0f, 110f), new Vector2(320f, 170f));

            items = new Tile[ItemCount];
            items[ItemLevel] = CreateTile(menuRt, "Start Level", levelLabel, new Vector2(0f, -10f), new Vector2(300f, 56f),
                Mathf.Max(MinFontPx, buttonFontSize));
            lockedText = CreateText(menuRt, "Locked Hint", Mathf.Max(MinFontPx, hintFontSize), Grey, TextAnchor.MiddleCenter,
                new Vector2(0f, -55f), new Vector2(560f, 26f));
            lockedText.text = lockedHint;

            float tileY = -135f, tileW = 120f, tileH = 80f, gap = 24f;
            int f = Mathf.Max(MinFontPx, tileFontSize);
            items[ItemAnchor] = CreateTile(menuRt, "Anchors", "ANCHORS", new Vector2(-(tileW + gap), tileY), new Vector2(tileW, tileH), f);
            items[ItemTodo] = CreateTile(menuRt, "To-dos", "TO-DOS", new Vector2(0f, tileY), new Vector2(tileW, tileH), f);
            items[ItemExit] = CreateTile(menuRt, "Exit Game", "EXIT GAME", new Vector2(tileW + gap, tileY), new Vector2(tileW, tileH), f);

            var menuHint = CreateText(menuRt, "Hint", Mathf.Max(MinFontPx, hintFontSize), Green, TextAnchor.MiddleCenter,
                new Vector2(0f, -203f), new Vector2(580f, 26f));
            menuHint.text = "Swipe: select   Tap: open   2x tap: back";
        }

        private static GameObject CreatePanel(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return go;
        }

        /// <summary>
        /// Head-locked copy of the island model for the MENU: pivot (tilted) -> spin (turns) -> model.
        /// The model is centred on the spin axis and scaled so it fits inside the island area at the canvas
        /// distance, whatever its original size. Only active while the MENU is shown.
        /// </summary>
        private void CreateIsland3D(Vector2 posPx, Vector2 areaPx, float metersPerPx)
        {
            menuIslandRoot = new GameObject("Menu Island 3D");
            var pivot = menuIslandRoot.transform;
            pivot.SetParent(head, false);
            pivot.localPosition = new Vector3(posPx.x * metersPerPx, posPx.y * metersPerPx, HudDistanceM);
            pivot.localRotation = Quaternion.identity;

            menuIslandSpin = new GameObject("Spin").transform;
            menuIslandSpin.SetParent(pivot, false);

            var model = Instantiate(menuIsland3D, menuIslandSpin, false);
            model.name = menuIsland3D.name + " (menu)";
            model.SetActive(true);
            var mt = model.transform;
            mt.localPosition = Vector3.zero;
            mt.localRotation = Quaternion.identity;

            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogWarning("[Launcher] Menu Island 3D has no renderers");
                return;
            }
            foreach (var r in renderers) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) c.enabled = false;
            foreach (var mb in model.GetComponentsInChildren<MonoBehaviour>(true)) mb.enabled = false; // decoration only

            // Bounds in spin space (pivot and spin are unrotated / unscaled at this point).
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            Vector3 centre = menuIslandSpin.InverseTransformPoint(b.center);
            Vector3 size = b.size;

            // Fit: the turning footprint (horizontal diagonal) must fit the area width, the height its height.
            float footprint = Mathf.Max(0.0001f, Mathf.Sqrt(size.x * size.x + size.z * size.z));
            float fit = Mathf.Min(areaPx.x * metersPerPx / footprint, areaPx.y * metersPerPx / Mathf.Max(0.0001f, size.y));
            mt.localScale *= fit;
            mt.localPosition = -centre * fit;

            pivot.localRotation = Quaternion.Euler(menuIslandTiltDeg, 0f, 0f);
            menuIslandRoot.SetActive(false);
            Debug.Log($"[Launcher] Menu island ready ({renderers.Length} renderers, scale x{fit:F3})");
        }

        // Island picture, fitted inside maxSize keeping its aspect ratio. No texture -> a text placeholder.
        private void CreateIslandImage(RectTransform parent, Vector2 pos, Vector2 maxSize)
        {
            if (menuIslandImage == null)
            {
                var t = CreateText(parent, "Island (no image)", Mathf.Max(MinFontPx, buttonFontSize), Purple,
                    TextAnchor.MiddleCenter, pos, maxSize);
                t.text = "Island";
                return;
            }
            var go = new GameObject("Island Image", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            float aspect = menuIslandImage.height > 0 ? (float)menuIslandImage.width / menuIslandImage.height : 1.5f;
            Vector2 size = maxSize;
            if (maxSize.x / maxSize.y > aspect) size.x = maxSize.y * aspect; else size.y = maxSize.x / aspect;
            rt.sizeDelta = size;
            var img = go.AddComponent<RawImage>();
            img.texture = menuIslandImage;
            img.raycastTarget = false;
        }

        private static Tile CreateTile(RectTransform parent, string name, string label, Vector2 pos, Vector2 size, int fontSize)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.layer = parent.gameObject.layer;
            var rt = (RectTransform)root.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tile = new Tile { root = rt, lines = new Image[4], lineRts = new RectTransform[4] };
            // Outline only, no fill (black = transparent on the glasses).
            CreateLine(rt, tile, 0, "Top", new Vector2(0f, 1f), new Vector2(1f, 1f));
            CreateLine(rt, tile, 1, "Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f));
            CreateLine(rt, tile, 2, "Left", new Vector2(0f, 0f), new Vector2(0f, 1f));
            CreateLine(rt, tile, 3, "Right", new Vector2(1f, 0f), new Vector2(1f, 1f));

            tile.title = CreateText(rt, "Label", fontSize, Green, TextAnchor.MiddleCenter, Vector2.zero, size);
            tile.title.horizontalOverflow = HorizontalWrapMode.Overflow;
            tile.title.text = label;
            return tile;
        }

        // Stretched along one edge of the parent; thickness is set in SetFrame.
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

        // Centered text box at pos (px from the parent's center).
        private static Text CreateText(RectTransform parent, string name, int fontSize, Color color, TextAnchor align,
            Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
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