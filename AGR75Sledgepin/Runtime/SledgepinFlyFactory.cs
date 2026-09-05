using Sledgepin.Bootstrap;
using UnityEngine;

namespace Sledgepin.Runtime
{
    internal static class SledgepinFlyFactory
    {
        internal static GameObject? BindSharedShell(MissileDefinition? donor)
        {
            if (donor?.unitPrefab == null)
            {
                SledgepinPlugin.ModLog?.LogError("AGR-75 Sledgepin: no AGR-24 unitPrefab to share.");
                return null;
            }

            SledgepinMotors.LoadProfile();
            SledgepinPlugin.ModLog?.LogInfo(
                $"Sledgepin uses stock unitPrefab '{donor.unitPrefab.name}' jsonKey={donor.jsonKey}.");
            return donor.unitPrefab;
        }
    }
}
