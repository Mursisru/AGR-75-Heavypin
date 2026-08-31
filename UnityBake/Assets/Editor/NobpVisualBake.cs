using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Heavypin.UnityBake
{
    internal static class NobpVisualBake
    {
        internal const float TargetLengthM = 1.36f;
        private const float FileScale = 100f;
        private const float ScaleEps = 0.5f;

        // FBX FileScale: scale 100 + positions in cm-space (CenterOfModel -269 → -2.69).
        internal static void FlattenFileScale(GameObject root)
        {
            if (root == null)
                return;

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t == root.transform)
                    continue;

                t.localPosition /= FileScale;

                Vector3 s = t.localScale;
                if (Mathf.Abs(s.x - FileScale) < ScaleEps &&
                    Mathf.Abs(s.y - FileScale) < ScaleEps &&
                    Mathf.Abs(s.z - FileScale) < ScaleEps)
                    t.localScale = Vector3.one;
            }
        }

        // Clips authored at FileScale 100 — restore would blow Cube fins to hangar size.
        internal static AnimationClip SanitizeClipFileScale(AnimationClip src, string folder)
        {
            if (src == null)
                return null;

            string safe = SanitizeName(src.name);
            string path = $"{folder}/{safe}.anim";
            AnimationClip dst = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (dst == null)
            {
                dst = new AnimationClip { name = src.name };
                AssetDatabase.CreateAsset(dst, path);
            }
            else
            {
                dst.ClearCurves();
                dst.name = src.name;
            }

            dst.frameRate = src.frameRate;
            dst.wrapMode = src.wrapMode;
            AnimationUtility.SetAnimationClipSettings(dst, AnimationUtility.GetAnimationClipSettings(src));

            int scaleFixed = 0;
            int posFixed = 0;
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(src);
            for (int i = 0; i < bindings.Length; i++)
            {
                EditorCurveBinding b = bindings[i];
                AnimationCurve curve = AnimationUtility.GetEditorCurve(src, b);
                if (curve == null)
                    continue;

                string prop = b.propertyName ?? string.Empty;
                if (prop.StartsWith("m_LocalScale", StringComparison.Ordinal))
                    curve = ScaleCurve(curve, 1f / FileScale, ref scaleFixed);
                else if (prop.StartsWith("m_LocalPosition", StringComparison.Ordinal))
                    curve = ScaleCurve(curve, 1f / FileScale, ref posFixed);

                AnimationUtility.SetEditorCurve(dst, b, curve);
            }

            EditorCurveBinding[] objBindings = AnimationUtility.GetObjectReferenceCurveBindings(src);
            for (int i = 0; i < objBindings.Length; i++)
            {
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(src, objBindings[i]);
                if (keys != null)
                    AnimationUtility.SetObjectReferenceCurve(dst, objBindings[i], keys);
            }

            EditorUtility.SetDirty(dst);
            Debug.Log($"AGR-75 Heavypin: clip '{src.name}' scaleKeys~{scaleFixed} posKeys~{posFixed}");
            return dst;
        }

        internal static void StripCameraEmpties(GameObject root)
        {
            if (root == null)
                return;
            var strip = new List<GameObject>(4);
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || t == root.transform || IsDummy(t.name))
                    continue;
                if (t.GetComponent<MeshFilter>() != null || t.GetComponent<SkinnedMeshRenderer>() != null)
                    continue;
                if (t.childCount > 0)
                    continue;
                string n = t.name ?? string.Empty;
                if (n.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) < 0 &&
                    n.IndexOf("Камер", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                strip.Add(t.gameObject);
            }
            for (int i = 0; i < strip.Count; i++)
                UnityEngine.Object.DestroyImmediate(strip[i]);
        }

        internal static void ApplyRocketYaw(GameObject root)
        {
            if (root == null)
                return;
            root.transform.localRotation = Quaternion.FromToRotation(Vector3.left, Vector3.forward);
        }

        internal static float MeasureLongest(GameObject root)
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

        internal static void ApplyUniformRoot(GameObject root, float scale)
        {
            if (root == null)
                return;
            if (scale <= 0.0001f)
                scale = 1f;
            root.transform.localScale = new Vector3(scale, scale, scale);
        }

        internal static void LogAabbAndDummies(GameObject root, string label)
        {
            if (root == null)
                return;
            float longest = MeasureLongest(root);
            Debug.Log(
                $"AGR-75 Heavypin AABB [{label}] longest={longest:F3}m " +
                $"rootScale={root.transform.localScale.x:F4} euler={root.transform.localEulerAngles}");

            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                Transform t = all[i];
                if (t == null || !IsDummy(t.name))
                    continue;
                Debug.Log($"AGR-75 dummy [{label}] {t.name} local={t.localPosition} scale={t.localScale}");
            }
        }

        internal static bool IsDummy(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            if (string.Equals(name, "CenterOfModel", StringComparison.OrdinalIgnoreCase))
                return true;
            return name.StartsWith("EngineEffectsSpawn", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("PlaceOfDocking", StringComparison.OrdinalIgnoreCase) ||
                   name.StartsWith("CentarOfDockingAGRRocket", StringComparison.OrdinalIgnoreCase);
        }

        private static AnimationCurve ScaleCurve(AnimationCurve src, float mul, ref int touched)
        {
            Keyframe[] keys = src.keys;
            for (int k = 0; k < keys.Length; k++)
            {
                Keyframe key = keys[k];
                key.value *= mul;
                key.inTangent *= mul;
                key.outTangent *= mul;
                keys[k] = key;
                touched++;
            }
            return new AnimationCurve(keys) { postWrapMode = src.postWrapMode, preWrapMode = src.preWrapMode };
        }

        private static string SanitizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "clip";
            char[] chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
                    chars[i] = '_';
            }
            return new string(chars);
        }

        private static void Encapsulate(Mesh mesh, Matrix4x4 toWorld, ref Bounds world, ref bool any)
        {
            Bounds lb = mesh.bounds;
            Vector3 min = lb.min;
            Vector3 max = lb.max;
            Vector3 c0 = toWorld.MultiplyPoint3x4(new Vector3(min.x, min.y, min.z));
            Vector3 c1 = toWorld.MultiplyPoint3x4(new Vector3(min.x, min.y, max.z));
            Vector3 c2 = toWorld.MultiplyPoint3x4(new Vector3(min.x, max.y, min.z));
            Vector3 c3 = toWorld.MultiplyPoint3x4(new Vector3(min.x, max.y, max.z));
            Vector3 c4 = toWorld.MultiplyPoint3x4(new Vector3(max.x, min.y, min.z));
            Vector3 c5 = toWorld.MultiplyPoint3x4(new Vector3(max.x, min.y, max.z));
            Vector3 c6 = toWorld.MultiplyPoint3x4(new Vector3(max.x, max.y, min.z));
            Vector3 c7 = toWorld.MultiplyPoint3x4(new Vector3(max.x, max.y, max.z));
            if (!any)
            {
                world = new Bounds(c0, Vector3.zero);
                any = true;
            }
            world.Encapsulate(c0);
            world.Encapsulate(c1);
            world.Encapsulate(c2);
            world.Encapsulate(c3);
            world.Encapsulate(c4);
            world.Encapsulate(c5);
            world.Encapsulate(c6);
            world.Encapsulate(c7);
        }
    }
}
