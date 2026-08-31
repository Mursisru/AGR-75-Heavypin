using Heavypin.Bootstrap;
using UnityEngine;

namespace Heavypin.Runtime
{
    internal static class HeavypinFlyFactory
    {
        internal static GameObject? BindSharedShell(MissileDefinition? donor)
        {
            if (donor?.unitPrefab == null)
            {
                HeavypinPlugin.ModLog?.LogError("AGR-75 Heavypin: no AGR-24 unitPrefab to share.");
                return null;
            }

            HeavypinMotors.LoadProfile();
            HeavypinPlugin.ModLog?.LogInfo(
                $"Heavypin uses stock unitPrefab '{donor.unitPrefab.name}' jsonKey={donor.jsonKey}.");
            return donor.unitPrefab;
        }
    }
}
