using UnityEngine;

namespace ColorRoomVR
{
    /// <summary>
    /// Places a world-space UI in front of the player's head, facing them.
    /// </summary>
    public class WorldPanelPlacer : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [Tooltip("Distance in front of the head.")]
        [SerializeField] private float distance = 1.2f;
        [Tooltip("Height offset relative to the head.")]
        [SerializeField] private float heightOffset = -0.1f;

        private void Awake()
        {
            if (head == null && Camera.main != null)
                head = Camera.main.transform;
        }

        public void PlaceInFront()
        {
            if (head == null) return;

            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            forward.Normalize();

            Vector3 pos = head.position + forward * distance;
            pos.y = head.position.y + heightOffset;

            // World-space canvas faces the player: its forward points away from the head.
            transform.SetPositionAndRotation(pos, Quaternion.LookRotation(forward, Vector3.up));
        }
    }
}
