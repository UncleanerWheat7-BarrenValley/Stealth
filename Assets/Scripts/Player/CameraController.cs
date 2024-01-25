using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CameraController : MonoBehaviour
{
    PlayerInput playerInput;
    public Transform target;  // the target the camera is orbiting around
    public float distance;  // distance from target
    public float sensitivity;  // mouse sensitivity
    public float minY;  // minimum Y angle
    public float maxY;  // maximum Y angle

    private float currentX = 0.0f;
    private float currentY = 0.0f;

    void Start()
    {
        playerInput = GetComponentInParent<PlayerInput>();
    }

    void LateUpdate()
    {
        currentX += playerInput.mouseX * sensitivity;
        currentY += playerInput.mouseY * sensitivity;
        currentY = Mathf.Clamp(currentY, minY, maxY);  // clamp Y angle

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        transform.position = target.position - rotation * Vector3.forward * distance;
        transform.rotation = rotation;
    }
}
