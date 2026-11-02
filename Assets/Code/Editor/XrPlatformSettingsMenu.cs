using System.Collections.Generic;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace Code.EditorTools
{
    /// <summary>
    /// Switches for the Standalone XR tab, which drives editor Play mode: OpenXR for Quest Link,
    /// or AR Foundation's XR Simulation for trying the phone scenes in the editor. Only one of
    /// them can be the Standalone loader at a time.
    /// </summary>
    public static class XrPlatformSettingsMenu
    {
        private const string OpenXrLoader = "UnityEngine.XR.OpenXR.OpenXRLoader";
        private const string SimulationLoader = "UnityEngine.XR.Simulation.SimulationLoader";
        private const string LogPrefix = "[Xenobjects XR]";

        [MenuItem("Xenobjects/XR/Standalone: OpenXR (Quest Link)")]
        public static void UseOpenXrForStandalone()
        {
            var manager = GetManagerSettings(BuildTargetGroup.Standalone);
            XRPackageMetadataStore.RemoveLoader(manager, SimulationLoader, BuildTargetGroup.Standalone);
            XRPackageMetadataStore.AssignLoader(manager, OpenXrLoader, BuildTargetGroup.Standalone);
            EnableQuestControllerProfiles(BuildTargetGroup.Standalone);
            AssetDatabase.SaveAssets();

            Debug.Log($"{LogPrefix} Standalone now uses OpenXR with the Quest controller profiles (Quest Link).");
            LogValidation(BuildTargetGroup.Standalone);
        }

        [MenuItem("Xenobjects/XR/Standalone: XR Simulation (AR)")]
        public static void UseXrSimulationForStandalone()
        {
            var manager = GetManagerSettings(BuildTargetGroup.Standalone);
            XRPackageMetadataStore.RemoveLoader(manager, OpenXrLoader, BuildTargetGroup.Standalone);
            XRPackageMetadataStore.AssignLoader(manager, SimulationLoader, BuildTargetGroup.Standalone);
            AssetDatabase.SaveAssets();

            Debug.Log($"{LogPrefix} Standalone now uses XR Simulation (AR scenes in the editor). Quest Link won't work until you switch back.");
        }

        [MenuItem("Xenobjects/XR/Log OpenXR Validation Issues")]
        public static void LogValidationForQuestTargets()
        {
            LogValidation(BuildTargetGroup.Standalone);
            LogValidation(BuildTargetGroup.Android);
        }

        /// <summary>
        /// Logs every failing OpenXR project validation rule for the group. Returns the number of
        /// rules marked as errors.
        /// </summary>
        public static int LogValidation(BuildTargetGroup group)
        {
            var issues = new List<OpenXRFeature.ValidationRule>();
            OpenXRProjectValidation.GetCurrentValidationIssues(issues, group);

            var errorCount = 0;
            foreach (var issue in issues)
            {
                if (issue.error)
                    errorCount++;

                var severity = issue.error ? "ERROR" : "warning";
                Debug.Log($"{LogPrefix} OpenXR validation ({group}) {severity}: {issue.message}");
            }

            Debug.Log($"{LogPrefix} OpenXR validation ({group}): {issues.Count} issue(s), {errorCount} error(s).");
            return errorCount;
        }

        private static XRManagerSettings GetManagerSettings(BuildTargetGroup group)
        {
            var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);
            return generalSettings.AssignedSettings;
        }

        private static void EnableQuestControllerProfiles(BuildTargetGroup group)
        {
            var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            EnableFeature<OculusTouchControllerProfile>(settings);
            EnableFeature<MetaQuestTouchPlusControllerProfile>(settings);
            EditorUtility.SetDirty(settings);
        }

        private static void EnableFeature<T>(OpenXRSettings settings) where T : OpenXRFeature
        {
            var feature = settings.GetFeature<T>();
            if (feature == null)
            {
                Debug.LogWarning($"{LogPrefix} OpenXR feature {typeof(T).Name} not found.");
                return;
            }

            feature.enabled = true;
            EditorUtility.SetDirty(feature);
        }
    }
}
