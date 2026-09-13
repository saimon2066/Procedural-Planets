using UnityEngine;

namespace Celestial
{
    public abstract class CelestialBodyMonoBehaviour : MonoBehaviour
    {
        public CelestialBodySO CelestialBodySO;
        
        protected bool _isInitialized;
        
        public virtual void Initialize(CelestialBodySO celestialBodySO)
        {
            CelestialBodySO = celestialBodySO;
            _isInitialized = true;
        }
    }
}