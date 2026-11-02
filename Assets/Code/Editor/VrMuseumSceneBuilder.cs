using System;
using System.Collections.Generic;
using System.Linq;
using Code.VR;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using TrackedPoseDriver = UnityEngine.InputSystem.XR.TrackedPoseDriver;
using Object = UnityEngine.Object;

namespace Code.EditorTools
{
    /// <summary>
    /// Regenerates the VR museum (MuseumSimulationVR) from the keyboard museum (MuseumSimulation).
    /// Every exhibit keeps its scene overrides; the keyboard player, test provider and
    /// screen-space UI are replaced by an XR rig, one VR provider per exhibit, a wrist debug HUD
    /// and a gaze reticle for the Object 1 puzzle. Re-run after editing MuseumSimulation.
    /// </summary>
    public static class VrMuseumSceneBuilder
    {
        public const string SourceScenePath = "Assets/Scenes/MuseumSimulation.unity";
        public const string VrScenePath = "Assets/Scenes/MuseumSimulationVR.unity";

        private const string XrOriginPrefabPath = "Assets/Prefabs/VR/XR Origin (VR).prefab";
        private const string XriSamplesPath = "Assets/Samples/XR Interaction Toolkit/3.4.1";
        private const string XriActionsPath = XriSamplesPath + "/Starter Assets/XRI Default Input Actions.inputactions";
        private const string LeftControllerModelPath = XriSamplesPath + "/Starter Assets/Prefabs/Controllers/XR Controller Left.prefab";
        private const string RightControllerModelPath = XriSamplesPath + "/Starter Assets/Prefabs/Controllers/XR Controller Right.prefab";
        private const string SimulatorPrefabPath = XriSamplesPath + "/XR Device Simulator/XR Device Simulator.prefab";
        private const string VrActionsPath = "Assets/Code/VR/XenobjectsVRInput.inputactions";
        private const string ReticleMaterialPath = "Assets/Code/VR/GazeReticle.mat";
        private const string VrPostProcessProfilePath = "Assets/Code/VR/MuseumVRPostProcessProfile.asset";
        private const string VrActionMap = "Xenobjects VR";

        // Wrist HUD placement relative to the left controller, its canvas size and width in metres.
        private static readonly Vector3 HudLocalPosition = new Vector3(0f, 0.1f, 0f);
        private static readonly Vector3 HudLocalEuler = new Vector3(40f, 0f, 0f);
        private static readonly Vector2 HudSizePixels = new Vector2(820f, 420f);
        private const float HudWidthMeters = 0.26f;
        private const float ReticleDiameter = 0.015f;

        private const string LogPrefix = "[VrMuseumSceneBuilder]";

        [MenuItem("Xenobjects/VR/Rebuild Museum VR Scene")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Build();
        }

        /// <summary>Batch entry: -executeMethod Code.EditorTools.VrMuseumSceneBuilder.BuildFromCommandLine</summary>
        public static void BuildFromCommandLine()
        {
            Build();
        }

        public static void Build()
        {
            var source = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(source, VrScenePath, true))
                throw new InvalidOperationException($"{LogPrefix} Could not save {VrScenePath}.");

            var scene = EditorSceneManager.OpenScene(VrScenePath, OpenSceneMode.Single);
            var exhibits = FindExhibits(scene);
            var exhibitRoots = exhibits.Select(e => e.Root).ToArray();

            // Capture what the keyboard rig and test provider carried before removing them.
            var motor = FindAll<SimpleCharacterMotor>(scene).FirstOrDefault();
            if (!motor)
                throw new InvalidOperationException($"{LogPrefix} No keyboard player (SimpleCharacterMotor) in {SourceScenePath}.");

            var playerPosition = motor.transform.position;
            var playerYaw = Quaternion.Euler(0f, motor.transform.eulerAngles.y, 0f);
            var playerCamera = motor.GetComponentInChildren<Camera>(true);

            var testProvider = FindAll<MovementInteractionTestProvider>(scene).FirstOrDefault();
            if (!testProvider)
                throw new InvalidOperationException($"{LogPrefix} No MovementInteractionTestProvider in {SourceScenePath}.");

            var pathVisualizer = testProvider.movementPathVisualizer
                ? testProvider.movementPathVisualizer
                : FindAll<MovementPathVisualizer>(scene).FirstOrDefault();
            var providerSettings = ReadProviderSettings(testProvider);

