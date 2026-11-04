using System;
using UnityEngine;

namespace OpenMinimap.Util
{
    // Procedural textures for window chrome (masks, rings, the mod icon).
    // Everything object-related uses the game's own sprites instead.
    public static class TextureGen
    {
        public static Texture2D Bake(int size, Func<int, int, Color> pixel)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    px[y * size + x] = pixel(x, y);
            tex.SetPixels(px);
            tex.Apply(false, true);
            return tex;
        }

        public static Texture2D FilledCircle(int size, Color fill, Color ring, float ringWidth)
        {
            float c = (size - 1) / 2f;
            return Bake(size, (x, y) =>
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                if (d > c) return Color.clear;
                return d >= c - ringWidth ? ring : fill;
            });
        }

        public static Texture2D CircleAlpha(int size)
        {
            float c = (size - 1) / 2f;
            return Bake(size, (x, y) =>
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                return new Color(1f, 1f, 1f, Mathf.Clamp01(c - d + 0.5f));
            });
        }

        public static Texture2D RingAlpha(int size, float width)
        {
            float c = (size - 1) / 2f;
            return Bake(size, (x, y) =>
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float inside = Mathf.Clamp01(c - d + 0.5f);
                float band = Mathf.Clamp01(d - (c - width) + 0.5f);
                return new Color(1f, 1f, 1f, inside * band);
            });
        }

        public static Texture2D RectRingAlpha(int size, float width)
        {
            return Bake(size, (x, y) =>
            {
                float edge = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                return new Color(1f, 1f, 1f, Mathf.Clamp01(width - edge + 0.5f));
            });
        }

        public static Texture2D SolidAlpha(int size)
        {
            return Bake(size, (x, y) => Color.white);
        }

        // Solid triangle pointing up (used as a selectable marker shape).
        public static Texture2D TriangleAlpha(int size)
        {
            Vector2 apex = new Vector2(size * 0.5f, size * 0.92f);
            Vector2 left = new Vector2(size * 0.08f, size * 0.1f);
            Vector2 right = new Vector2(size * 0.92f, size * 0.1f);
            return Bake(size, (x, y) =>
            {
                var p = new Vector2(x + 0.5f, y + 0.5f);
                float d1 = Sign(p, apex, left);
                float d2 = Sign(p, left, right);
                float d3 = Sign(p, right, apex);
                bool inside = d1 >= 0 && d2 >= 0 && d3 >= 0;
                float edge = Mathf.Min(Mathf.Min(Mathf.Abs(d1), Mathf.Abs(d2)), Mathf.Abs(d3));
                float a = inside ? Mathf.Clamp01(edge + 0.5f) : 0f;
                return new Color(1f, 1f, 1f, a);
            });
        }

        private static float Sign(Vector2 p, Vector2 a, Vector2 b)
        {
            return (b.x - a.x) * (p.y - a.y) - (a.y - b.y) * (p.x - a.x);
        }

        public static Sprite MakeSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
