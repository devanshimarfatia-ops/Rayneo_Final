using System.Collections.Generic;
using RayneoIdle.Profile;
using UnityEngine;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Pixel character (7x10, two walk frames per outfit) and pets (6x5), drawn in code. Simpler than
    /// Stardew-style sprites on purpose (30 deg FOV, 4 px texels). Codes: C = clothing (the chosen color),
    /// H = hair / hat, F = skin, P = pet, . = clear. Textures are cached per outfit + color + frame.
    /// </summary>
    public static class CharacterArt
    {
        public const int W = 7, H = 10;

        // Fixed tones: light (black = transparent, no dark gray per spec).
        private static readonly Color32 Hair = new Color32(0xE6, 0xC8, 0x8C, 0xFF);
        private static readonly Color32 Skin = new Color32(0xF4, 0xDC, 0xC4, 0xFF);

        public static Color32 ClothingColor(string id)
        {
            switch (id)
            {
                case "green": return Palette.Accent;
                case "gold": return new Color32(0xFF, 0xC8, 0x3D, 0xFF);
                case "silver": return new Color32(0xD0, 0xD8, 0xE0, 0xFF);
                case "purple": return new Color32(0x85, 0x66, 0xFF, 0xFF);
                case "pink": return new Color32(0xFF, 0x8A, 0xD8, 0xFF);
                default: return Palette.Main; // cyan
            }
        }

        // Each outfit: frame A (legs apart) + frame B (legs together). Top row first.
        private static readonly Dictionary<string, string[][]> Outfits = new Dictionary<string, string[][]>
        {
            ["explorer"] = new[]
            {
                new[] { "..HHH..", ".HHHHH.", "..FFF..", "..F.F..", ".CCCCC.", "F.CCC.F", "..CCC..", "..C.C..", ".C...C.", ".C...C." },
                new[] { "..HHH..", ".HHHHH.", "..FFF..", "..F.F..", ".CCCCC.", "F.CCC.F", "..CCC..", "..C.C..", "..C.C..", "..C.C.." },
            },
            ["dress"] = new[]
            {
                new[] { "..HHH..", ".HFFFH.", ".HF.FH.", ".HFFFH.", ".HCCCH.", "F.CCC.F", ".CCCCC.", "CCCCCCC", ".F...F.", ".F...F." },
                new[] { "..HHH..", ".HFFFH.", ".HF.FH.", ".HFFFH.", ".HCCCH.", "F.CCC.F", ".CCCCC.", "CCCCCCC", "..F.F..", "..F.F.." },
            },
            ["overalls"] = new[]
            {
                new[] { ".HHHH..", "HHHHHH.", "..FFF..", "..F.F..", ".FC.CF.", "F.CCC.F", "..CCC..", "..C.C..", ".C...C.", ".C...C." },
                new[] { ".HHHH..", "HHHHHH.", "..FFF..", "..F.F..", ".FC.CF.", "F.CCC.F", "..CCC..", "..C.C..", "..C.C..", "..C.C.." },
            },
            ["cloak"] = new[]
            {
                new[] { "..CCC..", ".CCCCC.", ".CFFFC.", ".CF.FC.", "CCCCCCC", "CCCCCCC", ".CCCCC.", ".CCCCC.", ".F...F.", ".F...F." },
                new[] { "..CCC..", ".CCCCC.", ".CFFFC.", ".CF.FC.", "CCCCCCC", "CCCCCCC", ".CCCCC.", ".CCCCC.", "..F.F..", "..F.F.." },
            },
        };

        // Pets, 6x5, facing right.
        private static readonly Dictionary<string, string[]> PetSprites = new Dictionary<string, string[]>
        {
            ["cat"] = new[] { "P...P.", "PP.PP.", "PPPPP.", ".PPPPP", ".P..P." },
            ["dog"] = new[] { "PP....", "PPPPP.", ".PPPPP", ".PPPP.", ".P..P." },
            ["bird"] = new[] { "..PP..", ".PPPP.", "PPPPPP", "..PP..", "..P.P." },
            ["turtle"] = new[] { "..PPP.", ".PPPPP", "PPPPPP", ".P..P.", "......" },
        };

        private static readonly Color32 PetColor = new Color32(0xE6, 0xC8, 0x8C, 0xFF);

        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        /// <summary>Character texture for an outfit + color, frame 0 or 1.</summary>
        public static Texture2D Character(string outfit, string color, int frame)
        {
            string key = $"{outfit}|{color}|{frame}";
            if (cache.TryGetValue(key, out var t)) return t;
            if (!Outfits.TryGetValue(outfit, out var frames)) frames = Outfits["explorer"];
            var rows = frames[Mathf.Clamp(frame, 0, 1)];
            var cloth = ClothingColor(color);
            var pc = new PixelCanvas(W, H);
            pc.Clear();
            for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
            {
                Color32 col;
                switch (rows[r][c])
                {
                    case 'C': col = cloth; break;
                    case 'H': col = Hair; break;
                    case 'F': col = Skin; break;
                    default: continue;
                }
                pc.Set(c, H - 1 - r, col);
            }
            return cache[key] = pc.ToTexture();
        }

        /// <summary>Pet texture, or null for "none".</summary>
        public static Texture2D Pet(string pet, bool flip = false)
        {
            if (!PetSprites.TryGetValue(pet ?? "", out var rows)) return null;
            string key = $"pet|{pet}|{flip}";
            if (cache.TryGetValue(key, out var t)) return t;
            var pc = new PixelCanvas(6, 5);
            pc.Clear();
            for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
                if (rows[r][c] == 'P') pc.Set(flip ? 5 - c : c, 4 - r, PetColor);
            return cache[key] = pc.ToTexture();
        }

        public static Texture2D Character(ProfileState p, int frame) => Character(p.outfit, p.color, frame);
    }
}
