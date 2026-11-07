using System;
using System.Collections.Generic;
using System.Linq;
using RayneoIdle.Cards;
using RayneoIdle.GameInput;
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Chest reward window (device test 2, Yaxin's sketch): a modal window over the HUD that shows what a chest
    /// gave, one reward after another. A blind pack in the chest is opened right here with a tap: its 5 cards
    /// appear in a frame right after the pack, the following rewards flow on (and wrap to the next row).
    /// Without a pack the rewards sit in one row of 5 places. Tap = skip the animation / open the next pack /
    /// close; double tap = close (an unopened pack stays in the Collection). The map is hidden while open.
    /// Triggers: IslandMap.OnChestOpened (PanelHost calls Show).
    /// </summary>
    public class RewardWindow : PanelBase
    {
        // ---- layout (canvas px) ----
        private const float TileW = 56f, TileH = 64f, LabelH = 22f, Gap = 12f, RowH = 100f;
        private const float GroupPad = 8f;
        private const float Left = 20f, TopY = -64f;
        private const int MaxTiles = 24;
        /// <summary>Seconds between two rewards appearing.</summary>
        private const float RevealSeconds = 0.3f;

        public override string Name => "Rewards";
        public override string Hint => !AllRevealed ? "Tap: show all" : NextPack() != null ? "Tap: open pack   Double tap: close" : "Tap: continue";

        /// <summary>A pack in this window was opened (onboarding listens).</summary>
        public event Action OnPackOpened;

        private class Item
        {
            public ChestSlotKind kind;
            public double coins;
            public CardDrawResult card;
            public List<CardDrawResult> packCards; // null = pack not opened yet
            public bool IsPack => kind == ChestSlotKind.Pack;
        }

        private class Tile
        {
            public RectTransform rt;
            public RawImage frame, image;
            public Text label;
        }

        private Text title;
        private readonly List<Tile> tiles = new List<Tile>();
        private readonly List<RawImage> groupFrames = new List<RawImage>();
        private readonly List<Item> items = new List<Item>();
        private Texture2D tileFrameTex, coinTex, packTex, groupTex;
        private Texture2D[] faces;
        private int revealed;     // number of tiles shown so far (flow order)
        private int totalTiles;
        private float nextRevealAt;
        private TreasureMapView map;

        private bool AllRevealed => revealed >= totalTiles;

        protected override void Build()
        {
            title = Title("Chest");
            map = UnityEngine.Object.FindObjectOfType<TreasureMapView>();

            var pc = new PixelCanvas((int)(TileW / 4), (int)(TileH / 4));
            pc.Clear();
            pc.RectOutline(0, 0, pc.Width, pc.Height, Palette.Secondary);
            tileFrameTex = pc.ToTexture();
            coinTex = PixelCanvas.FromSprite(PixelSprites.Coin);
            packTex = PixelCanvas.FromSprite(PixelSprites.Pack);

            float gw = 5 * TileW + 4 * Gap + 2 * GroupPad, gh = TileH + LabelH + 2 * GroupPad;
            var g = new PixelCanvas((int)(gw / 4), (int)(gh / 4));
            g.Clear();
            g.RectOutline(0, 0, g.Width, g.Height, Palette.Main);
            groupTex = g.ToTexture();

            var cat = session.Cards.Catalog;
            faces = new Texture2D[cat.Count];
            for (int i = 0; i < cat.Count; i++) faces[i] = CardArt.Face(cat.NumberInRarity(i), cat.Cards[i].rarity, true);

            for (int i = 0; i < 3; i++)
            {
                var gf = UiFactory.CreatePixelImage(root, $"Pack group {i}", groupTex, 4f, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero);
                gf.enabled = false;
                groupFrames.Add(gf);
            }
            for (int i = 0; i < MaxTiles; i++)
            {
                var t = new Tile();
                t.rt = UiFactory.CreateRect(root, $"Tile {i}", new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(TileW, TileH + LabelH));
                t.frame = UiFactory.CreatePixelImage(t.rt, "Frame", tileFrameTex, 4f, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero);
                t.image = UiFactory.CreatePixelImage(t.rt, "Image", coinTex, 4f, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -TileH / 2));
                t.label = UiFactory.CreateText(t.rt, "Label", UiFactory.AuxFont, Palette.Accent, TextAnchor.UpperCenter,
                    new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, -4), new Vector2(TileW + 24, LabelH + 4));
                t.rt.gameObject.SetActive(false);
                tiles.Add(t);
            }
        }

        /// <summary>Opens the window for a chest result (PanelHost, on IslandMap.OnChestOpened).</summary>
        public void Show(ChestOpenResult result)
        {
            items.Clear();
            foreach (var s in result.slots)
            {
                var it = new Item { kind = s.kind, coins = s.coins };
                if (s.kind == ChestSlotKind.Card && s.cards.Count > 0) it.card = s.cards[0];
                items.Add(it);
            }
            title.text = result.isTutorial ? "Tutorial chest" : "Chest";
            revealed = 0;
            nextRevealAt = Time.unscaledTime + RevealSeconds;
            if (IsOpen) Refresh(); else Open();
        }

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

        private Item NextPack() => items.FirstOrDefault(i => i.IsPack && i.packCards == null);

        public override void Tick()
        {
            if (!AllRevealed && Time.unscaledTime >= nextRevealAt)
            {
                revealed++;
                nextRevealAt = Time.unscaledTime + RevealSeconds;
                Refresh();
            }
        }

        protected override void OnAction(GameAction action)
        {
            if (action != GameAction.Confirm) return;
            if (!AllRevealed) { revealed = totalTiles; Refresh(); return; } // tap = skip the animation
            var pack = NextPack();
            if (pack != null && OpenPack(pack)) return;
            Close();
        }

        private bool OpenPack(Item pack)
        {
            var drawn = session.Cards.OpenPack(); // packs from chests are stored first; this opens one of them
            if (drawn == null) return false;
            pack.packCards = drawn;
            nextRevealAt = Time.unscaledTime + RevealSeconds;
            Refresh();
            OnPackOpened?.Invoke();
            return true;
        }

        /// <summary>For tests: show everything and open the next pack.</summary>
        public bool OpenNextPackForTest()
        {
            revealed = int.MaxValue / 2;
            var pack = NextPack();
            bool ok = pack != null && OpenPack(pack);
            revealed = totalTiles;
            Refresh();
            return ok;
        }

        public override void Refresh()
        {
            // Flow layout: tiles left to right, a pack's 5 cards as one framed group, wrap when a row is full.
            float maxRight = Left + PanelSize.x - 2 * Pad;
            float x = Left, y = TopY;
            int t = 0, group = 0;
            foreach (var gf in groupFrames) gf.enabled = false;

            void Place(Tile tile, float px, float py, int order)
            {
                tile.rt.anchoredPosition = new Vector2(px, py);
                tile.rt.gameObject.SetActive(order < revealed);
            }

            foreach (var it in items)
            {
                if (t >= tiles.Count) break;
                if (x + TileW > maxRight) { x = Left; y -= RowH; }
                var tile = tiles[t];
                SetupTile(tile, it);
                Place(tile, x, y, t);
                t++;
                x += TileW + Gap;

                if (it.IsPack && it.packCards != null && group < groupFrames.Count)
                {
                    float gw = 5 * TileW + 4 * Gap + 2 * GroupPad;
                    if (x + gw > maxRight + GroupPad) { x = Left; y -= RowH; }
                    var gf = groupFrames[group++];
                    gf.rectTransform.anchoredPosition = new Vector2(x, y + GroupPad);
                    gf.enabled = t < revealed + 1;
                    float cx = x + GroupPad;
                    foreach (var c in it.packCards)
                    {
                        if (t >= tiles.Count) break;
                        var ct = tiles[t];
                        SetupCard(ct, c);
                        Place(ct, cx, y, t);
                        t++;
                        cx += TileW + Gap;
                    }
                    x += gw + Gap;
                }
            }
            totalTiles = t;
            for (int i = t; i < tiles.Count; i++) tiles[i].rt.gameObject.SetActive(false);
        }

        private void SetupTile(Tile tile, Item it)
        {
            switch (it.kind)
            {
                case ChestSlotKind.Coins:
                    tile.frame.enabled = true;
                    SetImage(tile, coinTex, 4f);
                    tile.label.text = $"+{it.coins:0}";
                    tile.label.color = Palette.Accent;
                    break;
                case ChestSlotKind.Pack:
                    tile.frame.enabled = true;
                    SetImage(tile, packTex, 4f);
                    tile.label.text = it.packCards == null ? "Pack" : "Opened";
                    tile.label.color = it.packCards == null ? (Color)Palette.Accent : Palette.Secondary;
                    break;
                default:
                    SetupCard(tile, it.card);
                    break;
            }
        }

        private void SetupCard(Tile tile, CardDrawResult c)
        {
            tile.frame.enabled = false; // the card face has its own frame
            SetImage(tile, faces[c.cardIndex], 3f);
            tile.label.text = c.isNew ? "NEW" : $"+{c.duplicateCoins:0}";
            tile.label.color = c.isNew ? (Color)CardArt.RarityColor(c.card.rarity) : Palette.Secondary;
        }

        private static void SetImage(Tile tile, Texture2D tex, float px)
        {
            tile.image.texture = tex;
            tile.image.rectTransform.sizeDelta = new Vector2(tex.width, tex.height) * px;
        }
    }
}
