using System.Reflection;
using UnityEngine;

namespace Sledgepin.Runtime
{
    // Stock finArea for CalcRange/coast drag. Extra lift only via ApplyAero postfix (L/D up).
    internal static class SledgepinAero
    {
        private const float ClosedFinAreaRatio = 0.1f;

        private static readonly FieldInfo? FinAreaField =
            typeof(Missile).GetField("finArea", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? CurrentFinAreaField =
            typeof(Missile).GetField("currentFinArea", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? AirDensityField =
            typeof(Missile).GetField("airDensity", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? LiftCurveField =
            typeof(Missile).GetField("liftCurve", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? DragCurveField =
            typeof(Missile).GetField("dragCurve", BindingFlags.Instance | BindingFlags.NonPublic);

        private static float _stockFinAreaM2 = -1f;

        internal static void CaptureBase(Missile? donor)
        {
            if (donor == null || _stockFinAreaM2 > 0f)
                return;
            float read = ReadFinArea(donor);
            if (read > 0.001f)
                _stockFinAreaM2 = read;
        }

        // Keep AGR-24 finArea — inflating it raises drag in CalcRange and ApplyAero equally (no L/D gain).
        internal static void Apply(Missile missile)
        {
            if (missile == null || FinAreaField == null || CurrentFinAreaField == null)
                return;

            float stock = _stockFinAreaM2 > 0f ? _stockFinAreaM2 : ReadFinArea(missile);
            if (stock < 0.001f)
                stock = 0.4f;

            FinAreaField.SetValue(missile, stock);
            bool finsOpen = missile.GetComponent<SledgepinTag>()?.FinsOpen == true;
            CurrentFinAreaField.SetValue(missile, finsOpen ? stock : stock * ClosedFinAreaRatio);
        }

        internal static void OnFinsDeployed(Missile missile)
        {
            if (missile == null || FinAreaField == null || CurrentFinAreaField == null)
                return;
            if (FinAreaField.GetValue(missile) is float fin && fin > 0f)
                CurrentFinAreaField.SetValue(missile, fin);
        }

        // After vanilla ApplyAero: add (LiftScale-1)×lift and (DragScale-1)×drag so glide L/D rises.
        internal static void BoostGlide(Missile missile)
        {
            if (missile == null || missile.rb == null)
                return;
            if (CurrentFinAreaField?.GetValue(missile) is not float area || area < 0.001f)
                return;

            float liftExtra = SledgepinConstants.GlideLiftScale - 1f;
            float dragExtra = SledgepinConstants.GlideDragScale - 1f;
            if (Mathf.Abs(liftExtra) < 0.001f && Mathf.Abs(dragExtra) < 0.001f)
                return;

            Vector3 airVel = missile.rb.velocity;
            float v2 = airVel.sqrMagnitude;
            if (v2 < 1f)
                return;

            float rho = 1.2f;
            if (AirDensityField?.GetValue(missile) is float d && d > 0f)
                rho = d;

            float aoa = 0.017453292f * Vector3.Angle(missile.transform.forward, airVel);
            float cl = 0f;
            float cd = 0.02f;
            if (LiftCurveField?.GetValue(missile) is AnimationCurve lift)
                cl = lift.Evaluate(aoa);
            if (DragCurveField?.GetValue(missile) is AnimationCurve drag)
                cd = drag.Evaluate(aoa);

            float qA = rho * v2 * 0.5f * area;
            Vector3 liftDir = Vector3.Cross(Vector3.Cross(missile.transform.forward, airVel), airVel).normalized;
            Vector3 dragDir = -airVel.normalized;

            // Vanilla uses d2 = cl * … * -0.5 * area; force is liftDir * d2 (negative cl → toward lift).
            Vector3 force = liftDir * (cl * qA * -liftExtra) + dragDir * (cd * qA * dragExtra);
            if (force.sqrMagnitude > 0.01f)
                missile.rb.AddForce(force);
        }

        private static float ReadFinArea(Missile missile)
        {
            if (FinAreaField?.GetValue(missile) is float fin)
                return fin;
            return 0f;
        }
    }
}
