using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FPSController : MonoBehaviour
{
    [Header("=== KAMERA AYARLARI (Buraya Ata!) ===")]
    [Tooltip("Player kamerasını buraya sürükle")]
    public Camera playerCamera;
    [Header("=== ANIMATOR (Opsiyonel) ===")]
    [Tooltip("Animator varsa buraya ata, yoksa boş bırak")]
    public Animator playerAnimator;
    [Header("=== HAREKET AYARLARI ===")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 9f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravity = 20f;
    [Header("=== FARE AYARLARI ===")]
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float maxLookAngle = 85f;
    [SerializeField] private bool invertY = false;
    [Header("=== EĞİLME AYARLARI ===")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [Header("=== HEADBOB AYARLARI ===")]
    [SerializeField] private bool enableHeadbob = true;
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = 0.1f;
    [Header("=== AYAK SESİ AYARLARI ===")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip landSound;
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float sprintStepInterval = 0.3f;
    [SerializeField] private float footstepVolume = 0.5f;
    private CharacterController characterController;
    private AudioSource audioSource;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0f;
    private float currentSpeed;
    private bool isCrouching = false;
    private bool isSprinting = false;
    private bool isMoving = false;
    private bool wasGrounded = true;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpPressed;
    private bool sprintPressed;
    private bool crouchPressed;
    private float headbobTimer = 0f;
    private Vector3 originalCameraPosition;
    private float stepTimer = 0f;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
    private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting");
    private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        SetupInputActions();
    }
    void SetupInputActions()
    {
        moveAction = new InputAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        moveAction.AddBinding("<Gamepad>/leftStick");
        lookAction = new InputAction("Look", InputActionType.Value);
        lookAction.AddBinding("<Mouse>/delta");
        lookAction.AddBinding("<Gamepad>/rightStick");
        jumpAction = new InputAction("Jump", InputActionType.Button);
        jumpAction.AddBinding("<Keyboard>/space");
        jumpAction.AddBinding("<Gamepad>/buttonSouth");
        sprintAction = new InputAction("Sprint", InputActionType.Button);
        sprintAction.AddBinding("<Keyboard>/leftShift");
        sprintAction.AddBinding("<Gamepad>/leftTrigger");
        crouchAction = new InputAction("Crouch", InputActionType.Button);
        crouchAction.AddBinding("<Keyboard>/leftCtrl");
        crouchAction.AddBinding("<Keyboard>/c");
        crouchAction.AddBinding("<Gamepad>/rightStickPress");
    }
    void OnEnable()
    {
        moveAction?.Enable();
        lookAction?.Enable();
        jumpAction?.Enable();
        sprintAction?.Enable();
        crouchAction?.Enable();
        if (crouchAction != null)
            crouchAction.performed += OnCrouchPerformed;
    }
    void OnDisable()
    {
        moveAction?.Disable();
        lookAction?.Disable();
        jumpAction?.Disable();
        sprintAction?.Disable();
        crouchAction?.Disable();
        if (crouchAction != null)
            crouchAction.performed -= OnCrouchPerformed;
    }
    void OnDestroy()
    {
        moveAction?.Dispose();
        lookAction?.Dispose();
        jumpAction?.Dispose();
        sprintAction?.Dispose();
        crouchAction?.Dispose();
    }
    void Start()
    {
        if (playerCamera == null)
        {
            Debug.LogError("FPSController: Kamera atanmadı! Inspector'dan kamerayı ata.");
            playerCamera = GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                Debug.Log("FPSController: Child'da kamera bulundu ve otomatik atandı.");
            }
        }
        if (playerCamera != null)
        {
            originalCameraPosition = playerCamera.transform.localPosition;
        }
        LockCursor(true);
        currentSpeed = walkSpeed;
    }
    void Update()
    {
        ReadInput();
        HandleMouseLook();
        HandleMovement();
        HandleCrouch();
        HandleHeadbob();
        HandleFootsteps();
        UpdateAnimator();
    }
    void ReadInput()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        lookInput = lookAction.ReadValue<Vector2>();
        jumpPressed = jumpAction.WasPressedThisFrame();
        sprintPressed = sprintAction.IsPressed();
    }
    void OnCrouchPerformed(InputAction.CallbackContext context)
    {
        isCrouching = !isCrouching;
    }
    private void HandleMouseLook()
    {
        if (playerCamera == null) return;
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;
        if (invertY)
            mouseY = -mouseY;
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
    private void HandleMovement()
    {
        bool wasOnGround = characterController.isGrounded;
        if (characterController.isGrounded)
        {
            Vector3 inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            inputDirection = transform.TransformDirection(inputDirection);
            isSprinting = sprintPressed && !isCrouching && moveInput.y > 0;
            if (isCrouching)
                currentSpeed = crouchSpeed;
            else if (isSprinting)
                currentSpeed = sprintSpeed;
            else
                currentSpeed = walkSpeed;
            moveDirection = inputDirection * currentSpeed;
            isMoving = inputDirection.magnitude > 0.1f;
            if (jumpPressed && !isCrouching)
            {
                moveDirection.y = jumpForce;
                PlaySound(jumpSound);
                TriggerJumpAnimation();
            }
            if (!wasOnGround && wasGrounded == false)
            {
                PlaySound(landSound);
            }
        }
        moveDirection.y -= gravity * Time.deltaTime;
        characterController.Move(moveDirection * Time.deltaTime);
        wasGrounded = wasOnGround;
    }
    private void HandleCrouch()
    {
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        if (Mathf.Abs(characterController.height - targetHeight) > 0.01f)
        {
            characterController.height = Mathf.Lerp(
                characterController.height, 
                targetHeight, 
                crouchTransitionSpeed * Time.deltaTime
            );
            if (playerCamera != null)
            {
                Vector3 cameraPos = playerCamera.transform.localPosition;
                float targetCamY = isCrouching ? 
                    originalCameraPosition.y - (standingHeight - crouchHeight) / 2f : 
                    originalCameraPosition.y;
                cameraPos.y = Mathf.Lerp(cameraPos.y, targetCamY, crouchTransitionSpeed * Time.deltaTime);
                playerCamera.transform.localPosition = cameraPos;
            }
        }
    }
    private void HandleHeadbob()
    {
        if (!enableHeadbob || playerCamera == null) return;
        if (isMoving && characterController.isGrounded)
        {
            float bobSpeed = isSprinting ? sprintBobSpeed : walkBobSpeed;
            float bobAmount = isSprinting ? sprintBobAmount : walkBobAmount;
            headbobTimer += Time.deltaTime * bobSpeed;
            Vector3 newPosition = originalCameraPosition;
            newPosition.y += Mathf.Sin(headbobTimer) * bobAmount;
            newPosition.x += Mathf.Cos(headbobTimer / 2f) * bobAmount * 0.5f;
            if (!isCrouching)
            {
                playerCamera.transform.localPosition = Vector3.Lerp(
                    playerCamera.transform.localPosition,
                    newPosition,
                    Time.deltaTime * 10f
                );
            }
        }
        else
        {
            headbobTimer = 0f;
            Vector3 targetPos = isCrouching ? 
                new Vector3(originalCameraPosition.x, originalCameraPosition.y - (standingHeight - crouchHeight) / 2f, originalCameraPosition.z) : 
                originalCameraPosition;
            playerCamera.transform.localPosition = Vector3.Lerp(
                playerCamera.transform.localPosition,
                targetPos,
                Time.deltaTime * 10f
            );
        }
    }
    private void HandleFootsteps()
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;
        if (isMoving && characterController.isGrounded && !isCrouching)
        {
            float stepInterval = isSprinting ? sprintStepInterval : walkStepInterval;
            stepTimer += Time.deltaTime;
            if (stepTimer >= stepInterval)
            {
                stepTimer = 0f;
                PlayRandomFootstep();
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }
    private void PlayRandomFootstep()
    {
        if (footstepSounds.Length == 0) return;
        int randomIndex = Random.Range(0, footstepSounds.Length);
        audioSource.PlayOneShot(footstepSounds[randomIndex], footstepVolume);
    }
    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, footstepVolume);
        }
    }
    private void UpdateAnimator()
    {
        if (playerAnimator == null) return;
        float animSpeed = moveDirection.magnitude / sprintSpeed;
        playerAnimator.SetFloat(SpeedHash, animSpeed);
        playerAnimator.SetBool(IsGroundedHash, characterController.isGrounded);
        playerAnimator.SetBool(IsCrouchingHash, isCrouching);
        playerAnimator.SetBool(IsSprintingHash, isSprinting);
    }
    private void TriggerJumpAnimation()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger(JumpTriggerHash);
        }
    }
    public void LockCursor(bool lockState)
    {
        Cursor.lockState = lockState ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !lockState;
    }
    public void SetMovementSpeed(float walk, float sprint, float crouch)
    {
        walkSpeed = walk;
        sprintSpeed = sprint;
        crouchSpeed = crouch;
    }
    public bool IsSprinting => isSprinting;
    public bool IsCrouching => isCrouching;
    public bool IsGrounded => characterController.isGrounded;
    public bool IsMoving => isMoving;
    public float CurrentSpeed => currentSpeed;
    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            LockCursor(true);
        }
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        mouseSensitivity = Mathf.Max(0.01f, mouseSensitivity);
        walkSpeed = Mathf.Max(0.1f, walkSpeed);
        sprintSpeed = Mathf.Max(walkSpeed, sprintSpeed);
        crouchSpeed = Mathf.Max(0.1f, crouchSpeed);
        jumpForce = Mathf.Max(0f, jumpForce);
    }
#endif
}
