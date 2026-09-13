using System;
using PQS.Mods;
using UnityEngine;

namespace Celestial
{
    [CreateAssetMenu(fileName = "CelestialBodySO_", menuName = "Celestial Body", order = 0)]
    public class CelestialBodySO : ScriptableObject
    {
        public string Name = "Celestial";
        public Material Material;
        // public Vector3 Position;
        public float Radius = 1000f;
        public NoiseMod[] NoiseMods;

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