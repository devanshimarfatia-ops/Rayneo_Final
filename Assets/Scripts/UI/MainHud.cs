using System;
using System.Collections.Generic;
using RayneoIdle.Game;
using RayneoIdle.GameInput;
using RayneoIdle.Idle;
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.UI
{
    /// <summary>
    /// W8 HUD, head-locked, 640x480 canvas zones (RayNeo spec):
    ///   top-left  = coins + coins/h        top-center = toasts (auto-hide)       top-right = demo badge
    ///   right edge = "look right" arrow to the map (shown while the map is out of view and a chest waits)
    ///   bottom    = toolbar of square pixel icons; swipe = focus, tap = open, label above the focused icon.
    /// Bottom toolbar is the base input handler; the map / panels push themselves above it.
    ///
    /// Toast triggers: launch settle (D4: 10 decimals), chest found, To-do reward, upgrade, demo on/off.
    /// </summary>
    public class MainHud : MonoBehaviour, IGameInputHandler
    {
        private const string LogTag = "[HUD]";

        [SerializeField] private float hudDistanceM = 2f;
        [Tooltip("Seconds a toast stays (spec: 2-3 s).")]
        [SerializeField] private float toastSeconds = 2.5f;
        [Tooltip("Seconds the arrow blinks on / off.")]
        [SerializeField] private float arrowBlinkSeconds = 0.6f;

        // ---- Toolbar layout (canvas px) ----
        private const float IconPxPerTexel = 4f;   // 12x12 icon -> 48 px
        private const float SlotPx = 56f;          // icon + 4 px padding each side
        private const float SlotGapPx = 12f;

        public enum ToolId { Anchors, Todo, Collection, Profile, Pomodoro, Settings }

        private class Tool
        {
            public ToolId id;
            public string label;
            public string[] icon;
            public RawImage frame;
            public RawImage image;
        }

        /// <summary>Fired when a toolbar item is confirmed and no built-in action handled it (panels hook in here).</summary>
        public event Action<ToolId> OnToolOpened;

        public ToolId FocusedTool => tools[focus].id;

        private GameSession session;
        private DemoMode demo;
        private TreasureMapView map;
        private RectTransform canvasRt, mainRt, toolbarRt;
        private Text coinsText, rateText, toastText, badgeText, labelText, arrowLabel;
        private RawImage arrow;
        private Texture2D frameFocusTex;
        /// <summary>Unfocused icons are multiplied by this (sand -> about ink brightness), focused = white (full).</summary>
        private static readonly Color DimIcon = new Color(0.78f, 0.78f, 0.78f, 1f);
        private readonly List<Tool> tools = new List<Tool>();
        private int focus;
        private float toastHideAt;
        private readonly Queue<string> toastQueue = new Queue<string>();

        private void Start()
        {
            session = GameSession.Instance;
            var head = UiFactory.FindHead();
            if (session == null || head == null)
            {
                Debug.LogError($"{LogTag} needs a GameSession and a head camera");
                enabled = false;
                return;
            }
            UiFactory.ApplyBlackClear(head.GetComponent<Camera>());
            demo = FindObjectOfType<DemoMode>();
            map = FindObjectOfType<TreasureMapView>();
            todoService = FindObjectOfType<Todo.ZoneTodoService>();

            Build(head);
            Subscribe();

            var input = InputAdapter.Instance;
            if (input != null) input.Push(this); // base handler: map / panels push above it

            // The launch settle is shown by the Start screen (offline earnings window), not as a toast.
        }

        private void OnDestroy()
        {
            if (InputAdapter.Instance != null) InputAdapter.Instance.Pop(this);
        }

        // ================================================================ build

        private void Build(Transform head)
        {
            canvasRt = UiFactory.CreateCanvas("HUD Canvas", head, new Vector2(UiFactory.CanvasWidthPx, UiFactory.CanvasHeightPx),
                hudDistanceM, head.GetComponent<Camera>());
            canvasRt.localPosition = new Vector3(0, 0, hudDistanceM);
            canvasRt.localRotation = Quaternion.identity;
            float m = UiFactory.MarginPx;

            // Everything except the toast lives in mainRt, hidden while the map is in view (the HUD is
            // head-locked and would otherwise draw over the map).
            mainRt = UiFactory.CreateRect(canvasRt, "Main", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(UiFactory.CanvasWidthPx, UiFactory.CanvasHeightPx));
            // Toolbar + map arrow: hidden while a panel is open (the panel uses the center and bottom zones).
            toolbarRt = UiFactory.CreateRect(mainRt, "Toolbar", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(UiFactory.CanvasWidthPx, UiFactory.CanvasHeightPx));

            // top-left: coins
            UiFactory.CreatePixelImage(mainRt, "Coin icon", PixelCanvas.FromSprite(PixelSprites.Coin), 3f,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(m, -m - 2));
            coinsText = UiFactory.CreateText(mainRt, "Coins", 24, Palette.Accent, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(m + 32, -m), new Vector2(320, 30));
            rateText = UiFactory.CreateText(mainRt, "Rate", UiFactory.AuxFont, Palette.Secondary, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(m + 32, -m - 30), new Vector2(200, 24));

            // top-center: toast (between the coin block and the badge)
            toastText = UiFactory.CreateText(canvasRt, "Toast", UiFactory.BodyFont, Palette.Accent, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -m), new Vector2(360, 60));

            // top-right: demo badge
            badgeText = UiFactory.CreateText(mainRt, "Demo badge", UiFactory.AuxFont, Palette.Main, TextAnchor.UpperRight,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-m, -m), new Vector2(120, 24));

            // right edge: arrow to the map
            arrow = UiFactory.CreatePixelImage(toolbarRt, "Map arrow", PixelCanvas.FromSprite(PixelSprites.ArrowRight), 4f,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-m, 0));
            arrowLabel = UiFactory.CreateText(toolbarRt, "Map arrow label", UiFactory.AuxFont, Palette.Accent, TextAnchor.MiddleRight,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-m - 30, 0), new Vector2(120, 24));

            // bottom: toolbar. Unfocused icons: no frame, dimmed. Focused: full brightness + 4 px accent frame.
            var framePc = new PixelCanvas((int)(SlotPx / IconPxPerTexel), (int)(SlotPx / IconPxPerTexel));
            framePc.Clear();
            framePc.RectOutline(0, 0, framePc.Width, framePc.Height, Palette.Accent);
            frameFocusTex = framePc.ToTexture();

            AddTool(ToolId.Anchors, "Anchors", PixelSprites.IconAnchor);
            AddTool(ToolId.Todo, "To-do", PixelSprites.IconTodo);
            AddTool(ToolId.Collection, "Collection", PixelSprites.IconChest);
            AddTool(ToolId.Profile, "Profile", PixelSprites.IconProfile);
            AddTool(ToolId.Pomodoro, "Pomodoro", PixelSprites.IconPomodoro);
            AddTool(ToolId.Settings, "Settings", PixelSprites.IconSettings);

            float total = tools.Count * SlotPx + (tools.Count - 1) * SlotGapPx;
            for (int i = 0; i < tools.Count; i++)
            {
                float x = -total / 2 + i * (SlotPx + SlotGapPx);
                tools[i].frame = UiFactory.CreatePixelImage(toolbarRt, $"Slot {tools[i].id}", frameFocusTex, IconPxPerTexel,
                    new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(x, m));
                tools[i].image = UiFactory.CreatePixelImage(toolbarRt, $"Icon {tools[i].id}", PixelCanvas.FromSprite(tools[i].icon), IconPxPerTexel,
                    new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(x + 4, m + 4));
            }
            labelText = UiFactory.CreateText(toolbarRt, "Tool label", UiFactory.AuxFont, Palette.Main, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, m + SlotPx + 6), new Vector2(240, 24));
            // upper left, under the coins: current zone + its first todos (demo step 4: "shown at upper left").
            // In toolbarRt, so it hides while a panel uses the center zone.
            zoneHeader = UiFactory.CreateText(toolbarRt, "Zone", UiFactory.AuxFont, Palette.Accent, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(m, -m - 62), new Vector2(300, 24));
            zoneTodos = UiFactory.CreateText(toolbarRt, "Zone todos", UiFactory.AuxFont, Palette.Main, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(m, -m - 86), new Vector2(300, 80));
            zoneTodos.lineSpacing = 1.1f;

            // bottom center: chest search status + bar (moved here from the map, first device test).
            // On canvasRt so it stays while the map is in view; it then drops to the bottom row under the map.
            progressRt = UiFactory.CreateRect(canvasRt, "Progress", new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                new Vector2(0, ProgressAboveToolbarY), new Vector2(420, 20));
            progressText = UiFactory.CreateText(progressRt, "Progress text", UiFactory.AuxFont, Palette.Main, TextAnchor.MiddleRight,
                new Vector2(0.5f, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(210, 24));
            var barPc = new PixelCanvas((int)(BarWidthPx / 4), (int)(BarHeightPx / 4));
            barPc.Clear();
            barPc.RectOutline(0, 0, barPc.Width, barPc.Height, Palette.Secondary);
            UiFactory.CreatePixelImage(progressRt, "Bar frame", barPc.ToTexture(), 4f,
                new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(8, 0));
            var fillPc = new PixelCanvas(1, 1);
            fillPc.Set(0, 0, Palette.Main);
            barFill = UiFactory.CreatePixelImage(progressRt, "Bar fill", fillPc.ToTexture(), 1f,
                new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(12, 0));
            barFill.rectTransform.sizeDelta = new Vector2(0, BarHeightPx - 8);

            SetFocus(0);
        }

        // ---- progress row layout (canvas px from the bottom) ----
        /// <summary>Above the tool label (label sits at 86..110).</summary>
        public const float ProgressAboveToolbarY = 118f;
        /// <summary>While the map is in view (canvas px from the bottom).</summary>
        public const float ProgressMapViewY = 368f; // map in view: the map fills the lower part, so the row goes ABOVE it (under the toasts)
        private const float BarWidthPx = 160f, BarHeightPx = 16f;

        private Text zoneHeader, zoneTodos, progressText;
        private RectTransform progressRt;
        private RawImage barFill;
        private Todo.ZoneTodoService todoService;

        private void UpdateZoneTodos()
        {
            if (todoService == null) { zoneHeader.text = ""; zoneTodos.text = ""; return; }
            var zone = todoService.CurrentZone;
            var list = todoService.Visible;
            zoneHeader.text = zone == Todo.TodoZone.General ? (list.Count > 0 ? "To-do" : "") : todoService.ZoneDisplayName(zone);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < list.Count && i < 3; i++)
            {
                string s = list[i].displayText;
                sb.AppendLine("- " + (s.Length > 24 ? s.Substring(0, 23) + "…" : s));
            }
            if (list.Count > 3) sb.Append($"+{list.Count - 3} more");
            zoneTodos.text = sb.ToString().TrimEnd();
        }

        private void UpdateProgress(bool mapVisible)
        {
            // Map in view: the map shows its own progress row above it, so the HUD row hides (device test 4).
            bool show = map != null && map.ShowProgress && !panelOpen && !CardOpen && !mapVisible;
            progressRt.gameObject.SetActive(show);
            if (!show) return;
            progressRt.anchoredPosition = new Vector2(0, mapVisible ? ProgressMapViewY : ProgressAboveToolbarY);
            progressText.text = map.StatusText;
            float inner = BarWidthPx - 8;
            barFill.rectTransform.sizeDelta = new Vector2(Mathf.Round(map.Progress01 * inner / 4f) * 4f, BarHeightPx - 8);
        }

        private void AddTool(ToolId id, string label, string[] icon) => tools.Add(new Tool { id = id, label = label, icon = icon });

        private void Subscribe()
        {
            var core = session.Core;
            core.OnChestFound += _ => Toast("Chest found");
            core.OnCoinRateUpgraded += lvl => Toast($"Coin Rate {lvl}: {core.CoinsPerHour:0}/h");
            core.OnCoinsAdded += (amount, src) => { if (src == CoinSource.Todo) Toast($"+{amount:0} coins"); };
            // (launch settle: StartScreen shows it before Start)
            session.OnStateReplaced += () => gainStart = -1f; // Reset / slot switch: drop the coin count-up
            if (demo != null) demo.OnActiveChanged += on => Toast(on ? "Demo mode on" : "Demo mode off");
        }

        // ================================================================ runtime

        public void Toast(string message)
        {
            if (Time.unscaledTime < toastHideAt) toastQueue.Enqueue(message);
            else ShowToastNow(message);
        }

        private void ShowToastNow(string message)
        {
            toastText.text = message;
            toastHideAt = Time.unscaledTime + toastSeconds;
        }

        private void Update()
        {
            if (session == null) return;
            var core = session.Core;

            coinsText.text = CoinText(core.State.coins); // HUD shows whole coins, rounded down
            rateText.text = $"{core.CoinsPerHour:0}/h";
            badgeText.text = demo != null ? demo.BadgeText : "";

            if (Time.unscaledTime >= toastHideAt)
            {
                if (toastQueue.Count > 0) ShowToastNow(toastQueue.Dequeue());
                else toastText.text = "";
            }

            // Trigger: a chest waits (or the tutorial chest) and the map is not in view -> blink the arrow.
            bool chestWaiting = core.State.chestsWaiting > 0 || session.Island.State.spots.Exists(s => s.hasChest);
            bool mapVisible = map != null && map.InView;
            // Trigger: map in view -> hide everything but the toast (head-locked HUD would cover the map).
            if (mainRt.gameObject.activeSelf == mapVisible) mainRt.gameObject.SetActive(!mapVisible);
            // Arrow only, no text (Yaxin): also forced on by onboarding while it asks the user to look right.
            bool showArrow = map != null && (chestWaiting || ForceMapArrow) && !mapVisible
                             && Mathf.Repeat(Time.unscaledTime, arrowBlinkSeconds * 2) < arrowBlinkSeconds;
            arrow.enabled = showArrow;
            arrowLabel.text = "";

            labelText.text = tools[focus].label;
            UpdateZoneTodos();
            UpdateProgress(mapVisible);
        }

        /// <summary>Onboarding sets this while it asks the user to turn right to the map.</summary>
        public bool ForceMapArrow { get; set; }

        /// <summary>Onboarding intro / end card is showing: toolbar, zone list and progress row hide.</summary>
        public bool CardOpen { get; set; }

        private void SetFocus(int index)
        {
            focus = Mathf.Clamp(index, 0, tools.Count - 1); // clamped, not wrapped (same as the launcher)
            for (int i = 0; i < tools.Count; i++)
            {
                tools[i].frame.enabled = i == focus;
                tools[i].image.color = i == focus ? Color.white : DimIcon;
            }
        }

        /// <summary>Base input handler: toolbar focus + open.</summary>
        public bool HandleAction(GameAction action)
        {
            switch (action)
            {
                case GameAction.Prev: SetFocus(focus - 1); return true;
                case GameAction.Next: SetFocus(focus + 1); return true;
                case GameAction.Confirm: Activate(tools[focus].id); return true;
                default: return false;
            }
        }

        /// <summary>Runs a toolbar item. Panels (To-do, Collection, Settings) are separate modules hooked via OnToolOpened.</summary>
        public void Activate(ToolId id)
        {
            switch (id)
            {
                case ToolId.Pomodoro:
                    Toast("Coming soon"); // D7: icon only in the MVP
                    return;
            }
            if (OnToolOpened != null) OnToolOpened.Invoke(id);
            else Toast("Coming soon");
        }

        public void FocusTool(ToolId id) => SetFocus(tools.FindIndex(t => t.id == id));

        /// <summary>Canvas x (from the bottom-center anchor) of a toolbar slot center: onboarding points at it.</summary>
        public float ToolSlotCenterX(ToolId id)
        {
            int i = tools.FindIndex(t => t.id == id);
            float total = tools.Count * SlotPx + (tools.Count - 1) * SlotGapPx;
            return -total / 2 + i * (SlotPx + SlotGapPx) + SlotPx / 2;
        }

        /// <summary>Canvas y just above the tool label: where onboarding draws its pointer.</summary>
        public float ToolPointerY => UiFactory.MarginPx + SlotPx + 32f;

        // ---- coin gain animation (Start screen after the offline earnings window) ----
        // Trigger: ShowCoinGain(before, gain). Phase 1 (CoinGainHoldSeconds): "before + gain". Phase 2
        // (CoinGainCountSeconds): the number counts up to the live balance. Then the normal display.
        public const float CoinGainHoldSeconds = 0.9f, CoinGainCountSeconds = 1.0f;
        private double gainBefore, gainAmount;
        private float gainStart = -1f;

        public void ShowCoinGain(double before, double gain)
        {
            gainBefore = before;
            gainAmount = gain;
            gainStart = Time.unscaledTime;
        }

        private string CoinText(double live)
        {
            if (gainStart < 0) return Math.Floor(live).ToString("N0");
            float t = Time.unscaledTime - gainStart;
            if (t < CoinGainHoldSeconds)
                return $"{Math.Floor(gainBefore):N0} + {(gainAmount < 1 ? gainAmount.ToString("0.00") : Math.Floor(gainAmount).ToString("N0"))}";
            float u = (t - CoinGainHoldSeconds) / CoinGainCountSeconds;
            if (u >= 1f) { gainStart = -1f; return Math.Floor(live).ToString("N0"); }
            float ease = 1f - (1f - u) * (1f - u);
            return Math.Floor(gainBefore + (live - gainBefore) * ease).ToString("N0");
        }

        /// <summary>Canvas that panels build into (head-locked, 640x480).</summary>
        public RectTransform PanelParent => canvasRt;

        /// <summary>Panel open: hide toolbar + map arrow (coins and toasts stay).</summary>
        public void SetPanelMode(bool panelOpen)
        {
            toolbarRt.gameObject.SetActive(!panelOpen && !CardOpen); // tutorial cards also need the space
            this.panelOpen = panelOpen;
        }

        private bool panelOpen;
    }
}