            // The rig copies the keyboard camera's look, so it is created before the flat-screen
            // objects go; everything added after the cleanup is safe from it.
            var xrOrigin = CreateXrOrigin(scene, playerPosition, playerYaw, playerCamera, motor.gameObject);
            var xrCamera = xrOrigin.Camera;
            RemoveFlatscreenObjects(scene, exhibitRoots, motor.gameObject, testProvider.gameObject);

            var leftController = CreateController(xrOrigin, "Left Controller", "XRI Left", LeftControllerModelPath);
            var rightController = CreateController(xrOrigin, "Right Controller", "XRI Right", RightControllerModelPath);

            var providers = CreateProviders(scene, exhibits, providerSettings, leftController, rightController, out var focus);
            ConfigureFocus(focus, exhibits, providers, pathVisualizer);
            CreateDebugHud(leftController, focus, xrCamera);
            CreateGazeReticle(scene, exhibits);
            CreateDeviceSimulator(scene);

            foreach (var exhibit in exhibits)
            {
                if (!exhibit.Root.gameObject.activeSelf)
                {
                    exhibit.Root.gameObject.SetActive(true);
                    Debug.Log($"{LogPrefix} Activated {exhibit.Root.name} (inactive in the source scene).");
                }
            }

            AddToBuildSettings(VrScenePath);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"{LogPrefix} Could not save {VrScenePath}.");

