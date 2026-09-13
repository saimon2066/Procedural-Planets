using System.Collections.Generic;
using Celestial;
using Game;
using UnityEngine;

namespace PQS
{
    public class PQSTerrain : CelestialBodyMonoBehaviour
    {
        public readonly List<PQSChunk> Chunks = new List<PQSChunk>();
        
        private readonly float[] _sqrThresholds = new float[PQSManager.MAX_DETAIL_LEVEL + 1];

        private static float SqrThreshold(float radius, int detailLevel)
        {
            float threshold = radius * Mathf.Pow(0.5f, detailLevel);
            return threshold * threshold;
        }
        
        public override void Initialize(CelestialBodySO celestialBodySO)
        {
            base.Initialize(celestialBodySO);
            
            CelestialBodySO.ValidateSO += OnValidateSO;

            for (int i = 0; i < _sqrThresholds.Length; i++)
            {
                _sqrThresholds[i] = SqrThreshold(CelestialBodySO.Radius, i);
            }
            
            Vector3[] cubeDirections = {new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, -1f, 0f), new Vector3(0f, 0f, 1f), new Vector3(0f, 0f, -1f)};
            foreach (Vector3 direction in cubeDirections)
            {
                PQSTerrain terrain = this;
                PQSChunk.LocalData localData = new PQSChunk.LocalData(direction);
                
                PQSChunk chunk = new PQSChunk(terrain, localData);
            }
        }
        
        private void OnDisable()
        {
            CelestialBodySO.ValidateSO -= OnValidateSO;
        }
        
        private void OnValidateSO()
        {
            foreach (PQSChunk chunk in Chunks)
            {
                chunk.Generate();
            }
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            Vector3 player = GameManager.Instance.PlayerTransform.position;

            foreach (PQSChunk chunk in Chunks.ToArray())
            {
                if (!chunk.IsActive || chunk.MeshRenderer == null)
                {
                    continue;
                }
                
                float sqrDistance = chunk.MeshRenderer.bounds.SqrDistance(player);
                if (sqrDistance < _sqrThresholds[chunk.DetailLevel] && chunk.ChildrenCount <= 0)
                {
                    chunk.Split();
                }
                else if (chunk.Parent != null)
                {
                    float parentSqrDistance = chunk.Parent.MeshRenderer.bounds.SqrDistance(player);
                    if (parentSqrDistance > _sqrThresholds[chunk.Parent.DetailLevel])
                    {
                        chunk.Merge();
                    }
                }
            }
        }

        private void OnDrawGizmos()
        {
            foreach (PQSChunk chunk in Chunks)
            {
                if (!chunk.IsActive || chunk.MeshRenderer == null)
                {
                    continue;
                }
                
                Gizmos.color = Color.HSVToRGB(1f / PQSManager.MAX_DETAIL_LEVEL * chunk.DetailLevel, 1f, 1f);

                Bounds bounds = chunk.MeshRenderer.bounds;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
    }
}
