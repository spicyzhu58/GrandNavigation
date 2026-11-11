Shader "GrandNavigation/IntegratedStylizedWater"
{
    Properties
    {
        [Header(Depth Color)]
        _DeepRange ("DeepRange", Vector) = (0, 20, 0, 0)
        _DeepColor ("DeepColor", Color) = (1, 1, 1, 1)
        _ShallowColor ("ShallowColor", Color) = (1, 1, 1, 1)
        _FresnelColor ("FresnelColor", Color) = (1, 1, 1, 1)
        _FresnelPower ("FresnelPower", Float) = 1
        
        [Header(Surface Normal)]
        _NormalScale ("NormalScale", Float) = 1
        _NormalSpeed ("NormalSpeed", Vector) = (0, 0, 0, 0)
        [Normal] _NormalMap ("NormalMap", 2D) = "bump" { }
        
        [Header(Reflection)]
        _ReflectMap ("ReflectMap", Cube) = "white" { }
        _ReflectIntensity ("ReflectIntensity", Float) = 0
        _ReflectPower ("ReflectPower", Float) = 0
        _ReflectDistort ("ReflectDistort", Range(0, 1)) = 0
        _SSRIntensity ("SSR Intensity", Range(0, 1)) = 1

        [Header(Underwater)]
        _UnderWaterDistort ("UnderWaterDistort", Float) = 0

        [Header(Caustics)]
        _CausticsScale ("CausticsScale", Float) = 8
        _CausticsTex ("CausticsTex", 2D) = "white" { }
        _CausticsSpeed ("CausticsSpeed", Vector) = (-8, 0, 0, 0)
        _CausticsIntensity ("Caustics Intensity", Float) = 3
        _CausticsRange ("CausticsRange", Float) = 0.5

        [Header(Shore)]
        _ShoreRange ("ShoreRange", Float) = 1
        _ShoreColor ("ShoreColor", Color) = (0, 0, 0, 0)
        _ShoreOpacity ("Shore Opacity", Range(0, 1)) = 1
        _ShoreEdgeWidth ("ShoreEdgeWidth", Range(0, 1)) = 0
        _ShoreEdgeIntensity ("ShoreEdgeIntensity", Float) = 0
        
        [Header(Foam)]
        _FoamColor ("FoamColor", Color) = (1, 1, 1, 1)
        _FoamRange ("FoamRange", Float) = 1
        _FoamSpeed ("FoamSpeed", Float) = -1
        _FoamFrequency ("FoamFrequency", Float) = 10
        _FoamBlend ("FoamBlend", Range(0, 1)) = 0
        _FoamDissolve ("FoamDissolve", Float) = 0
        _FoamNoiseSize ("FoamNoiseSize", Vector) = (1, 1, 0, 0)
        _FoamWidth ("FoamWidth", Float) = 0
        
        [Header(Noise)]
        _NoiseTex ("NoiseTex", 2D) = "white" { }
        _NoiseScale ("NoiseScale", Vector) = (1, 1, 1, 1)
        _NoiseSpeed ("NoiseSpeed", Vector) = (0, 0, 0, 0)
        _NoiseIntensity ("NoiseIntensity", Float) = 1
        [HDR]_NoiseColor ("NoiseColor", Color) = (1, 1, 1, 1)
        
        [Header(Specular)]
        _Glossy ("Glossy", Range(1, 512)) = 1
        _SpecularMaskMin ("SpecularMaskMin", Float) = 0
        _SpecularMaskMax ("SpecularMaskMax", Float) = 1
        [HDR]_SpecularColor ("SpecularColor", Color) = (1, 1, 1, 1)

        [Header(Wave)]
        [HDR]_WaveColor ("WaveColor", Color) = (1, 1, 1, 0)
        _WaveA ("Wave A (xy Dir, z Amp, w Length)", Vector) = (1, 1, 0.45, 28)
        _WaveB ("Wave B (xy Dir, z Amp, w Length)", Vector) = (-0.4, 1, 0.25, 16)
        _WaveC ("Wave C (xy Dir, z Amp, w Length)", Vector) = (1, -0.3, 0.12, 8)
        _WaveSpeed ("Wave Speed (A, B, C)", Vector) = (6.6, 5, 3.5, 0)
        _WaveMaskScale ("Wave Mask Scale", Vector) = (100, 100, 100, 100)
        _WaveMaskRange ("Wave Mask Range", Vector) = (0.55, 0.82, 0, 0)
        _WaveHeightRange ("Wave Height Range", Vector) = (0, 1, 0, 0)
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _TestParameter, _WaveA, _WaveB, _WaveC, _WaveSpeed, _WaveMaskScale;
            half4 _DeepColor, _ShallowColor, _FresnelColor, _ShoreColor, _FoamColor, _WaveColor, _SpecularColor, _NoiseColor;
            float2 _DeepRange, _NormalSpeed, _CausticsSpeed, _FoamNoiseSize, _WaveMaskRange, _WaveHeightRange, _NoiseScale, _NoiseSpeed;
            float _FresnelPower, _NormalScale, _ReflectIntensity, _ReflectPower, _ReflectDistort, _UnderWaterDistort, _CausticsScale, _CausticsIntensity, _CausticsRange, _ShoreOpacity, _ShoreRange, _ShoreEdgeWidth, _ShoreEdgeIntensity, _FoamRange, _FoamSpeed, _FoamFrequency, _FoamBlend, _FoamDissolve, _FoamWidth, _Glossy, _SpecularMaskMin, _SpecularMaskMax, _NoiseIntensity, _SSRIntensity;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 positionSS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
                half4 tangentWS : TEXCOORD4;
                float waveHeight : TEXCOORD5;
                float NoiseMask : TEXCOORD6;
                half4 color : COLOR;
            };

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURECUBE(_ReflectMap);
            SAMPLER(sampler_ReflectMap);
            TEXTURE2D(_CausticsTex);
            SAMPLER(sampler_CausticsTex);

            TEXTURE2D(_WaterSSRTexture);
            float _WaterSSREnabled;

            #include "SSR/WaterWaves.hlsl"
            
            //定义双层滚动法线混合后的法线方向
            float3 BlendNormalsWhiteout(float3 n1, float3 n2)
            {
                return normalize(float3(n1.xy + n2.xy, n1.z * n2.z));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                ComputeWaterVertex(IN.positionOS.xyz, IN.tangentOS, OUT.positionHCS, OUT.positionSS, OUT.positionWS, OUT.normalWS, OUT.tangentWS, OUT.waveHeight, OUT.NoiseMask);
                OUT.uv = TransformObjectToWorld(IN.positionOS.xyz).xz / 1000.0;
                OUT.color = IN.color;
                return OUT;
            }

            float3 BlendOverlay(float3 baseColor, float3 layerColor)
            {
                float3 dark = 2.0 * baseColor * layerColor;
                float3 light = 1.0 - 2.0 * (1.0 - baseColor) * (1.0 - layerColor);
                return lerp(dark, light, step(0.5, baseColor));
            }
            
            half4 frag( Varyings IN, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                bool isFrontFace = IS_FRONT_VFACE(face, true, false);

                float2 screenUV = IN.positionSS.xy / IN.positionSS.w;
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float3 geomNormalWS = NavalSurfaceNormal(normalize(IN.normalWS), IN.positionWS);
                float3 tangentWS = normalize(IN.tangentWS.xyz);
                float3 bitangentWS = cross(geomNormalWS, tangentWS) * IN.tangentWS.w * GetOddNegativeScale();
                float3x3 tangentToWorld = float3x3(tangentWS, bitangentWS, geomNormalWS);

                // 1. 水深：用场景深度重建世界坐标，再与水面高度做差
                float rawDepth = SampleSceneDepth(screenUV);
                //把读取到的rawDepth调整到之后 ComputeWorldSpacePosition() 所需要的形式
                #if !UNITY_REVERSED_Z
                    rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
                #endif
                float3 scenePosWS = ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);
                float waterDepth = max(0.0, IN.positionWS.y - scenePosWS.y);

                // 2. 深浅水底色
                float DepthMask = saturate (smoothstep(_DeepRange.x, _DeepRange.y, waterDepth));
                half4 WaterColor = lerp( _ShallowColor, _DeepColor, DepthMask);
                float BaseAlpha = saturate(WaterColor.a);                                 // 深浅水决定基础不透明度

                // 3. 岸边颜色+透明度
                float WaterShore = saturate (exp(-waterDepth / max(_ShoreRange, 1e-5)));  // 岸边遮罩：靠岸接近 1，远离岸边接近 0
                float FinalAlpha = lerp (BaseAlpha, saturate(_ShoreOpacity), WaterShore); // 岸边逐渐采用 ShoreColor 的 A
                float WaterOpacity = 1.0 - FinalAlpha;                                    // 水下颜色混合也使用调整后的不透明度
                float ShoreStrength = WaterShore * saturate(_ShoreColor.a);               // 岸边染色：Shore Color 的 A 只决定 Overlay 的占比
                float3 ShoreOverlay = BlendOverlay (WaterColor.rgb, _ShoreColor.rgb);

                WaterColor.rgb = lerp( WaterColor.rgb, ShoreOverlay, ShoreStrength);

                // 4. 菲涅尔图层：A 只控制反射的染色强度
                float WaterFresnel = pow( max(1.0 - saturate(dot(geomNormalWS, viewDirWS)), 0.0001), _FresnelPower);
                float FresnelStrength = saturate(WaterFresnel) * saturate(_FresnelColor.a);
                WaterColor.rgb = lerp( WaterColor.rgb, _FresnelColor.rgb, FresnelStrength);
                
                // 5. 表面法线扰动（双层滚动法线混合）
                float2 worldXZ = IN.positionWS.xz / max(_NormalScale, 1e-5);
                float2 normalFlow = _NormalSpeed * _TimeParameters.x * 0.01;
                float3 n0 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, worldXZ + normalFlow), 1.0);
                float3 n1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, worldXZ * 2.0 + normalFlow * (-0.5)), 1.0);
                float3 surfaceNormalTS = BlendNormalsWhiteout(n0, n1);

                // 6. 反射：扰动后的反射方向采样自定义 Cubemap
                float3 reflectNormalTS = lerp(float3(0, 0, 1), surfaceNormalTS, _ReflectDistort);
                float3 reflectNormalWS = normalize(TransformTangentToWorld(reflectNormalTS, tangentToWorld));
                float3 reflectVector = reflect(-viewDirWS, reflectNormalWS);
                
                float facing = max(1.0 - saturate(dot(geomNormalWS, viewDirWS)), 0.0001);
                float reflectFresnel = saturate(_ReflectIntensity * pow(facing, _ReflectPower));
                half3 cubemapReflect = SAMPLE_TEXTURECUBE(_ReflectMap, sampler_ReflectMap, reflectVector).rgb * reflectFresnel;
                // _WaterSSRTexture 由 Water SSR Renderer Feature 写入。a 为命中置信度，未命中时继续用 Cubemap
                half4 ssr = SAMPLE_TEXTURE2D(_WaterSSRTexture, sampler_LinearClamp, screenUV);
                float ssrFresnel = pow(facing, max(_ReflectPower, 1e-4));
                float ssrWeight = saturate(ssr.a * _SSRIntensity * _WaterSSREnabled);
                half3 reflectColor = lerp(cubemapReflect, ssr.rgb * ssrFresnel, ssrWeight);

                
                // 7. 水下扰动：用切线法线偏移不透明场景颜色
                float2 distortUV = screenUV + surfaceNormalTS.xy * _UnderWaterDistort * 0.01;
                half3 sceneColor = SampleSceneColor(distortUV);
                
                // 8. 焦散：场景世界 XZ 双层采样取 min，再按场深衰减
                float2 causticsUV = scenePosWS.xz / max(_CausticsScale, 1e-5);
                float2 causticsFlow = _CausticsSpeed * _TimeParameters.x * 0.01;
                half4 causticsA = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, causticsUV + causticsFlow);
                half4 causticsB = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, -causticsUV + causticsFlow);
                float causticsMask = saturate(exp(-waterDepth / max(_CausticsRange, 1e-5)));
                half4 causticsColor = min(causticsA, causticsB) * _CausticsIntensity * causticsMask;
                half4 underWaterColor = half4(sceneColor, 1.0) + causticsColor;

                half4 SurfaceColor = WaterColor + half4(reflectColor, 0.0);
                
                // 9. 海浪与波峰颜色：波高经 smoothstep 后作为插值权重。_WaveColor.a 控制替换强度
                float WaveHeight = smoothstep(_WaveHeightRange.x, _WaveHeightRange.y, IN.waveHeight);
                float WaveStrength = saturate(WaveHeight) * saturate(_WaveColor.a);
                SurfaceColor.rgb = lerp (SurfaceColor.rgb, SurfaceColor.rgb * (1.0 + _WaveColor.rgb), WaveStrength);
                
                half4 color = lerp(SurfaceColor, underWaterColor, WaterOpacity);


                // 10. 岸边泡沫、岸线高光
                float FoamDepth = saturate(waterDepth / max(_FoamRange, 1e-5)); // 根据水深计算泡沫范围
                float FoamInv = 1.0 - FoamDepth;
                float FoamMask = 1.0 - smoothstep(_FoamBlend, 1.0, FoamDepth + 0.1);
                float FoamWave = sin(FoamInv * _FoamFrequency + _FoamSpeed * _TimeParameters.x);
                float FoamNoise = GradientNoise(IN.uv * _FoamNoiseSize, 1.0);
                float FoamPattern = step(FoamInv - _FoamWidth, FoamInv + FoamWave + FoamNoise - _FoamDissolve);
                float FoamStrength = saturate(FoamMask * FoamPattern) * saturate(_FoamColor.a);
                color.rgb += _FoamColor.rgb * FoamStrength;
                color.rgb=lerp(color.rgb,_FoamColor.rgb,saturate(NavalCrestFoam(IN.positionWS))*.65);

                float ShoreEdge = smoothstep(1.0 - _ShoreEdgeWidth, 1.0, WaterShore) * _ShoreEdgeIntensity;
                color.rgb = max(color.rgb + ShoreEdge, 0.0);

                //Lighting
                float3 LightColor = GetMainLight().color;
                color.rgb = color.rgb * LightColor;

                float3 light_dir = GetMainLight().direction;
                float3 view_dir = normalize(_WorldSpaceCameraPos - IN.positionWS);
                
                float3 half_dir = normalize(light_dir + view_dir);
                float NdotH = saturate(dot(reflectNormalWS, half_dir));
                float specular = pow(NdotH, max(_Glossy, 1));
                specular = smoothstep(_SpecularMaskMin, _SpecularMaskMax, specular);
                half4 specularColor = specular * half4(LightColor, 1) * _SpecularColor;
                specularColor.rgb *= saturate(_SpecularColor.a);

                //Noise
                // float2 noiseUV = IN.positionWS.xz/1000 / max(_NoiseScale.x, 1e-5);
                // float2 noiseUV2 = IN.positionWS.xz/1000 / max(_NoiseScale.y, 1e-5);
                //
                // float2 noiseFlow = _NoiseSpeed * _TimeParameters.x * 0.01;
                // half4 noiseA = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV + noiseFlow);
                // half4 noiseB = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV2+noiseA*0.03);
                // noiseB = smoothstep(0.7,0.9,noiseB)*noiseA;
                // noiseB = smoothstep(0.1,0.4,noiseB);
                // half4 noiseColor = noiseB * _NoiseIntensity * _NoiseColor;
                // noiseColor.rgb *= saturate(_NoiseColor.a);
                
                // 直接使用控制波浪位移的同一份噪声遮罩
                float NoiseMask = saturate(IN.NoiseMask); // 与波浪位移共用的区域遮罩
                // 只让较高的波峰参与染色
                float CrestMask = smoothstep (_WaveHeightRange.x, _WaveHeightRange.y, IN.waveHeight);
                // 起浪区域与波峰高度同时满足，才出现浪尖颜色
                float CrestNoiseMask = NoiseMask * CrestMask;
                half3 NoiseColor = CrestNoiseMask * _NoiseIntensity * _NoiseColor.rgb * saturate(_NoiseColor.a);

                return float4 (color.rgb + specularColor.rgb + NoiseColor, FinalAlpha);
            }
            ENDHLSL
        }

        Pass
        {
            Name "WaterSSR"
            Tags { "LightMode" = "WaterSSR" }

            ZWrite Off
            ZTest Always
            Cull off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "SSR/WaterWaves.hlsl"
            #include "SSR/WaterSSR.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 tangentOS : TANGENT;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 positionSS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
            };

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            
            float3 BlendNormalsWhiteout(float3 n1, float3 n2)
            {
                return normalize(float3(n1.xy + n2.xy, n1.z * n2.z));
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float unusedWaveHeight;
                float unusedNoiseMask;
                ComputeWaterVertex(IN.positionOS.xyz, IN.tangentOS, OUT.positionHCS, OUT.positionSS, OUT.positionWS, OUT.normalWS, OUT.tangentWS, unusedWaveHeight, unusedNoiseMask);
                return OUT;
            }
            
            half4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.positionSS.xy / IN.positionSS.w;
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float3 geomNormalWS = NavalSurfaceNormal(normalize(IN.normalWS), IN.positionWS);
                float3 tangentWS = normalize(IN.tangentWS.xyz);
                float3 bitangentWS = cross(geomNormalWS, tangentWS) * IN.tangentWS.w * GetOddNegativeScale();
                float3x3 tangentToWorld = float3x3(tangentWS, bitangentWS, geomNormalWS);

                float2 worldXZ = IN.positionWS.xz / max(_NormalScale, 1e-5);
                float2 normalFlow = _NormalSpeed * _TimeParameters.x * 0.01;
                float3 n0 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, worldXZ + normalFlow), 1.0);
                float3 n1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, worldXZ * 2.0 + normalFlow * (-0.5)), 1.0);
                float3 surfaceNormalTS = BlendNormalsWhiteout(n0, n1);
                float3 reflectNormalTS = lerp(float3(0, 0, 1), surfaceNormalTS, _ReflectDistort);
                float3 reflectNormalWS = normalize(TransformTangentToWorld(reflectNormalTS, tangentToWorld));
                float3 reflectVector = reflect(-viewDirWS, reflectNormalWS);

                return TraceWaterSSR(IN.positionWS, geomNormalWS, reflectVector, screenUV);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
