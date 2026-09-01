using UnityEngine;

namespace Heavypin.Runtime
{
    internal static class VisualShader
    {
        private static Shader? _lit;
        private static Material? _template;

        internal static void PrimeFrom(GameObject? sampleRoot)
        {
            if (sampleRoot == null)
                return;
            MeshRenderer[] rs = sampleRoot.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                MeshRenderer r = rs[i];
                if (r == null)
                    continue;
                Material? mat = r.sharedMaterial;
                if (mat?.shader == null || !IsUrpMeshLit(mat.shader.name))
                    continue;
                _template = mat;
                _lit = mat.shader;
                HeavypinPlugin.ModLog?.LogInfo($"VisualShader: primed '{_lit.name}' from '{sampleRoot.name}/{r.name}'");
                return;
            }
        }

        internal static Material Make(string name, float cull)
        {
            Material mat = _template != null ? new Material(_template) : new Material(Resolve());
            mat.name = name;
            StripInheritedMaps(mat);
            ForceOpaqueLit(mat, cull);
            return mat;
        }

        internal static Material MakeGlass(string name)
        {
            Material mat = _template != null ? new Material(_template) : new Material(Resolve());
            mat.name = name;
            StripInheritedMaps(mat);
            return mat;
        }

        // Runtime: keep nobp bake as-is. Standard → URP Lit copies baked maps/tints/mode only.
        internal static Material InstanceBaked(Material? src)
        {
            if (src == null)
                return new Material(Resolve());
            if (src.shader != null && IsUrpMeshLit(src.shader.name))
            {
                Material urpClone = new Material(src);
                WipeEmission(urpClone);
                return urpClone;
            }
            if (src.shader != null && IsStandardShader(src.shader.name))
                return RemapStandardToUrp(src);
            Material clone = new Material(src);
            WipeEmission(clone);
            return clone;
        }

