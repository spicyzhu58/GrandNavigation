#ifndef SHALLOW_WATER_CONTACT_BLUR_INCLUDED
#define SHALLOW_WATER_CONTACT_BLUR_INCLUDED

// 单个交互物体的轮廓先横向平滑，再纵向平滑并合入当前接触 RT。
// 独立于求解状态：只改变激发波纹的空间尺度，不会每帧把已有波纹一起抹平。
Texture2D<float> _SWBlurSource;
SamplerState sampler_SWBlurSource;
float4 _SWBlurSourceDomain; // minX,minZ,sizeX,sizeZ
float4 _SWBlurTargetDomain;
float4 _SWBlurAxis;         // xy：源纹理 UV 中的平滑半径，单次仅一轴非零

struct SWBlurVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
};

SWBlurVaryings SWBlurVert(uint vertexID : SV_VertexID)
{
    SWBlurVaryings output;
    // 一个覆盖整个目标的三角形，不需要创建 Mesh。
    float2 uv = float2((vertexID << 1) & 2, vertexID & 2);
    output.positionCS = float4(uv * 2.0 - 1.0, 0.5, 1.0);
    #if UNITY_UV_STARTS_AT_TOP
        uv.y = 1.0 - uv.y;
    #endif
    output.uv = uv;
    return output;
}

float4 SWBlurFrag(SWBlurVaryings input) : SV_Target
{
    // 工作 RT 的世界范围向外扩过，可包含域外物体对域内边缘的影响。
    // 第二遍写回模拟域，因此通过世界坐标匹配源/目标，而非直接复用 UV。
    float2 worldXZ = _SWBlurTargetDomain.xy + input.uv * _SWBlurTargetDomain.zw;
    float2 sourceUV = (worldXZ - _SWBlurSourceDomain.xy) / _SWBlurSourceDomain.zw;
    float contact = 0.0;
    float weightSum = 0.0;
    // 17 tap 可分离高斯，半径覆盖约 3σ。权重归一化，保持大块轮廓内部强度。
    [unroll]
    for (int tap = -8; tap <= 8; tap++)
    {
        float t = tap / 8.0;
        float weight = exp(-4.5 * t * t);
        float2 uv = sourceUV + _SWBlurAxis.xy * t;
        // 范围外按零处理，避免 Clamp 把边缘接触无限延伸。
        if (all(uv >= 0.0) && all(uv <= 1.0))
            contact += _SWBlurSource.SampleLevel(sampler_SWBlurSource, uv, 0) * weight;
        weightSum += weight;
    }
    return float4(contact / weightSum, 0, 0, 0);
}

#endif
