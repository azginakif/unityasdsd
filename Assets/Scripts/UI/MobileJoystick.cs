using UnityEngine;
using UnityEngine.EventSystems;

namespace MobilOfl.UI
{
    public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField] private float handleRange = 72f;

        private Vector2 _value;

        public Vector2 Value => _value;

        private void Awake()
        {
            if (background == null)
            {
                background = transform as RectTransform;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateValue(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateValue(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _value = Vector2.zero;

            if (handle != null)
            {
                handle.anchoredPosition = Vector2.zero;
            }
        }

        private void UpdateValue(PointerEventData eventData)
        {
            if (background == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                background,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);

            _value = Vector2.ClampMagnitude(localPoint / handleRange, 1f);

            if (handle != null)
            {
                handle.anchoredPosition = _value * handleRange;
            }
        }
    }
}
