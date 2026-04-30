using UnityEngine;

namespace MobilOfl.Gameplay
{
    public class HideSpotInteractable : InteractableBase
    {
        [SerializeField] private float holdDuration = 1.25f;
        [SerializeField] private float useDistance = 3.2f;
        [SerializeField] [TextArea] private string successMessage = "Biraz bekleyip nefesini topladin. Dikkat seviyesi dustu.";
        [SerializeField] private float targetAlertLevel = 0.12f;
        [SerializeField] private float targetNoiseLevel = 0.03f;
        [SerializeField] private float staminaRestoreAmount = 0.42f;

        public override bool RequiresHold => true;
        public override float HoldDuration => holdDuration;

        public override bool CanMaintainHold(GameObject interactor)
        {
            if (interactor == null)
            {
                return false;
            }

            return Vector3.Distance(interactor.transform.position, transform.position) <= useDistance;
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
    }
}
