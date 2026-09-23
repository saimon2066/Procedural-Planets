using Game;
using PQS;
using UnityEngine;

namespace Celestial
{
    public class CelestialBodyManager : MonoBehaviour
    {
        [SerializeField] private CelestialBodySO[] _celestialBodies;

        private void Start()
        {
            foreach (CelestialBodySO so in _celestialBodies)
            {
                 GameObject go = new GameObject
                 {
                     name = so.Name,
                     transform = { parent = GameManager.Instance.WorldTransform }
                 };
                 go.AddComponent<PQSTerrain>().Initialize(so);
            }
        }
    }
}