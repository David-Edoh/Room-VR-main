using UnityEngine;
using UnityEngine.InputSystem;

namespace ColorRoomVR
{
    /// <summary>
    /// Keyboard locomotion for desktop XR simulator sessions.
    /// WASD moves the XR Origin in the direction the headset is facing.
    /// </summary>
    public sealed class KeyboardXROriginLocomotion : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float moveSpeed = 2f;
        [SerializeField, Min(1f)] private float fastMoveMultiplier = 2f;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            var input = Vector2.zero;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) input.x -= 1f;

            if (input.sqrMagnitude <= 0f)
                return;

            input = Vector2.ClampMagnitude(input, 1f);
            var forward = Camera.main != null ? Camera.main.transform.forward : transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f)
                forward = transform.forward;
            forward.Normalize();

            var right = Vector3.Cross(Vector3.up, forward).normalized;
            var speed = moveSpeed * (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed
                ? fastMoveMultiplier
                : 1f);
            transform.position += (forward * input.y + right * input.x) * (speed * Time.deltaTime);
        }
    }
}
