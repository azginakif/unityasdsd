using UnityEngine;

namespace MobilOfl.Visuals
{
    public class CharacterMovementAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private float runSpeed = 5.8f;
        [SerializeField] private float damping = 10f;
        [SerializeField] private bool proceduralFallback = true;
        [SerializeField] private float idleBobAmount = 0.018f;
        [SerializeField] private float moveBobAmount = 0.045f;
        [SerializeField] private float swayDegrees = 2.8f;

        private Vector3 _lastPosition;
        private Transform _visualRoot;
        private Vector3 _visualBaseLocalPosition;
        private Quaternion _visualBaseLocalRotation;
        private float _smoothedSpeed01;
        private float _phase;

        private void Awake()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            _visualRoot = animator != null ? animator.transform : (transform.childCount > 0 ? transform.GetChild(0) : transform);
            _visualBaseLocalPosition = _visualRoot.localPosition;
            _visualBaseLocalRotation = _visualRoot.localRotation;
            _lastPosition = transform.position;
        }

        private void OnEnable()
        {
            _lastPosition = transform.position;
            _smoothedSpeed01 = 0f;
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            var delta = transform.position - _lastPosition;
            delta.y = 0f;
            _lastPosition = transform.position;

            var speed = Time.deltaTime > 0.0001f ? delta.magnitude / Time.deltaTime : 0f;
            var targetSpeed01 = Mathf.Clamp01(speed / Mathf.Max(0.1f, runSpeed));
            _smoothedSpeed01 = Mathf.Lerp(_smoothedSpeed01, targetSpeed01, 1f - Mathf.Exp(-damping * Time.deltaTime));

            if (animator != null && animator.enabled && animator.runtimeAnimatorController != null)
            {
                animator.SetFloat(speedParameter, _smoothedSpeed01);
            }

            if (proceduralFallback)
            {
                ApplyProceduralFallback();
            }
        }

        private void ApplyProceduralFallback()
        {
            if (_visualRoot == null)
            {
                return;
            }

            _phase += Time.deltaTime * Mathf.Lerp(1.7f, 8.5f, _smoothedSpeed01);
            var bobAmount = Mathf.Lerp(idleBobAmount, moveBobAmount, _smoothedSpeed01);
            var bob = Mathf.Sin(_phase) * bobAmount;
            var sway = Mathf.Sin(_phase * 0.5f) * swayDegrees * _smoothedSpeed01;

            _visualRoot.localPosition = _visualBaseLocalPosition + Vector3.up * bob;
            _visualRoot.localRotation = _visualBaseLocalRotation * Quaternion.Euler(0f, sway, -sway * 0.35f);
        }
    }
}
