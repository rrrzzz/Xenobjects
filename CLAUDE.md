# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**Xenobjects** — Unity 6 (6000.4.5f1) art project. Android AR (phone, marker-based) plus a Quest track (VR prototype now, Quest 3 passthrough AR later). See **ARCHITECTURE.md** for system design, spawning flow, input abstraction, effects system, and dependencies.

Build via **File → Build Settings → Android**. No CLI build pipeline or automated tests.

## Quest specifics

- Stack: OpenXR + Meta Quest Support + XRI 3.x + Starter Assets; Input System only (legacy disabled).
- **Sticks are non-standard: RIGHT = move, LEFT = turn.** Intentional.
- Locomotion: XR Body Transformer with "Use Character Controller If Exists" (Character Controller Driver is deprecated — don't reintroduce).

### Gotchas already hit

- Oculus Touch Controller Profile must be in OpenXR Interaction Profiles on BOTH Android and Standalone tabs — empty list = dead controllers.
- Prefer Input System / XRI actions over `UnityEngine.XR.InputDevices`; many `CommonUsages` features aren't populated on OpenXR (`deviceAcceleration` — use `deviceVelocity`).
- Quest 2 stick drift vs snap-turn reset: Stick Deadzone ~0.25–0.3 on the Turn action.
- VR UI: World Space canvas only, scale ~0.001.

### Workflow

- Primary iteration: Quest Link (USB-C) + editor Play mode. XR Device Simulator when no headset; keep it disabled during Link.
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
- `Assets/Code/Utils/` and `Assets/Code/Tests/` contain dev-only utilities not used in the shipped build.

## Code style

- One statement per line — never put code after `;` on the same line.
- Braces force multi-line: every statement inside `{ }` goes on its own indented line.
- An if/else chain must use the same brace style across all branches — no mixing.
- Braceless one-liner `if` is allowed, but the body still goes on its own indented line.

## Working with Claude

- If you are unsure which framework, library, input system, or API the project uses for a given task (e.g. XR input, tweening, networking), ASK the user instead of grepping or parsing large parts of the codebase to infer it. A one-line clarification is cheaper than a multi-agent exploration and avoids picking the wrong idiom.
