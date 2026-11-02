using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.OpenXR.Features.Interactions;
using InputCommonUsages = UnityEngine.InputSystem.CommonUsages;

namespace Code.Tests
{
    /// <summary>
    /// End-to-end checks of the generated VR museum with a simulated headset and controllers:
    /// every phone interaction, remapped to VR, still drives its effect on the right object.
    /// Any error or exception logged during a test fails it.
    /// </summary>
    public class MuseumVrSceneTests
    {
        private const int Object1 = 0;
        private const int Object2 = 1;
        private const int Object3 = 2;
        private const float FrameTime = 1f / 72f;

        private static readonly string[] ActionFields =
        {
            "puzzleRotationAction", "singleTouchAction", "doubleTouchAction", "leftHandPositionAction", "rightHandPositionAction"
        };

        private MuseumRig _rig;
        private SimulatedXr _xr;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assume.That(File.Exists(MuseumRig.ScenePath), "Run Xenobjects/VR/Rebuild Museum VR Scene first.");

            // Fixed 72 Hz frames (Quest 2) so motion and velocity are deterministic.
            Time.captureDeltaTime = FrameTime;
            _rig = new MuseumRig();
            yield return _rig.Load();
            _xr = new SimulatedXr();
            yield return Frames(3);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _xr?.Remove();
            _xr = null;
            Time.captureDeltaTime = 0f;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_HasOneXrCamera_AndNoFlatScreenLeftovers()
        {
            var enabledCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.enabled).ToList();
            Assert.That(enabledCameras, Has.Count.EqualTo(1), "exactly one active camera");
            Assert.That(Camera.main, Is.SameAs(_rig.Camera), "Camera.main is the XR camera");

            var flatCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.renderMode != RenderMode.WorldSpace)
                .Select(c => c.name)
                .ToList();
            Assert.That(flatCanvases, Is.Empty, "screen-space canvases are invisible in a headset");

