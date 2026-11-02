# Xenobjects — Architecture

This document describes how the runtime code in `Assets/Code/` is structured and how the
parts interact. It was inferred from the code itself (there was no prior design doc), so it
describes what *is*, plus a short section on where the design is heading.

## Overview

The app shows three interactive art objects ("AR objects") that a visitor discovers with a
phone (marker-based AR) or, on the Quest track, a headset. Each object reacts to how the
visitor behaves — distance, device tilt, movement vs. standing still, touches, shakes —
by driving particle systems, shader parameters, and animated tentacle splines.

The runtime is three layers plus a spawning layer that connects them:

```
 ┌────────────────────────────────────────────────────────────────┐
 │ SPAWNERS (scene-level, one active per scene)                   │
 │   ArTrackingManager  – AR Foundation tracked images → prefab   │
 │   ArCeo              – color/cross detection + plane anchor    │
 └───────────────┬────────────────────────────────────────────────┘
                 │ Instantiate(prefab), then
                 │ dataProvider.SetArObjectTransform(spawned, pathViz)
                 ▼
 ┌────────────────────────────────────────────────────────────────┐
 │ INTERACTION PROVIDER (per platform; one per exhibit in VR)     │
 │   MovementInteractionProviderBase (abstract)                   │
 │     ├─ ARMovementInteractionDataProvider   (phone)             │
 │     ├─ VRMovementInteractionDataProvider   (Quest / OpenXR)    │
 │     └─ MovementInteractionTestProvider     (editor keyboard)   │
 │   Continuous signals: DistanceToArObject01/Raw, TiltZ01,       │
 │     SignedTiltY01/Z01, IsMoving, MovementDuration, IdleDuration│
 │   Discrete events:  SingleTouchEvent, DoubleTouchEvent,        │
 │     ShakeEvent (UnityEvents)                                   │
 └───────────────┬────────────────────────────────────────────────┘
                 │ Initialize(provider, pathVisualizer)
                 ▼
 ┌────────────────────────────────────────────────────────────────┐
 │ ART-OBJECT MANAGERS (one per prefab, inside the prefab)        │
 │   ArObjectManagerBase (abstract)                               │
 │     ├─ ArObject1Manager – orb, distortion, ink lines, fog,     │
 │     │    swarm, tentacle pathfinding + rotation puzzle         │
 │     ├─ ArObject2Manager – tornado, blood systems, gas,         │
 │     │    visibility-by-movement                                │
 │     └─ ArObject3Manager – ribbons, colored flame, smoke,       │
 │          orbiting tentacle                                     │
 │   Consume provider signals per frame; react to events.         │
 │   Track which "interactable elements" the visitor has          │
 │   discovered (base class); when all are used, trigger          │
 │   MovementPathVisualizer.                                      │
 └───────────────┬────────────────────────────────────────────────┘
                 ▼
 ┌────────────────────────────────────────────────────────────────┐
 │ EFFECTS (plain C# classes, not MonoBehaviours)                 │
 │   EffectBase – collects material instances from a GameObject,  │
 │     lerps/tweens one shader property (float or vector)         │
 │     ├─ OrbGlowingEffect, DistortionEffect, InkLinesEffect,     │
 │     │  DissolveEffect, RisingSteamEffect                       │
 │   SplineMeshTiling (customized third-party) – tentacle mesh    │
 │     tiling + grow/shrink/vanish tweens + segment rotation      │
 └────────────────────────────────────────────────────────────────┘
```

## Spawning flow

Two alternative spawners exist; a scene uses one of them.

**Marker flow (`ArTrackingManager`)** — listens to `ARTrackedImageManager.trackablesChanged`.
When a reference image is recognized, it looks the image name up in a marker→(prefab, offset)
table, destroys any previously spawned content, instantiates the prefab parented to the
tracked image (with a per-object positional offset), and hands the spawned transform to the
provider via `SetArObjectTransform`. A double-touch respawns the current object using
offsets/scale/rotation typed into a debug input field.

**Color/plane flow (`ArCeo`)** — a frame-driven detection sequence: wait for AR session
tracking → check phone is roughly horizontal (gyro) → capture a CPU camera image and let
`ARColorAnalyzer` find a dominant red/green/blue → require a detected AR plane under the
screen center → require crosshair alignment → verify the camera is centered on a printed
colored cross → attach an `ARAnchor` to the plane and instantiate the prefab matching the
detected color. A manual mode spawns via three UI buttons instead. `ArCeo` is a state
machine expressed as flags (`_isTargetFound`, `_planeWasAdded`, `_checkingIfSeeingTarget`,
`_hasCheckedCross`, …) inside one `Update`.

**Handoff contract** — `MovementInteractionProviderBase.SetArObjectTransform(transform, pathViz)`
stores the object transform (for distance calculation), finds the `ArObjectManagerBase` in
the spawned hierarchy, and calls `Initialize(this, pathViz)`. From that point the manager
pulls signals from the provider every frame and subscribes to its events.

## Input abstraction

The provider is the platform boundary. Managers never read `Input`, XR devices, or actions
directly — they only see normalized floats and UnityEvents. Per platform:

| Signal | AR (phone) | VR (Quest) | Test (editor) |
|---|---|---|---|
| TiltZ01 / SignedTiltZ01 | camera euler Z | HMD euler Z (head roll) | Q/E keys |
| SignedTiltY01 (puzzle rotation) | camera yaw minus yaw-at-puzzle-entry | left-stick X, rate-controlled (angle holds on release), zeroed at puzzle lock-on | Z/C keys |
| SingleTouch / DoubleTouch | 1-finger / 2-finger tap | trigger (either hand) or B / A | U / T keys |
| Shake | accelerometer high-pass + cooldown | `ShakeDetector` over per-hand `deviceVelocity`: low-pass, burst counting in a sliding window, cooldown | S key |
| Puzzle lock-on (`GetForwardRayHit`) | screen centre | head gaze, with `VrGazeReticle` on the puzzle | camera forward |

