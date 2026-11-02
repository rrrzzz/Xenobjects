using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Code.Tests
{
    /// <summary>
    /// Renders the XR camera from head height at the start spot and at each podium, plus a look
    /// at the wrist HUD, and writes PNGs for a visual check without a headset (mono view).
    /// Needs a graphics device, so run it without -nographics. Output: Logs/VrSnapshots, or the
    /// folder in the XENO_SNAPSHOT_DIR environment variable.
    /// </summary>
    [Explicit("Writes PNG snapshots on demand.")]
    public class VrSnapshotCapture
    {
        private const int Width = 1280;
        private const int Height = 720;
        private const int WarmUpFrames = 150;

        [UnityTest]
        public IEnumerator CaptureMuseumViews()
        {
            Assume.That(File.Exists(MuseumRig.ScenePath), "Run Xenobjects/VR/Rebuild Museum VR Scene first.");
            Time.captureDeltaTime = 1f / 72f;
            var rig = new MuseumRig();
            yield return rig.Load();
            var xr = new SimulatedXr();
            var folder = Environment.GetEnvironmentVariable("XENO_SNAPSHOT_DIR");
            if (string.IsNullOrEmpty(folder))
                folder = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "VrSnapshots");

            Directory.CreateDirectory(folder);
            try
            {
                var start = rig.Origin.transform.position;
                rig.StandAt(xr, start, start + rig.Origin.transform.forward * 5f + Vector3.up * 1.2f, MuseumRig.EyeHeight);
                yield return Frames(WarmUpFrames);
                Save(rig.Camera, Path.Combine(folder, "00_start_view.png"));

                for (var i = 0; i < rig.ExhibitCount; i++)
                {
                    rig.StandInFront(xr, i, 1.3f);
                    yield return Frames(WarmUpFrames);

                    // Object 2 holds still for 4 real seconds after it starts (tornado, gas).
                    if (i == 1)
                        yield return new WaitForSecondsRealtime(4.5f);

                    Save(rig.Camera, Path.Combine(folder, $"0{i + 1}_object{i + 1}_1.3m.png"));

                    rig.StandInFront(xr, i, 2.4f);
                    yield return Frames(WarmUpFrames);
                    Save(rig.Camera, Path.Combine(folder, $"0{i + 1}_object{i + 1}_2.4m.png"));
                }

                // Raise the left controller in front of the face and look down at the wrist HUD.
                rig.StandInFront(xr, 1, 2.4f);
                xr.SetControllerPose(true, rig.ToTrackingSpace(rig.Camera.transform.position + rig.Camera.transform.forward * 0.35f
                                                               + Vector3.down * 0.25f), Quaternion.identity);
                yield return Frames(10);
                var hud = GameObject.Find("Wrist Debug HUD");
                if (hud)
                {
                    Debug.Log($"[VrSnapshotCapture] camera {rig.Camera.transform.position} offset {rig.Origin.CameraFloorOffsetObject.transform.localPosition} " +
                              $"left controller {hud.transform.parent.position} (local {hud.transform.parent.localPosition}) hud {hud.transform.position}");
                    var head = rig.Origin.transform.position + Vector3.up * MuseumRig.EyeHeight;
                    rig.PlaceHead(xr, head, hud.transform.position);
                    yield return Frames(30);

                    // The HUD refreshes on real time; frames here run faster than that.
                    yield return new WaitForSecondsRealtime(0.3f);
                    Save(rig.Camera, Path.Combine(folder, "05_wrist_hud.png"));
                }

                Debug.Log($"[VrSnapshotCapture] Wrote snapshots to {folder}");
            }
            finally
            {
                xr.Remove();
                Time.captureDeltaTime = 0f;
            }
        }

        private static IEnumerator Frames(int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return null;
            }
        }

        private static void Save(Camera camera, string path)
        {
            var target = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = previousTarget;

            var previousActive = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.Destroy(texture);
        }
    }
}
