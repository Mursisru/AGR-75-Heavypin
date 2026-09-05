using UnityEngine;

namespace Sledgepin.Runtime
{
    internal static class SledgepinCubeClosed
    {
        internal static void Apply(Transform? vis)
        {
            if (vis == null)
                return;
            SledgepinCubeDriver driver = vis.GetComponent<SledgepinCubeDriver>();
            if (driver == null)
                driver = vis.gameObject.AddComponent<SledgepinCubeDriver>();
            driver.CaptureBindIfNeeded();
            driver.StopClosed();
        }

        internal static Transform? FindExact(Transform root, string name)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && string.Equals(all[i].name, name, System.StringComparison.Ordinal))
                    return all[i];
            }
            return null;
        }
    }
}
