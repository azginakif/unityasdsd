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
        [SerializeField] private bool openAwayFromInteractor = true;
        [SerializeField] private bool startsOpen;

        private Quaternion _closedRotation;
        private Quaternion _openRotation;
        private bool _isOpen;
        private Vector3 _doorCenterLocalToHinge;

        private void Awake()
        {
            if (doorTransform == null)
            {
                doorTransform = transform;
            }

            _closedRotation = doorTransform.localRotation;
            _openRotation = _closedRotation * Quaternion.Euler(0f, openAngle, 0f);
            _doorCenterLocalToHinge = CalculateDoorCenterLocalToHinge();
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

            if (!_isOpen && openAwayFromInteractor)
            {
                _openRotation = CalculateOpenRotation(interactor);
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

        private Quaternion CalculateOpenRotation(GameObject interactor)
        {
            if (doorTransform == null || interactor == null)
            {
                return _closedRotation * Quaternion.Euler(0f, openAngle, 0f);
            }

            var positiveRotation = _closedRotation * Quaternion.Euler(0f, Mathf.Abs(openAngle), 0f);
            var negativeRotation = _closedRotation * Quaternion.Euler(0f, -Mathf.Abs(openAngle), 0f);
            var positiveCenter = GetDoorCenterForRotation(positiveRotation);
            var negativeCenter = GetDoorCenterForRotation(negativeRotation);
            var interactorPosition = interactor.transform.position;
            var positiveDistance = (positiveCenter - interactorPosition).sqrMagnitude;
            var negativeDistance = (negativeCenter - interactorPosition).sqrMagnitude;

            return positiveDistance >= negativeDistance ? positiveRotation : negativeRotation;
        }

        private Vector3 CalculateDoorCenterLocalToHinge()
        {
            if (doorTransform == null)
            {
                return Vector3.zero;
            }

            var renderers = doorTransform.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return Vector3.forward * 0.5f;
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return doorTransform.InverseTransformPoint(bounds.center);
        }

        private Vector3 GetDoorCenterForRotation(Quaternion localRotation)
        {
            var parent = doorTransform.parent;
            if (parent == null)
            {
                return doorTransform.position + localRotation * _doorCenterLocalToHinge;
            }

            return parent.TransformPoint(doorTransform.localPosition + localRotation * _doorCenterLocalToHinge);
        }
    }
}
