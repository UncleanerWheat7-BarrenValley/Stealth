using System;
using System.Data;
using System.Drawing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.HID;
using static PlayerStateMachine;
using static UnityEngine.GraphicsBuffer;



public class PlayerController : MonoBehaviour
{
    public MyState myState;
    private PlayerStateMachine playerStateMachine = new PlayerStateMachine();

    PlayerInput playerInput;
    Transform mainCamera;
    Vector3 moveDirection;
    Vector3 normalVector;

    public float movementSpeed;
    public Rigidbody rb;
    public GameObject model;

    public enum MyState
    {
        normal, crouch, wall
    }

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main.transform;
        UpdateCurrentState();
    }

    // Update is called once per frame
    void Update()
    {
        float tick = Time.deltaTime;
        ApplyInputMovement(tick);
        HandleWallHug(tick);
        playerStateMachine.Update();
    }



    public void UpdateCurrentState()
    {
        if (myState == MyState.normal)
        {
            playerStateMachine.ChangeState(new NormalState(this.gameObject));
        }
        else if (myState == MyState.crouch)
        {
            playerStateMachine.ChangeState(new CrouchState(this.gameObject));
        }
        else if (myState == MyState.wall)
        {
            playerStateMachine.ChangeState(new WallState(this.gameObject));
        }
    }

    private void ApplyInputMovement(float tick)
    {
        if (myState is not MyState.wall)
        {
            moveDirection = mainCamera.forward * playerInput.verticalInput;
            moveDirection += mainCamera.right * playerInput.horizontalInput;
        }
        else
        {
            moveDirection = transform.right * -playerInput.horizontalInput;
            Debug.DrawRay(transform.position + moveDirection * 1, transform.forward + Vector3.right * 0.5f * -1, UnityEngine.Color.red);
            if (!Physics.Raycast(transform.position + moveDirection / 3, transform.forward * -1 + Vector3.right * 0.5f, out RaycastHit hitInfo, 0.5f))
            {
                print("HOORAH");
                moveDirection = Vector3.zero;                
            }
        }

        moveDirection.y = 0;
        moveDirection.Normalize();

        moveDirection *= movementSpeed;

        Vector3 projectedVelocity = Vector3.ProjectOnPlane(moveDirection, normalVector);
        GetComponent<Rigidbody>().velocity = projectedVelocity;

        handlePlayerRotation(tick);
    }

    private void handlePlayerRotation(float delta)
    {
        if (myState is MyState.wall) return;

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

    public void HandleWallHug(float tick)
    {
        if (playerInput.wallHugFlag)
        {
            Debug.DrawRay(transform.localPosition, transform.forward * 1, UnityEngine.Color.red);

            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hitInfo, 1))
            {
                print(hitInfo.transform.name);

                myState = MyState.wall;
                UpdateCurrentState();
                AttachToWall(hitInfo);

            }
            else 
            {
                if (myState is not MyState.wall) 
                {
                    playerInput.wallHugFlag = false;
                }
            }
        }
        else 
        {
            myState = MyState.normal;
            UpdateCurrentState();
        }
    }

    private void AttachToWall(RaycastHit hitInfo)
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, hitInfo.normal);
        float yRot = rotation.eulerAngles.y;
        Quaternion yRotQuaternion = Quaternion.Euler(0, yRot, 0);
        transform.position = hitInfo.point;
        transform.rotation = yRotQuaternion;
    }
}
