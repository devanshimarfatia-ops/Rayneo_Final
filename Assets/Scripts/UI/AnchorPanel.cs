using RayneoIdle.GameInput;
using RayneoIdle.Todo;
using RayneoIdle.Zones;
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Anchors tool (toolbar, diamond icon): three anchor slots in three boxes (1 / 2 / 3).
    /// Slot i = ZoneType i (Work / Bedroom / Kitchen internally); the player sees the slot's NAME.
    /// Boxes view: Prev / Next = focus slot, tap = open the slot.
    /// Slot view rows (Up / Down or Prev / Next move; on the name row Prev / Next change the preset name):
    ///   Place anchor here  - anchor = where the user stands (no hit test on X3 Pro), replaces the old one
    ///   Name: &lt; preset &gt;  - preset names, works offline
    ///   Say a name         - voice rename through the system To-do flow (needs network)
    ///   Remove anchor
    /// Renaming also rewrites the "(Name)-" prefix of the slot's open todos (ZoneTodoService.RenameSlot).
    /// </summary>
    public class AnchorPanel : PanelBase
    {
        public static string[] PresetNames => ZoneTag.PresetNames;

        private enum SlotAction { Place, Name, Voice, Remove }

        public override string Name => "Anchors";
        public override string Hint => slotView < 0 ? "Swipe: slot   Tap: open   Double tap: back"
                                                    : "Swipe: move / name   Tap: select   Double tap: slots";

        private Box[] boxes;
        private RectTransform boxesRt, slotRt;
        private Text title;
        private readonly SlotAction[] rowsOrder = { SlotAction.Place, SlotAction.Name, SlotAction.Voice, SlotAction.Remove };
        private readonly System.Collections.Generic.List<PanelBase.Row> rows = new System.Collections.Generic.List<PanelBase.Row>();
        private int focusSlot;
        private int slotView = -1; // -1 = boxes view
        private int focusRow;
        private ZoneAnchorStore store;
        private ModeManager modes;
        private ZoneTodoService todos;

        protected override void Build()
        {
            title = Title("Anchors");
            boxesRt = UiFactory.CreateRect(root, "Boxes", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);
            boxes = CreateBoxes(boxesRt);
            slotRt = UiFactory.CreateRect(root, "Slot", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);
            // Rows are children of root (PanelBase helper); shown only in the slot view.
            for (int i = 0; i < rowsOrder.Length; i++) rows.Add(CreateRow(i));
            todos = Object.FindObjectOfType<ZoneTodoService>();
        }

        private void FindZones()
        {
            if (store == null) store = Object.FindObjectOfType<ZoneAnchorStore>();
            if (modes == null) modes = Object.FindObjectOfType<ModeManager>();
        }

        public override void Open()
        {
            slotView = -1;
            FindZones();
            base.Open();
        }

        private string SlotName(int slot) => todos != null ? todos.ZoneDisplayName(ZoneTag.FromSlot(slot)) : ZoneTag.DefaultNames[slot];
        private bool Placed(int slot) => store != null && store.Get((ZoneType)slot) != null;
        private bool Here(int slot) => modes != null && ZoneTodoService.MapMode(modes.CurrentMode) == ZoneTag.FromSlot(slot);

        public override void Refresh()
        {
            bool boxesView = slotView < 0;
            boxesRt.gameObject.SetActive(boxesView);
            slotRt.gameObject.SetActive(!boxesView);

            if (boxesView)
            {
                title.text = store == null ? "Anchors  (not available)" : "Anchors";
                for (int i = 0; i < 3; i++)
                {
                    var b = boxes[i];
                    b.SetFocused(i == focusSlot, BoxTex, BoxFocusTex);
                    b.big.text = (i + 1).ToString();
                    b.big.color = i == focusSlot ? (Color)Palette.Accent : Palette.Main;
                    b.name.text = SlotName(i);
                    b.status.text = !Placed(i) ? "Empty" : Here(i) ? "Placed\nYou are here" : "Placed";
                    b.status.color = Here(i) ? (Color)Palette.Accent : Palette.Secondary;
                }
                foreach (var r in rows) r.Hide();
                return;
            }

            title.text = $"Anchor {slotView + 1}   {SlotName(slotView)}";
            int count = Placed(slotView) ? rowsOrder.Length : rowsOrder.Length - 1; // no "Remove" when empty
            focusRow = Mathf.Clamp(focusRow, 0, count - 1);
            for (int i = 0; i < rows.Count; i++)
            {
                if (i >= count) { rows[i].Hide(); continue; }
                bool f = i == focusRow;
                switch (rowsOrder[i])
                {
                    case SlotAction.Place: rows[i].Set(Placed(slotView) ? "Move anchor here" : "Place anchor here", f); break;
                    case SlotAction.Name: rows[i].Set($"Name   <  {SlotName(slotView)}  >", f); break;
                    case SlotAction.Voice: rows[i].Set(todos != null && todos.WaitingForVoice ? "Listening..." : "Say a name (voice)", f); break;
                    case SlotAction.Remove: rows[i].Set("Remove anchor", f, Palette.Secondary); break;
                }
            }
        }

        public override void Tick()
        {
            // "You are here" follows the zone detector.
            if (slotView < 0 && Time.frameCount % 15 == 0) Refresh();
        }

        protected override bool OnBack()
        {
            if (slotView < 0) return false;
            slotView = -1;
            Refresh();
            return true;
        }

        protected override void OnAction(GameAction action)
        {
            if (slotView < 0)
            {
                if (action == GameAction.Prev || action == GameAction.Up) focusSlot = Mathf.Max(0, focusSlot - 1);
                else if (action == GameAction.Next || action == GameAction.Down) focusSlot = Mathf.Min(2, focusSlot + 1);
                else if (action == GameAction.Confirm) { slotView = focusSlot; focusRow = 0; }
                Refresh();
                return;
            }

            int count = Placed(slotView) ? rowsOrder.Length : rowsOrder.Length - 1;
            var row = rowsOrder[focusRow];
            switch (action)
            {
                case GameAction.Up: focusRow = Mathf.Max(0, focusRow - 1); break;
                case GameAction.Down: focusRow = Mathf.Min(count - 1, focusRow + 1); break;
                case GameAction.Prev:
                case GameAction.Next:
                    if (row == SlotAction.Name) CyclePreset(action == GameAction.Next ? +1 : -1);
                    else focusRow = Mathf.Clamp(focusRow + (action == GameAction.Next ? 1 : -1), 0, count - 1);
                    break;
                case GameAction.Confirm: Confirm(row); break;
            }
            Refresh();
        }

        private void CyclePreset(int step)
        {
            string current = SlotName(slotView);
            int i = System.Array.FindIndex(PresetNames, n => string.Equals(n, current, System.StringComparison.OrdinalIgnoreCase));
            i = i < 0 ? 0 : (i + step + PresetNames.Length) % PresetNames.Length;
            todos?.RenameSlot(slotView, PresetNames[i]);
        }

        private void Confirm(SlotAction row)
        {
            int slot = slotView;
            switch (row)
            {
                case SlotAction.Place:
                    if (PlaceHere(slot)) focusRow = 1; // next: the Name row (onboarding teaches naming right after)
                    break;
                case SlotAction.Name: CyclePreset(+1); break;
                case SlotAction.Voice:
                    if (todos == null || !todos.StartVoiceCapture(text =>
                        {
                            string stored = string.IsNullOrWhiteSpace(text) ? null : todos.RenameSlot(slot, text);
                            hud.Toast(stored != null ? $"Named: {stored}" : "No name heard");
                            if (IsOpen) Refresh();
                        }))
                        hud.Toast("Voice not available");
                    break;
                case SlotAction.Remove:
                    if (store != null && store.RemoveSlot((ZoneType)slot)) hud.Toast($"Anchor {slot + 1} removed");
                    break;
            }
        }

        /// <summary>Places slot's anchor where the user stands. Also used by onboarding.</summary>
        public static bool PlaceAnchor(int slot, MainHud hud = null)
        {
            var store = Object.FindObjectOfType<ZoneAnchorStore>();
            var head = UiFactory.FindHead();
            if (store == null || head == null) { hud?.Toast("Anchors not available"); return false; }
            store.SetSlot((ZoneType)slot, head.position);
            hud?.Toast($"Anchor {slot + 1} set");
            return true;
        }

        private bool PlaceHere(int slot) => PlaceAnchor(slot, hud);

        /// <summary>Opened by onboarding: go straight to a slot view.</summary>
        public void ShowSlot(int slot)
        {
            slotView = Mathf.Clamp(slot, 0, 2);
            focusSlot = slotView;
            focusRow = 0;
            Refresh();
        }

        /// <summary>True while a slot (not the three boxes) is shown.</summary>
        public bool InSlotView => slotView >= 0;
    }
}
