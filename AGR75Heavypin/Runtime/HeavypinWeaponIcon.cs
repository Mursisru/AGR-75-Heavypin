using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Heavypin.Runtime
{
    internal static class HeavypinWeaponIcon
    {
        private static Sprite? _sprite;
        private static bool _tried;

        internal static Sprite? Get()
        {
            if (_sprite != null)
                return _sprite;
            if (_tried)
                return null;
            _tried = true;
            byte[]? bytes = ReadBytes();
            if (bytes == null || bytes.Length == 0)
                return null;
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear: false);
                tex.name = "AGR-Preview";
                tex.filterMode = FilterMode.Bilinear;
                if (!ImageConversion.LoadImage(tex, bytes, markNonReadable: false))
                {
                    UnityEngine.Object.Destroy(tex);
                    return null;
                }
                ShadeToAlpha(tex);
                tex = FlipVerticalNative(tex);
                tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);
                _sprite = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f,
                    0,
                    SpriteMeshType.FullRect);
                _sprite.name = "AGR-Preview";
                return _sprite;
            }
            catch (Exception ex)
            {
                HeavypinPlugin.ModLog?.LogWarning($"Heavypin preview icon: {ex.Message}");
                return null;
            }
        }

        private static void ShadeToAlpha(Texture2D tex)
        {
            int baseA = HeavypinConstants.PreviewIconAlphaBase;
            int darkA = baseA / 2;
            int darkLuma = HeavypinConstants.PreviewIconDarkLuma;
            Color32[] px = tex.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                Color32 c = px[i];
                if (c.a == 0)
                {
                    px[i] = new Color32(255, 255, 255, 0);
                    continue;
                }
                int luma = (c.r * 299 + c.g * 587 + c.b * 114) / 1000;
                if (luma < 12)
                {
                    px[i] = new Color32(255, 255, 255, 0);
                    continue;
                }
                int a = luma < darkLuma ? darkA : baseA;
                a = a * c.a / 255;
                px[i] = new Color32(255, 255, 255, (byte)a);
            }
            tex.SetPixels32(px);
        }

        // Native PNG size — vertical flip only.
        private static Texture2D FlipVerticalNative(Texture2D src)
        {
            int w = src.width;
            int h = src.height;
            if (w < 2 || h < 2)
                return src;

            Color32[] px = src.GetPixels32();
            var dst = new Color32[px.Length];
            for (int y = 0; y < h; y++)
            {
                int srcRow = y * w;
                int dstRow = (h - 1 - y) * w;
                Array.Copy(px, srcRow, dst, dstRow, w);
            }

            var flipped = new Texture2D(w, h, TextureFormat.RGBA32, false, linear: false);
            flipped.name = src.name;
            flipped.filterMode = FilterMode.Bilinear;
            flipped.SetPixels32(dst);
            UnityEngine.Object.Destroy(src);
            return flipped;
        }

        private static byte[]? ReadBytes()
        {
            string? pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(pluginDir))
            {
                string path = Path.Combine(pluginDir, HeavypinConstants.PreviewIconFileName);
                if (File.Exists(path))
                    return File.ReadAllBytes(path);
            }
            Assembly asm = Assembly.GetExecutingAssembly();
            using Stream? s = asm.GetManifestResourceStream(HeavypinConstants.PreviewIconResource);
            if (s == null)
                return null;
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
