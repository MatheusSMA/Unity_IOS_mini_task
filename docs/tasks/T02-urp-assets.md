# T02 — Create URP pipeline + renderer assets

**Phase**: 1 (P0 Bootstrap) | **Requirement**: BOOT-01 | **Depends on**: T01 | **Tests**: none | **Gate**: build

## Functional description

The room will render through URP. This task creates the render pipeline configuration the whole app draws with. The renderer asset created here is also where the P2 selection outline (two RenderObjects features, T24) will later be added — so it must exist as a proper, editable asset, not a default.

## Technical description

- **Files**: `Assets/Settings/` — one URP Pipeline Asset + one Universal Renderer asset, cross-referenced.
- Created via Unity MCP; fallback AD-006: agent writes the asset YAML directly.
- Defaults are fine (no HDR/post tuning needed); mobile-reasonable settings (no MSAA requirements in spec).
- Do NOT add RenderObjects features yet — that is T24 (OUT-01 is P2 polish, AD-010).

## Done when

- [ ] URP asset + renderer asset exist and reference each other
- [ ] No console errors on import (build gate)

**Tools**: unity-mcp + unity-mcp-skill | **Commit**: `[chore] add URP pipeline and renderer assets`
