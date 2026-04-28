using UnityEngine;
using UnityEngine.EventSystems;

namespace MobilOfl.UI
{
    public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private bool _isPressed;
        private bool _wasPressedThisFrame;

        public bool IsPressed => _isPressed;

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
