using DG.Tweening;
using UnityEngine;

namespace ColorRoomVR
{
    /// <summary>
    /// Punch-scales this object when appearing. Play() is called by EnableOnPaint, not on OnEnable,
    /// so loading a save never triggers it.
    /// </summary>
    public class PunchOnEnable : MonoBehaviour
    {
        // Private serialized fields
        [Tooltip("Punch strength, relative to the original scale.")]
        [SerializeField] private Vector3 punch = new(0.2f, 0.2f, 0.2f);
        [SerializeField] private float duration = 0.4f;
        [SerializeField] private int vibrato = 6;
        [SerializeField] private float elasticity = 0.6f;

        // Private fields
        private Vector3 _originalScale;
        private bool _hasOriginalScale;
        private Tween _tween;

        private void Awake()
        {
            CacheOriginalScale();
        }

        private void CacheOriginalScale()
        {
            if (_hasOriginalScale)
                return;

            _originalScale = transform.localScale;
            _hasOriginalScale = true;
        }

        public void Play()
        {
            CacheOriginalScale();

            _tween?.Kill();
            transform.localScale = _originalScale;
            _tween = transform.DOPunchScale(punch, duration, vibrato, elasticity)
                .OnComplete(() => transform.localScale = _originalScale);
        }

        private void OnDisable()
        {
            _tween?.Kill();
            _tween = null;
            if (_hasOriginalScale)
                transform.localScale = _originalScale;
        }
    }
}
