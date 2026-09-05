using System;
using System.Collections.Generic;
using Sledgepin.Bootstrap;
using UnityEngine;

namespace Sledgepin.Runtime
{
    // Embedded "Rocket" meshes inside launcher prefab (not stamped on MountedMissile).
    internal static class SledgepinLauncherRockets
    {
        internal static void SetupMount(GameObject mountGo, Transform launcher, int want)
        {
            if (mountGo == null || launcher == null || want < 1)
                return;

            List<Transform> embedded = FindEmbedded(launcher, want);
            for (int i = 0; i < embedded.Count; i++)
            {
                Transform rocket = embedded[i];
                if (rocket == null)
                    continue;
                rocket.gameObject.SetActive(true);
                StockVisual.MarkOurs(rocket);
                VisualMaterials.ApplyFbxLook(rocket.gameObject);
                SledgepinAnim.Park(rocket);
            }

            LinkSlots(mountGo, embedded);
            SledgepinPlugin.ModLog?.LogInfo(
                $"SledgepinLauncherRockets setup embedded={embedded.Count} want={want} launcher='{launcher.name}'");
        }

        internal static void LinkSlots(GameObject mountGo, List<Transform> embedded)
        {
            if (mountGo == null || embedded == null || embedded.Count == 0)
                return;
            MountedMissile[] slots = mountGo.GetComponentsInChildren<MountedMissile>(true);
            int n = Mathf.Min(slots.Length, embedded.Count);
            for (int i = 0; i < n; i++)
            {
                MountedMissile? slot = slots[i];
                if (slot == null)
                    continue;
                SledgepinSlotLink link = slot.GetComponent<SledgepinSlotLink>() ?? slot.gameObject.AddComponent<SledgepinSlotLink>();
                link.Embedded = embedded[i];
            }
        }

        internal static void EnsureSlotLinks(GameObject mountGo)
        {
            if (mountGo == null)
                return;
            Transform? launcher = PrefabFactory.FindLauncherVisual(mountGo.transform);
            if (launcher == null)
                return;
            MountedMissile[] slots = mountGo.GetComponentsInChildren<MountedMissile>(true);
            if (slots.Length == 0)
                return;
            for (int i = 0; i < slots.Length; i++)
            {
                SledgepinSlotLink? link = slots[i]?.GetComponent<SledgepinSlotLink>();
                if (link != null && link.Embedded != null)
                    continue;
                List<Transform> embedded = FindEmbedded(launcher, slots.Length);
                LinkSlots(mountGo, embedded);
                return;
            }
        }

        internal static void EnableEmbedded(GameObject mountGo)
        {
            Transform? launcher = PrefabFactory.FindLauncherVisual(mountGo.transform);
            if (launcher == null)
                return;
            List<Transform> embedded = FindEmbedded(launcher, 8);
            for (int i = 0; i < embedded.Count; i++)
                SetEmbeddedVisible(embedded[i], true);
        }

        internal static void ParkEmbedded(GameObject mountGo)
        {
            Transform? launcher = PrefabFactory.FindLauncherVisual(mountGo.transform);
            if (launcher == null)
                return;
            List<Transform> embedded = FindEmbedded(launcher, 8);
            for (int i = 0; i < embedded.Count; i++)
            {
                if (embedded[i] != null)
                    SledgepinAnim.Park(embedded[i]);
            }
        }

        internal static void HideEmbedded(MountedMissile? mount)
        {
            Transform? rocket = ResolveEmbedded(mount);
            if (rocket == null)
            {
                SledgepinPlugin.ModLog?.LogWarning($"Sledgepin: HideEmbedded miss slot='{mount?.name}'");
                return;
            }
            SetEmbeddedVisible(rocket, false);
        }

        internal static void RestoreEmbedded(MountedMissile? mount)
        {
            Transform? rocket = ResolveEmbedded(mount);
            if (rocket == null)
                return;
            SetEmbeddedVisible(rocket, true);
            SledgepinAnim.Park(rocket);
        }

