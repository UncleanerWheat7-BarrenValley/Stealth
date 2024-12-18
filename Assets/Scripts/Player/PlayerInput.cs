using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    [SerializeField]
    PlayerController playerController;
    [SerializeField]
    UIController uiController;
    Controls controls;
    public CameraController cameraController;
    Vector2 movementInput;
    Vector2 cameraInput;


    public bool eInput = false;
    public bool twoInput = false;
    public bool wallHugFlag;
    public bool gunFlag;
    public bool aimFlag;
    public bool weaponWheelFlag = false;
    public bool crouchFlag = false;

    public float horizontalInput;
    public float verticalInput;
    public float moveAmount;
    public float mouseX;
    public float mouseY;
    public bool attack = false;

    private void OnEnable()
    {
        if (controls == null)
        {
            controls = new Controls();
            controls.PlayerInput.Movement.performed += inputActions => movementInput = inputActions.ReadValue<Vector2>();
            controls.PlayerInput.Camera.performed += i => cameraInput = i.ReadValue<Vector2>();

            controls.PlayerActions.HugWall.performed += i => HandleEInput();
            controls.PlayerActions.WeaponWheel.performed += inputActions => HandleWeaponWheel();

            controls.PlayerInput.Pause.performed += InputActions => HandlePause();

            controls.PlayerInput.Fire1.performed += inputActions => Fire1();
            controls.PlayerActions.Crouch.performed += inputActions => HandleCrouch();
            controls.PlayerActions.WallKnock.performed += inputActions => HandleWallKnock();

            controls.WeaponWheel.WeaponSelect.performed += inputActions => HandleWeaponSelect(inputActions.ReadValue<Vector2>());
        }

        controls.Enable();
    }



    private void Update()
    {
        if (!weaponWheelFlag)
        {
            TranslateInputMovement();
            TranslateInputCamera();
        }
        else
        {
            horizontalInput = 0f; verticalInput = 0f; moveAmount = 0f; mouseX = 0f; mouseY = 0f;
        }
    }

    private void TranslateInputCamera()
    {
        mouseX = cameraInput.x;
        mouseY = cameraInput.y * -1;
    }

    private void TranslateInputMovement()
    {
        horizontalInput = movementInput.x;
        verticalInput = movementInput.y;

        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));
    }
    private void HandleEInput()
    {
        wallHugFlag = !wallHugFlag;
        playerController.HandleWallHug();
    }

    private void HandleWeaponWheel()
    {
        weaponWheelFlag = !weaponWheelFlag;
        playerController.ToggleWeaponWheel();
    }

    private void HandleWeaponSelect(Vector2 vector2Direction)
    {
        if (!weaponWheelFlag) return;
        if (vector2Direction.x == 1 || vector2Direction.y == -1)
        {
            playerController.SelectedWeapon(1);
        }
        else if (vector2Direction.x == -1 || vector2Direction.y == 1)
        {
            playerController.SelectedWeapon(-1);
        }
    }

    private void HandleCrouch()
    {
        crouchFlag = !crouchFlag;
        if (crouchFlag)
        {
            playerController.SetState(PlayerController.MyState.crouch);
        }
        else
        {
            playerController.SetState(PlayerController.MyState.normal);
        }
    }

    private void HandlePause()
    {
        uiController.OpenPauseMenu();
    }

    private void HandleWallKnock()
    {
        playerController.HandleWallKnock();
    }

    private void Fire1()
    {
        if (weaponWheelFlag)
        {
            return;
        }
        if (controls.PlayerInput.Fire1.IsPressed() && gunFlag)
        {
            print("aim");
            aimFlag = true;
            attack = true;
            playerController.SetState(PlayerController.MyState.aim);
        }
        else if (!controls.PlayerInput.Fire1.IsPressed() && gunFlag)
        {
            print("Fire");
            aimFlag = false;
            attack = true;
            playerController.SetState(PlayerController.MyState.normal);
        }
        else if (controls.PlayerInput.Fire1.IsPressed() && !gunFlag)
        {
            print("Fire");
            attack = true;
        }
    }

    private void OnDisable()
    {
        controls.Disable();
    }
}
