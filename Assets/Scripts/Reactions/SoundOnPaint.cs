using UnityEngine;
using UnityEngine.Events;

namespace ColorRoomVR
{
    /// <summary>Plays an optional clip when the player paints the target (never when a saved state is restored).</summary>
    public class SoundOnPaint : MonoBehaviour
    {
        // Private serialized fields
        [Tooltip("Exactly one of target / groupTarget should be assigned.")]
        [SerializeField] private PaintableObject target;
        [SerializeField] private PaintableGroup groupTarget;
        [Tooltip("Optional. With no clip nothing happens.")]
        [SerializeField] private AudioClip clip;
        [Tooltip("Play only the first time the target is ever painted. A target restored from a save counts as already painted.")]
        [SerializeField] private bool firstPaintOnly;

        private bool _alreadyPlayed;

        private UnityEvent SourceEvent => groupTarget != null ? groupTarget.OnPainted : target != null ? target.OnPainted : null;
        private bool PaintedByPlayer => groupTarget != null ? groupTarget.LastPaintByPlayer : target != null && target.LastPaintByPlayer;

        private void OnEnable()
        {
            SourceEvent?.AddListener(OnPainted);
        }

        private void OnDisable()
        {
            SourceEvent?.RemoveListener(OnPainted);
        }

        private void OnPainted()
        {
            if (!PaintedByPlayer)
            {
                // OnPainted also fires when a saved paint is restored, so the first paint already happened.
                if (firstPaintOnly)
                    _alreadyPlayed = true;
                return;
            }

            if (clip == null || (firstPaintOnly && _alreadyPlayed))
                return;

            _alreadyPlayed = true;
            AudioManager.Instance?.Play(clip, transform.position);
        }
    }
}