            foreach (var typeName in new[] { "SimpleCharacterMotor", "MovementInteractionTestProvider", "StandaloneInputModule", "MouseLook" })
            {
                Assert.That(Probe.FindComponents(typeName), Is.Empty, typeName);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_WiresEachExhibitToItsOwnProvider()
        {
            Assert.That(_rig.ExhibitCount, Is.EqualTo(3));

            var managerTypes = Enumerable.Range(0, 3).Select(i => _rig.Manager(i).GetType().Name).ToArray();
            Assert.That(managerTypes, Is.EqualTo(new[] { "ArObject1Manager", "ArObject2Manager", "ArObject3Manager" }));

            var providers = Enumerable.Range(0, 3).Select(_rig.Provider).ToList();
            Assert.That(providers.Distinct().Count(), Is.EqualTo(3), "one provider per exhibit");
            foreach (var provider in providers)
            {
                Assert.That(provider.GetType().Name, Is.EqualTo("VRMovementInteractionDataProvider"));
                foreach (var field in ActionFields)
                {
                    var reference = Probe.Get<InputActionReference>(provider, field);
                    Assert.That(reference != null && reference.action != null, $"{provider.name}.{field} resolves to an action");
                }
            }

            Assert.That(Probe.Get<Component>(_rig.Focus, "pathVisualizer"), Is.Not.Null, "path visualizer wired");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Bindings_ResolveOnOculusTouchControllers()
        {
            var left = InputSystem.AddDevice<OculusTouchControllerProfile.OculusTouchController>("Touch Left");
            var right = InputSystem.AddDevice<OculusTouchControllerProfile.OculusTouchController>("Touch Right");
            InputSystem.SetDeviceUsage(left, InputCommonUsages.LeftHand);
            InputSystem.SetDeviceUsage(right, InputCommonUsages.RightHand);
            try
            {
                yield return null;
                var provider = _rig.Provider(Object1);
                AssertBoundTo(provider, "puzzleRotationAction", left);
                AssertBoundTo(provider, "singleTouchAction", left, right);
                AssertBoundTo(provider, "doubleTouchAction", right);
                AssertBoundTo(provider, "leftHandPositionAction", left);
                AssertBoundTo(provider, "rightHandPositionAction", right);
            }
            finally
            {
                InputSystem.RemoveDevice(left);
                InputSystem.RemoveDevice(right);
            }
        }

        [UnityTest]
        public IEnumerator Rig_StandsOnTheFloor()
        {
            var origin = _rig.Origin.transform.position;
            var hasFloor = Physics.Raycast(origin + Vector3.up * 0.5f, Vector3.down, out var hit, 2f, ~0, QueryTriggerInteraction.Ignore);

            Assert.That(hasFloor, "floor under the rig");
            Assert.That(hit.point.y, Is.EqualTo(origin.y).Within(0.05f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Locomotion_RightStickMoves_LeftStickDoesNotTurn()
        {
            _rig.StandInFront(_xr, Object2, 1.4f);
            yield return Frames(5);
            var startPosition = _rig.Origin.transform.position;
            var startYaw = _rig.Origin.transform.eulerAngles.y;

            _xr.SetStick(true, new Vector2(1f, 0f));
            yield return Frames(72);
            _xr.SetStick(true, Vector2.zero);
            Assert.That(Mathf.DeltaAngle(startYaw, _rig.Origin.transform.eulerAngles.y), Is.EqualTo(0f).Within(0.5f),
                "the left stick is the puzzle's rotation, not a turn");

            // Strafe right: stays within reach of Object 2, so its provider keeps tracking movement.
            _xr.SetStick(false, new Vector2(1f, 0f));
            yield return Frames(40);
            _xr.SetStick(false, Vector2.zero);
            Assert.That(Vector3.Distance(startPosition, _rig.Origin.transform.position), Is.GreaterThan(0.5f), "the right stick moves the rig");

            // Movement is sampled once per real second.
            var provider = _rig.Provider(Object2);
            var sawMoving = false;
            var deadline = Time.realtimeSinceStartup + 1.5f;
            while (!sawMoving && Time.realtimeSinceStartup < deadline)
            {
                sawMoving = Probe.Get<bool>(provider, "IsMoving");
                yield return null;
            }

            Assert.That(sawMoving, "IsMoving after walking with the stick");
        }

        [UnityTest]
        public IEnumerator WalkingToEachPodium_FocusesOnlyThatExhibit_AndStartsIt()
        {
            for (var i = 0; i < 3; i++)
            {
                _rig.StandInFront(_xr, i, 1.3f);
                yield return Frames(5);

                Assert.That(_rig.FocusedIndex, Is.EqualTo(i), $"focus at exhibit {i}");
                for (var j = 0; j < 3; j++)
                {
                    Assert.That(Probe.Get<bool>(_rig.Provider(j), "IsFocused"), Is.EqualTo(i == j), $"provider {j} while at exhibit {i}");
                }

                Assert.That(Probe.Get<bool>(_rig.Manager(i), "IsInitialized"), $"exhibit {i} starts on first approach");
            }

            // Between the podiums and well back from them: nothing has focus.
            _rig.StandAt(_xr, new Vector3(-2.5f, _rig.FloorY, -3.5f), new Vector3(-2.5f, 1f, 0f), MuseumRig.EyeHeight);
            yield return Frames(5);
            Assert.That(_rig.FocusedIndex, Is.EqualTo(-1));
        }

        [UnityTest]
        public IEnumerator HeadRoll_DrivesObject1Distortion()
        {
            _rig.StandInFront(_xr, Object1, 1.3f);
            yield return Frames(10);
            var provider = _rig.Provider(Object1);
            var capsule = Probe.Get<GameObject>(_rig.Manager(Object1), "capsuleObj").GetComponentInChildren<Renderer>().sharedMaterial;
            var levelDistortion = capsule.GetFloat("_Distortion");
            Assert.That(Probe.Get<float>(provider, "TiltZ01"), Is.EqualTo(0f).Within(0.02f), "level head");

            _rig.StandInFront(_xr, Object1, 1.3f, 25f);
            yield return Frames(10);
            var tilt = Probe.Get<float>(provider, "TiltZ01");
            Assert.That(tilt, Is.EqualTo(25f / Probe.Get<float>(provider, "maxTilt")).Within(0.03f), "25° roll");
            Assert.That(capsule.GetFloat("_Distortion"), Is.EqualTo(tilt * 150f).Within(1f));
            Assert.That(capsule.GetFloat("_Distortion"), Is.GreaterThan(levelDistortion + 20f));
        }

        [UnityTest]
        public IEnumerator HeadRoll_ChangesObject3FlameColour()
        {
            var glow = Probe.Get<ParticleSystem>(_rig.Manager(Object3), "objectCoreGlowPs");
            _rig.StandInFront(_xr, Object3, 1.3f);
            yield return Frames(10);
            var levelColour = glow.main.startColor.color;

            _rig.StandInFront(_xr, Object3, 1.3f, -30f);
            yield return Frames(10);
            var tiltedColour = glow.main.startColor.color;

            var difference = Mathf.Abs(levelColour.r - tiltedColour.r) + Mathf.Abs(levelColour.g - tiltedColour.g) +
                             Mathf.Abs(levelColour.b - tiltedColour.b);
            Assert.That(difference, Is.GreaterThan(0.2f), $"flame colour {levelColour} -> {tiltedColour}");
        }

        [UnityTest]
        public IEnumerator Distance_DrivesFogTornadoAndSmoke()
        {
            var fog = Probe.Get<ParticleSystem>(_rig.Manager(Object1), "fogParticleSystem");
            _rig.StandInFront(_xr, Object1, 0.9f);
            yield return Frames(5);
            var fogNear = fog.main.startSize.constant;
            _rig.StandInFront(_xr, Object1, 2.2f);
            yield return Frames(5);
            Assert.That(fog.main.startSize.constant, Is.GreaterThan(fogNear + 2f), "Object 1 fog grows with distance");

            var smoke = Probe.Get<ParticleSystem>(_rig.Manager(Object3), "smokePs");
            _rig.StandInFront(_xr, Object3, 1.1f);
            yield return Frames(5);
            var smokeNear = smoke.main.maxParticles;
            _rig.StandInFront(_xr, Object3, 2.3f);
            yield return Frames(5);
            Assert.That(smoke.main.maxParticles, Is.GreaterThan(smokeNear + 2000), "Object 3 smoke grows with distance");

            // Object 2 waits 4 real seconds after it starts before reacting at all.
            var tornado = Probe.Get<Transform>(_rig.Manager(Object2), "tornadoTransform");
            _rig.StandInFront(_xr, Object2, 1.1f);
            yield return new WaitForSecondsRealtime(4.5f);
            var tornadoNear = tornado.localScale.x;
            _rig.StandInFront(_xr, Object2, 2.3f);
            yield return Frames(5);
            Assert.That(tornado.localScale.x, Is.GreaterThan(tornadoNear + 3f), "Object 2 tornado grows with distance");
        }

        [UnityTest]
        public IEnumerator TapButtons_ToggleOnlyTheFocusedExhibit()
        {
            _rig.StandInFront(_xr, Object1, 1.3f);
            yield return Frames(5);
            yield return Press(false, ControllerButton.TriggerButton);
            Assert.That(Probe.Get<bool>(_rig.Manager(Object1), "_isTouchToggleOn"), Is.True, "right trigger: Object 1 distortion oscillation");

            _rig.StandInFront(_xr, Object2, 1.3f);
            yield return Frames(5);
            Assert.That(Probe.Get<bool>(_rig.Manager(Object2), "isBloodRotating"), Is.False, "earlier taps don't reach other exhibits");
            yield return Press(false, ControllerButton.TriggerButton);
            Assert.That(Probe.Get<bool>(_rig.Manager(Object2), "isBloodRotating"), Is.True, "right trigger: Object 2 blood rotation");
            Assert.That(Probe.Get<bool>(_rig.Manager(Object1), "_isTouchToggleOn"), Is.True, "Object 1 unchanged while at Object 2");

            _rig.StandInFront(_xr, Object3, 1.3f);
            yield return Frames(5);
            yield return Press(false, ControllerButton.SecondaryButton);
            Assert.That(Probe.Get<bool>(_rig.Manager(Object3), "_isOscillating"), Is.True, "B: Object 3 ribbon oscillation");

            _rig.StandInFront(_xr, Object1, 1.3f);
            yield return Frames(5);
            yield return Press(true, ControllerButton.TriggerButton);
            Assert.That(Probe.Get<bool>(_rig.Manager(Object1), "_isTouchToggleOn"), Is.False, "left trigger toggles Object 1 back");
        }

        [UnityTest]
        public IEnumerator DoubleTap_IsTheAButton()
        {
            _rig.StandInFront(_xr, Object1, 1.3f);
            yield return Frames(5);
            var doubleTaps = 0;
            Probe.Get<UnityEvent>(_rig.Provider(Object1), "DoubleTouchEvent").AddListener(() => doubleTaps++);

            yield return Press(false, ControllerButton.PrimaryButton);
            Assert.That(doubleTaps, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ShakingAController_PlaysObject1SwarmOnce()
        {
            _rig.StandInFront(_xr, Object1, 1.3f);
            yield return Frames(5);

            var provider = _rig.Provider(Object1);
            var swarm = Probe.Get<ParticleSystem>(_rig.Manager(Object1), "swarmParticles");
            var shakes = 0;
            var swarmStarted = false;
            Probe.Get<UnityEvent>(provider, "ShakeEvent").AddListener(() =>
            {
                shakes++;
                swarmStarted |= swarm.isPlaying;
            });

            // Hold the hand where the shake starts, so the first sample isn't a jump.
            var centre = new Vector3(0.25f, 1.1f, 0.35f);
            _xr.SetControllerPosition(false, centre + Vector3.left * 0.1f);
            yield return Frames(10);

            // Half a second of a 10 cm, 4 Hz shake (peaks around 2.5 m/s), then hold still.
            for (var frame = 0; frame < 36; frame++)
            {
                var t = frame * FrameTime;
                _xr.SetControllerPosition(false, centre + Vector3.left * (0.1f * Mathf.Cos(2f * Mathf.PI * 4f * t)));
                yield return null;
            }

            yield return Frames(72);

            Assert.That(shakes, Is.EqualTo(1), "one shake event");
            Assert.That(swarmStarted, "Object 1 swarm plays on shake");
        }

        [UnityTest]
        public IEnumerator StandingStill_StartsObject1PathSequence()
        {
            _rig.StandInFront(_xr, Object1, 1.3f);
            yield return Frames(5);
            var manager = _rig.Manager(Object1);
            Assert.That(Probe.Get<bool>(manager, "_isPathDrawingActive"), Is.False);

            // Idle time is measured in real seconds; the threshold is 3 s.
            yield return new WaitForSecondsRealtime(4f);

            Assert.That(Probe.Get<bool>(manager, "_isPathDrawingActive"), Is.True, "idling starts the path sequence");
        }

        [UnityTest]
        public IEnumerator Object1Path_CanBeTracedWithAHand()
        {
            _rig.StandInFront(_xr, Object1, 1.0f);
            yield return Frames(5);
            var manager = _rig.Manager(Object1);
            var path = Probe.Get<Component>(manager, "highResMiddleTentacleSpline");

            // Idling for 3 s starts the sequence; the path then pulses until the visitor reaches its start.
            yield return new WaitForSecondsRealtime(3.5f);
            yield return WaitFor(() => Probe.Get<bool>(manager, "_isMidPulsing"), 30f);
            Assert.That(Probe.Get<bool>(manager, "_isMidPulsing"), "the path pulses, waiting for the visitor");

            var nodeCount = Probe.Get<IList>(Probe.Get<object>(path, "_spline"), "nodes").Count;
            var nodes = new Vector3[nodeCount];
            for (var i = 0; i < nodeCount; i++)
            {
                nodes[i] = (Vector3)Probe.Call(path, "GetGlobalNodePos", i);
                Debug.Log($"[Object1Path] node {i}: {nodes[i]}, {nodes[i].y - _rig.FloorY:F2} m above the floor");
            }

            // Trace the path with the right hand while standing in front of the podium; the head
            // stays ~0.5 m above the path, so only the hand can reach the nodes.
            for (var i = 0; i < nodeCount && !Probe.Get<bool>(manager, "_isPathEndReached"); i++)
            {
                var node = i;
                _xr.SetControllerPosition(false, _rig.ToTrackingSpace(nodes[node]));
                yield return WaitFor(() => Probe.Get<int>(path, "_currentNodeToReach") > node ||
                                           Probe.Get<bool>(manager, "_isPathEndReached"), node == 0 ? 20f : 5f);
            }

            Assert.That(Probe.Get<bool>(manager, "_isPathEndReached"),
                $"path traced to its end by hand (reached node {Probe.Get<int>(path, "_currentNodeToReach")} of {nodeCount})");
        }

        [UnityTest]
        public IEnumerator GazeLockOn_ThenLeftStick_SolvesTheObject1Puzzle()
        {
            _rig.StandInFront(_xr, Object1, 1.3f);
            yield return Frames(5);
            var manager = _rig.Manager(Object1);
            var puzzleCollider = Probe.Get<Component>(manager, "puzzleTentacleSpline").GetComponent<Collider>();
            Assume.That(puzzleCollider, Is.Not.Null, "puzzle collider");

            Probe.Call(manager, "StartPuzzleTest");
            yield return Frames(80);

            Assert.That(TryFindGazeSpot(puzzleCollider, _rig.ExhibitRoot(Object1).position, out var head), "a head position that sees the puzzle");
            _rig.PlaceHead(_xr, head, puzzleCollider.bounds.center);
            yield return Frames(5);
            Assert.That(Probe.Get<bool>(manager, "_isFirstHit"), Is.False, "head gaze locks on to the puzzle");

            // Stick right lowers SignedTiltY01; the puzzle is solved when the angle returns to 0.
            var startAngle = Probe.Get<float>(manager, "_startPuzzleYRotation");
            _xr.SetStick(true, new Vector2(Mathf.Sign(startAngle), 0f));
            var frames = 0;
            while (!Probe.Get<bool>(manager, "_isPuzzleCompleted") && frames < 6 * 72)
            {
                frames++;
                yield return null;
            }

            _xr.SetStick(true, Vector2.zero);
            Assert.That(Probe.Get<bool>(manager, "_isPuzzleCompleted"), $"puzzle solved (start {startAngle:F0}°, {frames} frames)");
        }

        private static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        // Waits until the condition holds or the scaled-time timeout passes.
        private static IEnumerator WaitFor(System.Func<bool> condition, float timeoutSeconds)
        {
            var frames = Mathf.CeilToInt(timeoutSeconds / FrameTime);
            for (var i = 0; i < frames && !condition(); i++)
            {
                yield return null;
            }
        }

        private IEnumerator Press(bool isLeft, ControllerButton button)
        {
            _xr.SetButton(isLeft, button, true);
            yield return Frames(3);
            _xr.SetButton(isLeft, button, false);
            yield return Frames(3);
        }

        private static void AssertBoundTo(Component provider, string field, params InputDevice[] devices)
        {
            var action = Probe.Get<InputActionReference>(provider, field).action;
            foreach (var device in devices)
            {
                Assert.That(action.controls.Any(control => control.device == device),
                    $"{action.name} binds a control on {device.name} ({string.Join(", ", action.bindings.Select(b => b.path))})");
            }
        }

        // Head positions around the exhibit from which the head ray reaches the puzzle first.
        private bool TryFindGazeSpot(Collider target, Vector3 exhibit, out Vector3 head)
        {
            var centre = target.bounds.center;
            for (var radius = 0.9f; radius <= 1.51f; radius += 0.3f)
            {
                for (var height = 1.1f; height <= 1.81f; height += 0.35f)
                {
                    for (var angle = -80f; angle <= 80f; angle += 20f)
                    {
                        var candidate = new Vector3(exhibit.x, _rig.FloorY + height, exhibit.z) +
                                        Quaternion.Euler(0f, angle, 0f) * Vector3.back * radius;
                        if (Physics.Raycast(candidate, centre - candidate, out var hit, 5f) && hit.collider == target)
                        {
                            head = candidate;
                            return true;
                        }
                    }
                }
            }

            head = default;
            return false;
        }
    }
}
