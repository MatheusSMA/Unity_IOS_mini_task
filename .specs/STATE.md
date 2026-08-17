# Project State

## Decisions

| ID | Decision | Status | Date | Rationale |
| -- | -------- | ------ | ---- | --------- |
| AD-001 | Unity 6 (6000.x) + URP; validation target is the Unity Editor only (project stays iOS-exportable, no device build required) | active | 2026-08-17 | User decision during spec Q&A |
| AD-002 | Architecture: light MVC/MVP — plain-C# domain layer + C# `event Action`; no DI framework, no ScriptableObject event system | active | 2026-08-17 | User decision; keeps domain unit-testable in EditMode |
| AD-003 | Input via Input System (new) + EnhancedTouch; UI via uGUI (Canvas) + TextMeshPro | active | 2026-08-17 | User decision |
| AD-004 | Docs, code and UI language: English  | active | 2026-08-17 | User expressed no preference; consistency |
| AD-005 | Tests: Unity Test Framework — EditMode for pure logic, PlayMode for interaction | active | 2026-08-17 | User decision |

## Handoff

**Last session:** 2026-08-17
**Feature:** room-wall-selection
**State:** Specify + Design artifacts written (spec.md validated clean, context.md, design.md draft) + consolidated docs/tecnico.md. Awaiting user approval of spec/design before Tasks phase. No code exists yet (docs-only repo).
**Next step:** On approval → Tasks phase (`tasks.md` breakdown per phase P1 → P2 → P3), then Execute.
