# T01 — Create Packages/manifest.json with pinned packages

**Phase**: 1 (P0 Bootstrap) | **Requirement**: BOOT-01 | **Depends on**: none | **Tests**: none | **Gate**: build

## Functional description

Nothing user-visible yet. This is the foundation of the whole project: it declares every Unity package the app needs, at pinned versions, so the project opens identically on any machine and stays iOS-exportable. Without it, no rendering (URP), no touch input, no AR, no tests, no UI text.

## Technical description

- **File**: `Packages/manifest.json` (agent-written text asset per AD-006).
- Pin, at exact versions verified for Unity 6 (6000.x):
  - `com.unity.render-pipelines.universal` 17.x (ships with Unity 6)
  - `com.unity.inputsystem` (EnhancedTouch + touch simulation, AD-003)
  - `com.unity.xr.arfoundation` 6.x and `com.unity.xr.arkit` 6.x — versions must match (6.4.3 verified for 6000.4 during design)
  - `com.unity.test-framework` (AD-005)
  - `com.unity.ugui` (uGUI + TextMeshPro, AD-003)
- Keep default Unity registry modules; no extra scoped registries.

## Done when

- [ ] All packages listed at pinned versions
- [ ] Unity resolves them without errors on import (build gate)

**Tools**: none (text asset) | **Commit**: `[chore] pin project packages in manifest`
