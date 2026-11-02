using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using InputCommonUsages = UnityEngine.InputSystem.CommonUsages;

namespace Code.Tests
{
    /// <summary>
    /// A simulated headset and two controllers fed straight into the Input System — the same
    /// devices the XR Device Simulator creates — so tests can pose the head, move the hands and
    /// press buttons deterministically. Poses are in tracking space (relative to Camera Offset).
    /// </summary>
    internal sealed class SimulatedXr
    {
        private static readonly int Tracked = (int)(InputTrackingState.Position | InputTrackingState.Rotation);

        private readonly XRSimulatedHMD _hmd;
        private readonly XRSimulatedController _left;
        private readonly XRSimulatedController _right;
        private XRSimulatedHMDState _hmdState;
        private XRSimulatedControllerState _leftState;
        private XRSimulatedControllerState _rightState;

        public SimulatedXr()
        {
            _hmd = InputSystem.AddDevice<XRSimulatedHMD>("Test HMD");
            _left = InputSystem.AddDevice<XRSimulatedController>("Test Left Controller");
            _right = InputSystem.AddDevice<XRSimulatedController>("Test Right Controller");
            InputSystem.SetDeviceUsage(_left, InputCommonUsages.LeftHand);
            InputSystem.SetDeviceUsage(_right, InputCommonUsages.RightHand);

            _hmdState = new XRSimulatedHMDState
            {
                centerEyeRotation = Quaternion.identity,
                deviceRotation = Quaternion.identity,
                leftEyeRotation = Quaternion.identity,
                rightEyeRotation = Quaternion.identity,
                trackingState = Tracked,
                isTracked = true
            };
            _leftState = NewControllerState(new Vector3(-0.2f, 1.1f, 0.3f));
            _rightState = NewControllerState(new Vector3(0.2f, 1.1f, 0.3f));
            Push();
        }

        public void SetHeadPose(Vector3 position, Quaternion rotation)
        {
            _hmdState.centerEyePosition = position;
            _hmdState.centerEyeRotation = rotation;
            _hmdState.devicePosition = position;
            _hmdState.deviceRotation = rotation;
            _hmdState.leftEyePosition = position;
            _hmdState.leftEyeRotation = rotation;
            _hmdState.rightEyePosition = position;
            _hmdState.rightEyeRotation = rotation;
            InputSystem.QueueStateEvent(_hmd, _hmdState);
        }

        public void SetControllerPose(bool isLeft, Vector3 position, Quaternion rotation)
        {
            if (isLeft)
            {
                _leftState.devicePosition = position;
                _leftState.deviceRotation = rotation;
                InputSystem.QueueStateEvent(_left, _leftState);
            }
            else
            {
                _rightState.devicePosition = position;
                _rightState.deviceRotation = rotation;
                InputSystem.QueueStateEvent(_right, _rightState);
            }
        }

        public void SetControllerPosition(bool isLeft, Vector3 position)
        {
            var rotation = isLeft ? _leftState.deviceRotation : _rightState.deviceRotation;
            SetControllerPose(isLeft, position, rotation);
        }

        public void SetStick(bool isLeft, Vector2 value)
        {
            if (isLeft)
            {
                _leftState.primary2DAxis = value;
                InputSystem.QueueStateEvent(_left, _leftState);
            }
            else
            {
                _rightState.primary2DAxis = value;
                InputSystem.QueueStateEvent(_right, _rightState);
            }
        }

        public void SetButton(bool isLeft, ControllerButton button, bool isPressed)
        {
            if (isLeft)
            {
                _leftState = WithButton(_leftState, button, isPressed);
                InputSystem.QueueStateEvent(_left, _leftState);
            }
            else
            {
                _rightState = WithButton(_rightState, button, isPressed);
                InputSystem.QueueStateEvent(_right, _rightState);
            }
        }

        public void Remove()
        {
            InputSystem.RemoveDevice(_hmd);
            InputSystem.RemoveDevice(_left);
            InputSystem.RemoveDevice(_right);
        }

        private static XRSimulatedControllerState NewControllerState(Vector3 position)
        {
            return new XRSimulatedControllerState
            {
                devicePosition = position,
                deviceRotation = Quaternion.identity,
                trackingState = Tracked,
                isTracked = true
            };
        }

        private static XRSimulatedControllerState WithButton(XRSimulatedControllerState state, ControllerButton button, bool isPressed)
        {
            state = state.WithButton(button, isPressed);
            if (button == ControllerButton.TriggerButton)
                state.trigger = isPressed ? 1f : 0f;

            if (button == ControllerButton.GripButton)
                state.grip = isPressed ? 1f : 0f;

            return state;
        }

        private void Push()
        {
            InputSystem.QueueStateEvent(_hmd, _hmdState);
            InputSystem.QueueStateEvent(_left, _leftState);
            InputSystem.QueueStateEvent(_right, _rightState);
        }
    }
}
