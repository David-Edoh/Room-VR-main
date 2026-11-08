using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ColorRoomVR
{
    public class PaintableGroup : MonoBehaviour
    {
        // Private serialized fields
        [Tooltip("Unique ID for the entire group (e.g., 'Commode').")]
        [SerializeField] private string groupID;
        [Tooltip("All paintable parts belonging to this group.")]
        [SerializeField] private List<PaintableObject> members = new List<PaintableObject>();
        [Header("Events")]
        [Tooltip("Fired when the group's color is set by the player, or restored from a saved state.")]
        public UnityEvent OnPainted;

        public string GroupID => groupID;
        public IReadOnlyList<PaintableObject> Members => members;
        /// <summary>True only while OnPainted is being invoked for a paint made by the player (false when restoring a save).</summary>
        public bool LastPaintByPlayer { get; private set; }

        private void Reset()
        {
            // Automatically set the objectID to the GameObject's name when creating or resetting the component.
            if (string.IsNullOrEmpty(groupID))
                groupID = gameObject.name;
        }

        private void Start()
        {
            if (ColorsDataManager.Instance.TryGetColor(groupID, out var saved))
            {
                SetColor(saved, false);
                // Like PaintableObject, re-fire events so reactions re-activate on load.
                NotifyPainted(false);
            }
            else
            {
                SetColor(Color.white, false);
            }
        }

        public void AddMember(PaintableObject member)
        {
            if (member != null && !members.Contains(member))
                members.Add(member);
        }

        public void SetColor(Color color, bool isPlayerAction = true)
        {
            members.RemoveAll(m => m == null);

            foreach (var m in members)
            {
                m.SetColor(color, false);
            }

            if (isPlayerAction)
            {
                ColorsDataManager.Instance.SetColor(groupID, color);
                NotifyPainted(true);
            }
        }

        public void ApplyDesignTexture(Texture texture, Vector2 tiling)
        {
            members.RemoveAll(m => m == null);
            foreach (var member in members)
                member.ApplyDesignTexture(texture, tiling);
        }

        // Fires the group event and every member's event, so reactions wired to any of them run
        // regardless of the order in which members registered.
        private void NotifyPainted(bool byPlayer)
        {
            LastPaintByPlayer = byPlayer;
            try { OnPainted?.Invoke(); }
            finally { LastPaintByPlayer = false; }

            foreach (var m in members)
                m.NotifyPainted(byPlayer);
        }

        public void EnableOutline(Color color)
        {
            foreach (var m in members)
            {
                m.EnableOutline(color);
            }
        }

        public void DisableOutline()
        {
            foreach (var m in members)
            {
                m.DisableOutline();
            }
        }
    }
}