Notes:
- `NormalizeRotationAngles` (shared in `InteractionUtils`) maps eulers to [-180, 180) and
  flips sign; all tilt logic depends on this convention.
- OpenXR does not populate `deviceAcceleration`; the VR shake pipeline derives bursts from
  `deviceVelocity` instead (see CLAUDE.md gotchas). Without XR InputDevices (XR Device
  Simulator, tests) it differentiates the Input System hand position instead.
- VR bindings live in `Assets/Code/VR/XenobjectsVRInput.inputactions`.
- The provider also owns debug TMP texts and a debug param input field
  (`TryGetParamValue`) — acknowledged wart, see "Direction". The VR museum's wrist HUD
  (`VrDebugHud`) already observes providers instead of using those fields.

## VR museum (several exhibits)

`MuseumSimulationVR` shows all three objects at once, so it breaks the one-object assumption
of the phone flow. Each exhibit gets its own `VRMovementInteractionDataProvider`;
`VrExhibitFocus` picks the exhibit the visitor stands at (horizontal head distance, with
hysteresis, via the pure `ExhibitFocusSelector`) and calls `SetFocused` on the providers:

- An exhibit's object is initialized (`SetArObjectTransform`) the first time it gains focus —
  the VR stand-in for scanning its marker — so start delays play when the visitor arrives.
- An **unfocused** provider keeps reporting distance (distance-driven effects stay live from
  afar) but holds tilt, movement and durations at zero and raises no events. Without this,
  idling at Object 3 would start Object 1's path sequence, and a tap would toggle all three.
- Managers are unchanged; AR providers are always focused.

The scene is generated from `MuseumSimulation` by `VrMuseumSceneBuilder` (Xenobjects ▸ VR ▸
Rebuild Museum VR Scene): keyboard player, test provider and screen-space UI out; XR rig,
controllers, wrist HUD, gaze reticle, self-disabling XR Device Simulator and a VR copy of
the post-processing profile (no ambient occlusion) in.

## Effects system

`EffectBase` subclasses are constructed with shader property IDs and min/max ranges, get
their materials once via `SetMaterial(gameObject)` (instanced materials, optionally filtered
by material name), and are then driven either continuously
(`SetEffectByNormalizedValue(t)`) or with DOTween fades. They are deliberately not
MonoBehaviours — managers own and drive them.

`SplineMeshTiling` (in `Assets/MeshDeformers/SplineMesh/`, heavily customized) is the
tentacle workhorse: per-node scale tweens (grow/shrink/vanish, with per-node delays),
walk-to-vanish behavior (`InitVanishing` pauses the vanish tweens and resumes them node by
node as the player physically approaches; fires `PathEndReachedEvent` at the end), segment
Y-rotation for the Object 1 puzzle, and material access for tinting. All its tweens carry a
per-instance DOTween id so instances never pause/kill each other's tweens.

## Interactable discovery / path visualization

Each object defines N "interactable elements" (things the visitor can discover: fog change,
shake swarm, touch toggle, …). `ArObjectManagerBase` tracks discovered ids in a set
(`MarkInteractableUsed("fog-distance")`) and, once all N are used,
`TryShowPathWhenAllInteractablesUsed` triggers `MovementPathVisualizer` — which records the
camera path while the visitor explores. (The actual line rendering is currently commented
out — the feature is parked, the trigger plumbing works.)

## Dependencies

| Asset | Role |
|---|---|
| AR Foundation / ARCore | tracked images, planes, anchors, CPU camera image |
| OpenXR + XRI 3.x | Quest track: input actions, locomotion, rig |
| DOTween | all tweening (managers, effects, spline scaling) |
| SplineMesh (customized) | tentacle spline mesh deformation |
| Deform | mesh deformation framework |
| EasyButtons | `[Button]` inspector triggers for testing sequences |
| TextMeshPro | debug HUD texts |
| PostProcessing | bloom / image effects |

## Direction (target architecture)

Agreed improvements, in priority order — see git history for what's already done:

1. **Backend-agnostic object anchor.** Parent all spawned content under a single
   `ObjectAnchor` transform driven by whichever backend is active (tracked image, ARAnchor,
   hand-placed in VR, Quest 3 spatial anchor later). Spawners then differ only in *how they
   drive the anchor*, and the Quest passthrough track becomes a driver swap instead of a
   third spawner.
2. **`ArCeo` as an explicit state machine.** The flag-soup `Update` should become named
   states (`WaitingForTracking → CheckingHorizontal → DetectingColor → WaitingForPlane →
   Aiming → VerifyingCross → Spawned`) with one handler per state. Same behavior, testable
   transitions, self-documenting.
3. **Slim provider.** Move the debug TMP texts + `TryGetParamValue` debug field out of
   `MovementInteractionProviderBase` into a separate debug HUD component that *observes* the
   provider, so the input abstraction has a single responsibility.
4. **Split `SplineMeshTiling`.** Mesh tiling (third-party concern) vs. game sequencing
   (grow/vanish/puzzle rotation) should be two components; the sequencer belongs in
   `Assets/Code/` under the `Code` namespace.
5. **Puzzle extraction.** Object 1's rotation puzzle (raycast lock-on, error→color mapping,
   completion) deserves its own class; the manager should only start/stop it.
