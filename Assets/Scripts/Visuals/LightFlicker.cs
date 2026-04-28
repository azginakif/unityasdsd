using UnityEngine;

namespace MobilOfl.Visuals
{
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField] private Light targetLight;
        [SerializeField] private float baseIntensity = 1.4f;
        [SerializeField] private float flickerAmount = 0.25f;
        [SerializeField] private float flickerSpeed = 7f;

        private float _seed;

        private void Awake()
        {
            if (targetLight == null)
            {
                targetLight = GetComponent<Light>();
            }

            _seed = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (targetLight == null)
            {
                return;
            }

            var noise = Mathf.PerlinNoise(_seed, Time.time * flickerSpeed);
            targetLight.intensity = baseIntensity + (noise - 0.5f) * flickerAmount;
        }
    }
}
