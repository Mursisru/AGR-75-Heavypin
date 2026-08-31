using Heavypin.Bootstrap;
using UnityEngine;

namespace Heavypin.Runtime
{
    internal static class VisualMaterials
    {
        internal static void StripSceneJunk(GameObject root)
        {
            if (root == null)
                return;
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (light != null)
                    UnityEngine.Object.DestroyImmediate(light.gameObject);
            }
            foreach (Camera cam in root.GetComponentsInChildren<Camera>(true))
            {
                if (cam != null)
                    UnityEngine.Object.DestroyImmediate(cam.gameObject);
            }
        }

        internal static void ApplyFbxLook(GameObject root)
        {
            if (root == null)
                return;
            StripSceneJunk(root);
            int n = 0;
            Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                Renderer r = rs[i];
                if (r == null || !StockVisual.IsOurs(r))
                    continue;
                if (r is not MeshRenderer && r is not SkinnedMeshRenderer)
                    continue;
                MeshFilter? mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh == null)
                    continue;
                if (r is SkinnedMeshRenderer skinChk && skinChk.sharedMesh == null)
                    continue;
                Material[] src = r.sharedMaterials;
                int slots = src != null && src.Length > 0 ? src.Length : 1;
                Material[] dst = new Material[slots];
                for (int m = 0; m < slots; m++)
                {
                    Material? old = src != null && m < src.Length ? src[m] : null;
                    string matName = old != null && !string.IsNullOrEmpty(old.name) ? old.name : r.gameObject.name;
                    Material mat = VisualShader.Make(matName + "_hp", cull: 0f);
                    Texture? albedo = HeavypinMaps.Albedo(matName);
                    if (albedo == null)
                    {
                        string? fb = MatFallback(matName);
                        if (!string.IsNullOrEmpty(fb))
                            albedo = HeavypinMaps.Albedo(fb!);
                    }
                    if (albedo == null)
                        albedo = PeekAlbedo(old);
                    bool albedoOwns = albedo != null;
                    if (albedo != null)
                        WriteAlbedo(mat, albedo);
                    else
                        ClearAlbedoMaps(mat);
                    Texture2D? nml = HeavypinMaps.Normal(matName);
                    if (nml == null)
                    {
                        string? fbN = MatFallback(matName);
                        if (!string.IsNullOrEmpty(fbN))
                            nml = HeavypinMaps.Normal(fbN!);
                    }
                    if (nml != null)
                        ApplyDiskNormal(mat, nml, old);
                    else
                    {
                        CopyMap(old, mat, "_BumpMap", "_BumpMap");
                        CopyMap(old, mat, "_BumpMap", "_NormalMap");
                    }
                    KillEmission(mat);
                    HeavypinLook.ApplyFromBaked(mat, old, albedoOwns);
                    dst[m] = mat;
                    n++;
                }
                r.sharedMaterials = dst;
                r.enabled = true;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                r.receiveShadows = true;
            }
            HeavypinPlugin.ModLog?.LogInfo($"VisualMaterials FBX-look '{root.name}' slots={n}");
        }

        internal static void MatchHostDrawState(GameObject vis, GameObject host)
        {
            if (vis == null || host == null)
                return;
            int layer = host.layer;
            uint mask = 1u;
            Renderer? donor = null;
            Renderer[] hostRs = host.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < hostRs.Length; i++)
            {
                Renderer r = hostRs[i];
                if (r == null)
                    continue;
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer))
                    continue;
                if (PrefabFactory.IsOurVisualRoot(r.transform))
                    continue;
                donor = r;
                layer = r.gameObject.layer;
                mask = r.renderingLayerMask;
                break;
            }

            Transform[] all = vis.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null)
                    all[i].gameObject.layer = layer;
            }
            Renderer[] visRs = vis.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < visRs.Length; i++)
            {
                Renderer r = visRs[i];
                if (r == null)
                    continue;
                r.renderingLayerMask = mask;
                if (donor == null)
                    continue;
                r.lightProbeUsage = donor.lightProbeUsage;
                r.reflectionProbeUsage = donor.reflectionProbeUsage;
            }
        }

        private static void ApplyDiskNormal(Material mat, Texture2D nml, Material? baked)
        {
            Texture? bump = baked != null && baked.HasProperty("_BumpMap") ? baked.GetTexture("_BumpMap") : null;
            if (bump == null)
                bump = nml;
            if (bump == null)
                return;
            if (mat.HasProperty("_BumpMap"))
                mat.SetTexture("_BumpMap", bump);
            if (mat.HasProperty("_NormalMap"))
                mat.SetTexture("_NormalMap", bump);
            if (mat.HasProperty("_BumpScale"))
                mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
        }

        private static void CopyMap(Material? src, Material dst, string srcProp, string dstProp)
        {
            if (src == null || dst == null || !src.HasProperty(srcProp) || !dst.HasProperty(dstProp))
                return;
            Texture t = src.GetTexture(srcProp);
            if (t != null)
                dst.SetTexture(dstProp, t);
        }

        private static void KillEmission(Material mat)
        {
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", Color.black);
            if (mat.HasProperty("_EmissiveColor"))
                mat.SetColor("_EmissiveColor", Color.black);
            if (mat.HasProperty("_EmissionMap"))
                mat.SetTexture("_EmissionMap", null);
            mat.DisableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        }

        // Main slot 2: Материал.004 → Материал.003 disk stem. Empty = no extra try.
        private static string? MatFallback(string matName)
        {
            if (string.IsNullOrEmpty(matName))
                return null;
            string n = matName;
            int inst = n.LastIndexOf(" (Instance)", System.StringComparison.OrdinalIgnoreCase);
            if (inst > 0)
                n = n.Substring(0, inst);
            if (n.EndsWith(".004", System.StringComparison.Ordinal) ||
                n.EndsWith("_004", System.StringComparison.Ordinal))
                return n.Substring(0, n.Length - 4) + ".003";
            if (n.EndsWith("004", System.StringComparison.Ordinal))
                return n.Substring(0, n.Length - 3) + "003";
            return null;
        }

        private static Texture? PeekAlbedo(Material? mat)
        {
            if (mat == null)
                return null;
            if (mat.HasProperty("_BaseMap"))
            {
                Texture t = mat.GetTexture("_BaseMap");
                if (t != null)
                    return t;
            }
            if (mat.HasProperty("_MainTex"))
            {
                Texture t = mat.GetTexture("_MainTex");
                if (t != null)
                    return t;
            }
            return null;
        }

        private static void WriteAlbedo(Material mat, Texture tex)
        {
            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", tex);
                VisualShader.ResetSt(mat, "_BaseMap");
            }
            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", tex);
                VisualShader.ResetSt(mat, "_MainTex");
            }
            if (mat.HasProperty("_BaseColorMap"))
            {
                mat.SetTexture("_BaseColorMap", tex);
                VisualShader.ResetSt(mat, "_BaseColorMap");
            }
        }

        private static void ClearAlbedoMaps(Material mat)
        {
            if (mat.HasProperty("_BaseMap"))
                mat.SetTexture("_BaseMap", null);
            if (mat.HasProperty("_MainTex"))
                mat.SetTexture("_MainTex", null);
            if (mat.HasProperty("_BaseColorMap"))
                mat.SetTexture("_BaseColorMap", null);
        }
    }
}
