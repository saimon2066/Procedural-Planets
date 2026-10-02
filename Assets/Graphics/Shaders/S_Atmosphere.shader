Shader "Custom/S_Atmosphere"
{
    Properties
    {
        _Center ("Atmosphere Center", Vector) = (0, 0, 0, 0)
        _Radius ("Atmosphere Radius", Float) = 1050
        
        _BodyRadius ("Body Radius", Float) = 1000
        _OceanRadius ("Ocean Radius", Float) = 800
        
        _ScatteringCoefficients ("Scattering Coefficients", Vector) = (700, 530, 440)
        _ScatteringStrength ("Scattering Strength", Float) = 5
        
        _DensityFalloff ("Density Falloff", Float) = 5
        
        _ScatteringPoints ("Scattering Resolution Points", Integer) = 10
        _OpticalDepthPoints ("Optical Depth Resolution Points", Integer) = 10
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
            Name "S_Atmosphere"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag
            
            float3 _Center;
            float _Radius;
            
            float _BodyRadius;
            float _OceanRadius;
            
            float3 _ScatteringCoefficients;
            float _ScatteringStrength;
            
            float _DensityFalloff;
            
            int _ScatteringPoints;
            int _OpticalDepthPoints;
            
            float3 GetCoefficients()
            {
                float r = Pow4(400 / _ScatteringCoefficients.x);
                float g = Pow4(400 / _ScatteringCoefficients.y);
                float b = Pow4(400 / _ScatteringCoefficients.z);
                
                return float3(r, g, b) * _ScatteringStrength;
            }
            
            float DensityAtPoint(float3 samplePoint)
            {
                float heightAboveSurface = length(samplePoint - _Center) - _BodyRadius;
                float percent = heightAboveSurface / (_Radius - _BodyRadius);
                float densityAtPoint = exp(-percent * _DensityFalloff) * (1 - percent);
                
                return densityAtPoint;
            }
            
            float OpticalDepth(float3 rayOrigin, float3 rayDir, float rayLength)
            {
                float3 densitySamplePoint = rayOrigin;
                float stepSize = rayLength / (_OpticalDepthPoints - 1);
                float opticalDepth = 0;
                
                for (int i = 0; i < _OpticalDepthPoints; i++)
                {
                    float densityAtPoint = DensityAtPoint(densitySamplePoint);
                    opticalDepth += densityAtPoint * stepSize;
                    densitySamplePoint += rayDir * stepSize;
                }
                
                return opticalDepth;
            }
            
            float3 CalculateLight(float3 rayOrigin, float3 rayDir, float rayLength, float3 color)
            {
                float3 coeffs = GetCoefficients();
                
                float3 inScatterPoint = rayOrigin;
                float stepSize = rayLength / (_ScatteringPoints - 1);
                float3 inScatteredLight = 0;
                float viewRayOpticalDepth = 0;

                Light mainLight = GetMainLight();
                float3 mainLightDir = mainLight.direction;
                
                for (int i = 0; i < _ScatteringPoints; i++)
                {
                    float sunRayLength = RaySphere(_Center, _Radius, inScatterPoint, mainLightDir).y;
                    
                    float sunRayOpticalDepth = OpticalDepth(inScatterPoint, mainLightDir, sunRayLength);
                    viewRayOpticalDepth = OpticalDepth(inScatterPoint, -rayDir, stepSize * i);
                    
                    float3 lightAtPoint = exp(-(sunRayOpticalDepth + viewRayOpticalDepth) * coeffs);
                    float densityAtPoint = DensityAtPoint(inScatterPoint);
                    
                    inScatteredLight += densityAtPoint * lightAtPoint * coeffs * stepSize;
                    inScatterPoint += rayDir * stepSize;
                }
                
                return color * exp(-viewRayOpticalDepth * coeffs) + inScatteredLight;
            }

            float4 Frag (Varyings input) : SV_Target
            {
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord).rgba;
                
                float depth = SampleSceneDepth(input.texcoord);
                
                float3 viewVector = ComputeWorldSpacePosition(input.texcoord, depth, UNITY_MATRIX_I_VP) - GetCameraPositionWS();
                
                float sceneDepth = length(viewVector);
                
                float3 rayOrigin = GetCameraPositionWS();
                float3 rayDir = normalize(viewVector);
                
                float distToOcean = RaySphere(_Center, _OceanRadius, rayOrigin, rayDir).x;
                float distToSurface = min(sceneDepth, distToOcean);
                
                float2 hitInfo = RaySphere(_Center, _Radius, rayOrigin, rayDir);
                float distToAtmosphere = hitInfo.x;
                float distThroughAtmosphere = min(hitInfo.y, distToSurface - distToAtmosphere);
                
                if (distThroughAtmosphere > 0)
                {
                    float3 atmospherePoint = rayOrigin + rayDir * distToAtmosphere;
                    float3 lightAtAtmospherePoint = CalculateLight(atmospherePoint, rayDir, distThroughAtmosphere, color);
                    
                    return float4(lightAtAtmospherePoint, color.a);
                }
                
                return color;
            }
            
            ENDHLSL
        }
    }
}
