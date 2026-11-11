using UnityEngine;

namespace WaterInteraction
{
    /// <summary>
    /// 挂到漂浮物体上的唯一入口组件。多个浮点独立采样/施力，自然形成起伏和俯仰、侧倾。
    /// Play Mode 中驱动 Rigidbody；编辑模式可预览采样得到的起伏和倾斜，并恢复原始编辑姿态。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public sealed class ShallowWaterBuoyancy : MonoBehaviour
    {
        [Header("Water Surface")]
        [Tooltip("实际水面的 Renderer，用其共享材质采样 Gerstner 波。留空时启用组件会查找最近的 Custom/Water_Code 水面；建议明确指定。")]
        public Renderer waterSurface;
        [Tooltip("交互波来源。留空时自动使用活动浅水域。")]
        public ShallowWaterSimulation simulation;
        [Tooltip("没有水面 Renderer 时使用的平均高度；存在浅水域时优先使用域的 Y。")]
        public float fallbackWaterLevel;
        [Tooltip("限制浮力在水面 Renderer 的世界 XZ 包围盒内；没有 Renderer 时使用浅水域范围。")]
        public bool limitToWaterBounds = true;
        public bool includeInteractionWaves = true;

        [Header("Float Points (Local Space)")]
        [Tooltip("物体局部坐标中的浮点，均匀分担浮力。默认四角；添加组件时尝试按模型底部自动摆放。可用组件菜单 Fit Float Points To Bounds 重新生成。")]
        public Vector3[] floatPoints =
        {
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, 0.5f), new Vector3(-0.5f, -0.5f, 0.5f)
        };

        [Header("Buoyancy")]
        [Tooltip("完全浸没时总浮力相对物体重量的倍数。2 表示可产生两倍重力；通常需大于 1 才能浮起。按刚体质量归一化，便于不同物体调参。")]
        [Min(0f)] public float buoyancyStrength = 2f;
        [Tooltip("浮点从刚接触水面到满浮力的浸没深度（世界米）。平水面下，浮点的平衡浸深约为此值 / Buoyancy Strength。")]
        [Min(0.001f)] public float submergeDepth = 0.5f;
        [Tooltip("浮点竖直速度的水阻尼，越大越快抑制上下弹跳和侧倾。单位 1/秒，仅在浸水时作用。")]
        [Min(0f)] public float verticalDamping = 4f;
        [Tooltip("浸水后的水平速度衰减，单位 1/秒。")]
        [Min(0f)] public float horizontalDrag = 0.8f;
        [Tooltip("浸水后的旋转速度衰减，单位 1/秒。过大时姿态跟随变迟钝。")]
        [Min(0f)] public float angularDrag = 1.5f;

        [Header("Editor Preview")]
        [Tooltip("未进入 Play Mode 时，在 SceneView 预览漂浮高度和姿态。关闭、保存场景或进入运行前恢复原编辑姿态；不启动场景物理模拟。")]
        public bool previewInEditMode = true;
        [Tooltip("编辑预览高度和旋转的跟随速度，单位 1/秒。仅影响预览，运行时仍由浮力和阻尼决定。")]
        [Min(0f)] public float previewFollowSpeed = 8f;
        [Tooltip("编辑预览相对世界竖直方向的最大倾斜角度。")]
        [Range(0f, 80f)] public float previewMaxTilt = 45f;

        [Header("Interaction Wave Readback")]
        [Tooltip("允许使用的浅水异步高度样本最大年龄（秒）。过期时只跟随平均水面和 Gerstner 波。")]
        [Min(0.01f)] public float maxReadbackAge = 0.25f;
        [Tooltip("浮点相对异步采样位置可移动的最大 XZ 距离（米），防止高速移动时使用远处的旧波纹高度。")]
        [Min(0f)] public float maxReadbackDistance = 0.5f;

        Rigidbody body;
        ShallowWaterSimulation registeredSimulation;
        float[] shallowHeights;
        Vector2[] shallowPositions;
        float[] shallowTimes;
        int[] shallowGenerations;
        int queryVersion;
#if UNITY_EDITOR
        ShallowWaterBuoyancyPreview editorPreview;
#endif

        internal int QueryVersion => queryVersion;
        internal int QueryPointCount
        {
            get
            {
                bool canSample = Application.IsPlaying(gameObject) && body != null && !body.isKinematic;
#if UNITY_EDITOR
                if (!Application.isPlaying) canSample = previewInEditMode && editorPreview != null && editorPreview.IsRunning;
#endif
                return isActiveAndEnabled && canSample && includeInteractionWaves
                    && registeredSimulation != null && floatPoints != null ? floatPoints.Length : 0;
            }
        }

        void Reset() => FitFloatPointsToBounds();

