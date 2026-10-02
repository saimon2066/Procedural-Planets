using Celestial;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PQS
{
    public class PQSMaterialController
    {
        private const int _GRADIENT_RESOLUTION = 128;
        
        public readonly Material TerrainMaterial = new Material(PQSManager.Instance.TerrainShader);
        // public readonly Material OceanMaterial = new Material(PQSManager.Instance.OceanShader);
        // public readonly Material AtmosphereMaterial = new Material(PQSManager.Instance.AtmosphereShader);

        private readonly Texture2D _terrainTexture = new Texture2D(_GRADIENT_RESOLUTION, 1);
        private readonly Texture2D _slopeTexture = new Texture2D(_GRADIENT_RESOLUTION, 1);
        
        private readonly CelestialBodySO _celestialBodySO;

        private static Color[] GetGradientColors(Gradient gradient, int resolution)
        {
            Color[] colors = new Color[resolution];
            for (int i = 0; i < resolution; i++)
            {
                colors[i] = gradient.Evaluate(i / (resolution - 1f));
            }

            return colors;
        }
        
        public PQSMaterialController(CelestialBodySO celestialBodySO)
        {
            _celestialBodySO = celestialBodySO;
            
            SetMaterial();
        }
            
        public void SetMaterial()
        {
            Color[] terrainColors = GetGradientColors(_celestialBodySO.TerrainGradient, _GRADIENT_RESOLUTION);
            _terrainTexture.SetPixels(terrainColors);
            _terrainTexture.Apply();

            Color[] slopeColors = GetGradientColors(_celestialBodySO.SlopeGradient, _GRADIENT_RESOLUTION);
            _slopeTexture.SetPixels(slopeColors);
            _slopeTexture.Apply();
            
            TerrainMaterial.SetTexture("_TerrainTexture", _terrainTexture);
            TerrainMaterial.SetTexture("_SlopeTexture", _slopeTexture);
            TerrainMaterial.SetVector("_Smoothstep", _celestialBodySO.Smoothstep);
            ApproximateAndSetElevationMinMax();
        }
        
        private void ApproximateAndSetElevationMinMax()
        {
            const float quantizeMultiplier = 1000000f;
            
            NativeArray<int> quantizedMinMax =
                new NativeArray<int>(2, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            quantizedMinMax[0] = int.MaxValue;
            quantizedMinMax[1] = int.MinValue;

            const int floatSize = sizeof(float);
            const int intSize = sizeof(int);
            const int noiseModSize = intSize * 6 + floatSize * 7;
            
            ComputeBuffer quantizedMinMaxBuffer = new ComputeBuffer(2, intSize);
            quantizedMinMaxBuffer.SetData(quantizedMinMax);
            
            int noiseModCount = 9999;
            if (_celestialBodySO.NoiseMods.Length > 0)
            {
                noiseModCount = _celestialBodySO.NoiseMods.Length;
            }
            ComputeBuffer noiseModBuffer = new ComputeBuffer(noiseModCount, noiseModSize);
            noiseModBuffer.SetData(_celestialBodySO.NoiseMods);
            
            ComputeShader shader = PQSManager.Instance.MinMaxCS;

            int minMaxKernel = shader.FindKernel("QuantizedMinMax");
            
            shader.SetBuffer(minMaxKernel, "QuantizedMinMaxBuffer", quantizedMinMaxBuffer);
            shader.SetBuffer(minMaxKernel, "NoiseModBuffer", noiseModBuffer);
            
            shader.SetFloat("Radius", _celestialBodySO.Radius);
            
            shader.SetInt("SampleCount", PQSManager.MIN_MAX_SAMPLE_COUNT);
            
            int mainGroup = Mathf.CeilToInt(PQSManager.MIN_MAX_SAMPLE_COUNT / 64f);
            
            shader.Dispatch(minMaxKernel, mainGroup, 1, 1);
            
            AsyncGPUReadback.Request(quantizedMinMaxBuffer, quantizedMinMaxRequest =>
            {
                if (quantizedMinMaxRequest.hasError)
                {
                    quantizedMinMax.Dispose();
                    quantizedMinMaxBuffer.Dispose();
                    noiseModBuffer.Dispose();
                    
                    return;
                }
                quantizedMinMaxRequest.GetData<int>().CopyTo(quantizedMinMax);

                float min = quantizedMinMax[0] / quantizeMultiplier;
                float max = quantizedMinMax[1] / quantizeMultiplier;
                Vector2 minMax = new Vector2(min, max);
                
                TerrainMaterial.SetVector("_ElevationMinMax", minMax);
                
                quantizedMinMax.Dispose();
                quantizedMinMaxBuffer.Dispose();
                noiseModBuffer.Dispose();
            });
        } 
    }
}