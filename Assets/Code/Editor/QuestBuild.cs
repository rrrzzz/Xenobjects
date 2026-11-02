using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Code.EditorTools
{
    /// <summary>
    /// Headless Quest build of the VR museum, independent of the Build Settings scene list
    /// (the phone AR scenes share the Android target).
    /// Batch: -buildTarget Android -executeMethod Code.EditorTools.QuestBuild.BuildMuseumApk [-questApkPath path]
    /// </summary>
    public static class QuestBuild
    {
        private const string DefaultApkPath = "Builds/Quest/XenobjectsMuseumVR.apk";

        [MenuItem("Xenobjects/VR/Build Quest APK (Museum VR)")]
        public static void BuildMuseumApk()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { VrMuseumSceneBuilder.VrScenePath },
                locationPathName = GetArgument("-questApkPath") ?? DefaultApkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[QuestBuild] {summary.result}: {summary.outputPath}, {summary.totalSize / (1024f * 1024f):F1} MB, " +
                      $"{summary.totalErrors} errors, {summary.totalWarnings} warnings, {summary.totalTime.TotalSeconds:F0} s.");

            if (summary.result != BuildResult.Succeeded && Application.isBatchMode)
                EditorApplication.Exit(1);
        }

        private static string GetArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
