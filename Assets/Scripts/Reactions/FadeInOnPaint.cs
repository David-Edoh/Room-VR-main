using DG.Tweening;
using UnityEngine;

namespace ColorRoomVR
{
    /// <summary>
    /// Keeps a picture (SpriteRenderer child) hidden until its frame is painted, then fades it in.
    /// When a saved painted state is restored the picture is shown instantly, without the fade.
    /// </summary>
    public class FadeInOnPaint : MonoBehaviour
    {
        // Private serialized fields
        [SerializeField] private PaintableObject target;
        [SerializeField] private SpriteRenderer picture;
        [SerializeField] private float duration = 0.8f;

        // Private fields
        private bool _shown;
        private Tween _tween;

        private void Reset()
        {
            target = GetComponent<PaintableObject>();
            picture = GetComponentInChildren<SpriteRenderer>(true);
        }

        private void Awake()
        {
            SetAlpha(0f);
        }

        private void OnEnable()
        {
            if (target != null)
                target.OnPainted.AddListener(OnPainted);
        }

        private void OnDisable()
        {
            if (target != null)
                target.OnPainted.RemoveListener(OnPainted);

            // Never leave a picture half faded if this object gets disabled mid-fade.
            _tween?.Kill();
            _tween = null;
            if (_shown)
                SetAlpha(1f);
        }

        private void OnPainted()
        {
            if (_shown || picture == null)
                return;

            _shown = true;
            if (target.LastPaintByPlayer)
                _tween = picture.DOFade(1f, duration).SetTarget(this);
            else
                SetAlpha(1f);
        }

        private void SetAlpha(float alpha)
        {
            if (picture == null)
                return;

            var c = picture.color;
            c.a = alpha;
            picture.color = c;
        }
    }
}
