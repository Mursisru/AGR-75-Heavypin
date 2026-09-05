using HarmonyLib;
using Sledgepin.Blueprinter;
using Sledgepin.Bootstrap;
using Sledgepin.Runtime;
using UnityEngine;

namespace Sledgepin.Patches
{
    [HarmonyPatch(typeof(WeaponManager), nameof(WeaponManager.InitializeWeaponManager))]
    internal static class SledgepinWeaponManagerInitPatch
    {
        private static void Prefix(WeaponManager __instance)
        {
            HardpointInjector.EnsureRuntime(__instance);
        }
    }

    [HarmonyPatch(typeof(Hardpoint), nameof(Hardpoint.SpawnMount))]
    internal static class SledgepinSpawnMountPatch
    {
        private static void Prefix(Aircraft aircraft, WeaponMount weaponMount)
        {
            if (aircraft?.weaponManager != null)
                HardpointInjector.EnsureRuntime(aircraft.weaponManager);
            if (!SledgepinBootstrap.IsOurMount(weaponMount) || weaponMount.prefab == null)
                return;
            WeaponInfo? shared = SledgepinBootstrap.Info ?? weaponMount.info;
            if (shared != null)
            {
                weaponMount.info = shared;
                weaponMount.sortWeapons = true;
                if (SledgepinBootstrap.Definition?.unitPrefab != null)
                    shared.weaponPrefab = SledgepinBootstrap.Definition.unitPrefab;
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
            if (!SledgepinBootstrap.IsOurMount(weaponMount) || __result == null)
                return;
            if (weaponMount.prefab != null)
            {
                PrefabFactory.FreezeTemplatePhysics(weaponMount.prefab);
                weaponMount.prefab.SetActive(false);
            }
            PrefabFactory.ActivateMountedInstance(__result);
            SledgepinAnim.ParkMount(__result);
        }
    }

    [HarmonyPatch(typeof(Spawner), nameof(Spawner.SpawnMissile), new[] { typeof(GameObject), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(Unit), typeof(Unit) })]
    internal static class SledgepinSpawnMissileGoPatch
    {
        private static void Prefix(GameObject missile, out bool __state)
        {
            if (SledgepinSpawnGate.IsOurFlyPrefab(missile) && SledgepinSpawnGate.Pending > 0)
                SledgepinSpawnGate.BeginPrefabStamp(missile);
            __state = SledgepinSpawnGate.TryBegin();
        }

        private static void Postfix(bool __state, GameObject missile, Unit target, Missile __result)
        {
            try
            {
                SledgepinSpawnGate.EndPrefabStamp();
                if (__result == null)
                    return;
                bool rescue = !__state && SledgepinSpawnGate.ShouldRescueClaim(missile);
                if (!__state && !rescue)
                    return;
                SledgepinSpawnGate.Claim(__result, target);
            }
            finally
            {
                SledgepinSpawnGate.EndPrefabStamp();
                if (__state)
                    SledgepinSpawnGate.End();
            }
        }
    }

    [HarmonyPatch(typeof(Spawner), nameof(Spawner.SpawnMissile), new[] { typeof(MissileDefinition), typeof(Vector3), typeof(Quaternion), typeof(Vector3), typeof(Unit), typeof(Unit) })]
    internal static class SledgepinSpawnMissileDefPatch
    {
        private static void Prefix(MissileDefinition missile, out bool __state)
        {
            if (missile == null)
            {
                __state = false;
                return;
            }
            __state = string.Equals(missile.jsonKey, SledgepinConstants.MissileJsonKey, System.StringComparison.Ordinal);
            if (!__state)
                return;
            SledgepinSpawnGate.InFlight = true;
            SledgepinSpawnGate.BeginPrefabStamp(missile.unitPrefab);
        }

        private static void Postfix(bool __state, Unit target, Missile __result)
        {
            try
            {
                SledgepinSpawnGate.EndPrefabStamp();
                if (!__state || __result == null)
                    return;
                SledgepinSpawnGate.Claim(__result, target);
            }
            finally
            {
                SledgepinSpawnGate.EndPrefabStamp();
                if (__state)
                    SledgepinSpawnGate.End();
            }
        }
    }

