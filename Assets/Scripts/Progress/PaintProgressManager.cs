using System;
using System.Collections.Generic;
using EditorAttributes;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace ColorRoomVR
{
    /// <summary>
    /// Tracks how many paintable units (a group counts as one) have been painted at least once.
    /// "Painted" means the unit's id has an entry in <see cref="ColorsDataManager"/>, so progress is restored from the save.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PaintProgressManager : MonoBehaviour
    {
        [SerializeField]
        private Transform room;
        [Header("Events")]
        [Tooltip("Fired with (painted, total) at start and every time a new unit is painted.")]
        public UnityEvent<int, int> OnProgressChanged;
        [Tooltip("Fired when the last unit gets painted during play (not when loading an already finished room).")]
        public UnityEvent OnRoomCompleted;

        private readonly Dictionary<string, Action> _units = new();
        private readonly HashSet<string> _painted = new();

        public int Total => _units.Count;
        public int PaintedCount => _painted.Count;
        public bool IsComplete => Total > 0 && PaintedCount >= Total;

        private void Start()
        {
            var groups = room.GetComponentsInChildren<PaintableGroup>(true);
            foreach (var group in groups)
            {
                Register(group.GroupID, () => group.SetColor(Color.blue));
            }

            var objects = room.GetComponentsInChildren<PaintableObject>(true);
            foreach (var obj in objects)
            {
                if (obj.IsPartOfGroup) continue;

                Register(obj.ObjectID, () => obj.SetColor(Color.blue));
            }

            var data = ColorsDataManager.Instance;
            foreach (var id in _units.Keys)
            {
                if (data.HasColor(id))
                    _painted.Add(id);
            }

            data.OnColorChanged += HandleColorChanged;
            OnProgressChanged?.Invoke(PaintedCount, Total);
        }

        private void OnDestroy()
        {
            if (ColorsDataManager.Instance != null)
                ColorsDataManager.Instance.OnColorChanged -= HandleColorChanged;
        }

        private void Register(string id, Action paint)
        {
            if (string.IsNullOrEmpty(id) || !_units.TryAdd(id, paint))
                Debug.LogWarning($"PaintProgressManager: empty or duplicate paintable id '{id}'.", this);
        }

        private void HandleColorChanged(string id)
        {
            if (!_units.ContainsKey(id) || !_painted.Add(id)) return;

            OnProgressChanged?.Invoke(PaintedCount, Total);
            if (IsComplete)
                OnRoomCompleted?.Invoke();
        }

        /// <summary>Deletes the save and reloads the scene so colors and reactions go back to their initial state.</summary>
        public void ResetRoom()
        {
            ColorsDataManager.Instance.ClearAll();
            InteriorDesignManager.Instance?.ResetDesign();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

#if UNITY_EDITOR
        [Button("Paint All (debug)", 25)]
        private void PaintAllDebug()
        {
            if (!Application.isPlaying) return;

            foreach (var paint in new List<Action>(_units.Values))
                paint();
        }
#endif
    }
}
