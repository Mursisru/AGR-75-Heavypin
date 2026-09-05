using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Sledgepin.Blueprinter
{
    internal static class NobpContent
    {
        private static AssetBundle? _bundle;
        private static GameObject? _rocket;
        private static GameObject? _launcher4;
        private static GameObject? _launcher6;
        private static bool _tried;

        internal static GameObject? RocketPrefab => _rocket;
        internal static GameObject? Launcher4Prefab => _launcher4;
        internal static GameObject? Launcher6Prefab => _launcher6;

        internal static void TryLoad()
        {
            if (_tried)
                return;
            _tried = true;
            try
            {
                _bundle = FindLoaded() ?? LoadFromDisk();
                if (_bundle == null)
                {
                    SledgepinPlugin.ModLog?.LogWarning("AGR75Sledgepin.nobp missing — visual stamp skipped.");
                    return;
                }

                _rocket = LoadNamed(SledgepinConstants.RocketVisualName, "HeavypinRocket", "Rocket");
                _launcher4 = LoadNamed(SledgepinConstants.LauncherVisualName + "4", "HeavypinLauncher4", "Launcher4", "4X", "4x");
                _launcher6 = LoadNamed(SledgepinConstants.LauncherVisualName + "6", "HeavypinLauncher6", "Launcher6", "6X", "6x");

                SledgepinPlugin.ModLog?.LogInfo(
                    $"Sledgepin nobp rocket={(_rocket != null)} launcher4={(_launcher4 != null)} launcher6={(_launcher6 != null)}");
            }
            catch (Exception ex)
            {
                SledgepinPlugin.ModLog?.LogError($"NobpContent: {ex}");
            }
        }

        internal static GameObject? LauncherForSlots(int slots) =>
            slots >= SledgepinConstants.SlotCount6 ? _launcher6 : _launcher4;

        private static GameObject? LoadNamed(string primary, params string[] hints)
        {
            GameObject? go = _bundle!.LoadAsset<GameObject>(primary);
            if (go != null)
                return go;
            GameObject[] all = _bundle.LoadAllAssets<GameObject>();
            if (all == null)
                return null;
            for (int i = 0; i < all.Length; i++)
            {
                GameObject cand = all[i];
                if (cand == null)
                    continue;
                if (string.Equals(cand.name, primary, StringComparison.OrdinalIgnoreCase))
                    return cand;
            }
            for (int h = 0; h < hints.Length; h++)
            {
                string hint = hints[h];
                for (int i = 0; i < all.Length; i++)
                {
                    GameObject cand = all[i];
                    if (cand != null && cand.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                        return cand;
                }
            }
            return null;
        }

        private static AssetBundle? FindLoaded()
        {
            foreach (AssetBundle b in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (b == null)
                    continue;
                try
                {
                    if (b.Contains(SledgepinConstants.RocketVisualName) ||
                        b.Contains("HeavypinRocket") ||
                        b.Contains(SledgepinConstants.NobpFileName) ||
                        b.Contains("AGR75Heavypin.nobp"))
                        return b;
                }
                catch
                {
                    // ignore
                }
            }
            return null;
        }

        private static AssetBundle? LoadFromDisk()
        {
            string? path = FindNobpPath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return null;
            AssetBundle? fromFile = AssetBundle.LoadFromFile(path);
            if (fromFile != null)
            {
                SledgepinPlugin.ModLog?.LogInfo($"Loaded .nobp from file: {path}");
                return fromFile;
            }
            SledgepinPlugin.ModLog?.LogWarning($"LoadFromFile returned null: {path}");
            return null;
        }

        private static string? FindNobpPath()
        {
            string? pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (string.IsNullOrEmpty(pluginDir))
                return null;
            string direct = Path.Combine(pluginDir, SledgepinConstants.NobpFileName);
            if (File.Exists(direct))
                return direct;
            string lower = Path.Combine(pluginDir, SledgepinConstants.NobpFileName.ToLowerInvariant());
            if (File.Exists(lower))
                return lower;
            string legacy = Path.Combine(pluginDir, "AGR75Heavypin.nobp");
            if (File.Exists(legacy))
                return legacy;
            string legacyLower = Path.Combine(pluginDir, "agr75heavypin.nobp");
            return File.Exists(legacyLower) ? legacyLower : null;
        }
    }
}
