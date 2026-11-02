using System.Collections;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Code.Tests
{
    /// <summary>
    /// Loads the generated VR museum and places the simulated visitor in it. Exhibit indices
    /// follow the builder: 0 = Object 1, 1 = Object 2, 2 = Object 3.
    /// </summary>
    internal sealed class MuseumRig
    {
        public const string ScenePath = "Assets/Scenes/MuseumSimulationVR.unity";
        public const float EyeHeight = 1.6f;

        public XROrigin Origin { get; private set; }
        public Component Focus { get; private set; }
        public float FloorY { get; private set; }
        public Camera Camera => Origin.Camera;

        public int ExhibitCount => Probe.Get<int>(Focus, "ExhibitCount");
        public int FocusedIndex => Probe.Get<int>(Focus, "FocusedIndex");

        /// <param name="keepSimulator">
        /// Leave the XR Device Simulator toggle alone. By default it is disabled: the simulator
        /// would add its own HMD and controllers, and tests bring their own.
        /// </param>
        public IEnumerator Load(bool keepSimulator = false)
        {
            if (!keepSimulator)
                SceneManager.sceneLoaded += DisableSimulatorToggle;

#if UNITY_EDITOR
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            SceneManager.sceneLoaded -= DisableSimulatorToggle;

            Origin = Object.FindAnyObjectByType<XROrigin>();
            Focus = Probe.FindComponent("VrExhibitFocus");
            FloorY = Origin.transform.position.y;
        }

        public Transform ExhibitRoot(int index)
        {
            return Probe.Get<Transform>(Exhibit(index), "objectRoot");
        }

        public Component Provider(int index)
        {
            return Probe.Get<Component>(Exhibit(index), "provider");
        }

        public Component Manager(int index)
        {
            return ExhibitRoot(index)
                .GetComponentsInChildren<Component>(true)
                .First(component => component && component.GetType().BaseType?.Name == "ArObjectManagerBase");
        }

        /// <summary>Stands the visitor <paramref name="distance"/> m in front of an exhibit, looking at it.</summary>
        public void StandInFront(SimulatedXr xr, int exhibit, float distance, float rollDegrees = 0f)
        {
            var target = ExhibitRoot(exhibit).position;
            StandAt(xr, new Vector3(target.x, FloorY, target.z - distance), target, EyeHeight, rollDegrees);
        }

        /// <summary>Moves the rig to <paramref name="standOn"/> and poses the head above it.</summary>
        public void StandAt(SimulatedXr xr, Vector3 standOn, Vector3 lookAt, float eyeHeight, float rollDegrees = 0f)
        {
            Origin.transform.position = new Vector3(standOn.x, FloorY, standOn.z);
            Physics.SyncTransforms();

            var cameraOffset = Origin.CameraFloorOffsetObject.transform;
            var head = Origin.transform.position + Vector3.up * eyeHeight;
            var rotation = Quaternion.LookRotation(lookAt - head) * Quaternion.Euler(0f, 0f, rollDegrees);
            xr.SetHeadPose(cameraOffset.InverseTransformPoint(head), Quaternion.Inverse(cameraOffset.rotation) * rotation);
        }

        /// <summary>Puts the head at a world position (rig directly below it), looking at a point.</summary>
        public void PlaceHead(SimulatedXr xr, Vector3 headWorld, Vector3 lookAt)
        {
            StandAt(xr, new Vector3(headWorld.x, FloorY, headWorld.z), lookAt, headWorld.y - FloorY);
        }

        /// <summary>Converts a world position to the tracking space controllers are posed in.</summary>
        public Vector3 ToTrackingSpace(Vector3 world)
        {
            return Origin.CameraFloorOffsetObject.transform.InverseTransformPoint(world);
        }

        private object Exhibit(int index)
        {
            return Probe.Call(Focus, "GetExhibit", index);
        }

        private static void DisableSimulatorToggle(Scene scene, LoadSceneMode mode)
        {
            foreach (var toggle in Probe.FindComponents("XrDeviceSimulatorToggle"))
            {
                ((Behaviour)toggle).enabled = false;
            }
        }
    }
}
