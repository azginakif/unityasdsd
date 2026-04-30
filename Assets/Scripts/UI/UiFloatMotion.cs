using UnityEngine;
using UnityEngine.UI;

namespace MobilOfl.UI
{
    public class UiFloatMotion : MonoBehaviour
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private Vector2 motionAmplitude = new Vector2(0f, 8f);
        [SerializeField] private float motionSpeed = 1.2f;
        [SerializeField] private float alphaPulse = 0.08f;
        [SerializeField] private float scalePulse = 0.025f;
        [SerializeField] private float phaseOffset;

        private Vector2 _basePosition;
        private Vector3 _baseScale;
        private Color _baseColor;
        private bool _initialized;

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
        }

        public void Configure(RectTransform newTarget, Graphic newGraphic, Vector2 amplitude, float speed, float alpha, float scale, float phase)
        {
            target = newTarget;
            targetGraphic = newGraphic;
            motionAmplitude = amplitude;
            motionSpeed = speed;
            alphaPulse = alpha;
            scalePulse = scale;
            phaseOffset = phase;
            _initialized = false;
            Initialize();
        }

        private void Update()
        {
            Initialize();
            if (target == null)
            {
                return;
            }

            var wave = Mathf.Sin((Time.unscaledTime * motionSpeed) + phaseOffset);
            target.anchoredPosition = _basePosition + (motionAmplitude * wave);
            target.localScale = _baseScale * (1f + (wave * scalePulse));

            if (targetGraphic != null)
            {
                var color = _baseColor;
                color.a = Mathf.Clamp01(_baseColor.a + (wave * alphaPulse));
                targetGraphic.color = color;
            }
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            if (target == null)
            {
                target = transform as RectTransform;
            }

            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }

            if (target != null)
            {
                _basePosition = target.anchoredPosition;
                _baseScale = target.localScale;
            }

            if (targetGraphic != null)
            {
                _baseColor = targetGraphic.color;
            }

            _initialized = true;
        }
    }
}
