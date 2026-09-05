using Sledgepin;
using UnityEngine;

namespace Sledgepin.Runtime
{
    internal static class SledgepinOpening
    {
        internal static void PoseClosed(Transform? visual)
        {
            if (visual == null)
                return;
            DisableAnimations(visual);
            SledgepinCubeClosed.Apply(visual);
        }

        internal static void Play(Transform? visual)
        {
            if (visual == null)
                return;
            SledgepinTag? tag = visual.GetComponentInParent<SledgepinTag>();
            if (tag != null)
                tag.FinsOpen = true;
            DisableAnimations(visual);
            SledgepinCubeDriver driver = visual.GetComponent<SledgepinCubeDriver>();
            if (driver == null)
                driver = visual.gameObject.AddComponent<SledgepinCubeDriver>();
            driver.Begin();
        }

        internal static bool IsFinPart(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            return name!.StartsWith("Cube-", System.StringComparison.Ordinal);
        }

        private static void DisableAnimations(Transform visual)
        {
            Animation[] anims = visual.GetComponentsInChildren<Animation>(true);
            for (int i = 0; i < anims.Length; i++)
            {
                Animation a = anims[i];
                if (a == null)
                    continue;
                a.playAutomatically = false;
                a.Stop();
                a.enabled = false;
            }

            Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator an = animators[i];
                if (an == null)
                    continue;
                an.speed = 0f;
                an.enabled = false;
            }
        }
    }
}
