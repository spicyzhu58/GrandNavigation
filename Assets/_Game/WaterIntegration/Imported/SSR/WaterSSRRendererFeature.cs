using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering;
/// <summary>
/// 在透明物体之前，把带 WaterSSR pass 的水面画进 _WaterSSRTexture。
/// 水面 Forward 用屏幕 UV 采样这张 RT：rgb 是反射颜色，a 是命中置信度。
/// </summary>
[DisallowMultipleRendererFeature("Water SSR")]
public class WaterSSRRendererFeature : ScriptableRendererFeature
{
    [Header("Ray March")]
    [Tooltip("反射射线的世界空间最大长度。")]
    public float maxDistance = 40f;

    [Tooltip("屏幕空间每次步进的像素数。越小越不容易漏掉薄物体，也越贵。")]
    [Range(1f, 32f)]
    public float pixelStride = 4f;

    [Tooltip("单次反射最多步进多少次。")]
    [Range(1, 64)]
    public int maxSteps = 32;

    [Tooltip("判定命中的视空间厚度。越大越容易命中，也越容易穿到物体里面。")]
    public float thickness = 0.75f;

    [Tooltip("粗步进命中后的二分查找次数，用来把交点收准。")]
    [Range(0, 8)]
    public int binarySearchSteps = 5;

    [Tooltip("靠近屏幕边缘时把置信度淡出，避免反射被屏幕边界截断。")]
    [Range(0.001f, 0.25f)]
    public float edgeFade = 0.05f;

    [Tooltip("沿法线把射线起点往外推一点，减少立刻打到脚下水底。")]
    public float surfaceBias = 0.05f;

    [Tooltip("只绘制这些 Layer 上、且 Shader 含 WaterSSR Pass 的物体。")]
    public LayerMask layerMask = ~0;

    WaterSSRPass _mPass;

    public override void Create()
    {
        _mPass = new WaterSSRPass
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        CameraType cameraType = renderingData.cameraData.cameraType;
        bool skip = _mPass == null
                    || renderingData.cameraData.camera.orthographic
                    || cameraType == CameraType.Preview
                    || cameraType == CameraType.Reflection
                    || renderingData.cameraData.xrRendering;

        if (skip)
        {
            Shader.SetGlobalFloat(WaterSSRPass.EnabledId, 0f);
            return;
        }

        _mPass.Setup(this);
        _mPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        renderer.EnqueuePass(_mPass);
    }

    protected override void Dispose(bool disposing)
    {
        Shader.SetGlobalFloat(WaterSSRPass.EnabledId, 0f);
    }

    class WaterSSRPass : ScriptableRenderPass
    {
        public static readonly int EnabledId = Shader.PropertyToID("_WaterSSREnabled");
        static readonly int TextureId = Shader.PropertyToID("_WaterSSRTexture");
        static readonly int ParamsId = Shader.PropertyToID("_WaterSSRParams");
        static readonly int Params2Id = Shader.PropertyToID("_WaterSSRParams2");

        readonly List<ShaderTagId> m_ShaderTags = new List<ShaderTagId> { new ShaderTagId("WaterSSR") };

        LayerMask m_LayerMask;
        Vector4 m_Params;
        Vector4 m_Params2;

        class PassData
        {
            public RendererListHandle rendererList;
            public Vector4 ssrParams;
            public Vector4 ssrParams2;
        }

        public void Setup(WaterSSRRendererFeature feature)
        {
            m_LayerMask = feature.layerMask;
            m_Params = new Vector4(
                Mathf.Max(0.01f, feature.maxDistance),
                Mathf.Max(1f, feature.pixelStride),
                Mathf.Max(0.001f, feature.thickness),
                Mathf.Clamp(feature.maxSteps, 1, 64));
            m_Params2 = new Vector4(
                Mathf.Clamp(feature.binarySearchSteps, 0, 8),
                Mathf.Max(0.001f, feature.edgeFade),
                feature.surfaceBias,
                0f);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (!resourceData.cameraColor.IsValid()
                || !resourceData.cameraDepthTexture.IsValid()
                || !resourceData.cameraOpaqueTexture.IsValid())
            {
                Shader.SetGlobalFloat(EnabledId, 0f);
                return;
            }

            TextureDesc desc = renderGraph.GetTextureDesc(resourceData.cameraColor);
            desc.colorFormat = GraphicsFormat.R16G16B16A16_SFloat;
            desc.name = "_WaterSSRTexture";
            desc.clearBuffer = true;
            desc.clearColor = Color.clear;
            desc.filterMode = FilterMode.Bilinear;
            desc.wrapMode = TextureWrapMode.Clamp;
            desc.msaaSamples = MSAASamples.None;
            desc.useMipMap = false;
            desc.autoGenerateMips = false;
            desc.enableRandomWrite = false;
            TextureHandle ssrTexture = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Water SSR", out PassData passData))
            {
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                FilteringSettings filter = new FilteringSettings(RenderQueueRange.all, m_LayerMask);
                DrawingSettings draw = RenderingUtils.CreateDrawingSettings(
                    m_ShaderTags, renderingData, cameraData, lightData, SortingCriteria.CommonTransparent);
                RendererListParams listParams = new RendererListParams(renderingData.cullResults, draw, filter);
                passData.rendererList = renderGraph.CreateRendererList(listParams);
                passData.ssrParams = m_Params;
                passData.ssrParams2 = m_Params2;

                builder.UseRendererList(passData.rendererList);
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                builder.UseTexture(resourceData.cameraOpaqueTexture, AccessFlags.Read);
                builder.SetRenderAttachment(ssrTexture, 0, AccessFlags.Write);
                builder.SetGlobalTextureAfterPass(ssrTexture, TextureId);
                builder.AllowGlobalStateModification(true);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    context.cmd.SetGlobalFloat(EnabledId, 1f);
                    context.cmd.SetGlobalVector(ParamsId, data.ssrParams);
                    context.cmd.SetGlobalVector(Params2Id, data.ssrParams2);
                    context.cmd.DrawRendererList(data.rendererList);
                });
            }
        }
    }
}
