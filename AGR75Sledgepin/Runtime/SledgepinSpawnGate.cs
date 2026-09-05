using System.Collections.Generic;
using System.Reflection;
using Sledgepin.Blueprinter;
using Sledgepin.Bootstrap;
using Sledgepin.Runtime;
using UnityEngine;

namespace Sledgepin
{
    internal sealed class SledgepinTag : MonoBehaviour
    {
        internal bool FlightReady;
        internal bool VisualReady;
        internal bool FinsOpen;
    }

    internal static class SledgepinSpawnGate
    {
        private static readonly FieldInfo? InfoField =
            typeof(Missile).GetField("info", BindingFlags.Instance | BindingFlags.NonPublic);

        private const float PendingTtlS = 8f;
        internal static int Pending;
        internal static bool InFlight;
        private static float _until = -1f;
        private static Unit? _pendingTarget;
        private static readonly Queue<MountedMissile?> PendingMounts = new Queue<MountedMissile?>(8);

        internal static void NoteFire(MountedMissile? mount, Unit? target)
        {
            Expire();
            Pending++;
            _until = Time.realtimeSinceStartup + PendingTtlS;
            _pendingTarget = target;
            PendingMounts.Enqueue(mount);
            SledgepinMountVisual.HideFired(mount);
            SyncSharedInfo(mount);
        }

        internal static bool HasRecentFire() =>
            _until > 0f && Time.realtimeSinceStartup <= _until;

        internal static bool ShouldRescueClaim(GameObject? prefab)
        {
            if (!HasRecentFire())
                return false;
            GameObject? fly = SledgepinBootstrap.Definition?.unitPrefab;
            return fly != null && ReferenceEquals(prefab, fly);
        }

        internal static void SyncSharedInfo(MountedMissile? mount)
        {
            WeaponInfo? shared = SledgepinBootstrap.Info;
            GameObject? fly = SledgepinBootstrap.Definition?.unitPrefab;
            if (shared == null)
                return;
            if (fly != null)
                shared.weaponPrefab = fly;
            if (mount != null)
                mount.info = shared;
        }

        internal static bool TryBegin()
        {
            Expire();
            if (Pending <= 0)
                return false;
            Pending--;
            InFlight = true;
            return true;
        }

        internal static void End() => InFlight = false;

        private static void Expire()
        {
            if (Pending <= 0)
                return;
            if (_until < 0f || Time.realtimeSinceStartup <= _until)
                return;
            Pending = 0;
            _until = -1f;
            _pendingTarget = null;
            PendingMounts.Clear();
        }

        private static Missile? _stampMissile;
        private static UnitDefinition? _stampSavedDef;

        internal static bool BeginPrefabStamp(GameObject? prefab)
        {
            EndPrefabStamp();
            MissileDefinition? ours = SledgepinBootstrap.Definition;
            if (prefab == null || ours == null)
                return false;
            Missile? m = prefab.GetComponent<Missile>() ?? prefab.GetComponentInChildren<Missile>(true);
            if (m == null)
                return false;
            _stampMissile = m;
            _stampSavedDef = m.definition;
            m.definition = ours;
            return true;
        }

        internal static void EndPrefabStamp()
        {
            if (_stampMissile != null && _stampSavedDef != null)
                _stampMissile.definition = _stampSavedDef;
            _stampMissile = null;
            _stampSavedDef = null;
        }

        internal static void ApplyDisplayIdentity(Missile missile)
        {
            if (missile == null)
                return;
            MissileDefinition? def = SledgepinBootstrap.Definition;
            if (def != null)
                missile.definition = def;
            missile.NetworkunitName = SledgepinConstants.UnitName;
            missile.unitName = SledgepinConstants.UnitName;
            if (!UnitRegistry.TryGetPersistentUnit(missile.persistentID, out PersistentUnit pu) || pu == null)
                return;
            pu.unitName = SledgepinConstants.UnitName;
            if (def != null)
                pu.definition = def;
        }

        internal static bool IsSharedShell(GameObject? go)
        {
            if (go == null)
                return false;
            GameObject? fly = SledgepinBootstrap.Definition?.unitPrefab;
            return fly != null && ReferenceEquals(go, fly);
        }

