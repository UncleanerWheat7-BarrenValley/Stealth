using System;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    [SerializeField]
    PlayerController playerController;
    Controls controls;
    public CameraController cameraController;
    Vector2 movementInput;
    Vector2 cameraInput;

    public bool eInput = false;
    public bool twoInput = false;
    public bool wallHugFlag;
    public bool gunFlag;
    public bool aimFlag;

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
            controls.PlayerActions.HugWall.performed += i => eInput = i.performed;
            controls.PlayerActions.SelectGun.performed += i => twoInput = i.performed;
            controls.PlayerInput.Fire1.performed += inputActions => Fire1();
        }

        controls.Enable();
    }

    private void Update()
    {
        TranslateInputMovement();
        TranslateInputCamera();
        HandleEInput();
        HandleTwoInput();
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
        if (eInput)
        {
            wallHugFlag = !wallHugFlag;
            playerController.HandleWallHug();
            eInput = false;
        }
    }
    private void HandleTwoInput()
    {
        if (twoInput)
        {
            print(twoInput);
            gunFlag = !gunFlag;
            GetComponent<PlayerController>().gunB = gunFlag;
            twoInput = false;
        }
    }

    private void Fire1()
    {
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
        else if(controls.PlayerInput.Fire1.IsPressed() && !gunFlag)
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
