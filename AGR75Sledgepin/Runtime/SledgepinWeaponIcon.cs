using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Sledgepin.Runtime
{
    internal static class SledgepinWeaponIcon
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
                tex = PreparePreview(tex);
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
                SledgepinPlugin.ModLog?.LogWarning($"Sledgepin preview icon: {ex.Message}");
                return null;
            }
        }

        private static void ShadeToAlpha(Texture2D tex)
        {
            int baseA = SledgepinConstants.PreviewIconAlphaBase;
            int darkA = baseA / 2;
            int darkLuma = SledgepinConstants.PreviewIconDarkLuma;
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

        // Native PNG size — vertical flip, horizontal mirror, vertical squeeze.
        private static Texture2D PreparePreview(Texture2D src)
        {
            int w = src.width;
            int h = src.height;
            if (w < 2 || h < 2)
                return src;

            float vScale = SledgepinConstants.PreviewIconVerticalScale;
            int newH = Mathf.Max(2, Mathf.RoundToInt(h * vScale));
            Color32[] srcPx = src.GetPixels32();
            UnityEngine.Object.Destroy(src);

            var dstPx = new Color32[w * newH];
            float invNewH = newH > 1 ? 1f / (newH - 1) : 0f;
            for (int y = 0; y < newH; y++)
            {
                float ty = 1f - y * invNewH;
                float srcYf = ty * (h - 1);
                int dstRow = y * w;
                for (int x = 0; x < w; x++)
                {
                    float srcXf = (w - 1) - x;
                    dstPx[dstRow + x] = SampleBilinear(srcPx, w, h, srcXf, srcYf);
                }
            }

            var dst = new Texture2D(w, newH, TextureFormat.RGBA32, false, linear: false);
            dst.name = "AGR-Preview";
            dst.filterMode = FilterMode.Bilinear;
            dst.SetPixels32(dstPx);
            return dst;
        }

        private static Color32 SampleBilinear(Color32[] px, int w, int h, float fx, float fy)
        {
            fx = Mathf.Clamp(fx, 0f, w - 1);
            fy = Mathf.Clamp(fy, 0f, h - 1);
            int x0 = (int)fx;
            int y0 = (int)fy;
            int x1 = Mathf.Min(x0 + 1, w - 1);
            int y1 = Mathf.Min(y0 + 1, h - 1);
            float tx = fx - x0;
            float ty = fy - y0;

            Color32 c00 = px[y0 * w + x0];
            Color32 c10 = px[y0 * w + x1];
            Color32 c01 = px[y1 * w + x0];
            Color32 c11 = px[y1 * w + x1];

            byte Lerp(byte a, byte b, float t) => (byte)Mathf.Clamp(Mathf.RoundToInt(a + (b - a) * t), 0, 255);
            return new Color32(
                Lerp(Lerp(c00.r, c10.r, tx), Lerp(c01.r, c11.r, tx), ty),
                Lerp(Lerp(c00.g, c10.g, tx), Lerp(c01.g, c11.g, tx), ty),
                Lerp(Lerp(c00.b, c10.b, tx), Lerp(c01.b, c11.b, tx), ty),
                Lerp(Lerp(c00.a, c10.a, tx), Lerp(c01.a, c11.a, tx), ty));
        }

        private static byte[]? ReadBytes()
        {
            string? pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(pluginDir))
            {
                string path = Path.Combine(pluginDir, SledgepinConstants.PreviewIconFileName);
                if (File.Exists(path))
                    return File.ReadAllBytes(path);
            }
            Assembly asm = Assembly.GetExecutingAssembly();
            using Stream? s = asm.GetManifestResourceStream(SledgepinConstants.PreviewIconResource);
            if (s == null)
                return null;
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
