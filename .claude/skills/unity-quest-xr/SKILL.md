---
name: unity-quest-xr
description: Best practices for Unity VR/AR/MR development on Meta Quest (Quest 2/3/Pro) with OpenXR and XR Interaction Toolkit 3.x. Use this skill whenever the task involves Unity XR code or configuration — controller input, locomotion, VR UI/canvas work, spatial anchors, passthrough AR, hand tracking, XR performance, headset debugging, or build/iteration workflow — even if the user just says "VR", "AR", "headset", "Quest", "controllers", or describes a symptom like "input doesn't work in the headset" or "my UI is invisible in VR". Also apply when reviewing or writing any MonoBehaviour that reads XR devices, tracked poses, or XRI interactables.
---

# Unity XR development for Meta Quest

Conventions and hard-won fixes for Unity + OpenXR + XR Interaction Toolkit (XRI) 3.x targeting Meta Quest standalone. Quest is an Android device with a mobile GPU; every decision flows from that.

## Project configuration checklist

When diagnosing "nothing works" symptoms, check these in order — they cover the large majority of Quest setup failures:

1. **XR Plug-in Management → OpenXR enabled on BOTH tabs.** Android tab drives on-device builds; Standalone tab drives editor Play mode over Quest Link. Configuring only one is the classic mistake.
2. **Interaction Profiles are NOT optional.** OpenXR routes zero controller input unless the matching profile (Oculus Touch Controller Profile for Quest) is explicitly added under Enabled Interaction Profiles — **separately on each platform tab**. Empty list = head tracks, controllers dead. This is the single most common Quest input failure; check it first.
3. **Meta Quest Support feature** enabled on the Android tab (Android-only; not needed on Standalone — Link presents as a generic OpenXR runtime).
4. **Active Input Handling** set to Input System (or Both). Legacy Input Manager APIs silently return nothing for XR.
5. Android build target, headset in developer mode, USB debugging authorized.

Unity is informative about misconfiguration here — scan XR Plug-in Management for yellow warning icons before deep debugging.

## Iteration workflow (fastest to slowest)

Match the tool to what's being tested; never build an APK to test something Link can test.

1. **XR Device Simulator** (XRI sample prefab) — editor-only, mouse/keyboard drives virtual HMD + controllers. Good for interaction logic, bindings, rough UI placement. No stereo judgment. Bindings vary by XRI version — the in-Play overlay and the `XRDeviceSimulatorControls` action asset in the sample folder are the source of truth.
2. **Quest Link + editor Play mode** — headset over USB-C/Air Link becomes a PC VR device; press Play, full hot iteration, breakpoints, live inspector. Requires OpenXR on the Standalone tab. Covers locomotion, interaction, UI, effect tuning. Does NOT cover Android-runtime features: passthrough, spatial anchors, hand tracking specifics, real perf.
3. **Script-Only / Patch build** — after one full Build & Run, code-only changes patch in ~20 s instead of a full APK.
4. **Full APK build** — for manifest changes, passthrough/anchors, frame-rate verification, untethered testing.

Conflicts: the Device Simulator and a live Link session fight over input. Keep the simulator GameObject disabled when Link is active (or enable its auto-disable-on-HMD option). "Mock HMD / Mock Runtime" is a static pipeline-validation feature, not an iteration tool — ignore it for development. "XR Simulation" in XR Plug-in Management is AR Foundation's editor simulation, unrelated to VR controller input.

## Input

- Prefer the Input System pattern: `[SerializeField] InputActionReference`, subscribe to `performed`/`canceled` in `OnEnable`/`OnDisable`, unsubscribe symmetrically. For world-object interaction, wire XRI interactable UnityEvents (`Activated`, etc.) in the inspector.
- Binding paths: X/A = `<XRController>{LeftHand|RightHand}/primaryButton`, Y/B = `secondaryButton`; sticks = `thumbstick`; also `trigger`, `grip`, `deviceVelocity`, `devicePose`.
- **`CommonUsages.deviceAcceleration` is unsupported on OpenXR for Quest controllers** — `TryGetFeatureValue` returns false. Use `CommonUsages.deviceVelocity` and derive acceleration-like signals from it. Pose, velocity, and angular velocity are reliable; linear acceleration is not.
- Quest 2 sticks drift. If snap turn "sticks" or actions fire at rest, add a Stick Deadzone processor (min ~0.25–0.3) on the action in the Actions asset — the action layer is the right place, not the provider component.

### Gesture detection from motion (shake and similar)

Detecting "repeated short motions" from a continuous signal needs a multi-stage pipeline; single-threshold checks fire on any sharp motion:

