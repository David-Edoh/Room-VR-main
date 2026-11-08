using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ColorRoomVR
{
    /// <summary>
    /// Animates the visual change when the player paints. The paint itself (saving, events) is already done
    /// by the time Play is called; this only replays the rendered color from the old value to the new one.
    /// </summary>
    public class PaintEffectManager : MonoBehaviour
    {
        public enum Mode
        {
            None,
            /// <summary>Blends the whole object from the old color to the new one.</summary>
            Tween,
            /// <summary>The new color spreads from the hit point (needs the "Color Room/Paintable Lit" shader; other objects use Tween).</summary>
            Shader
        }

        // Private serialized fields
        [SerializeField] private Mode mode = Mode.Tween;
        [SerializeField, Min(0.01f)] private float duration = 0.5f;
        [SerializeField] private Ease ease = Ease.OutQuad;
        [Header("Shader mode")]
        [SerializeField, Min(0.01f)] private float revealDuration = 0.8f;
        [SerializeField] private Ease revealEase = Ease.OutSine;
        [Tooltip("Added to the distance to the farthest corner so the soft, noisy edge fully leaves the object. Keep it above the material's Paint Edge Width + Noise Amount.")]
        [SerializeField, Min(0f)] private float revealMargin = 0.3f;

        // Private fields
        private readonly List<PaintableObject> _targets = new();
        private readonly List<Color> _fromColors = new();

        /// <summary>
        /// Call right before painting: remembers the colors currently rendered by the objects that the paint
        /// will affect (a group = all its members), since SetColor applies the new color immediately.
        /// </summary>
        public void Capture(PaintableObject obj, PaintableGroup group)
        {
            _targets.Clear();
            _fromColors.Clear();

            if (group != null)
                _targets.AddRange(group.Members);
            else if (obj != null)
                _targets.Add(obj);

            foreach (var t in _targets)
                _fromColors.Add(t != null ? t.VisualColor : Color.white);
        }

        /// <summary>Call right after painting, to animate from the captured colors to the new one. hitPoint is in world space.</summary>
        public void Play(Color to, Vector3 hitPoint)
        {
            if (mode == Mode.None)
                return;

            for (int i = 0; i < _targets.Count; i++)
            {
                var target = _targets[i];
                if (target == null)
                    continue;

                // Targeting the component (not the transform) so other tweens on the object are left alone.
                target.DOKill();

                if (mode == Mode.Shader && target.SupportsPaintReveal)
                    PlayReveal(target, _fromColors[i], hitPoint);
                else
                    PlayTween(target, _fromColors[i], to);
            }
        }

        // The new color is already in _BaseColor (SetColor); the shader blends it with the previous one around the hit point.
        private void PlayReveal(PaintableObject target, Color from, Vector3 hitPoint)
        {
            float maxRadius = FarthestCornerDistance(target.WorldBounds, hitPoint) + revealMargin;
            target.SetPaintReveal(from, hitPoint, 0f);

            DOVirtual.Float(0f, maxRadius, revealDuration, r => target.SetPaintReveal(from, hitPoint, r))
                .SetEase(revealEase)
                .SetTarget(target)
                .SetLink(target.gameObject)
                .OnKill(() =>
                {
                    // Leave the object fully painted if the reveal is cut short by a new paint or by disabling.
                    if (target != null)
                        target.SetPaintReveal(from, hitPoint, PaintableObject.PaintRevealDone);
                });
        }

        private static float FarthestCornerDistance(Bounds bounds, Vector3 point)
        {
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;
            var farthest = new Vector3(
                point.x > c.x ? c.x - e.x : c.x + e.x,
                point.y > c.y ? c.y - e.y : c.y + e.y,
                point.z > c.z ? c.z - e.z : c.z + e.z);
            return Vector3.Distance(point, farthest);
        }

        private void PlayTween(PaintableObject target, Color from, Color to)
        {
            // The final color was already applied by SetColor; rewind to the start and blend forward.
            target.ApplyVisualColor(from);

            DOVirtual.Float(0f, 1f, duration, t => target.ApplyVisualColor(Color.Lerp(from, to, t)))
                .SetEase(ease)
                .SetTarget(target)
                .SetLink(target.gameObject)
                .OnKill(() =>
                {
                    // Never leave a half-blended color if the tween is cut short by a new paint or by disabling.
                    if (target != null)
                        target.ApplyVisualColor(to);
                });
        }
    }
}
