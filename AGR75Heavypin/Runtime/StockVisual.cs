using Heavypin.Bootstrap;
using UnityEngine;

namespace Heavypin.Runtime
{
    // Mesh that came from our nobp stamp. Kingpin parented under HeavypinRocket has none.
    internal sealed class HeavypinOurs : MonoBehaviour
    {
    }

    internal static class StockVisual
    {
        internal static void MarkOurs(Transform? vis)
        {
            if (vis == null)
                return;

            MeshRenderer[] meshes = vis.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < meshes.Length; i++)
            {
                MeshRenderer mr = meshes[i];
                if (mr == null || mr.GetComponent<HeavypinOurs>() != null)
                    continue;
                MeshFilter? mf = mr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh == null)
                    continue;
                mr.gameObject.AddComponent<HeavypinOurs>();
            }

            SkinnedMeshRenderer[] skins = vis.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skins.Length; i++)
            {
                SkinnedMeshRenderer skin = skins[i];
                if (skin == null || skin.GetComponent<HeavypinOurs>() != null)
                    continue;
                if (skin.sharedMesh == null)
                    continue;
                skin.gameObject.AddComponent<HeavypinOurs>();
            }
        }

        internal static bool IsOurs(Renderer? r) =>
            r != null && r.GetComponent<HeavypinOurs>() != null;

        // Walk unit root. Mute AGR-24 even if it sits under HeavypinRocket.
        internal static void Hide(GameObject? root)
        {
            if (root == null)
                return;

            Animator[] anims = root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < anims.Length; i++)
            {
                Animator a = anims[i];
                if (a == null || PrefabFactory.IsOurVisualRoot(a.transform))
                    continue;
                a.enabled = false;
                a.applyRootMotion = false;
            }

            LODGroup[] lods = root.GetComponentsInChildren<LODGroup>(true);
            for (int i = 0; i < lods.Length; i++)
            {
                LODGroup lod = lods[i];
                if (lod == null || PrefabFactory.IsOurVisualRoot(lod.transform))
                    continue;
                lod.enabled = false;
            }

            Renderer[] rs = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                Renderer r = rs[i];
                if (r == null || IsOurs(r))
                    continue;
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer)
                    continue;
                Mute(r);
            }
        }

        private static void Mute(Renderer r)
        {
            r.enabled = false;
            if (r is not MeshRenderer && r is not SkinnedMeshRenderer)
                return;
            MeshFilter? mf = r.GetComponent<MeshFilter>();
            if (mf != null)
                mf.sharedMesh = null;
            if (r is SkinnedMeshRenderer skin)
                skin.sharedMesh = null;
        }
    }
}
