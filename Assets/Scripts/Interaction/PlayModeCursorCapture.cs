using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ColorRoomVR
{
    /// <summary>
    /// Hides and captures the mouse during play so UI interaction is controller-only.
    /// Press Escape to restore the cursor and mouse UI input.
    /// </summary>
    public sealed class PlayModeCursorCapture : MonoBehaviour
    {
        private XRUIInputModule _uiInputModule;
        private bool _releasedByUser;

        private void Start()
        {
            _uiInputModule = FindFirstObjectByType<XRUIInputModule>();
            CaptureCursor();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (!_releasedByUser && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                ReleaseCursor();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && !_releasedByUser)
                CaptureCursor();
        }

        private void OnDisable()
        {
            ReleaseCursor();
        }

        private void CaptureCursor()
        {
            if (_uiInputModule != null)
                _uiInputModule.enableMouseInput = false;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ReleaseCursor()
        {
            _releasedByUser = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (_uiInputModule != null)
                _uiInputModule.enableMouseInput = true;
        }
    }
}
