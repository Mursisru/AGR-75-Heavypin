using System;
using System.Collections.Generic;
using System.Reflection;
using Sledgepin.Bootstrap;
using UnityEngine;

namespace Sledgepin.Runtime
{
    internal static class SledgepinMotorFx
    {
        private static readonly FieldInfo? MotorsField =
            typeof(Missile).GetField("motors", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly Type? MotorType =
            typeof(Missile).GetNestedType("Motor", BindingFlags.NonPublic | BindingFlags.Public);
        private static readonly FieldInfo? ParticlesField =
            MotorType?.GetField("particleSystems", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? TrailsField =
            MotorType?.GetField("trailEmitters", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? LightsField =
            MotorType?.GetField("lights", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo? AudioField =
            MotorType?.GetField("audioSources", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static readonly List<GameObject> PsTemplates = new List<GameObject>(4);
        private static readonly List<GameObject> TrailTemplates = new List<GameObject>(2);
        private static readonly List<GameObject> AudioTemplates = new List<GameObject>(2);
        private static readonly List<GameObject> LightTemplates = new List<GameObject>(4);
        private static GameObject? _hold;

        internal static void Capture(Encyclopedia enc, MissileDefinition? donor)
        {
            PsTemplates.Clear();
            TrailTemplates.Clear();
            AudioTemplates.Clear();
            LightTemplates.Clear();
            if (MotorsField == null || ParticlesField == null)
                return;

            MissileDefinition? def = donor ?? AgrDonor.FindAgr24Missile(enc);
            if (def?.unitPrefab == null)
            {
                SledgepinPlugin.ModLog?.LogWarning("SledgepinMotorFx: no AGR-24 for FX capture.");
                return;
            }
            Missile? mis = def.unitPrefab.GetComponent<Missile>() ??
                           def.unitPrefab.GetComponentInChildren<Missile>(true);
            if (mis == null)
                return;
            Array? motors = MotorsField.GetValue(mis) as Array;
            if (motors == null || motors.Length == 0)
                return;
            object? booster = motors.GetValue(0);
            if (booster == null)
                return;

            if (_hold == null)
            {
                _hold = new GameObject("Sledgepin_Agr24FxHold");
                UnityEngine.Object.DontDestroyOnLoad(_hold);
                _hold.SetActive(false);
            }

            CaptureArray(ParticlesField.GetValue(booster) as Array, PsTemplates, "SledgepinDonorPs", typeof(ParticleSystem));
            CaptureArray(TrailsField?.GetValue(booster) as Array, TrailTemplates, "SledgepinDonorTrail", typeof(TrailEmitter));
            CaptureAudio(AudioField?.GetValue(booster) as Array);
            CaptureArray(LightsField?.GetValue(booster) as Array, LightTemplates, "SledgepinDonorLit", typeof(Light));
            SledgepinPlugin.ModLog?.LogInfo(
                $"SledgepinMotorFx capture from '{def.jsonKey}' ps={PsTemplates.Count} trails={TrailTemplates.Count} audio={AudioTemplates.Count} lights={LightTemplates.Count}");
        }

        internal static void Bind(Missile missile)
        {
            if (missile == null || MotorsField == null || MotorType == null)
                return;

            if (PsTemplates.Count == 0)
            {
                SledgepinPlugin.ModLog?.LogWarning("SledgepinMotorFx: donor PS templates empty — recapture skipped.");
                return;
            }

            Transform? vis = VisualStamp.FindRocket(missile.transform);
            List<Transform> nozzles = vis != null ? DummyFind.FindNozzles(vis) : new List<Transform>();
            if (nozzles.Count == 0)
                nozzles = DummyFind.FindNozzles(missile.transform);
            if (nozzles.Count == 0 && vis != null)
            {
                Transform aft = CreateAftSocket(vis);
                if (aft != null)
                    nozzles.Add(aft);
            }
            if (nozzles.Count == 0)
            {
                SledgepinPlugin.ModLog?.LogWarning("Sledgepin: EngineEffectsSpawn dummies missing.");
                return;
            }

            Array? motors = MotorsField.GetValue(missile) as Array;
            if (motors == null || motors.Length == 0 || motors.GetValue(0) is not object motor)
                return;

            WipeStockFx(missile);

            if (!MotorNeedsInject(motor, nozzles))
            {
                ForcePlay(motor);
                SledgepinPlugin.ModLog?.LogInfo(
                    $"SledgepinMotorFx bind vis='{(vis != null ? vis.name : "null")}' nozzles={nozzles.Count} reused");
                return;
            }

            WipeOursFx(missile);
            InjectOnNozzles(missile, motor, nozzles);

            ForcePlay(motor);
            SledgepinPlugin.ModLog?.LogInfo(
                $"SledgepinMotorFx bind vis='{(vis != null ? vis.name : "null")}' nozzles={nozzles.Count}");
        }

        internal static void Ensure(Missile missile)
        {
            if (missile == null)
                return;
            Bind(missile);
        }

        internal static void SilenceStock(Missile missile)
        {
            if (missile != null)
                WipeStockFx(missile);
        }

        private static void InjectOnNozzles(Missile missile, object motor, List<Transform> nozzles)
        {
            var lights = new List<Light>(8);
            var psList = new List<ParticleSystem>(8);
            var audios = new List<AudioSource>(4);

            GameObject? psTpl = PsTemplates.Count > 0 ? PsTemplates[0] : null;
            for (int n = 0; n < nozzles.Count; n++)
            {
                Transform sock = nozzles[n];
                if (sock == null || psTpl == null)
                    continue;
                GameObject? go = PlaceOnDummy(psTpl, sock, "SledgepinExhaust");
                if (go == null)
                    continue;
                StripLooseTrails(go);
                ParticleSystem? root = go.GetComponent<ParticleSystem>() ??
                                       go.GetComponentInChildren<ParticleSystem>(true);
                if (root == null)
                {
                    UnityEngine.Object.Destroy(go);
                    continue;
                }
                LoopExhaust(root);
                HarvestLights(go, lights);
                psList.Add(root);
                for (int i = 0; i < LightTemplates.Count; i++)
                {
                    GameObject? litGo = PlaceOnDummy(LightTemplates[i], sock, "SledgepinLight");
                    if (litGo == null)
                        continue;
                    Light? lit = litGo.GetComponent<Light>() ?? litGo.GetComponentInChildren<Light>(true);
                    if (lit == null)
                    {
                        UnityEngine.Object.Destroy(litGo);
                        continue;
                    }
                    lit.enabled = true;
                    lights.Add(lit);
                }
            }

            if (AudioTemplates.Count > 0 && nozzles.Count > 0)
            {
                GameObject? go = PlaceOnDummy(AudioTemplates[0], nozzles[0], "SledgepinAudio");
                if (go != null)
                {
                    AudioSource? src = go.GetComponent<AudioSource>() ?? go.GetComponentInChildren<AudioSource>(true);
                    if (src != null)
                    {
                        src.playOnAwake = false;
                        src.loop = true;
                        src.spatialBlend = 1f;
                        audios.Add(src);
                    }
                }
            }

            if (lights.Count == 0 && nozzles.Count > 0)
            {
                GameObject go = new GameObject("SledgepinLight");
                go.transform.SetParent(nozzles[0], false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                Light lit = go.AddComponent<Light>();
                lit.type = LightType.Point;
                lit.color = new Color(1f, 0.55f, 0.2f);
                lit.intensity = 6f;
                lit.range = 18f;
                lit.enabled = true;
                lights.Add(lit);
            }

            if (ParticlesField != null)
                ParticlesField.SetValue(motor, psList.ToArray());
            if (TrailsField != null)
                TrailsField.SetValue(motor, Array.CreateInstance(typeof(TrailEmitter), 0));
            if (AudioField != null)
                AudioField.SetValue(motor, audios.ToArray());
            if (LightsField != null)
                LightsField.SetValue(motor, lights.ToArray());

            FieldInfo? startupField = MotorType?.GetField("startupSource", BindingFlags.Instance | BindingFlags.NonPublic);
            if (startupField != null && audios.Count > 0)
                startupField.SetValue(motor, audios[0]);

            SledgepinPlugin.ModLog?.LogInfo(
                $"SledgepinMotorFx inject ps={psList.Count} audio={audios.Count} lights={lights.Count}");
        }

        private static void StripLooseTrails(GameObject go)
        {
            TrailEmitter[] tes = go.GetComponentsInChildren<TrailEmitter>(true);
            for (int i = 0; i < tes.Length; i++)
            {
                TrailEmitter te = tes[i];
                if (te == null)
                    continue;
                te.StopTrail();
                te.enabled = false;
                UnityEngine.Object.Destroy(te);
            }
        }

        private static void ForcePlay(object motor)
        {
            if (ParticlesField?.GetValue(motor) is ParticleSystem[] psArr)
            {
                for (int i = 0; i < psArr.Length; i++)
                {
                    if (psArr[i] != null)
                        psArr[i].Play(true);
                }
            }
            if (AudioField?.GetValue(motor) is AudioSource[] auArr)
            {
                for (int i = 0; i < auArr.Length; i++)
                {
                    AudioSource a = auArr[i];
                    if (a == null || a.clip == null)
                        continue;
                    a.enabled = true;
                    a.Play();
                }
            }
            FieldInfo? startupField = MotorType?.GetField("startupSource", BindingFlags.Instance | BindingFlags.NonPublic);
            if (startupField?.GetValue(motor) is AudioSource start && start != null && start.clip != null)
            {
                start.enabled = true;
                if (!start.isPlaying)
                    start.Play();
            }
            if (TrailsField?.GetValue(motor) is TrailEmitter[] trArr)
            {
                for (int i = 0; i < trArr.Length; i++)
                {
                    if (trArr[i] != null)
                        trArr[i].StartTrail();
                }
            }
            if (LightsField?.GetValue(motor) is Light[] litArr)
            {
                for (int i = 0; i < litArr.Length; i++)
                {
                    if (litArr[i] != null)
                        litArr[i].enabled = true;
                }
            }
        }

        private static Transform CreateAftSocket(Transform vis)
        {
            Transform? existing = vis.Find("EngineEffectsSpawn");
            if (existing != null)
                return existing;
            var go = new GameObject("EngineEffectsSpawn");
            Transform sock = go.transform;
            sock.SetParent(vis, false);
            sock.localRotation = Quaternion.identity;
            sock.localScale = Vector3.one;
            Renderer[] rs = vis.GetComponentsInChildren<Renderer>(true);
            float minZ = 0f;
            bool any = false;
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null)
                    continue;
                Bounds b = rs[i].bounds;
                Vector3 p = vis.InverseTransformPoint(b.center - vis.forward * b.extents.z);
                if (!any || p.z < minZ)
                {
                    minZ = p.z;
                    any = true;
                }
            }
            sock.localPosition = any
                ? new Vector3(0f, 0f, minZ)
                : new Vector3(0f, 0f, -SledgepinConstants.LengthM * 0.5f);
            return sock;
        }

        private static GameObject? PlaceOnDummy(GameObject? tpl, Transform socket, string name)
        {
            if (tpl == null || socket == null)
                return null;
            GameObject go = UnityEngine.Object.Instantiate(tpl);
            go.name = name;
            go.transform.SetParent(socket, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            go.SetActive(true);
            return go;
        }

        private static void CaptureArray(Array? src, List<GameObject> dst, string name, Type componentType)
        {
            if (src == null)
                return;
            for (int i = 0; i < src.Length; i++)
            {
                object? item = src.GetValue(i);
                if (item is not Component c || c == null)
                    continue;
                if (!componentType.IsInstanceOfType(c))
                    continue;
                GameObject go = UnityEngine.Object.Instantiate(c.gameObject, _hold!.transform);
                go.name = name;
                go.SetActive(false);
                dst.Add(go);
            }
        }

        private static void CaptureAudio(Array? src)
        {
            if (src == null)
                return;
            for (int i = 0; i < src.Length; i++)
            {
                if (src.GetValue(i) is not AudioSource a || a == null || a.clip == null)
                    continue;
                GameObject go = UnityEngine.Object.Instantiate(a.gameObject, _hold!.transform);
                go.name = "SledgepinDonorAu";
                go.SetActive(false);
                AudioTemplates.Add(go);
            }
        }

        private static void HarvestLights(GameObject go, List<Light> dst)
        {
            Light[] found = go.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] == null)
                    continue;
                found[i].enabled = true;
                dst.Add(found[i]);
            }
        }

        private static void LoopExhaust(ParticleSystem root)
        {
            ParticleSystem[] all = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < all.Length; i++)
            {
                ParticleSystem ps = all[i];
                if (ps == null)
                    continue;
                ParticleSystem.MainModule main = ps.main;
                main.loop = true;
                main.playOnAwake = false;
                ParticleSystem.EmissionModule em = ps.emission;
                em.enabled = true;
            }
        }

        private static bool MotorNeedsInject(object motor, List<Transform> nozzles)
        {
            int want = nozzles.Count;
            if (want == 0)
                return false;

            if (ParticlesField?.GetValue(motor) is not ParticleSystem[] ps || ps.Length != want)
                return true;

            for (int i = 0; i < want; i++)
            {
                ParticleSystem? p = ps[i];
                Transform? sock = nozzles[i];
                if (p == null || sock == null)
                    return true;
                if (!IsOursFx(p.transform))
                    return true;
                if (p.transform.parent != sock)
                    return true;
            }

            return false;
        }

        private static void WipeOursFx(Missile missile)
        {
            if (missile == null)
                return;

            var kill = new List<GameObject>(12);
            TrailEmitter[] trails = missile.GetComponentsInChildren<TrailEmitter>(true);
            for (int i = 0; i < trails.Length; i++)
            {
                TrailEmitter te = trails[i];
                if (te == null || !IsOursFx(te.transform))
                    continue;
                te.StopTrail();
                te.enabled = false;
                kill.Add(te.gameObject);
            }

            ParticleSystem[] all = missile.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < all.Length; i++)
            {
                ParticleSystem ps = all[i];
                if (ps == null || !IsOursFx(ps.transform))
                    continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                kill.Add(ps.gameObject);
            }

            Light[] lights = missile.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                Light lit = lights[i];
                if (lit == null || !IsOursFx(lit.transform))
                    continue;
                lit.enabled = false;
                kill.Add(lit.gameObject);
            }

