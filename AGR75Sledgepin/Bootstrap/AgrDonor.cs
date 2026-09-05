using System;
using UnityEngine;

namespace Sledgepin.Bootstrap
{
    internal static class AgrDonor
    {
        internal static bool IsAgr24Name(string? s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            return s!.IndexOf(SledgepinConstants.Agr24Name, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   s.IndexOf(SledgepinConstants.Agr24Alt, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsAgr18Name(string? s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            return s!.IndexOf(SledgepinConstants.Agr18Name, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   s.IndexOf(SledgepinConstants.Agr18Alt, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool IsAgrName(string? s) => IsAgr24Name(s) || IsAgr18Name(s);

        internal static bool IsAgr24Def(MissileDefinition? def)
        {
            if (def == null)
                return false;
            return IsAgr24Name(def.unitName) || IsAgr24Name(def.jsonKey) || IsAgr24Name(def.bogeyName);
        }

        internal static bool IsAgr24Info(WeaponInfo? info)
        {
            if (info == null)
                return false;
            return IsAgr24Name(info.weaponName) || IsAgr24Name(info.shortName);
        }

        internal static bool IsAgrMount(WeaponMount? mount)
        {
            if (mount == null || PrefabFactory.IsOurMountKey(mount.jsonKey))
                return false;
            if (IsAgr24Info(mount.info) || IsAgr18Info(mount.info))
                return true;
            return IsAgrName(mount.mountName) || IsAgrName(mount.jsonKey);
        }

        internal static bool IsAgr18Info(WeaponInfo? info)
        {
            if (info == null)
                return false;
            return IsAgr18Name(info.weaponName) || IsAgr18Name(info.shortName);
        }

        internal static bool IsSmallAgrSlot(WeaponMount mount)
        {
            int ammo = CountAmmo(mount);
            return ammo > 0 && ammo <= SledgepinConstants.SmallAmmoMax;
        }

        internal static bool IsLargeAgrSlot(WeaponMount mount)
        {
            int ammo = CountAmmo(mount);
            return ammo > SledgepinConstants.SmallAmmoMax;
        }

        internal static int CountAmmo(WeaponMount mount)
        {
            if (mount == null)
                return 0;
            if (mount.prefab != null)
            {
                int n = mount.prefab.GetComponentsInChildren<MountedMissile>(true).Length;
                if (n > 0)
                    return n;
            }
            return mount.ammo;
        }

        internal static MissileDefinition? FindAgr24Missile(Encyclopedia enc)
        {
            if (enc?.missiles == null)
                return null;
            MissileDefinition? fallback = null;
            foreach (MissileDefinition m in enc.missiles)
            {
                if (m?.unitPrefab == null || !IsAgr24Def(m))
                    continue;
                if (m.jsonKey != null && m.jsonKey.IndexOf("single", StringComparison.OrdinalIgnoreCase) >= 0)
                    return m;
                fallback ??= m;
            }
            return fallback;
        }

        internal static WeaponMount? FindAgr24MountWithSlots(Encyclopedia enc, int wantSlots)
        {
            if (enc?.weaponMounts == null)
                return null;
            WeaponMount? best = null;
            int bestDiff = int.MaxValue;
            foreach (WeaponMount w in enc.weaponMounts)
            {
                if (w?.prefab == null || !IsAgr24Info(w.info))
                    continue;
                int n = CountAmmo(w);
                if (n < 1)
                    continue;
                int diff = Math.Abs(n - wantSlots);
                if (diff < bestDiff)
                {
                    bestDiff = diff;
                    best = w;
                    if (diff == 0)
                        return w;
                }
            }
            return best;
        }

        internal static WeaponInfo? FindAgr24WeaponInfo(Encyclopedia enc)
        {
            WeaponMount? m = FindAgr24MountWithSlots(enc, 4);
            if (m?.info != null)
                return m.info;
            if (enc?.weaponMounts == null)
                return null;
            foreach (WeaponMount w in enc.weaponMounts)
            {
                if (IsAgr24Info(w?.info))
                    return w!.info;
            }
            return null;
        }
    }
}
