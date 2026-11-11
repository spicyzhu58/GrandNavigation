#ifndef SHALLOW_WATER_SAMPLING_INCLUDED
#define SHALLOW_WATER_SAMPLING_INCLUDED

// 全局属性由 ShallowWaterSimulation.RecordFrame 绑定。
// 不要把这些属性放进 Shader Properties / UnityPerMaterial，以免材质值覆盖全局 RT。
TEXTURE2D(_ShallowWaterState);
SAMPLER(sampler_ShallowWaterState);
float4 _ShallowWaterDomain; // minX,minZ,sizeX,sizeZ
float4 _ShallowWaterTexel;  // 1/N,1/N,dx,dz
float _ShallowWaterEnabled;
float _ShallowWaterEdgeFade;
float _ShallowWaterLevel;
float _ShallowWaterHeightTolerance;

float ShallowWaterHeight(float2 worldXZ)
{
    if (_ShallowWaterEnabled < 0.5) return 0.0;
    float2 uv = (worldXZ - _ShallowWaterDomain.xy) / _ShallowWaterDomain.zw;
    if (any(uv <= 0.0) || any(uv >= 1.0)) return 0.0;
    float2 edgeDistance = min(uv, 1.0 - uv) * _ShallowWaterDomain.zw;
    float fade = smoothstep(0.0, max(_ShallowWaterEdgeFade, 1e-4), min(edgeDistance.x, edgeDistance.y));
    // 显式 LOD，既能在顶点阶段使用，又不会因屏幕距离变化切换求解网格。
    return SAMPLE_TEXTURE2D_LOD(_ShallowWaterState, sampler_ShallowWaterState, uv, 0).r * fade;
}

float ShallowWaterSurfaceMask(float baseHeight)
{
    // 同一 XZ 上不同楼层的水不应被这个域一起位移。判断使用位移前的平均平面高度。
    return step(abs(baseHeight - _ShallowWaterLevel), _ShallowWaterHeightTolerance);
}

float3 ApplyShallowWaterNormal(float3 normalWS, float3 positionWS, float baseHeight)
{
    if (_ShallowWaterEnabled < 0.5 || ShallowWaterSurfaceMask(baseHeight) < 0.5) return normalWS;
    float2 cell = _ShallowWaterTexel.zw;
    float dhdx = (ShallowWaterHeight(positionWS.xz + float2(cell.x, 0))
                - ShallowWaterHeight(positionWS.xz - float2(cell.x, 0))) / (2.0 * cell.x);
    float dhdz = (ShallowWaterHeight(positionWS.xz + float2(0, cell.y))
                - ShallowWaterHeight(positionWS.xz - float2(0, cell.y))) / (2.0 * cell.y);
    // 对高度场的法线叠加斜率。片元阶段采样，粗网格也能看到细小法线波纹。
    return normalize(normalWS - float3(dhdx, 0, dhdz) * normalWS.y);
}

#endif
