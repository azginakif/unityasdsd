using UnityEngine;

namespace MobilOfl.Visuals
{
    public class EvidenceVisualPulse : MonoBehaviour
    {
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color baseColor = new Color(0.2f, 0.9f, 1f);
        [SerializeField] private Color emissionColor = new Color(0.15f, 0.85f, 1f);
        [SerializeField] private float pulseSpeed = 2.6f;
        [SerializeField] private float pulseAmount = 0.08f;
        [SerializeField] private float rotationSpeed = 35f;
        [SerializeField] private float bobHeight = 0.06f;
        [SerializeField] private float bobSpeed = 1.8f;
        [SerializeField] private float proximityRange = 6f;
        [SerializeField] private float proximityGlowBoost = 1.8f;
        [SerializeField] private Light glowLight;

        private Vector3 _baseScale;
        private Vector3 _basePosition;
        private Material _runtimeMaterial;
        private float _proximityFactor;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            _baseScale = transform.localScale;
            _basePosition = transform.localPosition;

            if (targetRenderer != null)
            {
                _runtimeMaterial = targetRenderer.material;
                _runtimeMaterial.color = baseColor;

                if (_runtimeMaterial.HasProperty("_EmissionColor"))
                {
                    _runtimeMaterial.EnableKeyword("_EMISSION");
                }
            }
        }

        private void Update()
        {
            var pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            transform.localScale = _baseScale * (1f + pulse * pulseAmount);
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            // Vertical bob
            var bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = _basePosition + Vector3.up * bob;

            // Proximity glow
            UpdateProximityFactor();
            var proximityBoost = Mathf.Lerp(1f, proximityGlowBoost, _proximityFactor);
            var glow = Mathf.Lerp(0.45f, 1.25f, pulse) * proximityBoost;

            if (_runtimeMaterial != null && _runtimeMaterial.HasProperty("_EmissionColor"))
            {
                _runtimeMaterial.SetColor("_EmissionColor", emissionColor * glow);
            }

            if (glowLight != null)
            {
                glowLight.intensity = glow;
                glowLight.range = Mathf.Lerp(2f, 4f, _proximityFactor);
            }
        }

        private void UpdateProximityFactor()
        {
            var player = Camera.main;
            if (player == null)
            {
                _proximityFactor = 0f;
                return;
            }

            var distance = Vector3.Distance(transform.position, player.transform.position);
            _proximityFactor = Mathf.Clamp01(1f - (distance / proximityRange));
        }
    }
}
