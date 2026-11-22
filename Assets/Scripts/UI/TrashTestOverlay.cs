using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Trash on the treasure map, test version (the real trash system with regrowth and blocking does not exist yet).
    /// Design assumption: one trash every 30 min, so 24 h (the offline cap) = 48 trash = map full.
    /// "Fill" puts all 48 on the map at once; the map pointer (MapAimController) clears them one target at a time.
    /// Visual only: trash does not stop exploration, gives no coins yet and is not saved.
    ///
    /// Layout (player preference in Settings, kept per device in PlayerPrefs):
    ///   Pieces  - 48 single pieces (recommended: device test 2026-11-22, Yaxin preferred it). One tap = one piece.
    ///   Piles   - 12 piles x 4 layers (only 12 things to aim at). One tap clears a pile (4 trash).
    /// Placement: random but fixed (seeded), on land (inside 88% of the coast), clear of the chest areas,
    /// with a minimum spacing that is relaxed step by step if the island runs out of room.
    /// </summary>
    public class TrashTestOverlay : MonoBehaviour
    {
        private const string LogTag = "[Trash]";

        public enum Style { Piles = 0, Pieces = 1 }

        /// <summary>One aimable trash target.</summary>
        public class Item
        {
            public Vector2 center;   // map texels (middle of the art)
            public Vector2 bottom;   // map texels (bottom-center: where the explorer stands to pick it up)
            public bool queued;      // tapped, the explorer is on the way (shown green, not aimable)
            public float radius;     // aim radius, texels
            public int trash;        // trash units in it (pile = 4, piece = 1)
            public RawImage image;
        }

        // Tunables (texels; 1 texel = TreasureMapView.TexelPx canvas px).
        public const int Capacity = 48;               // 24 h x 2 per hour
        private const int PileCount = 12;
        private const int LayersPerPile = 4;
        private const float PieceMinSpacing = 8f;      // start spacing between single pieces
        private const float PileMinSpacing = 17f;      // start spacing between piles
        private const float PieceAimRadius = 6f;       // ~1 deg at map size M
        private const float PileAimRadius = 9f;
        private const float LandInset = 0.88f;         // keep off the coastline
        private const float AreaMargin = 2f;           // keep clear of the chest areas
        private const int Seed = 4807;
        public const Style RecommendedStyle = Style.Pieces;
        private const string StylePrefKey = "trash.style";

        /// <summary>Single piece, 5x4 (S = cyan, I = gray-cyan).</summary>
        private static readonly string[] Piece =
        {
            "..S..",
            ".III.",
            "II.II",
            "IIIII",
        };

        public Style CurrentStyle { get; private set; } = RecommendedStyle;
        public bool Filled => items.Count > 0;
        public IReadOnlyList<Item> Items => items;
        public int TrashCount { get; private set; }

        private TreasureMapView map;
        private readonly List<Item> items = new List<Item>();
        private Text label;
        private Texture2D pieceTex, pileTex, pieceFlash, pileFlash;

        public static string StyleName(Style s) => s == Style.Piles ? "12 piles" : "48 pieces";

        private void Awake()
        {
            CurrentStyle = (Style)Mathf.Clamp(PlayerPrefs.GetInt(StylePrefKey, (int)RecommendedStyle), 0, 1);
        }

        private bool Ready()
        {
            if (map == null) map = GetComponent<TreasureMapView>();
            if (map == null) map = FindObjectOfType<TreasureMapView>();
            if (map == null || map.MapRect == null) { Debug.LogWarning($"{LogTag} map not ready"); return false; }
            if (pieceTex == null)
            {
                pieceTex = PixelCanvas.FromSprite(Piece);
                pieceFlash = PixelCanvas.FromSprite(System.Array.ConvertAll(Piece, r => r.Replace('S', 'A').Replace('I', 'A')));
                pileTex = DrawPile(LayersPerPile, false);
                pileFlash = DrawPile(LayersPerPile, true);
            }
            return true;
        }

        /// <summary>Piles <-> Pieces. Saved. If trash is on the map it is redrawn full in the new layout.</summary>
        public Style ToggleStyle()
        {
            CurrentStyle = CurrentStyle == Style.Piles ? Style.Pieces : Style.Piles;
            PlayerPrefs.SetInt(StylePrefKey, (int)CurrentStyle);
            PlayerPrefs.Save();
            if (Filled) Fill();
            return CurrentStyle;
        }

        /// <summary>Test: put the full 48 trash on the map (replaces what is there).</summary>
        public void Fill()
        {
            if (!Ready()) return;
            ClearAll();
            if (CurrentStyle == Style.Pieces)
                foreach (var p in Place(Capacity, PieceMinSpacing, Seed)) Add(pieceTex, p, 1, PieceAimRadius);
            else
                foreach (var p in Place(PileCount, PileMinSpacing, Seed + 1)) Add(pileTex, p, LayersPerPile, PileAimRadius);
            label = UiFactory.CreateText(map.MapRect, "Trash label", 24, Palette.Main, TextAnchor.MiddleCenter,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -8 * TreasureMapView.TexelPx), new Vector2(480, 32));
            RefreshLabel();
            Debug.Log($"{LogTag} filled: {StyleName(CurrentStyle)}, {items.Count} targets, {TrashCount} trash");
        }

        /// <summary>Remove all trash and the label.</summary>
        public void ClearAll()
        {
            foreach (var it in items) if (it.image != null) Destroy(it.image.gameObject);
            items.Clear();
            TrashCount = 0;
            if (label != null) Destroy(label.gameObject);
            label = null;
        }

        /// <summary>Green flash on a target (tap feedback).</summary>
        public void Flash(Item it)
        {
            if (it?.image == null) return;
            it.image.texture = it.trash > 1 ? pileFlash : pieceFlash;
        }

        /// <summary>Remove one target (after its flash). Returns the trash units removed.</summary>
        public int Remove(Item it)
        {
            if (it == null || !items.Remove(it)) return 0;
            if (it.image != null) Destroy(it.image.gameObject);
            TrashCount -= it.trash;
            RefreshLabel();
            Debug.Log($"{LogTag} cleared {it.trash}, left {TrashCount}/{Capacity}");
            return it.trash;
        }

        /// <summary>Nearest target whose aim radius contains the texel, or null.</summary>
        public Item Pick(Vector2 texel)
        {
            Item best = null;
            float bestD = float.MaxValue;
            foreach (var it in items)
            {
                float d = Vector2.Distance(texel, it.center);
                if (!it.queued && d <= it.radius && d < bestD) { best = it; bestD = d; }
            }
            return best;
        }

        private void RefreshLabel()
        {
            if (label == null) return;
            // Trigger: map full (48/48) = in the real system exploration would pause until trash is cleared.
            label.text = TrashCount >= Capacity ? $"Trash {TrashCount}/{Capacity} - exploring paused" : $"Trash {TrashCount}/{Capacity}";
        }

        private void Add(Texture2D tex, Vector2 bottomCenter, int trash, float radius)
        {
            float px = TreasureMapView.TexelPx;
            var pos = new Vector2(Mathf.Round(bottomCenter.x), Mathf.Round(bottomCenter.y));
            var img = UiFactory.CreatePixelImage(map.MapRect, trash > 1 ? "Trash pile" : "Trash piece", tex, px,
                Vector2.zero, new Vector2(0.5f, 0f), pos * px);
            img.rectTransform.SetSiblingIndex(0); // under chests, explorer and pet
            items.Add(new Item { center = pos + new Vector2(0, tex.height / 2f), bottom = pos, radius = radius, trash = trash, image = img });
            TrashCount += trash;
        }

        /// <summary>A pile with n stacked layers, each 3 texels high and 2 narrower than the one below.</summary>
        private static Texture2D DrawPile(int layers, bool flash)
        {
            int w = 11, h = layers * 3 + 2;
            var pc = new PixelCanvas(w, h);
            pc.Clear();
            for (int k = 0; k < layers; k++)
                pc.RectOutline(k, k * 3, w - 2 * k, 3, flash ? Palette.Accent : k == layers - 1 ? Palette.Main : Palette.Secondary);
            pc.Set(w / 2, h - 1, flash ? Palette.Accent : Palette.Main); // small top piece
            return pc.ToTexture();
        }

        /// <summary>
        /// Seeded positions (bottom-center texels) on land and clear of chest areas. If `count` does not fit at
        /// `spacing`, the spacing shrinks by 10% and placement continues, so the full count is always reached.
        /// </summary>
        private List<Vector2> Place(int count, float spacing, int seed)
        {
            var rng = new System.Random(seed);
            var areas = map.AreaRectsTexels();
            var pts = new List<Vector2>();
            int attempts = 0;
            while (pts.Count < count && spacing > 2f)
            {
                var p = new Vector2(20 + (float)rng.NextDouble() * 100, 14 + (float)rng.NextDouble() * 72);
                attempts++;
                if (attempts > 4000) { spacing *= 0.9f; attempts = 0; }
                if (!TreasureMapView.IsOnLand(p, LandInset)) continue;
                bool clear = true;
                foreach (var a in areas)
                    if (p.x > a.xMin - AreaMargin && p.x < a.xMax + AreaMargin && p.y > a.yMin - AreaMargin && p.y < a.yMax + AreaMargin) { clear = false; break; }
                if (!clear) continue;
                foreach (var q in pts) if (Vector2.Distance(p, q) < spacing) { clear = false; break; }
                if (clear) pts.Add(p);
            }
            return pts;
        }
    }
}
