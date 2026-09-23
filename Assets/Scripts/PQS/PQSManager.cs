using UnityEngine;

namespace PQS
{
    public class PQSManager : MonoBehaviour
    {
        public const int GLOBAL_RESOLUTION = 256;
        public const int MAX_DETAIL_LEVEL = 10;
        public const int MIN_DETAIL_LEVEL = 1;
        
        public static PQSManager Instance { get; private set; }

        public Shader TerrainShader;
        public ComputeShader ChunkCS;
        
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }
    }
}