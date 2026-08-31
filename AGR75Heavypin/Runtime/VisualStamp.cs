using Mirage;
using Heavypin.Bootstrap;
using Heavypin.Runtime;
using UnityEngine;

namespace Heavypin
{
    internal static class VisualStamp
    {
        // Same uniform on rocket + both launchers (Blender-matched).
        private static float _sharedUniform = HeavypinConstants.VisualUniformScale;
        private static bool _uniformReady;

        internal static Transform? FindRocket(Transform root) => PrefabFactory.FindRocketVisual(root);
        internal static Transform? FindLauncher(Transform root) => PrefabFactory.FindLauncherVisual(root);

        internal static void StampMountTemplate(GameObject mountGo, GameObject? launcherPrefab, GameObject? rocketPrefab, int slots)
        {
            if (mountGo == null || launcherPrefab == null || rocketPrefab == null)
                return;

            EnsureSharedUniform(rocketPrefab);

            Transform? launcher = FindLauncher(mountGo.transform);
            if (launcher == null)
            {
                launcher = StampNamed(mountGo, launcherPrefab, HeavypinConstants.LauncherVisualName);
                if (launcher == null)
                {
                    HeavypinPlugin.ModLog?.LogWarning("Heavypin: launcher stamp failed.");
                    return;
                }
            }

            SlotLayout.EnsureCount(mountGo, slots);

            MountedMissile[] mms = mountGo.GetComponentsInChildren<MountedMissile>(true);
            int n = 0;
            for (int i = 0; i < mms.Length; i++)
            {
                if (mms[i] != null && StampRocket(mms[i].gameObject, rocketPrefab))
                    n++;
            }

            ConfigureLauncher(launcher, launcherPrefab);
            SlotLayout.PlaceOnDummies(mountGo, launcher, slots);

            StockVisual.Hide(mountGo);
            mountGo.SetActive(false);
            NetworkPrefabPrep.PrepareTemplate(mountGo);
            HeavypinPlugin.ModLog?.LogInfo($"Heavypin mount stamp slots={n} want={slots} uniform={_sharedUniform:F4}");
        }

        internal static bool StampRocket(GameObject host, GameObject? rocketPrefab)
        {
            if (host == null || rocketPrefab == null)
                return false;
            if (HeavypinSpawnGate.IsSharedShell(host))
                return false;

            EnsureSharedUniform(rocketPrefab);

            Transform? vis = FindRocket(host.transform);
            if (vis == null)
            {
                vis = StampNamed(host, rocketPrefab, HeavypinConstants.RocketVisualName);
                if (vis == null)
                    return false;
            }

            ConfigureRocket(vis, rocketPrefab);
            HeavypinAnim.Park(vis); // Cube clips stay off on rail until DeployFins
            VisualMaterials.ApplyFbxLook(vis.gameObject);
            StockVisual.Hide(host);
            return true;
        }

        internal static void ConfigureRocket(Transform vis, GameObject? rocketPrefab = null)
        {
            if (vis == null)
                return;

            EnsureSharedUniform(rocketPrefab);
            Quaternion rot = rocketPrefab != null
                ? rocketPrefab.transform.localRotation
                : Quaternion.FromToRotation(Vector3.left, Vector3.forward);

            vis.localPosition = Vector3.zero;
            vis.localRotation = rot;
            vis.localScale = Vector3.one * _sharedUniform;

            Transform? center = DummyFind.FindRocketCenter(vis);
            if (center == null)
                HeavypinPlugin.ModLog?.LogWarning("Heavypin: CenterOfModel missing on rocket.");
            else
                DummySnap.AlignDummyToParent(vis, center);
        }

        internal static void ConfigureLauncher(Transform launcher, GameObject? launcherPrefab = null)
        {
            if (launcher == null)
                return;

            Quaternion rot = launcherPrefab != null
                ? launcherPrefab.transform.localRotation
                : Quaternion.FromToRotation(Vector3.left, Vector3.forward);

            launcher.localPosition = Vector3.zero;
            launcher.localRotation = rot;
            launcher.localScale = Vector3.one * _sharedUniform;

            Transform? attach = DummyFind.FindPylonAttach(launcher);
            if (attach != null)
                DummySnap.AlignDummyToParent(launcher, attach);
            else
                HeavypinPlugin.ModLog?.LogWarning("Heavypin: PlaceOfDocking missing on launcher.");
        }

        private static void EnsureSharedUniform(GameObject? rocketPrefab)
        {
            if (_uniformReady)
                return;

            float baked = 0f;
            if (rocketPrefab != null)
                baked = rocketPrefab.transform.localScale.x;

            // Prefer bake root scale; if bundle lost it (==1), measure native at scale 1.
            if (baked > 0.001f && Mathf.Abs(baked - 1f) > 0.001f)
            {
                _sharedUniform = baked;
                _uniformReady = true;
                HeavypinPlugin.ModLog?.LogInfo($"Heavypin uniform from bake={_sharedUniform:F4}");
                return;
            }

            if (rocketPrefab == null)
            {
                _sharedUniform = HeavypinConstants.VisualUniformScale;
                _uniformReady = true;
                return;
            }

            GameObject? probe = null;
            try
            {
                probe = Object.Instantiate(rocketPrefab);
                probe.transform.SetParent(null, false);
                probe.transform.position = Vector3.zero;
                probe.transform.rotation = Quaternion.identity;
                probe.transform.localScale = Vector3.one;
                probe.SetActive(true);
                float native = VisualMeasure.Longest(probe.transform);
                _sharedUniform = native > 0.05f
                    ? Mathf.Clamp(HeavypinConstants.LengthM / native, 0.01f, 2f)
                    : HeavypinConstants.VisualUniformScale;
                HeavypinPlugin.ModLog?.LogInfo(
                    $"Heavypin uniform measured native={native:F3}m → {_sharedUniform:F4}");
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
            vis.transform.localRotation = visualPrefab.transform.localRotation;
            vis.transform.localScale = Vector3.one * _sharedUniform;

            VisualMaterials.StripSceneJunk(vis);
            StripVisualPhysics(vis);
            VisualMaterials.MatchHostDrawState(vis, host);
            StockVisual.MarkOurs(vis.transform);

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
