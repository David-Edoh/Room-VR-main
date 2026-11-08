using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ColorRoomVR
{
    /// <summary>Runtime finish and layout editor for the room. Textures are applied with property blocks,
    /// so the original shared materials stay intact.</summary>
    [DefaultExecutionOrder(-80)]
    public sealed class InteriorDesignManager : MonoBehaviour
    {
        public enum Tool { Color, Texture, Move }

        [Serializable] private class ItemState
        {
            public string id;
            public int textureIndex = -1;
            public bool hasPosition;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
        }
        [Serializable] private class DesignFile { public List<ItemState> items = new(); }
        private sealed class Finish
        {
            public string name;
            public Vector2 tiling;
            public Texture2D texture;
            public string imageUrl;
        }

        public static InteriorDesignManager Instance { get; private set; }
        public Tool CurrentTool { get; private set; } = Tool.Texture;
        public int SelectedTextureIndex { get; private set; }
        public string SelectedTextureName => _finishes.Count == 0 ? "Oak" : _finishes[SelectedTextureIndex].name;
        public event Action<string> OnFinishApplied;

        [SerializeField] private Transform roomRoot;
        private readonly List<Finish> _finishes = new();
        private readonly Dictionary<string, ItemState> _savedItems = new();
        private string _savePath;
        private Canvas _toolbar;
        private CanvasGroup _toolbarGroup;
        private CanvasGroup _colorWheelGroup;
        private CanvasGroup _textureSwitchButtonGroup;
        private Text _status;
        private Transform _movingRoot;
        private Vector3 _moveOffset;
        private Plane _movePlane;
        private bool _hasUnsavedChanges;
        private bool _toolbarVisible = true;
        private bool _showTextureSelector = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            _savePath = Path.Combine(Application.persistentDataPath, "RoomDesign.json");
            LoadDesign();
            CreateFinishes();
        }

        private IEnumerator Start()
        {
            // Let every paintable restore its existing saved color before we apply the default design.
            yield return null;
            if (roomRoot == null)
            {
                var room = GameObject.Find("Room");
                roomRoot = room != null ? room.transform : transform;
            }
            ApplyDefaultDesign();
            ApplySavedLayout();
            SetupColorWheelPanel();
            BuildToolbar();
            AddTextureSwitchButton();
            SetCustomizerPanel(true);
            UpdateStatus("Preset: Warm Scandinavian · choose a finish");
            StartCoroutine(LoadOnlineFinishes());
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SaveDesign();
                Instance = null;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveDesign();
        }

        private void OnApplicationQuit() => SaveDesign();

        public void SetTool(Tool tool)
        {
            if (_movingRoot != null) EndMove();
            CurrentTool = tool;
            if (tool == Tool.Color) SetCustomizerPanel(false);
            else if (tool == Tool.Texture) SetCustomizerPanel(true);
            UpdateStatus(tool switch
            {
                Tool.Color => "Aim at an element and pull the trigger to paint",
                Tool.Texture => $"Selected finish: {SelectedTextureName} · aim and pull trigger to apply",
                _ => "Aim at furniture, hold the trigger and drag to move it"
            });
            RefreshToolbarSelection();
        }

        public void SelectTexture(int index)
        {
            if (_finishes.Count == 0) return;
            SelectedTextureIndex = (index % _finishes.Count + _finishes.Count) % _finishes.Count;
            UpdateStatus($"Selected finish: {SelectedTextureName} · aim and pull trigger to apply");
            RefreshToolbarSelection();
        }

        public void ApplyTexture(PaintableObject obj)
        {
            if (obj == null || _finishes.Count == 0) return;
            var finish = _finishes[SelectedTextureIndex];
            if (obj.Group != null)
            {
                obj.Group.ApplyDesignTexture(finish.texture, finish.tiling);
                SaveTextureChoice(obj.Group.GroupID, SelectedTextureIndex);
                OnFinishApplied?.Invoke(obj.Group.GroupID);
            }
            else
            {
                obj.ApplyDesignTexture(finish.texture, finish.tiling);
                string id = GetItemId(obj);
                SaveTextureChoice(id, SelectedTextureIndex);
                OnFinishApplied?.Invoke(id);
            }
            UpdateStatus($"Applied {finish.name} to {obj.Group?.GroupID ?? obj.name}");
        }

        public bool BeginMove(PaintableObject obj, Ray ray, Vector3 hitPoint)
        {
            if (obj == null) return false;
            var root = FindMovableRoot(obj.transform);
            if (root == null) return false;
            _movingRoot = root;
            _movePlane = new Plane(Vector3.up, new Vector3(0f, root.position.y, 0f));
            _moveOffset = root.position - hitPoint;
            _moveOffset.y = 0f;
            UpdateMove(ray);
            UpdateStatus($"Moving {root.name} · release trigger to place");
            return true;
        }

        public void UpdateMove(Ray ray)
        {
            if (_movingRoot == null || !_movePlane.Raycast(ray, out float distance)) return;
            Vector3 point = ray.GetPoint(distance);
            _movingRoot.position = new Vector3(point.x + _moveOffset.x, _movingRoot.position.y, point.z + _moveOffset.z);
        }

        public void EndMove()
        {
            if (_movingRoot == null) return;
            StoreLayout(_movingRoot);
            _movingRoot = null;
            UpdateStatus("Layout saved · select another tool or move another item");
        }

        public void ResetDesign()
        {
            _savedItems.Clear();
            _hasUnsavedChanges = true;
            SaveDesign();
        }

        public void SetToolbarVisible(bool visible)
        {
            _toolbarVisible = visible;
            UpdatePanelVisibility();
        }

        private void SetCustomizerPanel(bool showTextures)
        {
            _showTextureSelector = showTextures;
            UpdatePanelVisibility();
        }

        private void UpdatePanelVisibility()
        {
            bool showTextures = _toolbarVisible && _showTextureSelector;
            bool showColorWheel = _toolbarVisible && !_showTextureSelector;
            if (_toolbarGroup != null)
            {
                _toolbarGroup.alpha = showTextures ? 1f : 0f;
                _toolbarGroup.interactable = showTextures;
                _toolbarGroup.blocksRaycasts = showTextures;
            }
            if (_colorWheelGroup != null)
            {
                _colorWheelGroup.alpha = showColorWheel ? 1f : 0f;
                _colorWheelGroup.interactable = showColorWheel;
                _colorWheelGroup.blocksRaycasts = showColorWheel;
            }
            if (_textureSwitchButtonGroup != null)
            {
                _textureSwitchButtonGroup.alpha = showColorWheel ? 1f : 0f;
                _textureSwitchButtonGroup.interactable = showColorWheel;
                _textureSwitchButtonGroup.blocksRaycasts = showColorWheel;
            }
        }

        private void SetupColorWheelPanel()
        {
            var colorPalette = GameObject.Find("ColorPalette");
            if (colorPalette == null) return;
            _colorWheelGroup = colorPalette.GetComponent<CanvasGroup>();
            if (_colorWheelGroup == null) _colorWheelGroup = colorPalette.AddComponent<CanvasGroup>();
        }

        private void ApplyDefaultDesign()
        {
            if (ColorsDataManager.Instance == null) return;
            var groups = roomRoot.GetComponentsInChildren<PaintableGroup>(true);
            foreach (var group in groups)
            {
                int index = GetTextureIndex(group.GroupID, group.name);
                ApplyFinish(group, index);
                if (!ColorsDataManager.Instance.HasColor(group.GroupID))
                    group.SetColor(GetDefaultColor(group.name), false);
                ApplySavedTexture(group.GroupID, group);
            }

            var objects = roomRoot.GetComponentsInChildren<PaintableObject>(true);
            foreach (var obj in objects)
            {
                if (obj.IsPartOfGroup) continue;
                string id = GetItemId(obj);
                int index = GetTextureIndex(id, obj.name);
                ApplyFinish(obj, index);
                if (!ColorsDataManager.Instance.HasColor(id))
                    obj.SetColor(GetDefaultColor(obj.name), false);
                ApplySavedTexture(id, obj);
            }
        }

        private void ApplySavedLayout()
        {
            foreach (var item in _savedItems.Values)
            {
                if (!item.hasPosition) continue;
                var root = FindByStableId(item.id);
                if (root == null) continue;
                root.localPosition = item.localPosition;
                root.localEulerAngles = item.localEulerAngles;
            }
        }

        private void ApplySavedTexture(string id, PaintableGroup group)
        {
            if (_savedItems.TryGetValue(id, out var state) && state.textureIndex >= 0)
                ApplyFinish(group, state.textureIndex);
        }

        private void ApplySavedTexture(string id, PaintableObject obj)
        {
            if (_savedItems.TryGetValue(id, out var state) && state.textureIndex >= 0)
                ApplyFinish(obj, state.textureIndex);
        }

        private void ApplyFinish(PaintableGroup group, int index)
        {
            if (_finishes.Count == 0) return;
            var finish = _finishes[Mathf.Clamp(index, 0, _finishes.Count - 1)];
            group.ApplyDesignTexture(finish.texture, finish.tiling);
        }

        private void ApplyFinish(PaintableObject obj, int index)
        {
            if (_finishes.Count == 0) return;
            var finish = _finishes[Mathf.Clamp(index, 0, _finishes.Count - 1)];
            obj.ApplyDesignTexture(finish.texture, finish.tiling);
        }

        private void SaveTextureChoice(string id, int index)
        {
            if (!_savedItems.TryGetValue(id, out var state))
                _savedItems[id] = state = new ItemState { id = id };
            state.textureIndex = index;
            MarkDirty();
        }

        private void StoreLayout(Transform root)
        {
            string id = GetStableId(root);
            if (!_savedItems.TryGetValue(id, out var state))
                _savedItems[id] = state = new ItemState { id = id };
            state.hasPosition = true;
            state.localPosition = root.localPosition;
            state.localEulerAngles = root.localEulerAngles;
            MarkDirty();
        }

        private Transform FindMovableRoot(Transform child)
        {
            Transform root = child;
            while (root.parent != null && root.parent != roomRoot)
                root = root.parent;
            string name = root.name.ToLowerInvariant();
            if (name.Contains("wall") || name.Contains("floor") || name.Contains("ceiling") ||
                name.Contains("window") || name.Contains("door") || name.Contains("room")) return null;
            return root;
        }

        private Transform FindByStableId(string id)
        {
            if (roomRoot == null) return null;
            var transforms = roomRoot.GetComponentsInChildren<Transform>(true);
            foreach (var candidate in transforms)
                if (GetStableId(candidate) == id) return candidate;
            return null;
        }

        private string GetStableId(Transform target)
        {
            var path = new Stack<string>();
            for (var current = target; current != null && current != roomRoot; current = current.parent)
                path.Push($"{current.name}[{current.GetSiblingIndex()}]");
            return string.Join("/", path);
        }

        private string GetItemId(PaintableObject obj)
        {
            return string.IsNullOrWhiteSpace(obj.ObjectID) ? GetStableId(obj.transform) : obj.ObjectID;
        }

        private static int GetTextureIndex(string id, string objectName)
        {
            string name = (id + " " + objectName).ToLowerInvariant();
            if (name.Contains("tile") || name.Contains("ceramic")) return 2;
            if (name.Contains("wall")) return 4;
            if (name.Contains("curtain")) return 3;
            if (name.Contains("floor") || name.Contains("desk") || name.Contains("table") || name.Contains("shelf") || name.Contains("chair") || name.Contains("bed_base") || name.Contains("commode")) return 0;
            if (name.Contains("bed") || name.Contains("pillow") || name.Contains("rug") || name.Contains("sofa")) return 3;
            return 4;
        }

        private static Color GetDefaultColor(string objectName)
        {
            string name = objectName.ToLowerInvariant();
            if (name.Contains("wall")) return new Color(0.94f, 0.86f, 0.73f);
            if (name.Contains("floor")) return new Color(0.82f, 0.67f, 0.46f);
            if (name.Contains("bed_base")) return new Color(0.72f, 0.51f, 0.32f);
            if (name.Contains("bed") || name.Contains("pillow") || name.Contains("rug") || name.Contains("curtain")) return new Color(0.72f, 0.79f, 0.68f);
            if (name.Contains("desk") || name.Contains("table") || name.Contains("shelf") || name.Contains("chair")) return new Color(0.72f, 0.51f, 0.32f);
            return new Color(0.94f, 0.83f, 0.64f);
        }

        private void LoadDesign()
        {
            if (!File.Exists(_savePath)) return;
            try
            {
                var data = JsonUtility.FromJson<DesignFile>(File.ReadAllText(_savePath));
                if (data?.items == null) return;
                foreach (var item in data.items)
                    if (item != null && !string.IsNullOrEmpty(item.id)) _savedItems[item.id] = item;
            }
            catch (Exception ex) { Debug.LogWarning($"Could not read saved room design: {ex.Message}"); }
        }

        private void MarkDirty()
        {
            _hasUnsavedChanges = true;
            SaveDesign();
        }

        private void SaveDesign()
        {
            if (!_hasUnsavedChanges || string.IsNullOrEmpty(_savePath)) return;
            try
            {
                File.WriteAllText(_savePath, JsonUtility.ToJson(new DesignFile { items = new List<ItemState>(_savedItems.Values) }, true));
                _hasUnsavedChanges = false;
            }
            catch (Exception ex) { Debug.LogWarning($"Could not save room design: {ex.Message}"); }
        }

        private void CreateFinishes()
        {
            _finishes.Add(new Finish { name = "Oak", tiling = new Vector2(2.2f, 2.2f), texture = CreatePattern("Oak", Pattern.Oak), imageUrl = "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/wood_floor/wood_floor_diff_1k.jpg" });
            _finishes.Add(new Finish { name = "Botanical", tiling = new Vector2(1.6f, 1.6f), texture = CreatePattern("Botanical", Pattern.Botanical), imageUrl = "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/floral_jacquard/floral_jacquard_diff_1k.jpg" });
            _finishes.Add(new Finish { name = "Ceramic", tiling = new Vector2(3f, 3f), texture = CreatePattern("Ceramic", Pattern.Ceramic), imageUrl = "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/interior_tiles/interior_tiles_diff_1k.jpg" });
            _finishes.Add(new Finish { name = "Linen", tiling = new Vector2(2f, 2f), texture = CreatePattern("Linen", Pattern.Linen), imageUrl = "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/rough_linen/rough_linen_diff_1k.jpg" });
            _finishes.Add(new Finish { name = "Plaster", tiling = new Vector2(1.5f, 1.5f), texture = CreatePattern("Plaster", Pattern.Plaster), imageUrl = "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/1k/painted_plaster_wall/painted_plaster_wall_diff_1k.jpg" });
        }

        private IEnumerator LoadOnlineFinishes()
        {
            for (int i = 0; i < _finishes.Count; i++)
            {
                var finish = _finishes[i];
                using (var request = UnityWebRequestTexture.GetTexture(finish.imageUrl))
                {
                    request.timeout = 8;
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning($"Could not load the {finish.name} image texture; using the built-in finish instead. {request.error}");
                        continue;
                    }

                    finish.texture = DownloadHandlerTexture.GetContent(request);
                    finish.texture.name = finish.name + " · Poly Haven CC0";
                    ApplyFinishToRoom(i);
                    RefreshFinishSwatch(i);
                }
            }
        }

        private void ApplyFinishToRoom(int finishIndex)
        {
            if (roomRoot == null) return;
            foreach (var group in roomRoot.GetComponentsInChildren<PaintableGroup>(true))
            {
                int selected = _savedItems.TryGetValue(group.GroupID, out var state) && state.textureIndex >= 0
                    ? state.textureIndex : GetTextureIndex(group.GroupID, group.name);
                if (selected == finishIndex) ApplyFinish(group, selected);
            }
            foreach (var obj in roomRoot.GetComponentsInChildren<PaintableObject>(true))
            {
                if (obj.IsPartOfGroup) continue;
                string id = GetItemId(obj);
                int selected = _savedItems.TryGetValue(id, out var state) && state.textureIndex >= 0
                    ? state.textureIndex : GetTextureIndex(id, obj.name);
                if (selected == finishIndex) ApplyFinish(obj, selected);
            }
        }

        private void RefreshFinishSwatch(int finishIndex)
        {
            if (_toolbar == null) return;
            var buttons = _toolbar.GetComponentsInChildren<Button>();
            int buttonIndex = finishIndex + 3;
            if (buttonIndex >= buttons.Length) return;
            var image = buttons[buttonIndex].GetComponent<Image>();
            if (image == null) return;
            if (image.sprite != null) Destroy(image.sprite);
            image.sprite = Sprite.Create(_finishes[finishIndex].texture,
                new Rect(0, 0, _finishes[finishIndex].texture.width, _finishes[finishIndex].texture.height),
                new Vector2(.5f, .5f));
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }

        private enum Pattern { Oak, Botanical, Ceramic, Linen, Plaster }

        private static Texture2D CreatePattern(string textureName, Pattern pattern)
        {
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = textureName + " Finish",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float n = Mathf.PerlinNoise(x * 0.065f, y * 0.065f);
                Color c;
                switch (pattern)
                {
                    case Pattern.Oak:
                    {
                        float grain = Mathf.Sin((x + 7f * Mathf.Sin(y * .08f)) * .48f) * .035f;
                        float shade = Mathf.Clamp01(.84f + (n - .5f) * .22f + grain);
                        c = new Color(.57f * shade, .34f * shade, .18f * shade);
                        break;
                    }
                    case Pattern.Botanical:
                    {
                        float u = (x % 48) - 24f, v = (y % 48) - 24f;
                        float leaf = Mathf.Abs(u * .42f + v * .9f) < 5f && Mathf.Abs(u * .9f - v * .42f) < 15f ? 1f : 0f;
                        c = leaf > 0 ? new Color(.38f, .52f, .36f) : new Color(.91f, .86f, .74f);
                        c *= .98f + (n - .5f) * .05f;
                        break;
                    }
                    case Pattern.Ceramic:
                    {
                        bool grout = x % 32 < 2 || y % 32 < 2;
                        c = grout ? new Color(.72f, .67f, .59f) : new Color(.87f, .82f, .72f) * (.97f + n * .04f);
                        break;
                    }
                    case Pattern.Linen:
                    {
                        float weave = ((x % 8 == 0 ? .94f : 1f) * (y % 8 == 0 ? .95f : 1f));
                        float thread = ((x % 8 == 3 && y % 8 < 4) || (y % 8 == 5 && x % 8 > 3)) ? 1.025f : 1f;
                        c = new Color(.76f, .79f, .68f) * weave * thread * (.98f + n * .04f);
                        break;
                    }
                    default:
                        c = new Color(.88f, .82f, .71f) * (.96f + n * .06f);
                        break;
                }
                pixels[y * size + x] = c;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void BuildToolbar()
        {
            Camera camera = Camera.main;
            if (camera == null || _toolbar != null) return;
            var canvasObject = new GameObject("Interior Design Toolbar", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.localScale = Vector3.one * .0005f;
            var toolbarAnchor = canvasObject.AddComponent<PaletteAnchor>();
            var mainCanvasObject = GameObject.Find("MainCanvas");
            var paletteAnchor = mainCanvasObject != null ? mainCanvasObject.GetComponent<PaletteAnchor>() : null;
            toolbarAnchor.CopyHandTrackingFrom(paletteAnchor);
            _toolbar = canvasObject.GetComponent<Canvas>();
            _toolbar.renderMode = RenderMode.WorldSpace;
            _toolbar.worldCamera = camera;
            _toolbarGroup = canvasObject.AddComponent<CanvasGroup>();
            SetToolbarVisible(_toolbarVisible);
            canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            var rect = (RectTransform)canvasObject.transform;
            rect.sizeDelta = new Vector2(460f, 108f);
            var background = canvasObject.AddComponent<Image>();
            background.color = new Color(.075f, .09f, .085f, .94f);

            MakeButton("COLOR WHEEL", new Vector2(-128, 26), new Vector2(148, 36), () => SetTool(Tool.Color));
            MakeButton("TEXTURE", new Vector2(2, 26), new Vector2(104, 36), () => SetTool(Tool.Texture));
            MakeButton("MOVE", new Vector2(116, 26), new Vector2(82, 36), () => SetTool(Tool.Move));
            for (int i = 0; i < _finishes.Count; i++)
            {
                int captured = i;
                var button = MakeButton("", new Vector2(-160 + i * 51, -19), new Vector2(44, 26), () => SelectTexture(captured));
                var image = button.GetComponent<Image>();
                image.sprite = Sprite.Create(_finishes[i].texture, new Rect(0, 0, _finishes[i].texture.width, _finishes[i].texture.height), new Vector2(.5f, .5f));
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                MakeText(button.transform, _finishes[i].name, new Vector2(0, -15), new Vector2(50, 15), 10, Color.white);
            }
            _status = MakeText(canvasObject.transform, "", new Vector2(152, -20), new Vector2(145, 43), 12, new Color(.9f, .88f, .79f));
            RefreshToolbarSelection();
        }

        private void AddTextureSwitchButton()
        {
            var mainCanvasObject = GameObject.Find("MainCanvas");
            if (mainCanvasObject == null) return;
            var button = CreateButton(mainCanvasObject.transform, "TEXTURES", new Vector2(0f, 270f), new Vector2(154f, 42f),
                () => SetTool(Tool.Texture));
            _textureSwitchButtonGroup = button.gameObject.AddComponent<CanvasGroup>();
        }

        private Button MakeButton(string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            return CreateButton(_toolbar.transform, label, position, size, action);
        }

        private Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var go = new GameObject(string.IsNullOrEmpty(label) ? "Finish" : label + " Tool", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = label switch { "COLOR WHEEL" => new Color(.28f, .42f, .34f), "TEXTURES" => new Color(.48f, .35f, .22f), "TEXTURE" => new Color(.48f, .35f, .22f), "MOVE" => new Color(.25f, .36f, .43f), _ => Color.white };
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            if (!string.IsNullOrEmpty(label)) MakeText(go.transform, label, Vector2.zero, size, 14, Color.white);
            return button;
        }

        private Text MakeText(Transform parent, string value, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private void RefreshToolbarSelection()
        {
            if (_toolbar == null) return;
            var buttons = _toolbar.GetComponentsInChildren<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                var image = buttons[i].GetComponent<Image>();
                if (image == null) continue;
                string n = buttons[i].name;
                if (n == "COLOR WHEEL Tool") image.color = CurrentTool == Tool.Color ? new Color(.42f, .65f, .48f) : new Color(.28f, .42f, .34f);
                else if (n == "TEXTURE Tool") image.color = CurrentTool == Tool.Texture ? new Color(.7f, .49f, .29f) : new Color(.48f, .35f, .22f);
                else if (n == "MOVE Tool") image.color = CurrentTool == Tool.Move ? new Color(.37f, .57f, .68f) : new Color(.25f, .36f, .43f);
                else if (n == "Finish") image.color = i - 3 == SelectedTextureIndex ? Color.white : new Color(.72f, .72f, .72f);
            }
        }

        private void UpdateStatus(string message)
        {
            if (_status != null) _status.text = message;
        }
    }
}
