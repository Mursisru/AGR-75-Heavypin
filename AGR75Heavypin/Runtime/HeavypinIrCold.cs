using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Heavypin.Runtime
{
    // After motor burnout strip IR sources so IR SAMs lose the coasting rocket.
    internal sealed class HeavypinIrCold : MonoBehaviour
    {
        private static readonly FieldInfo? MotorsField =
            typeof(Missile).GetField("motors", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? FuelMassField =
            typeof(Missile).GetNestedType("Motor", BindingFlags.NonPublic | BindingFlags.Public)
                ?.GetField("fuelMass", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        private static readonly FieldInfo? IrListField =
            typeof(Unit).GetField("IRSources", BindingFlags.Instance | BindingFlags.NonPublic);

        private Missile? _missile;
        private bool _cold;

        internal static void Ensure(Missile missile)
        {
            if (missile == null)
                return;
            if (missile.GetComponent<HeavypinIrCold>() == null)
                missile.gameObject.AddComponent<HeavypinIrCold>();
        }

        private void Awake()
        {
            _missile = GetComponent<Missile>();
        }

        private void FixedUpdate()
        {
            if (_cold || _missile == null)
                return;
            if (!MotorSpent(_missile))
                return;
            StripIr(_missile);
            _cold = true;
            HeavypinPlugin.ModLog?.LogInfo("Heavypin IR cold after burnout.");
        }

        private static bool MotorSpent(Missile missile)
        {
            if (MotorsField?.GetValue(missile) is not System.Array motors || motors.Length == 0)
                return false;
            object? motor = motors.GetValue(0);
            if (motor == null || FuelMassField == null)
                return false;
            return FuelMassField.GetValue(motor) is float fuel && fuel <= 0.01f;
        }

        private static void StripIr(Missile missile)
        {
            if (IrListField?.GetValue(missile) is List<IRSource> list)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    IRSource src = list[i];
                    if (src != null)
                        missile.RemoveIRSource(src);
                }
                list.Clear();
                return;
            }

            int guard = 0;
            while (missile.HasIRSignature() && guard++ < 16)
            {
                IRSource src = missile.GetIRSource();
                if (src == null)
                    break;
                missile.RemoveIRSource(src);
            }
        }
    }
}
