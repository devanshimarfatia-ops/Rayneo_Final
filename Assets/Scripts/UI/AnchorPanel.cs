using System.Collections.Generic;
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
    /// Order (Yaxin 2026-11-22): name first, then place.
    ///   Boxes view: Prev / Next = focus slot, tap = open the slot.
    ///   Slot view rows:
    ///     Name: &lt;name&gt;       - tap opens the name list
    ///     Place anchor here    - anchor = where the user stands (no hit test on X3 Pro), replaces the old one
    ///     Remove anchor        - only when placed
    ///   Name list (popup): preset names (offline) + "Say a name (voice)"; swipe moves, tap confirms, double tap
    ///     cancels. Confirming returns to the slot view with the focus on "Place anchor here".
    ///   Placed feedback: a check mark + "Anchor placed: &lt;name&gt;" in green under the rows, shown while the slot
    ///     is placed; it says "You are here" while the zone detector reports this slot.
    /// Renaming also rewrites the "(Name)-" prefix of the slot's open todos (ZoneTodoService.RenameSlot).
    /// </summary>
    public class AnchorPanel : PanelBase
    {
        public static string[] PresetNames => ZoneTag.PresetNames;

        private enum SlotAction { Name, Place, Remove }

        /// <summary>Rows visible at once (panel 336 px, 42 px rows); the name list scrolls.</summary>
        private const int VisibleRows = 6;

        private static readonly string[] CheckSprite =
        {
            "........AA",
            ".......AA.",
            "AA....AA..",
            ".AA..AA...",
            "..AAAA....",
            "...AA.....",
        };

        public override string Name => "Anchors";
        public override string Hint => nameView ? "Swipe: choose   Tap: confirm   Double tap: cancel"
                                     : slotView < 0 ? "Swipe: slot   Tap: open   Double tap: back"
                                     : "Swipe: move   Tap: select   Double tap: slots";

        private Box[] boxes;
        private RectTransform boxesRt, slotRt;
        private Text title, placedText;
        private RawImage placedCheck;
        private readonly SlotAction[] rowsOrder = { SlotAction.Name, SlotAction.Place, SlotAction.Remove };
        private readonly List<Row> rows = new List<Row>();
        private int focusSlot;
        private int slotView = -1; // -1 = boxes view
        private int focusRow;
        private bool nameView;
        private int focusName;
        private ZoneAnchorStore store;
        private ModeManager modes;
        private ZoneTodoService todos;

        /// <summary>True once a name was confirmed in the name list since the slot was opened (onboarding hint).</summary>
        public bool NameConfirmed { get; private set; }

        protected override void Build()
        {
            title = Title("Anchors");
            boxesRt = UiFactory.CreateRect(root, "Boxes", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);
            boxes = CreateBoxes(boxesRt);
            slotRt = UiFactory.CreateRect(root, "Slot", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);
            // Rows are children of root (PanelBase helper); shown in the slot view and the name list.
            for (int i = 0; i < VisibleRows; i++) rows.Add(CreateRow(i));
            // Placed feedback, under the three slot rows (rows end ~194 px below the top).
            placedCheck = UiFactory.CreatePixelImage(slotRt, "Placed check", PixelCanvas.FromSprite(CheckSprite), 4f,
                new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(Pad, -236));
            placedText = UiFactory.CreateText(slotRt, "Placed text", 26, Palette.Accent, TextAnchor.MiddleLeft,
                new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(Pad + 56, -236), new Vector2(PanelSize.x - 2 * Pad - 56, 64));
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
            nameView = false;
            FindZones();
            base.Open();
        }

        private string SlotName(int slot) => todos != null ? todos.ZoneDisplayName(ZoneTag.FromSlot(slot)) : ZoneTag.DefaultNames[slot];
        private bool Placed(int slot) => store != null && store.Get((ZoneType)slot) != null;
        private bool Here(int slot) => modes != null && ZoneTodoService.MapMode(modes.CurrentMode) == ZoneTag.FromSlot(slot);
        private int NameItemCount => PresetNames.Length + 1; // presets + voice
        private int SlotRowCount => Placed(slotView) ? rowsOrder.Length : rowsOrder.Length - 1; // no "Remove" when empty

        public override void Refresh()
        {
            bool boxesView = slotView < 0;
            boxesRt.gameObject.SetActive(boxesView);
            slotRt.gameObject.SetActive(!boxesView && !nameView);

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

            if (nameView)
            {
                title.text = $"Name anchor {slotView + 1}";
                focusName = Mathf.Clamp(focusName, 0, NameItemCount - 1);
                int first = Mathf.Clamp(focusName - (rows.Count - 1), 0, Mathf.Max(0, NameItemCount - rows.Count));
                for (int r = 0; r < rows.Count; r++)
                {
                    int i = first + r;
                    if (i >= NameItemCount) { rows[r].Hide(); continue; }
                    bool f = i == focusName;
                    if (i < PresetNames.Length)
                    {
                        bool current = string.Equals(PresetNames[i], SlotName(slotView), System.StringComparison.OrdinalIgnoreCase);
                        rows[r].Set(current ? $"{PresetNames[i]}   (current)" : PresetNames[i], f);
                    }
                    else rows[r].Set(todos != null && todos.WaitingForVoice ? "Listening..." : "Say a name (voice)", f);
                }
                return;
            }

            title.text = $"Anchor {slotView + 1}   {SlotName(slotView)}";
            int count = SlotRowCount;
            focusRow = Mathf.Clamp(focusRow, 0, count - 1);
            for (int i = 0; i < rows.Count; i++)
            {
                if (i >= count) { rows[i].Hide(); continue; }
                bool f = i == focusRow;
                switch (rowsOrder[i])
                {
                    case SlotAction.Name: rows[i].Set($"Name:  {SlotName(slotView)}", f); break;
                    case SlotAction.Place: rows[i].Set(Placed(slotView) ? "Move anchor here" : "Place anchor here", f); break;
                    case SlotAction.Remove: rows[i].Set("Remove anchor", f, Palette.Secondary); break;
                }
            }
            // Trigger: the slot has an anchor -> green check + text (stays while the slot view is open).
            bool placed = Placed(slotView);
            placedCheck.enabled = placed;
            placedText.text = placed ? $"Anchor placed: {SlotName(slotView)}" + (Here(slotView) ? "\nYou are here" : "") : "";
        }

        public override void Tick()
        {
            // "You are here" follows the zone detector.
            if (!nameView && Time.frameCount % 15 == 0) Refresh();
        }

        protected override bool OnBack()
        {
            if (nameView) { nameView = false; Refresh(); return true; }
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
                else if (action == GameAction.Confirm) OpenSlotView(focusSlot);
                Refresh();
                return;
            }

            if (nameView)
            {
                switch (action)
                {
                    case GameAction.Up: case GameAction.Prev: focusName = Mathf.Max(0, focusName - 1); break;
                    case GameAction.Down: case GameAction.Next: focusName = Mathf.Min(NameItemCount - 1, focusName + 1); break;
                    case GameAction.Confirm: ConfirmName(); break;
                }
                Refresh();
                return;
            }

            int count = SlotRowCount;
            switch (action)
            {
                case GameAction.Up: case GameAction.Prev: focusRow = Mathf.Max(0, focusRow - 1); break;
                case GameAction.Down: case GameAction.Next: focusRow = Mathf.Min(count - 1, focusRow + 1); break;
                case GameAction.Confirm: Confirm(rowsOrder[focusRow]); break;
            }
            Refresh();
        }

        private void OpenSlotView(int slot)
        {
            slotView = slot;
            focusSlot = slot;
            focusRow = 0;
            nameView = false;
            NameConfirmed = false;
        }

        private void OpenNameView()
        {
            nameView = true;
            int i = System.Array.FindIndex(PresetNames, n => string.Equals(n, SlotName(slotView), System.StringComparison.OrdinalIgnoreCase));
            focusName = i < 0 ? 0 : i;
        }

        private void ConfirmName()
        {
            int slot = slotView;
            if (focusName < PresetNames.Length)
            {
                todos?.RenameSlot(slot, PresetNames[focusName]);
                AfterNamed();
                return;
            }
            // Voice: a temporary system To-do captures the name (needs network); the list stays until it returns.
            if (todos == null || !todos.StartVoiceCapture(text =>
                {
                    string stored = string.IsNullOrWhiteSpace(text) ? null : todos.RenameSlot(slot, text);
                    hud.Toast(stored != null ? $"Named: {stored}" : "No name heard");
                    if (stored != null && slotView == slot) AfterNamed();
                    if (IsOpen) Refresh();
                }))
                hud.Toast("Voice not available");
        }

        /// <summary>Trigger: a name was confirmed -> back to the slot view, focus on "Place anchor here".</summary>
        private void AfterNamed()
        {
            NameConfirmed = true;
            nameView = false;
            focusRow = System.Array.IndexOf(rowsOrder, SlotAction.Place);
        }

        private void Confirm(SlotAction row)
        {
            int slot = slotView;
            switch (row)
            {
                case SlotAction.Name: OpenNameView(); break;
                case SlotAction.Place: PlaceHere(slot); break;
                case SlotAction.Remove:
                    if (store != null && store.RemoveSlot((ZoneType)slot)) hud.Toast($"Anchor {slot + 1} removed");
                    focusRow = 0;
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
            hud?.Toast($"Anchor {slot + 1} placed");
            return true;
        }

        private bool PlaceHere(int slot) => PlaceAnchor(slot, hud);

        /// <summary>Opened by onboarding: go straight to a slot view (focus on the Name row).</summary>
        public void ShowSlot(int slot)
        {
            OpenSlotView(Mathf.Clamp(slot, 0, 2));
            Refresh();
        }

        /// <summary>True while a slot (not the three boxes) is shown, including its name list.</summary>
        public bool InSlotView => slotView >= 0;
        /// <summary>True while the name list (popup) is shown.</summary>
        public bool InNameView => nameView;
    }
}
