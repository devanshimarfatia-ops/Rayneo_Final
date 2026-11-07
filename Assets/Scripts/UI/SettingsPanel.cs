using System.Collections.Generic;
using RayneoIdle.Game;
using RayneoIdle.GameInput;
using UnityEngine;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Settings: Recenter map, Reset game (D10: Reset + two confirmations via ConfirmGate, red warning text),
    /// and the version row, which is also the hidden Demo Mode toggle (DemoMode.RegisterSecretTap:
    /// N taps within the window). While Demo Mode is on, extra rows: speed tier and "Skip 8 hours".
    /// </summary>
    public class SettingsPanel : PanelBase
    {
        public const string Version = "Prototype 0.1";

        /// <summary>Red only for destructive warnings (RayNeo spec). Muted to sit with the parchment palette.</summary>
        private static readonly Color WarningRed = new Color32(0xE8, 0x7A, 0x6A, 0xFF);

        private enum Item { Recenter, Reset, DemoSpeed, DemoSkip, Version }

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
            for (int i = 0; i < 5; i++) rows.Add(CreateRow(i));
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
            items.Add(Item.Reset);
            if (demo != null && demo.IsActive) { items.Add(Item.DemoSpeed); items.Add(Item.DemoSkip); }
            items.Add(Item.Version);
            focus = Mathf.Clamp(focus, 0, items.Count - 1);

            for (int i = 0; i < rows.Count; i++)
            {
                if (i >= items.Count) { rows[i].Hide(); continue; }
                bool f = i == focus;
                switch (items[i])
                {
                    case Item.Recenter: rows[i].Set("Recenter map", f); break;
                    case Item.Reset:
                        if (resetGate.Step == 0) rows[i].Set("Reset game", f);
                        else rows[i].Set(resetGate.Step == 1 ? "Reset? Tap again" : "All progress lost. Tap to reset", f, WarningRed);
                        if (resetGate.Step > 0) rows[i].text.color = WarningRed;
                        break;
                    case Item.DemoSpeed:
                        rows[i].Set($"Demo speed  {session.Core.DemoMultiplier:0}x", f); break;
                    case Item.DemoSkip: rows[i].Set("Skip 8 hours", f); break;
                    case Item.Version: rows[i].Set(demo != null && demo.IsActive ? $"{Version}  DEMO" : Version, f, Palette.Ink); break;
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
                case Item.Version:
                    demo?.RegisterSecretTap(); // hidden toggle; HUD toasts "Demo mode on/off"
                    break;
            }
        }
    }
}
