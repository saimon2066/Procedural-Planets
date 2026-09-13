using System;
using Game.Input;
using TMPro;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float _sensitivity;
    [SerializeField] private TextMeshProUGUI _speedDisplay;
    
    private float _speed;
    
    private float _pitch;
    private float _yaw;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Vector2 moveVector = InputManager.Instance.Inputs.Player.Move.ReadValue<Vector2>();
        Vector2 lookVector = InputManager.Instance.Inputs.Player.Look.ReadValue<Vector2>();
        Vector2 speedVector = InputManager.Instance.Inputs.Player.Speed.ReadValue<Vector2>() ;
        
        _pitch -= lookVector.y * _sensitivity;
        _pitch = Mathf.Clamp(_pitch, -85, 85);
        _yaw += lookVector.x * _sensitivity;
        
        Quaternion rot = Quaternion.Euler(_pitch, _yaw, 0f);
        
        _speed *= 1f + speedVector.y * 0.5f;
        _speed = Mathf.Clamp(_speed, 0.0001f, 100000f);
        _speedDisplay.text = $"Speed: {_speed}";
        
        transform.position += (transform.right * moveVector.x + transform.forward * moveVector.y) * _speed;
        transform.localRotation = rot;
        
    }
}
