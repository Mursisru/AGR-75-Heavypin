using System.Collections;
using System.Reflection;
using HarmonyLib;
using Heavypin.Runtime;
using UnityEngine;

namespace Heavypin.Patches
{
    [HarmonyPatch(typeof(HUDLaserGuidedState), "CalcWeaponRange")]
    internal static class HeavypinLaserHudRangePatch
    {
        private static readonly FieldInfo? WeaponInfoField =
            AccessTools.Field(typeof(HUDLaserGuidedState), "weaponInfo");
        private static readonly FieldInfo? AircraftField =
            AccessTools.Field(typeof(HUDLaserGuidedState), "aircraft");
        private static readonly FieldInfo? KnownPosField =
            AccessTools.Field(typeof(HUDLaserGuidedState), "knownPos");
        private static readonly FieldInfo? MaxRangeField =
            AccessTools.Field(typeof(HUDLaserGuidedState), "maxRange");
        private static readonly FieldInfo? LastCalcField =
            AccessTools.Field(typeof(HUDLaserGuidedState), "lastWeaponRangeCalc");
        private static readonly FieldInfo? TargetListField =
            AccessTools.Field(typeof(HUDLaserGuidedState), "targetList");
        private static readonly FieldInfo? TargetDistField =
            AccessTools.Field(typeof(HUDLaserGuidedState), "targetDist");

        private static bool Prefix(HUDLaserGuidedState __instance)
        {
            if (WeaponInfoField?.GetValue(__instance) is not WeaponInfo wi || !HeavypinBootstrap.IsOurInfo(wi))
                return true;
            if (TargetListField?.GetValue(__instance) is not IList list || list.Count == 0)
                return false;
            float last = LastCalcField?.GetValue(__instance) is float l ? l : 0f;
            if (last > 0f && Time.timeSinceLevelLoad - last < 1f)
                return false;
            if (AircraftField?.GetValue(__instance) is not Aircraft ac || ac == null)
                return true;

            float tgtDist = TargetDistField?.GetValue(__instance) is float td ? td : 0f;
            float tgtAlt = ac.GlobalPosition().y;
            if (KnownPosField?.GetValue(__instance) is GlobalPosition kp)
            {
                tgtAlt = kp.y;
                tgtDist = FastMath.Distance(kp, ac.GlobalPosition());
            }
            else if (list[0] is Unit u && u != null && ac.NetworkHQ.TryGetKnownPosition(u, out GlobalPosition gp))
            {
                tgtAlt = gp.y;
                tgtDist = FastMath.Distance(gp, ac.GlobalPosition());
            }

            float range = HeavypinCalcProxy.CalcRange(
                ac.speed, ac.GlobalPosition().y, tgtAlt, tgtDist, 0f, out _);
            MaxRangeField?.SetValue(__instance, range);
            LastCalcField?.SetValue(__instance, Time.timeSinceLevelLoad);
            return false;
        }
    }
}
