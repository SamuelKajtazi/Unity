using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 20f;
    public float lookSpeed = 2f;
    public float lookXLimit = 89f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private CharacterController characterController;

    private bool canMove = true;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // settings saved from the main menu
        lookSpeed = PlayerPrefs.GetFloat("Sensitivity", 2f);
        playerCamera.fieldOfView = PlayerPrefs.GetFloat("FOV", 60f);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;   // dead or paused: no movement

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        bool crouching = Input.GetKey(KeyCode.LeftControl) && canMove;
        bool isRunning = Input.GetKey(KeyCode.LeftShift) && !crouching;

        float currentSpeed = crouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);

        float curSpeedX = canMove ? currentSpeed * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? currentSpeed * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
            moveDirection.y = jumpPower;
        else
            moveDirection.y = movementDirectionY;

        if (!characterController.isGrounded)
            moveDirection.y -= gravity * Time.deltaTime;

        // crouch: only change the capsule when the height actually needs to change
        float targetHeight = crouching ? crouchHeight : defaultHeight;
        if (!Mathf.Approximately(characterController.height, targetHeight))
        {
            characterController.height = targetHeight;
            characterController.center = Vector3.zero;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        if (canMove && !PauseMenu.GameIsPaused)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }
}