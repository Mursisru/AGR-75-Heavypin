using System;
using System.Collections.Generic;
using UnityEngine;

namespace Heavypin.Runtime
{
    internal static class DummyFind
    {
        internal static Transform? FindExact(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return null;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t != null && string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase))
                    return t;
            }
            return null;
        }

        internal static Transform? FindByAliases(Transform root, string[] aliases)
        {
            if (root == null || aliases == null || aliases.Length == 0)
                return null;
            Transform? exact = null;
            Transform? contains = null;
            FindRecursive(root, aliases, ref exact, ref contains);
            return exact != null ? exact : contains;
        }

        internal static Transform? FindRocketCenter(Transform root) =>
            FindByAliases(root, HeavypinConstants.RocketCenterAliases);

        internal static Transform? FindPylonAttach(Transform root) =>
            FindByAliases(root, HeavypinConstants.AttachPylonAliases);

        internal static List<Transform> FindRocketSlots(Transform root)
        {
            var list = new List<Transform>(8);
            if (root == null)
                return list;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || !IsRocketSlotName(t.name))
                    continue;
                list.Add(t);
            }
            list.Sort(CompareDummyName);
            return list;
        }

        internal static List<Transform> FindNozzles(Transform root)
        {
            var list = new List<Transform>(4);
            if (root == null)
                return list;
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || !IsNozzleName(t.name))
                    continue;
                list.Add(t);
            }
            list.Sort(CompareDummyName);
            return list;
        }

        internal static bool IsRocketSlotName(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            if (string.Equals(name, HeavypinConstants.DummyPylonAttach, StringComparison.OrdinalIgnoreCase))
                return false;
            return StartsWith(name!, HeavypinConstants.DummySlot4Prefix) ||
                   StartsWith(name!, HeavypinConstants.DummySlot6Prefix);
        }

        internal static bool IsNozzleName(string? name)
        {
            return !string.IsNullOrEmpty(name) && StartsWith(name!, HeavypinConstants.DummyNozzlePrefix);
        }

        private static bool StartsWith(string name, string prefix) =>
            name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

        private static int CompareDummyName(Transform a, Transform b)
        {
            return DummySortKey(a.name).CompareTo(DummySortKey(b.name));
        }

        private static int DummySortKey(string name)
        {
            int dot = name.LastIndexOf('.');
            if (dot < 0 || dot >= name.Length - 1)
                return 0;
            int n;
            return int.TryParse(name.Substring(dot + 1), out n) ? n : 0;
        }

        private static void FindRecursive(Transform t, string[] aliases, ref Transform? exact, ref Transform? contains)
        {
            string n = t.name ?? string.Empty;
            for (int i = 0; i < aliases.Length; i++)
            {
                string a = aliases[i];
                if (string.IsNullOrEmpty(a))
                    continue;
                if (string.Equals(n, a, StringComparison.OrdinalIgnoreCase))
                {
                    exact = t;
                    return;
                }
                if (contains == null && n.IndexOf(a, StringComparison.OrdinalIgnoreCase) >= 0)
                    contains = t;
            }
            for (int i = 0; i < t.childCount; i++)
            {
                FindRecursive(t.GetChild(i), aliases, ref exact, ref contains);
                if (exact != null)
                    return;
            }
        }
    }
}