            Debug.Log($"{LogPrefix} Built {VrScenePath}: XR Origin at {xrOrigin.transform.position} facing {xrOrigin.transform.forward}, " +
                      $"{exhibits.Count} exhibits ({string.Join(", ", exhibits.Select(e => $"{e.Label}={e.Root.name}@{e.Root.position}"))}).");
        }

        private sealed class ExhibitInfo
        {
            public string Label;
            public Transform Root;
            public ArObjectManagerBase Manager;
        }

        // Tunables the VR providers inherit from the keyboard test provider.
        private static readonly string[] ProviderSettingNames =
        {
            "maxTilt", "maxTiltY", "maxDistance", "minDistance", "movementTrackingInterval", "movementThreshold"
        };

        private static Dictionary<string, float> ReadProviderSettings(MovementInteractionProviderBase provider)
        {
            var serialized = new SerializedObject(provider);
            return ProviderSettingNames.ToDictionary(name => name, name => serialized.FindProperty(name).floatValue);
        }

        private static List<ExhibitInfo> FindExhibits(Scene scene)
        {
            var exhibits = new List<ExhibitInfo>
            {
                FindExhibit<ArObject1Manager>(scene, "Object 1"),
                FindExhibit<ArObject2Manager>(scene, "Object 2"),
                FindExhibit<ArObject3Manager>(scene, "Object 3")
            };
            return exhibits;
        }

        // An exhibit is a top-level prefab instance holding the object's manager; test copies
        // nested under other objects (TestObjects) are skipped, active instances win.
        private static ExhibitInfo FindExhibit<T>(Scene scene, string label) where T : ArObjectManagerBase
        {
            var manager = FindAll<T>(scene)
                .Where(m => PrefabUtility.IsOutermostPrefabInstanceRoot(m.transform.root.gameObject))
                .OrderByDescending(m => m.gameObject.activeInHierarchy)
                .ThenByDescending(m => m.transform.root.gameObject.activeSelf)
                .FirstOrDefault();

            if (!manager)
                throw new InvalidOperationException($"{LogPrefix} No top-level {typeof(T).Name} exhibit in {SourceScenePath}.");

            return new ExhibitInfo
            {
                Label = label,
                Root = manager.transform.root,
                Manager = manager
            };
        }

        private static XROrigin CreateXrOrigin(Scene scene, Vector3 playerPosition, Quaternion playerYaw, Camera playerCamera,
            GameObject playerRoot)
        {
            // Raycast for the floor before the rig exists (its CharacterController is a collider too)
            // and ignore the keyboard player's own controller.
            playerRoot.SetActive(false);
            Physics.SyncTransforms();
            var floorY = playerPosition.y;
            if (Physics.Raycast(playerPosition + Vector3.up * 2f, Vector3.down, out var hit, 50f, ~0, QueryTriggerInteraction.Ignore))
            {
                floorY = hit.point.y;
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} No floor found under the player; placing the rig at y={floorY}.");
            }

            var prefab = LoadAsset<GameObject>(XrOriginPrefabPath);
            var originObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            originObject.name = "XR Origin (VR)";
            originObject.transform.SetPositionAndRotation(new Vector3(playerPosition.x, floorY, playerPosition.z), playerYaw);

            var xrOrigin = originObject.GetComponent<XROrigin>();
            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            PrefabUtility.RecordPrefabInstancePropertyModifications(xrOrigin);

            // Stick turning stays off, as in VRTest: the left stick is the puzzle's "phone rotation".
            var turnProvider = originObject.GetComponent<ContinuousTurnProvider>();
            if (turnProvider)
            {
                turnProvider.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(turnProvider);
            }

            var actionManager = originObject.GetComponent<InputActionManager>();
            var vrActions = LoadAsset<InputActionAsset>(VrActionsPath);
            if (!actionManager.actionAssets.Contains(vrActions))
            {
                actionManager.actionAssets.Add(vrActions);
                PrefabUtility.RecordPrefabInstancePropertyModifications(actionManager);
            }

            if (playerCamera)
                CopyCameraLook(playerCamera, xrOrigin.Camera, scene);

            return xrOrigin;
        }

        // Carries the keyboard camera's look (skybox, HDR, clip planes, bloom) over to the XR camera.
        private static void CopyCameraLook(Camera from, Camera to, Scene scene)
        {
            to.clearFlags = from.clearFlags;
            to.backgroundColor = from.backgroundColor;
            to.allowHDR = from.allowHDR;
            to.allowMSAA = true;
            to.nearClipPlane = from.nearClipPlane;
            to.farClipPlane = from.farClipPlane;
            PrefabUtility.RecordPrefabInstancePropertyModifications(to);

            var fromLayer = from.GetComponent<PostProcessLayer>();
            if (fromLayer)
            {
                var layer = to.gameObject.AddComponent<PostProcessLayer>();
                EditorUtility.CopySerialized(fromLayer, layer);
                layer.volumeTrigger = to.transform;
                // TAA jitters in a headset; FXAA is cheap and works with single-pass instanced.
                layer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
            }

            var fromVolume = from.GetComponent<PostProcessVolume>();
            if (fromVolume)
            {
                var volumeObject = new GameObject("Post Process Volume (from Player 1)");
                SceneManager.MoveGameObjectToScene(volumeObject, scene);
                volumeObject.layer = from.gameObject.layer;
                var volume = volumeObject.AddComponent<PostProcessVolume>();
                EditorUtility.CopySerialized(fromVolume, volume);
                volume.sharedProfile = GetOrCreateVrProfile(fromVolume.sharedProfile);
            }
        }

        // The keyboard camera's profile is shared with other scenes, so VR gets its own copy without
        // ambient occlusion: SAO needs a depth-normals pass that is costly on Quest and isn't
        // reliable with single-pass instanced. Created once; later edits to the copy are kept.
        private static PostProcessProfile GetOrCreateVrProfile(PostProcessProfile source)
        {
            var profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(VrPostProcessProfilePath);
            if (profile || !source)
                return profile ? profile : source;

            AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), VrPostProcessProfilePath);
            profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(VrPostProcessProfilePath);
            if (profile.TryGetSettings(out AmbientOcclusion ambientOcclusion))
            {
                ambientOcclusion.enabled.Override(false);
                EditorUtility.SetDirty(ambientOcclusion);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            }

            return profile;
        }

        private static List<VRMovementInteractionDataProvider> CreateProviders(Scene scene, List<ExhibitInfo> exhibits,
            Dictionary<string, float> settings, Transform leftHand, Transform rightHand, out VrExhibitFocus focus)
        {
            var root = new GameObject("VR Interaction");
            SceneManager.MoveGameObjectToScene(root, scene);
            focus = root.AddComponent<VrExhibitFocus>();

            var actions = LoadActionReferences(VrActionsPath);
            var providers = new List<VRMovementInteractionDataProvider>();
            foreach (var exhibit in exhibits)
            {
                var providerObject = new GameObject($"VR Provider - {exhibit.Label}");
                providerObject.transform.SetParent(root.transform, false);
                var provider = providerObject.AddComponent<VRMovementInteractionDataProvider>();

                var serialized = new SerializedObject(provider);
                foreach (var setting in settings)
                {
                    serialized.FindProperty(setting.Key).floatValue = setting.Value;
                }

                serialized.FindProperty("isDebugInfoShown").boolValue = false;
                serialized.FindProperty("puzzleRotationAction").objectReferenceValue = actions[VrActionMap + "/Puzzle Rotate"];
                serialized.FindProperty("singleTouchAction").objectReferenceValue = actions[VrActionMap + "/Single Touch"];
                serialized.FindProperty("doubleTouchAction").objectReferenceValue = actions[VrActionMap + "/Double Touch"];
                serialized.FindProperty("leftHandPositionAction").objectReferenceValue = actions[VrActionMap + "/Left Hand Position"];
                serialized.FindProperty("rightHandPositionAction").objectReferenceValue = actions[VrActionMap + "/Right Hand Position"];
                serialized.FindProperty("leftHand").objectReferenceValue = leftHand;
                serialized.FindProperty("rightHand").objectReferenceValue = rightHand;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                providers.Add(provider);
            }

            return providers;
        }

        private static void ConfigureFocus(VrExhibitFocus focus, List<ExhibitInfo> exhibits,
            List<VRMovementInteractionDataProvider> providers, MovementPathVisualizer pathVisualizer)
        {
            var serialized = new SerializedObject(focus);
            var array = serialized.FindProperty("exhibits");
            array.arraySize = exhibits.Count;
            for (var i = 0; i < exhibits.Count; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("label").stringValue = exhibits[i].Label;
                element.FindPropertyRelative("objectRoot").objectReferenceValue = exhibits[i].Root;
                element.FindPropertyRelative("provider").objectReferenceValue = providers[i];
            }

            serialized.FindProperty("pathVisualizer").objectReferenceValue = pathVisualizer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Transform CreateController(XROrigin xrOrigin, string name, string actionMap, string modelPrefabPath)
        {
            var actions = LoadActionReferences(XriActionsPath);
            var controller = new GameObject(name);
            controller.transform.SetParent(xrOrigin.CameraFloorOffsetObject.transform, false);

            var poseDriver = controller.AddComponent<TrackedPoseDriver>();
            poseDriver.positionInput = new InputActionProperty(actions[actionMap + "/Position"]);
            poseDriver.rotationInput = new InputActionProperty(actions[actionMap + "/Rotation"]);
            poseDriver.trackingStateInput = new InputActionProperty(actions[actionMap + "/Tracking State"]);

            var model = (GameObject)PrefabUtility.InstantiatePrefab(LoadAsset<GameObject>(modelPrefabPath), controller.transform);
            model.name = name + " Model";
            return controller.transform;
        }

        // A small dark panel on the left wrist: one readout text plus a line for logged errors.
        private static void CreateDebugHud(Transform leftController, VrExhibitFocus focus, Camera xrCamera)
        {
            var hudObject = new GameObject("Wrist Debug HUD", typeof(RectTransform), typeof(Canvas));
            hudObject.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)hudObject.transform;
            rect.SetParent(leftController, false);
            rect.sizeDelta = HudSizePixels;
            rect.anchoredPosition3D = HudLocalPosition;
            rect.localRotation = Quaternion.Euler(HudLocalEuler);
            rect.localScale = Vector3.one * (HudWidthMeters / HudSizePixels.x);

            var canvas = hudObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = xrCamera;

            var background = CreateUiChild<Image>(hudObject.transform, "Background", Vector2.zero, Vector2.one, 0f);
            background.color = new Color(0.05f, 0.05f, 0.07f, 0.8f);
            background.raycastTarget = false;

            var readout = CreateUiChild<TextMeshProUGUI>(hudObject.transform, "Readout", new Vector2(0f, 0.25f), Vector2.one, 18f);
            readout.fontSize = 30f;
            readout.color = Color.white;
            readout.alignment = TextAlignmentOptions.TopLeft;
            readout.raycastTarget = false;
            readout.text = "Waiting for an exhibit...";

            var errors = CreateUiChild<TextMeshProUGUI>(hudObject.transform, "Errors", Vector2.zero, new Vector2(1f, 0.25f), 18f);
            errors.fontSize = 20f;
            errors.color = new Color(1f, 0.55f, 0.5f);
            errors.alignment = TextAlignmentOptions.BottomLeft;
            errors.raycastTarget = false;
            errors.text = string.Empty;

            var hud = hudObject.AddComponent<VrDebugHud>();
            var serialized = new SerializedObject(hud);
            serialized.FindProperty("focus").objectReferenceValue = focus;
            serialized.FindProperty("readout").objectReferenceValue = readout;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var logger = hudObject.AddComponent<ARDebugLogger>();
            logger.consoleOutput = errors;
        }

        private static T CreateUiChild<T>(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float padding)
            where T : Component
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.layer = parent.gameObject.layer;
            var rect = (RectTransform)child.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            return child.AddComponent<T>();
        }

        private static void CreateGazeReticle(Scene scene, List<ExhibitInfo> exhibits)
        {
            var object1 = exhibits.Select(e => e.Manager).OfType<ArObject1Manager>().First();
            var puzzleCollider = object1.puzzleTentacleSpline ? object1.puzzleTentacleSpline.GetComponent<Collider>() : null;
            if (!puzzleCollider)
            {
                Debug.LogWarning($"{LogPrefix} Object 1 puzzle has no collider; skipping the gaze reticle.");
                return;
            }

            var reticle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            reticle.name = "Gaze Reticle";
            SceneManager.MoveGameObjectToScene(reticle, scene);
            // No collider: the provider's lock-on ray must not hit the reticle itself.
            Object.DestroyImmediate(reticle.GetComponent<Collider>());
            reticle.transform.localScale = Vector3.one * ReticleDiameter;
            reticle.GetComponent<MeshRenderer>().sharedMaterial = GetOrCreateReticleMaterial();
            reticle.SetActive(false);

            var gazeObject = new GameObject("Gaze Reticle Driver");
            SceneManager.MoveGameObjectToScene(gazeObject, scene);
            var gaze = gazeObject.AddComponent<VrGazeReticle>();
            var serialized = new SerializedObject(gaze);
            serialized.FindProperty("reticle").objectReferenceValue = reticle.transform;
            var targets = serialized.FindProperty("targets");
            targets.arraySize = 1;
            targets.GetArrayElementAtIndex(0).objectReferenceValue = puzzleCollider;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material GetOrCreateReticleMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(ReticleMaterialPath);
            if (material)
                return material;

            material = new Material(Shader.Find("Unlit/Color"))
            {
                color = new Color(1f, 0.85f, 0.55f, 1f)
            };
            AssetDatabase.CreateAsset(material, ReticleMaterialPath);
            return material;
        }

        private static void CreateDeviceSimulator(Scene scene)
        {
            var simulator = (GameObject)PrefabUtility.InstantiatePrefab(LoadAsset<GameObject>(SimulatorPrefabPath), scene);
            simulator.SetActive(false);

            var toggleObject = new GameObject("XR Device Simulator Toggle");
            SceneManager.MoveGameObjectToScene(toggleObject, scene);
            var toggle = toggleObject.AddComponent<XrDeviceSimulatorToggle>();
            var serialized = new SerializedObject(toggle);
            serialized.FindProperty("simulator").objectReferenceValue = simulator;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // Removes what only makes sense on a flat screen: the keyboard player, the keyboard test
        // provider, screen-space canvases, stray cameras and the legacy-input EventSystem.
        private static void RemoveFlatscreenObjects(Scene scene, Transform[] exhibitRoots, GameObject player, GameObject testProvider)
        {
            var removed = new List<string>();

            void Remove(GameObject target)
            {
                if (!target || IsUnderAny(target.transform, exhibitRoots))
                    return;

                removed.Add(target.name);
                Object.DestroyImmediate(target);
            }

            Remove(player);
            Remove(testProvider);

            foreach (var canvas in FindAll<Canvas>(scene).Where(c => c.isRootCanvas && c.renderMode != RenderMode.WorldSpace).ToList())
            {
                Remove(canvas.gameObject);
            }

            foreach (var camera in FindAll<Camera>(scene).Where(c => !c.GetComponentInParent<XROrigin>(true)).ToList())
            {
                Remove(camera.gameObject);
            }

            foreach (var eventSystem in FindAll<EventSystem>(scene).ToList())
            {
                Remove(eventSystem.gameObject);
            }

            Debug.Log($"{LogPrefix} Removed flat-screen objects: {string.Join(", ", removed)}.");
        }

        private static void AddToBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath))
                return;

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static Dictionary<string, InputActionReference> LoadActionReferences(string assetPath)
        {
            var references = new Dictionary<string, InputActionReference>();
            foreach (var reference in AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<InputActionReference>())
            {
                var action = reference.action;
                if (action != null)
                    references[$"{action.actionMap.name}/{action.name}"] = reference;
            }

            return references;
        }

        private static bool IsUnderAny(Transform transform, Transform[] roots)
        {
            return roots.Any(root => transform == root || transform.IsChildOf(root));
        }

        private static IEnumerable<T> FindAll<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true));
        }

        private static T LoadAsset<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset)
                throw new InvalidOperationException($"{LogPrefix} Missing asset {path}.");

            return asset;
        }
    }
}
