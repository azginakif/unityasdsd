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
        [SerializeField] private Light glowLight;

        private Vector3 _baseScale;
        private Material _runtimeMaterial;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            _baseScale = transform.localScale;

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

            var glow = Mathf.Lerp(0.45f, 1.25f, pulse);

            if (_runtimeMaterial != null && _runtimeMaterial.HasProperty("_EmissionColor"))
            {
                _runtimeMaterial.SetColor("_EmissionColor", emissionColor * glow);
            }

            if (glowLight != null)
            {
                glowLight.intensity = glow;
            }
        }
    }
}