        void OnEnable()
        {
            if (!ShallowWaterSimulation.IsSceneInstance(gameObject)) return;
            body = GetComponent<Rigidbody>();
            queryVersion++;
            if (waterSurface == null) FindWaterSurface();
            EnsureSampleStorage();
            if (Application.IsPlaying(gameObject) && (body.isKinematic || !body.useGravity))
                Debug.LogWarning("浮力需要 Rigidbody 关闭 Is Kinematic 并开启 Use Gravity；姿态跟随还需允许 X/Z 旋转。", this);
#if UNITY_EDITOR
            editorPreview?.Dispose();
            editorPreview = new ShallowWaterBuoyancyPreview(this);
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            editorPreview?.Dispose();
            editorPreview = null;
#endif
            StopHeightSampling();
        }

        internal void StopHeightSampling()
        {
            if (registeredSimulation != null) registeredSimulation.UnregisterBuoyancy(this);
            registeredSimulation = null;
            queryVersion++;
            InvalidateSamples();
        }

        internal void PrepareHeightSampling()
        {
            UpdateSimulationBinding();
            EnsureSampleStorage();
        }

        void UpdateSimulationBinding()
        {
            ShallowWaterSimulation desired = simulation != null ? simulation : ShallowWaterSimulation.Active;
            if (!includeInteractionWaves || (desired != null && !desired.isActiveAndEnabled)) desired = null;
            if (desired == registeredSimulation) return;
            if (registeredSimulation != null) registeredSimulation.UnregisterBuoyancy(this);
            registeredSimulation = desired;
            queryVersion++;
            InvalidateSamples();
            if (registeredSimulation != null) registeredSimulation.RegisterBuoyancy(this);
        }

        void FixedUpdate()
        {
            if (!Application.IsPlaying(gameObject)) return;
            PrepareHeightSampling();
            if (body == null || body.isKinematic || !body.useGravity || floatPoints == null || floatPoints.Length == 0) return;
            ShallowWaterSimulation source = simulation != null ? simulation : ShallowWaterSimulation.Active;
            float baseHeight = BaseWaterHeight(source);
            WaterWaveSampling waves = WaterWaveSampling.FromMaterial(waterSurface != null ? waterSurface.sharedMaterial : null);
            float gravity = Mathf.Max(0f, -Physics.gravity.y);
            float pointShare = 1f / floatPoints.Length;
            float wetFraction = 0f;
            for (int i = 0; i < floatPoints.Length; i++)
            {
                Vector3 point = GetFloatPoint(i);
                if (!ContainsWater(point, source)) continue;
                float height = SurfaceHeight(i, point, source, waves, baseHeight, WaterWaveSampling.SampleTime);
                float immersion = Mathf.Clamp01((height - point.y) / Mathf.Max(0.001f, submergeDepth));
                if (immersion <= 0f) continue;
                wetFraction += immersion * pointShare;
                // 每个浮点产生竖直浮力和水阻；不同浸没程度自然产生恢复姿态的力矩。
                // 乘质量让 Strength 表示重量倍数，Force 模式的质量/惯性仍由 Rigidbody 正常处理。
                float acceleration = (gravity * Mathf.Max(0f, buoyancyStrength)
                    - body.GetPointVelocity(point).y * Mathf.Max(0f, verticalDamping)) * immersion * pointShare;
                body.AddForceAtPosition(Vector3.up * (body.mass * acceleration), point, ForceMode.Force);
            }
            if (wetFraction <= 0f) return;
            Vector3 velocity = body.linearVelocity;
            body.AddForce(-new Vector3(velocity.x, 0f, velocity.z) * (Mathf.Max(0f, horizontalDrag) * wetFraction), ForceMode.Acceleration);
            body.AddTorque(-body.angularVelocity * (Mathf.Max(0f, angularDrag) * wetFraction), ForceMode.Acceleration);
        }

        internal float BaseWaterHeight(ShallowWaterSimulation source) => waterSurface != null ? waterSurface.bounds.center.y
            : source != null ? source.WaterLevel : fallbackWaterLevel;

        internal float SurfaceHeight(int index, Vector3 point, ShallowWaterSimulation source,
            WaterWaveSampling waves, float baseHeight, float time)
        {
            float height = baseHeight + waves.Height(new Vector2(point.x, point.z), time);
            if (source != null && Mathf.Abs(baseHeight - source.WaterLevel) <= source.surfaceHeightTolerance)
                height += ShallowHeight(index, point, source);
            return height;
        }

