using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace ColorRoomVR
{
    public class ObjectDetection : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Ray used to find paintable objects. Its raycast mask and max distance decide what can be hit.")]
        [SerializeField] private XRRayInteractor rayInteractor;
        [Tooltip("Line visual of the ray. While a paintable is hovered its color is tinted with the selected color. Defaults to the one on the ray interactor.")]
        [SerializeField] private XRInteractorLineVisual lineVisual;
        [Tooltip("Action that paints the hovered object (right hand trigger).")]
        [SerializeField] private InputActionProperty paintAction;
        [SerializeField] private PaintVFXManager vfxManager;
        [Tooltip("Optional. Animates the object's color change when painting.")]
        [SerializeField] private PaintEffectManager paintEffect;
        [SerializeField] private ColorPaletteController palette;
        [Header("Haptics")]
        [Tooltip("Haptic pulse sent to the painting controller when the player paints.")]
        [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.5f;
        [SerializeField, Min(0f)] private float hapticDuration = 0.1f;
#if UNITY_EDITOR
        [Tooltip("Editor only: raycast from the mouse and paint with left click instead of the controller.")]
        [SerializeField] private bool mouseFallbackInEditor;
#endif

        private PaintableObject _hoveredObject;
        private PaintableGroup _hoveredGroup;
        private Gradient _originalInvalidGradient;
        private bool _lineTinted;
        private InteriorDesignManager _designer;
        private XRDeviceSimulator _deviceSimulator;

        private void Awake()
        {
            _designer = InteriorDesignManager.Instance;
            _deviceSimulator = FindFirstObjectByType<XRDeviceSimulator>();
            if (lineVisual == null && rayInteractor != null)
                lineVisual = rayInteractor.GetComponent<XRInteractorLineVisual>();
            if (lineVisual != null)
                _originalInvalidGradient = CopyGradient(lineVisual.invalidColorGradient);
        }

        private void OnEnable()
        {
            paintAction.action?.Enable();
            if (palette != null)
                palette.OnColorChange.AddListener(OnPaletteColorChanged);
        }

        private void OnDisable()
        {
            paintAction.action?.Disable();
            if (palette != null)
                palette.OnColorChange.RemoveListener(OnPaletteColorChanged);
            ClearHovered();
        }

        /// <summary>When false, nothing is hovered, outlined or painted (e.g. during the first tutorial steps).</summary>
        public bool PaintingEnabled
        {
            get => _paintingEnabled;
            set
            {
                _paintingEnabled = value;
                if (!value) ClearHovered();
            }
        }

        private bool _paintingEnabled = true;

        private void Update()
        {
            if (!_paintingEnabled)
                return;

            _designer ??= InteriorDesignManager.Instance;
            if (!TryGetHit(out RaycastHit hit, out Ray ray, out bool pressed, out bool held, out bool released))
            {
                if (_designer != null && _designer.CurrentTool == InteriorDesignManager.Tool.Move)
                {
                    if (held) _designer.UpdateMove(GetPointerRay());
                    if (released) _designer.EndMove();
                }
                ClearHovered();
                return;
            }

            if (!hit.collider.TryGetComponent(out PaintableObject obj))
            {
                if (_designer != null && _designer.CurrentTool == InteriorDesignManager.Tool.Move)
                {
                    if (held) _designer.UpdateMove(ray);
                    if (released) _designer.EndMove();
                }
                ClearHovered();
                return;
            }

            UpdateHover(obj);
            if (_designer != null && _designer.CurrentTool == InteriorDesignManager.Tool.Move)
            {
                if (pressed) _designer.BeginMove(obj, ray, hit.point);
                if (held) _designer.UpdateMove(ray);
                if (released) _designer.EndMove();
            }
            else if (pressed)
            {
                if (_designer != null && _designer.CurrentTool == InteriorDesignManager.Tool.Texture)
                    _designer.ApplyTexture(obj);
                else
                    Paint(hit);
            }
        }

        private bool TryGetHit(out RaycastHit hit, out Ray ray, out bool pressed, out bool held, out bool released)
        {
            hit = default;
            ray = default;
            pressed = held = released = false;

#if UNITY_EDITOR
            if (ShouldUseMouseFallback() && Mouse.current != null && Camera.main != null)
            {
                ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                pressed = Mouse.current.leftButton.wasPressedThisFrame;
                held = Mouse.current.leftButton.isPressed;
                released = Mouse.current.leftButton.wasReleasedThisFrame;
                if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    return false;
                return Physics.Raycast(ray, out hit, Mathf.Infinity, ~0, QueryTriggerInteraction.Ignore);
            }
#endif
            pressed = paintAction.action != null && paintAction.action.WasPressedThisFrame();
            held = paintAction.action != null && paintAction.action.IsPressed();
            released = paintAction.action != null && paintAction.action.WasReleasedThisFrame();
            if (rayInteractor == null || rayInteractor.IsOverUIGameObject())
                return false;

            ray = new Ray(rayInteractor.transform.position, rayInteractor.transform.forward);
            return rayInteractor.TryGetCurrent3DRaycastHit(out hit);
        }

        private Ray GetPointerRay()
        {
#if UNITY_EDITOR
            if (ShouldUseMouseFallback() && Mouse.current != null && Camera.main != null)
                return Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
#endif
            return rayInteractor == null ? new Ray() : new Ray(rayInteractor.transform.position, rayInteractor.transform.forward);
        }

#if UNITY_EDITOR
        private bool ShouldUseMouseFallback()
        {
            // The XR Device Simulator creates simulated controllers without activating an XR runtime.
            // In that case, prefer the simulated controller ray unless mouse fallback was explicitly requested.
            var simulatorActive = _deviceSimulator != null && _deviceSimulator.isActiveAndEnabled;
            return mouseFallbackInEditor || (!XRSettings.isDeviceActive && !simulatorActive);
        }
#endif

        private void UpdateHover(PaintableObject obj)
        {
            var group = obj.Group;
            if (group != null)
            {
                if (group == _hoveredGroup) return;
                ClearHovered();
                _hoveredGroup = group;
                _hoveredObject = obj;
                group.EnableOutline(palette.SelectedColor);
            }
            else if (obj != _hoveredObject || _hoveredGroup != null)
            {
                ClearHovered();
                _hoveredObject = obj;
                obj.EnableOutline(palette.SelectedColor);
            }
            else
            {
                return;
            }

            SetLineColor(palette.SelectedColor);
        }

        private void Paint(RaycastHit hit)
        {
            if (_hoveredGroup == null && _hoveredObject == null)
                return;

            paintEffect?.Capture(_hoveredObject, _hoveredGroup);

            if (_hoveredGroup != null)
                _hoveredGroup.SetColor(palette.SelectedColor);
            else
                _hoveredObject.SetColor(palette.SelectedColor);

            paintEffect?.Play(palette.SelectedColor, hit.point);
            vfxManager?.PlayAt(hit.point, hit.normal, palette.SelectedColor);
            SendPaintHaptic();
            AudioManager.Instance?.PlayPaint();
            ClearHovered();
        }

        // Only sends the pulse when a real right-hand XR device supports haptics. Skipping otherwise avoids
        // XRI's "Failed to get haptic capabilities" warning for simulated controllers (no XR device) and no-headset runs.
        private void SendPaintHaptic()
        {
            if (rayInteractor == null || hapticDuration <= 0f)
                return;

            var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!device.isValid || !device.TryGetHapticCapabilities(out var caps) || !caps.supportsImpulse)
                return;

            rayInteractor.SendHapticImpulse(hapticAmplitude, hapticDuration);
        }

        private void OnPaletteColorChanged(Color color)
        {
            if (_hoveredGroup != null)
                _hoveredGroup.EnableOutline(color);
            else if (_hoveredObject != null)
                _hoveredObject.EnableOutline(color);
            else
                return;

            SetLineColor(color);
        }

        private void ClearHovered()
        {
            if (_hoveredObject != null) _hoveredObject.DisableOutline();
            if (_hoveredGroup != null) _hoveredGroup.DisableOutline();
            _hoveredObject = null;
            _hoveredGroup = null;
            RestoreLineColor();
        }

        // The line only turns "valid" over XR interactables or UI, so paintables would show the invalid (red) color.
        // Tint the invalid gradient with the selected color instead, as a preview of what will be painted.
        private void SetLineColor(Color color)
        {
            if (lineVisual == null) return;

            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            lineVisual.invalidColorGradient = gradient;
            _lineTinted = true;
        }

        private void RestoreLineColor()
        {
            if (!_lineTinted || lineVisual == null) return;

            lineVisual.invalidColorGradient = _originalInvalidGradient;
            _lineTinted = false;
        }

        private static Gradient CopyGradient(Gradient source)
        {
            var copy = new Gradient { mode = source.mode };
            copy.SetKeys(source.colorKeys, source.alphaKeys);
            return copy;
        }
    }
}
