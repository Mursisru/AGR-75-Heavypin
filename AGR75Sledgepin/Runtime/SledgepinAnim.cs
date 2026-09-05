using Sledgepin;
using Sledgepin.Blueprinter;
using UnityEngine;

namespace Sledgepin.Runtime
{
    internal static class SledgepinAnim
    {
        internal static void Park(Transform? vis)
        {
            if (vis == null)
                return;
            SledgepinTag? tag = vis.GetComponentInParent<SledgepinTag>();
            if (tag != null && tag.FinsOpen)
                return;
            SledgepinOpening.PoseClosed(vis);
        }

        internal static void Play(Transform? vis)
        {
            if (vis == null)
                return;
            SledgepinOpening.Play(vis);
        }

        internal static void PlayFly(Missile? missile)
        {
            if (missile == null)
                return;

            SledgepinSpawnGate.Ensure(missile);
            Transform? vis = VisualStamp.FindRocket(missile.transform);
            if (vis == null)
            {
                NobpContent.TryLoad();
                if (NobpContent.RocketPrefab != null)
                    VisualStamp.StampRocket(missile.gameObject, NobpContent.RocketPrefab);
                vis = VisualStamp.FindRocket(missile.transform);
            }

            if (vis == null)
            {
                SledgepinPlugin.ModLog?.LogWarning("SledgepinAnim.PlayFly: SledgepinRocket missing.");
                return;
            }

            SledgepinOpening.Play(vis);
            SledgepinPlugin.ModLog?.LogInfo($"SledgepinOpening deploy vis='{vis.name}'");
        }

        internal static void ParkMount(GameObject? mount)
        {
            if (mount == null)
                return;
            SledgepinLauncherRockets.ParkEmbedded(mount);
        }
    }
}
