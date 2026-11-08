using UnityEngine;

namespace ColorRoomVR
{
    /// <summary>
    /// Central place for the game's sound effects. Every clip slot is optional: with an empty slot
    /// the matching Play method does nothing (no exceptions, no warnings).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // Static variables and properties
        public static AudioManager Instance { get; private set; }

        // Private serialized fields
        [Tooltip("Paint sound. Use pitch for variants.")]
        [SerializeField] private AudioClip paint;
        [Tooltip("Random pitch offset (+/-) applied to paint sounds.")]
        [SerializeField, Range(0f, 0.3f)] private float pitchVariation = 0.1f;
        [SerializeField] private AudioClip uiClick;
        [SerializeField] private AudioClip roomComplete;

        // Private fields
        private const int PaintSourceCount = 4;
        private AudioSource _sfxSource;
        private AudioSource[] _paintSources;
        private int _nextPaintSource;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            _sfxSource = CreateSource("SFX");
            // Paint sounds each get their own pitch, so they rotate through a few sources
            // instead of changing the pitch of a sound that is still playing.
            _paintSources = new AudioSource[PaintSourceCount];
            for (int i = 0; i < PaintSourceCount; i++)
                _paintSources[i] = CreateSource($"Paint {i}");
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private AudioSource CreateSource(string sourceName)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }

        public void PlayPaint()
        {
            if (paint == null || _paintSources == null)
                return;

            var source = _paintSources[_nextPaintSource];
            _nextPaintSource = (_nextPaintSource + 1) % _paintSources.Length;
            source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            source.PlayOneShot(paint, 0.3f);
        }

        public void PlayUIClick() => Play(uiClick, volume: 0.3f);

        public void PlayRoomComplete() => Play(roomComplete, volume: 0.6f);

        /// <summary>Plays a clip once. Plays in 2D when position is null, otherwise at that world position.</summary>
        public void Play(AudioClip clip, Vector3? position = null, float volume = 1)
        {
            if (clip == null)
                return;

            if (position.HasValue)
                AudioSource.PlayClipAtPoint(clip, position.Value, volume);
            else if (_sfxSource != null)
                _sfxSource.PlayOneShot(clip, volume);
        }
    }
}
