using UnityEngine;

namespace MobilOfl.Gameplay
{
    public class HideSpotInteractable : InteractableBase
    {
        [SerializeField] private float holdDuration = 0.95f;
        [SerializeField] private float useDistance = 4.6f;
        [SerializeField] [TextArea] private string successMessage = "Biraz bekleyip nefesini topladin. Dikkat seviyesi dustu.";
        [SerializeField] private float targetAlertLevel = 0.12f;
        [SerializeField] private float targetNoiseLevel = 0.03f;
        [SerializeField] private float staminaRestoreAmount = 0.42f;

        public override bool RequiresHold => true;
        public override float HoldDuration => Mathf.Clamp(holdDuration, 0.35f, 0.95f);

        public override bool CanShowInteractionPrompt(GameObject interactor)
        {
            return isActiveAndEnabled && interactor != null && GetDistanceToInteractor(interactor) <= Mathf.Max(1f, useDistance, 4.6f);
        }

        public override bool CanMaintainHold(GameObject interactor)
        {
            if (interactor == null)
            {
                return false;
            }

            return GetDistanceToInteractor(interactor) <= Mathf.Max(1f, useDistance, 4.6f);
        }

        public override bool TryInteract(GameObject interactor)
        {
            if (interactor == null)
            {
                return false;
            }

            var stealth = interactor.GetComponent<PlayerStealthController>();
            var movement = interactor.GetComponent<PrototypeFirstPersonController>();
            if (stealth == null && movement == null)
            {
                return false;
            }

            stealth?.ApplyCalmRecovery(targetAlertLevel, targetNoiseLevel);
            movement?.RestoreSprintStamina(staminaRestoreAmount);

            if (CaseSessionManager.Instance != null)
            {
                CaseSessionManager.Instance.PublishMessage(successMessage);
            }

            return true;
        }

        private float GetDistanceToInteractor(GameObject interactor)
        {
            var interactorPosition = interactor.transform.position + Vector3.up * 1.05f;
            var colliders = GetComponentsInChildren<Collider>(false);
            var closestDistance = float.PositiveInfinity;

            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i];
                if (collider == null || !collider.enabled)
                {
                    continue;
                }

                var closestPoint = collider.ClosestPoint(interactorPosition);
                closestDistance = Mathf.Min(closestDistance, Vector3.Distance(interactorPosition, closestPoint));
            }

            return float.IsPositiveInfinity(closestDistance)
                ? Vector3.Distance(interactor.transform.position, transform.position)
                : closestDistance;
        }
    }
}
