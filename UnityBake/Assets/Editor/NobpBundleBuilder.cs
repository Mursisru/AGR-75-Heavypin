using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Heavypin.UnityBake
{
    public static class NobpBundleBuilder
    {
        private const string OutputName = "AGR75Heavypin.nobp";
        private const string RocketPrefabName = "HeavypinRocket";
        private const string Launcher4Name = "HeavypinLauncher4";
        private const string Launcher6Name = "HeavypinLauncher6";
        private const string RocketFbx = "AGR-75-Heavypin-MainRocket.fbx";
        private const string Launcher4Fbx = "LaunchStandAGR-75-4X.fbx";
        private const string Launcher6Fbx = "LaunchStandAGR-75-6X.fbx";

        [MenuItem("AGR-75-Heavypin/Build Nobp Bundle")]
        public static void Build()
        {
            string assetsRoot = "Assets/MissilePack";
            string buildDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Build"));
            Directory.CreateDirectory(buildDir);

            float visualScale = 1f;
            EnsurePrefab(assetsRoot, RocketFbx, RocketPrefabName, importAnim: true, "Rocket", computeScale: true, ref visualScale);
            EnsurePrefab(assetsRoot, Launcher4Fbx, Launcher4Name, importAnim: false, "Launcher", computeScale: false, ref visualScale);
            EnsurePrefab(assetsRoot, Launcher6Fbx, Launcher6Name, importAnim: false, "Launcher", computeScale: false, ref visualScale);
            EnsureManifest(assetsRoot);

            var assetNames = new List<string>
            {
                $"{assetsRoot}/{RocketPrefabName}.prefab",
                $"{assetsRoot}/{Launcher4Name}.prefab",
                $"{assetsRoot}/{Launcher6Name}.prefab",
                $"{assetsRoot}/patch_manifest.txt",
                $"{assetsRoot}/HeavypinRocket.controller"
            };

            AddFolderAssets(assetNames, $"{assetsRoot}/Materials/Rocket");
            AddFolderAssets(assetNames, $"{assetsRoot}/Materials/Launcher");
            AddFolderAssets(assetNames, $"{assetsRoot}/Textures/Rocket");
            AddFolderAssets(assetNames, $"{assetsRoot}/Textures/Launcher");
            AddFolderAssets(assetNames, $"{assetsRoot}/AnimClips");
            AddIfExists(assetNames, $"{assetsRoot}/{RocketFbx}");
            AddIfExists(assetNames, $"{assetsRoot}/{Launcher4Fbx}");
            AddIfExists(assetNames, $"{assetsRoot}/{Launcher6Fbx}");

            var build = new AssetBundleBuild
            {
                assetBundleName = OutputName,
                assetNames = assetNames.ToArray()
            };

            BuildPipeline.BuildAssetBundles(
                buildDir,
                new[] { build },
                BuildAssetBundleOptions.ForceRebuildAssetBundle,
                BuildTarget.StandaloneWindows64);

            string produced = Path.Combine(buildDir, OutputName);
            string alt = Path.Combine(buildDir, OutputName.ToLowerInvariant());
            string src = File.Exists(produced) ? produced : alt;

            string pluginRes = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "AGR75Heavypin", "Resources"));
            Directory.CreateDirectory(pluginRes);
            if (File.Exists(src))
            {
                File.Copy(src, Path.Combine(pluginRes, OutputName), true);
                string binRel = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "AGR75Heavypin", "bin", "Release"));
                Directory.CreateDirectory(binRel);
                File.Copy(src, Path.Combine(binRel, OutputName), true);
            }

            string deploy = @"C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option\BepInEx\plugins\AGR-75-Heavypin";
            Directory.CreateDirectory(deploy);
            if (File.Exists(src))
            {
                File.Copy(src, Path.Combine(deploy, OutputName), true);
                File.Copy(src, Path.Combine(deploy, OutputName.ToLowerInvariant()), true);
            }

            CopyDiskMaps(Path.Combine(Application.dataPath, "MissilePack", "Textures", "Rocket"), deploy);
            CopyDiskMaps(Path.Combine(Application.dataPath, "MissilePack", "Textures", "Launcher"), deploy);

            Debug.Log($"AGR-75 Heavypin: built {src}");
            AssetDatabase.Refresh();
        }

        private static void EnsureManifest(string assetsRoot)
        {
            string json =
@"{
  ""modName"": ""AGR75Heavypin"",
  ""schemaVersion"": 3,
  ""modVersion"": ""0.0.0"",
  ""Patches"": [],
  ""Ops"": [],
  ""Addressables"": []
}";
            string txtPath = Path.Combine(Application.dataPath, "MissilePack", "patch_manifest.txt");
            File.WriteAllText(txtPath, json);
            AssetDatabase.ImportAsset($"{assetsRoot}/patch_manifest.txt");
        }

        private static void EnsurePrefab(
            string assetsRoot,
            string fbxName,
            string prefabName,
            bool importAnim,
            string texFolder,
            bool computeScale,
            ref float uniformScale)
        {
            string fbxPath = $"{assetsRoot}/{fbxName}";
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), fbxPath.Replace('/', Path.DirectorySeparatorChar))))
            {
                Debug.LogWarning($"AGR-75 Heavypin: {fbxName} not found.");
                return;
            }

            ConfigureImporter(fbxPath, importAnim);
            GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbx == null)
            {
                Debug.LogError($"AGR-75 Heavypin: failed to load {fbxName}");
                return;
            }

            GameObject root = UnityEngine.Object.Instantiate(fbx);
            root.name = prefabName.StartsWith("HeavypinLauncher", StringComparison.Ordinal)
                ? "HeavypinLauncher"
                : prefabName;

            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (light != null)
                    UnityEngine.Object.DestroyImmediate(light.gameObject);
            }
            foreach (Camera cam in root.GetComponentsInChildren<Camera>(true))
            {
                if (cam != null)
                    UnityEngine.Object.DestroyImmediate(cam.gameObject);
            }

            Shader lit = Shader.Find("Standard") ?? Shader.Find("Legacy Shaders/Diffuse");
            string matFolder = $"{assetsRoot}/Materials/{texFolder}";
            if (!AssetDatabase.IsValidFolder($"{assetsRoot}/Materials"))
                AssetDatabase.CreateFolder(assetsRoot, "Materials");
            if (!AssetDatabase.IsValidFolder(matFolder))
                AssetDatabase.CreateFolder($"{assetsRoot}/Materials", texFolder);

            Dictionary<string, Material> bakedByBlender = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r == null)
                    continue;
                Material[] src = r.sharedMaterials;
                Material[] dst = new Material[Mathf.Max(1, src != null ? src.Length : 1)];
                for (int i = 0; i < dst.Length; i++)
                {
                    Material imported = src != null && i < src.Length ? src[i] : null;
                    string blenderName = imported != null && !string.IsNullOrEmpty(imported.name)
                        ? StripInstance(imported.name)
                        : r.gameObject.name + "_" + i;
                    if (bakedByBlender.TryGetValue(blenderName, out Material shared))
                    {
                        dst[i] = shared;
                        continue;
                    }

                    string matAssetPath = $"{matFolder}/{Sanitize(blenderName)}.mat";
                    Material mat = AssetDatabase.LoadAssetAtPath<Material>(matAssetPath);
                    if (mat == null)
                    {
                        mat = imported != null ? new Material(imported) : new Material(lit);
                        mat.name = blenderName;
                        AssetDatabase.CreateAsset(mat, matAssetPath);
                    }
                    else if (imported != null)
                        mat.CopyPropertiesFromMaterial(imported);

                    mat.name = blenderName;
                    if (mat.shader == null || mat.shader.name.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0)
                        mat.shader = lit;
                    ApplyDiskMaps(mat, blenderName, assetsRoot, texFolder);
                    if (IsMat004Name(blenderName))
                        ApplyGlassBake(mat);
                    if (mat.HasProperty("_EmissionColor"))
                        mat.SetColor("_EmissionColor", Color.black);
                    mat.DisableKeyword("_EMISSION");
                    EditorUtility.SetDirty(mat);
                    bakedByBlender[blenderName] = mat;
                    dst[i] = mat;
                }
                r.sharedMaterials = dst;
            }

            if (importAnim)
                BindAnimator(root, fbxPath, assetsRoot);

            NobpVisualBake.FlattenFileScale(root);
            root.transform.localScale = Vector3.one;
            root.transform.localRotation = Quaternion.identity;
            NobpVisualBake.StripCameraEmpties(root);
            if (computeScale)
            {
                float longest = NobpVisualBake.MeasureLongest(root);
                uniformScale = longest > 0.01f ? NobpVisualBake.TargetLengthM / longest : 1f;
                Debug.Log(
                    $"AGR-75 Heavypin: flatten longest={longest:F3}m uniform={uniformScale:F4} " +
                    $"target={NobpVisualBake.TargetLengthM:F3}m");
                // Rocket + launcher share Blender axis; mount orients both. No root yaw.
            }
            NobpVisualBake.ApplyUniformRoot(root, uniformScale);
            NobpVisualBake.LogAabbAndDummies(root, prefabName);

            AssetDatabase.SaveAssets();
            string prefabPath = $"{assetsRoot}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            Debug.Log($"AGR-75 Heavypin: {prefabName} from '{fbxPath}'");
        }

        private static void BindAnimator(GameObject root, string fbxPath, string assetsRoot)
        {
            AnimationClip[] raw = FilterOwnActionClips(LoadClips(fbxPath));
            if (raw.Length == 0)
            {
                Debug.LogWarning("AGR-75 Heavypin: no animation clips on rocket FBX.");
                return;
            }

            string clipFolder = $"{assetsRoot}/AnimClips";
            if (!AssetDatabase.IsValidFolder(clipFolder))
                AssetDatabase.CreateFolder(assetsRoot, "AnimClips");

            AnimationClip[] clips = new AnimationClip[raw.Length];
            for (int i = 0; i < raw.Length; i++)
                clips[i] = NobpVisualBake.SanitizeClipFileScale(raw[i], clipFolder);

            string ctrlPath = $"{assetsRoot}/HeavypinRocket.controller";
            AssetDatabase.DeleteAsset(ctrlPath);
            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            BindClipToLayer(ctrl, 0, clips[0]);
            for (int i = 1; i < clips.Length; i++)
            {
                ctrl.AddLayer("L" + i);
                AnimatorControllerLayer[] layers = ctrl.layers;
                AnimatorControllerLayer layer = layers[i];
                layer.defaultWeight = 1f;
                layer.blendingMode = AnimatorLayerBlendingMode.Override;
                layers[i] = layer;
                ctrl.layers = layers;
                BindClipToLayer(ctrl, i, clips[i]);
            }

            Animator animator = root.GetComponent<Animator>();
            if (animator == null)
                animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = ctrl;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.enabled = false; // hangar: off until DeployFins / HeavypinAnim.Play
            Debug.Log($"AGR-75 Heavypin: animator own-clips={clips.Length} first='{clips[0].name}'");
        }

        private static void BindClipToLayer(AnimatorController ctrl, int layer, AnimationClip clip)
        {
            AnimatorStateMachine sm = ctrl.layers[layer].stateMachine;
            AnimatorState state = sm.defaultState ?? sm.AddState(clip.name);
            state.motion = clip;
            sm.defaultState = state;
        }

        private static AnimationClip[] FilterOwnActionClips(AnimationClip[] clips)
        {
            var own = new List<AnimationClip>();
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip c = clips[i];
                if (c != null && IsOwnAction(c.name))
                    own.Add(c);
            }
            return own.Count > 0 ? own.ToArray() : clips;
        }

        private static bool IsOwnAction(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            int p = name.IndexOf('|');
            if (p < 0)
                return true;
            string left = name.Substring(0, p);
            string right = name.Substring(p + 1);
            return right.StartsWith(left, StringComparison.OrdinalIgnoreCase);
        }

        private static AnimationClip[] LoadClips(string fbxPath)
        {
            UnityEngine.Object[] all = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var list = new List<AnimationClip>();
            if (all == null)
                return Array.Empty<AnimationClip>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] is AnimationClip clip && clip != null && !clip.name.StartsWith("__preview", StringComparison.OrdinalIgnoreCase))
                    list.Add(clip);
            }
            return list.ToArray();
        }

        private static void ApplyDiskMaps(Material mat, string blenderName, string assetsRoot, string texFolder)
        {
            string texRoot = $"{assetsRoot}/Textures/{texFolder}";
            Texture2D color = FindTex(texRoot, blenderName, "Color");
            if (color != null)
            {
                if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", color);
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", color);
            }
            string normalPath = FindTexPath(texRoot, blenderName, "Normal");
            string colorPath = FindTexPath(texRoot, blenderName, "Color");
            if (!string.IsNullOrEmpty(colorPath))
                ConfigureTextureImport(colorPath, asNormal: false, alphaIsTransparency: IsMat004Name(blenderName));
            if (!string.IsNullOrEmpty(normalPath))
            {
                ConfigureTextureImport(normalPath, asNormal: true, alphaIsTransparency: false);
                Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (normal != null)
                {
                    if (mat.HasProperty("_BumpMap"))
                        mat.SetTexture("_BumpMap", normal);
                    if (mat.HasProperty("_BumpScale"))
                        mat.SetFloat("_BumpScale", 1f);
                    mat.EnableKeyword("_NORMALMAP");
                }
            }
        }

        private static Texture2D FindTex(string texRoot, string blenderName, string kind)
        {
            string path = FindTexPath(texRoot, blenderName, kind);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static string FindTexPath(string texRoot, string blenderName, string kind)
        {
            string[] tries =
            {
                $"{texRoot}/{blenderName} {kind}.png",
                $"{texRoot}/{blenderName.Replace('_', '.')} {kind}.png",
                $"{texRoot}/Материал.004 {kind}.png",
                $"{texRoot}/Material.004 {kind}.png",
                $"{texRoot}/Материал.003 {kind}.png",
                $"{texRoot}/Material.003 {kind}.png",
                $"{texRoot}/Материал {kind}.png",
                $"{texRoot}/Material {kind}.png",
                $"{texRoot}/Cube-3-001-002-003-004.003 {kind}.png"
            };
            for (int i = 0; i < tries.Length; i++)
            {
                string abs = Path.Combine(Directory.GetCurrentDirectory(), tries[i].Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(abs))
                    return tries[i];
            }

            string foldWant = Fold(blenderName);
            string absDir = Path.Combine(Directory.GetCurrentDirectory(), texRoot.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(absDir))
                return null;
            foreach (string file in Directory.GetFiles(absDir, $"* {kind}.png"))
            {
                string stem = Path.GetFileNameWithoutExtension(file);
                int idx = stem.LastIndexOf(" " + kind, StringComparison.OrdinalIgnoreCase);
                if (idx > 0)
                    stem = stem.Substring(0, idx);
                if (Fold(stem) == foldWant || Fold(stem).IndexOf(foldWant, StringComparison.Ordinal) >= 0 ||
                    foldWant.IndexOf(Fold(stem), StringComparison.Ordinal) >= 0)
                    return $"{texRoot}/{Path.GetFileName(file)}";
            }
            return null;
        }

        private static void ConfigureTextureImport(string assetPath, bool asNormal, bool alphaIsTransparency)
        {
            TextureImporter imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null)
                return;
            bool dirty = false;
            if (asNormal && imp.textureType != TextureImporterType.NormalMap)
            {
                imp.textureType = TextureImporterType.NormalMap;
                dirty = true;
            }
            if (!asNormal && imp.textureType != TextureImporterType.Default)
            {
                imp.textureType = TextureImporterType.Default;
                dirty = true;
            }
            if (alphaIsTransparency && !imp.alphaIsTransparency)
            {
                imp.alphaIsTransparency = true;
                dirty = true;
            }
            if (!imp.mipmapEnabled)
            {
                imp.mipmapEnabled = true;
                dirty = true;
            }
            if (dirty)
                imp.SaveAndReimport();
        }

        private static bool IsMat004Name(string blenderName)
        {
            if (string.IsNullOrEmpty(blenderName))
                return false;
            return blenderName.EndsWith(".004", StringComparison.Ordinal) ||
                   blenderName.EndsWith("_004", StringComparison.Ordinal) ||
                   blenderName.EndsWith("004", StringComparison.Ordinal);
        }

        private static void ApplyGlassBake(Material mat)
        {
            if (mat == null)
                return;
            mat.SetFloat("_Mode", 3f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.SetFloat("_Glossiness", 0.92f);
            mat.SetFloat("_Metallic", 0f);
        }

        private static void ConfigureImporter(string fbxPath, bool importAnim)
        {
            ModelImporter imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (imp == null)
            {
                AssetDatabase.ImportAsset(fbxPath, ImportAssetOptions.ForceUpdate);
                return;
            }
            imp.weldVertices = false;
            imp.meshOptimizationFlags = (MeshOptimizationFlags)0;
            imp.importNormals = ModelImporterNormals.Import;
            imp.importTangents = ModelImporterTangents.CalculateMikk;
            imp.preserveHierarchy = true;
            imp.addCollider = false;
            imp.importLights = false;
            imp.importCameras = false;
            imp.useFileScale = false;
            imp.globalScale = 1f;
            imp.globalScale = 1f;
            if (importAnim)
            {
                imp.animationType = ModelImporterAnimationType.Generic;
                imp.importAnimation = true;
            }
            else
            {
                imp.animationType = ModelImporterAnimationType.None;
                imp.importAnimation = false;
            }
            imp.SaveAndReimport();
        }

        private static void CopyDiskMaps(string texAbs, string deploy)
        {
            if (!Directory.Exists(texAbs))
                return;
            string texDeploy = Path.Combine(deploy, "Textures", "AGR75");
            Directory.CreateDirectory(texDeploy);
            foreach (string file in Directory.GetFiles(texAbs, "*.png"))
            {
                string name = Path.GetFileName(file);
                if (name.IndexOf("Displacement", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (name.IndexOf("without Bump", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                if (name.IndexOf("Preview", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;
                File.Copy(file, Path.Combine(texDeploy, name), true);
            }
        }

        private static void AddFolderAssets(List<string> dst, string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                return;
            foreach (string guid in AssetDatabase.FindAssets("", new[] { folder }))
                dst.Add(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static void AddIfExists(List<string> dst, string assetPath)
        {
            string abs = Path.Combine(Directory.GetCurrentDirectory(), assetPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(abs))
                dst.Add(assetPath);
        }

        private static string StripInstance(string name)
        {
            const string inst = " (Instance)";
            if (!string.IsNullOrEmpty(name) && name.EndsWith(inst, StringComparison.OrdinalIgnoreCase))
                return name.Substring(0, name.Length - inst.Length);
            return name;
        }

        private static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "mesh";
            char[] chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
                    chars[i] = '_';
            }
            return new string(chars);
        }

        private static string Fold(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            char[] buf = new char[s.Length];
            int n = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsLetterOrDigit(c))
                    buf[n++] = char.ToLowerInvariant(c);
            }
            return new string(buf, 0, n);
        }
    }
}
