using Heavypin.Bootstrap;
using UnityEngine;

namespace Heavypin.Runtime
{
    internal static class HeavypinCalcProxy
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
                HeavypinPlugin.ModLog?.LogWarning("HeavypinCalcProxy: no AGR-24 donor.");
                return;
            }

            GameObject go = Object.Instantiate(donor.unitPrefab);
            go.name = "HeavypinCalcProxy";
            go.SetActive(false);
            Object.DontDestroyOnLoad(go);
            NetworkPrefabPrep.PrepareTemplate(go);

            _missile = go.GetComponent<Missile>() ?? go.GetComponentInChildren<Missile>(true);
            if (_missile == null)
            {
                Object.Destroy(go);
                HeavypinPlugin.ModLog?.LogWarning("HeavypinCalcProxy: no Missile component.");
                return;
            }

            HeavypinMotors.LoadProfile();
            HeavypinAero.CaptureBase(_missile);
            HeavypinMotors.Apply(_missile);
            HeavypinAero.Apply(_missile);
            CacheEncyclopediaStats();
            if (EncyclopediaRangeM < HeavypinConstants.DesignRangeM * 0.5f)
                EncyclopediaRangeM = HeavypinConstants.DesignRangeM;
            HeavypinPlugin.ModLog?.LogInfo(
                $"HeavypinCalcProxy range={EncyclopediaRangeM:F0}m burn={EncyclopediaBurnS:F1}s dV={EncyclopediaDeltaVMps:F0} thrust={HeavypinMotors.AppliedThrustN:F0} fuel={HeavypinMotors.AppliedFuelKg:F1}");
        }

        private static void CacheEncyclopediaStats()
        {
            if (_missile == null)
                return;
            EncyclopediaBurnS = _missile.GetTotalBurnTime();
            EncyclopediaDeltaVMps = _missile.CalcDeltaV();
            float nez;
            EncyclopediaRangeM = _missile.CalcRange(
                HeavypinConstants.CalcRestLaunchSpeedMps,
                HeavypinConstants.CalcRestLaunchAltM,
                HeavypinConstants.CalcRestTargetAltM,
                HeavypinConstants.CalcRestTargetDistM,
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
            noEscapeDistance = HeavypinConstants.DesignRangeM * 0.65f;
            return HeavypinConstants.DesignRangeM;
        }
    }
}
