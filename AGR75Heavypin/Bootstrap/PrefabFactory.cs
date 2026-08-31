using System;
using System.Collections.Generic;
using Mirage;
using Heavypin.Runtime;
using UnityEngine;

namespace Heavypin.Bootstrap
{
    internal static class PrefabFactory
    {
        internal static GameObject CloneAsPrefab(GameObject source, string name)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            GameObject clone = UnityEngine.Object.Instantiate(source);
            clone.name = name;
            NetworkPrefabPrep.PrepareTemplate(clone);
            UnityEngine.Object.DontDestroyOnLoad(clone);
            ResetPrefabTransform(clone);
            FreezeTemplatePhysics(clone);
            clone.SetActive(false);
            NetworkPrefabPrep.PrepareTemplate(clone);
            return clone;
        }

        internal static void ResetPrefabTransform(GameObject go)
        {
            if (go == null)
                return;
            go.hideFlags = HideFlags.None;
            go.transform.SetParent(null, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
        }

        internal static void FreezeTemplatePhysics(GameObject root)
        {
            if (root == null)
                return;
            foreach (Rigidbody rb in root.GetComponentsInChildren<Rigidbody>(true))
            {
                if (rb == null)
                    continue;
                rb.detectCollisions = false;
                rb.isKinematic = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            foreach (Camera cam in root.GetComponentsInChildren<Camera>(true))
            {
                if (cam != null)
                    cam.enabled = false;
            }
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (light != null)
                    light.enabled = false;
            }
        }

        internal static void ActivateMountedInstance(GameObject instance)
        {
            if (instance == null)
                return;
            instance.hideFlags = HideFlags.None;
            instance.SetActive(true);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            WakeMountedSlots(instance);
            foreach (Rigidbody rb in instance.GetComponentsInChildren<Rigidbody>(true))
            {
                if (rb == null)
                    continue;
                rb.isKinematic = true;
                rb.detectCollisions = false;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            EnableGameplayBehaviours(instance);
            EnsureVisualRenderers(instance);
            StockVisual.Hide(instance);
        }

        private static void WakeMountedSlots(GameObject instance)
        {
            MountedMissile[] slots = instance.GetComponentsInChildren<MountedMissile>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slots[i].gameObject.SetActive(true);
            }
        }

        private static void EnableGameplayBehaviours(GameObject root)
        {
            foreach (Behaviour b in root.GetComponentsInChildren<Behaviour>(true))
            {
                if (b == null)
                    continue;
                if (b is NetworkIdentity || b is NetworkBehaviour)
                    continue;
                string tn = b.GetType().Name;
                if (tn == "Camera" || tn == "AudioListener" || tn == "Flare" || tn == "Light" ||
                    tn == "ReflectionProbe" || tn == "Skybox")
                {
                    b.enabled = false;
                    continue;
                }
                if (tn == "Missile" || tn.EndsWith("Seeker", StringComparison.Ordinal))
                {
                    b.enabled = false;
                    continue;
                }
                b.enabled = true;
            }
            VisualMaterials.StripSceneJunk(root);
        }

        internal static Transform? FindVisual(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return null;
            Transform direct = root.Find(name);
            if (direct != null)
                return direct;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == name)
                    return all[i];
            }
            return null;
        }

        internal static Transform? FindRocketVisual(Transform root) =>
            FindVisual(root, HeavypinConstants.RocketVisualName);

        internal static Transform? FindLauncherVisual(Transform root) =>
            FindVisual(root, HeavypinConstants.LauncherVisualName);

        internal static bool IsOurVisualRoot(Transform t)
        {
            while (t != null)
            {
                string n = t.name;
                if (n == HeavypinConstants.RocketVisualName || n == HeavypinConstants.LauncherVisualName)
                    return true;
                t = t.parent;
            }
            return false;
        }

        private static void EnsureVisualRenderers(GameObject root)
        {
            EnableVis(FindLauncherVisual(root.transform));
            MountedMissile[] slots = root.GetComponentsInChildren<MountedMissile>(true);
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    EnableVis(FindRocketVisual(slots[i].transform));
            }
        }

        private static void EnableVis(Transform? vis)
        {
            if (vis == null)
                return;
            vis.gameObject.SetActive(true);
            Renderer[] rs = vis.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] != null && StockVisual.IsOurs(rs[i]))
                    rs[i].enabled = true;
            }
        }

        internal static WeaponMount? FindMountByExactKey(Encyclopedia enc, string jsonKey)
        {
            if (string.IsNullOrEmpty(jsonKey))
                return null;
            if (Encyclopedia.WeaponLookup != null &&
                Encyclopedia.WeaponLookup.TryGetValue(jsonKey, out WeaponMount m) &&
                m != null)
                return m;
            if (enc?.weaponMounts == null)
                return null;
            foreach (WeaponMount w in enc.weaponMounts)
            {
                if (w != null && string.Equals(w.jsonKey, jsonKey, StringComparison.Ordinal))
                    return w;
            }
            return null;
        }

        internal static bool IsOurMountKey(string? jsonKey) =>
            !string.IsNullOrEmpty(jsonKey) &&
            jsonKey!.StartsWith(HeavypinConstants.MountKeyPrefix, StringComparison.Ordinal);

        internal static void CopyMountScalars(WeaponMount src, WeaponMount dst)
        {
            dst.ammo = src.ammo;
            dst.turret = src.turret;
            dst.missileBay = src.missileBay;
            dst.radar = false;
            dst.tailHook = false;
            dst.slingloadHook = false;
            dst.countermeasure = false;
            dst.colorable = src.colorable;
            dst.Cargo = false;
            dst.Troops = false;
            dst.sortWeapons = true;
            dst.GearSafety = src.GearSafety;
            dst.GroundSafety = src.GroundSafety;
            dst.GunAmmo = false;
            dst.emptyCost = src.emptyCost;
            dst.emptyMass = src.emptyMass;
            dst.mass = src.mass;
            dst.drag = src.drag;
            dst.emptyDrag = src.emptyDrag;
            dst.RCS = src.RCS;
            dst.emptyRCS = src.emptyRCS;
            dst.dontAutomaticallyAddToEncyclopedia = false;
        }

        internal static void CopyUnitDefScalars(UnitDefinition src, UnitDefinition dst)
        {
            dst.visibleRange = src.visibleRange;
            dst.iconRange = src.iconRange;
            dst.iconSize = src.iconSize;
            dst.mapIconSize = src.mapIconSize;
            dst.captureStrength = 0f;
            dst.captureDefense = 0f;
            dst.manpower = 0f;
            dst.armorTier = src.armorTier;
            dst.damageTolerance = src.damageTolerance;
            dst.minEditorHeight = src.minEditorHeight;
            dst.maxEditorHeight = src.maxEditorHeight;
            dst.code = src.code;
            dst.spawnOffset = src.spawnOffset;
        }

        internal static void CopyMapIdentity(UnitDefinition src, UnitDefinition dst)
        {
            dst.mapIcon = src.mapIcon;
            dst.friendlyIcon = src.friendlyIcon;
            dst.hostileIcon = src.hostileIcon;
            dst.mapOrient = src.mapOrient;
            dst.mapIconSize = src.mapIconSize;
            dst.typeIdentity = src.typeIdentity;
            dst.roleIdentity = src.roleIdentity;
            dst.IsObstacle = false;
        }

        internal static bool ContainsNet(List<INetworkDefinition> list, INetworkDefinition item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item))
                    return true;
            }
            return false;
        }

        internal static void AssertDonorsIntact(Encyclopedia enc)
        {
            if (enc == null)
                return;
            if (enc.missiles != null)
            {
                foreach (MissileDefinition m in enc.missiles)
                {
                    if (m == null || !AgrDonor.IsAgr24Def(m))
                        continue;
                    if (!string.IsNullOrEmpty(m.unitName) &&
                        m.unitName.IndexOf("Heavypin", StringComparison.OrdinalIgnoreCase) >= 0)
                        HeavypinPlugin.ModLog?.LogError($"AGR-24 unitName mutated: '{m.unitName}' key='{m.jsonKey}'");
                }
            }
            if (enc.weaponMounts == null)
                return;
            foreach (WeaponMount m in enc.weaponMounts)
            {
                if (m == null || IsOurMountKey(m.jsonKey))
                    continue;
                if (!AgrDonor.IsAgrMount(m))
                    continue;
                if (m.mountName != null &&
                    m.mountName.IndexOf("Heavypin", StringComparison.OrdinalIgnoreCase) >= 0)
                    HeavypinPlugin.ModLog?.LogError($"Donor corrupted: mountName '{m.mountName}' on '{m.jsonKey}'");
                if (m.info != null &&
                    !string.IsNullOrEmpty(m.info.weaponName) &&
                    m.info.weaponName.IndexOf("Heavypin", StringComparison.OrdinalIgnoreCase) >= 0)
                    HeavypinPlugin.ModLog?.LogError($"Donor WeaponInfo mutated: '{m.info.weaponName}' on '{m.jsonKey}'");
            }
        }
    }
}
