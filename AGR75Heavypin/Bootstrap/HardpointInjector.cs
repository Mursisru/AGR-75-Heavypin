using System;
using System.Collections.Generic;
using UnityEngine;

namespace Heavypin.Bootstrap
{
    internal static class HardpointInjector
    {
        internal static void Inject(Encyclopedia enc, WeaponMount? mount4, WeaponMount? mount6)
        {
            if (enc?.aircraft == null)
                return;

            int smallSets = 0;
            int largeSets = 0;
            foreach (AircraftDefinition ad in enc.aircraft)
            {
                if (ad?.unitPrefab == null)
                    continue;
                InjectOnPrefab(ad, mount4, mount6, ref smallSets, ref largeSets);
            }
            HeavypinPlugin.ModLog?.LogInfo(
                $"HardpointInjector: small-AGR 4x sets={smallSets} large-AGR 6x sets={largeSets}.");
        }

        internal static void EnsureRuntime(WeaponManager wm)
        {
            if (wm == null || !HeavypinBootstrap.IsReady)
                return;
            Aircraft? aircraft = wm.GetComponent<Aircraft>();
            if (aircraft?.definition?.unitPrefab == null)
                return;
            WeaponManager? template = aircraft.definition.unitPrefab.GetComponent<Aircraft>()?.weaponManager;
            if (template?.hardpointSets == null || wm.hardpointSets == null)
                return;

            int count = Math.Min(wm.hardpointSets.Length, template.hardpointSets.Length);
            for (int i = 0; i < count; i++)
            {
                HardpointSet? live = wm.hardpointSets[i];
                HardpointSet? def = template.hardpointSets[i];
                if (live == null || def?.weaponOptions == null)
                    continue;
                live.weaponOptions ??= new List<WeaponMount>();
                MergeOurMounts(live.weaponOptions, def.weaponOptions);
            }
        }

        private static void InjectOnPrefab(
            AircraftDefinition ad,
            WeaponMount? mount4,
            WeaponMount? mount6,
            ref int smallSets,
            ref int largeSets)
        {
            WeaponManager[] managers = ad.unitPrefab.GetComponentsInChildren<WeaponManager>(true);
            foreach (WeaponManager wm in managers)
            {
                if (wm?.hardpointSets == null)
                    continue;
                foreach (HardpointSet set in wm.hardpointSets)
                {
                    if (set == null)
                        continue;
                    set.weaponOptions ??= new List<WeaponMount>();
                    bool addedSmall = false;
                    bool addedLarge = false;
                    int count = set.weaponOptions.Count;
                    for (int i = 0; i < count; i++)
                    {
                        WeaponMount? o = set.weaponOptions[i];
                        if (!AgrDonor.IsAgrMount(o))
                            continue;
                        if (AgrDonor.IsSmallAgrSlot(o!) && mount4 != null && !ContainsRef(set.weaponOptions, mount4))
                        {
                            set.weaponOptions.Add(mount4);
                            addedSmall = true;
                        }
                        if (AgrDonor.IsLargeAgrSlot(o!) && mount6 != null && !ContainsRef(set.weaponOptions, mount6))
                        {
                            set.weaponOptions.Add(mount6);
                            addedLarge = true;
                        }
                    }
                    if (addedSmall)
                        smallSets++;
                    if (addedLarge)
                        largeSets++;
                }
            }
        }

        private static void MergeOurMounts(List<WeaponMount> live, List<WeaponMount> def)
        {
            for (int i = 0; i < def.Count; i++)
            {
                WeaponMount? m = def[i];
                if (m == null || !PrefabFactory.IsOurMountKey(m.jsonKey))
                    continue;
                if (!ContainsRef(live, m))
                    live.Add(m);
            }
        }

        private static bool ContainsRef(List<WeaponMount> list, WeaponMount item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item))
                    return true;
            }
            return false;
        }
    }
}
