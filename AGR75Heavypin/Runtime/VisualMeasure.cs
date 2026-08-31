using UnityEngine;

namespace Heavypin.Runtime
{
    // One-shot mesh AABB (no alloc in hot paths — stamp only).
    internal static class VisualMeasure
    {
        internal static float Longest(Transform root)
        {
            if (root == null)
                return 0f;
            bool any = false;
            Bounds world = default;

            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                MeshFilter mf = filters[i];
                if (mf == null || mf.sharedMesh == null || mf.transform == null)
                    continue;
                Encapsulate(mf.sharedMesh, mf.transform.localToWorldMatrix, ref world, ref any);
            }

            SkinnedMeshRenderer[] skins = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skins.Length; i++)
            {
                SkinnedMeshRenderer skin = skins[i];
                if (skin == null || skin.sharedMesh == null || skin.transform == null)
                    continue;
                Encapsulate(skin.sharedMesh, skin.transform.localToWorldMatrix, ref world, ref any);
            }

            if (!any)
                return 0f;
            Vector3 size = world.size;
            return Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        }

        private static void Encapsulate(Mesh mesh, Matrix4x4 toWorld, ref Bounds world, ref bool any)
        {
            Bounds lb = mesh.bounds;
            Vector3 min = lb.min;
            Vector3 max = lb.max;
            for (int ix = 0; ix < 2; ix++)
            for (int iy = 0; iy < 2; iy++)
            for (int iz = 0; iz < 2; iz++)
            {
                Vector3 p = toWorld.MultiplyPoint3x4(new Vector3(
                    ix == 0 ? min.x : max.x,
                    iy == 0 ? min.y : max.y,
                    iz == 0 ? min.z : max.z));
                if (!any)
                {
                    world = new Bounds(p, Vector3.zero);
                    any = true;
                }
                else
                    world.Encapsulate(p);
            }
        }
    }
}
