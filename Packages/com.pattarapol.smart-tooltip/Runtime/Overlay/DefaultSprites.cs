using UnityEngine;

namespace SmartTooltip
{
    // Made in code so the package needs no art to import.
    static class DefaultSprites
    {
        const int CornerRadius = 24;

        static Sprite roundedRect;
        static Sprite arrow;

        public static Sprite RoundedRect => roundedRect != null ? roundedRect : roundedRect = CreateRoundedRect();
        public static Sprite Arrow => arrow != null ? arrow : arrow = CreateArrow();

        static Sprite CreateRoundedRect()
        {
            var size = CornerRadius * 2 + 4;
            var center = (size - 1) * 0.5f;
            var inner = center - CornerRadius;
            var texture = NewTexture(size, size);
            var pixels = new Color32[size * size];

            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = Mathf.Max(Mathf.Abs(x - center) - inner, 0);
                var dy = Mathf.Max(Mathf.Abs(y - center) - inner, 0);
                var alpha = Mathf.Clamp01(CornerRadius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            var border = new Vector4(CornerRadius, CornerRadius, CornerRadius, CornerRadius);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
        }

        // Points up. The overlay turns it to face the target.
        static Sprite CreateArrow()
        {
            const int width = 64;
            const int height = 32;
            var texture = NewTexture(width, height);
            var pixels = new Color32[width * height];

            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var halfWidth = (1f - (y + 0.5f) / height) * width * 0.5f;
                var alpha = Mathf.Clamp01(halfWidth - Mathf.Abs(x + 0.5f - width * 0.5f) + 0.5f);
                pixels[y * width + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100);
        }

        static Texture2D NewTexture(int width, int height) =>
            new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
    }
}
