using UnityEngine;

namespace PQS
{
    public class PQSManager : MonoBehaviour
    {
        public const int GLOBAL_RESOLUTION = 64;
        public const int MAX_DETAIL_LEVEL = 10;
        public const int MIN_DETAIL_LEVEL = 2;
        public const int MIN_MAX_SAMPLE_COUNT = 262144;
        
        public static PQSManager Instance { get; private set; }

        public Shader TerrainShader;
        public Shader OceanShader;
        public Shader AtmosphereShader;
        public ComputeShader ChunkCS;
        public ComputeShader MinMaxCS;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }
    }
}