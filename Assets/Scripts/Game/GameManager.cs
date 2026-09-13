using UnityEngine;

namespace Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public Transform WorldTransform;
        public Transform PlayerTransform;

        private void Awake()
        {
            Instance = this;
        }
    }
}