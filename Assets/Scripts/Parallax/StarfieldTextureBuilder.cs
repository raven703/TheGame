using UnityEngine;

namespace Parallax
{
    /// <summary>
    /// Строит Texture2D из StarfieldData. Текстура бесшовная (тайлится по краям).
    /// </summary>
    public static class StarfieldTextureBuilder
    {
        public static Texture2D Build(StarfieldData data)
        {
            if (data == null)
            {
                Debug.LogError("[StarfieldTextureBuilder] data == null");
                return null;
            }

            int size = Mathf.Max(16, data.textureSize);
            int starCount = data.stars != null ? data.stars.Length : 0;

            Debug.Log($"[StarfieldTextureBuilder] '{data.name}' → size={size}, stars={starCount}");

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeStarfield_" + data.name,
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 1,
                hideFlags = HideFlags.DontSave
            };

            var pixels = new Color32[size * size];
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            int drawn = 0;
            if (data.stars != null)
            {
                foreach (var star in data.stars)
                {
                    if (star.brightness <= 0f) continue;
                    DrawStar(pixels, size, star);
                    drawn++;
                }
            }

            // Считаем, сколько пикселей получили альфу > 0
            int nonEmptyPixels = 0;
            byte maxAlpha = 0;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 0)
                {
                    nonEmptyPixels++;
                    if (pixels[i].a > maxAlpha) maxAlpha = pixels[i].a;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);

            Debug.Log($"[StarfieldTextureBuilder] '{data.name}' → drawn={drawn}, " +
                      $"nonEmptyPixels={nonEmptyPixels}, maxAlpha={maxAlpha}");
            // Диагностика: посмотрим несколько пикселей
            int center = (size / 2) * size + (size / 2);
            Debug.Log($"[CheckPixel] '{data.name}' corner[0] = {pixels[0]}");
            Debug.Log($"[CheckPixel] '{data.name}' corner[last] = {pixels[pixels.Length - 1]}");
            Debug.Log($"[CheckPixel] '{data.name}' center = {pixels[center]}");
            Debug.Log($"[CheckPixel] '{data.name}' first nonEmpty = {System.Array.Find(pixels, p => p.a > 0)}");
            return tex;
        }

        private static void DrawStar(Color32[] pixels, int texSize, StarfieldData.Star star)
        {
            int cx = Mathf.RoundToInt(star.x * texSize);
            int cy = Mathf.RoundToInt(star.y * texSize);

            float radius = Mathf.Max(0.5f, star.size);
            int r = Mathf.CeilToInt(radius);

            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int centerX = cx + ox * texSize;
                    int centerY = cy + oy * texSize;
                    DrawCircle(pixels, texSize, centerX, centerY, radius, r, star);
                }
            }
        }

        private static void DrawCircle(
            Color32[] pixels, int texSize,
            int cx, int cy, float radius, int r,
            StarfieldData.Star star)
        {
            int minX = cx - r;
            int maxX = cx + r;
            int minY = cy - r;
            int maxY = cy + r;

            float radiusSq = radius * radius;

            for (int py = minY; py <= maxY; py++)
            {
                for (int px = minX; px <= maxX; px++)
                {
                    float dx = px - cx;
                    float dy = py - cy;
                    float distSq = dx * dx + dy * dy;

                    if (distSq > radiusSq) continue;

                    int tx = ((px % texSize) + texSize) % texSize;
                    int ty = ((py % texSize) + texSize) % texSize;
                    int index = ty * texSize + tx;

                    float dist = Mathf.Sqrt(distSq);
                    float falloff = 1f - (dist / radius);
                    falloff = Mathf.Clamp01(falloff);

                    if (radius <= 1.0f) falloff = 1f;

                    byte a = (byte)Mathf.RoundToInt(255f * falloff * star.brightness);

                    Color32 prev = pixels[index];
                    if (a > prev.a)
                    {
                        Color c = star.color;
                        pixels[index] = new Color32(
                            (byte)(c.r * 255f),
                            (byte)(c.g * 255f),
                            (byte)(c.b * 255f),
                            a);
                    }
                }
            }
        }
    }
}