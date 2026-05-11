using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public sealed class FirstPersonTestController : MonoBehaviour
{
    private static readonly Vector3 DefaultCameraLocalPosition = new Vector3(0f, 1.6f, 0f);

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 7.5f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -25f;

    [Header("Look")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private bool lockCursorOnPlay = true;

    private CharacterController characterController;
    private float verticalVelocity;
    private float pitch;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        EnsureCameraPivot();
        SyncPitchWithCameraPivot();
    }

    private void Start()
    {
        if (lockCursorOnPlay)
        {
            LockCursor(true);
        }
    }

    private void Update()
    {
        HandleCursorState();
        HandleLook();
        HandleMovement();
    }

    private void HandleCursorState()
    {
        if (IsEscapePressed())
        {
            LockCursor(false);
        }

        if (IsPrimaryMousePressed())
        {
            LockCursor(true);
        }
    }

    private void HandleLook()
    {
        if (cameraPivot == null || Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Vector2 lookInput = ReadLookInput() * mouseSensitivity;
        pitch = Mathf.Clamp(pitch - lookInput.y, minPitch, maxPitch);

        transform.Rotate(Vector3.up * lookInput.x);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        Vector2 moveInput = ReadMoveInput();
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        float speed = IsSprintHeld() ? sprintSpeed : walkSpeed;

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        if (characterController.isGrounded && IsJumpPressed())
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = move * speed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private void LockCursor(bool shouldLock)
    {
        Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !shouldLock;
    }

    public void SetCameraPivot(Transform pivot)
    {
        cameraPivot = pivot;
        SyncPitchWithCameraPivot();
    }

    private void EnsureCameraPivot()
    {
        if (cameraPivot != null)
        {
            return;
        }

        Camera childCamera = GetComponentInChildren<Camera>(true);

        if (childCamera != null)
        {
            cameraPivot = childCamera.transform;
            return;
        }

        Transform reusableCameraPivot = TryReuseSceneCamera();

        if (reusableCameraPivot != null)
        {
            cameraPivot = reusableCameraPivot;
            return;
        }

        cameraPivot = CreateFallbackCameraPivot();
    }

    private Transform TryReuseSceneCamera()
    {
        Camera[] sceneCameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Camera sceneCamera in sceneCameras)
        {
            if (sceneCamera != null && sceneCamera.gameObject.name == "Player Camera")
            {
                return AdoptSceneCamera(sceneCamera);
            }
        }

        return Camera.main != null ? AdoptSceneCamera(Camera.main) : null;
    }

    private Transform AdoptSceneCamera(Camera sceneCamera)
    {
        sceneCamera.transform.SetParent(transform, false);
        sceneCamera.transform.localPosition = DefaultCameraLocalPosition;
        sceneCamera.transform.localRotation = Quaternion.identity;
        sceneCamera.nearClipPlane = 0.03f;

        return sceneCamera.transform;
    }

    private Transform CreateFallbackCameraPivot()
    {
        GameObject cameraObject = new GameObject("Player Camera");
        cameraObject.transform.SetParent(transform, false);
        cameraObject.transform.localPosition = DefaultCameraLocalPosition;
        cameraObject.tag = "MainCamera";

        Camera fallbackCamera = cameraObject.AddComponent<Camera>();
        fallbackCamera.nearClipPlane = 0.03f;
        cameraObject.AddComponent<AudioListener>();

        return cameraObject.transform;
    }

    private void SyncPitchWithCameraPivot()
    {
        if (cameraPivot == null)
        {
            return;
        }

        pitch = NormalizePitch(cameraPivot.localEulerAngles.x);
    }

    private static float NormalizePitch(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    private Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        Vector2 moveInput = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed)
            {
                moveInput.x -= 1f;
            }

            if (Keyboard.current.dKey.isPressed)
            {
                moveInput.x += 1f;
            }

            if (Keyboard.current.sKey.isPressed)
            {
                moveInput.y -= 1f;
            }

            if (Keyboard.current.wKey.isPressed)
            {
                moveInput.y += 1f;
            }
        }

        return Vector2.ClampMagnitude(moveInput, 1f);
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
#else
        return Vector2.zero;
#endif
    }

    private Vector2 ReadLookInput()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
#else
        return Vector2.zero;
#endif
    }

    private bool IsJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetButtonDown("Jump");
#else
        return false;
#endif
    }

    private bool IsSprintHeld()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift);
#else
        return false;
#endif
    }

    private bool IsPrimaryMousePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private bool IsEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }
}
