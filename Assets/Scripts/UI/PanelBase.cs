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
            var t = UiFactory.CreateText(root, "Title", 28, Palette.Sand, TextAnchor.UpperLeft,
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
            row.text = UiFactory.CreateText(root, $"Row {index}", UiFactory.BodyFont, Palette.Sand, TextAnchor.MiddleLeft,
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
                text.color = focused ? (Color)Palette.Accent : color ?? Palette.Sand;
                marker.enabled = focused;
            }

            public void Hide()
            {
                text.text = "";
                marker.enabled = false;
            }
        }

        private static Texture2D marker;
        protected static Texture2D Marker => marker != null ? marker : (marker = PixelCanvas.FromSprite(PixelSprites.ArrowRight));

        /// <summary>Corner marks around the panel area (3x3 texel L-shapes), 4 px per texel.</summary>
        private static Texture2D CornerFrame()
        {
            int w = (int)(PanelSize.x / 4), h = (int)(PanelSize.y / 4);
            var pc = new PixelCanvas(w, h);
            pc.Clear();
            const int len = 5;
            foreach (var (x, y, dx, dy) in new[] { (0, 0, 1, 1), (w - 1, 0, -1, 1), (0, h - 1, 1, -1), (w - 1, h - 1, -1, -1) })
            {
                for (int i = 0; i < len; i++) { pc.Set(x + dx * i, y, Palette.Ink); pc.Set(x, y + dy * i, Palette.Ink); }
            }
            return pc.ToTexture();
        }
    }
}
