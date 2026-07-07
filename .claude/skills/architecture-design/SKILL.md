---
name: architecture-design
description: Use when discussing, reviewing, or documenting architecture design before coding; when comparing refactor directions; when deciding source-of-truth, storage, authority, lifecycle, Surface/use-case, API, or socket contract shape; or when the user asks to keep design grounded and approved-decision-only.
---

# Architecture Design

## Core Rule

Do not invent architecture. Ground every proposed type, field, function, event,
storage rule, lifecycle rule, and authority rule in existing code, or mark it
as a design choice that needs explicit approval.

Separate facts, inferences, proposals, and approved decisions.

Before proposing a new lifecycle, storage, projection, replacement, or
invalidation primitive, enumerate the existing primitives that appear to cover
the same responsibility and check all current callsites. If an existing
primitive is too broad, first consider narrowing or replacing that primitive
instead of adding a second primitive beside it.

For lifecycle design, identify the state transition that proves ownership was
created or removed. Keep finalization at that transition when possible. If a
method such as `close()`, `dispose()`, or `release()` can be bypassed by a hard
removal path, treat the method as a request/implementation detail and place
the finalizer at the authoritative removal point.

When observing an existing registry, state exactly which field owns lifetime
and which adjacent fields do not. Do not infer domain identity, dedupe, or
authority from generic registries unless that registry explicitly owns those
semantics.

Before adding source-specific cleanup paths, check whether all affected
resources are the same lifecycle class. Prefer one shared finalization rule
with source-specific private cleanup implementations.

When mirroring an existing architecture, identify the invariant that must
mirror and the implementation details that are context-specific. Do not copy
physical mechanics across authority boundaries unless that authority also
matches. For example, server persistence algorithms for row locking,
revisioning, or ORM identity may not belong in a client in-memory projection;
the client may only need to preserve the same source-of-truth and lifecycle
invariant.

Views and projection DTOs must not expose mutable properties or mutation-shaped fields.
They may expose stable identity, such as `sourceId`, when a ViewModel or
module command needs to call a UseCase, but mutation authority belongs to
UseCases and command methods. Do not name view fields with `mutable*` or
encode mutability policy in render data.

Do not create a new interface, surface, DTO, or type alias when an existing
contract has the same fields and the same authority. Reuse the existing
contract. A new name is justified only when it changes semantics, ownership,
authority, lifecycle, validation, serialization, or available commands. If the
only difference is the consuming component or call site, it is duplication, not
architecture.

Do not introduce type aliases for new architecture contracts. Prefer named
interfaces for durable contracts and inline structural types for one-off local
parameters. Existing type aliases may remain only when they are already part of
the committed public API or model a true union/discriminant that cannot be
expressed clearly as an interface.

Do not introduce a DTO whose sole purpose is carrying method parameters. If a
suggested type has no independent lifecycle, authority, validation,
serialization, storage, or reusable contract meaning, pass the values as direct
method parameters using existing narrow types. Prefer a method that takes
several explicit parameters over one wrapper object that exists only because
the call has several arguments.

Do not add optional methods to interfaces. Optional methods hide which
capability a caller actually needs and usually lead to runtime existence
checks instead of explicit contracts. If a caller needs a behavior, type that
caller against a narrower interface where the method is required. If only some
objects support the behavior, split the broad interface into explicit
capability interfaces and perform capability selection at the composition
boundary, not at every call site. Do not replace an optional method with a
required nullable method/property that means the same thing.

Do not choose between interface segregation and DRY. Split contracts by
capability, authority, lifecycle, and caller need, but factor overlapping
members with identical meaning into a smaller shared contract instead of
redeclaring them on unrelated interfaces. Do not force unrelated methods into a
broad interface only to avoid duplication.

Before proposing a public interface or Surface shape, build a field-by-field
capability matrix for each real flow or lifecycle. For every field, state which
concrete scenario requires it, which layer owns its implementation, and whether
each flow should expose it now, expose it later, or explicitly not expose it.

Excluding a field from one branch of a split interface is an architectural
decision, not an implementation detail. Ask before making that exclusion when
the existing product flow or user-stated architecture suggests the capability
may belong there. Do not infer that a capability is absent from a flow only
because its implementation is future-owned by another session.

## Workflow

- Read the relevant docs and code before proposing a model.
- State the narrow refactor target in one sentence.
- Resolve open questions one at a time.
- Update docs only after explicit approval.

