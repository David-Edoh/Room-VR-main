using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace ColorRoomVR
{
    /// <summary>
    /// The XRI device simulator keeps controller pitch level in FPS mode. Apply the
    /// headset's pitch to the simulated controller poses so their aim follows the view.
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public sealed class SimulatedControllerPitchFollow : MonoBehaviour
    {
        private XRDeviceSimulator _simulator;
        private Transform _head;
        private XRBaseController _leftController;
        private XRBaseController _rightController;

        private void Awake()
        {
            _simulator = FindFirstObjectByType<XRDeviceSimulator>();
            _head = Camera.main != null ? Camera.main.transform : GetComponentInChildren<Camera>(true)?.transform;

            foreach (var controller in GetComponentsInChildren<XRBaseController>(true))
            {
                if (controller.name.Contains("Left"))
                    _leftController = controller;
                else if (controller.name.Contains("Right"))
                    _rightController = controller;
            }
        }

        private void LateUpdate()
        {
            if (_simulator == null || !_simulator.manipulatingFPS || _head == null)
                return;

            var headEuler = _head.localEulerAngles;
            var pitch = Mathf.DeltaAngle(0f, headEuler.x);
            if (Mathf.Abs(pitch) < 0.01f)
                return;

            // Rotate about the headset's yawed right axis, matching the simulator's
            // pitch direction while leaving controller positions and yaw unchanged.
            var pitchAxis = Quaternion.Euler(0f, headEuler.y, 0f) * Vector3.right;
            var pitchRotation = Quaternion.AngleAxis(pitch, pitchAxis);
            ApplyPitch(_leftController, pitchRotation);
            ApplyPitch(_rightController, pitchRotation);
        }

        private static void ApplyPitch(XRBaseController controller, Quaternion pitchRotation)
        {
            if (controller == null)
                return;

            var state = controller.currentControllerState;
            state.rotation = pitchRotation * state.rotation;
            controller.currentControllerState = state;
            controller.transform.localRotation = state.rotation;
        }
    }
}
