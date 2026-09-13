using UnityEngine;

namespace PQS
{
    public class PQSManager : MonoBehaviour
    {
        public const int GLOBAL_RESOLUTION = 32;
        public const int MAX_DETAIL_LEVEL = 10;
        public const int MIN_DETAIL_LEVEL = 0;
        
        public static PQSManager Instance { get; private set; }
        
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