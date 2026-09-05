using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Sledgepin.Runtime
{
    internal static class SledgepinEncyclopediaStats
    {
        private static readonly FieldInfo? RangeTextField =
            AccessTools.Field(typeof(EncyclopediaBrowser), "range");
        private static readonly FieldInfo? BurnTextField =
            AccessTools.Field(typeof(EncyclopediaBrowser), "burnTime");
        private static readonly FieldInfo? DeltaVTextField =
            AccessTools.Field(typeof(EncyclopediaBrowser), "deltaV");
        private static readonly FieldInfo? TopSpeedTextField =
            AccessTools.Field(typeof(EncyclopediaBrowser), "topSpeed");

        internal static void ApplyMissilePanels(EncyclopediaBrowser browser)
        {
            if (browser == null)
                return;
            float rangeM = SledgepinCalcProxy.EncyclopediaRangeM;
            float burnS = SledgepinCalcProxy.EncyclopediaBurnS;
            float deltaVMps = SledgepinCalcProxy.EncyclopediaDeltaVMps;
            if (rangeM < 1000f)
                rangeM = SledgepinConstants.DesignRangeM;
            if (burnS < 0.5f)
                burnS = SledgepinMotors.AppliedBurnS;
            SetText(RangeTextField, browser, UnitConverter.DistanceReading(rangeM));
            SetText(BurnTextField, browser, string.Format("{0:F1}s", burnS));
            SetText(DeltaVTextField, browser, UnitConverter.SpeedReading(deltaVMps));
            if (SledgepinMotors.AppliedTopSpeedMps > 1f)
                SetText(TopSpeedTextField, browser, UnitConverter.SpeedReading(SledgepinMotors.AppliedTopSpeedMps));
        }

        internal static void ApplyTargetRequirements(WeaponInfo info)
        {
            if (info == null)
                return;
            TargetRequirements tr = info.targetRequirements;
            tr.maxRange = SledgepinCalcProxy.EncyclopediaRangeM > 1000f
                ? SledgepinCalcProxy.EncyclopediaRangeM
                : SledgepinConstants.DesignRangeM;
            tr.minRange = SledgepinConstants.EncyclopediaMinRangeM;
            info.targetRequirements = tr;
        }

        private static void SetText(FieldInfo? field, EncyclopediaBrowser browser, string value)
        {
            object? tmp = field?.GetValue(browser);
            if (tmp == null)
                return;
            PropertyInfo? p = tmp.GetType().GetProperty("text");
            p?.SetValue(tmp, value);
        }
    }
}
