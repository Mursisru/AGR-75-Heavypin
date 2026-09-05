using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Sledgepin.Blueprinter;
using Sledgepin.Bootstrap;
using Sledgepin.Patches;
using Sledgepin.Runtime;
using UnityEngine;

namespace Sledgepin
{
    internal static class SledgepinBootstrap
    {
        private static bool _done;
        private static bool _bootstrapping;
        internal static bool IsReady => _done;
        internal static MissileDefinition? Definition { get; private set; }
        internal static WeaponInfo? Info { get; private set; }
        internal static WeaponMount? Mount4x { get; private set; }
        internal static WeaponMount? Mount6x { get; private set; }

        private static readonly FieldInfo? UnitDisabled =
            typeof(UnitDefinition).GetField("disabled", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? MountDisabled =
            typeof(WeaponMount).GetField("disabled", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static IEnumerator Run(Encyclopedia enc)
        {
            if (enc == null)
                yield break;
            if (_bootstrapping)
                yield break;
            if (_done)
            {
                HardpointInjector.Inject(enc, Mount4x, Mount6x);
                yield break;
            }

            _bootstrapping = true;
            yield return BlueprinterGate.WaitUntilReady();

            try
            {
                PrefabFactory.AssertDonorsIntact(enc);
                NobpContent.TryLoad();

                MissileDefinition? agr24 = AgrDonor.FindAgr24Missile(enc);
                if (agr24?.unitPrefab != null)
                    VisualShader.PrimeFrom(agr24.unitPrefab);

                SledgepinMotorFx.Capture(enc, agr24);

                SledgepinMaterialDonor.Ensure();

                if (Encyclopedia.Lookup != null &&
                    Encyclopedia.Lookup.TryGetValue(SledgepinConstants.MissileJsonKey, out UnitDefinition existing) &&
                    existing is MissileDefinition md && md.unitPrefab != null)
                {
                    Definition = md;
                    GameObject? shellGo = SledgepinFlyFactory.BindSharedShell(agr24 ?? md);
                    if (shellGo != null)
                        md.unitPrefab = shellGo;
                    ApplySize(md);
                }
                else
                    Definition = CreateDefinition(enc, agr24);

                SledgepinDefinitionMass.Apply(Definition, SledgepinConstants.LaunchMassKg);
                SledgepinCalcProxy.Init(enc);

                Info = CreateSharedInfo(enc, Definition);
                Mount4x = CreateMount(enc, Definition, Info, SledgepinConstants.MountJsonKey4x, SledgepinConstants.SlotCount4);
                Mount6x = CreateMount(enc, Definition, Info, SledgepinConstants.MountJsonKey6x, SledgepinConstants.SlotCount6);

                if (Mount4x != null || Mount6x != null)
                    HardpointInjector.Inject(enc, Mount4x, Mount6x);

                PrefabFactory.AssertDonorsIntact(enc);
                _done = Definition != null && Info != null && (Mount4x != null || Mount6x != null);
                SledgepinPlugin.ModLog?.LogInfo(_done
                    ? $"AGR-75 Sledgepin ready def={SledgepinConstants.MissileJsonKey} 4x={(Mount4x != null)} 6x={(Mount6x != null)} visual={(NobpContent.RocketPrefab != null)}"
                    : "AGR-75 Sledgepin bootstrap incomplete.");
            }
            catch (Exception ex)
            {
                SledgepinPlugin.ModLog?.LogError($"SledgepinBootstrap: {ex}");
            }
            finally
            {
                _bootstrapping = false;
            }
        }

        internal static bool IsOurs(Missile? missile)
        {
            if (missile == null)
                return false;
            if (missile.GetComponent<SledgepinTag>() != null)
                return true;
            WeaponInfo? wi = missile.GetWeaponInfo();
            if (IsOurInfo(wi))
                return true;
            return missile.definition != null &&
                   string.Equals(missile.definition.jsonKey, SledgepinConstants.MissileJsonKey, StringComparison.Ordinal);
        }

        internal static bool IsOurMount(WeaponMount? mount)
        {
            return mount != null && PrefabFactory.IsOurMountKey(mount.jsonKey);
        }

        internal static bool IsOurInfo(WeaponInfo? info)
        {
            if (info == null)
                return false;
            if (Info != null && ReferenceEquals(info, Info))
                return true;
            return string.Equals(info.weaponName, SledgepinConstants.WeaponInfoName, StringComparison.Ordinal) ||
                   string.Equals(info.shortName, SledgepinConstants.ShortName, StringComparison.Ordinal) ||
                   string.Equals(info.weaponName, "AGR-75 Heavypin", StringComparison.Ordinal) ||
                   string.Equals(info.shortName, "AGR-75", StringComparison.Ordinal);
        }

        private static MissileDefinition? CreateDefinition(Encyclopedia enc, MissileDefinition? agr24)
        {
            if (agr24?.unitPrefab == null)
            {
                SledgepinPlugin.ModLog?.LogError("AGR-75 Sledgepin: no AGR-24 unitPrefab.");
                return null;
            }

            MissileDefinition def = ScriptableObject.CreateInstance<MissileDefinition>();
            def.name = "MissilePack_AGR75Sledgepin_Definition";
            def.jsonKey = SledgepinConstants.MissileJsonKey;
            PrefabFactory.CopyUnitDefScalars(agr24, def);
            PrefabFactory.CopyMapIdentity(agr24, def);
            def.unitName = SledgepinConstants.UnitName;
            def.bogeyName = SledgepinConstants.BogeyName;
            def.description = SledgepinConstants.Description;
            def.value = SledgepinConstants.Cost;
            def.mass = SledgepinConstants.LaunchMassKg;
            ApplySize(def);
            def.radarSize = SledgepinConstants.RadarSize;
            def.code = "MSL";
            def.IsObstacle = false;
            UnitDisabled?.SetValue(def, false);

            GameObject? fly = SledgepinFlyFactory.BindSharedShell(agr24);
            if (fly == null)
            {
                SledgepinPlugin.ModLog?.LogError("AGR-75 Sledgepin: fly prefab bind failed.");
                return null;
            }
            def.unitPrefab = fly;

            enc.missiles ??= new List<MissileDefinition>();
            if (!enc.missiles.Contains(def))
                enc.missiles.Add(def);
            Encyclopedia.Lookup ??= new Dictionary<string, UnitDefinition>(StringComparer.Ordinal);
            Encyclopedia.Lookup[def.jsonKey] = def;
            List<INetworkDefinition>? idx = enc.IndexLookup;
            if (idx != null && !PrefabFactory.ContainsNet(idx, def))
            {
                idx.Add(def);
                ((INetworkDefinition)def).LookupIndex = idx.Count - 1;
            }

            SledgepinDefinitionMass.Apply(def, SledgepinConstants.LaunchMassKg);
            SledgepinPlugin.ModLog?.LogInfo($"Created AGR-75 Sledgepin definition from shell '{agr24.jsonKey}'.");
            return def;
        }

        private static void ApplySize(MissileDefinition def)
        {
            def.length = SledgepinConstants.LengthM;
            def.width = SledgepinConstants.WidthM;
            def.height = SledgepinConstants.HeightM;
            if (def.spawnOffset.y < 0.05f)
                def.spawnOffset = new Vector3(def.spawnOffset.x, SledgepinConstants.HeightM * 0.5f, def.spawnOffset.z);
        }

        private static WeaponInfo CreateSharedInfo(Encyclopedia enc, MissileDefinition? def)
        {
            WeaponInfo info = ScriptableObject.CreateInstance<WeaponInfo>();
            info.name = "MissilePack_AGR75Sledgepin_Info";
            WeaponInfo? donor = AgrDonor.FindAgr24WeaponInfo(enc);
            if (donor != null)
            {
                info.effectiveness = donor.effectiveness;
                info.targetRequirements = donor.targetRequirements;
                info.pK = donor.pK;
                info.fireInterval = donor.fireInterval;
                info.muzzleVelocity = donor.muzzleVelocity;
                info.maxSpeed = donor.maxSpeed;
                info.dragCoef = donor.dragCoef;
                info.gravMult = donor.gravMult;
                info.pierceDamage = donor.pierceDamage;
                info.armorTierEffectiveness = donor.armorTierEffectiveness;
                info.visibilityWhenFired = donor.visibilityWhenFired;
                info.useWeaponDoors = donor.useWeaponDoors;
                info.boresight = donor.boresight;
                info.rearmGround = donor.rearmGround;
                info.rearmShip = donor.rearmShip;
            }

            TargetRequirements tr = info.targetRequirements;
            tr.minAltitude = -200f;
            tr.maxAltitude = 80000f;
            tr.maxRange = SledgepinConstants.DesignRangeM;
            tr.minRange = SledgepinConstants.EncyclopediaMinRangeM;
            info.targetRequirements = tr;
            SledgepinEncyclopediaStats.ApplyTargetRequirements(info);

            Sprite? preview = SledgepinWeaponIcon.Get();
            if (preview != null)
                info.weaponIcon = preview;

            info.weaponName = SledgepinConstants.WeaponInfoName;
            info.shortName = SledgepinConstants.ShortName;
            info.description = SledgepinConstants.Description;
            info.massPerRound = SledgepinConstants.LaunchMassKg;
            info.costPerRound = SledgepinConstants.Cost;
            SledgepinWarhead.ApplyInfo(info);
            info.pK = SledgepinConstants.Pk;
            info.nuclear = false;
            info.strategic = false;
            info.bomb = false;
            info.glideBomb = false;
            info.missile = true;
            info.overHorizon = false;
            info.laserGuided = true;
            info.gun = false;
            info.energy = false;
            info.jammer = false;
            info.troops = false;
            info.hideInDisplay = false;
            info.cargo = false;
            info.sling = false;
            if (def?.unitPrefab != null)
                info.weaponPrefab = def.unitPrefab;
            return info;
        }

        private static WeaponMount? CreateMount(
            Encyclopedia enc,
            MissileDefinition? def,
            WeaponInfo info,
            string jsonKey,
            int slots)
        {
            if (enc.weaponMounts == null || def?.unitPrefab == null)
                return null;

            if (Encyclopedia.WeaponLookup != null &&
                Encyclopedia.WeaponLookup.TryGetValue(jsonKey, out WeaponMount existing) &&
                existing != null &&
                existing.prefab != null &&
                PrefabFactory.IsOurMountKey(existing.jsonKey))
            {
                RefreshMount(existing, info, slots);
                return existing;
            }

            WeaponMount? donor = AgrDonor.FindAgr24MountWithSlots(enc, SledgepinConstants.SlotCount4) ??
                                 AgrDonor.FindAgr24MountWithSlots(enc, 1);
            if (donor?.prefab == null)
            {
                SledgepinPlugin.ModLog?.LogError($"Sledgepin: no AGR-24 mount donor for '{jsonKey}'.");
                return null;
            }

            WeaponMount mount = ScriptableObject.CreateInstance<WeaponMount>();
            mount.name = jsonKey;
            mount.jsonKey = jsonKey;
            mount.mountName = SledgepinConstants.MountDisplayName;
            PrefabFactory.CopyMountScalars(donor, mount);
            MountDisabled?.SetValue(mount, false);
            mount.info = info;
            mount.sortWeapons = true;
            mount.missileBay = false;
            mount.emptyMass = slots >= SledgepinConstants.SlotCount6
                ? SledgepinConstants.MountEmptyMass6Kg
                : SledgepinConstants.MountEmptyMass4Kg;

            GameObject mountGo = PrefabFactory.CloneAsPrefab(donor.prefab, jsonKey + "_Prefab");
            StampMount(mountGo, slots);
            mount.prefab = mountGo;
            BindMountedInfo(mount, info);

            int ammo = mountGo.GetComponentsInChildren<Weapon>(true).Length;
            if (ammo < 1)
                ammo = slots;
            mount.ammo = ammo;
            mount.mass = mount.emptyMass + SledgepinConstants.LaunchMassKg * ammo;
            mount.RCS = SledgepinConstants.RadarSize;

            enc.weaponMounts ??= new List<WeaponMount>();
            if (!enc.weaponMounts.Contains(mount))
                enc.weaponMounts.Add(mount);
            Encyclopedia.WeaponLookup ??= new Dictionary<string, WeaponMount>(StringComparer.Ordinal);
            Encyclopedia.WeaponLookup[mount.jsonKey] = mount;
            List<INetworkDefinition>? idx = enc.IndexLookup;
            if (idx != null && !PrefabFactory.ContainsNet(idx, mount))
            {
                idx.Add(mount);
                ((INetworkDefinition)mount).LookupIndex = idx.Count - 1;
            }

            try { mount.Initialize(); }
            catch (Exception ex) { SledgepinPlugin.ModLog?.LogWarning($"Sledgepin Initialize '{jsonKey}': {ex.Message}"); }

            mount.jsonKey = jsonKey;
            mount.info = info;
            mount.sortWeapons = true;
            mount.mountName = string.Format("{0} x{1}", SledgepinConstants.MountDisplayName, ammo);
            BindMountedInfo(mount, info);
            SledgepinPlugin.ModLog?.LogInfo($"Sledgepin mount '{jsonKey}' ammo={ammo} from '{donor.jsonKey}'.");
            return mount;
        }

        private static void RefreshMount(WeaponMount mount, WeaponInfo info, int slots)
        {
            NobpContent.TryLoad();
            if (mount.prefab != null)
                StampMount(mount.prefab, slots);
            mount.info = info;
            mount.sortWeapons = true;
            if (Definition?.unitPrefab != null)
                info.weaponPrefab = Definition.unitPrefab;
            BindMountedInfo(mount, info);
            mount.RCS = SledgepinConstants.RadarSize;
        }

        private static void StampMount(GameObject mountGo, int slots)
        {
            NobpContent.TryLoad();
            GameObject? launcher = NobpContent.LauncherForSlots(slots);
            if (launcher == null || mountGo == null)
                return;
            VisualStamp.StampMountTemplate(mountGo, launcher, slots);
        }

        private static void BindMountedInfo(WeaponMount mount, WeaponInfo info)
        {
            if (mount.prefab == null)
                return;
            foreach (MountedMissile mm in mount.prefab.GetComponentsInChildren<MountedMissile>(true))
            {
                if (mm != null)
                    mm.info = info;
            }
        }
    }
}
