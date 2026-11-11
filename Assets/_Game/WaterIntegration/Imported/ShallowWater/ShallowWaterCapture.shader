Shader "Hidden/Water/ShallowWaterCapture"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "TopDownContact"
            Cull Off
            ZWrite Off
            ZTest Always
            // 同一物体的正反面、多个子网格不能重复累加强度。
            Blend One One
            BlendOp Max
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 _SWCaptureDomain; // minX, minZ, sizeX, sizeZ
            float _SWContactValue;   // 本次 DrawRenderer 的等效排水高度（米）

            float4 Vert(float3 positionOS : POSITION) : SV_POSITION
            {
                float3 world = TransformObjectToWorld(positionOS);
                float2 uv = (world.xz - _SWCaptureDomain.xy) / _SWCaptureDomain.zw;
                float2 clipXY = uv * 2.0 - 1.0;
                // 明确规定 RT 像素 (0,0) 对应世界 (minX,minZ)。
                // D3D/Metal 等 RT 原点在顶部，需要翻转裁剪空间 Y；不使用主相机矩阵。
                #if UNITY_UV_STARTS_AT_TOP
                    clipXY.y = -clipXY.y;
                #endif
                return float4(clipXY, 0.5, 1.0);
            }

            float4 Frag() : SV_Target
            {
                return float4(_SWContactValue, 0, 0, 0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ContactBlurHorizontal"
            Cull Off
            ZWrite Off
            ZTest Always
            Blend Off
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SWBlurVert
            #pragma fragment SWBlurFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ShallowWaterContactBlur.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ContactBlurVerticalAndMerge"
            Cull Off
            ZWrite Off
            ZTest Always
            // 每个物体先独立平滑，再取最大值合并，保留其各自的宽度。
            Blend One One
            BlendOp Max
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex SWBlurVert
            #pragma fragment SWBlurFrag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ShallowWaterContactBlur.hlsl"
            ENDHLSL
        }
    }
}
