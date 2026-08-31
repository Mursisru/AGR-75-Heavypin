using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Heavypin.Blueprinter;
using Heavypin.Bootstrap;
using Heavypin.Patches;
using Heavypin.Runtime;
using UnityEngine;

namespace Heavypin
{
    internal static class HeavypinBootstrap
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

                HeavypinMotorFx.Capture(enc, agr24);

                if (Encyclopedia.Lookup != null &&
                    Encyclopedia.Lookup.TryGetValue(HeavypinConstants.MissileJsonKey, out UnitDefinition existing) &&
                    existing is MissileDefinition md && md.unitPrefab != null)
                {
                    Definition = md;
                    GameObject? shellGo = HeavypinFlyFactory.BindSharedShell(agr24 ?? md);
                    if (shellGo != null)
                        md.unitPrefab = shellGo;
                    ApplySize(md);
                }
                else
                    Definition = CreateDefinition(enc, agr24);

                HeavypinDefinitionMass.Apply(Definition, HeavypinConstants.LaunchMassKg);
                HeavypinCalcProxy.Init(enc);

                Info = CreateSharedInfo(enc, Definition);
                Mount4x = CreateMount(enc, Definition, Info, HeavypinConstants.MountJsonKey4x, HeavypinConstants.SlotCount4);
                Mount6x = CreateMount(enc, Definition, Info, HeavypinConstants.MountJsonKey6x, HeavypinConstants.SlotCount6);

                if (Mount4x != null || Mount6x != null)
                    HardpointInjector.Inject(enc, Mount4x, Mount6x);

