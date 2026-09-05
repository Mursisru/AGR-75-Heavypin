using Sledgepin.Bootstrap;
using UnityEngine;

namespace Sledgepin.Runtime
{
    // Mesh that came from our nobp stamp. Kingpin parented under SledgepinRocket has none.
    internal sealed class SledgepinOurs : MonoBehaviour
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
                if (mr == null || mr.GetComponent<SledgepinOurs>() != null)
                    continue;
                MeshFilter? mf = mr.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh == null)
                    continue;
                mr.gameObject.AddComponent<SledgepinOurs>();
            }

            SkinnedMeshRenderer[] skins = vis.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skins.Length; i++)
            {
                SkinnedMeshRenderer skin = skins[i];
                if (skin == null || skin.GetComponent<SledgepinOurs>() != null)
                    continue;
                if (skin.sharedMesh == null)
                    continue;
                skin.gameObject.AddComponent<SledgepinOurs>();
            }
        }

        internal static bool IsOurs(Renderer? r) =>
            r != null && r.GetComponent<SledgepinOurs>() != null;

        // Walk unit root. Mute AGR-24 even if it sits under SledgepinRocket.
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

            MuteStockFx(root);
        }

        private static void MuteStockFx(GameObject root)
        {
            ParticleSystem[] ps = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < ps.Length; i++)
            {
                ParticleSystem p = ps[i];
                if (p == null || IsOursFxRoot(p.transform))
                    continue;
                p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                p.gameObject.SetActive(false);
            }

            TrailEmitter[] trails = root.GetComponentsInChildren<TrailEmitter>(true);
            for (int i = 0; i < trails.Length; i++)
            {
                TrailEmitter te = trails[i];
                if (te == null || IsOursFxRoot(te.transform))
                    continue;
                te.StopTrail();
                te.enabled = false;
            }
        }

        private static bool IsOursFxRoot(Transform t)
        {
            while (t != null)
            {
                string n = t.name;
                if (n == "SledgepinExhaust" || n == "SledgepinTrail" || n == "SledgepinAudio" || n == "SledgepinLight")
                    return true;
                t = t.parent;
            }
            return false;
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
