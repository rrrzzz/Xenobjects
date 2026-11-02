using UnityEngine;
using UnityEngine.XR;

namespace Code.VR
{
    /// <summary>
    /// Enables the XR Device Simulator only in the editor and only when no headset is running,
    /// so the same scene works over Quest Link and with mouse and keyboard. Keep the simulator
    /// object inactive in the scene; this decides at startup.
    /// </summary>
    public class XrDeviceSimulatorToggle : MonoBehaviour
    {
        [SerializeField] private GameObject simulator;

        private void Start()
        {
            if (!simulator)
                return;

            var useSimulator = Application.isEditor && !XRSettings.isDeviceActive;
            simulator.SetActive(useSimulator);
            if (useSimulator)
                Debug.Log("[Xenobjects] No headset running - XR Device Simulator enabled.");
        }
    }
}
