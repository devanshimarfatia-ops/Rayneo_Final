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

        public enum ToolId { Todo, Map, Collection, Upgrade, Pomodoro, Settings }

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
        private RectTransform canvasRt, mainRt;
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

            Build(head);
            Subscribe();

            var input = InputAdapter.Instance;
            if (input != null) input.Push(this); // base handler: map / panels push above it

            if (session.LastLaunchSettle.hadSave) ShowSettleToast(session.LastLaunchSettle);
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

            // top-left: coins
            UiFactory.CreatePixelImage(mainRt, "Coin icon", PixelCanvas.FromSprite(PixelSprites.Coin), 3f,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(m, -m - 2));
            coinsText = UiFactory.CreateText(mainRt, "Coins", 24, Palette.Accent, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(m + 32, -m), new Vector2(200, 30));
            rateText = UiFactory.CreateText(mainRt, "Rate", UiFactory.AuxFont, Palette.Ink, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(m + 32, -m - 30), new Vector2(200, 24));

            // top-center: toast (between the coin block and the badge)
            toastText = UiFactory.CreateText(canvasRt, "Toast", UiFactory.BodyFont, Palette.Accent, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -m), new Vector2(360, 60));

            // top-right: demo badge
            badgeText = UiFactory.CreateText(mainRt, "Demo badge", UiFactory.AuxFont, Palette.Sand, TextAnchor.UpperRight,
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-m, -m), new Vector2(120, 24));

            // right edge: arrow to the map
            arrow = UiFactory.CreatePixelImage(mainRt, "Map arrow", PixelCanvas.FromSprite(PixelSprites.ArrowRight), 4f,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-m, 0));
            arrowLabel = UiFactory.CreateText(mainRt, "Map arrow label", UiFactory.AuxFont, Palette.Accent, TextAnchor.MiddleRight,
                new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-m - 30, 0), new Vector2(120, 24));

            // bottom: toolbar. Unfocused icons: no frame, dimmed. Focused: full brightness + 4 px accent frame.
            var framePc = new PixelCanvas((int)(SlotPx / IconPxPerTexel), (int)(SlotPx / IconPxPerTexel));
            framePc.Clear();
            framePc.RectOutline(0, 0, framePc.Width, framePc.Height, Palette.Accent);
            frameFocusTex = framePc.ToTexture();

            AddTool(ToolId.Todo, "To-do", PixelSprites.IconTodo);
            AddTool(ToolId.Map, "Map", PixelSprites.IconMap);
            AddTool(ToolId.Collection, "Collection", PixelSprites.IconCollection);
            AddTool(ToolId.Upgrade, "Upgrade", PixelSprites.IconUpgrade);
            AddTool(ToolId.Pomodoro, "Focus timer", PixelSprites.IconPomodoro);
            AddTool(ToolId.Settings, "Settings", PixelSprites.IconSettings);

            float total = tools.Count * SlotPx + (tools.Count - 1) * SlotGapPx;
            for (int i = 0; i < tools.Count; i++)
            {
                float x = -total / 2 + i * (SlotPx + SlotGapPx);
                tools[i].frame = UiFactory.CreatePixelImage(mainRt, $"Slot {tools[i].id}", frameFocusTex, IconPxPerTexel,
                    new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(x, m));
                tools[i].image = UiFactory.CreatePixelImage(mainRt, $"Icon {tools[i].id}", PixelCanvas.FromSprite(tools[i].icon), IconPxPerTexel,
                    new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(x + 4, m + 4));
            }
            labelText = UiFactory.CreateText(mainRt, "Tool label", UiFactory.AuxFont, Palette.Sand, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, m + SlotPx + 6), new Vector2(240, 24));
            SetFocus(0);
        }

        private void AddTool(ToolId id, string label, string[] icon) => tools.Add(new Tool { id = id, label = label, icon = icon });

        private void Subscribe()
        {
            var core = session.Core;
            core.OnChestFound += _ => Toast("Chest found");
            core.OnCoinRateUpgraded += lvl => Toast($"Coin Rate {lvl}: {core.CoinsPerHour:0}/h");
            core.OnCoinsAdded += (amount, src) => { if (src == CoinSource.Todo) Toast($"+{amount:0} coins"); };
            session.OnLaunchSettled += ShowSettleToast;
            session.Island.OnChestOpened += (_, result) => Toast(TreasureMapView.Summary(result));
            if (demo != null) demo.OnActiveChanged += on => Toast(on ? "Demo mode on" : "Demo mode off");
        }

        private void ShowSettleToast(LaunchSettleInfo info)
        {
            // D4: the settle toast shows 10 decimals so even a 10 s absence is visible.
            Toast($"Welcome back\n+{info.coins:F10}");
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

            coinsText.text = Math.Floor(core.State.coins).ToString("N0"); // HUD shows whole coins, rounded down
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
            bool showArrow = map != null && chestWaiting && !mapVisible && Mathf.Repeat(Time.unscaledTime, arrowBlinkSeconds * 2) < arrowBlinkSeconds;
            arrow.enabled = showArrow;
            arrowLabel.text = showArrow ? "Chest" : "";

            // Toolbar label of the focused item (with price for Upgrade).
            var t = tools[focus];
            labelText.text = t.id == ToolId.Upgrade
                ? (core.CoinRateMaxed ? "Upgrade  MAX" : $"Upgrade  {core.NextCoinRateCost:0}")
                : t.label;
        }

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
            var core = session.Core;
            switch (id)
            {
                case ToolId.Upgrade:
                    if (!core.TryBuyCoinRate())
                        Toast(core.CoinRateMaxed ? "Coin Rate max" : $"Need {core.NextCoinRateCost:0} coins");
                    return;
                case ToolId.Map:
                    Toast("Look right");
                    return;
                case ToolId.Pomodoro:
                    Toast("Coming soon"); // D7: icon only in the MVP
                    return;
            }
            if (OnToolOpened != null) OnToolOpened.Invoke(id);
            else Toast("Coming soon");
        }

        public void FocusTool(ToolId id) => SetFocus(tools.FindIndex(t => t.id == id));
    }
}
