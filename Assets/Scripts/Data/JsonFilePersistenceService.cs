using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ColorRoomVR
{
    public class JsonFilePersistenceService : IColorPersistenceService
    {
        private readonly string _filePath;

        public JsonFilePersistenceService(string filePath)
        {
            _filePath = filePath;
        }

        public Dictionary<string, Color> Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new Dictionary<string, Color>();

                var json = File.ReadAllText(_filePath);
                var wrapper = JsonUtility.FromJson<ColorDictionaryWrapper>(json);
                return wrapper?.ToDictionary() ?? new Dictionary<string, Color>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load colors: {e}");
                return new Dictionary<string, Color>();
            }
        }

        public void Save(Dictionary<string, Color> colors)
        {
            try
            {
                var wrapper = new ColorDictionaryWrapper(colors);
                var json = JsonUtility.ToJson(wrapper);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save colors: {e}");
            }
        }

        [Serializable]
        private class ColorDictionaryWrapper
        {
            public List<string> keys = new List<string>();
            public List<Color> values = new List<Color>();

            public ColorDictionaryWrapper() { }

            public ColorDictionaryWrapper(Dictionary<string, Color> dict)
            {
                foreach (var kvp in dict)
                {
                    keys.Add(kvp.Key);
                    values.Add(kvp.Value);
                }
            }

            public Dictionary<string, Color> ToDictionary()
            {
                var dict = new Dictionary<string, Color>();
                for (int i = 0; i < Math.Min(keys.Count, values.Count); i++)
                {
                    dict[keys[i]] = values[i];
                }
                return dict;
            }
        }
    }
}
