using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Heavypin.Runtime
{
    internal static class HeavypinEncyclopediaStats
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
            float rangeM = HeavypinCalcProxy.EncyclopediaRangeM;
            float burnS = HeavypinCalcProxy.EncyclopediaBurnS;
            float deltaVMps = HeavypinCalcProxy.EncyclopediaDeltaVMps;
            if (rangeM < 1000f)
                rangeM = HeavypinConstants.DesignRangeM;
            if (burnS < 0.5f)
                burnS = HeavypinMotors.AppliedBurnS;
            SetText(RangeTextField, browser, UnitConverter.DistanceReading(rangeM));
            SetText(BurnTextField, browser, string.Format("{0:F1}s", burnS));
            SetText(DeltaVTextField, browser, UnitConverter.SpeedReading(deltaVMps));
            if (HeavypinMotors.AppliedTopSpeedMps > 1f)
                SetText(TopSpeedTextField, browser, UnitConverter.SpeedReading(HeavypinMotors.AppliedTopSpeedMps));
        }

        internal static void ApplyTargetRequirements(WeaponInfo info)
        {
            if (info == null)
                return;
            TargetRequirements tr = info.targetRequirements;
            tr.maxRange = HeavypinCalcProxy.EncyclopediaRangeM > 1000f
                ? HeavypinCalcProxy.EncyclopediaRangeM
                : HeavypinConstants.DesignRangeM;
            tr.minRange = HeavypinConstants.EncyclopediaMinRangeM;
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
