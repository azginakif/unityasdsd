using UnityEngine;
using UnityEngine.InputSystem;
using MobilOfl.UI;

namespace MobilOfl.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
    public class PrototypeFirstPersonController : MonoBehaviour
    {
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float walkSpeed = 4.5f;
        [SerializeField] private float sprintSpeed = 6.5f;
        [SerializeField] private float jumpHeight = 1.1f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private float lookSmoothing = 18f;
        [SerializeField] private float maxLookDelta = 42f;
        [SerializeField] private float maxLookAngle = 80f;
        [SerializeField] private MobileJoystick mobileMoveJoystick;
        [SerializeField] private MobileLookArea mobileLookArea;
        [SerializeField] private MobileButton mobileSprintButton;
        [SerializeField] private MobileButton mobileJumpButton;

        private CharacterController _characterController;
        private float _verticalVelocity;
        private float _pitch;
        private Vector2 _smoothedLookDelta;
        private Vector2 _pendingLookDelta;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (MainMenuHud.IsBlockingGameplay || (CaseSessionManager.Instance != null && CaseSessionManager.Instance.IsCaseResolved))
            {
                return;
            }

            if (CaseNotebookHud.IsAnyNotebookOpen)
            {
                return;
            }

            UpdateMovement();
        }

        private void LateUpdate()
        {
            if (MainMenuHud.IsBlockingGameplay || (CaseSessionManager.Instance != null && CaseSessionManager.Instance.IsCaseResolved))
            {
                return;
            }

            if (cameraPivot == null)
            {
                return;
            }

            UpdateLook();
        }

        private void UpdateLook()
        {
            if (CaseNotebookHud.IsAnyNotebookOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                _pendingLookDelta = Vector2.zero;
                _smoothedLookDelta = Vector2.zero;
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var lookInput = ReadLookInput();
            if (mobileLookArea != null)
            {
                lookInput += mobileLookArea.ConsumeLookDelta();
            }

            lookInput = Vector2.ClampMagnitude(lookInput, maxLookDelta);
            _pendingLookDelta = lookInput * (lookSensitivity * 0.05f);

            var smoothing = 1f - Mathf.Exp(-lookSmoothing * Time.unscaledDeltaTime);
            _smoothedLookDelta = Vector2.Lerp(_smoothedLookDelta, _pendingLookDelta, smoothing);

            var mouseX = _smoothedLookDelta.x;
            var mouseY = _smoothedLookDelta.y;

            transform.Rotate(Vector3.up * mouseX);

            _pitch -= mouseY;
            _pitch = Mathf.Clamp(_pitch, -maxLookAngle, maxLookAngle);
            cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void UpdateMovement()
        {
            var planarInput = ReadMoveInput();
            if (mobileMoveJoystick != null)
            {
                planarInput += mobileMoveJoystick.Value;
            }

            planarInput = Vector2.ClampMagnitude(planarInput, 1f);

            var moveInput = new Vector3(planarInput.x, 0f, planarInput.y);
            moveInput = Vector3.ClampMagnitude(moveInput, 1f);

            var isSprinting =
                (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed) ||
                (mobileSprintButton != null && mobileSprintButton.IsPressed);
            var speed = isSprinting ? sprintSpeed : walkSpeed;
            var move = transform.TransformDirection(moveInput) * speed;
            var jumpPressed =
                (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                (mobileJumpButton != null && mobileJumpButton.ConsumeWasPressedThisFrame());

            if (_characterController.isGrounded)
            {
                if (_verticalVelocity < 0f)
                {
                    _verticalVelocity = -2f;
                }

                if (jumpPressed)
                {
                    _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }

            _verticalVelocity += gravity * Time.deltaTime;
            move.y = _verticalVelocity;

            _characterController.Move(move * Time.deltaTime);
        }

        private static Vector2 ReadLookInput()
        {
            if (Mouse.current != null)
            {
                return Mouse.current.delta.ReadValue();
            }

            if (Gamepad.current != null)
            {
                return Gamepad.current.rightStick.ReadValue() * 15f;
            }

            return Vector2.zero;
        }

        private static Vector2 ReadMoveInput()
        {
            var move = Vector2.zero;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed)
                {
                    move.x -= 1f;
                }

                if (Keyboard.current.dKey.isPressed)
                {
                    move.x += 1f;
                }

                if (Keyboard.current.sKey.isPressed)
                {
                    move.y -= 1f;
                }

                if (Keyboard.current.wKey.isPressed)
                {
                    move.y += 1f;
                }
            }

            if (Gamepad.current != null)
            {
                move += Gamepad.current.leftStick.ReadValue();
            }

            return Vector2.ClampMagnitude(move, 1f);
        }
    }
}
