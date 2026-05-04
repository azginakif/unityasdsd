using UnityEngine;

namespace MobilOfl.Visuals
{
    public class PlaceholderCharacterVisual : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private Transform body;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private float idleBobAmount = 0.035f;
        [SerializeField] private float idleBobSpeed = 1.8f;
        [SerializeField] private float armSwingAmount = 7f;
        [SerializeField] private float lookTurnSpeed = 5f;

        private Vector3 _headBaseLocalPosition;
        private Vector3 _bodyBaseLocalPosition;

        private void Awake()
        {
            if (body == null)
            {
                body = transform;
            }

            if (head != null)
            {
                _headBaseLocalPosition = head.localPosition;
            }

            _bodyBaseLocalPosition = body.localPosition;
        }

        private void Update()
        {
            var pulse = Mathf.Sin(Time.time * idleBobSpeed);

            if (body != null)
            {
                body.localPosition = _bodyBaseLocalPosition + Vector3.up * (pulse * idleBobAmount);
            }

            if (head != null)
            {
                head.localPosition = _headBaseLocalPosition + Vector3.up * (pulse * idleBobAmount * 0.55f);
                var camera = Camera.main;
                if (camera != null)
                {
                    var toCamera = camera.transform.position - head.position;
                    toCamera.y = 0f;
                    if (toCamera.sqrMagnitude > 0.01f)
                    {
                        var targetRotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
                        head.rotation = Quaternion.Slerp(head.rotation, targetRotation, 1f - Mathf.Exp(-lookTurnSpeed * Time.deltaTime));
                    }
                }
            }

            var armSwing = Mathf.Sin(Time.time * idleBobSpeed * 1.35f) * armSwingAmount;
            if (leftArm != null)
            {
                leftArm.localRotation = Quaternion.Euler(armSwing, 0f, -8f);
            }

            if (rightArm != null)
            {
                rightArm.localRotation = Quaternion.Euler(-armSwing, 0f, 8f);
            }
        }
    }
}
