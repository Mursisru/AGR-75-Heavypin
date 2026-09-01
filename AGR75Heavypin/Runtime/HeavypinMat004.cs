using System;
using UnityEngine;

namespace Heavypin.Runtime
{
    // Main slot 2 — Blender Principled transmission/glass (Материал.004).
    internal static class HeavypinMat004
    {
        internal static bool IsName(string? raw)
        {
            if (string.IsNullOrEmpty(raw))
                return false;
            string n = raw!;
            int inst = n.LastIndexOf(" (Instance)", StringComparison.OrdinalIgnoreCase);
            if (inst > 0)
                n = n.Substring(0, inst);
            int hp = n.LastIndexOf("_hp", StringComparison.OrdinalIgnoreCase);
            if (hp > 0)
                n = n.Substring(0, hp);
            // Mesh names like Cube-3-001-002-003-004 must not match.
            if (n.StartsWith("Cube-", StringComparison.OrdinalIgnoreCase))
                return false;
            return n.EndsWith(".004", StringComparison.Ordinal) ||
                   n.EndsWith("_004", StringComparison.Ordinal);
        }

        // Main body slot 2 in FBX (Материал.004 glass), even if Unity renamed the slot.
        internal static bool IsMainGlassSlot(string? meshName, int slotIndex, int slotCount) =>
            slotCount >= 2 &&
            slotIndex == 1 &&
            string.Equals(meshName, "Main", StringComparison.OrdinalIgnoreCase);

        internal static void ApplyGlass(Material mat, Texture? albedo)
        {
            if (mat == null)
                return;

            if (albedo == null)
            {
                albedo = HeavypinMaps.Albedo("Материал.004");
                if (albedo == null)
                    albedo = HeavypinMaps.Albedo("Material.004");
            }

            VisualShader.ApplyTransmissionGlass(mat);
            if (albedo == null)
                return;

            if (mat.HasProperty("_BaseMap"))
            {
                mat.SetTexture("_BaseMap", albedo);
                VisualShader.ResetSt(mat, "_BaseMap");
            }
            if (mat.HasProperty("_MainTex"))
            {
                mat.SetTexture("_MainTex", albedo);
                VisualShader.ResetSt(mat, "_MainTex");
            }
            if (mat.HasProperty("_BaseColorMap"))
            {
                mat.SetTexture("_BaseColorMap", albedo);
                VisualShader.ResetSt(mat, "_BaseColorMap");
            }
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.white);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", Color.white);
        }
    }
}
