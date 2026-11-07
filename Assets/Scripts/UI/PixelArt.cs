using System;
using System.Collections.Generic;
using UnityEngine;

namespace RayneoIdle.UI
{
    /// <summary>
    /// Agreed visual direction (2026-11-07): limited, muted "faded parchment" palette, line art on black.
    /// On the glasses black = transparent, so everything not drawn is see-through. No large fills (APL / power).
    /// </summary>
    public static class Palette
    {
        /// <summary>Main lines and text: warm sand.</summary>
        public static readonly Color32 Sand = new Color32(0xD8, 0xC0, 0x8A, 0xFF);
        /// <summary>Secondary lines, textures, inactive frames: old ink brown.</summary>
        public static readonly Color32 Ink = new Color32(0xA8, 0x8A, 0x5C, 0xFF);
        /// <summary>Highlights: focus, chests, coin count.</summary>
        public static readonly Color32 Accent = new Color32(0xE8, 0xD4, 0x9A, 0xFF);
        /// <summary>Transparent (= black on the glasses).</summary>
        public static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        /// <summary>Sprite string code -> color. '.' or ' ' = transparent.</summary>
        public static Color32 FromCode(char c)
        {
            switch (c)
            {
                case 'S': return Sand;
                case 'I': return Ink;
                case 'A': return Accent;
                default: return Clear;
            }
        }
    }

    /// <summary>
    /// Small CPU pixel canvas, uploaded as a point-filtered Texture2D. Texel (0,0) = bottom-left.
    /// Used for the treasure map and all pixel icons (drawn in code, no imported art yet).
    /// </summary>
    public class PixelCanvas
    {
        public readonly int Width, Height;
        private readonly Color32[] pixels;

        public PixelCanvas(int width, int height)
        {
            Width = width;
            Height = height;
            pixels = new Color32[width * height];
        }

        public void Clear()
        {
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Palette.Clear;
        }

        public void Set(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height) return;
            pixels[y * Width + x] = c;
        }

        /// <summary>Bresenham line, 1 texel wide.</summary>
        public void Line(int x0, int y0, int x1, int y1, Color32 c)
        {
            int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        public void HLine(int x0, int x1, int y, Color32 c) { for (int x = x0; x <= x1; x++) Set(x, y, c); }
        public void VLine(int x, int y0, int y1, Color32 c) { for (int y = y0; y <= y1; y++) Set(x, y, c); }

        public void RectOutline(int x, int y, int w, int h, Color32 c)
        {
            HLine(x, x + w - 1, y, c);
            HLine(x, x + w - 1, y + h - 1, c);
            VLine(x, y, y + h - 1, c);
            VLine(x + w - 1, y, y + h - 1, c);
        }

        /// <summary>Dashed outline: dash texels on, gap texels off, walking around the rectangle.</summary>
        public void DashedRect(int x, int y, int w, int h, int dash, int gap, Color32 c)
        {
            var path = new List<Vector2Int>();
            for (int i = 0; i < w; i++) path.Add(new Vector2Int(x + i, y));
            for (int i = 1; i < h; i++) path.Add(new Vector2Int(x + w - 1, y + i));
            for (int i = w - 2; i >= 0; i--) path.Add(new Vector2Int(x + i, y + h - 1));
            for (int i = h - 2; i > 0; i--) path.Add(new Vector2Int(x, y + i));
            int period = dash + gap;
            for (int i = 0; i < path.Count; i++) if (i % period < dash) Set(path[i].x, path[i].y, c);
        }

        /// <summary>
        /// Draws a sprite given as text rows, TOP row first (as it reads in code). Codes: see Palette.FromCode.
        /// </summary>
        public void Sprite(string[] rows, int x, int y)
        {
            int h = rows.Length;
            for (int r = 0; r < h; r++)
            {
                string row = rows[r];
                for (int col = 0; col < row.Length; col++)
                {
                    var c = Palette.FromCode(row[col]);
                    if (c.a > 0) Set(x + col, y + (h - 1 - r), c);
                }
            }
        }

        public Texture2D ToTexture(Texture2D reuse = null)
        {
            var tex = reuse;
            if (tex == null || tex.width != Width || tex.height != Height)
            {
                tex = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,   // crisp pixel look
                    wrapMode = TextureWrapMode.Clamp,
                };
            }
            tex.SetPixels32(pixels);
            tex.Apply(false);
            return tex;
        }

        /// <summary>One-shot texture from a sprite (icons, chest, character frames).</summary>
        public static Texture2D FromSprite(string[] rows)
        {
            int w = 0;
            foreach (var r in rows) w = Math.Max(w, r.Length);
            var pc = new PixelCanvas(w, rows.Length);
            pc.Sprite(rows, 0, 0);
            return pc.ToTexture();
        }
    }
}
