using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace WaterInteraction
{
    /// <summary>
    /// URP 17 RenderGraph 入口。在 SSR 和透明水面之前执行俯视捕获与 Compute。
    /// 场景引用保留在 ShallowWaterSimulation；Renderer 资产只需添加此 Feature。
    /// </summary>
    [DisallowMultipleRendererFeature("Shallow Water")]
    public sealed class ShallowWaterRendererFeature : ScriptableRendererFeature
    {
        [Tooltip("默认由带 MainCamera 标签的游戏相机推进一次。关闭后允许任意 Base 游戏相机驱动，模拟器仍按帧去重。")]
        public bool mainCameraOnly = true;

        [Tooltip("允许 SceneView 相机显示和驱动模拟。Main Camera Only 只限制游戏相机。")]
        public bool showInSceneView = true;

        [Tooltip("未进入 Play Mode 时也在 SceneView 预览，约每秒更新 30 次。关闭后停止编辑模拟。")]
        public bool previewInEditMode = true;

        SimulationPass pass;

        public override void Create()
        {
            pass?.Dispose();
            pass = new SimulationPass
            {
                // 现有 WaterSSR 在 BeforeRenderingTransparents，必须早于它。
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques
            };
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= UpdateEditorPreview;
            UnityEditor.EditorApplication.update += UpdateEditorPreview;
#endif
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            if (pass == null) return;
            if (camera.cameraType == CameraType.SceneView)
            {
                if (!showInSceneView || (!Application.isPlaying && !previewInEditMode)) return;
            }
            else if (!Application.isPlaying || camera.cameraType != CameraType.Game
                || renderingData.cameraData.renderType != CameraRenderType.Base
                || (mainCameraOnly && !camera.CompareTag("MainCamera"))) return;
            ShallowWaterSimulation simulation = ShallowWaterSimulation.Active;
            if (simulation == null || simulation.FrameDeltaTime <= 0f) return;
            pass.Setup(simulation);
            renderer.EnqueuePass(pass);
        }

#if UNITY_EDITOR
        void UpdateEditorPreview()
        {
            if (Application.isPlaying || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode
                || UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorApplication.isUpdating
                || !isActive || !showInSceneView || !previewInEditMode || UnityEditor.SceneView.sceneViews.Count == 0) return;
            ShallowWaterSimulation simulation = ShallowWaterSimulation.Active;
            if (simulation == null || !simulation.TickEditorPreview(UnityEditor.EditorApplication.timeSinceStartup)) return;
            // 即使鼠标不动也要让余波继续传播；这里只排队重绘，GPU 命令仍由渲染 Pass 提交。
            UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
            UnityEditor.SceneView.RepaintAll();
        }
#endif

        protected override void Dispose(bool disposing)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= UpdateEditorPreview;
#endif
            pass?.Dispose();
            pass = null;
            Shader.SetGlobalFloat("_ShallowWaterEnabled", 0f);
        }

        sealed class SimulationPass : ScriptableRenderPass
        {
            static readonly int StateId = Shader.PropertyToID("_ShallowWaterState");
            readonly RTHandle[] handles = new RTHandle[6];
            ShallowWaterSimulation simulation;

            sealed class PassData
            {
                public ShallowWaterSimulation simulation;
                public float deltaTime;
                public TextureHandle cameraColor;
                public TextureHandle cameraDepth;
                public Rect viewport;
            }

            public void Setup(ShallowWaterSimulation value) => simulation = value;

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (simulation == null || !simulation.PrepareResources()) return;
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                if (!resources.activeColorTexture.IsValid() || !resources.activeDepthTexture.IsValid()) return;
                UniversalCameraData camera = frameData.Get<UniversalCameraData>();

                // RT 是跨帧历史资源，RenderGraph 只导入包装，绝不接管纹理所有权。
                // 交换的是逻辑读写角色，物理纹理没变，不应每帧重新分配 RTHandle。
                EnsureHandles();
                using (var builder = renderGraph.AddUnsafePass<PassData>("Shallow Water Simulation", out var data))
                {
                    data.simulation = simulation;
                    data.deltaTime = simulation.FrameDeltaTime;
                    data.cameraColor = resources.activeColorTexture;
                    data.cameraDepth = resources.activeDepthTexture;
                    data.viewport = resources.isActiveTargetBackBuffer ? camera.camera.pixelRect
                        : new Rect(0, 0, camera.cameraTargetDescriptor.width, camera.cameraTargetDescriptor.height);
                    // Inject 交换一次，后面的每个子步交换两次，故最终结果一定落在原 scratch。
                    RenderTexture output = simulation.HasRecordedThisFrame ? simulation.StateTexture : simulation.StateScratchTexture;
                    TextureHandle outputHandle = default;
                    for (int i = 0; i < handles.Length; i++)
                    {
                        TextureHandle imported = renderGraph.ImportTexture(handles[i]);
                        builder.UseTexture(imported, AccessFlags.ReadWrite);
                        if (handles[i].rt == output) outputHandle = imported;
                    }
                    // 告诉图后续水面/SSR 会通过全局名字读取模拟结果。
                    builder.SetGlobalTextureAfterPass(outputHandle, StateId);
                    // Capture 会换 RenderTarget。声明并恢复相机目标，不清空、不修改内容。
                    builder.UseTexture(data.cameraColor, AccessFlags.ReadWrite);
                    builder.UseTexture(data.cameraDepth, AccessFlags.ReadWrite);
                    builder.AllowGlobalStateModification(true);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (PassData passData, UnsafeGraphContext context) =>
                    {
                        CommandBuffer cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                        passData.simulation.RecordFrame(cmd, passData.deltaTime);
                        context.cmd.SetRenderTarget(passData.cameraColor, passData.cameraDepth);
                        cmd.SetViewport(passData.viewport);
                    });
                }
            }

            void EnsureHandles()
            {
                if (Contains(simulation.StateTexture) && Contains(simulation.StateScratchTexture)
                    && Contains(simulation.LatestContactTexture) && Contains(simulation.ContactScratchTexture)
                    && Contains(simulation.ContactMaskTexture) && Contains(simulation.ContactFilterTexture)) return;
                Dispose();
                handles[0] = RTHandles.Alloc(simulation.StateTexture, transferOwnership: false);
                handles[1] = RTHandles.Alloc(simulation.StateScratchTexture, transferOwnership: false);
                handles[2] = RTHandles.Alloc(simulation.LatestContactTexture, transferOwnership: false);
                handles[3] = RTHandles.Alloc(simulation.ContactScratchTexture, transferOwnership: false);
                handles[4] = RTHandles.Alloc(simulation.ContactMaskTexture, transferOwnership: false);
                handles[5] = RTHandles.Alloc(simulation.ContactFilterTexture, transferOwnership: false);
            }

            bool Contains(RenderTexture texture)
            {
                for (int i = 0; i < handles.Length; i++)
                    if (handles[i] != null && handles[i].rt == texture) return true;
                return false;
            }

            public void Dispose()
            {
                for (int i = 0; i < handles.Length; i++)
                {
                    handles[i]?.Release();
                    handles[i] = null;
                }
            }
        }
    }
}
