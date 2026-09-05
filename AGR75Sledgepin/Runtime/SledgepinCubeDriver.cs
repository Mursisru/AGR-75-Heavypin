using Sledgepin;
using UnityEngine;

namespace Sledgepin.Runtime
{
    // Crosswim-style: baked keys, no Animator. Absolute local pos+rot per fin frame.
    internal sealed class SledgepinCubeDriver : MonoBehaviour
    {
        private readonly Transform?[] _fins = new Transform?[SledgepinCubeKeys.FinCount];
        private readonly Vector3[] _scale = new Vector3[SledgepinCubeKeys.FinCount];
        private bool _ready;
        private float _elapsed;
        private bool _playing;

        internal void CaptureBindIfNeeded()
        {
            if (_ready)
                return;

            for (int i = 0; i < SledgepinCubeKeys.FinCount; i++)
            {
                Transform? t = SledgepinCubeClosed.FindExact(transform, SledgepinCubeKeys.Names[i]);
                _fins[i] = t;
                if (t == null)
                    continue;
                _scale[i] = t.localScale;
            }
            _ready = true;
        }

        internal void Begin()
        {
            CaptureBindIfNeeded();
            int bound = 0;
            for (int i = 0; i < _fins.Length; i++)
            {
                if (_fins[i] != null)
                    bound++;
            }
            if (bound == 0)
                SledgepinPlugin.ModLog?.LogWarning("SledgepinCubeDriver: bound=0/8 fins missing.");
            else
                SledgepinPlugin.ModLog?.LogInfo($"SledgepinCubeDriver: bound={bound}/{SledgepinCubeKeys.FinCount} playing");

            _elapsed = 0f;
            _playing = true;
            enabled = true;
            ApplyFrame(0f);
        }

        internal void StopClosed()
        {
            _playing = false;
            enabled = false;
            CaptureBindIfNeeded();
            ApplyFrame(0f);
        }

        private void LateUpdate()
        {
            if (!_playing)
                return;

            _elapsed += Time.deltaTime;
            float frame = _elapsed * SledgepinCubeKeys.Fps * SledgepinConstants.OpeningPlaybackRate;
            float last = SledgepinCubeKeys.FrameCount - 1;
            if (frame >= last)
            {
                ApplyFrame(last);
                _playing = false;
                enabled = false;
                return;
            }

            ApplyFrame(frame);
        }

        private void ApplyFrame(float frame)
        {
            if (!_ready)
                return;

            for (int i = 0; i < _fins.Length; i++)
            {
                Transform? t = _fins[i];
                if (t == null)
                    continue;
                t.localScale = _scale[i];
                t.localPosition = SledgepinCubeKeys.SamplePos(i, frame);
                t.localRotation = SledgepinCubeKeys.SampleRot(i, frame);
            }
        }
    }
}
