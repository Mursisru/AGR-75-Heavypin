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

        // flyRocket: clone launcher embedded Rocket slots (Materials/Launcher bake), not Rocket prefab bake.
        internal static void ApplyFbxLook(GameObject root, bool flyRocket = false)
        {
            if (root == null)
                return;
            if (flyRocket)
                HeavypinMaterialDonor.Ensure();
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

                    if (flyRocket && HeavypinMaterialDonor.TryClone(matName, out Material? donated) && donated != null)
                    {
                        dst[m] = donated;
                        n++;
                        continue;
                    }

                    Material? bakeRef = flyRocket
                        ? HeavypinMaterialDonor.GetBaked(matName) ?? old
                        : old;
                    bool isGlass = HeavypinMat004.IsName(bakeRef?.name ?? matName) ||
                                   HeavypinMat004.IsMainGlassSlot(r.gameObject.name, m, slots);

                    if (isGlass)
                    {
                        Material mat = VisualShader.MakeGlass(matName + "_hp");
                        Texture? albedo = HeavypinMaps.Albedo(matName) ?? PeekAlbedo(bakeRef);
                        if (albedo == null)
                            albedo = HeavypinMaps.Albedo("Материал.004");
                        HeavypinMat004.ApplyGlass(mat, albedo);
                        ApplyNormal(mat, matName, bakeRef);
                        KillEmission(mat);
                        HeavypinLook.ApplyFromBaked(mat, bakeRef, albedo != null);
                        dst[m] = mat;
                        n++;
                        continue;
                    }

                    Material opaque = VisualShader.Make(matName + "_hp", cull: 0f);
                    Texture? alb = HeavypinMaps.Albedo(matName) ?? PeekAlbedo(bakeRef);
                    bool albedoOwns = alb != null;
                    if (alb != null)
                        WriteAlbedo(opaque, alb);
                    else
                        ClearAlbedoMaps(opaque);
                    ApplyNormal(opaque, matName, bakeRef);
                    KillEmission(opaque);
                    HeavypinLook.ApplyFromBaked(opaque, bakeRef, albedoOwns);
                    dst[m] = opaque;
                    n++;
                }
                r.sharedMaterials = dst;
                r.enabled = true;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                r.receiveShadows = true;
            }
            HeavypinPlugin.ModLog?.LogInfo($"VisualMaterials baked '{root.name}' fly={flyRocket} slots={n}");
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

        private static void ApplyNormal(Material mat, string matName, Material? baked)
        {
            Texture2D? nml = HeavypinMaps.Normal(matName);
            if (nml != null)
            {
                Texture? bump = nml;
                if (mat.HasProperty("_BumpMap"))
                    mat.SetTexture("_BumpMap", bump);
                if (mat.HasProperty("_NormalMap"))
                    mat.SetTexture("_NormalMap", bump);
                if (mat.HasProperty("_BumpScale"))
                    mat.SetFloat("_BumpScale", 1f);
                mat.EnableKeyword("_NORMALMAP");
                return;
            }
            CopyMap(baked, mat, "_BumpMap", "_BumpMap");
            CopyMap(baked, mat, "_BumpMap", "_NormalMap");
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
