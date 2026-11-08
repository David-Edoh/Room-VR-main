using UnityEngine;
using UnityEngine.Events;

namespace ColorRoomVR
{
    public class EnableOnPaint : MonoBehaviour
    {
        // Private serialized fields
        [SerializeField] private GameObject[] objectsToEnable;
        [Tooltip("Exactly one of target / groupTarget must be assigned.")]
        [SerializeField] private PaintableObject target;
        [SerializeField] private PaintableGroup groupTarget;

        private UnityEvent SourceEvent => groupTarget != null ? groupTarget.OnPainted : target != null ? target.OnPainted : null;
        private bool PaintedByPlayer => groupTarget != null ? groupTarget.LastPaintByPlayer : target != null && target.LastPaintByPlayer;

        private void OnEnable()
        {
            if ((target == null) == (groupTarget == null))
                Debug.LogWarning($"{nameof(EnableOnPaint)} on '{name}' needs exactly one of target / groupTarget assigned.", this);

            // Start hidden; a saved painted state re-fires OnPainted in the paintable's Start and re-enables them.
            foreach (var go in objectsToEnable)
                if (go != null)
                    go.SetActive(false);

            SourceEvent?.AddListener(OnPainted);
        }

        private void OnDisable()
        {
            SourceEvent?.RemoveListener(OnPainted);
        }

        private void OnPainted()
        {
            bool byPlayer = PaintedByPlayer;

            foreach (var go in objectsToEnable)
            {
                // Punch only objects that are appearing because the player just painted (never on save load or repaint).
                bool wasInactive = !go.activeSelf;
                go.SetActive(true);

                if (byPlayer && wasInactive && go.TryGetComponent(out PunchOnEnable punch))
                    punch.Play();
            }
        }
    }
}
