using System.Collections.Generic;
using System.IO;
using UnityEngine;
using EditorAttributes;

namespace ColorRoomVR
{
    [DefaultExecutionOrder(-100)]
    public class ColorsDataManager : MonoBehaviour
    {
        // Static variables and properties
        public static ColorsDataManager Instance { get; private set; }

        /// <summary>Fired with the id whenever a color is set by the player.</summary>
        public event System.Action<string> OnColorChanged;

        //Private variables (serialized fields)
        [SerializeField, Min(0)]
        private int roomID = 1;

        // Private variables
        private Dictionary<string, Color> _colors = new();
        private IColorPersistenceService _persistence;
        private readonly float _saveDelay = 1f;
        private float _lastSaveTime;
        private bool _dirty;

        //Properties
        public string OfflinePersistentDataPath { get { return Application.persistentDataPath; } private set { } }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            string path = Path.Combine(OfflinePersistentDataPath, $"ColorsRoom_{roomID}");
            _persistence = new JsonFilePersistenceService(path);

            LoadColors();
            if (GetComponent<InteriorDesignManager>() == null)
                gameObject.AddComponent<InteriorDesignManager>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (_dirty && Time.time - _lastSaveTime > _saveDelay)
                SaveColors();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) FlushPendingSave();
        }

        private void OnApplicationQuit() => FlushPendingSave();

        private void FlushPendingSave()
        {
            if (_dirty && _persistence != null)
                SaveColors();
        }

        public bool TryGetColor(string id, out Color color) => _colors.TryGetValue(id, out color);

        public bool HasColor(string id) => _colors.ContainsKey(id);

        public void SetColor(string id, Color color)
        {
            if (string.IsNullOrEmpty(id)) return;

            _colors[id] = color;
            _dirty = true;
            _lastSaveTime = Time.time;
            OnColorChanged?.Invoke(id);
        }

        /// <summary>Deletes every saved color and persists the empty state immediately.</summary>
        public void ClearAll()
        {
            _colors.Clear();
            SaveColors();
        }

        private void SaveColors()
        {
            _persistence.Save(_colors);
            _dirty = false;
        }

        private void LoadColors()
        {
            _colors = _persistence.Load();
        }

        [Button("Open Offline File Path", 25)]
        private void OpenOfflinePersistentDataPath()
        {
            Application.OpenURL("file://" + OfflinePersistentDataPath);
        }
    }
}
