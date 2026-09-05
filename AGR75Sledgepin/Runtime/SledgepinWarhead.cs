using System.Reflection;
using UnityEngine;

namespace Sledgepin.Runtime
{
    // blastYield → vanilla BlastFrag. pierceDamage → ArmorPenetrate.
    // Hangar AP/HE must NOT read shared AGR-24 unitPrefab — override TMP / WeaponInfo.
    internal static class SledgepinWarhead
    {
        private static readonly FieldInfo? BlastYieldField =
            typeof(Missile).GetField("blastYield", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? PierceDamageField =
            typeof(Missile).GetField("pierceDamage", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static float PierceDamage => SledgepinConstants.PierceDamage;
        internal static float ArmorTier => SledgepinConstants.ArmorTierEffectiveness;

        internal static void Apply(Missile missile)
        {
            if (missile == null)
                return;
            BlastYieldField?.SetValue(missile, SledgepinConstants.BlastYieldKg);
            PierceDamageField?.SetValue(missile, PierceDamage);
        }

        internal static void ApplyInfo(WeaponInfo info)
        {
            if (info == null)
                return;
            info.blastDamage = SledgepinConstants.BlastYieldKg;
            info.pierceDamage = PierceDamage;
            info.armorTierEffectiveness = ArmorTier;
        }
    }
}
