using UnityEngine;

namespace Heavypin.Runtime
{
    // Maps MountedMissile slot → embedded Rocket under launcher (baked on mount stamp).
    internal sealed class HeavypinSlotLink : MonoBehaviour
    {
        [SerializeField] internal Transform? Embedded;
    }
}
