using HarmonyLib;
using Heavypin.Blueprinter;
using Heavypin.Bootstrap;
using Heavypin.Runtime;
using UnityEngine;

namespace Heavypin.Patches
{
    [HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.InitializeWeaponManager))]
    internal static class HeavypinWeaponManagerInitPatch
    {
        private static void Prefix(WeaponManager __instance)
        {
            HardpointInjector.EnsureRuntime(__instance);
        }
    }

    [HarmonyPatch(typeof(Hardpoint), nameof(Hardpoint.SpawnMount))]
    internal static class HeavypinSpawnMountPatch
    {
        private static void Prefix(Aircraft aircraft, WeaponMount weaponMount)
        {
            if (aircraft?.weaponManager != null)
                HardpointInjector.EnsureRuntime(aircraft.weaponManager);
            if (!HeavypinBootstrap.IsOurMount(weaponMount) || weaponMount.prefab == null)
                return;
            WeaponInfo? shared = HeavypinBootstrap.Info ?? weaponMount.info;
            if (shared != null)
            {
                weaponMount.info = shared;
                weaponMount.sortWeapons = true;
                if (HeavypinBootstrap.Definition?.unitPrefab != null)
                    shared.weaponPrefab = HeavypinBootstrap.Definition.unitPrefab;
                foreach (MountedMissile mm in weaponMount.prefab.GetComponentsInChildren<MountedMissile>(true))
                {
                    if (mm != null)
                        mm.info = shared;
                }
            }
            PrefabFactory.FreezeTemplatePhysics(weaponMount.prefab);
            weaponMount.prefab.SetActive(true);
        }

        private static void Postfix(Hardpoint __instance, WeaponMount weaponMount, GameObject __result)
        {
            if (!HeavypinBootstrap.IsOurMount(weaponMount) || __result == null)
                return;
            if (weaponMount.prefab != null)
            {
                PrefabFactory.FreezeTemplatePhysics(weaponMount.prefab);
                weaponMount.prefab.SetActive(false);
            }
            PrefabFactory.ActivateMountedInstance(__result);
            HeavypinAnim.ParkMount(__result);
        }
    }

    [HarmonyPatch(typeof(Spawner), nameof(Spawner.SpawnMissile), new[] { typeof(GameObject), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(Unit), typeof(Unit) })]
    internal static class HeavypinSpawnMissileGoPatch
    {
        private static void Prefix(GameObject missile, out bool __state)
        {
            if (HeavypinSpawnGate.IsOurFlyPrefab(missile) && HeavypinSpawnGate.Pending > 0)
                HeavypinSpawnGate.BeginPrefabStamp(missile);
            __state = HeavypinSpawnGate.TryBegin();
        }

        private static void Postfix(bool __state, GameObject missile, Unit target, Missile __result)
        {
            try
            {
                HeavypinSpawnGate.EndPrefabStamp();
                if (__result == null)
                    return;
                bool rescue = !__state && HeavypinSpawnGate.ShouldRescueClaim(missile);
                if (!__state && !rescue)
                    return;
                HeavypinSpawnGate.Claim(__result, target);
            }
            finally
            {
                HeavypinSpawnGate.EndPrefabStamp();
                if (__state)
                    HeavypinSpawnGate.End();
            }
        }
    }

    [HarmonyPatch(typeof(Spawner), nameof(Spawner.SpawnMissile), new[] { typeof(MissileDefinition), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(Unit), typeof(Unit) })]
    internal static class HeavypinSpawnMissileDefPatch
    {
        private static void Prefix(MissileDefinition missile, out bool __state)
        {
            if (missile == null)
            {
                __state = false;
                return;
            }
            __state = string.Equals(missile.jsonKey, HeavypinConstants.MissileJsonKey, System.StringComparison.Ordinal);
            if (!__state)
                return;
            HeavypinSpawnGate.InFlight = true;
            HeavypinSpawnGate.BeginPrefabStamp(missile.unitPrefab);
        }

        private static void Postfix(bool __state, Unit target, Missile __result)
        {
            try
            {
                HeavypinSpawnGate.EndPrefabStamp();
                if (!__state || __result == null)
                    return;
                HeavypinSpawnGate.Claim(__result, target);
            }
            finally
            {
                HeavypinSpawnGate.EndPrefabStamp();
                if (__state)
                    HeavypinSpawnGate.End();
            }
        }
    }

    [HarmonyPatch(typeof(Spawner), nameof(Spawner.SpawnMissileEncyclopedia))]
    internal static class HeavypinEncyclopediaSpawnPatch
    {
        private static void Prefix(MissileDefinition missile, out bool __state)
        {
            if (missile == null)
            {
                __state = false;
                return;
            }
            __state = string.Equals(missile.jsonKey, HeavypinConstants.MissileJsonKey, System.StringComparison.Ordinal);
            if (!__state)
                return;
            HeavypinSpawnGate.InFlight = true;
            HeavypinSpawnGate.BeginPrefabStamp(missile.unitPrefab);
        }

        private static void Postfix(bool __state, Missile __result)
        {
            try
            {
                HeavypinSpawnGate.EndPrefabStamp();
                if (!__state || __result == null)
                    return;
                NobpContent.TryLoad();
                HeavypinSpawnGate.Claim(__result, null);
            }
            finally
            {
                HeavypinSpawnGate.EndPrefabStamp();
                if (__state)
                    HeavypinSpawnGate.End();
            }
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.Awake))]
    internal static class HeavypinMissileAwakePatch
    {
        private static void Postfix(Missile __instance)
        {
            try
            {
                HeavypinSpawnGate.TryEarlyVisual(__instance);
            }
            catch (System.Exception ex)
            {
                HeavypinPlugin.ModLog?.LogError($"HeavypinMissileAwakePatch: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.OnEnable))]
    internal static class HeavypinMissileOnEnablePatch
    {
        private static void Postfix(Missile __instance)
        {
            try
            {
                HeavypinSpawnGate.TryEarlyVisual(__instance);
            }
            catch (System.Exception ex)
            {
                HeavypinPlugin.ModLog?.LogError($"HeavypinMissileOnEnablePatch: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(MountedMissile), nameof(MountedMissile.Fire))]
    internal static class HeavypinFirePatch
    {
        private static void Prefix(MountedMissile __instance, Unit target)
        {
            if (__instance?.info == null || !HeavypinBootstrap.IsOurInfo(__instance.info))
                return;
            HeavypinSpawnGate.SyncSharedInfo(__instance);
            HeavypinSpawnGate.NoteFire(__instance, target);
        }

        private static void Postfix(MountedMissile __instance)
        {
            if (__instance?.info == null || !HeavypinBootstrap.IsOurInfo(__instance.info))
                return;
            HeavypinMountVisual.HideFired(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), "StartMissile")]
    internal static class HeavypinStartMissilePatch
    {
        private static void Postfix(Missile __instance)
        {
            if (HeavypinBootstrap.IsOurs(__instance))
                HeavypinSpawnGate.Ensure(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), "LocalStart")]
    internal static class HeavypinLocalStartPatch
    {
        private static void Postfix(Missile __instance)
        {
            if (HeavypinBootstrap.IsOurs(__instance))
                HeavypinSpawnGate.Ensure(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.DeployFins))]
    internal static class HeavypinDeployFinsPatch
    {
        private static void Postfix(Missile __instance)
        {
            if (!HeavypinBootstrap.IsOurs(__instance))
                return;
            HeavypinMotorFx.Ensure(__instance);
            HeavypinAnim.PlayFly(__instance);
        }
    }

    [HarmonyPatch(typeof(MountedMissile), nameof(MountedMissile.Rearm))]
    internal static class HeavypinRearmPatch
    {
        private static void Postfix(MountedMissile __instance)
        {
            if (__instance?.info == null || !HeavypinBootstrap.IsOurInfo(__instance.info))
                return;
            HeavypinMountVisual.Restore(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), "OnStartClient")]
    internal static class HeavypinOnStartClientPatch
    {
        private static void Postfix(Missile __instance)
        {
            if (HeavypinBootstrap.IsOurs(__instance))
                HeavypinSpawnGate.Ensure(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.GetSeekerType))]
    internal static class HeavypinGetSeekerTypePatch
    {
        private static bool Prefix(Missile __instance, ref string __result)
        {
            if (!HeavypinBootstrap.IsOurs(__instance))
                return true;
            __result = HeavypinConstants.SeekerTypeName;
            return false;
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.GetYield))]
    internal static class HeavypinGetYieldPatch
    {
        private static void Postfix(Missile __instance, ref float __result)
        {
            if (HeavypinBootstrap.IsOurs(__instance))
                __result = HeavypinConstants.BlastYieldKg;
        }
    }

    [HarmonyPatch(typeof(MissileDefinition), nameof(MissileDefinition.GetMass))]
    internal static class HeavypinDefMassPatch
    {
        private static void Postfix(MissileDefinition __instance, ref float __result)
        {
            if (__instance != null &&
                string.Equals(__instance.jsonKey, HeavypinConstants.MissileJsonKey, System.StringComparison.Ordinal))
                __result = HeavypinConstants.LaunchMassKg;
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.GetMass))]
    internal static class HeavypinGetMassPatch
    {
        private static void Postfix(Missile __instance, ref float __result)
        {
            if (HeavypinBootstrap.IsOurs(__instance))
                __result = HeavypinConstants.LaunchMassKg;
        }
    }

    [HarmonyPatch(typeof(AircraftSelectionMenu), nameof(AircraftSelectionMenu.DisplayInfo))]
    internal static class HeavypinDisplayInfoPatch
    {
        private static void Postfix(AircraftSelectionMenu __instance, WeaponInfo weaponInfo)
        {
            if (!HeavypinBootstrap.IsOurInfo(weaponInfo))
                return;
            weaponInfo.costPerRound = HeavypinConstants.Cost;
            weaponInfo.blastDamage = HeavypinConstants.BlastYieldKg;
            weaponInfo.massPerRound = HeavypinConstants.LaunchMassKg;
            AircraftSelectionDisplay.SetTmp(__instance, "weaponSeeker", HeavypinConstants.SeekerTypeName);
            AircraftSelectionDisplay.SetTmp(__instance, "weaponHE", "HE: " + UnitConverter.YieldReading(HeavypinConstants.BlastYieldKg));
            AircraftSelectionDisplay.SetTmp(__instance, "weaponCost", "C: " + UnitConverter.ValueReading(HeavypinConstants.Cost));
            AircraftSelectionDisplay.SetTmp(__instance, "weaponRCS", string.Format("RCS: {0}", HeavypinConstants.RadarSize));
        }
    }

    [HarmonyPatch(typeof(EncyclopediaBrowser), "DisplayUnitInfo")]
    internal static class HeavypinEncyclopediaDisplayPatch
    {
        private static void Postfix(EncyclopediaBrowser __instance, UnitDefinition definition)
        {
            if (definition == null ||
                !string.Equals(definition.jsonKey, HeavypinConstants.MissileJsonKey, System.StringComparison.Ordinal))
                return;
            definition.value = HeavypinConstants.Cost;
            definition.length = HeavypinConstants.LengthM;
            definition.width = HeavypinConstants.WidthM;
            definition.height = HeavypinConstants.HeightM;
            if (definition.spawnOffset.y < 0.05f)
                definition.spawnOffset = new Vector3(definition.spawnOffset.x, HeavypinConstants.HeightM * 0.5f, definition.spawnOffset.z);
            definition.radarSize = HeavypinConstants.RadarSize;
            AircraftSelectionDisplay.SetTmp(__instance, "guidance", HeavypinConstants.SeekerTypeName);
            AircraftSelectionDisplay.SetTmp(__instance, "yield", UnitConverter.YieldReading(HeavypinConstants.BlastYieldKg) + " TNT");
            AircraftSelectionDisplay.SetTmp(__instance, "mass", UnitConverter.WeightReading(HeavypinConstants.LaunchMassKg));
            AircraftSelectionDisplay.SetTmp(__instance, "cost", UnitConverter.ValueReading(HeavypinConstants.Cost));
            AircraftSelectionDisplay.SetTmp(__instance, "rcs", string.Format("{0}", HeavypinConstants.RadarSize));
            HeavypinEncyclopediaStats.ApplyMissilePanels(__instance);
            GameObject spawned = __instance.spawnedUnitObject;
            if (spawned == null)
                return;
            StockVisual.Hide(spawned);
        }
    }

    [HarmonyPatch(typeof(EncyclopediaBrowser), "SpawnUnit")]
    internal static class HeavypinEncyclopediaSpawnUnitPatch
    {
        private static void Postfix(EncyclopediaBrowser __instance, UnitDefinition definition)
        {
            if (definition == null ||
                !string.Equals(definition.jsonKey, HeavypinConstants.MissileJsonKey, System.StringComparison.Ordinal))
                return;
            GameObject spawned = __instance.spawnedUnitObject;
            if (spawned == null)
                return;
            NobpContent.TryLoad();
            if (NobpContent.RocketPrefab != null)
                VisualStamp.StampRocket(spawned, NobpContent.RocketPrefab);
            StockVisual.Hide(spawned);
        }
    }

    [HarmonyPatch(typeof(WeaponMount), nameof(WeaponMount.Initialize))]
    internal static class HeavypinMountInitPatch
    {
        private static void Postfix(WeaponMount __instance)
        {
            if (!HeavypinBootstrap.IsOurMount(__instance) || __instance.info == null)
                return;
            WeaponInfo info = HeavypinBootstrap.Info ?? __instance.info;
            __instance.info = info;
            __instance.sortWeapons = true;
            info.weaponName = HeavypinConstants.WeaponInfoName;
            info.shortName = HeavypinConstants.ShortName;
            info.massPerRound = HeavypinConstants.LaunchMassKg;
            info.blastDamage = HeavypinConstants.BlastYieldKg;
            info.costPerRound = HeavypinConstants.Cost;
            info.missile = true;
            info.bomb = false;
            info.glideBomb = false;
            info.overHorizon = false;
            info.laserGuided = true;
            Sprite? preview = HeavypinWeaponIcon.Get();
            if (preview != null)
                info.weaponIcon = preview;
            HeavypinEncyclopediaStats.ApplyTargetRequirements(info);
            if (HeavypinBootstrap.Definition?.unitPrefab != null)
                info.weaponPrefab = HeavypinBootstrap.Definition.unitPrefab;
            int ammo = __instance.ammo;
            if (__instance.prefab != null)
            {
                int counted = __instance.prefab.GetComponentsInChildren<Weapon>(true).Length;
                if (counted > 0)
                    ammo = counted;
                foreach (MountedMissile mm in __instance.prefab.GetComponentsInChildren<MountedMissile>(true))
                {
                    if (mm != null)
                        mm.info = info;
                }
            }
            __instance.mountName = ammo > 1
                ? string.Format("{0} x{1}", HeavypinConstants.MountDisplayName, ammo)
                : HeavypinConstants.MountDisplayName;
            __instance.mass = __instance.emptyMass + HeavypinConstants.LaunchMassKg * ammo;
        }
    }

    [HarmonyPatch(typeof(UnitRegistry), nameof(UnitRegistry.RegisterUnit))]
    internal static class HeavypinPersistentIdentityPatch
    {
        private static void Postfix(Unit unit)
        {
            if (unit is not Missile missile || !HeavypinBootstrap.IsOurs(missile))
                return;
            HeavypinSpawnGate.ApplyDisplayIdentity(missile);
        }
    }

    internal static class AircraftSelectionDisplay
    {
        internal static void SetTmp(object host, string field, string value)
        {
            System.Reflection.FieldInfo? f = host.GetType().GetField(field,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            object? tmp = f?.GetValue(host);
            if (tmp == null)
                return;
            System.Reflection.PropertyInfo? p = tmp.GetType().GetProperty("text");
            p?.SetValue(tmp, value);
        }
    }
}
