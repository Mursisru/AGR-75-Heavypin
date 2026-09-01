using Heavypin.Bootstrap;
using UnityEngine;

namespace Heavypin.Runtime
{
    internal static class HeavypinAnim
    {
        internal static void Park(Transform? vis)
        {
            if (vis == null)
                return;

            ResetFinCubes(vis);

            Animator[] animators = vis.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator a = animators[i];
                if (a == null)
                    continue;
                a.applyRootMotion = false;
                a.speed = 0f;
                if (a.runtimeAnimatorController != null)
                {
                    a.Rebind();
                    a.Update(0f);
                    for (int layer = 0; layer < a.layerCount; layer++)
                    {
                        AnimatorStateInfo info = a.GetCurrentAnimatorStateInfo(layer);
                        if (info.fullPathHash != 0)
                            a.Play(info.fullPathHash, layer, 0f);
                    }
                    a.Update(0f);
                }
                a.enabled = false;
            }

            Animation[] legacy = vis.GetComponentsInChildren<Animation>(true);
            for (int i = 0; i < legacy.Length; i++)
            {
                Animation an = legacy[i];
                if (an == null)
                    continue;
                an.Stop();
                an.enabled = false;
            }
        }

        internal static void Play(Transform? vis)
        {
            if (vis == null)
                return;
            Animator[] animators = vis.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator a = animators[i];
                if (a == null || a.runtimeAnimatorController == null)
                    continue;
                a.applyRootMotion = false;
                a.speed = 1f;
                a.enabled = true;
                a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                for (int layer = 0; layer < a.layerCount; layer++)
                {
                    AnimatorStateInfo info = a.GetCurrentAnimatorStateInfo(layer);
                    if (info.fullPathHash != 0)
                        a.Play(info.fullPathHash, layer, 0f);
                }
                a.Update(0f);
            }

            Animation[] legacy = vis.GetComponentsInChildren<Animation>(true);
            for (int i = 0; i < legacy.Length; i++)
            {
                Animation an = legacy[i];
                if (an == null)
                    continue;
                an.enabled = true;
                an.Play();
            }
        }

        internal static void ParkMount(GameObject? mount)
        {
            if (mount == null)
                return;
            HeavypinLauncherRockets.ParkEmbedded(mount);
        }

        private static void ResetFinCubes(Transform vis)
        {
            Transform[] all = vis.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t == vis)
                    continue;
                string n = t.name;
                if (string.IsNullOrEmpty(n) || !n.StartsWith("Cube-", System.StringComparison.Ordinal))
                    continue;
                t.localPosition = Vector3.zero;
                t.localScale = Vector3.one;
            }
        }
    }
}
