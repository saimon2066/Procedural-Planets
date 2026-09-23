using System;
using PQS.Mods;
using UnityEngine;

namespace Celestial
{
    [CreateAssetMenu(fileName = "CelestialBodySO_", menuName = "Celestial Body", order = 0)]
    public class CelestialBodySO : ScriptableObject
    {
        [SerializeField]
        public string Name = "Celestial";
        public float Radius = 1000f;
        public NoiseMod[] NoiseMods;
        [Header("Material")]
        public Gradient MaterialGradient;
        public Vector2 MinMax;
        
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