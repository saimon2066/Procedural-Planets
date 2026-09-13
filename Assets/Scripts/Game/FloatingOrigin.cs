using System;
using UnityEngine;

namespace Game
{
    public class FloatingOrigin : MonoBehaviour
    {
        private const float _THRESHOLD = 1000f;
        private const float _SQR_THRESHOLD = _THRESHOLD * _THRESHOLD;
        
        
        private void Update()
        {
            if (GameManager.Instance.PlayerTransform.position.sqrMagnitude > _SQR_THRESHOLD)
            {
                GameManager.Instance.WorldTransform.position -= GameManager.Instance.PlayerTransform.position;
                GameManager.Instance.PlayerTransform.position = Vector3.zero;
            }
        }
    }
}
