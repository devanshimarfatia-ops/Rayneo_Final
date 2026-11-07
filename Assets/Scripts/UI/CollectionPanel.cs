using System.Collections.Generic;
using System.Linq;
using RayneoIdle.Cards;
using RayneoIdle.GameInput;
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.UI
{
    /// <summary>
    /// W5 Collection (toolbar chest icon). Three views:
    ///   Overview: three boxes = the three rarities with "(owned/18)" (same layout as Anchors), plus
    ///             "Open pack (N)" below them while packs from chests wait.
    ///   Rarity page: 6 x 3 grid of that rarity's 18 cards + info pane (name, copies, NEW).
    ///   Pack reveal: the 5 drawn cards in a row (D1: cards show directly, no reveal animation).
    /// Not owned = silhouette (outline + "?"), owned = full card; NEW dot until the card was focused.
    /// Overview: Prev / Next = box / pack row, Down = pack row, tap = open. Page: Prev / Next = +-1, Up / Down = +-1 row.
    /// Back: page / reveal -> overview -> close.
    /// </summary>
    public class CollectionPanel : PanelBase
    {
        private const int PageCols = 6, PageRows = 3;
        private const float PagePx = 3f;            // px per card texel on the page -> 45 x 57 px cards
        private const float PageGapX = 14f, PageGapY = 10f;
        private const float RevealPx = 4f;          // reveal cards: 60 x 76 px
        private const int FirstButton = 3;          // overview focus: 0..2 boxes, 3.. shop row buttons

        public override string Name => "Collection";
        public override string Hint =>
            view == View.Reveal ? "Tap / double tap: back"
            : view == View.Page ? "Swipe: move   Double tap: back"
            : "Swipe: choose   Tap: open   Double tap: back";

        private enum View { Overview, Page, Reveal }

        private static readonly CardRarity[] Rarities = { CardRarity.Common, CardRarity.Treasured, CardRarity.Special };

        private CardSystem cards;
        private Text title;
        private View view;

        // overview
        private RectTransform overviewRt;
        private Box[] boxes;
        private readonly RawImage[] boxCards = new RawImage[3];
        private Button openButton, buyButton;
        private int overviewFocus;

        // page
        private RectTransform pageRt;
        private readonly List<RawImage> pageCells = new List<RawImage>();
        private readonly List<RawImage> pageNewDots = new List<RawImage>();
        private RawImage pageFocus;
        private Text infoName, infoStatus;
        private int pageRarity, pageFocusIndex;

        // reveal
        private RectTransform revealRt;
        private readonly List<RawImage> revealCards = new List<RawImage>();
        private readonly List<Text> revealLabels = new List<Text>();
        private Text revealInfo;

        // faces per catalog index
        private Texture2D[] ownedFaces, silhouetteFaces;

        protected override void Build()
        {
            cards = session.Cards;
            var cat = cards.Catalog;
            ownedFaces = new Texture2D[cat.Count];
            silhouetteFaces = new Texture2D[cat.Count];
            for (int i = 0; i < cat.Count; i++)
            {
                int n = cat.NumberInRarity(i);
                ownedFaces[i] = CardArt.Face(n, cat.Cards[i].rarity, true);
                silhouetteFaces[i] = CardArt.Face(n, cat.Cards[i].rarity, false);
            }

            title = Title("Collection");

            // ---- overview ----
            overviewRt = UiFactory.CreateRect(root, "Overview", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);
            boxes = CreateBoxes(overviewRt);
            for (int i = 0; i < 3; i++)
            {
                boxes[i].big.fontSize = UiFactory.BodyFont; // rarity name instead of a number
                boxCards[i] = UiFactory.CreatePixelImage(boxes[i].rt, "Card", silhouetteFaces[0], 3f,
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 4));
            }
            // Shop row under the three boxes (device test 4 sketch "Buy A Pack"): [Open pack (N)] [Buy a pack]
            openButton = CreateButton(overviewRt, "Open pack");
            buyButton = CreateButton(overviewRt, "Buy pack");

            // ---- rarity page ----
            pageRt = UiFactory.CreateRect(root, "Page", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);
            var newDot = NewDot();
            for (int i = 0; i < CardCatalog.CardsPerRarity; i++)
            {
                var pos = PageCellPos(i);
                pageCells.Add(UiFactory.CreatePixelImage(pageRt, $"Cell {i}", silhouetteFaces[0], PagePx,
                    new Vector2(0, 1), new Vector2(0, 1), pos));
                pageNewDots.Add(UiFactory.CreatePixelImage(pageRt, $"New {i}", newDot, PagePx,
                    new Vector2(0, 1), new Vector2(0, 1), pos + new Vector2((CardArt.W - 3) * PagePx, -1 * PagePx)));
            }
            pageFocus = UiFactory.CreatePixelImage(pageRt, "Focus", CardArt.FocusBrackets(), PagePx,
                new Vector2(0, 1), new Vector2(0, 1), Vector2.zero);
            float infoX = Pad + PageCols * CardArt.W * PagePx + (PageCols - 1) * PageGapX + 24;
            float infoW = PanelSize.x - infoX - Pad;
            infoName = UiFactory.CreateText(pageRt, "Info name", UiFactory.BodyFont, Palette.Main, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(infoX, -72), new Vector2(infoW, 60));
            infoStatus = UiFactory.CreateText(pageRt, "Info status", UiFactory.AuxFont, Palette.Secondary, TextAnchor.UpperLeft,
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(infoX, -136), new Vector2(infoW, 60));

            // ---- pack reveal ----
            revealRt = UiFactory.CreateRect(root, "Reveal", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, PanelSize);
            float cw = CardArt.W * RevealPx, ch = CardArt.H * RevealPx;
            float total = 5 * cw + 4 * 14f;
            for (int i = 0; i < 5; i++)
            {
                float x = -total / 2 + i * (cw + 14f);
                revealCards.Add(UiFactory.CreatePixelImage(revealRt, $"Reveal {i}", silhouetteFaces[0], RevealPx,
                    new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(x, 10)));
                revealLabels.Add(UiFactory.CreateText(revealRt, $"Reveal label {i}", UiFactory.AuxFont, Palette.Accent, TextAnchor.UpperCenter,
                    new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(x - 10, 10 - ch / 2 - 6), new Vector2(cw + 20, 24)));
            }
            revealInfo = UiFactory.CreateText(revealRt, "Reveal info", UiFactory.AuxFont, Palette.Main, TextAnchor.LowerCenter,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, Pad), new Vector2(PanelSize.x - 2 * Pad, 24));
        }

        private static Vector2 PageCellPos(int i) => new Vector2(
            Pad + (i % PageCols) * (CardArt.W * PagePx + PageGapX),
            -64 - (i / PageCols) * (CardArt.H * PagePx + PageGapY));

        private static Texture2D NewDot()
        {
            var pc = new PixelCanvas(2, 2);
            for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) pc.Set(x, y, Palette.Accent);
            return pc.ToTexture();
        }

        private List<int> IndicesOf(CardRarity r) => cards.Catalog.IndicesOf(r);
        private int Owned(CardRarity r) => IndicesOf(r).Count(i => cards.State.Owns(i));
        private bool HasStoredPacks => cards.State.unopenedPacks > 0;
        private int MaxOverviewFocus => HasStoredPacks ? FirstButton + 1 : FirstButton;
        private bool OpenFocused => HasStoredPacks && overviewFocus == FirstButton;
        private bool BuyFocused => overviewFocus == MaxOverviewFocus && overviewFocus >= FirstButton;

        public override void Open()
        {
            view = View.Overview;
            overviewFocus = 0;
            title.color = Palette.Main;
            base.Open();
        }

        public override void Refresh()
        {
            overviewRt.gameObject.SetActive(view == View.Overview);
            pageRt.gameObject.SetActive(view == View.Page);
            revealRt.gameObject.SetActive(view == View.Reveal);
            if (view == View.Overview) RefreshOverview();
            else if (view == View.Page) RefreshPage();
        }

        private void RefreshOverview()
        {
            var st = cards.State;
            title.text = $"Collection  {st.OwnedDistinct()}/{cards.Catalog.Count}";
            overviewFocus = Mathf.Clamp(overviewFocus, 0, MaxOverviewFocus);
            for (int i = 0; i < 3; i++)
            {
                var r = Rarities[i];
                var idx = IndicesOf(r);
                int owned = Owned(r);
                var b = boxes[i];
                b.SetFocused(i == overviewFocus, BoxTex, BoxFocusTex);
                b.big.text = CardArt.RarityName(r);
                b.big.color = CardArt.RarityColor(r);
                b.name.text = "";
                // sample: the first owned card of this rarity, else a silhouette
                var ownedIdx = idx.Where(c => st.Owns(c)).ToList();
                boxCards[i].texture = ownedIdx.Count > 0 ? ownedFaces[ownedIdx[0]] : silhouetteFaces[idx[0]];
                int fresh = idx.Count(c => st.Owns(c) && st.isNew[c]);
                b.status.text = fresh > 0 ? $"({owned}/{idx.Count})\n{fresh} NEW" : $"({owned}/{idx.Count})";
                b.status.color = fresh > 0 ? (Color)Palette.Accent : Palette.Main;
            }
            // Shop row: two buttons while packs from chests wait, otherwise one wide "Buy a pack" (sketch).
            const float rowY = -(BoxTop + 216f + 30f);
            double price = session.Config.packPrice;
            bool afford = session.Core.State.coins >= price;
            if (HasStoredPacks)
            {
                openButton.Place(-136, rowY, 256);
                openButton.Set($"Open pack ({st.unopenedPacks})", OpenFocused);
                buyButton.Place(136, rowY, 256);
            }
            else
            {
                openButton.Hide();
                buyButton.Place(0, rowY, 400);
            }
            buyButton.Set($"Buy a pack   {price:N0} coins", BuyFocused, afford);
        }

        private void RefreshPage()
        {
            var st = cards.State;
            var r = Rarities[pageRarity];
            var idx = IndicesOf(r);
            title.text = $"{CardArt.RarityName(r)}  ({Owned(r)}/{idx.Count})";
            title.color = CardArt.RarityColor(r);
            pageFocusIndex = Mathf.Clamp(pageFocusIndex, 0, idx.Count - 1);
            for (int i = 0; i < pageCells.Count; i++)
            {
                bool has = i < idx.Count;
                pageCells[i].enabled = has;
                pageNewDots[i].enabled = false;
                if (!has) continue;
                int c = idx[i];
                pageCells[i].texture = st.Owns(c) ? ownedFaces[c] : silhouetteFaces[c];
                pageNewDots[i].enabled = st.Owns(c) && st.isNew[c];
            }
            pageFocus.rectTransform.anchoredPosition = PageCellPos(pageFocusIndex) + new Vector2(-2 * PagePx, 2 * PagePx);

            int card = idx[pageFocusIndex];
            bool own = st.Owns(card);
            infoName.text = own ? cards.Catalog.Cards[card].displayName : "???";
            infoStatus.text = !own ? "Not found yet"
                : (st.ownedCounts[card] > 1 ? $"Owned x{st.ownedCounts[card]}" : "Owned") + (st.isNew[card] ? "\nNEW" : "");
            if (own) cards.MarkSeen(card); // seen once focused: the NEW dot clears on the next refresh
        }

        protected override void OnAction(GameAction action)
        {
            switch (view)
            {
                case View.Reveal:
                    if (action == GameAction.Confirm) view = View.Overview;
                    break;
                case View.Page:
                    int count = IndicesOf(Rarities[pageRarity]).Count;
                    if (action == GameAction.Prev) pageFocusIndex = Mathf.Max(0, pageFocusIndex - 1);
                    else if (action == GameAction.Next) pageFocusIndex = Mathf.Min(count - 1, pageFocusIndex + 1);
                    else if (action == GameAction.Up) pageFocusIndex = Mathf.Max(pageFocusIndex % PageCols, pageFocusIndex - PageCols);
                    else if (action == GameAction.Down && pageFocusIndex + PageCols < count) pageFocusIndex += PageCols;
                    break;
                default:
                    if (action == GameAction.Prev) overviewFocus = Mathf.Max(0, overviewFocus - 1);
                    else if (action == GameAction.Next) overviewFocus = Mathf.Min(MaxOverviewFocus, overviewFocus + 1);
                    else if (action == GameAction.Down) { if (overviewFocus < FirstButton) overviewFocus = FirstButton; }
                    else if (action == GameAction.Up) { if (overviewFocus >= FirstButton) overviewFocus = 1; }
                    else if (action == GameAction.Confirm)
                    {
                        if (OpenFocused) OpenPack();
                        else if (BuyFocused) BuyAndOpenPack();
                        else { pageRarity = overviewFocus; pageFocusIndex = 0; view = View.Page; }
                    }
                    break;
            }
            if (view != View.Page) title.color = Palette.Main;
            Refresh();
        }

        protected override bool OnBack()
        {
            if (view == View.Overview) return false;
            view = View.Overview;
            title.color = Palette.Main;
            Refresh();
            return true;
        }

        /// <summary>Shop: pay the pack price and open the pack at once (instant reward, device test 4).</summary>
        private void BuyAndOpenPack()
        {
            if (!cards.BuyPack())
            {
                hud.Toast($"Need {session.Config.packPrice:N0} coins");
                return;
            }
            Debug.Log($"[Shop] bought a pack for {session.Config.packPrice:N0}, coins left {session.Core.State.coins:N0}");
            OpenPack();
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
                revealLabels[i].color = c.isNew ? (Color)CardArt.RarityColor(c.card.rarity) : Palette.Secondary; // NEW in the rarity color
            }
            double dup = drawn.Sum(c => c.duplicateCoins);
            int fresh = drawn.Count(c => c.isNew);
            title.text = "Card pack";
            revealInfo.text = dup > 0 ? $"{fresh} new   +{dup:0} coins from duplicates" : $"{fresh} new";
            view = View.Reveal;
        }

        /// <summary>For tests / snapshots: open a stored pack directly.</summary>
        public bool OpenPackForTest()
        {
            if (cards.State.unopenedPacks <= 0) return false;
            OpenPack();
            Refresh();
            return true;
        }

        /// <summary>For tests / snapshots: show a rarity page.</summary>
        public void ShowPageForTest(int rarity)
        {
            pageRarity = Mathf.Clamp(rarity, 0, 2);
            pageFocusIndex = 0;
            view = View.Page;
            Refresh();
        }
    }
}
