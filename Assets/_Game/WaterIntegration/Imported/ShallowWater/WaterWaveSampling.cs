using UnityEngine;

namespace WaterInteraction
{
    /// <summary>
    /// WaterWaves.hlsl 的 CPU 高度采样。参数直接来自同一份共享水材质，
    /// 保留三层 Gerstner、噪声遮罩和水平位移，供物理浮点使用。
    /// </summary>
    internal struct WaterWaveSampling
    {
        // 与本项目 URP ScriptableRenderer 设置 _TimeParameters 的时钟一致。
        internal static float SampleTime => Application.isPlaying ? Time.time : Time.realtimeSinceStartup;
        static readonly int WaveAId = Shader.PropertyToID("_WaveA");
        static readonly int WaveBId = Shader.PropertyToID("_WaveB");
        static readonly int WaveCId = Shader.PropertyToID("_WaveC");
        static readonly int SpeedId = Shader.PropertyToID("_WaveSpeed");
        static readonly int MaskScaleId = Shader.PropertyToID("_WaveMaskScale");
        static readonly int MaskRangeId = Shader.PropertyToID("_WaveMaskRange");

        Vector4 a, b, c, speed;
        float maskScale;
        Vector2 maskRange;
        bool valid;

        internal static WaterWaveSampling FromMaterial(Material material)
        {
            if (material == null || !material.HasProperty(WaveAId) || !material.HasProperty(WaveBId)
                || !material.HasProperty(WaveCId) || !material.HasProperty(SpeedId)
                || !material.HasProperty(MaskScaleId) || !material.HasProperty(MaskRangeId)) return default;
            return new WaterWaveSampling
            {
                a = material.GetVector(WaveAId),
                b = material.GetVector(WaveBId),
                c = material.GetVector(WaveCId),
                speed = material.GetVector(SpeedId),
                maskScale = Mathf.Max(material.GetVector(MaskScaleId).x, 1e-5f),
                maskRange = material.GetVector(MaskRangeId),
                valid = true
            };
        }

        internal float Height(Vector2 worldXZ, float time)
        {
            if (!valid) return 0f;
            // Gerstner 会移动 XZ。由最终世界位置反求未位移位置，
            // 避免浮点直接在最终 XZ 上重算正弦而与可见波峰错位。
            // 适用于本水 Shader 的平缓、未打卷波形；迭代是数值近似。
            Vector2 restXZ = worldXZ;
            for (int i = 0; i < 6; i++)
            {
                Vector3 displacement = Offset(restXZ, time);
                restXZ = worldXZ - new Vector2(displacement.x, displacement.z);
            }
            return Offset(restXZ, time).y;
        }

        Vector3 Offset(Vector2 position, float time)
        {
            float noise = GradientNoise(position / maskScale) * 0.5f + 0.5f;
            float end = maskRange.y == maskRange.x ? maskRange.x + 1e-4f : maskRange.y;
            float t = Mathf.Clamp01((noise - maskRange.x) / (end - maskRange.x));
            float mask = t * t * (3f - 2f * t);
            return (Wave(position, a, speed.x, time) + Wave(position, b, speed.y, time)
                + Wave(position, c, speed.z, time)) * mask;
        }

        static Vector3 Wave(Vector2 position, Vector4 wave, float velocity, float time)
        {
            Vector2 direction = new Vector2(wave.x, wave.y);
            float length = direction.magnitude;
            direction = length > 1e-5f ? direction / length : Vector2.right;
            float phase = 2f * Mathf.PI / Mathf.Max(wave.w, 0.0001f)
                * (Vector2.Dot(direction, position) - velocity * time);
            float horizontal = wave.z * Mathf.Cos(phase);
            return new Vector3(direction.x * horizontal, wave.z * Mathf.Sin(phase), direction.y * horizontal);
        }

        static float Frac(float value) => value - Mathf.Floor(value);

        static Vector2 NoiseDirection(Vector2 position)
        {
            const float kx = 0.3183099f, ky = 0.3678794f;
            position = new Vector2(position.x * kx + ky, position.y * ky + kx);
            float f = Frac(position.x * position.y * (position.x + position.y));
            return new Vector2(-1f + 2f * Frac(16f * kx * f), -1f + 2f * Frac(16f * ky * f));
        }

        static float GradientNoise(Vector2 position)
        {
            Vector2 cell = new Vector2(Mathf.Floor(position.x), Mathf.Floor(position.y));
            Vector2 f = position - cell;
            Vector2 u = new Vector2(f.x * f.x * (3f - 2f * f.x), f.y * f.y * (3f - 2f * f.y));
            float n00 = Vector2.Dot(NoiseDirection(cell), f);
            float n10 = Vector2.Dot(NoiseDirection(cell + Vector2.right), f - Vector2.right);
            float n01 = Vector2.Dot(NoiseDirection(cell + Vector2.up), f - Vector2.up);
            float n11 = Vector2.Dot(NoiseDirection(cell + Vector2.one), f - Vector2.one);
            return Mathf.LerpUnclamped(Mathf.LerpUnclamped(n00, n10, u.x), Mathf.LerpUnclamped(n01, n11, u.x), u.y);
        }
    }
}
