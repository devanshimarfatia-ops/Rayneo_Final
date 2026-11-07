using System.Collections.Generic;
using System.Linq;
using RayneoIdle.Cards;
using RayneoIdle.GameInput;
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.UI
{
    /// <summary>
    /// W5 Collection: 3x3 gallery (one row per rarity, D6), info pane, and "Open pack" for packs from chests.
    /// Not owned = silhouette (outline + "?"), owned = full card, NEW badge until the card was focused (D1: no
    /// reveal animation, cards show directly). Opening a pack shows its 5 cards in a row (reveal view).
    /// Focus: Prev / Next = +-1, Up / Down = +-1 row; below the grid = the "Open pack" row (if packs > 0).
    /// </summary>
    public class CollectionPanel : PanelBase
    {
        private const float CardPx = 4f;   // px per card texel -> 60x76 px cards
        private const float CellGapX = 16f, CellGapY = 10f;
        private const int Cols = 3;
        private const int PackItem = 9;    // focus index of the "Open pack" row

        public override string Name => "Collection";
        public override string Hint => revealing ? "Tap / double tap: back to collection" : "Tap: select   Double tap: back";

        private CardSystem cards;
        private Text title, infoName, infoRarity, infoStatus, revealInfo;
        private Row packRow;
        private readonly List<RawImage> cells = new List<RawImage>();
        /// <summary>NEW marker: 2x2 texel accent dot inside the card's top-right corner (text would cover the frame).</summary>
        private readonly List<RawImage> newBadges = new List<RawImage>();
        private RawImage focusImg;
        private Texture2D[] ownedFaces, silhouetteFaces;
        private RectTransform gridRt, revealRt;
        private readonly List<RawImage> revealCards = new List<RawImage>();
        private readonly List<Text> revealLabels = new List<Text>();
        private int focus;
        private bool revealing;

        private static Texture2D newDot;
        private static Texture2D NewDot
        {
            get
            {
                if (newDot != null) return newDot;
                var pc = new PixelCanvas(2, 2);
                for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) pc.Set(x, y, Palette.Accent);
                return newDot = pc.ToTexture();
            }
        }

        private float CardW => CardArt.W * CardPx;
        private float CardH => CardArt.H * CardPx;

        protected override void Build()
        {
            cards = session.Cards;
            int n = cards.Catalog.Count;
            ownedFaces = new Texture2D[n];
            silhouetteFaces = new Texture2D[n];
            for (int i = 0; i < n; i++)
            {
                ownedFaces[i] = CardArt.Face(i, cards.Catalog.Cards[i].rarity, true);
                silhouetteFaces[i] = CardArt.Face(i, cards.Catalog.Cards[i].rarity, false);
            }

            title = Title("Collection");

            // ---- gallery view ----
            gridRt = UiFactory.CreateRect(root, "Grid view", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);
            for (int i = 0; i < n; i++)
            {
                var pos = CellPos(i);
                cells.Add(UiFactory.CreatePixelImage(gridRt, $"Card {i}", silhouetteFaces[i], CardPx,
                    new Vector2(0, 1), new Vector2(0, 1), pos));
                newBadges.Add(UiFactory.CreatePixelImage(gridRt, $"New {i}", NewDot, CardPx,
                    new Vector2(0, 1), new Vector2(0, 1), pos + new Vector2(CardW - 3 * CardPx, -1 * CardPx)));
            }
            focusImg = UiFactory.CreatePixelImage(gridRt, "Focus", CardArt.FocusBrackets(), CardPx,
                new Vector2(0, 1), new Vector2(0, 1), Vector2.zero);

            float infoX = Pad + Cols * CardW + (Cols - 1) * CellGapX + 36;
            float infoW = PanelSize.x - infoX - Pad;
            infoName = UiFactory.CreateText(gridRt, "Info name", 24, Palette.Sand, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(infoX, -72), new Vector2(infoW, 30));
            infoRarity = UiFactory.CreateText(gridRt, "Info rarity", UiFactory.AuxFont, Palette.Sand, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(infoX, -106), new Vector2(infoW, 24));
            infoStatus = UiFactory.CreateText(gridRt, "Info status", UiFactory.AuxFont, Palette.Ink, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(infoX, -132), new Vector2(infoW, 48));

            // "Open pack" row, bottom right
            packRow = new Row
            {
                marker = UiFactory.CreatePixelImage(gridRt, "Pack marker", Marker, 4f,
                    new Vector2(0, 0), new Vector2(0, 0.5f), new Vector2(infoX - 30, Pad + RowHeight / 2)),
                text = UiFactory.CreateText(gridRt, "Pack row", UiFactory.BodyFont, Palette.Sand, TextAnchor.MiddleLeft,
                    new Vector2(0, 0), new Vector2(0, 0.5f), new Vector2(infoX, Pad + RowHeight / 2), new Vector2(infoW, RowHeight)),
            };

            // ---- pack reveal view ----
            revealRt = UiFactory.CreateRect(root, "Reveal view", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);
            float total = 5 * CardW + 4 * 14f;
            for (int i = 0; i < 5; i++)
            {
                float x = -total / 2 + i * (CardW + 14f);
                revealCards.Add(UiFactory.CreatePixelImage(revealRt, $"Reveal {i}", silhouetteFaces[0], CardPx,
                    new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(x, 10)));
                revealLabels.Add(UiFactory.CreateText(revealRt, $"Reveal label {i}", UiFactory.AuxFont, Palette.Accent, TextAnchor.UpperCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(x - 10, 10 - CardH / 2 - 6), new Vector2(CardW + 20, 24)));
            }
            revealInfo = UiFactory.CreateText(revealRt, "Reveal info", UiFactory.AuxFont, Palette.Sand, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, Pad), new Vector2(PanelSize.x - 2 * Pad, 24));
            revealRt.gameObject.SetActive(false);
        }

        private Vector2 CellPos(int i) =>
            new Vector2(Pad + (i % Cols) * (CardW + CellGapX), -64 - (i / Cols) * (CardH + CellGapY));

        private int MaxFocus => cards.State.unopenedPacks > 0 ? PackItem : cards.Catalog.Count - 1;

        public override void Open()
        {
            revealing = false;
            focus = 0;
            base.Open();
        }

        public override void Refresh()
        {
            gridRt.gameObject.SetActive(!revealing);
            revealRt.gameObject.SetActive(revealing);
            if (revealing) return;

            var st = cards.State;
            int n = cards.Catalog.Count;
            title.text = $"Collection  {st.OwnedDistinct()}/{n}";
            focus = Mathf.Clamp(focus, 0, MaxFocus);

            for (int i = 0; i < n; i++)
            {
                bool owned = st.Owns(i);
                cells[i].texture = owned ? ownedFaces[i] : silhouetteFaces[i];
                newBadges[i].enabled = owned && st.isNew[i];
            }

            focusImg.enabled = focus < n;
            if (focus < n) focusImg.rectTransform.anchoredPosition = CellPos(focus) + new Vector2(-2 * CardPx, 2 * CardPx);

            if (focus < n)
            {
                var def = cards.Catalog.Cards[focus];
                bool owned = st.Owns(focus);
                infoName.text = owned ? def.displayName : "???";
                infoRarity.text = CardArt.RarityName(def.rarity);
                infoRarity.color = CardArt.RarityColor(def.rarity);
                infoStatus.text = owned ? (st.ownedCounts[focus] > 1 ? $"Owned x{st.ownedCounts[focus]}" : "Owned") : "Not found yet";
                if (owned && st.isNew[focus]) infoStatus.text += "   NEW";
                if (owned) cards.MarkSeen(focus); // seen once focused: the dot clears on the next refresh
            }
            else
            {
                infoName.text = "Card pack";
                infoRarity.text = "5 cards";
                infoRarity.color = Palette.Sand;
                infoStatus.text = "";
            }

            if (st.unopenedPacks > 0) packRow.Set($"Open pack ({st.unopenedPacks})", focus == PackItem);
            else packRow.Hide();
        }

        protected override void OnAction(GameAction action)
        {
            if (revealing)
            {
                if (action == GameAction.Confirm) { revealing = false; Refresh(); }
                return;
            }
            int n = cards.Catalog.Count;
            switch (action)
            {
                case GameAction.Prev: focus = Mathf.Max(0, focus - 1); break;
                case GameAction.Next: focus = Mathf.Min(MaxFocus, focus + 1); break;
                case GameAction.Up: focus = focus == PackItem ? n - 1 : Mathf.Max(focus % Cols, focus - Cols); break;
                case GameAction.Down:
                    if (focus < n - Cols) focus += Cols;
                    else if (MaxFocus == PackItem) focus = PackItem;
                    break;
                case GameAction.Confirm:
                    if (focus == PackItem) OpenPack();
                    break;
            }
            Refresh();
        }

        protected override bool OnBack()
        {
            if (!revealing) return false;
            revealing = false;
            Refresh();
            return true;
        }

        private void OpenPack()
        {
            var drawn = cards.OpenPack();
            if (drawn == null) return;
            for (int i = 0; i < revealCards.Count; i++)
            {
                bool has = i < drawn.Count;
                revealCards[i].enabled = has;
                revealLabels[i].text = "";
                if (!has) continue;
                var c = drawn[i];
                revealCards[i].texture = ownedFaces[c.cardIndex];
                revealLabels[i].text = c.isNew ? "NEW" : $"+{c.duplicateCoins:0}";
                revealLabels[i].color = c.isNew ? (Color)CardArt.RarityColor(c.card.rarity) : Palette.Ink; // NEW in the rarity color
            }
            double dup = drawn.Sum(c => c.duplicateCoins);
            int fresh = drawn.Count(c => c.isNew);
            title.text = "Card pack";
            revealInfo.text = dup > 0 ? $"{fresh} new   +{dup:0} coins from duplicates" : $"{fresh} new";
            revealing = true;
        }

        /// <summary>For tests / snapshots: focus the "Open pack" row and open it.</summary>
        public bool OpenPackForTest()
        {
            if (cards.State.unopenedPacks <= 0) return false;
            focus = PackItem;
            OpenPack();
            Refresh();
            return true;
        }
    }
}
