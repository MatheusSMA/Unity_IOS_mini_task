# T32 — Apply the Room Scanner HUD art kit + PlayMode raycast regression

**Phase**: 6 (P4 follow-up) | **Requirement**: HUD-01 (AC1, AC2, AC4, AC5) | **Depends on**: T30, T31 | **Tests**: PlayMode | **Gate**: build
**Orientation**: landscape, decided in AD-020 (2026-08-18).

## Functional description

The uGUI layer is built in code with grey boxes and white labels. The `Room Scanner HUD` kit (commit `eb0124a`) sits in `Assets/Sprite/Game UI mockups for Unity/` with a complete handoff — hierarchy, RectTransforms, 9-slice borders, colour roles, TMP settings — and none of it is applied. This task applies it.

Two things in the kit are **not** adopted, because they change behaviour rather than appearance:

- **"Clear asks for confirmation"** — CLR-01 says the Clear button is the only path to the empty state and clears idempotently. Adding a confirmation step is a product change, not a restyle.
- **"Window Mode is interactable only while a wall is selected"** with a disabled palette — that is the AD-019 question. The button follows whatever T30 settled; this task only paints it.

HUD-01 AC5 states the rule: where the kit's copy implies behaviour the spec does not have, the spec wins until a decision supersedes it.

## Technical description

- **Orientation (AD-020)**: the app ships landscape, phone held horizontally, so the handoff's RectTransform table transfers directly. Two things follow: `ProjectSettings.asset` currently has `defaultScreenOrientation: 4` with all four autorotations allowed and is narrowed to landscape, and `SurfaceListPanel.cs:148` builds a portrait 1170x2532 CanvasScaler that goes away. The reference resolution itself is an implementation choice — what is required is that the HUD looks like the kit.
- **Import settings** (`Assets/Sprite/Game UI mockups for Unity/sprites/*.png.meta`): Texture Type Sprite (2D and UI), Mesh Type Full Rect, Filter Bilinear, Compression None, sRGB on. 9-slice borders per the handoff table (`panel_*_9s` 8/8/8/8, `row_fill_9s` 6/6/6/6, `pill_fill_9s` 24/24/24/24, `window_border_9s` 6/6/6/6) for the **-1x** files, doubled for -2x and tripled for -3x with Pixels Per Unit 100/200/300 to match. `scanline_tile` is Tiled with Wrap Repeat. iOS ships the -3x set.
- **Construction**: `Assets/Scripts/UI/*.cs` build their own views in code, so the kit is applied there rather than in a prefab — SurfacesPanel, Readout, ViewToggle, RightRail (window mode / Clear / AR), HintPill, WorldOverlay (window rect, handles, delete button) and the Scanlines overlay.
- **Raycast hygiene**: every decorative Image — scanlines, glows, borders, dividers — gets Raycast Target off. The scanline overlay stretches the full screen; left on, it eats every tap and silently breaks selection everywhere.
- **Rows**: the green "SELECTED" tag and the 2 px left mark replace the label suffix. The state itself comes from T31's field, so no test reads the tag.

## Tests (PlayMode)

Regression, not appearance — appearance goes to human UAT. A tap whose screen point lands on the scanline overlay (and on a glow) still selects the surface behind it, keeping EDGE-02 honest: UI blocks raycasts, decoration does not. The full suite reruns unchanged.

## Done when

- [ ] Every sprite the HUD uses carries the handoff's import settings; none left on Unity defaults
- [ ] App is landscape-only in ProjectSettings; the HUD reproduces the kit's layout, proportions and palette
- [ ] PlayMode regression proves decorative images do not consume taps
- [ ] No acceptance criterion outside HUD-01 changes behaviour; full suite green
- [ ] Unity MCP console zero compile errors + `run_tests` EditMode + PlayMode green

**Tools**: unity-mcp (UI, manage_asset, run_tests) + unity-mcp-skill | **Commit**: `[feat] apply room scanner hud art kit`
