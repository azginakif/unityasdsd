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
        [SerializeField] private float minimumInteractDistance = 5.25f;
        [SerializeField] private float aimAssistRadius = 0.46f;
        [SerializeField] private float nearbyButtonRadius = 2.35f;
        [SerializeField] private LayerMask interactMask = ~0;
        [SerializeField] private Key interactKey = Key.E;
        [SerializeField] private Key alternateInteractKey = Key.B;
        [SerializeField] private Key secondAlternateInteractKey = Key.F;
        [SerializeField] private MobileButton mobileInteractButton;

        private InteractableBase _currentInteractable;
        private InteractableBase _lastInteractable;
        private float _holdProgress;
        private float _lastTargetSeenAt = -999f;

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

            var interactPressed =
                IsKeyPressedThisFrame(interactKey) ||
                IsKeyPressedThisFrame(alternateInteractKey) ||
                IsKeyPressedThisFrame(secondAlternateInteractKey) ||
                (mobileInteractButton != null && mobileInteractButton.ConsumeWasPressedThisFrame());
            var interactHeld =
                IsKeyHeld(interactKey) ||
                IsKeyHeld(alternateInteractKey) ||
                IsKeyHeld(secondAlternateInteractKey) ||
                (mobileInteractButton != null && mobileInteractButton.IsPressed);

            UpdateCurrentInteractable(interactHeld, interactPressed);

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

        private void UpdateCurrentInteractable(bool interactHeld, bool interactPressed)
        {
            _currentInteractable = null;

            if (playerCamera == null)
            {
                return;
            }

            var ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (TryFindAimedInteractable(ray, out var aimedInteractable))
            {
                SetCurrentInteractable(aimedInteractable);
                return;
            }

            if (interactHeld &&
                _lastInteractable != null &&
                _lastInteractable.RequiresHold &&
                _lastInteractable.CanMaintainHold(gameObject))
            {
                _currentInteractable = _lastInteractable;
                return;
            }

            if (interactPressed && TryFindNearbyInteractable(out var nearbyInteractable))
            {
                SetCurrentInteractable(nearbyInteractable);
                return;
            }

            if (_lastInteractable != null)
            {
                ResetHoldState();
            }
        }

        private bool TryFindAimedInteractable(Ray ray, out InteractableBase interactable)
        {
            interactable = null;
            var distance = Mathf.Max(interactDistance, minimumInteractDistance);

            if (Physics.Raycast(ray, out var exactHit, distance, interactMask, QueryTriggerInteraction.Collide))
            {
                interactable = exactHit.collider.GetComponentInParent<InteractableBase>();
                if (interactable != null && interactable.isActiveAndEnabled)
                {
                    return true;
                }
            }

            var radius = Mathf.Max(0.05f, aimAssistRadius);
            var hits = Physics.SphereCastAll(ray, radius, distance, interactMask, QueryTriggerInteraction.Collide);
            var bestScore = float.PositiveInfinity;

            for (var i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null)
                {
                    continue;
                }

                var candidate = hit.collider.GetComponentInParent<InteractableBase>();
                if (candidate == null || !candidate.isActiveAndEnabled)
                {
                    continue;
                }

                var toHit = hit.point - ray.origin;
                var alignment = toHit.sqrMagnitude > 0.001f
                    ? Vector3.Dot(ray.direction, toHit.normalized)
                    : 1f;
                var score = hit.distance + (1f - Mathf.Clamp01(alignment)) * 1.35f;
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                interactable = candidate;
            }

            return interactable != null;
        }

        private bool TryFindNearbyInteractable(out InteractableBase interactable)
        {
            interactable = null;
            var center = transform.position + Vector3.up * 1.05f;
            var colliders = Physics.OverlapSphere(center, Mathf.Max(0.25f, nearbyButtonRadius), interactMask, QueryTriggerInteraction.Collide);
            var bestScore = float.PositiveInfinity;
            var cameraForward = playerCamera != null ? playerCamera.transform.forward : transform.forward;

            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null)
                {
                    continue;
                }

                var candidate = collider.GetComponentInParent<InteractableBase>();
                if (candidate == null || !candidate.isActiveAndEnabled)
                {
                    continue;
                }

                var closestPoint = collider.ClosestPoint(center);
                var offset = closestPoint - center;
                var distance = offset.magnitude;
                var facing = offset.sqrMagnitude > 0.001f
                    ? Vector3.Dot(cameraForward, offset.normalized)
                    : 1f;
                var score = distance + (1f - Mathf.Clamp01((facing + 1f) * 0.5f)) * 0.8f;
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                interactable = candidate;
            }

            return interactable != null;
        }

        private void SetCurrentInteractable(InteractableBase interactable)
        {
            _currentInteractable = interactable;
            _lastTargetSeenAt = Time.time;
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

        private static bool IsKeyPressedThisFrame(Key key)
        {
            return Keyboard.current != null && key != Key.None && Keyboard.current[key].wasPressedThisFrame;
        }

        private static bool IsKeyHeld(Key key)
        {
            return Keyboard.current != null && key != Key.None && Keyboard.current[key].isPressed;
        }
    }
}
