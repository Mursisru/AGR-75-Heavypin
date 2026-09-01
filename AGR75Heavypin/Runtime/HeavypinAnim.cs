using Heavypin;
using Heavypin.Blueprinter;
using UnityEngine;

namespace Heavypin.Runtime
{
    internal static class HeavypinAnim
    {
        internal static void Park(Transform? vis)
        {
            if (vis == null)
                return;
            HeavypinTag? tag = vis.GetComponentInParent<HeavypinTag>();
            if (tag != null && tag.FinsOpen)
                return;
            HeavypinOpening.PoseClosed(vis);
        }

        internal static void Play(Transform? vis)
        {
            if (vis == null)
                return;
            HeavypinOpening.Play(vis);
        }

        internal static void PlayFly(Missile? missile)
        {
            if (missile == null)
                return;

            HeavypinSpawnGate.Ensure(missile);
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
                HeavypinPlugin.ModLog?.LogWarning("HeavypinAnim.PlayFly: HeavypinRocket missing.");
                return;
            }

            HeavypinOpening.Play(vis);
            HeavypinPlugin.ModLog?.LogInfo($"HeavypinOpening deploy vis='{vis.name}'");
        }

        internal static void ParkMount(GameObject? mount)
        {
            if (mount == null)
                return;
            HeavypinLauncherRockets.ParkEmbedded(mount);
        }
    }
}
