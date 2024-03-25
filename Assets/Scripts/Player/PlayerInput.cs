using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    Controls controls;
    public CameraController cameraController;
    Vector2 movementInput;
    Vector2 cameraInput;

    public bool eInput = false;
    public bool wallHugFlag;

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
            controls.PlayerActions.HugWall.performed += i => eInput = i.performed;
        }

        controls.Enable();
    }

    private void Update()
    {
        TranslateInputMovement();
        TranslateInputCamera();
        HandleEInput();
    }

    private void TranslateInputCamera()
    {
        mouseX = cameraInput.x;
        mouseY = cameraInput.y *-1;      
    }

    private void TranslateInputMovement()
    {
        horizontalInput = movementInput.x;
        verticalInput = movementInput.y;
        moveAmount = Mathf.Clamp01(Mathf.Abs(horizontalInput) + Mathf.Abs(verticalInput));
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void HandleEInput() 
    {        
        if (eInput) 
        {
            wallHugFlag = !wallHugFlag;
            eInput = false;
        }
    }
}
