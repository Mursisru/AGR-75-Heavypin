using UnityEngine;

namespace Heavypin.Runtime
{
    internal static class SlotLayout
    {
        internal static void EnsureCount(GameObject mountGo, int want)
        {
            if (mountGo == null || want < 1)
                return;
            MountedMissile[] slots = mountGo.GetComponentsInChildren<MountedMissile>(true);
            if (slots.Length == 0)
            {
                HeavypinPlugin.ModLog?.LogError("Heavypin: donor mount has no MountedMissile.");
                return;
            }

            if (slots.Length > want)
            {
                for (int i = want; i < slots.Length; i++)
                {
                    if (slots[i] != null)
                        Object.DestroyImmediate(slots[i].gameObject);
                }
                return;
            }

            Transform parent = slots[0].transform.parent;
            for (int i = slots.Length; i < want; i++)
            {
                GameObject extra = Object.Instantiate(slots[0].gameObject, parent, false);
                extra.name = slots[0].gameObject.name + "_" + i;
            }
        }

        internal static void PlaceOnDummies(GameObject mountGo, Transform launcher, int want)
        {
            if (mountGo == null || launcher == null)
                return;
            var dummies = DummyFind.FindRocketSlots(launcher);
            MountedMissile[] slots = mountGo.GetComponentsInChildren<MountedMissile>(true);
            if (dummies.Count < want)
                HeavypinPlugin.ModLog?.LogWarning(
                    $"Heavypin: launcher dummies={dummies.Count} want={want} on '{launcher.name}'");

            int n = Mathf.Min(want, Mathf.Min(dummies.Count, slots.Length));
            for (int i = 0; i < n; i++)
            {
                Transform dummy = dummies[i];
                Transform slot = slots[i].transform;
                if (dummy == null || slot == null)
                    continue;
                slot.position = dummy.position;
                slot.rotation = launcher.rotation;
            }
        }
    }
}
