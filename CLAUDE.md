# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Xenobjects** is a Unity 6 Android AR app that places interactive art objects in the real world via image-marker tracking and animates effects around them. Phone tilt, shake, proximity, and touch all drive the effects.

- **Unity:** 6000.4.5f1
- **Platform:** Android (AR Foundation 6.4.2 + ARCore)
- **Main scene:** `Assets/Scenes/ARScene.unity`
- **Desktop testing scene:** `Assets/Scenes/MuseumSimulation.unity` (no device needed)

Build via **File → Build Settings → Android** in the Unity Editor. No CLI build pipeline or automated tests.

## Architecture

### Spawning flow

`ArTrackingManager` detects one of three image markers and spawns the matching prefab. `ArCeo` is an alternative spawner using camera color detection (red/green/blue targets) or manual UI buttons. After spawning, `SetArObjectTransform()` links the input provider to the new object and calls `Initialize()` on the object's manager.

### Input abstraction (`Assets/Code/InteractionDataProviders/`)

`MovementInteractionProviderBase` (abstract) reads sensors each frame and exposes normalized values — tilt, yaw, proximity, walking state — plus `UnityEvent`s for shake, single-tap, and double-tap. The concrete AR implementation is `ARMovementInteractionDataProvider` (gyro + accelerometer). `MovementInteractionTestProvider` exists for editor testing without a device.

### Object managers (`Assets/Code/ArObjectManagers/`)

`ArObject1/2/3Manager` each subclass `ArObjectManagerBase`. They read the provider's properties to modulate particle systems, splines, and shader effects. Each tracks a fixed number of interactable steps; once all are triggered, `MovementPathVisualizer` shows the player's walked path.

### Effects system (`Assets/Code/Effects/`)

`EffectBase` (abstract) tweens a shader property on a `Material` between min/max values using DOTween. Subclasses (`OrbGlowingEffect`, `DistortionEffect`, `DissolveEffect`, etc.) declare which property to animate. All shader IDs are cached with `Shader.PropertyToID()`.

## Key dependencies

| Asset | Role |
|---|---|
| DOTween | All tweening in managers and effects |
| SplineMesh | Tentacle spline deformation (Object 1) |
| EasyButtons | `[Button]` attribute — triggers methods from the Inspector |
| Deform | Mesh deformation framework |
| PostProcessing | Bloom / image effects |

## Conventions

- Game code lives in `Assets/Code/` under the `Code` namespace (`ArObject3Manager` is a global-namespace exception).
- Shader property IDs are `private static readonly int` fields.
- Use `[FormerlySerializedAs]` when renaming serialized fields to avoid losing scene data.
- `Assets/Code/Utils/` and `Assets/Code/Tests/` contain dev-only utilities not used in the shipped build.

## Working with Claude

- If you are unsure which framework, library, input system, or API the project uses for a given task (e.g. XR input, tweening, networking), ASK the user instead of grepping or parsing large parts of the codebase to infer it. A one-line clarification is cheaper than a multi-agent exploration and avoids picking the wrong idiom.
