using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerStateMachine;
using static WeaponWheelButtonController;
public class PlayerController : MonoBehaviour
{
    public MyState myState;
    public Rigidbody rb;
    public GameObject model;
    public GameObject enemyToAimAt;


    public static int weaponID;

    public float movementSpeed;

    [SerializeField]
    GameObject gunObj;
    [SerializeField]
    AnimationHandler animationHandler;
    [SerializeField]
    weaponWheelController weaponWheelUI;

    private PlayerStateMachine playerStateMachine = new PlayerStateMachine();
    PlayerInput playerInput;
    Transform mainCamera;
    Vector3 moveDirection;
    Vector3 normalVector;
    public LayerMask enemyLayerMask;
    public bool gunB
    {
        get { return gunB; }
        set
        {
            gunObj.SetActive(playerInput.gunFlag);
        }
    }

    private void OnEnable()
    {
        selectedWeapon += SelectedWeapon;
    }

    private void OnDisable()
    {
        selectedWeapon -= SelectedWeapon;
    }

    public enum MyState
    {
        normal, crouch, wall, aim, dead
    }

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main.transform;
        SetState(myState);
        SelectedWeapon();
    }
    void Update()
    {
        float tick = Time.deltaTime;
        ApplyInputMovement();
        HandlePlayerRotation(tick);
        playerStateMachine.Update();
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
            moveDirection = mainCamera.forward * playerInput.verticalInput;
            moveDirection += mainCamera.right * playerInput.horizontalInput;
        }
        else
        {
            moveDirection = transform.right * -playerInput.horizontalInput;
            Debug.DrawRay(transform.position + moveDirection * 1, transform.forward + Vector3.right * 0.5f * -1, UnityEngine.Color.red);
            if (!Physics.Raycast(transform.position + moveDirection / 3, transform.forward * -1 + Vector3.right * 0.5f, out RaycastHit hitInfo, 0.5f))
            {
                moveDirection = Vector3.zero;
            }
        }

        moveDirection.y = 0;
        moveDirection.Normalize();
        moveDirection *= movementSpeed;

        Vector3 projectedVelocity = Vector3.ProjectOnPlane(moveDirection, normalVector);
        GetComponent<Rigidbody>().velocity = projectedVelocity;
    }

    private void HandlePlayerRotation(float tick)
    {
        if (myState is MyState.wall) return;

        Vector3 targetDir = (mainCamera.forward * playerInput.verticalInput) + (mainCamera.right * playerInput.horizontalInput);
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

    internal void OpenWeaponWheel()
    {
        weaponWheelUI.OpenWeaponWheel();
    }

    internal void CloseWeaponWheel()
    {        
        weaponWheelUI.OpenWeaponWheel();
    }

    void SelectedWeapon()
    {
        if (weaponID == 1)
        {
            playerInput.gunFlag = true;
            gunB = true;
        }
        else
        {
            playerInput.gunFlag = false;
            gunB = false;
        }
    }
}