    [HarmonyPatch(typeof(Spawner), nameof(Spawner.SpawnMissileEncyclopedia))]
    internal static class SledgepinEncyclopediaSpawnPatch
    {
        private static void Prefix(MissileDefinition missile, out bool __state)
        {
            if (missile == null)
            {
                __state = false;
                return;
            }
            __state = string.Equals(missile.jsonKey, SledgepinConstants.MissileJsonKey, System.StringComparison.Ordinal);
            if (!__state)
                return;
            SledgepinSpawnGate.InFlight = true;
            SledgepinSpawnGate.BeginPrefabStamp(missile.unitPrefab);
        }

        private static void Postfix(bool __state, Missile __result)
        {
            try
            {
                SledgepinSpawnGate.EndPrefabStamp();
                if (!__state || __result == null)
                    return;
                NobpContent.TryLoad();
                SledgepinSpawnGate.Claim(__result, null);
            }
            finally
            {
                SledgepinSpawnGate.EndPrefabStamp();
                if (__state)
                    SledgepinSpawnGate.End();
            }
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.Awake))]
    internal static class SledgepinMissileAwakePatch
    {
        private static void Postfix(Missile __instance)
        {
            try
            {
                SledgepinSpawnGate.TryEarlyVisual(__instance);
            }
            catch (System.Exception ex)
            {
                SledgepinPlugin.ModLog?.LogError($"SledgepinMissileAwakePatch: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.OnEnable))]
    internal static class SledgepinMissileOnEnablePatch
    {
        private static void Postfix(Missile __instance)
        {
            try
            {
                SledgepinSpawnGate.TryEarlyVisual(__instance);
            }
            catch (System.Exception ex)
            {
                SledgepinPlugin.ModLog?.LogError($"SledgepinMissileOnEnablePatch: {ex}");
            }
        }
    }

    [HarmonyPatch(typeof(MountedMissile), nameof(MountedMissile.Fire))]
    internal static class SledgepinFirePatch
    {
        private static void Prefix(MountedMissile __instance, Unit target)
        {
            if (__instance?.info == null || !SledgepinBootstrap.IsOurInfo(__instance.info))
                return;
            SledgepinSpawnGate.SyncSharedInfo(__instance);
            SledgepinSpawnGate.NoteFire(__instance, target);
        }

        private static void Postfix(MountedMissile __instance)
        {
            if (__instance?.info == null || !SledgepinBootstrap.IsOurInfo(__instance.info))
                return;
            SledgepinMountVisual.HideFired(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), "StartMissile")]
    internal static class SledgepinStartMissilePatch
    {
        private static void Postfix(Missile __instance)
        {
            if (SledgepinBootstrap.IsOurs(__instance))
                SledgepinSpawnGate.Ensure(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), "LocalStart")]
    internal static class SledgepinLocalStartPatch
    {
        private static void Postfix(Missile __instance)
        {
            if (SledgepinBootstrap.IsOurs(__instance))
                SledgepinSpawnGate.Ensure(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.DeployFins))]
    internal static class SledgepinDeployFinsPatch
    {
        private static void Postfix(Missile __instance)
        {
            if (!SledgepinBootstrap.IsOurs(__instance))
                return;
            SledgepinAnim.PlayFly(__instance);
            SledgepinAero.OnFinsDeployed(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), "ApplyAero")]
    internal static class SledgepinApplyAeroPatch
    {
        private static void Postfix(Missile __instance)
        {
            if (SledgepinBootstrap.IsOurs(__instance))
                SledgepinAero.BoostGlide(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.GetPierce))]
    internal static class SledgepinGetPiercePatch
    {
        private static void Postfix(Missile __instance, ref float __result)
        {
            if (SledgepinBootstrap.IsOurs(__instance))
                __result = SledgepinWarhead.PierceDamage;
        }
    }

    [HarmonyPatch(typeof(MountedMissile), nameof(MountedMissile.Rearm))]
    internal static class SledgepinRearmPatch
    {
        private static void Postfix(MountedMissile __instance)
        {
            if (__instance?.info == null || !SledgepinBootstrap.IsOurInfo(__instance.info))
                return;
            SledgepinMountVisual.Restore(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), "OnStartClient")]
    internal static class SledgepinOnStartClientPatch
    {
        private static void Postfix(Missile __instance)
        {
            if (SledgepinBootstrap.IsOurs(__instance))
                SledgepinSpawnGate.Ensure(__instance);
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.GetSeekerType))]
    internal static class SledgepinGetSeekerTypePatch
    {
        private static bool Prefix(Missile __instance, ref string __result)
        {
            if (!SledgepinBootstrap.IsOurs(__instance))
                return true;
            __result = SledgepinConstants.SeekerTypeName;
            return false;
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.GetYield))]
    internal static class SledgepinGetYieldPatch
    {
        private static void Postfix(Missile __instance, ref float __result)
        {
            if (SledgepinBootstrap.IsOurs(__instance))
                __result = SledgepinConstants.BlastYieldKg;
        }
    }

    [HarmonyPatch(typeof(MissileDefinition), nameof(MissileDefinition.GetMass))]
    internal static class SledgepinDefMassPatch
    {
        private static void Postfix(MissileDefinition __instance, ref float __result)
        {
            if (__instance != null &&
                string.Equals(__instance.jsonKey, SledgepinConstants.MissileJsonKey, System.StringComparison.Ordinal))
                __result = SledgepinConstants.LaunchMassKg;
        }
    }

    [HarmonyPatch(typeof(Missile), nameof(Missile.GetMass))]
    internal static class SledgepinGetMassPatch
    {
        private static void Postfix(Missile __instance, ref float __result)
        {
            if (SledgepinBootstrap.IsOurs(__instance))
                __result = SledgepinConstants.LaunchMassKg;
        }
    }

    [HarmonyPatch(typeof(AircraftSelectionMenu), nameof(AircraftSelectionMenu.DisplayInfo))]
    internal static class SledgepinDisplayInfoPatch
    {
        private static void Postfix(AircraftSelectionMenu __instance, WeaponInfo weaponInfo)
        {
            if (!SledgepinBootstrap.IsOurInfo(weaponInfo))
                return;
            weaponInfo.costPerRound = SledgepinConstants.Cost;
            weaponInfo.blastDamage = SledgepinConstants.BlastYieldKg;
            weaponInfo.massPerRound = SledgepinConstants.LaunchMassKg;
            SledgepinWarhead.ApplyInfo(weaponInfo);
            AircraftSelectionDisplay.SetTmp(__instance, "weaponSeeker", SledgepinConstants.SeekerTypeName);
            AircraftSelectionDisplay.SetTmp(__instance, "weaponAP", string.Format("AP: {0}", SledgepinWarhead.PierceDamage));
            AircraftSelectionDisplay.SetTmp(__instance, "weaponHE", "HE: " + UnitConverter.YieldReading(SledgepinConstants.BlastYieldKg));
            AircraftSelectionDisplay.SetTmp(__instance, "weaponCost", "C: " + UnitConverter.ValueReading(SledgepinConstants.Cost));
            AircraftSelectionDisplay.SetTmp(__instance, "weaponRCS", string.Format("RCS: {0}", SledgepinConstants.RadarSize));
            float rangeM = SledgepinCalcProxy.EncyclopediaRangeM > 1000f
                ? SledgepinCalcProxy.EncyclopediaRangeM
                : SledgepinConstants.DesignRangeM;
            AircraftSelectionDisplay.SetTmp(__instance, "weaponRange", "R: " + UnitConverter.DistanceReading(rangeM));
            SledgepinEncyclopediaStats.ApplyTargetRequirements(weaponInfo);
        }
    }

    [HarmonyPatch(typeof(EncyclopediaBrowser), "DisplayUnitInfo")]
    internal static class SledgepinEncyclopediaDisplayPatch
    {
        private static void Postfix(EncyclopediaBrowser __instance, UnitDefinition definition)
        {
            if (definition == null ||
                !string.Equals(definition.jsonKey, SledgepinConstants.MissileJsonKey, System.StringComparison.Ordinal))
                return;
            definition.value = SledgepinConstants.Cost;
            definition.length = SledgepinConstants.LengthM;
            definition.width = SledgepinConstants.WidthM;
            definition.height = SledgepinConstants.HeightM;
            if (definition.spawnOffset.y < 0.05f)
                definition.spawnOffset = new Vector3(definition.spawnOffset.x, SledgepinConstants.HeightM * 0.5f, definition.spawnOffset.z);
            definition.radarSize = SledgepinConstants.RadarSize;
            AircraftSelectionDisplay.SetTmp(__instance, "guidance", SledgepinConstants.SeekerTypeName);
            AircraftSelectionDisplay.SetTmp(__instance, "yield", UnitConverter.YieldReading(SledgepinConstants.BlastYieldKg) + " TNT");
            AircraftSelectionDisplay.SetTmp(__instance, "mass", UnitConverter.WeightReading(SledgepinConstants.LaunchMassKg));
            AircraftSelectionDisplay.SetTmp(__instance, "cost", UnitConverter.ValueReading(SledgepinConstants.Cost));
            AircraftSelectionDisplay.SetTmp(__instance, "rcs", string.Format("{0}", SledgepinConstants.RadarSize));
            SledgepinEncyclopediaStats.ApplyMissilePanels(__instance);
            GameObject spawned = __instance.spawnedUnitObject;
            if (spawned == null)
                return;
            StockVisual.Hide(spawned);
        }
    }

    [HarmonyPatch(typeof(EncyclopediaBrowser), "SpawnUnit")]
    internal static class SledgepinEncyclopediaSpawnUnitPatch
    {
        private static void Postfix(EncyclopediaBrowser __instance, UnitDefinition definition)
        {
            if (definition == null ||
                !string.Equals(definition.jsonKey, SledgepinConstants.MissileJsonKey, System.StringComparison.Ordinal))
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
    internal static class SledgepinMountInitPatch
    {
        private static void Postfix(WeaponMount __instance)
        {
            if (!SledgepinBootstrap.IsOurMount(__instance) || __instance.info == null)
                return;
            WeaponInfo info = SledgepinBootstrap.Info ?? __instance.info;
            __instance.info = info;
            __instance.sortWeapons = true;
            info.weaponName = SledgepinConstants.WeaponInfoName;
            info.shortName = SledgepinConstants.ShortName;
            info.massPerRound = SledgepinConstants.LaunchMassKg;
            info.blastDamage = SledgepinConstants.BlastYieldKg;
            info.costPerRound = SledgepinConstants.Cost;
            SledgepinWarhead.ApplyInfo(info);
            info.missile = true;
            info.bomb = false;
            info.glideBomb = false;
            info.overHorizon = false;
            info.laserGuided = true;
            Sprite? preview = SledgepinWeaponIcon.Get();
            if (preview != null)
                info.weaponIcon = preview;
            SledgepinEncyclopediaStats.ApplyTargetRequirements(info);
            if (SledgepinBootstrap.Definition?.unitPrefab != null)
                info.weaponPrefab = SledgepinBootstrap.Definition.unitPrefab;
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
                ? string.Format("{0} x{1}", SledgepinConstants.MountDisplayName, ammo)
                : SledgepinConstants.MountDisplayName;
            __instance.mass = __instance.emptyMass + SledgepinConstants.LaunchMassKg * ammo;
        }
    }

    [HarmonyPatch(typeof(UnitRegistry), nameof(UnitRegistry.RegisterUnit))]
    internal static class SledgepinPersistentIdentityPatch
    {
        private static void Postfix(Unit unit)
        {
            if (unit is not Missile missile || !SledgepinBootstrap.IsOurs(missile))
                return;
            SledgepinSpawnGate.ApplyDisplayIdentity(missile);
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
