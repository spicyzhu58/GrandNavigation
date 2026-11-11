using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace WaterInteraction
{
    /// <summary>
    /// 一个世界 XZ 平面的浅水域：管理 RT、录制俯视轮廓、推进求解器并发布 Shader 全局参数。
    /// 不创建 Camera，不自动提交命令；由 ShallowWaterRendererFeature 每帧调用 RecordFrame。
    /// 当前水 Shader 通过全局纹理接入，因此同一时间只允许一个活动域。
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class ShallowWaterSimulation : MonoBehaviour
    {
        public static ShallowWaterSimulation Active { get; private set; }
        const int MaxImpulses = 64;

        [Header("Resources")]
        [SerializeField] ComputeShader solver;
        [SerializeField] Shader captureShader;

        [Header("Top Down Domain (World XZ)")]
        [Tooltip("世界空间覆盖尺寸（米）。Transform.position 是中心，position.y 是平均水面；忽略旋转和缩放。")]
        public Vector2 size = new Vector2(20f, 20f);
        [Range(32, 2048)] public int resolution = 256;
        [Tooltip("只捕获此 LayerMask 内、挂了 ShallowWaterInteractor 的 Renderer。")]
        public LayerMask interactorLayers = ~0;
        [Tooltip("允许接入此域的水面平均高度与域中心 Y 的误差（米）。")]
        [Min(0.001f)] public float surfaceHeightTolerance = 0.1f;

        [Header("Linear Shallow Water")]
        [Tooltip("恒定静水深 H（米），波速 c = sqrt(gH)。这是模拟参数，不是相机深度。")]
        [Min(0.001f)] public float waterDepth = 0.5f;
        [Min(0.001f)] public float gravity = 9.81f;
        [Tooltip("每秒衰减率。0 表示不做全域衰减。")]
        [Min(0f)] public float damping = 0.35f;
        [Tooltip("水位偏移的绝对上限（米）。建议远小于 H，频繁触限会损失体积/能量。")]
        [Min(0.001f)] public float maxHeight = 0.15f;
        [Tooltip("CFL 安全系数；自动按网格尺寸和波速划分子步。")]
        [Range(0.1f, 0.95f)] public float cfl = 0.8f;
        [Tooltip("单帧最大子步数。帧时间超预算时减慢模拟，而不是破坏稳定条件。")]
        [Range(1, 128)] public int maxSubsteps = 16;
        [Tooltip("最多接受的单帧时间，避免暂停后补算大量历史。")]
        [Min(0.001f)] public float maxFrameDelta = 0.1f;

        [Header("Boundary")]
        [Tooltip("海绵吸收带宽度（米），0 表示封闭反射边界。")]
        [Min(0f)] public float boundaryWidth = 2f;
        [Min(0f)] public float boundaryDamping = 5f;
        [Tooltip("水 Shader 在域边缘淡出交互高度的距离（米）。")]
        [Min(0.001f)] public float edgeFade = 0.5f;

        // 两张状态 RT 交替读写；两张接触 RT 保存当前/上一帧轮廓。
        RenderTexture stateRead, stateWrite, contactNow, contactPrevious;
        // 全部物体共用两张工作 RT；逐个捕获/平滑/合并，不为每个物体常驻分配纹理。
        RenderTexture contactMask, contactFilter;
        Material captureMaterial;
        ComputeShader allocatedSolver;
        Vector4 allocatedDomain;
        float allocatedLevel;
        int allocatedResolution;
        int clearKernel, injectKernel, velocityKernel, heightKernel;
        int heightQueryKernel;
        Vector2Int pendingScroll;
        public int ScrollCount {get;private set;}
        readonly List<ShallowWaterBuoyancy> floatingBodies = new List<ShallowWaterBuoyancy>();
        ShallowWaterHeightQueries heightQueries;
        internal int HeightQueryGeneration { get; private set; }
        bool needsClear = true;
        bool reportedResourceError;
        int lastRecordedFrame = -1;
        readonly Vector4[] impulses = new Vector4[MaxImpulses];
        int impulseCount;
#if UNITY_EDITOR
        // 编辑模式的 Time.frameCount / deltaTime 不可靠，使用预览节拍去重。
        int editorPreviewTick = 1;
        double lastEditorPreviewTime = -1.0;
        float editorPreviewDelta = 1f / 30f;
#endif

        static readonly int EnabledId = Shader.PropertyToID("_ShallowWaterEnabled");
        static readonly int StateId = Shader.PropertyToID("_ShallowWaterState");
        static readonly int DomainId = Shader.PropertyToID("_ShallowWaterDomain");
        static readonly int TexelId = Shader.PropertyToID("_ShallowWaterTexel");
        static readonly int EdgeFadeId = Shader.PropertyToID("_ShallowWaterEdgeFade");
        static readonly int LevelId = Shader.PropertyToID("_ShallowWaterLevel");
        static readonly int ToleranceId = Shader.PropertyToID("_ShallowWaterHeightTolerance");
        static readonly int CaptureDomainId = Shader.PropertyToID("_SWCaptureDomain");
        static readonly int ContactValueId = Shader.PropertyToID("_SWContactValue");
        static readonly int BlurSourceId = Shader.PropertyToID("_SWBlurSource");
        static readonly int BlurSourceDomainId = Shader.PropertyToID("_SWBlurSourceDomain");
        static readonly int BlurTargetDomainId = Shader.PropertyToID("_SWBlurTargetDomain");
        static readonly int BlurAxisId = Shader.PropertyToID("_SWBlurAxis");
        static readonly int StateReadId = Shader.PropertyToID("_StateRead");
        static readonly int StateWriteId = Shader.PropertyToID("_StateWrite");
        static readonly int ContactNowId = Shader.PropertyToID("_ContactNow");
        static readonly int ContactPreviousId = Shader.PropertyToID("_ContactPrevious");
        static readonly int ResolutionId = Shader.PropertyToID("_Resolution");
        static readonly int SolverDomainId = Shader.PropertyToID("_Domain");
        static readonly int CellId = Shader.PropertyToID("_Cell");
        static readonly int DeltaTimeId = Shader.PropertyToID("_DeltaTime");
        static readonly int GravityId = Shader.PropertyToID("_Gravity");
        static readonly int DepthId = Shader.PropertyToID("_WaterDepth");
        static readonly int DampingId = Shader.PropertyToID("_Damping");
        static readonly int BoundaryWidthId = Shader.PropertyToID("_BoundaryWidth");
        static readonly int BoundaryDampingId = Shader.PropertyToID("_BoundaryDamping");
        static readonly int MaxHeightId = Shader.PropertyToID("_MaxHeight");
        static readonly int ImpulseCountId = Shader.PropertyToID("_ImpulseCount");
        static readonly int ImpulsesId = Shader.PropertyToID("_ImpulsePositionRadius");

        /// <summary>会在录制时交换引用。调试和 RenderGraph 导入时不要永久缓存单张引用。</summary>
        public RenderTexture StateTexture => stateRead;
        public RenderTexture StateScratchTexture => stateWrite;
        /// <summary>录制完成后是本帧捕获结果，因为接触 RT 已交换供下一帧使用。</summary>
        public RenderTexture LatestContactTexture => contactPrevious;
        public RenderTexture ContactScratchTexture => contactNow;
        public RenderTexture ContactMaskTexture => contactMask;
        public RenderTexture ContactFilterTexture => contactFilter;
        internal bool HasRecordedThisFrame => lastRecordedFrame == FrameToken;
        int FrameToken
        {
            get
            {
#if UNITY_EDITOR
                // 编辑节拍取负数，与 Play Mode 的帧号隔离。
                if (!Application.isPlaying) return ~editorPreviewTick;
#endif
                return Time.frameCount;
            }
        }

        internal float FrameDeltaTime
        {
            get
            {
#if UNITY_EDITOR
                if (!Application.isPlaying) return editorPreviewDelta;
#endif
                return Time.deltaTime;
            }
        }
        public float WaterLevel => transform.position.y;
        public Vector4 Domain => new Vector4(transform.position.x - Mathf.Max(0.01f, size.x) * 0.5f,
            transform.position.z - Mathf.Max(0.01f, size.y) * 0.5f, Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y));
        /// <summary>本帧实际推进的时间，达到子步预算时可能小于输入帧时间。</summary>
        public float LastSimulatedDelta { get; private set; }

        void OnEnable()
        {
            if (!IsSceneInstance(gameObject)) return;
            if (Active != null && Active != this)
            {
                Debug.LogError("浅水全局接入只支持一个活动域。请禁用另一个 ShallowWaterSimulation。", this);
                if (Application.IsPlaying(gameObject)) enabled = false;
                return;
            }
            Active = this;
            needsClear = true;
            lastRecordedFrame = -1;
#if UNITY_EDITOR
            lastEditorPreviewTime = -1.0;
#endif
            Shader.SetGlobalFloat(EnabledId, 0f);
        }

        internal static bool IsSceneInstance(GameObject target)
        {
#if UNITY_EDITOR
            // ExecuteAlways 也会运行在 Prefab Stage，不能让它注册进场景的全局域/交互列表。
            if (UnityEditor.EditorUtility.IsPersistent(target)
                || UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(target) != null) return false;
#endif
            return !Application.isPlaying || Application.IsPlaying(target);
        }

#if UNITY_EDITOR
        internal bool TickEditorPreview(double now)
        {
            const double interval = 1.0 / 30.0;
            double elapsed = lastEditorPreviewTime < 0.0 ? interval : now - lastEditorPreviewTime;
            if (elapsed < interval) return false;
            editorPreviewDelta = (float)Math.Min(elapsed, 0.1);
            lastEditorPreviewTime = now;
            unchecked { editorPreviewTick++; }
            return true;
        }
#endif

        void OnDisable()
        {
            if (Active == this)
            {
                Active = null;
                Shader.SetGlobalFloat(EnabledId, 0f);
                Shader.SetGlobalTexture(StateId, Texture2D.blackTexture);
            }
            ReleaseResources();
            impulseCount = 0;
        }

        /// <summary>清空历史，下次 RecordFrame 生效；不在这里提交 GPU 命令。</summary>
        public void ResetSimulation()
        {
            needsClear = true;
            impulseCount = 0;
            ResetHeightQueries();
        }

        internal void RegisterBuoyancy(ShallowWaterBuoyancy body)
        {
            if (!floatingBodies.Contains(body)) floatingBodies.Add(body);
        }

        internal void UnregisterBuoyancy(ShallowWaterBuoyancy body) => floatingBodies.Remove(body);

        void ResetHeightQueries()
        {
            HeightQueryGeneration++;
            heightQueries?.Dispose();
            heightQueries = null;
        }

        /// <summary>
        /// 排队一个圆形水位脉冲：XZ/半径/高度均为世界单位，Y 不参与定位。
        /// 落水、脚步、雨点可直接调用；返回 false 表示无效参数、域外或本帧已满 64 个。
        /// 在 RecordFrame 之前调用；每个脉冲只消费一次，强度不随子步数变化。
        /// </summary>
        public bool AddImpulse(Vector3 worldPosition, float radius, float height)
        {
            if (!isActiveAndEnabled || radius <= 0f || !IsFinite(radius) || !IsFinite(height)
                || !IsFinite(worldPosition.x) || !IsFinite(worldPosition.z) || impulseCount == MaxImpulses)
                return false;
            Vector4 domain = Domain;
            if (worldPosition.x + radius < domain.x || worldPosition.x - radius > domain.x + domain.z
                || worldPosition.z + radius < domain.y || worldPosition.z - radius > domain.y + domain.w)
                return false;
            impulses[impulseCount++] = new Vector4(worldPosition.x, worldPosition.z, radius, height);
            return true;
        }

        static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        /// <summary>
        /// 在 RenderGraph 导入 RT 之前调用。资源只在域/分辨率/资源引用变化时重建。
        /// 中心按整数像素移动时滚动历史；尺寸、分辨率或平均水位变化时重建。
        /// </summary>
        public bool PrepareResources()
        {
            if (!isActiveAndEnabled || Active != this) return false;
            if (solver == null || captureShader == null || !SystemInfo.supportsComputeShaders
                || !SystemInfo.SupportsRandomWriteOnRenderTextureFormat(RenderTextureFormat.ARGBFloat)
                || !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf)
                || !SystemInfo.SupportsBlendingOnRenderTextureFormat(RenderTextureFormat.RHalf)
                || !captureShader.isSupported)
            {
                if (!reportedResourceError)
                    Debug.LogError("浅水初始化失败：请指定 Solver / Capture Shader，设备需支持 Compute、ARGBFloat UAV 和 RHalf 混合。", this);
                reportedResourceError = true;
                Shader.SetGlobalFloat(EnabledId, 0f);
                return false;
            }
            reportedResourceError = false;
            Vector4 domain = Domain;
            int count = Mathf.Clamp(resolution, 32, Mathf.Min(2048, SystemInfo.maxTextureSize));
            if (stateRead != null && stateRead.IsCreated() && stateWrite != null && stateWrite.IsCreated()
                && contactNow != null && contactNow.IsCreated() && contactPrevious != null && contactPrevious.IsCreated()
                && contactMask != null && contactMask.IsCreated() && contactFilter != null && contactFilter.IsCreated()
                && allocatedSolver == solver && captureMaterial != null && captureMaterial.shader == captureShader
                && allocatedResolution == count && allocatedDomain.Equals(domain) && allocatedLevel.Equals(WaterLevel))
                return true;

            if(stateRead != null && allocatedSolver==solver && allocatedResolution==count
                && allocatedDomain.z==domain.z && allocatedDomain.w==domain.w && allocatedLevel==WaterLevel)
            {
                pendingScroll+=new Vector2Int(Mathf.RoundToInt((domain.x-allocatedDomain.x)*count/domain.z),Mathf.RoundToInt((domain.y-allocatedDomain.y)*count/domain.w));
                allocatedDomain=domain;return true;
            }
            ReleaseResources();pendingScroll=Vector2Int.zero;
            allocatedDomain = domain;
            allocatedLevel = WaterLevel;
            allocatedResolution = count;
            allocatedSolver = solver;
            captureMaterial = new Material(captureShader) { hideFlags = HideFlags.HideAndDontSave };
            clearKernel = solver.FindKernel("ClearState");
            injectKernel = solver.FindKernel("Inject");
            velocityKernel = solver.FindKernel("UpdateVelocity");
            heightKernel = solver.FindKernel("UpdateHeight");
            heightQueryKernel = solver.FindKernel("SampleHeights");
            stateRead = CreateTexture("Shallow Water State A", RenderTextureFormat.ARGBFloat, true);
            stateWrite = CreateTexture("Shallow Water State B", RenderTextureFormat.ARGBFloat, true);
            contactNow = CreateTexture("Shallow Water Contact A", RenderTextureFormat.RHalf, false);
            contactPrevious = CreateTexture("Shallow Water Contact B", RenderTextureFormat.RHalf, false);
            contactMask = CreateTexture("Shallow Water Contact Mask", RenderTextureFormat.RHalf, false);
            contactFilter = CreateTexture("Shallow Water Contact Filter", RenderTextureFormat.RHalf, false);
            needsClear = true;
            lastRecordedFrame = -1;
            return true;
        }

        RenderTexture CreateTexture(string label, RenderTextureFormat format, bool randomWrite)
        {
            var texture = new RenderTexture(allocatedResolution, allocatedResolution, 0, format, RenderTextureReadWrite.Linear)
            {
                name = label,
                enableRandomWrite = randomWrite,
                useMipMap = false,
                autoGenerateMips = false,
                antiAliasing = 1,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (!texture.Create())
            {
                DestroyOwned(texture);
                throw new InvalidOperationException("无法创建浅水 RT: " + label);
            }
            return texture;
        }

        /// <summary>
        /// 录制：初始化 → 俯视捕获 → 注入 → CFL 子步 → 绑定水 Shader。
        /// 调用方必须执行这些命令，并在此之后恢复相机 RenderTarget/Viewport。
        /// 每个游戏帧/编辑预览节拍只推进一次；重复相机调用只重新绑定。不要在被裁剪/丢弃的 Pass 中录制。
        /// RenderGraph 使用一个明确声明全部 6 张 RT 为 ReadWrite 的 Unsafe Pass 承载本方法。
        /// </summary>
        public bool RecordFrame(CommandBuffer cmd, float deltaTime)
        {
            if (cmd == null) throw new ArgumentNullException(nameof(cmd));
            if (!PrepareResources()) return false;
            if (HasRecordedThisFrame)
            {
                BindGlobals(cmd);
                return true;
            }
            if (!IsFinite(deltaTime) || deltaTime <= 0f) return false;

            cmd.BeginSample("Shallow Water");
            cmd.SetComputeIntParam(solver, ResolutionId, allocatedResolution);
            if (needsClear)
            {
                ClearState(cmd, stateRead);
                ClearState(cmd, stateWrite);
                ClearContact(cmd, contactPrevious);
                needsClear = false;
            }
            bool scrolled=pendingScroll!=Vector2Int.zero;
            if(scrolled)
            {
                int kernel=solver.FindKernel("ScrollState");
                cmd.SetComputeIntParams(solver,"_ScrollOffset",pendingScroll.x,pendingScroll.y);
                cmd.SetComputeTextureParam(solver,kernel,StateReadId,stateRead);cmd.SetComputeTextureParam(solver,kernel,StateWriteId,stateWrite);
                cmd.DispatchCompute(solver,kernel,(allocatedResolution+7)/8,(allocatedResolution+7)/8,1);
                cmd.CopyTexture(stateWrite,stateRead);pendingScroll=Vector2Int.zero;ScrollCount++;
            }
            CaptureInteractors(cmd);
            SetSolverParameters(cmd);
            cmd.SetComputeIntParam(solver, ImpulseCountId, impulseCount);
            // CommandBuffer 记录数组值，不能直接修改共享 ComputeShader 的状态。
            cmd.SetComputeVectorArrayParam(solver, ImpulsesId, impulses);
            cmd.SetComputeTextureParam(solver, injectKernel, ContactNowId, contactNow);
            cmd.SetComputeTextureParam(solver, injectKernel, ContactPreviousId, scrolled?contactNow:contactPrevious);
            DispatchState(cmd, injectKernel);
            impulseCount = 0;

            float dx = allocatedDomain.z / allocatedResolution;
            float dz = allocatedDomain.w / allocatedResolution;
            float waveSpeed = Mathf.Sqrt(Mathf.Max(0.001f, gravity) * Mathf.Max(0.001f, waterDepth));
            // 2D CFL：c*dt*sqrt(1/dx²+1/dz²) <= 1，而不是仅检查单轴。
            float stableDelta = Mathf.Clamp(cfl, 0.1f, 0.95f) / (waveSpeed * Mathf.Sqrt(1f / (dx * dx) + 1f / (dz * dz)));
            int budget = Mathf.Clamp(maxSubsteps, 1, 128);
            LastSimulatedDelta = Mathf.Min(deltaTime, Mathf.Max(0.001f, maxFrameDelta), stableDelta * budget);
            int steps = Mathf.Clamp(Mathf.CeilToInt(LastSimulatedDelta / stableDelta), 1, budget);
            cmd.SetComputeFloatParam(solver, DeltaTimeId, LastSimulatedDelta / steps);
            for (int i = 0; i < steps; i++)
            {
                // 必须先写完全部边界速度，再用新速度更新水位；不能合并成原地读写。
                DispatchState(cmd, velocityKernel);
                DispatchState(cmd, heightKernel);
            }
            Swap(ref contactNow, ref contactPrevious);
            if (floatingBodies.Count > 0)
            {
                if (heightQueries == null) heightQueries = new ShallowWaterHeightQueries();
                // 查询最终高度，不交换状态 RT；只异步回读浮点样本，保持水 Shader 的输出引用不变。
                heightQueries.Record(cmd, solver, heightQueryKernel, stateRead, this, floatingBodies);
            }
            BindGlobals(cmd);
            lastRecordedFrame = FrameToken;
            cmd.EndSample("Shallow Water");
            return true;
        }

        void SetSolverParameters(CommandBuffer cmd)
        {
            float dx = allocatedDomain.z / allocatedResolution;
            float dz = allocatedDomain.w / allocatedResolution;
            cmd.SetComputeVectorParam(solver, SolverDomainId, allocatedDomain);
            cmd.SetComputeVectorParam(solver, CellId, new Vector4(dx, dz, 1f / dx, 1f / dz));
            cmd.SetComputeFloatParam(solver, GravityId, Mathf.Max(0.001f, gravity));
            cmd.SetComputeFloatParam(solver, DepthId, Mathf.Max(0.001f, waterDepth));
            cmd.SetComputeFloatParam(solver, DampingId, Mathf.Max(0f, damping));
            cmd.SetComputeFloatParam(solver, BoundaryWidthId, Mathf.Max(0f, boundaryWidth));
            cmd.SetComputeFloatParam(solver, BoundaryDampingId, Mathf.Max(0f, boundaryDamping));
            cmd.SetComputeFloatParam(solver, MaxHeightId, Mathf.Max(0.001f, maxHeight));
        }

        void CaptureInteractors(CommandBuffer cmd)
        {
            ClearContact(cmd, contactNow);
            foreach (ShallowWaterInteractor interactor in ShallowWaterInteractor.Instances)
            {
                if (interactor == null || !interactor.isActiveAndEnabled) continue;
                float width = Mathf.Max(0f, interactor.rippleWidth);
                if (width == 0f)
                {
                    // 未开启宽度调整时保留原来的直接合并路径。
                    cmd.SetRenderTarget(contactNow);
                    DrawInteractor(cmd, interactor, allocatedDomain);
                    continue;
                }

                // 扩大工作范围，避免物体在域外但平滑范围触及域内时被提前裁掉。
                Vector4 paddedDomain = new Vector4(allocatedDomain.x - width, allocatedDomain.y - width,
                    allocatedDomain.z + 2f * width, allocatedDomain.w + 2f * width);
                ClearContact(cmd, contactMask);
                if (!DrawInteractor(cmd, interactor, paddedDomain)) continue;
                cmd.SetGlobalVector(BlurSourceDomainId, paddedDomain);
                cmd.SetGlobalVector(BlurTargetDomainId, paddedDomain);
                cmd.SetGlobalTexture(BlurSourceId, contactMask);
                cmd.SetGlobalVector(BlurAxisId, new Vector4(width / paddedDomain.z, 0f, 0f, 0f));
                cmd.SetRenderTarget(contactFilter);
                cmd.DrawProcedural(Matrix4x4.identity, captureMaterial, 1, MeshTopology.Triangles, 3);

                cmd.SetGlobalVector(BlurTargetDomainId, allocatedDomain);
                cmd.SetGlobalTexture(BlurSourceId, contactFilter);
                cmd.SetGlobalVector(BlurAxisId, new Vector4(0f, width / paddedDomain.w, 0f, 0f));
                cmd.SetRenderTarget(contactNow);
                cmd.DrawProcedural(Matrix4x4.identity, captureMaterial, 2, MeshTopology.Triangles, 3);
            }
        }

        bool DrawInteractor(CommandBuffer cmd, ShallowWaterInteractor interactor, Vector4 captureDomain)
        {
            cmd.SetGlobalVector(CaptureDomainId, captureDomain);
            bool hasDraw = false;
            foreach (Renderer target in interactor.TargetRenderers)
            {
                if (target == null || !target.enabled || !target.gameObject.activeInHierarchy
                    || (interactorLayers.value & (1 << target.gameObject.layer)) == 0) continue;
                Bounds bounds = target.bounds;
                if (bounds.max.x < captureDomain.x || bounds.min.x > captureDomain.x + captureDomain.z
                    || bounds.max.z < captureDomain.y || bounds.min.z > captureDomain.y + captureDomain.w) continue;
                float value = interactor.GetContactValue(bounds, allocatedLevel);
                if (value <= 0f) continue;
                Mesh mesh = null;
                if (target is SkinnedMeshRenderer skinned) mesh = skinned.sharedMesh;
                else if (target is MeshRenderer && target.TryGetComponent(out MeshFilter filter)) mesh = filter.sharedMesh;
                if (mesh == null) continue;
                cmd.SetGlobalFloat(ContactValueId, value);
                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    cmd.DrawRenderer(target, captureMaterial, submesh, 0);
                hasDraw = true;
            }
            return hasDraw;
        }

        void ClearContact(CommandBuffer cmd, RenderTexture texture)
        {
            cmd.SetRenderTarget(texture);
            cmd.SetViewport(new Rect(0, 0, allocatedResolution, allocatedResolution));
            cmd.ClearRenderTarget(false, true, Color.clear);
        }

        void ClearState(CommandBuffer cmd, RenderTexture texture)
        {
            cmd.SetComputeTextureParam(solver, clearKernel, StateWriteId, texture);
            cmd.DispatchCompute(solver, clearKernel, (allocatedResolution + 7) / 8, (allocatedResolution + 7) / 8, 1);
        }

        void DispatchState(CommandBuffer cmd, int kernel)
        {
            cmd.SetComputeTextureParam(solver, kernel, StateReadId, stateRead);
            cmd.SetComputeTextureParam(solver, kernel, StateWriteId, stateWrite);
            cmd.DispatchCompute(solver, kernel, (allocatedResolution + 7) / 8, (allocatedResolution + 7) / 8, 1);
            Swap(ref stateRead, ref stateWrite);
        }

        void BindGlobals(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(StateId, stateRead);
            cmd.SetGlobalVector(DomainId, allocatedDomain);
            cmd.SetGlobalVector(TexelId, new Vector4(1f / allocatedResolution, 1f / allocatedResolution,
                allocatedDomain.z / allocatedResolution, allocatedDomain.w / allocatedResolution));
            cmd.SetGlobalFloat(EdgeFadeId, Mathf.Max(0.001f, edgeFade));
            cmd.SetGlobalFloat(LevelId, allocatedLevel);
            cmd.SetGlobalFloat(ToleranceId, Mathf.Max(0.001f, surfaceHeightTolerance));
            cmd.SetGlobalFloat(EnabledId, 1f);
        }

        static void Swap(ref RenderTexture a, ref RenderTexture b)
        {
            RenderTexture temporary = a;
            a = b;
            b = temporary;
        }

        void ReleaseResources()
        {
            ResetHeightQueries();
            ReleaseTexture(ref stateRead);
            ReleaseTexture(ref stateWrite);
            ReleaseTexture(ref contactNow);
            ReleaseTexture(ref contactPrevious);
            ReleaseTexture(ref contactMask);
            ReleaseTexture(ref contactFilter);
            if (captureMaterial != null) DestroyOwned(captureMaterial);
            captureMaterial = null;
            allocatedSolver = null;
        }

        static void ReleaseTexture(ref RenderTexture texture)
        {
            if (texture == null) return;
            texture.Release();
            DestroyOwned(texture);
            texture = null;
        }

        static void DestroyOwned(UnityEngine.Object value)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(value);
            else UnityEngine.Object.DestroyImmediate(value);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireCube(transform.position, new Vector3(Mathf.Max(0.01f, size.x), 0.02f, Mathf.Max(0.01f, size.y)));
        }
    }
}
