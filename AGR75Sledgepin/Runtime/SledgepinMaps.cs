using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace Sledgepin.Runtime
{
    internal static class SledgepinMaps
    {
        private static readonly Dictionary<string, string> AlbedoPath =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> NormalPath =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Texture2D> Cache =
            new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        private static bool _scanned;

        internal static Texture2D? Albedo(string matName) =>
            Load(Resolve(matName), AlbedoPath, linear: false, "_albedo");

        internal static Texture2D? Normal(string matName)
        {
            Texture2D? tex = Load(Resolve(matName), NormalPath, linear: true, "_nml");
            if (tex != null)
                PackNormalAg(tex);
            return tex;
        }

        internal static string Resolve(string? raw)
        {
            Scan();
            string key = Strip(raw);
            string fold = Fold(key);
            if (AlbedoPath.ContainsKey(fold))
                return fold;

            // Cyrillic ↔ Material alias + .001 → base body maps when own slot png missing.
            string[] aliases = AliasFolds(fold);
            for (int a = 0; a < aliases.Length; a++)
            {
                if (AlbedoPath.ContainsKey(aliases[a]))
                    return aliases[a];
            }

            foreach (string k in AlbedoPath.Keys)
            {
                if (fold.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    k.IndexOf(fold, StringComparison.OrdinalIgnoreCase) >= 0)
                    return k;
            }
            return fold;
        }

        private static string[] AliasFolds(string fold)
        {
            if (string.IsNullOrEmpty(fold))
                return Array.Empty<string>();
            // Main slot 2: own Материал.004 maps (no 003 alias).
            if (fold == "материал001" || fold == "material001" || fold.EndsWith("001", StringComparison.Ordinal))
            {
                string base0 = fold.Substring(0, fold.Length - 3);
                return new[] { base0, "материал", "material" };
            }
            if (fold == "материал" || fold == "material")
                return new[] { "материал", "material" };
            if (fold.StartsWith("материал", StringComparison.Ordinal) && fold.Length > "материал".Length)
            {
                string en = "material" + fold.Substring("материал".Length);
                return new[] { en, fold, "материал", "material" };
            }
            if (fold.StartsWith("material", StringComparison.Ordinal) && fold.Length > "material".Length)
            {
                string ru = "материал" + fold.Substring("material".Length);
                return new[] { fold, ru, "material", "материал" };
            }
            return Array.Empty<string>();
        }

        private static Texture2D? Load(string fold, Dictionary<string, string> table, bool linear, string suffix)
        {
            string cacheKey = fold + suffix;
            if (Cache.TryGetValue(cacheKey, out Texture2D hit))
                return hit;
            if (!table.TryGetValue(fold, out string? file) || string.IsNullOrEmpty(file) || !File.Exists(file))
                return null;
            try
            {
                byte[] bytes = File.ReadAllBytes(file);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear);
                if (!ImageConversion.LoadImage(tex, bytes, markNonReadable: false))
                    return null;
                tex.name = cacheKey;
                tex.wrapMode = TextureWrapMode.Repeat;
                tex.filterMode = FilterMode.Bilinear;
                tex.anisoLevel = 4;
                Cache[cacheKey] = tex;
                SledgepinPlugin.ModLog?.LogInfo($"SledgepinMaps loaded '{Path.GetFileName(file)}' {tex.width}x{tex.height}");
                return tex;
            }
            catch (Exception ex)
            {
                SledgepinPlugin.ModLog?.LogWarning($"SledgepinMaps '{file}': {ex.Message}");
                return null;
            }
        }

        private static void Scan()
        {
            if (_scanned)
                return;
            _scanned = true;
            var dirs = new List<string>(8);
            string? plugin = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(plugin))
                dirs.Add(Path.Combine(plugin, "Textures", "AGR75"));
            try
            {
                if (!string.IsNullOrEmpty(Paths.PluginPath))
                    dirs.Add(Path.Combine(Paths.PluginPath, "AGR-75-Sledgepin", "Textures", "AGR75"));
            }
            catch
            {
                // ignore
            }

            for (int i = 0; i < dirs.Count; i++)
            {
                if (!Directory.Exists(dirs[i]))
                    continue;
                string[] files = Directory.GetFiles(dirs[i], "*.png", SearchOption.AllDirectories);
                for (int f = 0; f < files.Length; f++)
                    Register(files[f]);
            }
        }

        private static void Register(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (name.IndexOf("Displacement", StringComparison.OrdinalIgnoreCase) >= 0)
                return;
            if (name.IndexOf("Preview", StringComparison.OrdinalIgnoreCase) >= 0)
                return;
            if (name.IndexOf("without Bump", StringComparison.OrdinalIgnoreCase) >= 0)
                return;

            bool color = name.EndsWith(" Color", StringComparison.OrdinalIgnoreCase) ||
                         name.EndsWith(" Color.png", StringComparison.OrdinalIgnoreCase);
            bool normal = name.EndsWith(" Normal", StringComparison.OrdinalIgnoreCase);
            if (!color && !normal)
            {
                if (name.IndexOf(" Color", StringComparison.OrdinalIgnoreCase) >= 0)
                    color = true;
                else if (name.IndexOf(" Normal", StringComparison.OrdinalIgnoreCase) >= 0)
                    normal = true;
            }
            if (!color && !normal)
                return;

            string stem = name;
            int idx = name.LastIndexOf(" Color", StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
                stem = name.Substring(0, idx);
            idx = stem.LastIndexOf(" Normal", StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
                stem = stem.Substring(0, idx);

            string fold = Fold(stem);
            if (string.IsNullOrEmpty(fold))
                return;
            if (color)
                AlbedoPath[fold] = path;
            else
                NormalPath[fold] = path;
        }

        private static string Strip(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return "mat";
            string n = name!;
            int i = n.LastIndexOf(" (Instance)", StringComparison.OrdinalIgnoreCase);
            if (i > 0)
                n = n.Substring(0, i);
            i = n.LastIndexOf("_hp", StringComparison.OrdinalIgnoreCase);
            if (i > 0)
                n = n.Substring(0, i);
            return n;
        }

        private static string Fold(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            char[] buf = new char[s.Length];
            int n = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsLetterOrDigit(c))
                    buf[n++] = char.ToLowerInvariant(c);
            }
            return new string(buf, 0, n);
        }

        private static void PackNormalAg(Texture2D tex)
        {
            if (tex == null)
                return;
            string packedKey = tex.name + "_ag";
            if (Cache.ContainsKey(packedKey))
                return;
            Color32[] px = tex.GetPixels32();
            for (int i = 0; i < px.Length; i++)
            {
                byte x = px[i].r;
                byte y = px[i].g;
                px[i] = new Color32(255, y, 255, x);
            }
            tex.SetPixels32(px);
            tex.Apply(updateMipmaps: true, makeNoLongerReadable: false);
            Cache[packedKey] = tex;
        }
    }
}
