using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MobilOfl.UI
{
    [RequireComponent(typeof(Image))]
    public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform handle;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image handleImage;
        [SerializeField] private Image pulseRingImage;
        [SerializeField] private float handleRange = 72f;
        [SerializeField] private float visualLerpSpeed = 10f;

        private Vector2 _value;
        private bool _isActive;

        public Vector2 Value => _value;

        private void Awake()
        {
            if (background == null)
            {
                background = transform as RectTransform;
            }

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (handleImage == null && handle != null)
            {
                handleImage = handle.GetComponent<Image>();
            }
        }

        private void Update()
        {
            var magnitude = Mathf.Clamp01(_value.magnitude);
            var active = _isActive || magnitude > 0.01f;
            var t = 1f - Mathf.Exp(-visualLerpSpeed * Time.unscaledDeltaTime);

            if (backgroundImage != null)
            {
                var targetColor = active
                    ? new Color(0.22f, 0.48f, 0.5f, 0.62f)
                    : new Color(0.12f, 0.18f, 0.22f, 0.45f);
                backgroundImage.color = Color.Lerp(backgroundImage.color, targetColor, t);
            }

            if (handleImage != null)
            {
                var targetColor = active
                    ? new Color(0.96f, 0.98f, 1f, 0.98f)
                    : new Color(0.9f, 0.95f, 1f, 0.82f);
                handleImage.color = Color.Lerp(handleImage.color, targetColor, t);
            }

            if (handle != null)
            {
                var targetScale = 1f + (magnitude * 0.12f);
                handle.localScale = Vector3.Lerp(handle.localScale, Vector3.one * targetScale, t);
            }

            if (pulseRingImage != null)
            {
                var ringTransform = pulseRingImage.rectTransform;
                var targetScale = active ? 1.08f + (magnitude * 0.16f) : 0.96f;
                ringTransform.localScale = Vector3.Lerp(ringTransform.localScale, Vector3.one * targetScale, t);

                var ringColor = pulseRingImage.color;
                ringColor.a = Mathf.Lerp(ringColor.a, active ? 0.42f : 0.18f, t);
                pulseRingImage.color = ringColor;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isActive = true;
            UpdateValue(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdateValue(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isActive = false;
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
