using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    PlayerInput playerInput;
    Transform mainCamera;
    Vector3 moveDirection;
    Vector3 normalVector;

    public float movementSpeed;
    public Rigidbody rb;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        float tick = Time.deltaTime;
        ApplyInputMovement(tick);
    }

    private void ApplyInputMovement(float tick)
    {
        moveDirection = mainCamera.forward * playerInput.verticalInput;
        moveDirection += mainCamera.right * playerInput.horizontalInput;
        moveDirection.y = 0;
        moveDirection.Normalize();

        moveDirection *= movementSpeed;

        Vector3 projectedVelocity = Vector3.ProjectOnPlane(moveDirection, normalVector);
        GetComponent<Rigidbody>().velocity = projectedVelocity;

        handlePlayerRotation(tick);
    }

    private void handlePlayerRotation(float delta)
    {
        Vector3 targetDir = Vector3.zero;
        float moveOverride = playerInput.moveAmount;

        targetDir = mainCamera.forward * playerInput.verticalInput;
        targetDir += mainCamera.right * playerInput.horizontalInput;
        targetDir.Normalize();
        targetDir.y = 0;

        if (targetDir == Vector3.zero)
        {
            targetDir = transform.forward;
        }

        Quaternion tr = Quaternion.LookRotation(targetDir);
        Quaternion targetRotation = Quaternion.Slerp(transform.rotation, tr, 15 * delta);

        transform.rotation = targetRotation;
    }
}
