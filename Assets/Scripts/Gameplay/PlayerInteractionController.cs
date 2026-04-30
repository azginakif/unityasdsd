using UnityEngine;
using UnityEngine.InputSystem;
using MobilOfl.UI;
using MobilOfl.Online;

namespace MobilOfl.Gameplay
{
    public class PlayerInteractionController : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float interactDistance = 3f;
        [SerializeField] private LayerMask interactMask = ~0;
        [SerializeField] private Key interactKey = Key.E;
        [SerializeField] private MobileButton mobileInteractButton;

        private InteractableBase _currentInteractable;
        private InteractableBase _lastInteractable;
        private float _holdProgress;

        public InteractableBase CurrentInteractable => _currentInteractable;
        public Camera PlayerCamera => playerCamera;
        public float HoldProgress01 => _currentInteractable != null && _currentInteractable.RequiresHold ? Mathf.Clamp01(_holdProgress) : 0f;
        public bool IsHoldingInteract => HoldProgress01 > 0f;

        private void Update()
        {
            if (MainMenuHud.IsBlockingGameplay || (CaseSessionManager.Instance != null && CaseSessionManager.Instance.IsCaseResolved))
            {
                _currentInteractable = null;
                ResetHoldState();
                return;
            }

            if (NetworkCaseState.Instance != null &&
                NetworkCaseState.Instance.IsOnlineSessionActive &&
                !NetworkCaseState.Instance.IsGameplayPhase)
            {
                _currentInteractable = null;
                ResetHoldState();
                return;
            }

            if (CaseNotebookHud.IsAnyNotebookOpen)
            {
                _currentInteractable = null;
                ResetHoldState();
                return;
            }

            UpdateCurrentInteractable();

            var interactPressed =
                (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame) ||
                (mobileInteractButton != null && mobileInteractButton.ConsumeWasPressedThisFrame());
            var interactHeld =
                (Keyboard.current != null && Keyboard.current[interactKey].isPressed) ||
                (mobileInteractButton != null && mobileInteractButton.IsPressed);

            if (_currentInteractable == null)
            {
                ResetHoldState();
                return;
            }

            if (!_currentInteractable.RequiresHold)
            {
                if (interactPressed)
                {
                    _currentInteractable.TryInteract(gameObject);
                }

                ResetHoldState();
                return;
            }

            if (!interactHeld || !_currentInteractable.CanMaintainHold(gameObject))
            {
                if (!interactHeld)
                {
                    _holdProgress = 0f;
                }

                return;
            }

            var duration = Mathf.Max(0.05f, _currentInteractable.HoldDuration);
            _holdProgress += Time.deltaTime / duration;
            if (_holdProgress < 1f)
            {
                return;
            }

            _currentInteractable.TryInteract(gameObject);
            ResetHoldState();
        }

        public bool TryInteractFromUi()
        {
            return _currentInteractable != null && _currentInteractable.TryInteract(gameObject);
        }

        private void UpdateCurrentInteractable()
        {
            _currentInteractable = null;

            if (playerCamera == null)
            {
                return;
            }

            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (!Physics.Raycast(ray, out var hit, interactDistance, interactMask, QueryTriggerInteraction.Collide))
            {
                if (_lastInteractable != null)
                {
                    ResetHoldState();
                }

                return;
            }

            _currentInteractable = hit.collider.GetComponentInParent<InteractableBase>();
            if (_currentInteractable != _lastInteractable)
            {
                ResetHoldState();
                _lastInteractable = _currentInteractable;
            }
        }

        private void ResetHoldState()
        {
            _holdProgress = 0f;
            _lastInteractable = _currentInteractable;
        }
    }
}
