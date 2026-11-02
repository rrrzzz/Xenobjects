using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace Code.Tests
{
    /// <summary>
    /// The no-headset editor workflow: pressing Play without a headset hands the rig to the
    /// XR Device Simulator, and nothing in the museum logs an error while it runs.
    /// </summary>
    public class XrDeviceSimulatorTests
    {
        private GameObject _simulator;

        [UnityTest]
        public IEnumerator WithoutAHeadset_TheSimulatorTakesOver_Cleanly()
        {
            Assume.That(File.Exists(MuseumRig.ScenePath), "Run Xenobjects/VR/Rebuild Museum VR Scene first.");
            var rig = new MuseumRig();
            yield return rig.Load(true);

            for (var i = 0; i < 60; i++)
            {
                yield return null;
            }

            _simulator = GameObject.Find("XR Device Simulator");
            Assert.That(_simulator, Is.Not.Null, "simulator enabled when no headset is running");
            Assert.That(InputSystem.devices.Any(device => device is XRSimulatedHMD), "simulated headset registered");
            Assert.That(Camera.main, Is.SameAs(rig.Camera), "the XR camera is still the main camera");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Don't leave simulated devices behind for the tests that bring their own.
            if (_simulator)
                Object.Destroy(_simulator);

            yield return null;
            foreach (var device in InputSystem.devices.ToArray())
            {
                if (device is XRSimulatedHMD || device is XRSimulatedController)
                    InputSystem.RemoveDevice(device);
            }
        }
    }
}
