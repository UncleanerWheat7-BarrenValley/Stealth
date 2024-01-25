using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    Controls controls;
    public CameraController cameraController;
    Vector2 movementInput;
    Vector2 cameraInput;
    public float horizontalInput;
    public float verticalInput;
    public float moveAmount;
    public float mouseX;
    public float mouseY;

    private void OnEnable()
    {
        if (controls == null)
        {
            controls = new Controls();
            controls.PlayerInput.Movement.performed += inputActions => movementInput = inputActions.ReadValue<Vector2>();
            controls.PlayerInput.Camera.performed += i => cameraInput = i.ReadValue<Vector2>();
        }

        controls.Enable();
    }

    private void Update()
    {
        float tick = Time.deltaTime;
        TranslateInputMovement(tick);
        TranslateInputCamera(tick);
    }

    private void TranslateInputCamera(float tick)
    {
        mouseX = cameraInput.x;
        mouseY = cameraInput.y *-1;      
    }

    private void TranslateInputMovement(float tick)
    {
        horizontalInput = movementInput.x;
        verticalInput = movementInput.y;
        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));
    }

    private void OnDisable()
    {
        controls.Disable();
    }
}
