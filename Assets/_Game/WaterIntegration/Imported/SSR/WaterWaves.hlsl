#ifndef WATER_WAVES_INCLUDED
#define WATER_WAVES_INCLUDED

TEXTURE2D(_NoiseTex);
SAMPLER(sampler_NoiseTex);

// 顶点阶段没有自动计算纹理 LOD 所需的屏幕导数，使用显式 LOD
float SampleWaterNoiseMask(float2 baseWorldXZ)
{
    float2 noiseUV = baseWorldXZ / 1000.0 / max(_NoiseScale.x, 1e-5);
    float2 noiseUV2 = baseWorldXZ / 1000.0 / max(_NoiseScale.y, 1e-5);
    float2 noiseFlow = _NoiseSpeed * _TimeParameters.x * 0.01;

    float noiseA = SAMPLE_TEXTURE2D_LOD (_NoiseTex, sampler_NoiseTex, noiseUV + noiseFlow, 0).r;
    float noiseB = SAMPLE_TEXTURE2D_LOD (_NoiseTex, sampler_NoiseTex, noiseUV2 + noiseA * 0.03, 0).r;

    // noiseB = smoothstep(0.7, 0.9, noiseB) * noiseA;
    // noiseB = smoothstep(0.1, 0.4, noiseB);
    //
    // return saturate(noiseB);
    // 混合两层噪声，保留柔和变化，避免相乘后数值过小
    float noise = lerp(noiseA, noiseB, 0.5);

    return saturate(noise);
}

// 依赖：已包含 URP Core.hlsl，且 UnityPerMaterial 里已声明波形参数
// 每层 wave：xy 方向，z 振幅（世界单位），w 波长。速度由 _WaveSpeed.xyz 单独给出
// Forward 与 WaterSSR 共用同一套位移，保证 SSR RT 和水面像素对齐。

float2 GradientNoiseDir(float2 x)  //返回一个二维随机方向向量
{
    const float2 k = float2(0.3183099, 0.3678794);
    x = x * k + k.yx;
    return -1.0 + 2.0 * frac(16.0 * k * frac(x.x * x.y * (x.x + x.y)));
}

float GradientNoise(float2 uv, float scale)
{
    float2 p = uv * scale;              //将 UV 缩放到 Noise 的采样空间 
    float2 i = floor(p);                //取整数,当前所在网格的整数坐标
    float2 f = frac(p);                 //取小数,当前点在网格内部的局部坐标
    float2 u = f * f * (3.0 - 2.0 * f); //Smoothstep-like cubic interpolation curve，比普通线性插值更加平滑，减少明显的接缝
    
    float A = dot(GradientNoiseDir(i + float2(0, 0)), f - float2(0, 0));//左下
    float B = dot(GradientNoiseDir(i + float2(1, 0)), f - float2(1, 0));//右下
    float C = dot(GradientNoiseDir(i + float2(0, 1)), f - float2(0, 1));//左上
    float D = dot(GradientNoiseDir(i + float2(1, 1)), f - float2(1, 1));//右上
    float bottom = lerp(A, B, u.x);
    float top = lerp(C, D, u.x);
    return lerp(bottom, top, u.y);
    
    // return lerp(
    //     lerp(dot(GradientNoiseDir(i + float2(0, 0)), f - float2(0, 0)),
    //     dot(GradientNoiseDir(i + float2(1, 0)), f - float2(1, 0)), u.x),
    //     lerp(dot(GradientNoiseDir(i + float2(0, 1)), f - float2(0, 1)),
    //     dot(GradientNoiseDir(i + float2(1, 1)), f - float2(1, 1)), u.x),
    //     u.y);
}

float WaveRegionMask(float2 worldXZ)
{
    float n = GradientNoise(worldXZ / max(_WaveMaskScale.x, 1e-5), 1.0);
    n = n * 0.5 + 0.5;
    float edge0 = _WaveMaskRange.x;
    float edge1 = _WaveMaskRange.y;
    if (edge0 == edge1)
        edge1 = edge0 + 1e-4;
    return smoothstep(edge0, edge1, n);
}

float3 GerstnerWave(float3 position, inout float3 tangent, inout float3 binormal, float4 wave, float speed)
{
    float amplitude = wave.z;
    float wavelength = max(wave.w, 0.0001);
    float k = TWO_PI / wavelength;
    float2 dir = wave.xy;
    float dirLen = length(dir);
    float2 d = dirLen > 1e-5 ? dir / dirLen : float2(1, 0); //条件 ? 结果A : 结果B 
    // 如果方向向量的长度大于 0.00001，就用方向向量除以它的长度；否则，使用 (1, 0) 作为默认方向。最后把选出的结果存入 d
    
    // 相位扰动的世界空间尺度
    float phaseNoiseScale = 30.0;                         // 数值越大，波脊弯曲越舒展
    float phaseNoiseStrength = 1.2;                      // 扰动强度，单位为弧度
    float2 noiseUV = position.xz / phaseNoiseScale + d * 7.0 + float2(0.02, 0.013) * _TimeParameters.x;
    float phaseNoise = GradientNoise(noiseUV, 1.0); // 各层方向产生不同偏移，避免三层完全同步扰动
    
    // 给原本规则的相位，叠加平滑噪声
    float f = k * (dot(d, position.xz) - speed * _TimeParameters.x) + phaseNoiseStrength * phaseNoise;

    float sinF = sin(f);
    float cosF = cos(f);
    
    // 计算噪声对世界 X/Z 方向的变化率，让法线随相位扰动一起变化
    float epsilon = 0.1;
    float uvStep = epsilon / phaseNoiseScale;

    float noiseDx =
        (GradientNoise(noiseUV + float2(uvStep, 0), 1.0)
        - GradientNoise(noiseUV - float2(uvStep, 0), 1.0))
        / (2.0 * epsilon);

    float noiseDz =
        (GradientNoise(noiseUV + float2(0, uvStep), 1.0)
        - GradientNoise(noiseUV - float2(0, uvStep), 1.0))
        / (2.0 * epsilon);

    float phaseDx = k * d.x + phaseNoiseStrength * noiseDx;
    float phaseDz = k * d.y + phaseNoiseStrength * noiseDz;

    // 扰动后位移的偏导数。
    tangent += float3(
        -d.x * amplitude * sinF * phaseDx,
         amplitude * cosF * phaseDx,
        -d.y * amplitude * sinF * phaseDx);

    binormal += float3(
        -d.x * amplitude * sinF * phaseDz,
         amplitude * cosF * phaseDz,
        -d.y * amplitude * sinF * phaseDz);

    return float3(d.x * (amplitude * cosF), amplitude * sinF, d.y * (amplitude * cosF));
}

