# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Xenobjects** — Unity 6 (6000.6.0f1) art project. Android AR (phone, marker-based) plus a Quest track (VR prototype now, Quest 3 passthrough AR later). See **ARCHITECTURE.md** for system design, spawning flow, input abstraction, effects system, and dependencies.

Build via **File → Build Settings → Android**, or headless. EditMode and PlayMode tests cover the VR museum; **VR_TESTING.md** has the commands and the headset checklist.

## Quest specifics

- Stack: OpenXR + Meta Quest Support + XRI 3.x + Starter Assets. All VR input goes through the Input System; Active Input Handling is *Both* only because keyboard/editor test scripts still use legacy `Input`.
- **Sticks: RIGHT = move, LEFT = Object 1 puzzle rotation** (the phone's yaw). Stick turning is off in VRTest and the VR museum; visitors turn physically.
- Locomotion: XR Body Transformer with "Use Character Controller If Exists" (Character Controller Driver is deprecated — don't reintroduce).

### Gotchas already hit

- Oculus Touch Controller Profile must be in OpenXR Interaction Profiles on BOTH Android and Standalone tabs — empty list = dead controllers. Standalone also needs the OpenXR loader for Link: **Xenobjects ▸ XR** switches Standalone between OpenXR (Link) and XR Simulation (AR scenes in the editor).
- Prefer Input System / XRI actions over `UnityEngine.XR.InputDevices`; many `CommonUsages` features aren't populated on OpenXR (`deviceAcceleration` — use `deviceVelocity`). The VR provider's shake reads `deviceVelocity` on a headset and falls back to differentiating the Input System hand position (XR Device Simulator, tests).
- Quest 2 stick drift vs snap-turn reset: Stick Deadzone ~0.25–0.3 on the Turn action.
- VR UI: World Space canvas only, scale ~0.001. In editor scripts set a RectTransform's `anchoredPosition3D` — x/y live there, `localPosition` alone doesn't stick.
- Rendering is Single Pass Instanced: vertex/fragment shaders need the stereo macros and GrabPass needs `UNITY_DECLARE_SCREENSPACE_TEXTURE`, or they draw in one eye. `StereoShaderAuditTests` guards the VR museum.
- Play mode keeps static state: Enter Play Mode Options disable domain AND scene reload. Avoid statics in runtime code or reset them (`[AutoStaticsCleanup]`).
- ARCore is an **embedded package** (`Packages/com.unity.xr.arcore`, 6.6.2) with one patched line: its editor asmdef keyed the `URP_7_OR_NEWER` define on RP Core (pulled in by Shader Graph), so it compiled URP code in this Built-in project. The patch keys it on the URP package. Don't install URP to work around it: URP's global settings drag URP Lit shader variants into Quest builds. Delete the folder once a fixed ARCore ships.
- The live editor log is `Logs/Editor.log` inside the project.

### Workflow

- Primary iteration: Quest Link (USB-C) + editor Play mode. The VR museum's XR Device Simulator switches itself off when a headset is running (`XrDeviceSimulatorToggle`).
- `MuseumSimulationVR` is **generated** from `MuseumSimulation` by **Xenobjects ▸ VR ▸ Rebuild Museum VR Scene** (`VrMuseumSceneBuilder`). Edit the keyboard museum and rebuild; hand edits to the VR scene are overwritten.
- On-device builds only for Android-specific features (passthrough, anchors) — Script-Only/Patch build for code-only changes.
- Frame target: 72 Hz on Quest 2; re-verify at 90/120 on Quest 3.

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
- `Assets/Code/Utils/` and `Assets/Code/Tests/` contain dev-only utilities not used in the shipped build. `Assets/Code/Tests/EditMode` and `PlayMode` are the automated test assemblies.
- Pure, testable logic goes in `Assets/Code/VR/Core` (`Code.VR.Core` asmdef). Game code is in Assembly-CSharp, which test asmdefs cannot reference — PlayMode tests reach it through `Probe` (reflection).

## Code style

- One statement per line — never put code after `;` on the same line.
- Braces force multi-line: every statement inside `{ }` goes on its own indented line.
- An if/else chain must use the same brace style across all branches — no mixing.
- Braceless one-liner `if` is allowed, but the body still goes on its own indented line.

## Working with Claude

- If you are unsure which framework, library, input system, or API the project uses for a given task (e.g. XR input, tweening, networking), ASK the user instead of grepping or parsing large parts of the codebase to infer it. A one-line clarification is cheaper than a multi-agent exploration and avoids picking the wrong idiom.
