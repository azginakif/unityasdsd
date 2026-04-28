using UnityEngine;
using UnityEngine.InputSystem;
using MobilOfl.UI;

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

        public InteractableBase CurrentInteractable => _currentInteractable;

        private void Update()
        {
            if (MainMenuHud.IsBlockingGameplay || (CaseSessionManager.Instance != null && CaseSessionManager.Instance.IsCaseResolved))
            {
                _currentInteractable = null;
                return;
            }

            if (CaseNotebookHud.IsAnyNotebookOpen)
            {
                _currentInteractable = null;
                return;
            }

            UpdateCurrentInteractable();

            var interactPressed =
                (Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame) ||
                (mobileInteractButton != null && mobileInteractButton.ConsumeWasPressedThisFrame());

            if (_currentInteractable == null)
            {
                return;
            }

            if (interactPressed)
            {
                _currentInteractable.TryInteract(gameObject);
            }
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
                return;
            }

            _currentInteractable = hit.collider.GetComponentInParent<InteractableBase>();
        }
    }
}
