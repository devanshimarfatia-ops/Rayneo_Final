using RayneoIdle.Game;
using RayneoIdle.GameInput;
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Base for head-locked panels opened from the toolbar (To-do, Collection, Settings).
    /// Layout: center zone of the 640x480 canvas, PanelSize px, with corner marks (no full frame, low brightness).
    /// Input: pushed on the InputAdapter stack while open. Back (double tap) = OnBack(), and closes the panel
    /// when OnBack() returns false. Plain class; PanelHost owns it and calls Tick() each frame.
    /// </summary>
    public abstract class PanelBase : IGameInputHandler
    {
        /// <summary>Panel area in canvas px: leaves the top 80 px for coins / toasts and the bottom 64 px for hints.</summary>
        public static readonly Vector2 PanelSize = new Vector2(576, 336);
        protected const float Pad = 20f;
        protected const float RowHeight = 42f;

        protected RectTransform root;
        protected MainHud hud;
        protected GameSession session;

        public bool IsOpen { get; private set; }
        public abstract string Name { get; }

        /// <summary>Bottom hint while this panel is open.</summary>
        public virtual string Hint => "Tap: select   Double tap: back";

        public void Init(RectTransform parent, MainHud hud, GameSession session)
        {
            this.hud = hud;
            this.session = session;
            root = UiFactory.CreateRect(parent, Name + " Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0, -8), PanelSize); // 8 px lower: clears the coin block (top 80 px), bottom hint row stays free
            UiFactory.CreatePixelImage(root, "Corners", CornerFrame(), 4f, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            Build();
            root.gameObject.SetActive(false);
        }

        public virtual void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            root.gameObject.SetActive(true);
            InputAdapter.Instance?.Push(this);
            Refresh();
        }

        public virtual void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            root.gameObject.SetActive(false);
            InputAdapter.Instance?.Pop(this);
        }

        public bool HandleAction(GameAction action)
        {
            if (action == GameAction.Back)
            {
                if (!OnBack()) Close();
                return true;
            }
            OnAction(action);
            return true; // an open panel is modal: nothing falls through to the toolbar
        }

        protected abstract void Build();
        public abstract void Refresh();
        protected abstract void OnAction(GameAction action);
        /// <summary>Return true if Back was used inside the panel (e.g. leave a sub-view).</summary>
        protected virtual bool OnBack() => false;
        public virtual void Tick() { }

        // ---------------------------------------------------------------- helpers

        protected Text Title(string text)
        {
            var t = UiFactory.CreateText(root, "Title", 28, Palette.Main, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(Pad, -Pad + 4), new Vector2(PanelSize.x - 2 * Pad, 36));
            t.text = text;
            return t;
        }

        /// <summary>A list row: focus arrow + text. Row 0 is just below the title.</summary>
        protected Row CreateRow(int index, float indentPx = 0)
        {
            float y = -Pad - 48 - index * RowHeight;
            var row = new Row();
            row.marker = UiFactory.CreatePixelImage(root, $"Row {index} marker", Marker, 4f,
                new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(Pad, y - RowHeight / 2));
            row.text = UiFactory.CreateText(root, $"Row {index}", UiFactory.BodyFont, Palette.Main, TextAnchor.MiddleLeft,
                new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(Pad + 32 + indentPx, y - RowHeight / 2),
                new Vector2(PanelSize.x - 2 * Pad - 32 - indentPx, RowHeight));
            return row;
        }

        protected class Row
        {
            public RawImage marker;
            public Text text;

            public void Set(string label, bool focused, Color? color = null)
            {
                text.text = label;
                text.color = focused ? (Color)Palette.Accent : color ?? Palette.Main;
                marker.enabled = focused;
            }

            public void Hide()
            {
                text.text = "";
                marker.enabled = false;
            }
        }

        // ---------------------------------------------------------------- three boxes (Anchors, Collection overview)

        /// <summary>Box size and gap (canvas px). 3 x 160 + 2 x 28 = 536 px, fits the 576 px panel.</summary>
        protected static readonly Vector2 BoxSize = new Vector2(160, 216);
        protected const float BoxGap = 28f;
        /// <summary>Box top, from the panel top (below the title).</summary>
        protected const float BoxTop = 64f;

        protected class Box
        {
            public RectTransform rt;
            public RawImage frame;
            public Text big, name, status;

            public void SetFocused(bool focused, Texture2D normal, Texture2D focusTex)
            {
                frame.texture = focused ? focusTex : normal;
            }
        }

        private static Texture2D boxTex, boxFocusTex;
        protected static Texture2D BoxTex => boxTex != null ? boxTex : (boxTex = BoxFrame(false));
        protected static Texture2D BoxFocusTex => boxFocusTex != null ? boxFocusTex : (boxFocusTex = BoxFrame(true));

        /// <summary>Unfocused: 4 px secondary outline. Focused: 8 px accent outline.</summary>
        private static Texture2D BoxFrame(bool focused)
        {
            int w = (int)(BoxSize.x / 4), h = (int)(BoxSize.y / 4);
            var pc = new PixelCanvas(w, h);
            pc.Clear();
            pc.RectOutline(0, 0, w, h, focused ? Palette.Accent : Palette.Secondary);
            if (focused) pc.RectOutline(1, 1, w - 2, h - 2, Palette.Accent);
            return pc.ToTexture();
        }

        /// <summary>Three boxes side by side (layout from Yaxin's sketch): big label, name, status line.</summary>
        protected Box[] CreateBoxes(RectTransform parent)
        {
            var boxes = new Box[3];
            float total = 3 * BoxSize.x + 2 * BoxGap;
            for (int i = 0; i < 3; i++)
            {
                float x = -total / 2 + i * (BoxSize.x + BoxGap);
                var b = new Box();
                b.rt = UiFactory.CreateRect(parent, $"Box {i + 1}", new Vector2(0.5f, 1), new Vector2(0, 1), new Vector2(x, -BoxTop), BoxSize);
                b.frame = UiFactory.CreatePixelImage(b.rt, "Frame", BoxTex, 4f, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
                b.big = UiFactory.CreateText(b.rt, "Big", 36, Palette.Main, TextAnchor.MiddleCenter,
                    new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(BoxSize.x - 16, 60));
                b.name = UiFactory.CreateText(b.rt, "Name", UiFactory.BodyFont, Palette.Main, TextAnchor.UpperCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1), new Vector2(0, 10), new Vector2(BoxSize.x - 16, 56));
                b.status = UiFactory.CreateText(b.rt, "Status", UiFactory.AuxFont, Palette.Secondary, TextAnchor.LowerCenter,
                    new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(BoxSize.x - 16, 48));
                boxes[i] = b;
            }
            return boxes;
        }

        // ---------------------------------------------------------------- wide buttons (shop row, sketch: "Buy A Pack")

        protected const float ButtonH = 40f;

        protected class Button
        {
            public RawImage frame;
            public Text text;
            public float width;

            public void Set(string label, bool focused, bool enabled = true)
            {
                frame.gameObject.SetActive(true);
                frame.texture = ButtonFrame(width, focused);
                text.text = label;
                text.color = focused ? (Color)Palette.Accent : enabled ? (Color)Palette.Main : Palette.Secondary;
            }

            public void Hide() => frame.gameObject.SetActive(false);

            /// <summary>Move / resize (layout changes with the number of buttons in the row).</summary>
            public void Place(float x, float y, float w)
            {
                width = w;
                frame.rectTransform.anchoredPosition = new Vector2(x, y);
                frame.rectTransform.sizeDelta = new Vector2(w, ButtonH);
                text.rectTransform.sizeDelta = new Vector2(w - 16, ButtonH);
            }
        }

        private static readonly System.Collections.Generic.Dictionary<(int, bool), Texture2D> buttonTex =
            new System.Collections.Generic.Dictionary<(int, bool), Texture2D>();

        /// <summary>Button outline: 4 px secondary, focused 8 px accent.</summary>
        private static Texture2D ButtonFrame(float width, bool focused)
        {
            int w = Mathf.Max(4, Mathf.RoundToInt(width / 4)), h = (int)(ButtonH / 4);
            if (buttonTex.TryGetValue((w, focused), out var t)) return t;
            var pc = new PixelCanvas(w, h);
            pc.Clear();
            pc.RectOutline(0, 0, w, h, focused ? Palette.Accent : Palette.Secondary);
            if (focused) pc.RectOutline(1, 1, w - 2, h - 2, Palette.Accent);
            return buttonTex[(w, focused)] = pc.ToTexture();
        }

        /// <summary>A wide button; position = its center, relative to the parent's center-top.</summary>
        protected Button CreateButton(RectTransform parent, string name)
        {
            var b = new Button();
            b.frame = UiFactory.CreatePixelImage(parent, name, ButtonFrame(400, false), 4f,
                new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), Vector2.zero);
            b.text = UiFactory.CreateText(b.frame.rectTransform, "Label", UiFactory.BodyFont, Palette.Main, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(384, ButtonH));
            b.Place(0, 0, 400);
            return b;
        }

        private static Texture2D marker;
        protected static Texture2D Marker => marker != null ? marker : (marker = PixelCanvas.FromSprite(PixelSprites.Chevron));

        /// <summary>Corner marks around the panel area (3x3 texel L-shapes), 4 px per texel.</summary>
        private static Texture2D CornerFrame()
        {
            int w = (int)(PanelSize.x / 4), h = (int)(PanelSize.y / 4);
            var pc = new PixelCanvas(w, h);
            pc.Clear();
            const int len = 5;
            foreach (var (x, y, dx, dy) in new[] { (0, 0, 1, 1), (w - 1, 0, -1, 1), (0, h - 1, 1, -1), (w - 1, h - 1, -1, -1) })
            {
                for (int i = 0; i < len; i++) { pc.Set(x + dx * i, y, Palette.Secondary); pc.Set(x, y + dy * i, Palette.Secondary); }
            }
            return pc.ToTexture();
        }
    }
}
