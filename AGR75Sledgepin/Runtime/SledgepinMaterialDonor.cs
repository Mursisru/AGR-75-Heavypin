using System;
using System.Collections.Generic;
using Sledgepin.Blueprinter;
using Sledgepin.Bootstrap;
using UnityEngine;

namespace Sledgepin.Runtime
{
    // Launcher embedded Rocket = visual truth. Fly SledgepinRocket clones these URP slots by mat name.
    internal static class SledgepinMaterialDonor
    {
        private static readonly Dictionary<string, Material> Templates =
            new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        private static bool _ready;

        internal static bool Ready => _ready && Templates.Count > 0;

        internal static void Ensure()
        {
            if (_ready)
                return;
            _ready = true;

            NobpContent.TryLoad();
            GameObject? launcher = NobpContent.Launcher4Prefab ?? NobpContent.Launcher6Prefab;
            if (launcher == null)
            {
                SledgepinPlugin.ModLog?.LogWarning("SledgepinMaterialDonor: no launcher prefab.");
                return;
            }

            GameObject? probe = null;
            try
            {
                probe = UnityEngine.Object.Instantiate(launcher);
                probe.SetActive(true);
                Transform? launcherRoot = PrefabFactory.FindLauncherVisual(probe.transform) ?? probe.transform;
                List<Transform> embedded = SledgepinLauncherRockets.FindEmbedded(launcherRoot, 1);
                if (embedded.Count == 0 || embedded[0] == null)
                {
                    SledgepinPlugin.ModLog?.LogWarning("SledgepinMaterialDonor: embedded Rocket missing.");
                    return;
                }

                GameObject rocketGo = embedded[0].gameObject;
                StockVisual.MarkOurs(embedded[0]);
                VisualMaterials.ApplyFbxLook(rocketGo, flyRocket: false);

                Renderer[] rs = rocketGo.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < rs.Length; i++)
                {
                    Renderer r = rs[i];
                    if (r == null || !StockVisual.IsOurs(r))
                        continue;
                    Material[] mats = r.sharedMaterials;
                    for (int m = 0; m < mats.Length; m++)
                    {
                        Material? mat = mats[m];
                        if (mat == null || string.IsNullOrEmpty(mat.name))
                            continue;
                        string key = Strip(mat.name);
                        if (Templates.ContainsKey(key))
                            continue;
                        Templates[key] = new Material(mat) { name = mat.name + "_donor" };
                    }
                }

                SledgepinPlugin.ModLog?.LogInfo($"SledgepinMaterialDonor cached slots={Templates.Count}");
            }
            catch (Exception ex)
            {
                SledgepinPlugin.ModLog?.LogWarning($"SledgepinMaterialDonor: {ex.Message}");
            }
            finally
            {
                if (probe != null)
                    UnityEngine.Object.Destroy(probe);
            }
        }

        internal static bool TryClone(string? matName, out Material? clone)
        {
            clone = null;
            if (string.IsNullOrEmpty(matName))
                return false;
            Ensure();
            if (!Templates.TryGetValue(Strip(matName), out Material? tpl) || tpl == null)
                return false;
            clone = new Material(tpl) { name = Strip(matName) + "_fly" };
            return true;
        }

        internal static Material? GetBaked(string? matName)
        {
            if (string.IsNullOrEmpty(matName))
                return null;
            Ensure();
            Templates.TryGetValue(Strip(matName), out Material? tpl);
            return tpl;
        }

        private static string Strip(string? raw)
        {
            if (raw is not { Length: > 0 } name)
                return string.Empty;
            int inst = name.LastIndexOf(" (Instance)", StringComparison.OrdinalIgnoreCase);
            if (inst > 0)
                name = name.Substring(0, inst);
            int hp = name.LastIndexOf("_hp", StringComparison.OrdinalIgnoreCase);
            if (hp > 0)
                name = name.Substring(0, hp);
            int fly = name.LastIndexOf("_fly", StringComparison.OrdinalIgnoreCase);
            if (fly > 0)
                name = name.Substring(0, fly);
            int donor = name.LastIndexOf("_donor", StringComparison.OrdinalIgnoreCase);
            if (donor > 0)
                name = name.Substring(0, donor);
            return name;
        }
    }
}
