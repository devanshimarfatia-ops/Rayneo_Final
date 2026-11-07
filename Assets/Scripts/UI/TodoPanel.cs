using System.Collections.Generic;
using RayneoIdle.GameInput;
using RayneoIdle.Todo;
using UnityEngine;

namespace RayneoIdle.UI
{
    /// <summary>
    /// W7 To-do panel: the todos of the current zone (ZoneTodoService.Visible) plus a "New to-do" row.
    /// Up / Down (and Prev / Next) move the focus, the list scrolls by <see cref="VisibleRows"/>.
    /// Tap on "New to-do" = system voice create (startTodo); tap on a todo = finish it (+To-do reward, toast by HUD).
    /// </summary>
    public class TodoPanel : PanelBase
    {
        private const int VisibleRows = 6;

        public override string Name => "Todo";
        public override string Hint => "Tap: finish   Swipe: move   Double tap: back";

        private ZoneTodoService todos;
        private UnityEngine.UI.Text title, emptyText;
        private readonly List<Row> rows = new List<Row>();
        private int focus;   // 0 = "New to-do", 1.. = todos
        private int scroll;  // first item index shown

        private int ItemCount => 1 + (todos != null ? todos.Visible.Count : 0);

        protected override void Build()
        {
            todos = Object.FindObjectOfType<ZoneTodoService>();
            title = Title("To-do");
            for (int i = 0; i < VisibleRows; i++) rows.Add(CreateRow(i));
            emptyText = UiFactory.CreateText(root, "Empty", UiFactory.AuxFont, Palette.Secondary, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(Pad + 32, -Pad - 48 - RowHeight * 1.5f), new Vector2(400, RowHeight));
            if (todos != null)
            {
                todos.OnListChanged += () => { if (IsOpen) Refresh(); };
                todos.OnZoneChanged += _ => { if (IsOpen) Refresh(); };
                todos.OnVoiceCreateFailed += () => hud.Toast("No new to-do");
                todos.OnTodoCreated += t => hud.Toast("To-do added");
            }
        }

        public override void Open()
        {
            focus = 0;
            scroll = 0;
            todos?.Refresh();
            base.Open();
        }

        public override void Refresh()
        {
            if (todos == null)
            {
                title.text = "To-do";
                emptyText.text = "To-do service missing";
                foreach (var r in rows) r.Hide();
                return;
            }

            title.text = todos.CurrentZone == TodoZone.General ? "To-do" : $"To-do  {todos.CurrentZone}";
            focus = Mathf.Clamp(focus, 0, ItemCount - 1);
            if (focus < scroll) scroll = focus;
            if (focus >= scroll + VisibleRows) scroll = focus - VisibleRows + 1;

            for (int i = 0; i < VisibleRows; i++)
            {
                int item = scroll + i;
                if (item >= ItemCount) { rows[i].Hide(); continue; }
                if (item == 0) rows[i].Set(todos.WaitingForVoice ? "+ Listening..." : "+ New to-do (voice)", focus == 0, Palette.Secondary);
                else
                {
                    var t = todos.Visible[item - 1];
                    string zoneTag = t.zone == TodoZone.General || t.zone == todos.CurrentZone ? "" : $"  ({t.zone})";
                    rows[i].Set(Shorten(t.displayText, 30) + zoneTag, focus == item);
                }
            }
            emptyText.text = ItemCount == 1 ? "No to-dos here" : "";
            if (ItemCount == 1) emptyText.rectTransform.anchoredPosition = new Vector2(Pad + 32, -Pad - 48 - RowHeight * 1.5f);
        }

        protected override void OnAction(GameAction action)
        {
            switch (action)
            {
                case GameAction.Up:
                case GameAction.Prev:
                    focus = Mathf.Max(0, focus - 1);
                    Refresh();
                    break;
                case GameAction.Down:
                case GameAction.Next:
                    focus = Mathf.Min(ItemCount - 1, focus + 1);
                    Refresh();
                    break;
                case GameAction.Confirm:
                    Confirm();
                    break;
            }
        }

        private void Confirm()
        {
            if (todos == null) return;
            if (focus == 0)
            {
                if (!todos.StartVoiceCreate()) hud.Toast("Voice not available");
                Refresh();
                return;
            }
            var todo = todos.Visible[focus - 1];
            if (todos.Finish(todo) < 0) hud.Toast("Could not finish");
            // Finish refreshes the list; keep focus on the same position.
            Refresh();
        }

        private static string Shorten(string s, int max) => s.Length <= max ? s : s.Substring(0, max - 1) + "…";
    }
}