void ApplyGerstnerWaves(float3 positionWS, out float3 displacedWS, out float3 tangentWS, out float3 binormalWS, out float NoiseMask)
{
    tangentWS = float3(1, 0, 0);
    binormalWS = float3(0, 0, 1);
    float3 offset = GerstnerWave(positionWS, tangentWS, binormalWS, _WaveA, _WaveSpeed.x);
    offset += GerstnerWave(positionWS, tangentWS, binormalWS, _WaveB, _WaveSpeed.y);
    offset += GerstnerWave(positionWS, tangentWS, binormalWS, _WaveC, _WaveSpeed.z);
    
    // 位移前的位置采样；同一个值之后交给片元染色
    // 噪声为 0 → 不起浪、不加 Noise 染色
    // 噪声为 1 → 完整波浪、完整 Noise 染色
    // 中间值   → 位移和染色一起减弱
    NoiseMask = SampleWaterNoiseMask(positionWS.xz);
    float mask = lerp(0.6, 1.0, NoiseMask);  // 噪声最暗处仍保留 60% 波浪，最亮处使用完整波浪
    offset *= mask;
    tangentWS = float3(1, 0, 0) + (tangentWS - float3(1, 0, 0)) * mask;
    binormalWS = float3(0, 0, 1) + (binormalWS - float3(0, 0, 1)) * mask;
    displacedWS = positionWS + offset;
}

// Shared by Forward and WaterSSR: one displacement definition for both passes.
#include "../ShallowWater/ShallowWaterSampling.hlsl"
TEXTURE2D(_NavalFFTDisplacement);
TEXTURE2D(_NavalFFTNormal);

float _NavalFFTEnabled, _NavalPatchSize;
float4 _NavalWaveCenter;
float4 _NavalIslands[3]; // centre XZ, half extents XZ
float NavalWaveWeight(float2 xz)
{
    float weight=1.0-smoothstep(600.0,1800.0,distance(xz,_NavalWaveCenter.xz));
    for(int i=0;i<3;i++)
    {
        float2 q=(xz-_NavalIslands[i].xy)/max(_NavalIslands[i].zw,float2(1,1));
        weight*=smoothstep(.94,1.35,length(q));
    }
    return weight;
}
float3 NavalSurfaceNormal(float3 fallback,float3 world)
{
    float3 n=fallback;
    if(_NavalFFTEnabled>.5)
        n=normalize(lerp(float3(0,1,0),SAMPLE_TEXTURE2D_LOD(_NavalFFTNormal,sampler_LinearRepeat,world.xz/_NavalPatchSize,0).xyz,NavalWaveWeight(world.xz)));
    return ApplyShallowWaterNormal(n,world,0);
}
float NavalCrestFoam(float3 world)
{
    if(_NavalFFTEnabled<.5)return 0;
    return SAMPLE_TEXTURE2D_LOD(_NavalFFTNormal,sampler_LinearRepeat,world.xz/_NavalPatchSize,0).w*NavalWaveWeight(world.xz);
}
void ComputeWaterVertex(float3 positionOS, float4 tangentOS,
    out float4 positionHCS, out float4 positionSS, out float3 positionWS, out half3 normalWS, out half4 tangentWS, out float waveHeight, out float NoiseMask)
{
    float3 baseWS=TransformObjectToWorld(positionOS);
    float3 displacedWS,tangent,binormal;
    if(_NavalFFTEnabled>.5)
    {
        float2 uv=baseWS.xz/_NavalPatchSize;
        float weight=NavalWaveWeight(baseWS.xz);
        displacedWS=baseWS+SAMPLE_TEXTURE2D_LOD(_NavalFFTDisplacement,sampler_LinearRepeat,uv,0).xyz*weight;
        float3 n=normalize(lerp(float3(0,1,0),SAMPLE_TEXTURE2D_LOD(_NavalFFTNormal,sampler_LinearRepeat,uv,0).xyz,weight));
        tangent=normalize(float3(n.y,-n.x,0));binormal=cross(tangent,n);
        NoiseMask=SampleWaterNoiseMask(baseWS.xz);
    }
    else ApplyGerstnerWaves(baseWS,displacedWS,tangent,binormal,NoiseMask);
    displacedWS.y+=ShallowWaterHeight(displacedWS.xz)*ShallowWaterSurfaceMask(baseWS.y);
    positionWS=displacedWS;normalWS=normalize(cross(binormal,tangent));tangentWS=half4(normalize(tangent),-1);
    waveHeight=displacedWS.y-baseWS.y;positionHCS=TransformWorldToHClip(displacedWS);positionSS=ComputeScreenPos(positionHCS);
}
#endif
