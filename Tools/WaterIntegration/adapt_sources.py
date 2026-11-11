from pathlib import Path
p=Path(__file__).resolve().parents[2]/'Assets/_Game/WaterIntegration/Imported'
f=p/'SSR/WaterWaves.hlsl';s=f.read_text(encoding='utf8')
if 'NavalWaveWeight' in s:
    raise SystemExit('Already adapted. Run import_sources.py first only if you intend to replace the imported source files.')
start=s.index('void ComputeWaterVertex(')
s=s[:start]+'''// Shared by Forward and WaterSSR: one displacement definition for both passes.
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
        n=normalize(lerp(float3(0,1,0),SAMPLE_TEXTURE2D_LOD(_NavalFFTNormal,sampler_NavalFFTDisplacement,world.xz/_NavalPatchSize,0).xyz,NavalWaveWeight(world.xz)));
    return ApplyShallowWaterNormal(n,world,0);
}
float NavalCrestFoam(float3 world)
{
    if(_NavalFFTEnabled<.5)return 0;
    return SAMPLE_TEXTURE2D_LOD(_NavalFFTNormal,sampler_NavalFFTDisplacement,world.xz/_NavalPatchSize,0).w*NavalWaveWeight(world.xz);
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
        displacedWS=baseWS+SAMPLE_TEXTURE2D_LOD(_NavalFFTDisplacement,sampler_NavalFFTDisplacement,uv,0).xyz*weight;
        float3 n=normalize(lerp(float3(0,1,0),SAMPLE_TEXTURE2D_LOD(_NavalFFTNormal,sampler_NavalFFTDisplacement,uv,0).xyz,weight));
        tangent=normalize(float3(n.y,-n.x,0));binormal=cross(tangent,n);
        NoiseMask=SampleWaterNoiseMask(baseWS.xz);
    }
    else ApplyGerstnerWaves(baseWS,displacedWS,tangent,binormal,NoiseMask);
    displacedWS.y+=ShallowWaterHeight(displacedWS.xz)*ShallowWaterSurfaceMask(baseWS.y);
    positionWS=displacedWS;normalWS=normalize(cross(binormal,tangent));tangentWS=half4(normalize(tangent),-1);
    waveHeight=displacedWS.y-baseWS.y;positionHCS=TransformWorldToHClip(displacedWS);positionSS=ComputeScreenPos(positionHCS);
}
#endif
'''
f.write_text(s.replace('sampler_NavalFFTDisplacement','sampler_LinearRepeat'),encoding='utf8')
f=p/'Water.shader';s=f.read_text(encoding='utf8').replace('Shader "Custom/Water"','Shader "GrandNavigation/IntegratedStylizedWater"')
s=s.replace('float3 geomNormalWS = normalize(IN.normalWS);','float3 geomNormalWS = NavalSurfaceNormal(normalize(IN.normalWS), IN.positionWS);')
s=s.replace('float waterDepth = IN.positionWS.y - scenePosWS.y;','float waterDepth = max(0.0, IN.positionWS.y - scenePosWS.y);')
s=s.replace('OUT.uv = IN.uv;','OUT.uv = TransformObjectToWorld(IN.positionOS.xyz).xz / 1000.0;')
s=s.replace('color.rgb += _FoamColor.rgb * FoamStrength;','color.rgb += _FoamColor.rgb * FoamStrength;\n                color.rgb=lerp(color.rgb,_FoamColor.rgb,saturate(NavalCrestFoam(IN.positionWS))*.65);')
f.write_text(s,encoding='utf8')
f=p/'ShallowWater/ShallowWater.compute';s=f.read_text(encoding='utf8')
s+='''
// Preserve world-space waves when the local interaction window moves by whole texels.
#pragma kernel ScrollState
int2 _ScrollOffset;
[numthreads(8,8,1)]
void ScrollState(uint3 id:SV_DispatchThreadID)
{
    if(Outside(id.xy))return;
    int2 old=int2(id.xy)+_ScrollOffset;
    _StateWrite[id.xy]=(any(old<0)||any(old>=_Resolution))?float4(0,0,0,0):_StateRead[old];
}
''';f.write_text(s,encoding='utf8')
f=p/'ShallowWater/ShallowWaterSimulation.cs';s=f.read_text(encoding='utf8')
s=s.replace('改变中心或平均水面高度会清空历史；这里不实现跟随相机的滚动网格。','中心按整数像素移动时滚动历史；尺寸、分辨率或平均水位变化时重建。')
s=s.replace('int heightQueryKernel;','int heightQueryKernel;\n        Vector2Int pendingScroll;\n        public int ScrollCount {get;private set;}')
anchor='            ReleaseResources();\n            allocatedDomain = domain;'
s=s.replace(anchor,'''            if(stateRead != null && allocatedSolver==solver && allocatedResolution==count
                && allocatedDomain.z==domain.z && allocatedDomain.w==domain.w && allocatedLevel==WaterLevel)
            {
                pendingScroll+=new Vector2Int(Mathf.RoundToInt((domain.x-allocatedDomain.x)*count/domain.z),Mathf.RoundToInt((domain.y-allocatedDomain.y)*count/domain.w));
                allocatedDomain=domain;return true;
            }
            ReleaseResources();pendingScroll=Vector2Int.zero;
            allocatedDomain = domain;''')
s=s.replace('            CaptureInteractors(cmd);','''            bool scrolled=pendingScroll!=Vector2Int.zero;
            if(scrolled)
            {
                int kernel=solver.FindKernel("ScrollState");
                cmd.SetComputeIntParams(solver,"_ScrollOffset",pendingScroll.x,pendingScroll.y);
                cmd.SetComputeTextureParam(solver,kernel,StateReadId,stateRead);cmd.SetComputeTextureParam(solver,kernel,StateWriteId,stateWrite);
                cmd.DispatchCompute(solver,kernel,(allocatedResolution+7)/8,(allocatedResolution+7)/8,1);
                cmd.CopyTexture(stateWrite,stateRead);pendingScroll=Vector2Int.zero;ScrollCount++;
            }
            CaptureInteractors(cmd);''')
s=s.replace('injectKernel, ContactPreviousId, contactPrevious','injectKernel, ContactPreviousId, scrolled?contactNow:contactPrevious')
f.write_text(s,encoding='utf8')
f=p/'ShallowWater/ShallowWaterBuoyancy.cs';s=f.read_text(encoding='utf8')
f.write_text(s.replace('FindObjectsByType<Renderer>()','FindObjectsByType<Renderer>(FindObjectsSortMode.None)'),encoding='utf8')
print('Adapted shared displacement, normals, FFT foam and scrolling interaction domain')
