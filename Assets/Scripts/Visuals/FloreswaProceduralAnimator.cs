using UnityEngine;

namespace MobilOfl.Visuals
{
    public sealed class FloreswaProceduralAnimator : MonoBehaviour
    {
        [SerializeField] private float runSpeed = 4.2f;
        [SerializeField] private float damping = 9f;
        [SerializeField] private float armDropDegrees = 10f;
        [SerializeField] private float armSwingDegrees = 7f;
        [SerializeField] private float legSwingDegrees = 4f;
        [SerializeField] private float idleBreathAmount = 0.018f;
        [SerializeField] private float moveBobAmount = 0.045f;
        [SerializeField] private float bodySwayDegrees = 2.5f;

        private Transform _upperArmLeft;
        private Transform _upperArmRight;
        private Transform _forearmLeft;
        private Transform _forearmRight;
        private Transform _thighLeft;
        private Transform _thighRight;
        private Transform _shinLeft;
        private Transform _shinRight;
        private Transform _head;
        private Quaternion _upperArmLeftBase;
        private Quaternion _upperArmRightBase;
        private Quaternion _forearmLeftBase;
        private Quaternion _forearmRightBase;
        private Quaternion _thighLeftBase;
        private Quaternion _thighRightBase;
        private Quaternion _shinLeftBase;
        private Quaternion _shinRightBase;
        private Quaternion _headBase;
        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation;
        private Vector3 _lastPosition;
        private float _speed01;
        private float _phase;
        private bool _ready;

        private void Awake()
        {
            CacheBones();
            _baseLocalPosition = transform.localPosition;
            _baseLocalRotation = transform.localRotation;
            _lastPosition = transform.position;
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void OnEnable()
        {
            _lastPosition = transform.position;
            _speed01 = 0f;
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void LateUpdate()
        {
            if (!_ready)
            {
                CacheBones();
                if (!_ready)
                {
                    return;
                }
            }

            var delta = transform.position - _lastPosition;
            delta.y = 0f;
            _lastPosition = transform.position;

            var speed = Time.deltaTime > 0.0001f ? delta.magnitude / Time.deltaTime : 0f;
            var targetSpeed = Mathf.Clamp01(speed / Mathf.Max(0.1f, runSpeed));
            _speed01 = Mathf.Lerp(_speed01, targetSpeed, 1f - Mathf.Exp(-damping * Time.deltaTime));
            _phase += Time.deltaTime * Mathf.Lerp(1.8f, 7.5f, _speed01);

            var walk = Mathf.Sin(_phase);
            var counterWalk = Mathf.Sin(_phase + Mathf.PI);
            var bob = Mathf.Sin(_phase) * Mathf.Lerp(idleBreathAmount, moveBobAmount, _speed01);
            var sway = Mathf.Sin(_phase * 0.5f) * bodySwayDegrees * _speed01;
            transform.localPosition = _baseLocalPosition + Vector3.up * bob;
            transform.localRotation = _baseLocalRotation * Quaternion.Euler(0f, sway, -sway * 0.35f);

            var armIdle = Mathf.Sin(_phase * 0.45f) * armDropDegrees * 0.18f * (1f - _speed01);
            ApplyRotation(_upperArmLeft, _upperArmLeftBase, counterWalk * armSwingDegrees * _speed01, 0f, armIdle);
            ApplyRotation(_upperArmRight, _upperArmRightBase, walk * armSwingDegrees * _speed01, 0f, -armIdle);
            ApplyRotation(_forearmLeft, _forearmLeftBase, counterWalk * armSwingDegrees * 0.35f * _speed01, 0f, 0f);
            ApplyRotation(_forearmRight, _forearmRightBase, walk * armSwingDegrees * 0.35f * _speed01, 0f, 0f);
            ApplyRotation(_thighLeft, _thighLeftBase, walk * legSwingDegrees * _speed01, 0f, 0f);
            ApplyRotation(_thighRight, _thighRightBase, counterWalk * legSwingDegrees * _speed01, 0f, 0f);
            ApplyRotation(_shinLeft, _shinLeftBase, Mathf.Max(0f, counterWalk) * legSwingDegrees * 0.35f * _speed01, 0f, 0f);
            ApplyRotation(_shinRight, _shinRightBase, Mathf.Max(0f, walk) * legSwingDegrees * 0.35f * _speed01, 0f, 0f);
            ApplyRotation(_head, _headBase, 0f, Mathf.Sin(_phase * 0.25f) * 1.2f * (1f - _speed01), 0f);
        }

        private void CacheBones()
        {
            _upperArmLeft = FindDeepChild(transform, "upper_arm.L");
            _upperArmRight = FindDeepChild(transform, "upper_arm.R");
            _forearmLeft = FindDeepChild(transform, "forearm.L");
            _forearmRight = FindDeepChild(transform, "forearm.R");
            _thighLeft = FindDeepChild(transform, "thigh.L");
            _thighRight = FindDeepChild(transform, "thigh.R");
            _shinLeft = FindDeepChild(transform, "shin.L");
            _shinRight = FindDeepChild(transform, "shin.R");
            _head = FindDeepChild(transform, "spine.006") ?? FindDeepChild(transform, "Head");

            _ready = _upperArmLeft != null &&
                _upperArmRight != null &&
                _thighLeft != null &&
                _thighRight != null;

            if (!_ready)
            {
                return;
            }

            _upperArmLeftBase = _upperArmLeft.localRotation;
            _upperArmRightBase = _upperArmRight.localRotation;
            _forearmLeftBase = _forearmLeft != null ? _forearmLeft.localRotation : Quaternion.identity;
            _forearmRightBase = _forearmRight != null ? _forearmRight.localRotation : Quaternion.identity;
            _thighLeftBase = _thighLeft.localRotation;
            _thighRightBase = _thighRight.localRotation;
            _shinLeftBase = _shinLeft != null ? _shinLeft.localRotation : Quaternion.identity;
            _shinRightBase = _shinRight != null ? _shinRight.localRotation : Quaternion.identity;
            _headBase = _head != null ? _head.localRotation : Quaternion.identity;
        }

        private static void ApplyRotation(Transform target, Quaternion baseRotation, float x, float y, float z)
        {
            if (target == null)
            {
                return;
            }

            target.localRotation = baseRotation * Quaternion.Euler(x, y, z);
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.name == childName)
            {
                return parent;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var result = FindDeepChild(parent.GetChild(i), childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
