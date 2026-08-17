# T03 — Configure ProjectSettings (URP assigned, iOS-exportable, XR)

**Phase**: 1 (P0 Bootstrap) | **Requirement**: BOOT-01 | **Depends on**: T02 | **Tests**: none | **Gate**: build

## Functional description

Makes the project actually use URP, stay exportable to iOS (AD-001: Editor is the validation target, but the project must switch to the iOS build target without errors), and have AR ready: ARKit on device, XR Simulation in the Editor.

## Technical description

- **Files**: `ProjectSettings/` (GraphicsSettings, QualitySettings, ProjectSettings.asset, XR settings) — one cohesive config deliverable.
- Assign the T02 URP asset in **Graphics** and in **every Quality tier** (BOOT-01 AC1).
- Active Input Handling: **Input System (new)** only (AD-003).
- iOS identification: company/product name, bundle identifier (any valid reverse-DNS), minimum iOS version compatible with ARKit XR Plugin 6.x.
- XR Plug-in Management: enable **ARKit** provider for iOS and **XR Simulation** for Editor standalone (AR-01 validation path per spec assumption).
- Color space Linear (URP default).

## Done when

- [ ] URP asset assigned in Graphics and all Quality tiers
- [ ] Input System (new) active
- [ ] ARKit (iOS) + XR Simulation (Editor) enabled
- [ ] No console errors (build gate)

**Tools**: unity-mcp + unity-mcp-skill | **Commit**: `[chore] configure project settings for URP, iOS and XR`
