using UnityEngine;

namespace Heavypin.Runtime
{
    internal static class HeavypinAnim
    {
        // Hangar / rail: animator off so FileScale scale keys cannot inflate Cube fins.
        internal static void Park(Transform? vis)
        {
            if (vis == null)
                return;
            Animator[] animators = vis.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator a = animators[i];
                if (a == null)
                    continue;
                a.enabled = false;
                a.applyRootMotion = false;
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
                if (a == null)
                    continue;
                a.applyRootMotion = false;
                a.enabled = true;
                a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                for (int layer = 0; layer < a.layerCount; layer++)
                {
                    AnimatorStateInfo info = a.GetCurrentAnimatorStateInfo(layer);
                    a.Play(info.fullPathHash, layer, 0f);
                }
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
    }
}
