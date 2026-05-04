using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MobilOfl.UI
{
    [RequireComponent(typeof(Image))]
    public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private Image background;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private Text label;
        [SerializeField] private Color idleColor = new Color(0.18f, 0.22f, 0.26f, 0.84f);
        [SerializeField] private Color pressedColor = new Color(0.32f, 0.78f, 0.74f, 0.96f);
        [SerializeField] private float visualLerpSpeed = 14f;
        [SerializeField] private float pressedScale = 0.92f;

        private bool _isPressed;
        private bool _wasPressedThisFrame;

        public bool IsPressed => _isPressed;

        public void ConfigureMobileVisuals(Color idle, Color pressed)
        {
            idleColor = idle;
            pressedColor = pressed;
            if (background != null)
            {
                background.color = idleColor;
            }
        }

        private void Awake()
        {
            if (background == null)
            {
                background = GetComponent<Image>();
            }

            if (contentRoot == null)
            {
                contentRoot = transform as RectTransform;
            }

            if (label == null)
            {
                label = GetComponentInChildren<Text>();
            }
        }

        private void Update()
        {
            var targetColor = _isPressed ? pressedColor : idleColor;
            var targetScale = _isPressed ? pressedScale : 1f;

            if (background != null)
            {
                background.color = Color.Lerp(
                    background.color,
                    targetColor,
                    1f - Mathf.Exp(-visualLerpSpeed * Time.unscaledDeltaTime));
            }

            if (contentRoot != null)
            {
                var scale = Mathf.Lerp(
                    contentRoot.localScale.x,
                    targetScale,
                    1f - Mathf.Exp(-visualLerpSpeed * Time.unscaledDeltaTime));
                contentRoot.localScale = new Vector3(scale, scale, 1f);
            }

            if (label != null)
            {
                label.color = Color.Lerp(
                    label.color,
                    _isPressed ? Color.white : new Color(0.97f, 0.95f, 0.9f, 1f),
                    1f - Mathf.Exp(-visualLerpSpeed * Time.unscaledDeltaTime));
            }
        }

        public bool ConsumeWasPressedThisFrame()
        {
            var wasPressed = _wasPressedThisFrame;
            _wasPressedThisFrame = false;
            return wasPressed;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_isPressed)
            {
                _wasPressedThisFrame = true;
            }

            _isPressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
        }
    }
}
