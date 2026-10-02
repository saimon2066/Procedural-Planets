using System;
using PQS.Mods;
using UnityEngine;

namespace Celestial
{
    [CreateAssetMenu(fileName = "CelestialBodySO_", menuName = "Celestial Body", order = 0)]
    public class CelestialBodySO : ScriptableObject
    {
        [Header("General")]
        public string Name;
        public float Radius;
        public NoiseMod[] NoiseMods;
        
        [Header("Terrain")]
        public Gradient TerrainGradient;
        public Gradient SlopeGradient;
        public Vector2 Smoothstep;
        
        [Header("Ocean")]
        public float OceanRadius;
        public float OceanDepth;
        public float OceanAlphaMultiplier;
        public float OceanSmoothness;
        public Color ColorA;
        public Color ColorB;
        
        [Header("Atmosphere")]
        public float AtmosphereRadius;
        public float AtmosphereBodyRadius;
        public Vector3 ScatteringWavelenghts;
        public float ScatteringStrength;
        public float DensityFalloff;
        public int ScatteringResolutionPoints;
        public int OpticalDepthResolutionPoints;
        
        public Action ValidateSO;
        public void OnValidate()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            
            ValidateSO?.Invoke();
        }
    }
}