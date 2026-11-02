# VR museum — testing

`Assets/Scenes/MuseumSimulationVR.unity` is the VR port of the three-object museum
(`MuseumSimulation`). It is generated: edit `MuseumSimulation`, then run
**Xenobjects ▸ VR ▸ Rebuild Museum VR Scene**.

## Controls

| Phone | Quest |
|---|---|
| Tilt (roll): Object 1 distortion, Object 3 flame colour | Head roll |
| Rotation (yaw): Object 1 puzzle | Left stick X — the angle holds when you let go |
| Shake: Object 1 swarm | Shake either controller |
| Tap: toggles effects | Trigger (either hand) or B |
| Double tap | A (nothing in the museum uses it) |
| Aim screen centre at the puzzle | Look at it (a dot appears on the puzzle) |
| Walk | Walk, or right stick |

Stick turning is off: the left stick belongs to the puzzle. Only the podium you stand at
(within ~2.5 m) reacts to your head and controllers; the wrist HUD (look at your left hand)
names it and shows the live values.

## Automated checks (no headset)

- **EditMode** (`Code.Tests.EditMode`): shake detection (shake vs single/double swing, slow
  sway, jitter, cooldown, same result at 72/90/120 Hz), puzzle stick integration, podium focus
  with hysteresis, and a stereo audit of every material shader in the VR museum.
- **PlayMode** (`Code.Tests.PlayMode`): loads the VR museum with a simulated headset and
  controllers and checks every interaction end to end — scene integrity, bindings resolving
  on Oculus Touch controllers, floor placement, locomotion, podium focus, head roll →
  distortion / flame colour, distance → fog / tornado / smoke, taps reaching only the focused
  object, A = double tap, one shake → one swarm, 3 s idle → path sequence, gaze + left stick
  → puzzle solved. Any logged error fails a test.
- **Snapshots** (explicit test `VrSnapshotCapture`): renders head-height views of each
  podium and the wrist HUD to `Logs/VrSnapshots`.

In the editor: **Window ▸ General ▸ Test Runner**. Headless, with the editor closed
(`Unity` = `"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"`):

```
Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode.xml
Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults Logs/playmode.xml
Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testFilter Code.Tests.VrSnapshotCapture.CaptureMuseumViews
Unity -batchmode -nographics -projectPath . -quit -executeMethod Code.EditorTools.VrMuseumSceneBuilder.BuildFromCommandLine
Unity -batchmode -nographics -projectPath . -quit -buildTarget Android -executeMethod Code.EditorTools.QuestBuild.BuildMuseumApk
```

The APK lands in `Builds/Quest/XenobjectsMuseumVR.apk` (development build).

## First Quest 2 session over Link (~10 minutes)

Setup: Meta Quest Link app running, headset connected and Link enabled, then open
`MuseumSimulationVR` and press Play. If the controllers don't move, run
**Xenobjects ▸ XR ▸ Standalone: OpenXR (Quest Link)** and check the Console.

1. **Both eyes.** At Object 1 and Object 2, close one eye, then the other. The distortion
   (Object 1 capsule and effect meshes) and the heat haze (Object 2) should look the same in
   each eye. They are audited for single-pass instanced but not yet seen in a headset.
2. **Focus.** Walk to each podium; the HUD's first line switches to that object. A podium
   you walked away from stops reacting.
3. **Head roll.** At Object 1, tilt your head ~25°: distortion grows, HUD TiltZ01 ≈ 0.5.
   At Object 3 the flame colour shifts.
4. **Taps.** Trigger or B at each object: distortion oscillates (1), blood rotates (2),
   ribbons pulse (3).
5. **Shake.** Shake a controller briskly at Object 1: the swarm plays. The HUD should say
   `XR velocity`, not `position fallback`. If it's too eager or too stubborn, select all
   three `VR Provider - Object N` objects and tune `Burst Enter Threshold` (4) together.
6. **Path and puzzle.** Stand still at Object 1 for 3 s: the orb fades and the tentacle path
   appears around the object at podium height. Trace it with either controller from its
   start (your head also counts, but it would mean crouching). At the puzzle, look at it
   (dot), then push the left stick until it locks in.
7. **Distance.** Lean in and step back at each object: fog (1), tornado (2), smoke (3).
   Object 2 waits 4 s after you first arrive before it reacts.
8. **Scale and comfort.** Podium tops are ~1 m above the floor; objects should feel
   table-height.
9. **Frame rate.** Link renders on the PC, so it says nothing about standalone Quest 2.
   For that, install the APK and watch OVR Metrics Tool; the particle-heavy objects and the
   GrabPass distortion are the likely costs.

## Known limits

- The post-processing profile for VR (`Assets/Code/VR/MuseumVRPostProcessProfile.asset`)
  is a copy without ambient occlusion; bloom and colour grading are kept.
- In the editor without a headset, the XR Device Simulator drives the rig. Shake works there
  through the position fallback, but it takes a very fast mouse.
