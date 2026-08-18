# Task Breakdown — Room Wall Selection

One document per task, functional + technical description each. Canonical execution index (order, dependencies, test coverage matrix, gates, validation tables): `.specs/features/room-wall-selection/tasks.md`. Sources: `.specs/features/room-wall-selection/spec.md`, `design.md`, `.specs/STATE.md` (AD-001..AD-020).

| Phase | Tasks |
| ----- | ----- |
| 1 — P0 Bootstrap | [T01](T01-package-manifest.md) · [T02](T02-urp-assets.md) · [T03](T03-project-settings.md) · [T04](T04-bootstrap-scene.md) · [T05](T05-assembly-definitions.md) · [T06](T06-p0-verification.md) |
| 2 — Domain core | [T07](T07-domain-types.md) · [T08](T08-window-placement-validator.md) · [T09](T09-roommodel-selection.md) · [T10](T10-roommodel-windows.md) · [T11](T11-surface-mesh-builder.md) · [T12](T12-room-builder.md) |
| 3 — P1 Presentation | [T13](T13-mode-manager.md) · [T14](T14-input-router.md) · [T15](T15-surface-view.md) · [T16](T16-room-bootstrap.md) · [T17](T17-orbit-camera.md) · [T18](T18-selection-controller.md) · [T19](T19-surface-list-panel.md) · [T20](T20-clear-button.md) |
| 4 — P2 Windows + outline | [T21](T21-window-draw-controller.md) · [T22](T22-window-mode-button.md) · [T23](T23-window-view-deletion.md) · [T24](T24-selection-outline.md) |
| 5 — P3 AR + 2D | [T25](T25-ar-pose-camera.md) · [T26](T26-ar-toggle-button.md) · [T27](T27-topdown-controller.md) · [T28](T28-2d-wall-tap-tolerance.md) · [T29](T29-view-switch-buttons.md) |
| 6 — P4 HUD + button UX (open) | [T30](T30-window-mode-button-exit.md) · [T31](T31-surface-row-selected-state.md) · [T32](T32-hud-art-kit.md) |

32 tasks; 25/25 spec requirements mapped. Phases 1-5 are done and verified (`validation.md`); Phase 6 is the remaining work. Both product questions were answered on 2026-08-18 (AD-019 button always visible with a state dot, AD-020 landscape) and none of the three tasks is implemented yet — the owner is still planning T30.
