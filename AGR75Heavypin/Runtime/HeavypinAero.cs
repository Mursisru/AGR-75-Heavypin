using System.Reflection;
using UnityEngine;

namespace Heavypin.Runtime
{
    // finArea / currentFinArea drive lift+drag in Missile.ApplyAero (shared AGR-24 shell).
    internal static class HeavypinAero
    {
        private const float ClosedFinAreaRatio = 0.1f;

        private static readonly FieldInfo? FinAreaField =
            typeof(Missile).GetField("finArea", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? CurrentFinAreaField =
            typeof(Missile).GetField("currentFinArea", BindingFlags.Instance | BindingFlags.NonPublic);

        private static float _stockFinAreaM2 = -1f;

        internal static void CaptureBase(Missile? donor)
        {
            if (donor == null || _stockFinAreaM2 > 0f)
                return;
            float read = ReadFinArea(donor);
            if (read > 0.001f)
                _stockFinAreaM2 = read;
        }

        internal static void Apply(Missile missile)
        {
            if (missile == null || FinAreaField == null || CurrentFinAreaField == null)
                return;

            float stock = _stockFinAreaM2 > 0f ? _stockFinAreaM2 : ReadFinArea(missile);
            if (stock < 0.001f)
                stock = 0.4f;

            float fin = stock * HeavypinConstants.GlideFinAreaScale;
            FinAreaField.SetValue(missile, fin);

            bool finsOpen = missile.GetComponent<HeavypinTag>()?.FinsOpen == true;
            CurrentFinAreaField.SetValue(missile, finsOpen ? fin : fin * ClosedFinAreaRatio);
        }

        internal static void OnFinsDeployed(Missile missile)
        {
            if (missile == null || FinAreaField == null || CurrentFinAreaField == null)
                return;
            if (FinAreaField.GetValue(missile) is float fin && fin > 0f)
                CurrentFinAreaField.SetValue(missile, fin);
        }

        private static float ReadFinArea(Missile missile)
        {
            if (FinAreaField?.GetValue(missile) is float fin)
                return fin;
            return 0f;
        }
    }
}