        internal static void TryEarlyVisual(Missile? missile)
        {
            if (missile == null)
                return;
            try
            {
                if (IsSharedShell(missile.gameObject))
                    return;
                if (HasForeignOwnerTag(missile))
                    return;
                if (!SledgepinBootstrap.IsOurs(missile))
                    return;

                NobpContent.TryLoad();
                if (NobpContent.RocketPrefab != null)
                    VisualStamp.StampRocket(missile.gameObject, NobpContent.RocketPrefab);
                StockVisual.Hide(missile.gameObject);

                SledgepinTag? tag = missile.GetComponent<SledgepinTag>();

                if (tag == null)
                    tag = missile.gameObject.AddComponent<SledgepinTag>();
                tag.VisualReady = VisualStamp.FindRocket(missile.transform) != null;
            }
            catch (System.Exception ex)
            {
                SledgepinPlugin.ModLog?.LogError($"TryEarlyVisual: {ex}");
            }
        }

        internal static void Claim(Missile missile, Unit? fireTarget)
        {
            if (missile == null)
                return;
            if (IsSharedShell(missile.gameObject))
                return;
            if (HasForeignOwnerTag(missile))
                return;

            ApplyDisplayIdentity(missile);
            if (SledgepinBootstrap.Info != null)
                InfoField?.SetValue(missile, SledgepinBootstrap.Info);
            if (missile.GetComponent<SledgepinTag>() == null)
                missile.gameObject.AddComponent<SledgepinTag>();

            Unit? t = fireTarget != null ? fireTarget : _pendingTarget;
            if (t != null && !t.disabled)
                missile.SetTarget(t);
            _pendingTarget = null;

            MountedMissile? firedMount = PendingMounts.Count > 0 ? PendingMounts.Dequeue() : null;
            SledgepinMountVisual.HideFired(firedMount);

            missile.RCS = SledgepinConstants.RadarSize;

            NobpContent.TryLoad();
            if (NobpContent.RocketPrefab != null)
                VisualStamp.StampRocket(missile.gameObject, NobpContent.RocketPrefab);

            Transform? vis = VisualStamp.FindRocket(missile.transform);
            bool displayOnly = fireTarget == null && !HasRecentFire();
            if (!displayOnly)
            {
                FinishFlight(missile, vis);
                SledgepinAnim.Play(vis);
            }
            StockVisual.Hide(missile.gameObject);
            SledgepinTag? ready = missile.GetComponent<SledgepinTag>();
            if (ready != null)
                ready.VisualReady = vis != null || VisualStamp.FindRocket(missile.transform) != null;
        }

        internal static void FinishFlight(Missile missile, Transform? vis = null)
        {
            if (missile == null)
                return;
            missile.RCS = SledgepinConstants.RadarSize;

            SledgepinTag? tag = missile.GetComponent<SledgepinTag>();
            if (tag == null || !tag.FlightReady)
            {
                SledgepinMotors.Apply(missile);
                if (tag != null)
                    tag.FlightReady = true;
            }

            SledgepinAero.Apply(missile);
            SledgepinMotorFx.Bind(missile);
            SledgepinIrCold.Ensure(missile);

            if (vis == null)
                vis = VisualStamp.FindRocket(missile.transform);
            if (tag == null || !tag.FinsOpen)
                SledgepinAnim.Park(vis);
        }

        internal static void Ensure(Missile missile)
        {
            if (missile == null || !SledgepinBootstrap.IsOurs(missile))
                return;
            if (missile.GetComponent<SledgepinTag>() == null)
                Claim(missile, _pendingTarget);
            else
            {
                ApplyDisplayIdentity(missile);
                FinishFlight(missile);
            }
        }

        internal static bool IsOurFlyPrefab(GameObject? go)
        {
            if (go == null)
                return false;
            GameObject? fly = SledgepinBootstrap.Definition?.unitPrefab;
            if (fly != null && ReferenceEquals(go, fly))
                return true;
            return go.GetComponent<SledgepinTag>() != null || go.GetComponentInChildren<SledgepinTag>(true) != null;
        }

        private static bool HasForeignOwnerTag(Missile missile)
        {
            if (missile == null)
                return false;
            MonoBehaviour[] comps = missile.GetComponents<MonoBehaviour>();
            for (int i = 0; i < comps.Length; i++)
            {
                MonoBehaviour? c = comps[i];
                if (c == null)
                    continue;
                string n = c.GetType().Name;
                for (int t = 0; t < SledgepinConstants.ForeignOwnerTags.Length; t++)
                {
                    if (n.IndexOf(SledgepinConstants.ForeignOwnerTags[t], System.StringComparison.Ordinal) >= 0)
                        return true;
                }
            }
            return false;
        }
    }
}