## Design Checks

For every proposed concept, ask:

- Is this already covered by existing machinery?
- Does it encode distinct business meaning?
- Does it reduce copied state, sync logic, stale projections, or mutation paths?
- Does it keep engine/server generic and module semantics in the module?

For every proposed setter, mutator, or command method on a next-layer
interface, identify the concrete user scenario that requires that mutation. If
there is no current scenario, remove the method. If the scenario can be handled
by cleaner construction-time data flow, immutable ownership, or an existing
use-case that already owns the state, prefer that shape instead of exposing new
state mutability. Each layer should see only the mutations that are unavoidable
for its own user interactions.

## Design Review Delegation

When an architecture review is explicitly requested, or a reviewer/subagent is
explicitly authorized, set up the review as a design gate rather than a general
brainstorming pass.

Use a bounded contract, not "review everything":

- Ask for blockers, concrete design risks, source-of-truth leaks, authority
  leaks, public/private boundary violations, parallel data flow/storage, hidden
  new lifecycle models, and remaining decisions that need user approval.
- Ask the reviewer not to redesign from scratch unless they find a blocker.
- Constrain output to decisions needing approval whenever possible. This turns
  the review into a design gate instead of an open-ended preference discussion.

List approved decisions explicitly:

- State the current approved decisions before asking for review.
- Ask the reviewer to challenge whether those decisions fit the code and
  scenarios, but not to reopen settled choices without a concrete blocker.
- Treat any newly suggested concept, DTO, surface, socket behavior, storage
  rule, authority model, or lifecycle model as unapproved until the user accepts
  it.

Require evidence against current code and real scenarios:

- Ask the reviewer to compare the plan against current committed code paths,
  not only against the written design.
- Require at least one concrete user, gameplay, or workflow scenario for each
  meaningful concern.
- Prefer scenario traces that cross the real boundary under discussion, such as
  UI -> Surface/ViewModel -> use case/graph -> socket/API -> server/storage ->
  load/broadcast -> graph -> UI.

Frame the review around architecture invariants, not implementation taste:

- Is the shape minimal and sufficient for the declared goal?
- Does it introduce any parallel data flow or storage?
- Does it introduce any source-of-truth leak?
- Does it violate a public/private boundary?
- Does it create a hidden new authority or lifecycle model?
- Does it duplicate an existing DTO, type, interface, or command path?
- Does it keep generic engine/server responsibilities separate from module
  semantics?

Useful reviewer prompt shape:

```text
Review this architecture plan only. Do not edit files.

Current approved decisions:
- ...

Validate against the current codebase and concrete scenarios. Report only:
- blockers;
- concrete design risks;
- source-of-truth or authority leaks;
- parallel data flow/storage concerns;
- public/private boundary violations;
- remaining decisions that need user approval.

If the shape is minimal and sufficient, say so explicitly.
Do not redesign from scratch unless you find a blocker.
```

## Contract Checks

Before writing API/socket/use-case docs, explicitly settle:

- names
- payloads
- result shape
- success payloads
- error enum/result errors
- idempotency
- broadcast/ACK behavior
- where defaults/input normalization happen

Reuse existing result and naming patterns unless there is an approved reason
not to.

## Docs

Keep docs terse and approved-decision-only. Remove speculative details instead
of leaving them as implied future work. Commit design docs separately from
implementation WIP.

## Planning Gate For Behavior Changes

Before implementing any refactor, classify every touched path as one of:

- Mechanical: same inputs, same outputs, same side effects, same timing and
  ordering observability.
- Behavior-bearing: may change outputs, side effects, validation, accepted
  inputs, ordering, timing, invalidation, caching, stale-state visibility, or
  user-visible interaction.

A behavior-bearing path is not approved by a structural/refactor goal alone. It
must be listed in the plan with:

- the current behavior;
- the proposed behavior;
- the concrete user scenario affected;
- why the change is necessary for the goal;
- explicit user approval.

Do not infer approval for behavior changes from broad phrases like "make it
atomic", "clean it up", "reactive", "remove tech debt", "simplify", or
"improve quality".

If a behavior-bearing path is discovered during implementation, stop and return
to planning. Do not implement it and report afterward.

## Stop Gates

Stop and ask before adding a new storage scope, authority model, lifecycle
model, public surface, socket behavior, merge policy, compatibility shim, or
semantic engine behavior.
