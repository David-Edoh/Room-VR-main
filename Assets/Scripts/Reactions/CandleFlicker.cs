using UnityEngine;

namespace ColorRoomVR
{
    public class CandleFlicker : MonoBehaviour
    {
        // Private serialized fields
        [SerializeField] private PaintableObject target;
        [SerializeField] private Light flameLight;
        [SerializeField] private float baseIntensity = 1.5f;
        [SerializeField] private float amplitude = 0.4f;
        [SerializeField] private float speed = 6f;

        private bool isFlickering;
        private float noiseOffset;

        private void Awake()
        {
            noiseOffset = Random.value * 100f;
        }

        private void OnEnable()
        {
            target.OnPainted.AddListener(OnPainted);
        }

        private void OnDisable()
        {
            target.OnPainted.RemoveListener(OnPainted);
        }

        private void OnPainted()
        {
            isFlickering = true;
        }

        private void Update()
        {
            if (!isFlickering) return;

            float noise = Mathf.PerlinNoise(noiseOffset, Time.time * speed) * 2f - 1f;
            flameLight.intensity = baseIntensity + noise * amplitude;
        }
    }
}
