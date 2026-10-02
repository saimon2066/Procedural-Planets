Shader "Custom/S_Ocean"
{
    Properties
    {
        _OceanCenter ("Ocean Center", Vector) = (0, 0, 0, 1)
        _OceanRadius ("Ocean Radius", Float) = 1
        
        _ColorA ("Color A", Color) = (0, 0, 0, 1)
        _ColorB ("Color B", Color) = (0, 0, 0, 1)
        
        _DepthMultiplier ("Ocean Depth", Float) = 1
        _AlphaMultiplier ("Alpha Multiplier", Float) = 1
        
        _Smoothness ("Smoothness", Float) = 0
    }
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Assets/Graphics/Shaders/Includes/Math.hlsl"
        ENDHLSL

        Tags { "RenderType"="Opaque" }
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "S_Ocean"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag
            
            float3 _OceanCenter;
            float _OceanRadius;
            
            float _DepthMultiplier;
            float _AlphaMultiplier;
            
            float _Smoothness;
            
            float4 _ColorA;
            float4 _ColorB;

            float4 Frag (Varyings input) : SV_Target
            {
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgba;
                
                float depth = SampleSceneDepth(input.texcoord);
                
                float3 viewVector = ComputeWorldSpacePosition(input.texcoord, depth, UNITY_MATRIX_I_VP) - GetCameraPositionWS();
                
                float sceneDepth = length(viewVector);

                float3 rayOrigin = GetCameraPositionWS();
                float3 rayDir = normalize(viewVector);
                
                float2 hitInfo = RaySphere(_OceanCenter, _OceanRadius, rayOrigin, rayDir);
                float distToOcean = hitInfo.x;
                float distThroughOcean = hitInfo.y;
                float oceanViewDepth = min(distThroughOcean, sceneDepth - distToOcean);
                
                if (oceanViewDepth > 0)
                {
                    Light mainLight = GetMainLight();
                    float3 mainLightDir = mainLight.direction;
                    float4 mainLightColor = float4(mainLight.color, 1);
                    
                    float opticalDepth01 = 1 - exp(-oceanViewDepth * _DepthMultiplier);
                    float alpha = 1 - exp(-oceanViewDepth * _AlphaMultiplier);
                    
                    float3 oceanNormal = normalize(rayOrigin + rayDir * distToOcean);
                    
                    float specularAngle = acos(dot(normalize(mainLightDir - rayDir), oceanNormal));
                    float specularExponent = specularAngle / (1 - _Smoothness);
                    float specularHighlight = distToOcean < 5 ? lerp(0, exp(-specularExponent * specularExponent), distToOcean / 10) : exp(-specularExponent * specularExponent);
                    float diffuseLighting = saturate(dot(oceanNormal, mainLightDir));
                    
                    float4 oceanUnlitColor = lerp(_ColorA, _ColorB, opticalDepth01);
                    float4 oceanLitColor = lerp(oceanUnlitColor * 0.05, oceanUnlitColor, diffuseLighting) + specularHighlight * mainLightColor;
                    return lerp(color, oceanLitColor, alpha);
                }
                
                return color;
            }
            
            ENDHLSL
        }
    }
}