                PrefabFactory.AssertDonorsIntact(enc);
                _done = Definition != null && Info != null && (Mount4x != null || Mount6x != null);
                HeavypinPlugin.ModLog?.LogInfo(_done
                    ? $"AGR-75 Heavypin ready def={HeavypinConstants.MissileJsonKey} 4x={(Mount4x != null)} 6x={(Mount6x != null)} visual={(NobpContent.RocketPrefab != null)}"
                    : "AGR-75 Heavypin bootstrap incomplete.");
            }
            catch (Exception ex)
            {
                HeavypinPlugin.ModLog?.LogError($"HeavypinBootstrap: {ex}");
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
            if (missile.GetComponent<HeavypinTag>() != null)
                return true;
            WeaponInfo? wi = missile.GetWeaponInfo();
            if (IsOurInfo(wi))
                return true;
            return missile.definition != null &&
                   string.Equals(missile.definition.jsonKey, HeavypinConstants.MissileJsonKey, StringComparison.Ordinal);
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
            return string.Equals(info.weaponName, HeavypinConstants.WeaponInfoName, StringComparison.Ordinal) ||
                   string.Equals(info.shortName, HeavypinConstants.ShortName, StringComparison.Ordinal);
        }

        private static MissileDefinition? CreateDefinition(Encyclopedia enc, MissileDefinition? agr24)
        {
            if (agr24?.unitPrefab == null)
            {
                HeavypinPlugin.ModLog?.LogError("AGR-75 Heavypin: no AGR-24 unitPrefab.");
                return null;
            }

            MissileDefinition def = ScriptableObject.CreateInstance<MissileDefinition>();
            def.name = "MissilePack_AGR75Heavypin_Definition";
            def.jsonKey = HeavypinConstants.MissileJsonKey;
            PrefabFactory.CopyUnitDefScalars(agr24, def);
            PrefabFactory.CopyMapIdentity(agr24, def);
            def.unitName = HeavypinConstants.UnitName;
            def.bogeyName = HeavypinConstants.BogeyName;
            def.description = HeavypinConstants.Description;
            def.value = HeavypinConstants.Cost;
            def.mass = HeavypinConstants.LaunchMassKg;
            ApplySize(def);
            def.radarSize = HeavypinConstants.RadarSize;
            def.code = "MSL";
            def.IsObstacle = false;
            UnitDisabled?.SetValue(def, false);

            GameObject? fly = HeavypinFlyFactory.BindSharedShell(agr24);
            if (fly == null)
            {
                HeavypinPlugin.ModLog?.LogError("AGR-75 Heavypin: fly prefab bind failed.");
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

            HeavypinDefinitionMass.Apply(def, HeavypinConstants.LaunchMassKg);
            HeavypinPlugin.ModLog?.LogInfo($"Created AGR-75 Heavypin definition from shell '{agr24.jsonKey}'.");
            return def;
        }

        private static void ApplySize(MissileDefinition def)
        {
            def.length = HeavypinConstants.LengthM;
            def.width = HeavypinConstants.WidthM;
            def.height = HeavypinConstants.HeightM;
            if (def.spawnOffset.y < 0.05f)
                def.spawnOffset = new Vector3(def.spawnOffset.x, HeavypinConstants.HeightM * 0.5f, def.spawnOffset.z);
        }

        private static WeaponInfo CreateSharedInfo(Encyclopedia enc, MissileDefinition? def)
        {
            WeaponInfo info = ScriptableObject.CreateInstance<WeaponInfo>();
            info.name = "MissilePack_AGR75Heavypin_Info";
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
            tr.maxRange = HeavypinConstants.DesignRangeM;
            tr.minRange = HeavypinConstants.EncyclopediaMinRangeM;
            info.targetRequirements = tr;
            HeavypinEncyclopediaStats.ApplyTargetRequirements(info);

            Sprite? preview = HeavypinWeaponIcon.Get();
            if (preview != null)
                info.weaponIcon = preview;

            info.weaponName = HeavypinConstants.WeaponInfoName;
            info.shortName = HeavypinConstants.ShortName;
            info.description = HeavypinConstants.Description;
            info.massPerRound = HeavypinConstants.LaunchMassKg;
            info.costPerRound = HeavypinConstants.Cost;
            info.blastDamage = HeavypinConstants.BlastYieldKg;
            info.pK = HeavypinConstants.Pk;
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

            WeaponMount? donor = AgrDonor.FindAgr24MountWithSlots(enc, HeavypinConstants.SlotCount4) ??
                                 AgrDonor.FindAgr24MountWithSlots(enc, 1);
            if (donor?.prefab == null)
            {
                HeavypinPlugin.ModLog?.LogError($"Heavypin: no AGR-24 mount donor for '{jsonKey}'.");
                return null;
            }

            WeaponMount mount = ScriptableObject.CreateInstance<WeaponMount>();
            mount.name = jsonKey;
            mount.jsonKey = jsonKey;
            mount.mountName = HeavypinConstants.MountDisplayName;
            PrefabFactory.CopyMountScalars(donor, mount);
            MountDisabled?.SetValue(mount, false);
            mount.info = info;
            mount.sortWeapons = true;
            mount.missileBay = false;
            mount.emptyMass = slots >= HeavypinConstants.SlotCount6
                ? HeavypinConstants.MountEmptyMass6Kg
                : HeavypinConstants.MountEmptyMass4Kg;

            GameObject mountGo = PrefabFactory.CloneAsPrefab(donor.prefab, jsonKey + "_Prefab");
            StampMount(mountGo, slots);
            mount.prefab = mountGo;
            BindMountedInfo(mount, info);

            int ammo = mountGo.GetComponentsInChildren<Weapon>(true).Length;
            if (ammo < 1)
                ammo = slots;
            mount.ammo = ammo;
            mount.mass = mount.emptyMass + HeavypinConstants.LaunchMassKg * ammo;
            mount.RCS = HeavypinConstants.RadarSize;

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
            catch (Exception ex) { HeavypinPlugin.ModLog?.LogWarning($"Heavypin Initialize '{jsonKey}': {ex.Message}"); }

            mount.jsonKey = jsonKey;
            mount.info = info;
            mount.sortWeapons = true;
            mount.mountName = string.Format("{0} x{1}", HeavypinConstants.MountDisplayName, ammo);
            BindMountedInfo(mount, info);
            HeavypinPlugin.ModLog?.LogInfo($"Heavypin mount '{jsonKey}' ammo={ammo} from '{donor.jsonKey}'.");
            return mount;
        }

        private static void RefreshMount(WeaponMount mount, WeaponInfo info, int slots)
        {
            NobpContent.TryLoad();
            if (mount.prefab != null && NobpContent.RocketPrefab != null)
                StampMount(mount.prefab, slots);
            mount.info = info;
            mount.sortWeapons = true;
            if (Definition?.unitPrefab != null)
                info.weaponPrefab = Definition.unitPrefab;
            BindMountedInfo(mount, info);
            mount.RCS = HeavypinConstants.RadarSize;
        }

        private static void StampMount(GameObject mountGo, int slots)
        {
            NobpContent.TryLoad();
            GameObject? launcher = NobpContent.LauncherForSlots(slots);
            if (launcher == null || NobpContent.RocketPrefab == null || mountGo == null)
                return;
            VisualStamp.StampMountTemplate(mountGo, launcher, NobpContent.RocketPrefab, slots);
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