        private static bool IsStandardShader(string? name)
        {
            if (name is not { Length: > 0 } n)
                return false;
            return string.Equals(n, "Standard", System.StringComparison.OrdinalIgnoreCase) ||
                   n.IndexOf("Legacy Shaders/", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static Material RemapStandardToUrp(Material src)
        {
            Material mat = new Material(Resolve());
            mat.name = src.name;

            CopyTex(src, mat, "_MainTex", "_BaseMap");
            CopyTex(src, mat, "_MainTex", "_MainTex");
            CopyTex(src, mat, "_BumpMap", "_BumpMap");
            CopyTex(src, mat, "_BumpMap", "_NormalMap");
            CopyTex(src, mat, "_MetallicGlossMap", "_MetallicGlossMap");
            CopyTex(src, mat, "_OcclusionMap", "_OcclusionMap");
            ResetSt(mat, "_BaseMap");
            ResetSt(mat, "_MainTex");

            Color tint = src.HasProperty("_Color") ? src.GetColor("_Color") : Color.white;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", tint);

            if (src.HasProperty("_Metallic") && mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", src.GetFloat("_Metallic"));
            if (src.HasProperty("_Glossiness"))
            {
                float g = src.GetFloat("_Glossiness");
                if (mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", g);
                if (mat.HasProperty("_Glossiness"))
                    mat.SetFloat("_Glossiness", g);
            }
            if (src.HasProperty("_BumpScale") && mat.HasProperty("_BumpScale"))
                mat.SetFloat("_BumpScale", src.GetFloat("_BumpScale"));

            bool transparent = src.HasProperty("_Mode") && src.GetFloat("_Mode") >= 2.5f;
            if (!transparent && src.renderQueue >= 3000)
                transparent = true;

            if (transparent)
            {
                if (mat.HasProperty("_Surface"))
                    mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_Blend"))
                    mat.SetFloat("_Blend", 0f);
                if (mat.HasProperty("_AlphaClip"))
                    mat.SetFloat("_AlphaClip", 0f);
                if (mat.HasProperty("_SrcBlend"))
                    mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                if (mat.HasProperty("_DstBlend"))
                    mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (mat.HasProperty("_ZWrite"))
                    mat.SetFloat("_ZWrite", 0f);
                if (mat.HasProperty("_Cull"))
                    mat.SetFloat("_Cull", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = src.renderQueue > 0 ? src.renderQueue : 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_EMISSION");
            }
            else
            {
                if (mat.HasProperty("_Surface"))
                    mat.SetFloat("_Surface", 0f);
                if (mat.HasProperty("_ZWrite"))
                    mat.SetFloat("_ZWrite", 1f);
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.renderQueue = src.renderQueue > 0 ? src.renderQueue : 2000;
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            if (mat.GetTexture("_BumpMap") != null || mat.GetTexture("_NormalMap") != null)
                mat.EnableKeyword("_NORMALMAP");

            WipeEmission(mat);
            return mat;
        }

        private static void CopyTex(Material src, Material dst, string srcProp, string dstProp)
        {
            if (!src.HasProperty(srcProp) || !dst.HasProperty(dstProp))
                return;
            Texture? t = src.GetTexture(srcProp);
            if (t != null)
                dst.SetTexture(dstProp, t);
        }

        private static void WipeEmission(Material mat)
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

        // Материал.004 — URP transparent (alpha from dedicated Color.png).
        internal static void ApplyTransmissionGlass(Material mat)
        {
            if (mat == null)
                return;
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);
            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_AlphaClip"))
                mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_SrcBlend"))
                mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend"))
                mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite"))
                mat.SetFloat("_ZWrite", 0f);
            if (mat.HasProperty("_Cull"))
                mat.SetFloat("_Cull", 0f);
            if (mat.HasProperty("_CullMode"))
                mat.SetFloat("_CullMode", 0f);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.92f);
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", 0.92f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_EMISSION");
        }

        internal static void StripInheritedMaps(Material mat)
        {
            if (mat == null)
                return;
            string[] maps =
            {
                "_BaseMap", "_MainTex", "_BaseColorMap",
                "_BumpMap", "_NormalMap", "_BentNormalMap",
                "_MetallicGlossMap", "_MaskMap",
                "_OcclusionMap", "_DetailAlbedoMap", "_DetailNormalMap",
                "_DetailMask", "_EmissionMap", "_EmissiveColorMap",
                "_ParallaxMap", "_HeightMap", "_SpecGlossMap"
            };
            for (int i = 0; i < maps.Length; i++)
            {
                if (mat.HasProperty(maps[i]))
                    mat.SetTexture(maps[i], null);
            }
        }

        private static Shader Resolve()
        {
            if (_lit != null && IsUrpMeshLit(_lit.name))
                return _lit;

            MeshRenderer[] scene = Object.FindObjectsOfType<MeshRenderer>();
            for (int i = 0; i < scene.Length; i++)
            {
                MeshRenderer r = scene[i];
                if (r == null)
                    continue;
                Material? mat = r.sharedMaterial;
                if (mat?.shader == null || !IsUrpMeshLit(mat.shader.name))
                    continue;
                _template = mat;
                _lit = mat.shader;
                return _lit;
            }

            Shader? found = Shader.Find("Universal Render Pipeline/Lit") ??
                            Shader.Find("Universal Render Pipeline/Simple Lit") ??
                            Shader.Find("Lit");
            if (found != null && IsUrpMeshLit(found.name))
            {
                _lit = found;
                return _lit;
            }

            _lit = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse") ?? Shader.Find("Unlit/Texture");
            if (_lit == null)
                throw new System.InvalidOperationException("VisualShader: no usable shader");
            return _lit;
        }

        internal static bool IsUrpMeshLit(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            string n = name!;
            if (n.IndexOf("Error", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (n.IndexOf("Hidden", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (n.IndexOf("UI", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (n.IndexOf("Sprite", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (n.IndexOf("Particle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            if (n.IndexOf("Universal Render Pipeline/Lit", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (n.IndexOf("Universal Render Pipeline/Simple Lit", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            return n.Equals("Lit", System.StringComparison.OrdinalIgnoreCase);
        }

        internal static void ForceOpaqueLit(Material mat, float cull)
        {
            if (mat == null)
                return;
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 0f);
            if (mat.HasProperty("_Blend"))
                mat.SetFloat("_Blend", 0f);
            if (mat.HasProperty("_AlphaClip"))
                mat.SetFloat("_AlphaClip", 0f);
            if (mat.HasProperty("_SrcBlend"))
                mat.SetFloat("_SrcBlend", 1f);
            if (mat.HasProperty("_DstBlend"))
                mat.SetFloat("_DstBlend", 0f);
            if (mat.HasProperty("_ZWrite"))
                mat.SetFloat("_ZWrite", 1f);
            if (mat.HasProperty("_Cull"))
                mat.SetFloat("_Cull", cull);
            if (mat.HasProperty("_CullMode"))
                mat.SetFloat("_CullMode", cull);
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = 2000;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
            ResetSt(mat, "_BaseMap");
            ResetSt(mat, "_MainTex");
            ResetSt(mat, "_BaseColorMap");
        }

        internal static void ResetSt(Material mat, string prop)
        {
            if (mat == null || !mat.HasProperty(prop))
                return;
            mat.SetTextureScale(prop, Vector2.one);
            mat.SetTextureOffset(prop, Vector2.zero);
        }
    }
}
