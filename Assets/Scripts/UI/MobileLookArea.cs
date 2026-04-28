using UnityEngine;
using UnityEngine.EventSystems;

namespace MobilOfl.UI
{
    public class MobileLookArea : MonoBehaviour, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private float sensitivity = 1f;
        [SerializeField] private float maxDeltaPerEvent = 80f;

        private Vector2 _lookDelta;
        private int _activePointerId = int.MinValue;

        public Vector2 ConsumeLookDelta()
        {
            var delta = _lookDelta;
            _lookDelta = Vector2.zero;
            return delta;
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
