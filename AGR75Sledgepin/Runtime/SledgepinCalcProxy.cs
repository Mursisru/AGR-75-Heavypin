using Sledgepin.Bootstrap;
using UnityEngine;

namespace Sledgepin.Runtime
{
    internal static class SledgepinCalcProxy
    {
        private static Missile? _missile;

        internal static float EncyclopediaRangeM { get; private set; }
        internal static float EncyclopediaDeltaVMps { get; private set; }
        internal static float EncyclopediaBurnS { get; private set; }

        internal static void Init(Encyclopedia enc)
        {
            if (_missile != null || enc == null)
                return;

            MissileDefinition? donor = AgrDonor.FindAgr24Missile(enc);
            if (donor?.unitPrefab == null)
            {
                SledgepinPlugin.ModLog?.LogWarning("SledgepinCalcProxy: no AGR-24 donor.");
                return;
            }

            GameObject go = Object.Instantiate(donor.unitPrefab);
            go.name = "SledgepinCalcProxy";
            go.SetActive(false);
            Object.DontDestroyOnLoad(go);
            NetworkPrefabPrep.PrepareTemplate(go);

            _missile = go.GetComponent<Missile>() ?? go.GetComponentInChildren<Missile>(true);
            if (_missile == null)
            {
                Object.Destroy(go);
                SledgepinPlugin.ModLog?.LogWarning("SledgepinCalcProxy: no Missile component.");
                return;
            }

            SledgepinMotors.LoadProfile();
            SledgepinAero.CaptureBase(_missile);
            SledgepinMotors.Apply(_missile);
            SledgepinAero.Apply(_missile);
            CacheEncyclopediaStats();
            if (EncyclopediaRangeM < SledgepinConstants.DesignRangeM * 0.5f)
                EncyclopediaRangeM = SledgepinConstants.DesignRangeM;
            SledgepinPlugin.ModLog?.LogInfo(
                $"SledgepinCalcProxy range={EncyclopediaRangeM:F0}m burn={EncyclopediaBurnS:F1}s dV={EncyclopediaDeltaVMps:F0} thrust={SledgepinMotors.AppliedThrustN:F0} fuel={SledgepinMotors.AppliedFuelKg:F1}");
        }

        private static void CacheEncyclopediaStats()
        {
            if (_missile == null)
                return;
            EncyclopediaBurnS = _missile.GetTotalBurnTime();
            EncyclopediaDeltaVMps = _missile.CalcDeltaV();
            float nez;
            EncyclopediaRangeM = _missile.CalcRange(
                SledgepinConstants.CalcRestLaunchSpeedMps,
                SledgepinConstants.CalcRestLaunchAltM,
                SledgepinConstants.CalcRestTargetAltM,
                SledgepinConstants.CalcRestTargetDistM,
                0f,
                out nez);
        }

        internal static float CalcRange(
            float launchSpeed,
            float launchAltitude,
            float targetAltitude,
            float targetDist,
            float targetRelativeSpeed,
            out float noEscapeDistance)
        {
            if (_missile != null)
            {
                return _missile.CalcRange(
                    launchSpeed, launchAltitude, targetAltitude, targetDist, targetRelativeSpeed, out noEscapeDistance);
            }
            noEscapeDistance = SledgepinConstants.DesignRangeM * 0.65f;
            return SledgepinConstants.DesignRangeM;
        }
    }
}
