using EditorAttributes;
using UnityEngine;
using UnityEngine.Events;

namespace ColorRoomVR
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class PaintableObject : MonoBehaviour
    {
        public enum ApplyMode { OneMaterial, AllMaterials }

        // Private serialized fields
        [Header("Group Reference")]
        [Tooltip("Reference to the PaintableGroup this object belongs to.")]
        [SerializeField] private PaintableGroup paintableGroup;
        [Tooltip("Unique identifier for this object. Used for saving/loading color.")]
        [SerializeField] private string objectID;
        [SerializeField] private Color color = Color.white;
        [SerializeField] private ApplyMode applyMode;
        [Tooltip("The material index to change if ApplyMode is OneMaterial."), ShowField(nameof(applyMode), ApplyMode.OneMaterial), Clamp(0, Mathf.Infinity)]
        [SerializeField] private int materialIndex = 0;
        [Header("Events")]
        [Tooltip("Fired when the object's color is set by the player.")]
        public UnityEvent OnPainted;

        // Shader property ids
        private static readonly int PrevColorId = Shader.PropertyToID("_PrevColor");
        private static readonly int PaintOriginId = Shader.PropertyToID("_PaintOrigin");
        private static readonly int PaintRadiusId = Shader.PropertyToID("_PaintRadius");

        // Private fields
        private MeshRenderer _meshRenderer;
        private MaterialPropertyBlock _mpb;
        private Outline _outline;

        // Public properties
        public string ObjectID => objectID;
        public bool IsPartOfGroup => paintableGroup != null;
        public PaintableGroup Group => paintableGroup;
        /// <summary>The color currently rendered, which differs from the saved one while a paint effect is running.</summary>
        public Color VisualColor { get; private set; } = Color.white;
        /// <summary>True only while OnPainted is being invoked for a paint made by the player (false when restoring a save).</summary>
        public bool LastPaintByPlayer { get; private set; }

        private void Awake()
        {
            EnsureInitialized();

            if (paintableGroup != null)
                paintableGroup.AddMember(this);
        }

        private void EnsureInitialized()
        {
            if (_meshRenderer == null)
                _meshRenderer = GetComponent<MeshRenderer>();
            _mpb ??= new MaterialPropertyBlock();
        }

        private void Reset()
        {
            // Automatically set the objectID to the GameObject's name when creating or resetting the component.
            if (string.IsNullOrEmpty(objectID))
                objectID = gameObject.name;
        }

        private void Start()
        {
            if (IsPartOfGroup)
                return;

            LoadInitialState();
        }

        private void LoadInitialState()
        {
            if (ColorsDataManager.Instance.TryGetColor(objectID, out var saved))
            {
                SetColor(saved, false);
                // If an object is already painted on load, we should also trigger its event
                // to ensure animations/unlocks are activated correctly.
                NotifyPainted(false);
            }
            else
            {
                SetColor(Color.white, false);
            }
        }

        /// <summary>
        /// Applies a color to the object's material(s).
        /// </summary>
        /// <param name="color">The color to apply.</param>
        /// <param name="isPlayerAction">If true, this action was initiated by the player, saving the color and invoking the OnPainted event.</param>
        public void SetColor(Color color, bool isPlayerAction = true)
        {
            ApplyVisualColor(color);

            if (isPlayerAction)
            {
                ColorsDataManager.Instance.SetColor(objectID, color);
                NotifyPainted(true);
            }
        }

        /// <summary>
        /// Changes only what is rendered (no saving, no events). Used by paint effects to animate between colors.
        /// </summary>
        public void ApplyVisualColor(Color color)
        {
            EnsureInitialized();
            VisualColor = color;

            if (applyMode == ApplyMode.AllMaterials)
            {
                for (int i = 0; i < _meshRenderer.sharedMaterials.Length; i++)
                    WriteBaseColor(i, color);
            }
            else
            {
                WriteBaseColor(materialIndex, color);
            }
        }

        /// <summary>Applies a repeatable design texture without replacing the source material.</summary>
        public void ApplyDesignTexture(Texture texture, Vector2 tiling)
        {
            EnsureInitialized();
            var materials = _meshRenderer.sharedMaterials;
            int first = applyMode == ApplyMode.AllMaterials ? 0 : materialIndex;
            int last = applyMode == ApplyMode.AllMaterials ? materials.Length : Mathf.Min(materialIndex + 1, materials.Length);
            for (int i = first; i < last; i++)
            {
                if (materials[i] == null) continue;
                _meshRenderer.GetPropertyBlock(_mpb, i);
                if (materials[i].HasProperty("_BaseMap"))
                {
                    _mpb.SetTexture("_BaseMap", texture);
                    _mpb.SetVector("_BaseMap_ST", new Vector4(tiling.x, tiling.y, 0f, 0f));
                }
                else if (materials[i].HasProperty("_MainTex"))
                {
                    _mpb.SetTexture("_MainTex", texture);
                    _mpb.SetVector("_MainTex_ST", new Vector4(tiling.x, tiling.y, 0f, 0f));
                }
                _meshRenderer.SetPropertyBlock(_mpb, i);
            }
        }

        private void WriteBaseColor(int index, Color color)
        {
            _meshRenderer.GetPropertyBlock(_mpb, index);
            _mpb.SetColor("_BaseColor", color);
            _meshRenderer.SetPropertyBlock(_mpb, index);
        }

        /// <summary>True if the painted material(s) use the "Color Room/Paintable Lit" shader, which can play SetPaintReveal.</summary>
        public bool SupportsPaintReveal
        {
            get
            {
                EnsureInitialized();
                var materials = _meshRenderer.sharedMaterials;

                if (applyMode == ApplyMode.AllMaterials)
                {
                    foreach (var m in materials)
                    {
                        if (m == null || !m.HasProperty(PaintRadiusId))
                            return false;
                    }
                    return materials.Length > 0;
                }

                return materialIndex < materials.Length && materials[materialIndex] != null
                    && materials[materialIndex].HasProperty(PaintRadiusId);
            }
        }

        /// <summary>World-space bounds of the renderer.</summary>
        public Bounds WorldBounds
        {
            get
            {
                EnsureInitialized();
                return _meshRenderer.bounds;
            }
        }

        /// <summary>
        /// Shows the current color inside a sphere of the given radius around origin and the previous color outside it.
        /// A huge radius (see PaintRevealDone) means fully painted. Requires the Paintable Lit shader.
        /// </summary>
        public void SetPaintReveal(Color previousColor, Vector3 origin, float radius)
        {
            EnsureInitialized();

            if (applyMode == ApplyMode.AllMaterials)
            {
                for (int i = 0; i < _meshRenderer.sharedMaterials.Length; i++)
                    WritePaintReveal(i, previousColor, origin, radius);
            }
            else
            {
                WritePaintReveal(materialIndex, previousColor, origin, radius);
            }
        }

        /// <summary>Radius that makes the reveal cover the whole object (the shader's default).</summary>
        public const float PaintRevealDone = 10000f;

        private void WritePaintReveal(int index, Color previousColor, Vector3 origin, float radius)
        {
            _meshRenderer.GetPropertyBlock(_mpb, index);
            _mpb.SetColor(PrevColorId, previousColor);
            _mpb.SetVector(PaintOriginId, origin);
            _mpb.SetFloat(PaintRadiusId, radius);
            _meshRenderer.SetPropertyBlock(_mpb, index);
        }

        // Invokes OnPainted with LastPaintByPlayer set only for the duration of the call.
        // Also used by PaintableGroup to notify its members.
        internal void NotifyPainted(bool byPlayer)
        {
            LastPaintByPlayer = byPlayer;
            try { OnPainted?.Invoke(); }
            finally { LastPaintByPlayer = false; }
        }

        public void EnableOutline(Color color)
        {
            if (_outline == null)
                _outline = gameObject.AddComponent<Outline>();

            _outline.OutlineColor = color;
            _outline.OutlineWidth = 5f;
            _outline.enabled = true;
        }

        public void DisableOutline()
        {
            if (_outline != null)
                _outline.enabled = false;
        }
    }
}