        internal bool ContainsWater(Vector3 point, ShallowWaterSimulation source)
        {
            if (!limitToWaterBounds) return true;
            if (waterSurface != null)
            {
                Bounds bounds = waterSurface.bounds;
                return point.x >= bounds.min.x && point.x <= bounds.max.x && point.z >= bounds.min.z && point.z <= bounds.max.z;
            }
            if (source == null) return true;
            Vector4 domain = source.Domain;
            return point.x >= domain.x && point.x <= domain.x + domain.z && point.z >= domain.y && point.z <= domain.y + domain.w;
        }

        internal Vector3 GetFloatPoint(int index) => transform.TransformPoint(floatPoints[index]);

        void EnsureSampleStorage()
        {
            int count = floatPoints == null ? 0 : floatPoints.Length;
            if (shallowHeights != null && shallowHeights.Length == count) return;
            shallowHeights = new float[count];
            shallowPositions = new Vector2[count];
            shallowTimes = new float[count];
            shallowGenerations = new int[count];
            queryVersion++;
            InvalidateSamples();
        }

        void InvalidateSamples()
        {
            if (shallowTimes == null) return;
            for (int i = 0; i < shallowTimes.Length; i++) shallowTimes[i] = float.NegativeInfinity;
        }

        internal void ReceiveHeight(ShallowWaterSimulation source, int version, int generation,
            int index, Vector2 position, float height, float sampleTime)
        {
            if (!isActiveAndEnabled || source != registeredSimulation || version != queryVersion
                || shallowHeights == null || index >= shallowHeights.Length) return;
            shallowHeights[index] = height;
            shallowPositions[index] = position;
            shallowTimes[index] = sampleTime;
            shallowGenerations[index] = generation;
        }

        float ShallowHeight(int index, Vector3 point, ShallowWaterSimulation source)
        {
            if (!includeInteractionWaves || registeredSimulation != source
                || shallowGenerations[index] != source.HeightQueryGeneration
                || WaterWaveSampling.SampleTime - shallowTimes[index] > Mathf.Max(0.01f, maxReadbackAge)) return 0f;
            Vector2 distance = new Vector2(point.x, point.z) - shallowPositions[index];
            float limit = Mathf.Max(0f, maxReadbackDistance);
            return distance.sqrMagnitude <= limit * limit ? shallowHeights[index] : 0f;
        }

        void FindWaterSurface()
        {
            float bestDistance = float.PositiveInfinity;
            foreach (Renderer candidate in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!ShallowWaterSimulation.IsSceneInstance(candidate.gameObject)) continue;
                Material material = candidate.sharedMaterial;
                if (material == null || material.shader == null || material.shader.name != "Custom/Water_Code") continue;
                float distance = candidate.bounds.SqrDistance(transform.position);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                waterSurface = candidate;
            }
        }

        [ContextMenu("Fit Float Points To Bounds")]
        public void FitFloatPointsToBounds()
        {
            bool found = false;
            Bounds local = default;
            foreach (Renderer target in GetComponentsInChildren<Renderer>())
                if (target is MeshRenderer || target is SkinnedMeshRenderer)
                    EncapsulateBounds(target.localBounds, target.localToWorldMatrix, ref local, ref found);
            if (!found)
                foreach (Collider target in GetComponentsInChildren<Collider>())
                    EncapsulateBounds(target.bounds, Matrix4x4.identity, ref local, ref found);
            if (!found) local = new Bounds(Vector3.zero, Vector3.one);
            Vector3 center = local.center;
            Vector3 extent = local.extents * 0.8f;
            float y = local.min.y;
            floatPoints = new[]
            {
                new Vector3(center.x - extent.x, y, center.z - extent.z),
                new Vector3(center.x + extent.x, y, center.z - extent.z),
                new Vector3(center.x + extent.x, y, center.z + extent.z),
                new Vector3(center.x - extent.x, y, center.z + extent.z)
            };
            queryVersion++;
            InvalidateSamples();
        }

        void EncapsulateBounds(Bounds bounds, Matrix4x4 toWorld, ref Bounds local, ref bool found)
        {
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 p = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((corner & 1) == 0 ? -1f : 1f, (corner & 2) == 0 ? -1f : 1f, (corner & 4) == 0 ? -1f : 1f));
                p = transform.InverseTransformPoint(toWorld.MultiplyPoint3x4(p));
                if (!found) { local = new Bounds(p, Vector3.zero); found = true; }
                else local.Encapsulate(p);
            }
        }

        void OnDrawGizmosSelected()
        {
            if (floatPoints == null) return;
            Gizmos.color = Color.yellow;
            foreach (Vector3 local in floatPoints)
            {
                Vector3 point = transform.TransformPoint(local);
                Gizmos.DrawWireSphere(point, 0.04f);
                Gizmos.DrawLine(point, point + Vector3.up * Mathf.Max(0.001f, submergeDepth));
            }
        }
    }
}