1. **Framerate-independent low-pass:** `alpha = 1 - Mathf.Exp(-Time.deltaTime / timeConstant)`, then `avg = Lerp(avg, value, alpha)`. Never use a fixed lerp factor — it makes tuning framerate-dependent (72 vs 120 Hz behave differently).
2. **Prime the filter** on first valid sample (set avg = value) or the first frame produces a phantom spike.
3. **High-pass by subtraction:** `deltaSq = (value - avg).sqrMagnitude` isolates rapid change.
4. **Hysteresis burst counting:** separate enter/exit thresholds so one spike counts once, not once per frame above threshold.
5. **Sliding window:** queue of burst timestamps, expire entries older than the window, require N bursts within it. This is what distinguishes a shake (repeated reversals) from a chop (one burst).
6. **Cooldown after firing** and clear the burst queue, or one gesture fires many events.

Every stage is load-bearing; do not "simplify" by removing one. Duplicated per-hand state is acceptable for two hands — extract a tracker class only when instance count grows or reuse appears.

## UI in VR

- **World Space canvas is the answer** essentially always. Screen Space - Overlay renders nowhere visible in a headset; Screen Space - Camera has stereo convergence problems.
- Canvas units are pixels → scale the RectTransform to ~(0.001, 0.001, 0.001).
- Placement patterns: wrist-mounted (child of a controller, faces user when palm turns up — best for status/debug), on-tool (child of held object), diegetic in-world. For labels on world objects: world-space canvas + billboard-to-camera script.
- **Never head-locked UI in the central view** — no parallax, uncomfortable focus, breaks presence. Peripheral transient effects (damage vignette) are the only exception.
- For UI interaction, the rig's XR Ray Interactor plus a Tracked Device Graphic Raycaster on the canvas.

## Spatial anchoring for AR/MR (Quest 3)

- Prefer **Meta XR Core SDK's `OVRSpatialAnchor`** over raw OpenXR spatial entity extensions — cleaner create/save/load, coexists with OpenXR + XRI. Anchors save locally by UUID; re-localization needs the headset to recognize the room; guardian reset invalidates re-localization, so ship a recalibrate affordance.
- Expect a couple cm of drift per session — fine for centered effects on ~0.5 m objects, visible for surface-grazing effects.
- **Backend-agnostic anchor pattern:** parent all content to a single `ObjectAnchor` Transform. In VR prototype, a hand-placed GameObject; in AR, driven by the spatial anchor; with markers, driven by the tracker. Effect/content code never knows the backend, so the VR→AR transition is a driver swap. Design this in from day one.
- Tracking-approach ladder for real-world objects: (a) spatial anchor + manual alignment — stationary objects, simplest; (b) fiducial markers via Passthrough Camera API + CV — moving objects, visible marker; (c) model targets (Vuforia) — markerless, real integration and licensing cost. Take the cheapest rung that works.
- Alignment UX is a design decision: operator-at-install vs per-visitor vs hybrid. Two-handed wireframe grab-and-place reaches ~1 cm with care; landmark-touch calibration does better but costs more to build.
- Quest 3 exposes a depth texture (depth sensor) for shader-sampled environment occlusion — needed for effects that should visually wrap or sit behind real geometry.

## Performance

- Set target frame rate honestly per device (Quest 2: 72 Hz; Quest 3: 90/120 Hz) and re-verify budgets when changing devices — headroom differences cause overscoping.
- URP over Built-in/HDRP; mobile-class shader budgets; single-pass instanced rendering; avoid full-screen post effects and realtime shadows where possible.
- Profile on-device, not over Link — Link runs on the PC GPU and says nothing about standalone perf.
- Steady-state GC allocations cause hitches at 72+ Hz; avoid per-frame allocations in Update paths (LINQ, string concat, closures).

## Locomotion (XRI 3.x)

- Modern stack: Locomotion Mediator + XR Body Transformer on the XR Origin; enable "Use Character Controller If Exists" when a Character Controller is present. **Character Controller Driver is deprecated** — don't add it to new setups.
- Continuous Move + Snap Turn providers read from the XRI Default Input Actions; rebinding sticks (e.g. right = move, left = turn) happens in the Actions asset so all consumers, including the Device Simulator, stay consistent.
- Comfort defaults: snap turn over smooth turn, vignette during continuous movement, never move the camera without user input.

## Debugging habits

- When `TryGetFeatureValue` returns false: the feature may be genuinely unsupported on this runtime (see acceleration above) — verify support before debugging your own code.
- Debug.Log interpolation: keep values separated (`$"dSq={x:F2} t={Time.time:F2}"`) — a bare `+ Time.time` concatenated after a number reads as one growing number and sends debugging down the wrong path.
- Editor "works in Game view but not in headset" almost always means: wrong canvas render mode, missing interaction profile, or the wrong XR settings tab configured.
