using System.Collections.Generic;
using RayneoIdle.Game;
using RayneoIdle.GameInput;
using UnityEngine;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Settings: Recenter map, Map size (S / M / L) and Trash style (12 piles / 48 pieces) display preferences,
    /// Reset game (D10: Reset + two confirmations via ConfirmGate, red warning text), a test row that fills the map
    /// with trash,
    /// and the version row, which is also the hidden Demo Mode toggle (DemoMode.RegisterSecretTap:
    /// N taps within the window). While Demo Mode is on, extra rows: speed tier and "Skip 8 hours".
    /// </summary>
    public class SettingsPanel : PanelBase
    {
        public const string Version = "Prototype 0.1";

        /// <summary>Red only for destructive warnings (RayNeo spec).</summary>
        private static readonly Color WarningRed = new Color32(0xE8, 0x7A, 0x6A, 0xFF);

        private enum Item { Recenter, MapSize, TrashStyle, Reset, DemoSpeed, DemoSkip, TrashFill, Version }

        /// <summary>Test row "Fill map with trash" (until the real trash system exists). Set false for a clean demo build.</summary>
        private const bool ShowTestRows = true;

        /// <summary>Rows that fit in the panel (336 px high, 42 px rows); longer lists scroll with the focus.</summary>
        private const int VisibleRows = 6;

        public override string Name => "Settings";

        private readonly List<Row> rows = new List<Row>();
        private readonly List<Item> items = new List<Item>();
        private readonly ConfirmGate resetGate = new ConfirmGate(3, 5); // trigger: 3 taps, each within 5 s
        private DemoMode demo;
        private TreasureMapView map;
        private int focus;

        protected override void Build()
        {
            demo = Object.FindObjectOfType<DemoMode>();
            map = Object.FindObjectOfType<TreasureMapView>();
            Title("Settings");
            for (int i = 0; i < VisibleRows; i++) rows.Add(CreateRow(i));
            if (demo != null) demo.OnActiveChanged += _ => { if (IsOpen) Refresh(); };
        }

        public override void Open()
        {
            focus = 0;
            resetGate.Cancel();
            base.Open();
        }

        public override void Refresh()
        {
            items.Clear();
            items.Add(Item.Recenter);
            // Display preferences (player choice, kept per device): map size and trash layout.
            if (map != null) { items.Add(Item.MapSize); items.Add(Item.TrashStyle); }
            items.Add(Item.Reset);
            if (demo != null && demo.IsActive) { items.Add(Item.DemoSpeed); items.Add(Item.DemoSkip); }
            if (ShowTestRows && map != null) items.Add(Item.TrashFill);
            items.Add(Item.Version);
            focus = Mathf.Clamp(focus, 0, items.Count - 1);

            // Scroll window: keep the focused item inside the visible rows.
            int first = Mathf.Clamp(focus - (rows.Count - 1), 0, Mathf.Max(0, items.Count - rows.Count));
            for (int r = 0; r < rows.Count; r++)
            {
                int i = first + r;
                if (i >= items.Count) { rows[r].Hide(); continue; }
                bool f = i == focus;
                switch (items[i])
                {
                    case Item.MapSize:
                        rows[r].Set($"Map size  {TreasureMapView.SizePresetNames[map.SizePreset]}" +
                                    (map.SizePreset == TreasureMapView.RecommendedSizePreset ? "  (recommended)" : ""), f); break;
                    case Item.TrashStyle:
                        var style = Trash().CurrentStyle;
                        rows[r].Set($"Trash style  {TrashTestOverlay.StyleName(style)}" +
                                    (style == TrashTestOverlay.RecommendedStyle ? "  (recommended)" : ""), f); break;
                    case Item.TrashFill: rows[r].Set("Fill map with trash  (test)", f); break;
                    case Item.Recenter: rows[r].Set("Recenter map", f); break;
                    case Item.Reset:
                        if (resetGate.Step == 0) rows[r].Set("Reset game", f);
                        else rows[r].Set(resetGate.Step == 1 ? "Reset? Tap again" : "All progress lost. Tap to reset", f, WarningRed);
                        if (resetGate.Step > 0) rows[r].text.color = WarningRed;
                        break;
                    case Item.DemoSpeed:
                        rows[r].Set($"Demo speed  {session.Core.DemoMultiplier:0}x", f); break;
                    case Item.DemoSkip: rows[r].Set("Skip 8 hours", f); break;
                    case Item.Version: rows[r].Set(demo != null && demo.IsActive ? $"{Version}  DEMO" : Version, f, Palette.Secondary); break;
                }
            }
        }

        public override void Tick()
        {
            // The reset prompt closes by itself when the next tap does not come in time.
            int before = resetGate.Step;
            resetGate.Update(Time.unscaledTime);
            if (before != resetGate.Step) Refresh();
        }

        protected override void OnAction(GameAction action)
        {
            switch (action)
            {
                case GameAction.Up:
                case GameAction.Prev:
                    focus = Mathf.Max(0, focus - 1); resetGate.Cancel(); break;
                case GameAction.Down:
                case GameAction.Next:
                    focus = Mathf.Min(items.Count - 1, focus + 1); resetGate.Cancel(); break;
                case GameAction.Confirm:
                    Confirm(items[focus]); break;
            }
            Refresh();
        }

        private void Confirm(Item item)
        {
            switch (item)
            {
                case Item.Recenter:
                    if (map != null) { map.Recenter(); hud.Toast("Map moved to your right"); }
                    break;
                case Item.Reset:
                    if (resetGate.Press(Time.unscaledTime))
                    {
                        session.ResetSlot();
                        hud.Toast("Game reset");
                        Close();
                    }
                    break;
                case Item.DemoSpeed: demo.CycleTier(); break;
                case Item.DemoSkip: hud.Toast($"+{demo.SkipOffline():0} coins"); break;
                case Item.MapSize:
                    // Trigger: tap cycles S -> M -> L -> S. Saved per device (PlayerPrefs).
                    map.SetSizePreset((map.SizePreset + 1) % TreasureMapView.SizePresetDistances.Length);
                    hud.Toast($"Map size {TreasureMapView.SizePresetNames[map.SizePreset]}");
                    break;
                case Item.TrashStyle:
                    // Trigger: tap toggles 12 piles <-> 48 pieces. Saved per device; trash on the map is redrawn.
                    hud.Toast($"Trash style: {TrashTestOverlay.StyleName(Trash().ToggleStyle())}");
                    break;
                case Item.TrashFill:
                    // Test trigger: tap fills the map with the full 48 trash (24 h worth).
                    Trash().Fill();
                    hud.Toast($"Trash {Trash().TrashCount}/{TrashTestOverlay.Capacity}");
                    break;
                case Item.Version:
                    demo?.RegisterSecretTap(); // hidden toggle; HUD toasts "Demo mode on/off"
                    break;
            }
        }

        /// <summary>The trash test overlay lives on the map object; added on first use (no scene change needed).</summary>
        private TrashTestOverlay Trash()
        {
            var t = map.GetComponent<TrashTestOverlay>();
            return t != null ? t : map.gameObject.AddComponent<TrashTestOverlay>();
        }
    }
}
