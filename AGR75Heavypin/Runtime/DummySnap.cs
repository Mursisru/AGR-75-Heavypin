using UnityEngine;

namespace Heavypin.Runtime
{
    internal static class DummySnap
    {
        internal static void AlignDummyPosition(Transform mover, Transform dummy, Vector3 worldPos)
        {
            if (mover == null || dummy == null)
                return;
            mover.position += worldPos - dummy.position;
        }

        internal static void AlignDummyToParent(Transform mover, Transform dummy)
        {
            if (mover == null || dummy == null)
                return;
            Transform? parent = mover.parent;
            Vector3 worldPos = parent != null ? parent.position : Vector3.zero;
            AlignDummyPosition(mover, dummy, worldPos);
        }
    }
}