            AudioSource[] audios = missile.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < audios.Length; i++)
            {
                AudioSource a = audios[i];
                if (a == null || !IsOursFx(a.transform))
                    continue;
                a.Stop();
                kill.Add(a.gameObject);
            }

            for (int i = 0; i < kill.Count; i++)
            {
                if (kill[i] == null)
                    continue;
                kill[i].SetActive(false);
                UnityEngine.Object.Destroy(kill[i]);
            }
        }

        private static void WipeStockFx(Missile missile)
        {
            var kill = new List<GameObject>(8);
            TrailEmitter[] trails = missile.GetComponentsInChildren<TrailEmitter>(true);
            for (int i = 0; i < trails.Length; i++)
            {
                TrailEmitter te = trails[i];
                if (te == null || IsOursFx(te.transform))
                    continue;
                te.StopTrail();
                te.enabled = false;
                kill.Add(te.gameObject);
            }
            ParticleSystem[] all = missile.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < all.Length; i++)
            {
                ParticleSystem ps = all[i];
                if (ps == null || IsOursFx(ps.transform))
                    continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                kill.Add(ps.gameObject);
            }
            for (int i = 0; i < kill.Count; i++)
            {
                if (kill[i] == null)
                    continue;
                kill[i].SetActive(false);
                UnityEngine.Object.Destroy(kill[i]);
            }
        }

        private static bool IsOursFx(Transform t)
        {
            while (t != null)
            {
                string n = t.name;
                if (n == "SledgepinExhaust" || n == "SledgepinTrail" || n == "SledgepinAudio" || n == "SledgepinLight")
                    return true;
                t = t.parent;
            }
            return false;
        }
    }
}
