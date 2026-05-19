using UnityEngine;
using UnityEngine.UI;

namespace MobilOfl.UI
{
    /// <summary>
    /// Full-screen overlay effects: vignette darkening at high alert,
    /// red damage flash, scan pulse, and evidence collection celebration.
    /// Attaches itself to its own ScreenSpaceOverlay canvas.
    /// </summary>
    public class ScreenEffectsController : MonoBehaviour
    {
        private RectTransform _root;
        private Image _vignetteImage;
        private Image _flashImage;
        private Image _scanPulseImage;
        private bool _built;
        private float _flashAlpha;
        private float _scanPulseAlpha;
        private float _scanPulseScale;
        private Gameplay.PlayerStealthController _stealth;

        private static ScreenEffectsController _instance;
        public static ScreenEffectsController Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void OnEnable() { BuildIfNeeded(); }

        private void Update()
        {
            BuildIfNeeded();
            if (_root == null) return;

            _root.gameObject.SetActive(!MainMenuHud.IsBlockingGameplay);
            ResolveStealth();
            UpdateVignette();
            UpdateFlash();
            UpdateScanPulse();
        }

        public static void TriggerDamageFlash()
        {
            if (_instance != null) _instance._flashAlpha = 0.42f;
        }

        public static void TriggerScanPulse()
        {
            if (_instance == null) return;
            _instance._scanPulseAlpha = 0.55f;
            _instance._scanPulseScale = 0.3f;
        }

        private void BuildIfNeeded()
        {
            if (_built) return;

            var canvasRoot = RuntimeUiFactory.CreateUiRoot("ScreenEffectsCanvas", transform);
            var canvas = canvasRoot.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasRoot.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            RuntimeUiFactory.Stretch(canvasRoot);
            _root = canvasRoot;

            // Vignette
            var vignetteRect = RuntimeUiFactory.CreateUiRoot("Vignette", _root);
            RuntimeUiFactory.Stretch(vignetteRect);
            _vignetteImage = vignetteRect.gameObject.AddComponent<Image>();
            _vignetteImage.color = new Color(0f, 0f, 0f, 0f);
            _vignetteImage.raycastTarget = false;

            // Flash
            var flashRect = RuntimeUiFactory.CreateUiRoot("Flash", _root);
            RuntimeUiFactory.Stretch(flashRect);
            _flashImage = flashRect.gameObject.AddComponent<Image>();
            _flashImage.color = new Color(0.9f, 0.15f, 0.1f, 0f);
            _flashImage.raycastTarget = false;

            // Scan pulse
            var scanRect = RuntimeUiFactory.CreateUiRoot("ScanPulse", _root);
            RuntimeUiFactory.Stretch(scanRect);
            _scanPulseImage = scanRect.gameObject.AddComponent<Image>();
            _scanPulseImage.color = new Color(0.2f, 0.85f, 1f, 0f);
            _scanPulseImage.raycastTarget = false;

            _built = true;
        }

        private void UpdateVignette()
        {
            var targetAlpha = 0f;
            if (_stealth != null)
            {
                targetAlpha = Mathf.Lerp(0f, 0.35f, _stealth.AlertLevel01);
            }

            var current = _vignetteImage.color;
            current.a = Mathf.Lerp(current.a, targetAlpha, Time.deltaTime * 3f);

            // Tint towards red at high alert
            if (_stealth != null && _stealth.AlertLevel01 > 0.6f)
            {
                var alertBlend = (_stealth.AlertLevel01 - 0.6f) / 0.4f;
                current.r = Mathf.Lerp(0f, 0.25f, alertBlend);
            }
            else
            {
                current.r = Mathf.Lerp(current.r, 0f, Time.deltaTime * 2f);
            }

            _vignetteImage.color = current;
        }

        private void UpdateFlash()
        {
            if (_flashAlpha > 0.001f)
            {
                _flashAlpha = Mathf.MoveTowards(_flashAlpha, 0f, Time.deltaTime * 1.8f);
            }
            var c = _flashImage.color;
            c.a = _flashAlpha;
            _flashImage.color = c;
        }

        private void UpdateScanPulse()
        {
            if (_scanPulseAlpha > 0.001f)
            {
                _scanPulseAlpha = Mathf.MoveTowards(_scanPulseAlpha, 0f, Time.deltaTime * 0.8f);
                _scanPulseScale = Mathf.MoveTowards(_scanPulseScale, 1.5f, Time.deltaTime * 2.2f);
                _scanPulseImage.transform.localScale = Vector3.one * _scanPulseScale;
            }
            var c = _scanPulseImage.color;
            c.a = _scanPulseAlpha;
            _scanPulseImage.color = c;
        }

        private void ResolveStealth()
        {
            if (_stealth == null || !_stealth.isActiveAndEnabled)
            {
                _stealth = Object.FindAnyObjectByType<Gameplay.PlayerStealthController>();
            }
        }
    }
}
