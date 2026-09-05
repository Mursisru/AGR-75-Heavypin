using Mirage;
using Sledgepin.Bootstrap;
using Sledgepin.Runtime;
using UnityEngine;

namespace Sledgepin
{
    internal static class VisualStamp
    {
        private static float _sharedUniform = SledgepinConstants.VisualUniformScale;
        private static bool _uniformReady;

        private static readonly Quaternion MountYaw =
            Quaternion.Euler(0f, SledgepinConstants.VisualMountYawDeg, 0f);

        internal static Transform? FindRocket(Transform root) => PrefabFactory.FindRocketVisual(root);
        internal static Transform? FindLauncher(Transform root) => PrefabFactory.FindLauncherVisual(root);

        // Mount: launcher only. Embedded "Rocket" children are setup in SledgepinLauncherRockets.
        internal static void StampMountTemplate(GameObject mountGo, GameObject? launcherPrefab, int slots)
        {
            if (mountGo == null || launcherPrefab == null)
                return;

            EnsureSharedUniform(launcherPrefab);

            Transform? launcher = FindLauncher(mountGo.transform);
            if (launcher == null)
            {
                launcher = StampNamed(mountGo, launcherPrefab, SledgepinConstants.LauncherVisualName);
                if (launcher == null)
                {
                    SledgepinPlugin.ModLog?.LogWarning("Sledgepin: launcher stamp failed.");
                    return;
                }
            }

            SlotLayout.EnsureCount(mountGo, slots);
            ConfigureLauncher(launcher, launcherPrefab);
            SlotLayout.PlaceOnDummies(mountGo, launcher, slots);
            SledgepinLauncherRockets.SetupMount(mountGo, launcher, slots);

            StockVisual.Hide(mountGo);
            mountGo.SetActive(false);
            NetworkPrefabPrep.PrepareTemplate(mountGo);
            SledgepinPlugin.ModLog?.LogInfo($"Sledgepin mount stamp slots={slots} uniform={_sharedUniform:F4}");
        }

        // Fly / encyclopedia: stamp SledgepinRocket on shared unitPrefab shell.
        internal static bool StampRocket(GameObject host, GameObject? rocketPrefab)
        {
            if (host == null || rocketPrefab == null)
                return false;
            if (SledgepinSpawnGate.IsSharedShell(host))
                return false;

            EnsureSharedUniform(rocketPrefab);

            Transform? vis = FindRocket(host.transform);
            if (vis == null)
            {
                vis = StampNamed(host, rocketPrefab, SledgepinConstants.RocketVisualName);
                if (vis == null)
                    return false;
            }

            ConfigureFlyRocket(vis, rocketPrefab);
            SledgepinTag? tag = host.GetComponent<SledgepinTag>() ?? vis.GetComponentInParent<SledgepinTag>();
            if (tag == null || !tag.FinsOpen)
                SledgepinAnim.Park(vis);
            VisualMaterials.ApplyFbxLook(vis.gameObject, flyRocket: true);
            StockVisual.Hide(host);
            return true;
        }

        internal static void ConfigureFlyRocket(Transform vis, GameObject? rocketPrefab = null)
        {
            if (vis == null)
                return;

            EnsureSharedUniform(rocketPrefab);
            vis.localPosition = Vector3.zero;
            vis.localRotation = MountYaw;
            vis.localScale = Vector3.one * _sharedUniform;

            Transform? center = DummyFind.FindRocketCenter(vis);
            if (center == null)
                SledgepinPlugin.ModLog?.LogWarning("Sledgepin: CenterOfModel missing on fly rocket.");
            else
                DummySnap.AlignDummyToParent(vis, center);
        }

