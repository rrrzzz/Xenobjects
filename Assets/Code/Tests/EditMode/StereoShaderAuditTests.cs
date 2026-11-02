using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;

namespace Code.Tests
{
    /// <summary>
    /// The Quest renders single-pass instanced: a vertex/fragment shader without the stereo
    /// macros draws in one eye only, and a GrabPass must sample through the screen-space macros.
    /// Guards the shader of every material the VR museum uses, including sub-asset materials such
    /// as TextMeshPro fonts. Project-local #includes are followed; built-in shaders are trusted,
    /// and post-processing's fullscreen shaders are out of scope (PPv2 handles stereo itself).
    /// </summary>
    public class StereoShaderAuditTests
    {
        private const string VrScenePath = "Assets/Scenes/MuseumSimulationVR.unity";
        private static readonly Regex IncludeRegex = new Regex("#include\\s+\"([^\"]+)\"");

        [Test]
        public void EveryMaterialShaderInTheVrMuseum_SupportsSinglePassInstanced()
        {
            Assume.That(File.Exists(VrScenePath), "Run Xenobjects/VR/Rebuild Museum VR Scene first.");

            var shaderPaths = AssetDatabase.GetDependencies(VrScenePath, true)
                .Where(path => path.EndsWith(".mat") || path.EndsWith(".asset"))
                .SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<UnityEngine.Material>())
                .Where(material => material.shader)
                .Select(material => AssetDatabase.GetAssetPath(material.shader))
                .Where(path => path.EndsWith(".shader"))
                .Distinct()
                .ToList();
            Assert.That(shaderPaths, Is.Not.Empty, "Expected the VR museum to use project shaders.");

            var problems = new List<string>();
            foreach (var path in shaderPaths)
            {
                var problem = FindStereoProblem(ReadWithIncludes(path, new HashSet<string>()));
                if (problem != null)
                    problems.Add($"{path}: {problem}");
            }

            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        private static string FindStereoProblem(string source)
        {
            if (Regex.IsMatch(source, "\\bGrabPass\\b") && !source.Contains("UNITY_DECLARE_SCREENSPACE_TEXTURE"))
                return "GrabPass sampled without UNITY_DECLARE_SCREENSPACE_TEXTURE";

            // In stereo the depth texture is an array; SAMPLE_DEPTH_TEXTURE then needs its sampler,
            // which only UNITY_DECLARE_DEPTH_TEXTURE declares (fails to compile on Quest otherwise).
            if (Regex.IsMatch(source, "sampler2D(_float)?\\s+_CameraDepthTexture\\s*;"))
                return "_CameraDepthTexture declared as sampler2D instead of UNITY_DECLARE_DEPTH_TEXTURE";

            var hasProgram = Regex.IsMatch(source, "CGPROGRAM|HLSLPROGRAM");
            var isSurface = Regex.IsMatch(source, "#pragma\\s+surface\\b");
            if (!hasProgram || isSurface)
                return null;

            var hasStereoOutput = source.Contains("UNITY_VERTEX_OUTPUT_STEREO") ||
                                  source.Contains("UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO");
            return hasStereoOutput ? null : "vertex/fragment shader without UNITY_VERTEX_OUTPUT_STEREO";
        }

        private static string ReadWithIncludes(string path, HashSet<string> visited)
        {
            var fullPath = Path.GetFullPath(path);
            if (!visited.Add(fullPath) || !File.Exists(fullPath))
                return string.Empty;

            var source = File.ReadAllText(fullPath);
            var builder = new StringBuilder(source);
            foreach (Match match in IncludeRegex.Matches(source))
            {
                var includePath = Path.Combine(Path.GetDirectoryName(fullPath) ?? string.Empty, match.Groups[1].Value);
                builder.AppendLine();
                builder.Append(ReadWithIncludes(includePath, visited));
            }

            return builder.ToString();
        }
    }
}
