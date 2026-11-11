#ifndef WATER_SSR_INCLUDED
#define WATER_SSR_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

// Renderer Feature 每帧写入。x: 最大世界距离  y: 目标像素步长  z: 命中厚度  w: 最大步数
float4 _WaterSSRParams;
// x: 二分查找次数  y: 屏幕边缘淡出宽度  z: 沿法线外推的起点偏移  w: 未使用
float4 _WaterSSRParams2;

// 从水面沿反射方向做屏幕空间步进
// 命中：rgb = 不透明场景颜色，a = 置信度。未命中：a = 0，水面回退到 Cubemap
half4 TraceWaterSSR(float3 positionWS, float3 normalWS, float3 reflectDirWS, float2 screenUV)
{
    // 1. 判断当前水面可见性：当前水面像素对应的不透明深度（深度图采样）。水本身不写深度，这里读到的是水底或挡在前面的物体
    float rawScene = SampleSceneDepth(screenUV);
    // 把深度缓冲值换成相机前方的线性距离，后面才能和射线深度相减
    float sceneEye = LinearEyeDepth(rawScene, _ZBufferParams);
    float waterEye = LinearEyeDepth(positionWS, UNITY_MATRIX_V);
    // 水面比不透明物体更远，说明它被墙挡住了。不写反射，避免墙后的水面污染 RT
    if (waterEye > sceneEye + 0.05)
        return half4(0, 0, 0, 0);

    // 2. 判断世界空间射线方向和走的距离
    reflectDirWS = normalize(reflectDirWS);
    float maxDistance = max(_WaterSSRParams.x, 0.01);// 射线最远能走多长。给一个下限，避免后面除以 0 或步进长度为 0
    // 起点沿法线抬起一点，减少第一步就打到自己脚下的水底
    float3 originWS = positionWS + normalWS * _WaterSSRParams2.z;
    float3 endWS = originWS + reflectDirWS * maxDistance;

    // 3. 裁剪终点，防止终点跑到相机后方
    // Unity 相机看向 -Z，在相机前方的 viewZ 是负数
    // 把世界空间射线的起终点转换为相机空间的起终点
    // ①把世界空间坐标转化为齐次坐标 float4 worldPosition = float4(originWS, 1.0);
    // ②矩阵乘法 把世界空间转化为相机空间 float4 viewPosition = mul(UNITY_MATRIX_V, worldPosition);
    // ③ 取相机空间的 z，通过正负判断它在相机前后的位置 float originViewZ = viewPosition.z;
    float originViewZ = mul(UNITY_MATRIX_V, float4(originWS, 1.0)).z;
    float endViewZ = mul(UNITY_MATRIX_V, float4(endWS, 1.0)).z;
    // 终点跑到相机背后或贴到近裁剪面时，投影 UV 会翻掉，先把终点截回来
    //if (endViewZ > -0.1)
    float nearViewZ = -max(_ProjectionParams.y, 0.001);
    if (endViewZ > nearViewZ)
    {
        float denom = endViewZ - originViewZ;
        // 求射线和“viewZ = -0.1”这个平面的交点比例。分母太小说明射线几乎平行于该平面。等同于：
        //float tClip;
        // if (abs(denom) < 0.00001)
        // {
        //     tClip = 0.0;
        // }
        // else
        // {
        //     tClip = (nearViewZ - originViewZ) / denom;
        // }
        float tClip = abs(denom) < 1e-5 ? 0.0 : (nearViewZ - originViewZ) / denom;
        endWS = lerp(originWS, endWS, saturate(tClip) * 0.999);// 0.999 让终点停在平面前面一点点，避免刚好落在裁剪面上
    }

    // 4. 投影与步数预算
    // 把世界空间起点、终点投到 [0,1] 的屏幕 UV。后面只在这根屏幕线段上采样；投影成屏幕线段，决定步数
    
    //float2 startUV = ComputeNormalizedDeviceCoordinates(originWS, UNITY_MATRIX_VP);
    //float2 endUV = ComputeNormalizedDeviceCoordinates(endWS, UNITY_MATRIX_VP);
    
    //反射射线与原水面采用相同的屏幕 UV 转换方式
    float4 startSS = ComputeScreenPos(TransformWorldToHClip(originWS));
    float2 startUV = startSS.xy / startSS.w;

    float4 endSS = ComputeScreenPos(TransformWorldToHClip(endWS));
    float2 endUV = endSS.xy / endSS.w;
    
    // UV 差乘上分辨率，得到这根射线在屏幕上大约跨了多少像素
    float span = length((endUV - startUV) * _ScaledScreenParams.xy);
    // 不到 1 个像素就没有可走的步进，直接当未命中
    if (span < 1.0)
        return half4(0, 0, 0, 0);

    float stride = max(_WaterSSRParams.y, 1.0);
    // 步数 = 像素跨度span / 每步像素stride，并夹在 1 和最大步数之间。步数被夹住时，每步会变长，但仍能走到终点
    int stepCount = (int)clamp(ceil(span / stride), 1.0, max(_WaterSSRParams.w, 1.0)); //计算粗查找步数：沿屏幕射线走，寻找可能穿过表面的区间
    int binarySteps = (int)clamp(_WaterSSRParams2.x, 0.0, 8.0);                        //限制二分查找次数：找到候选区间后，反复取中点，把范围缩小
    float thickness = max(_WaterSSRParams.z, 1e-3);                                    //设置基础命中容差

    //5. 深度倒数与初始状态
    // 透视下，屏幕空间里线性变化的是 1/深度，不是深度本身，而是深度倒数
    //原因是透视除法改变了世界位置与屏幕位置之间的比例关系：float stepT = rcp((float)stepCount);
    float startEye = max(LinearEyeDepth(originWS, UNITY_MATRIX_V), 1e-3);
    float endEye = max(LinearEyeDepth(endWS, UNITY_MATRIX_V), 1e-3);
    float invStart = rcp(startEye);
    float invEnd = rcp(endEye);
    // t 从 0 到 1，均匀切开整段屏幕射线
    float stepT = rcp((float)stepCount); //屏幕线段参数每次增加 1 / stepCount

    //6. 粗步进，循环开始前，把“上一个位置”初始化为线段起点；prevT 用来记住“上一步走到了屏幕射线的哪个位置”，方便发现候选交点后，确定二分查找的范围
    float prevT = 0.0;
    // 上一步射线是否已经钻到场景表面后面。用来检测“刚从前面穿到后面”的那一步
    bool prevBehind = false;

    [loop]
    for (int i = 1; i <= stepCount; i++)
    {
        float t = i * stepT;
        // 屏幕 UV 沿线段线性插值。直线在屏幕上的投影本来就是直的
        float2 uv = lerp(startUV, endUV, t);
        // 走出屏幕就不可能再采到颜色，停止
        if (any(uv < 0.0) || any(uv > 1.0))
            break;

        float raw = SampleSceneDepth(uv);
        // 用 1/深度插值再取倒数，得到这一步射线在相机前方的真实距离
        float rayEye = rcp(lerp(invStart, invEnd, t));
        // 同一像素上，深度缓冲里物体离相机有多远
        float sampleEye = LinearEyeDepth(raw, _ZBufferParams);

        // Reversed-Z 时天空深度接近 0；普通 Z 时天空深度接近 1。天空不能当成被打中的表面
#if UNITY_REVERSED_Z
        bool isSky = raw <= 1e-5;
#else
        bool isSky = raw >= 1.0 - 1e-5;
#endif
        // 射线距离大于场景距离，说明这一步已经穿到物体背面
        // isSky 实际是根据深度推断“这里没有有效表面”，并不是识别天空的颜色
        bool behind = !isSky && (rayEye > sampleEye);

        // 上一步还在表面前面，这一步到了后面：中间一定有交点
        // 7. 二分缩小候选范围
        if (behind && !prevBehind)
        {
            float t0 = prevT;
            float t1 = t;
            // 在穿过表面的这一小段里二分，把交点从“一个大步”收准到更细的 UV。
            [loop]
            for (int b = 0; b < binarySteps; b++)
            {
                float tm = 0.5 * (t0 + t1);
                float2 uvm = lerp(startUV, endUV, tm);
                float rawM = SampleSceneDepth(uvm);
                float rayM = rcp(lerp(invStart, invEnd, tm));
                float sceneM = LinearEyeDepth(rawM, _ZBufferParams);
#if UNITY_REVERSED_Z
                bool skyM = rawM <= 1e-5;
#else
                bool skyM = rawM >= 1.0 - 1e-5;
#endif
                // 中点已经在表面后面，交点在左半段；否则在右半段
                if (!skyM && rayM > sceneM)
                    t1 = tm;
                else
                    t0 = tm;
            }

            // 8. 用厚度判断候选点是否可信
            // 二分结束后 t1 落在表面后面、且最靠近交点的一侧
            float2 hitUV = lerp(startUV, endUV, t1);
            if (all(hitUV > 0.0) && all(hitUV < 1.0))
            {
                float rawHit = SampleSceneDepth(hitUV);
                float rayHit = rcp(lerp(invStart, invEnd, t1));
                float sceneHit = LinearEyeDepth(rawHit, _ZBufferParams);
                float delta = rayHit - sceneHit; //计算射线比表面深了多少：把射线深度减去表面深度，看看射线已经超过这个表面多少
                float accept = thickness * max(1.0, rayHit * 0.05); //以 thickness 为基础容差。近处使用原容差，远处适当放大容差，允许更大的深度偏差，否则大步长很容易从薄物体上跳过去
#if UNITY_REVERSED_Z
                bool skyHit = rawHit <= 1e-5;
#else
                bool skyHit = rawHit >= 1.0 - 1e-5;
#endif
                // 穿进去的深度必须落在厚度里。太大说明只是从缝隙跳到了后面的另一层物体。
                if (!skyHit && delta > 0.0 && delta < accept) // 这里有有效表面，射线已经越过它，而且只越过了允许的一小段距离，因此接受这个候选命中
                {
                    float edgeFade = max(_WaterSSRParams2.y, 1e-4);
                    // 离屏幕四边越近，这个值越小。
                    float2 edge = min(hitUV, 1.0 - hitUV);
                    // 边缘命中置信度降低，水面会更多地混回 Cubemap，避免反射被屏幕边界切断。
                    float confidence = saturate(min(edge.x, edge.y) / edgeFade);
                    return half4(SampleSceneColor(hitUV), confidence);
                }
            }
        }

        prevBehind = behind;
        prevT = t;
    }

    // 整段射线都没找到合格交点。
    return half4(0, 0, 0, 0);
}

#endif
