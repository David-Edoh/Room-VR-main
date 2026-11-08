using UnityEngine;

namespace ColorRoomVR
{
    public class PaintVFXManager : MonoBehaviour
    {
        // Private serialized fields
        [Tooltip("Splash particle system prefab. Should not play on awake; its start color is overridden on each use.")]
        [SerializeField] private ParticleSystem splashPrefab;
        [Min(1)]
        [SerializeField] private int poolSize = 8;

        // Private fields
        private ParticleSystem[] _pool;
        private int _next;

        private void Awake()
        {
            if (splashPrefab == null) return;

            _pool = new ParticleSystem[Mathf.Max(1, poolSize)];
            for (int i = 0; i < _pool.Length; i++)
            {
                _pool[i] = Instantiate(splashPrefab, transform);
                _pool[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        /// <summary>Plays a splash at the impact point, oriented by the surface normal, tinted with the given color.</summary>
        public void PlayAt(Vector3 position, Vector3 normal, Color color)
        {
            if (_pool == null) return;

            // Circular queue: reuse the oldest splash instead of instantiating/destroying per stroke.
            var ps = _pool[_next];
            _next = (_next + 1) % _pool.Length;

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.transform.SetPositionAndRotation(position, Quaternion.LookRotation(normal));

            var main = ps.main;
            main.startColor = color;

            ps.Play(true);
        }
    }
}