        internal static Transform? ResolveEmbedded(MountedMissile? mount)
        {
            if (mount == null)
                return null;
            SledgepinSlotLink? link = mount.GetComponent<SledgepinSlotLink>();
            if (link?.Embedded != null)
                return link.Embedded;
            return FindEmbeddedForMount(mount);
        }

        private static void SetEmbeddedVisible(Transform? rocket, bool visible)
        {
            if (rocket == null)
                return;
            rocket.gameObject.SetActive(visible);
            Renderer[] rs = rocket.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] != null)
                    rs[i].enabled = visible;
            }
        }

        internal static Transform? FindEmbeddedForMount(MountedMissile? mount)
        {
            if (mount == null)
                return null;

            Transform? scope = FindMountScope(mount.transform);
            Transform? launcher = scope != null ? PrefabFactory.FindLauncherVisual(scope) : null;
            if (launcher == null)
                return null;

            MountedMissile[] slots = scope!.GetComponentsInChildren<MountedMissile>(true);
            int index = IndexOfSlot(slots, mount);
            if (index < 0)
                return null;

            List<Transform> embedded = FindEmbedded(launcher, slots.Length);
            return index < embedded.Count ? embedded[index] : null;
        }

        internal static Transform? FindMountScope(Transform slot)
        {
            if (slot == null)
                return null;
            Transform t = slot;
            while (t != null)
            {
                if (PrefabFactory.FindLauncherVisual(t) != null)
                    return t;
                t = t.parent;
            }
            return slot.root;
        }

        private static int IndexOfSlot(MountedMissile[] slots, MountedMissile mount)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (ReferenceEquals(slots[i], mount))
                    return i;
            }
            return -1;
        }

        internal static List<Transform> FindEmbedded(Transform launcher, int want)
        {
            var list = new List<Transform>(want);
            if (launcher == null)
                return list;

            List<Transform> dummies = DummyFind.FindRocketSlots(launcher);
            for (int i = 0; i < dummies.Count && list.Count < want; i++)
            {
                Transform? near = FindRocketNearDummy(dummies[i]);
                if (near != null && !list.Contains(near))
                    list.Add(near);
            }

            if (list.Count >= want)
                return list;

            Transform[] all = launcher.GetComponentsInChildren<Transform>(true);
            var fallback = new List<Transform>(want);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t == launcher || !IsEmbeddedRocketName(t.name))
                    continue;
                if (t.name == SledgepinConstants.RocketVisualName)
                    continue;
                fallback.Add(t);
            }
            fallback.Sort((a, b) => DummyFind.CompareDummyName(a, b));

            for (int i = 0; i < fallback.Count && list.Count < want; i++)
            {
                if (!list.Contains(fallback[i]))
                    list.Add(fallback[i]);
            }
            return list;
        }

        // FBX: Rocket / Rocket.00N are siblings of CentarOfDockingAGRRocket*, not children.
        private static Transform? FindRocketNearDummy(Transform dummy)
        {
            if (dummy == null)
                return null;
            for (int i = 0; i < dummy.childCount; i++)
            {
                Transform ch = dummy.GetChild(i);
                if (ch != null && IsEmbeddedRocketName(ch.name))
                    return ch;
            }

            Transform? parent = dummy.parent;
            if (parent == null)
                return null;
            int key = DummyFind.DummySortKey(dummy.name);
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform ch = parent.GetChild(i);
                if (ch == null || ch == dummy || !IsEmbeddedRocketName(ch.name))
                    continue;
                if (DummyFind.DummySortKey(ch.name) == key)
                    return ch;
            }
            return null;
        }

        internal static bool IsEmbeddedRocketName(string? name)
        {
            if (name is not { Length: > 0 } n)
                return false;
            const string root = SledgepinConstants.LauncherEmbeddedRocketName;
            if (string.Equals(n, root, StringComparison.OrdinalIgnoreCase))
                return true;
            return n.StartsWith(root + ".", StringComparison.OrdinalIgnoreCase);
        }
    }
}
