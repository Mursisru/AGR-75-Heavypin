using System.Reflection;
using UnityEngine;

namespace Heavypin.Runtime
{
    internal static class HeavypinMountVisual
    {
        private static readonly FieldInfo? DeployPsField =
            typeof(MountedMissile).GetField("deployParticles", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static void HideFired(MountedMissile? mount)
        {
            if (mount == null)
                return;
            StopDeployFx(mount);
            SilenceSlotFx(mount.gameObject);
            HeavypinLauncherRockets.HideEmbedded(mount);
        }

        internal static void Restore(MountedMissile? mount)
        {
            if (mount == null)
                return;
            HeavypinLauncherRockets.RestoreEmbedded(mount);
        }

        private static void StopDeployFx(MountedMissile mount)
        {
            if (DeployPsField?.GetValue(mount) is not ParticleSystem ps || ps == null)
                return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);
        }

        private static void SilenceSlotFx(GameObject host)
        {
            if (host == null)
                return;
            TrailEmitter[] trails = host.GetComponentsInChildren<TrailEmitter>(true);
            for (int i = 0; i < trails.Length; i++)
            {
                TrailEmitter te = trails[i];
                if (te == null)
                    continue;
                te.StopTrail();
                te.enabled = false;
            }
            ParticleSystem[] psArr = host.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < psArr.Length; i++)
            {
                ParticleSystem ps = psArr[i];
                if (ps == null)
                    continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }
        }
    }
}
