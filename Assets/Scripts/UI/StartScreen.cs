using System;
using RayneoIdle.Game;
using RayneoIdle.GameInput;
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Launch flow (device test 3, Yaxin): every app launch shows this modal before the game HUD.
    ///   1. Offline earnings window (only when a save existed = a real exit, D4): time away, coins earned
    ///      (5 decimals; D4 said 10, shortened 2026-11-07), chests found. Tap -> 2.
    ///   2. Start screen: only the Start button + the coin counter. Coming from 1, the counter shows
    ///      "before + gain" and then counts up to the live balance (MainHud.ShowCoinGain). Tap -> game.
    /// Fresh install / after Reset: straight to 2 with 0 coins. Pauses (D4) never show this.
    /// Onboarding waits for <see cref="OnStarted"/>. The map is hidden while this is open; double tap does nothing.
    /// </summary>
    public class StartScreen : PanelBase
    {
        public override string Name => "Start";
        public override string Hint => showingSettle ? "Tap to continue" : "Tap: start";

        /// <summary>Start was pressed: the game (and onboarding) may begin.</summary>
        public event Action OnStarted;
        /// <summary>True once Start was pressed in this app session.</summary>
        public bool Started { get; private set; }

        private bool showingSettle;
        private RectTransform settleRt, startRt;
        private Text settleTitle, settleCoins, settleInfo;
        private Box startBox;
        private TreasureMapView map;

        protected override void Build()
        {
            map = UnityEngine.Object.FindObjectOfType<TreasureMapView>();

            // ---- offline earnings ----
            settleRt = UiFactory.CreateRect(root, "Settle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);
            settleTitle = UiFactory.CreateText(settleRt, "Title", 28, Palette.Main, TextAnchor.UpperCenter,
                new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(PanelSize.x - 2 * Pad, 40));
            settleTitle.text = "While you were away";
            UiFactory.CreatePixelImage(settleRt, "Coin", PixelCanvas.FromSprite(PixelSprites.Coin), 5f,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 46));
            settleCoins = UiFactory.CreateText(settleRt, "Coins", 30, Palette.Accent, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -12), new Vector2(PanelSize.x - 2 * Pad, 44));
            settleInfo = UiFactory.CreateText(settleRt, "Info", UiFactory.AuxFont, Palette.Secondary, TextAnchor.UpperCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(PanelSize.x - 2 * Pad, 60));

            // ---- start ----
            startRt = UiFactory.CreateRect(root, "Start", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);
            var boxes = CreateBoxes(startRt);
            // One button = the middle box of the three-box layout, focused; the side boxes are not used.
            boxes[0].rt.gameObject.SetActive(false);
            boxes[2].rt.gameObject.SetActive(false);
            startBox = boxes[1];
            startBox.SetFocused(true, BoxTex, BoxFocusTex);
            startBox.big.text = "";
            startBox.name.text = "Start";
            startBox.name.fontSize = 30;
            startBox.name.color = Palette.Accent;
            startBox.name.rectTransform.anchoredPosition = new Vector2(0, 20);
            startBox.status.text = "";
        }

        /// <summary>Called by PanelHost on launch.</summary>
        public void ShowOnLaunch(LaunchSettleInfo info)
        {
            showingSettle = info.hadSave;
            if (showingSettle)
            {
                // Only this window shows decimals.
                settleCoins.text = $"+{info.coins:F5}"; // 5 decimals (Yaxin 2026-11-07, was 10 in D4): a 10 s absence still shows
                string chests = info.chestsFound > 0 ? $"\n{info.chestsFound} chest{(info.chestsFound > 1 ? "s" : "")} found" : "";
                settleInfo.text = $"Away {FormatAway(info.awaySeconds)}   {session.Core.CoinsPerHour:0} coins/h{chests}";
                pendingGain = info.coins;
            }
            Open();
        }

        private double pendingGain;

        public override void Open()
        {
            base.Open();
            map?.SetHidden(true);
        }

        public override void Close()
        {
            base.Close();
            map?.SetHidden(false);
        }

        public override void Refresh()
        {
            settleRt.gameObject.SetActive(showingSettle);
            startRt.gameObject.SetActive(!showingSettle);
        }

        protected override bool OnBack() => true; // double tap does not skip the launch flow

        protected override void OnAction(GameAction action)
        {
            if (action != GameAction.Confirm) return;
            if (showingSettle)
            {
                // Trigger: tap on the earnings window -> Start screen with the coin count-up.
                showingSettle = false;
                double live = session.Core.State.coins;
                hud.ShowCoinGain(live - pendingGain, pendingGain);
                Refresh();
                return;
            }
            Started = true;
            Close();
            Debug.Log("[StartScreen] started");
            OnStarted?.Invoke();
        }

        public static string FormatAway(double seconds)
        {
            var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m";
            if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds}s";
            return $"{t.Seconds}s";
        }
    }
}
