using UnityEngine;

namespace Sledgepin.Runtime
{
    // Maps MountedMissile slot → embedded Rocket under launcher (baked on mount stamp).
    internal sealed class SledgepinSlotLink : MonoBehaviour
    {
        [SerializeField] internal Transform? Embedded;
    }
}
