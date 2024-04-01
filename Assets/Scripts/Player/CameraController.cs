using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

public class CameraController : MonoBehaviour
{
    PlayerInput playerInput;
    public Transform target;  // the target the camera is orbiting around
    public Transform targetAim;
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
        if (playerInput.aimFlag != true)
        {
            NormalCameraFollow();
            return;
        }
        else
        {
            AimCameraFollow();
        }
    }

    private void AimCameraFollow()
    {
        currentX += playerInput.mouseX * sensitivity;
        currentY = 20;

        Quaternion rotation = Quaternion.Euler(0, currentX, 0);
        //transform.position = targetAim.position - rotation * Vector3.forward * 0.5f;
        transform.position = Vector3.Slerp(transform.position, targetAim.position - rotation * Vector3.forward * 0.5f, Time.deltaTime * 10);

        Quaternion lookAt = Quaternion.Slerp(transform.rotation, rotation, Time.fixedDeltaTime * 20);
        transform.rotation = lookAt;


        //transform.rotation = rotation;
    }

    private void NormalCameraFollow()
    {
        currentX += playerInput.mouseX * sensitivity;
        currentY += playerInput.mouseY * sensitivity;
        currentY = Mathf.Clamp(currentY, minY, maxY);  // clamp Y angle

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        transform.position = target.position - rotation * Vector3.forward * distance;
        transform.rotation = rotation;
    }
}