        internal static void ConfigureLauncher(Transform launcher, GameObject? launcherPrefab = null)
        {
            if (launcher == null)
                return;

            launcher.localPosition = Vector3.zero;
            launcher.localRotation = MountYaw;
            launcher.localScale = Vector3.one * _sharedUniform;

            Transform? attach = DummyFind.FindPylonAttach(launcher);
            if (attach != null)
                DummySnap.AlignDummyToParent(launcher, attach);
            else
                SledgepinPlugin.ModLog?.LogWarning("Sledgepin: PlaceOfDocking missing on launcher.");

            Transform? mount = launcher.parent;
            Vector3 up = mount != null ? mount.up : Vector3.up;
            Vector3 fwd = mount != null ? mount.forward : Vector3.forward;
            launcher.position += up * SledgepinConstants.LauncherLiftM + fwd * SledgepinConstants.LauncherForwardM;
        }

        private static void EnsureSharedUniform(GameObject? prefab)
        {
            if (_uniformReady)
                return;

            float baked = 0f;
            if (prefab != null)
                baked = prefab.transform.localScale.x;

            if (baked > 0.001f && Mathf.Abs(baked - 1f) > 0.001f)
            {
                _sharedUniform = baked;
                _uniformReady = true;
                SledgepinPlugin.ModLog?.LogInfo($"Sledgepin uniform from bake={_sharedUniform:F4}");
                return;
            }

            if (prefab == null)
            {
                _sharedUniform = SledgepinConstants.VisualUniformScale;
                _uniformReady = true;
                return;
            }

            GameObject? probe = null;
            try
            {
                probe = Object.Instantiate(prefab);
                probe.transform.SetParent(null, false);
                probe.transform.position = Vector3.zero;
                probe.transform.rotation = Quaternion.identity;
                probe.transform.localScale = Vector3.one;
                probe.SetActive(true);
                float native = VisualMeasure.Longest(probe.transform);
                _sharedUniform = native > 0.05f
                    ? Mathf.Clamp(SledgepinConstants.LengthM / native, 0.01f, 2f)
                    : SledgepinConstants.VisualUniformScale;
                SledgepinPlugin.ModLog?.LogInfo(
                    $"Sledgepin uniform measured native={native:F3}m → {_sharedUniform:F4}");
            }
            finally
            {
                if (probe != null)
                    Object.Destroy(probe);
            }
            _uniformReady = true;
        }

        private static Transform? StampNamed(GameObject host, GameObject visualPrefab, string name)
        {
            Transform parent = host.transform;
            GameObject vis = Object.Instantiate(visualPrefab, parent, false);
            vis.name = name;
            vis.hideFlags = HideFlags.None;
            vis.SetActive(true);
            vis.transform.localPosition = Vector3.zero;
            bool isRocket = name == SledgepinConstants.RocketVisualName;
            vis.transform.localRotation = MountYaw;
            vis.transform.localScale = Vector3.one * _sharedUniform;

            VisualMaterials.StripSceneJunk(vis);
            StripVisualPhysics(vis);
            VisualMaterials.MatchHostDrawState(vis, host);
            StockVisual.MarkOurs(vis.transform);

            if (isRocket)
                SledgepinAnim.Park(vis.transform);

            Renderer[] rs = vis.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < rs.Length; i++)
            {
                Renderer r = rs[i];
                if (r == null || !StockVisual.IsOurs(r))
                    continue;
                MeshFilter? mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh == null)
                    continue;
                if (r is SkinnedMeshRenderer skin && skin.sharedMesh == null)
                    continue;
                r.enabled = true;
            }
            if (!isRocket)
                VisualMaterials.ApplyFbxLook(vis);
            else
                VisualMaterials.ApplyFbxLook(vis);
            return vis.transform;
        }

        private static void StripVisualPhysics(GameObject vis)
        {
            Collider[] cols = vis.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null)
                    cols[i].enabled = false;
            }
            NetworkIdentity[] ids = vis.GetComponentsInChildren<NetworkIdentity>(true);
            for (int i = 0; i < ids.Length; i++)
            {
                if (ids[i] != null)
                    ids[i].enabled = false;
            }
        }
    }
}
