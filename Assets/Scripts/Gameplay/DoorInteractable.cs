using UnityEngine;

namespace MobilOfl.Gameplay
{
    public class DoorInteractable : InteractableBase
    {
        [SerializeField] private Transform doorTransform;
        [SerializeField] private string requiredToolId;
        [SerializeField] private string lockedMessage = "Bu kapi icin uygun erisim gerekiyor.";
        [SerializeField] private float openAngle = 88f;
        [SerializeField] private float turnSpeed = 8f;
        [SerializeField] private bool startsOpen;

        private Quaternion _closedRotation;
        private Quaternion _openRotation;
        private bool _isOpen;

        private void Awake()
        {
            if (doorTransform == null)
            {
                doorTransform = transform;
            }

            _closedRotation = doorTransform.localRotation;
            _openRotation = _closedRotation * Quaternion.Euler(0f, openAngle, 0f);
            _isOpen = startsOpen;
            doorTransform.localRotation = _isOpen ? _openRotation : _closedRotation;
        }

        private void Update()
        {
            if (doorTransform == null)
            {
                return;
            }

            var target = _isOpen ? _openRotation : _closedRotation;
            doorTransform.localRotation = Quaternion.Slerp(
                doorTransform.localRotation,
                target,
                1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        }

        public override bool TryInteract(GameObject interactor)
        {
            if (!HasAccess())
            {
                if (CaseSessionManager.Instance != null)
                {
                    CaseSessionManager.Instance.PublishMessage(string.IsNullOrWhiteSpace(lockedMessage)
                        ? "Bu kapi kilitli."
                        : lockedMessage);
                }

                return false;
            }

            _isOpen = !_isOpen;
            return true;
        }

        public void ConfigureAccess(string toolId, string message, bool openAtStart)
        {
            requiredToolId = string.IsNullOrWhiteSpace(toolId) ? string.Empty : toolId.Trim();
            if (!string.IsNullOrWhiteSpace(message))
            {
                lockedMessage = message.Trim();
            }

            startsOpen = openAtStart;
            _isOpen = openAtStart;
        }

        private bool HasAccess()
        {
            return string.IsNullOrWhiteSpace(requiredToolId) ||
                   CaseSessionManager.Instance == null ||
                   CaseSessionManager.Instance.HasTool(requiredToolId);
        }
    }
}
