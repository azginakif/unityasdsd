using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MobilOfl.UI
{
    public class MobileLookArea : MonoBehaviour, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private float sensitivity = 1f;
        [SerializeField] private float maxDeltaPerEvent = 80f;
        [SerializeField] private Graphic overlayGraphic;
        [SerializeField] private float idleOverlayAlpha = 0.015f;
        [SerializeField] private float activeOverlayAlpha = 0.08f;
        [SerializeField] private float feedbackFadeSpeed = 12f;
        [SerializeField] private float activeHoldDuration = 0.18f;

        private Vector2 _lookDelta;
        private int _activePointerId = int.MinValue;
        private float _lastDragTime;

        public Vector2 ConsumeLookDelta()
        {
            var delta = _lookDelta;
            _lookDelta = Vector2.zero;
            return delta;
        }

        private void Awake()
        {
            if (overlayGraphic == null)
            {
                overlayGraphic = GetComponent<Graphic>();
            }
        }

        private void Update()
        {
            if (overlayGraphic == null)
            {
                return;
            }

            var targetAlpha = Time.unscaledTime - _lastDragTime <= activeHoldDuration
                ? activeOverlayAlpha
                : idleOverlayAlpha;
            var color = overlayGraphic.color;
            color.a = Mathf.Lerp(
                color.a,
                targetAlpha,
                1f - Mathf.Exp(-feedbackFadeSpeed * Time.unscaledDeltaTime));
            overlayGraphic.color = color;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_activePointerId == int.MinValue)
            {
                _activePointerId = eventData.pointerId;
            }

            if (_activePointerId != eventData.pointerId)
            {
                return;
            }

            var clampedDelta = Vector2.ClampMagnitude(eventData.delta, maxDeltaPerEvent);
            _lookDelta += clampedDelta * sensitivity;
            _lastDragTime = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_activePointerId != eventData.pointerId)
            {
                return;
            }

            _activePointerId = int.MinValue;
            _lookDelta = Vector2.zero;
        }
    }
}
