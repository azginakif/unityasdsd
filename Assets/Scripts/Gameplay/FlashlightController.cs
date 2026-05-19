using UnityEngine;
using UnityEngine.InputSystem;
using MobilOfl.UI;

namespace MobilOfl.Gameplay
{
    public class FlashlightController : MonoBehaviour
    {
        [SerializeField] private Key toggleKey = Key.F;
        [SerializeField] private float maxBattery = 100f;
        [SerializeField] private float batteryDrainPerSecond = 2.8f;
        [SerializeField] private float batteryRechargePerSecond = 1.2f;
        [SerializeField] private float rechargeDelay = 3f;
        [SerializeField] private float lightIntensity = 1.8f;
        [SerializeField] private float lightRange = 18f;
        [SerializeField] private float lightSpotAngle = 55f;
        [SerializeField] private float flickerThreshold = 15f;
        [SerializeField] private Color lightColor = new Color(0.98f, 0.94f, 0.82f, 1f);
        [SerializeField] private MobileButton mobileFlashlightButton;

        private Light _spotLight;
        private float _battery;
        private float _lastUsedAt;
        private bool _isOn;
        private bool _created;
        private Camera _playerCamera;
        private float _flickerTimer;

        public bool IsOn => _isOn;
        public float Battery01 => Mathf.Clamp01(_battery / maxBattery);
        public float BatteryPercent => Mathf.RoundToInt(Battery01 * 100f);
        public bool IsFlickering => _isOn && _battery < flickerThreshold;

        private void Awake()
        {
            _battery = maxBattery;
            CreateLightIfNeeded();
        }

        private void Update()
        {
            ResolveCamera();
            CreateLightIfNeeded();

            if (MainMenuHud.IsBlockingGameplay || CaseNotebookHud.IsAnyNotebookOpen)
            {
                return;
            }

            var togglePressed =
                (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame) ||
                (mobileFlashlightButton != null && mobileFlashlightButton.ConsumeWasPressedThisFrame());

            if (togglePressed)
            {
                Toggle();
            }

            UpdateBattery();
            UpdateLight();
        }

        public void Toggle()
        {
            if (_battery <= 0.5f && !_isOn)
            {
                if (CaseSessionManager.Instance != null)
                {
                    CaseSessionManager.Instance.PublishMessage("Fener pili bitmis. Sarj olmasi icin biraz bekle.");
                }
                return;
            }

            _isOn = !_isOn;
            if (_isOn)
            {
                _lastUsedAt = Time.time;
            }
        }

        public void SetMobileButton(MobileButton button)
        {
            mobileFlashlightButton = button;
        }

        private void UpdateBattery()
        {
            if (_isOn)
            {
                _lastUsedAt = Time.time;
                _battery = Mathf.Max(0f, _battery - batteryDrainPerSecond * Time.deltaTime);

                if (_battery <= 0f)
                {
                    _isOn = false;
                    if (CaseSessionManager.Instance != null)
                    {
                        CaseSessionManager.Instance.PublishMessage("Fener pili tukendi!");
                    }
                }
            }
            else
            {
                if (Time.time > _lastUsedAt + rechargeDelay)
                {
                    _battery = Mathf.Min(maxBattery, _battery + batteryRechargePerSecond * Time.deltaTime);
                }
            }
        }

        private void UpdateLight()
        {
            if (_spotLight == null)
            {
                return;
            }

            _spotLight.enabled = _isOn;
            if (!_isOn)
            {
                return;
            }

            var intensityMultiplier = 1f;
            if (_battery < flickerThreshold)
            {
                _flickerTimer += Time.deltaTime * (12f + Random.Range(0f, 8f));
                var flicker = Mathf.PerlinNoise(_flickerTimer, 0f);
                intensityMultiplier = Mathf.Lerp(0.15f, 0.85f, flicker);

                if (_battery < flickerThreshold * 0.3f)
                {
                    intensityMultiplier *= Mathf.Lerp(0.3f, 1f, Mathf.PingPong(Time.time * 6f, 1f));
                }
            }

            _spotLight.intensity = lightIntensity * intensityMultiplier;
            _spotLight.range = lightRange * Mathf.Lerp(0.7f, 1f, Battery01);
        }

        private void CreateLightIfNeeded()
        {
            if (_created)
            {
                return;
            }

            _created = true;
            ResolveCamera();

            var lightHost = _playerCamera != null ? _playerCamera.transform : transform;
            var existingLight = lightHost.Find("Flashlight");
            if (existingLight != null)
            {
                _spotLight = existingLight.GetComponent<Light>();
                if (_spotLight != null)
                {
                    return;
                }
            }

            var lightObject = new GameObject("Flashlight");
            lightObject.transform.SetParent(lightHost, false);
            lightObject.transform.localPosition = new Vector3(0.15f, -0.08f, 0.1f);
            lightObject.transform.localRotation = Quaternion.identity;

            _spotLight = lightObject.AddComponent<Light>();
            _spotLight.type = LightType.Spot;
            _spotLight.color = lightColor;
            _spotLight.intensity = lightIntensity;
            _spotLight.range = lightRange;
            _spotLight.spotAngle = lightSpotAngle;
            _spotLight.innerSpotAngle = lightSpotAngle * 0.4f;
            _spotLight.shadows = LightShadows.Soft;
            _spotLight.shadowStrength = 0.65f;
            _spotLight.enabled = false;
        }

        private void ResolveCamera()
        {
            if (_playerCamera != null)
            {
                return;
            }

            _playerCamera = GetComponentInChildren<Camera>();
            if (_playerCamera == null)
            {
                _playerCamera = Camera.main;
            }
        }
    }
}
