using RayneoIdle.Cards;
using UnityEngine;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Placeholder card faces drawn in code (15x19 texels). Replace with the final artworks later (R11).
    /// Owned: frame + symbol in the rarity color, rarity pips at the bottom.
    /// Not owned (silhouette, D1): ink outline + "?" only. Outline is >= 2 px, so it stays visible on the
    /// glasses where black is transparent.
    /// </summary>
    public static class CardArt
    {
        public const int W = 15, H = 19;

        // Rarity colors from the GDD (Common silver-gray, Treasured gold, Special purple). Deliberately clear and
        // distinct, an exception to the muted parchment palette (Yaxin 2026-11-07). Silver is kept light:
        // the RayNeo spec forbids dark gray. Purple = RayNeo spec purple.
        public static readonly Color32 Common = new Color32(0xD0, 0xD8, 0xE0, 0xFF);    // silver-gray
        public static readonly Color32 Treasured = new Color32(0xFF, 0xC8, 0x3D, 0xFF); // gold
        public static readonly Color32 Special = new Color32(0x85, 0x66, 0xFF, 0xFF);   // purple

        /// <summary>"#RRGGBB" for rich text.</summary>
        public static string RarityHex(CardRarity r)
        {
            var c = RarityColor(r);
            return $"#{c.r:X2}{c.g:X2}{c.b:X2}";
        }

        public static Color32 RarityColor(CardRarity r) =>
            r == CardRarity.Common ? Common : r == CardRarity.Treasured ? Treasured : Special;

        public static string RarityName(CardRarity r) =>
            r == CardRarity.Common ? "Common" : r == CardRarity.Treasured ? "Treasured" : "Special";

        // 9x9 symbols, one per catalog index (R = rarity color, S = sand). Order = CardCatalog.CreateMvp().
        private static readonly string[][] Symbols =
        {
            new[] { "....R....", "...R.R...", "..R.R.R..", ".R..R..R.", ".R..R..R.", ".R.R.R.R.", "..RRRRR..", "...S.S...", "........." }, // shell
            new[] { ".RR...RR.", "R..R.R..R", "....R....", "...RSR...", "....S....", "....S....", "....S....", "...S.S...", "SSSSSSSSS" }, // palm
            new[] { ".........", "....RR...", "R..R..R..", "RRR....R.", "R..R..R..", "....RR...", ".........", ".........", "........." }, // fish
            new[] { "....R....", "...RRR...", "....R....", ".R..R..R.", "RRRRSRRRR", ".R..R..R.", "....R....", "...RRR...", "....R...." }, // compass
            new[] { "...RRR...", "...R.R...", "...RRR...", "....R....", ".RRRRRRR.", "....R....", "R...R...R", ".R..R..R.", "..RRRRR.." }, // anchor
            new[] { "..RRR....", ".R...R...", ".R...R...", "..RRR....", "...R.....", "...R.....", "...RRR...", "...R.....", "...RR...." }, // key
            new[] { ".........", "R...R...R", "RR.RRR.RR", "RRRRRRRRR", "R.S.R.S.R", "RRRRRRRRR", ".........", ".........", "........." }, // crown
            new[] { ".........", "..RRRRR..", ".R.R.R.R.", "RRRRRRRRR", ".R..S..R.", "..R...R..", "...R.R...", "....R....", "........." }, // gem
            new[] { "....R....", "....R....", "...RRR...", "RRRRSRRRR", ".RRRRRRR.", "..RRRRR..", "..RR.RR..", ".RR...RR.", "........." }, // star
        };

        private static readonly string[] Question =
        {
            ".III.", "I...I", "....I", "..II.", "..I..", ".....", "..I..",
        };

        public static Texture2D Face(int cardIndex, CardRarity rarity, bool owned)
        {
            var pc = new PixelCanvas(W, H);
            pc.Clear();
            if (!owned)
            {
                pc.RectOutline(0, 0, W, H, Palette.Ink);
                pc.Sprite(Question, 5, 7);
                return pc.ToTexture();
            }

            var rc = RarityColor(rarity);
            pc.RectOutline(0, 0, W, H, rc);
            // inner corner dots for a framed look
            pc.Set(2, 2, Palette.Ink); pc.Set(W - 3, 2, Palette.Ink); pc.Set(2, H - 3, Palette.Ink); pc.Set(W - 3, H - 3, Palette.Ink);

            var sym = Symbols[Mathf.Clamp(cardIndex, 0, Symbols.Length - 1)];
            for (int r = 0; r < sym.Length; r++)
            for (int c = 0; c < sym[r].Length; c++)
            {
                char ch = sym[r][c];
                if (ch == 'R') pc.Set(3 + c, H - 4 - r, rc);
                else if (ch == 'S') pc.Set(3 + c, H - 4 - r, Palette.Sand);
            }

            // rarity pips: 1 / 2 / 3 dots at the bottom
            int pips = (int)rarity + 1;
            for (int i = 0; i < pips; i++) pc.Set(W / 2 - (pips - 1) + i * 2, 2, rc);
            return pc.ToTexture();
        }

        /// <summary>Focus brackets around a card (W+4 x H+4 texels).</summary>
        public static Texture2D FocusBrackets()
        {
            int w = W + 4, h = H + 4;
            var pc = new PixelCanvas(w, h);
            pc.Clear();
            foreach (var (x, y, dx, dy) in new[] { (0, 0, 1, 1), (w - 1, 0, -1, 1), (0, h - 1, 1, -1), (w - 1, h - 1, -1, -1) })
            for (int i = 0; i < 4; i++) { pc.Set(x + dx * i, y, Palette.Accent); pc.Set(x, y + dy * i, Palette.Accent); }
            return pc.ToTexture();
        }
    }
}
