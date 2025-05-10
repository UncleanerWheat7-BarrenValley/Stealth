using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.SocialPlatforms;
using static PlayerStateMachine;
using static UnityEngine.GraphicsBuffer;
public class PlayerController : MonoBehaviour
{
    public MyState myState;
    public Rigidbody rb;
    public CharacterController characterController;
    public GameObject model;
    public GameObject enemyToAimAt;
    public float movementSpeed;
    public List<GameObject> weapons;
    [SerializeField]
    GameObject currentGun;

    [SerializeField]
    GameObject gunPlacement;
    [SerializeField]
    AnimationHandler animationHandler;
    [SerializeField]
    CapsuleCollider collider;
    [SerializeField]
    GameObject weaponWheel;

    private PlayerStateMachine playerStateMachine = new PlayerStateMachine();
    PlayerInput playerInput;
    GameObject mainCamera;
    Vector3 moveDirection;
    Vector3 normalVector;
    public LayerMask enemyLayerMask;
    public WeaponController weaponController;

    bool weaponSelect = false;

    public enum MyState
    {
        normal, crouch, wall, aim, dead
    }


    public CodecCalls codecCalls;
    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        characterController = GetComponent<CharacterController>();
        mainCamera = Camera.main.gameObject;
        SetState(myState);


        //string[] dialogue = {
        //    "Snake, do you read me?",
        //    "Loud and clear, Colonel.",
        //    "You have to infiltrate the base without being detected."
        //};

        //codecCalls.StartCodecConversation(dialogue);
    }
    void FixedUpdate()
    {
        float tick = Time.deltaTime;
        ApplyInputMovement();
        HandlePlayerRotation(tick);
        playerStateMachine.Update();
        SnapToTerrain();
    }

    public LayerMask LayerMask;
    RaycastHit hit;
    private void SnapToTerrain()
    {
        Debug.DrawRay(transform.position + Vector3.up * 1, transform.up * -2, Color.red);
        if (Physics.Raycast(transform.position + Vector3.up * 1, transform.up * -1, out hit, 2f, LayerMask))
        {
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }
    }

    public void SetState(MyState newState)
    {
        myState = newState;
        UpdateCurrentState();
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
        else if (myState == MyState.aim)
        {
            playerStateMachine.ChangeState(new AimState(this.gameObject));
            HandleAutoAim();
        }
        else if (myState == MyState.dead)
        {
            playerStateMachine.ChangeState(new DeadState(this.gameObject));
        }
    }

    private void HandleAutoAim()
    {
        enemyToAimAt = null;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 10, enemyLayerMask);

        float distance = 20;
        foreach (Collider enemy in hitColliders)
        {
            if (Vector3.Angle(enemy.transform.position - transform.position, transform.forward) < 30)
            {
                if (Vector3.Distance(transform.position, enemy.transform.position) < distance)
                {
                    enemyToAimAt = enemy.gameObject;
                    distance = Vector3.Distance(transform.position, enemy.transform.position);
                }
            }
        }

        if (enemyToAimAt != null)
        {
            transform.LookAt(enemyToAimAt.transform.position);
        }
    }

    private void ApplyInputMovement()
    {
        if (myState is MyState.aim)
        {
            return;
        }

        if (myState is not MyState.wall)
        {
            moveDirection = new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z).normalized * playerInput.verticalInput;
            moveDirection += new Vector3(mainCamera.transform.right.x, 0, mainCamera.transform.right.z).normalized * playerInput.horizontalInput;
        }
        else
        {
            moveDirection = transform.right * -playerInput.horizontalInput;
            Debug.DrawRay(transform.position + moveDirection / 2, transform.forward * -1, Color.red);
            if (!Physics.Raycast(transform.position + moveDirection / 2, transform.forward * -1, out RaycastHit hitInfo, 0.5f))
            {
                moveDirection = Vector3.zero;
            }
        }

        moveDirection.y = 0;
        float currentSpeed = movementSpeed * moveDirection.magnitude;
        moveDirection.Normalize();
        moveDirection *= currentSpeed;

        Vector3 projectedVelocity = Vector3.ProjectOnPlane(moveDirection, normalVector) + Vector3.down * 1f;
        print(projectedVelocity);
        //GetComponent<Rigidbody>().velocity = projectedVelocity;
        GetComponent<CharacterController>().Move(projectedVelocity * Time.deltaTime);
    }

    private void HandlePlayerRotation(float tick)
    {
        if (myState is MyState.wall) return;

        Vector3 targetDir = (mainCamera.transform.forward * playerInput.verticalInput) + (mainCamera.transform.right * playerInput.horizontalInput);
        targetDir.y = 0;
        targetDir.Normalize();

        if (targetDir == Vector3.zero)
        {
            targetDir = transform.forward;
        }

        Quaternion tr = Quaternion.LookRotation(targetDir);
        Quaternion targetRotation = Quaternion.Slerp(transform.rotation, tr, 15 * tick);

        transform.rotation = targetRotation;
    }

    public void HandleWallHug()
    {
        if (myState is MyState.wall)
        {
            myState = MyState.normal;
            SetState(myState);
            return;
        }

        Debug.DrawRay(transform.localPosition, transform.forward * 1, UnityEngine.Color.red);

        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hitInfo, 1))
        {
            myState = MyState.wall;
            SetState(myState);
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

    private void AttachToWall(RaycastHit hitInfo)
    {
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, hitInfo.normal);
        float yRot = rotation.eulerAngles.y;
        Quaternion yRotQuaternion = Quaternion.Euler(0, yRot, 0);
        transform.position = hitInfo.point;
        transform.rotation = yRotQuaternion;
    }

    internal void Dead()
    {
        animationHandler.PlayDeath();
        DisableSelf();
    }

    private void DisableSelf()
    {
        this.enabled = false;
    }

    internal void ToggleWeaponWheel()
    {
        if (weaponWheel.gameObject.activeSelf)
        {
            weaponWheel.SetActive(false);
            weaponSelect = false;
        }
        else
        {
            weaponWheel.SetActive(true);
            weaponSelect = true;
        }
    }

    internal void CloseWeaponWheel()
    {
        weaponWheel.SetActive(false);
    }

    public void SelectedWeapon(int weaponInput)
    {
        weaponWheel.GetComponent<WeaponController>().ChangeSelectedGun(weaponInput);
    }

    public void ActivateGun(bool gunActive)
    {
        Destroy(currentGun);

        playerInput.gunFlag = gunActive;
        gunPlacement.SetActive(gunActive);

        if (gunActive)
        {
            currentGun = Instantiate(weapons[weaponController.currentGun], gunPlacement.transform.position, gunPlacement.transform.rotation, gunPlacement.transform);
        }
    }

    internal void Crouch(bool crouched)
    {
        animationHandler.Crouch(crouched);

        if (crouched)
        {
            collider.height = 1f;
            collider.center = new Vector3(0, 0.55f, 0);
        }
        else
        {
            collider.height = 1.6f;
            collider.center = new Vector3(0, 0.75f, 0);
        }
    }

    internal void HandleWallKnock()
    {
        if (myState is not MyState.wall)
        {
            myState = MyState.normal;
            SetState(myState);
            return;
        }

        animationHandler.WallKnock();

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "CameraSwitch")
        {
            print("CameraSwitchIn");
            mainCamera.GetComponent<CameraController2>().SetCameraTarget(other.transform.Find("CameraStealPos").position);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "CameraSwitch")
        {
            mainCamera.GetComponent<CameraController2>().ResetCameraTarget();
        }
    }
}
