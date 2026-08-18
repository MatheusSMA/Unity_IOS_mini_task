# HUD art kit — what you need to know about the art

Reference material for the HUD visual pass. This folder is **not** a second feature spec: the requirements it
serves live in `.specs/features/room-wall-selection/spec.md` (HUD-01, and WIN-01 AC1 for the window mode
button). Nothing here overrides that spec — where the kit and the spec disagree, the spec wins until a decision
in `.specs/STATE.md` supersedes it (see [Adopted / not adopted](#adopted--not-adopted)).

| | |
| --- | --- |
| Kit location | `Build/FormifyTest/Assets/Sprite/Game UI mockups for Unity/` (imported at commit `eb0124a`) |
| Kit handoff | `Unity-handoff.md` in that folder — hierarchy, RectTransforms, borders, colours, TMP settings |
| Requirements | HUD-01 (AC1-AC5), WIN-01 AC1 |
| Tasks | T30 (button behaviour), T31 (row state seam), T32 (paint) — `docs/tasks/` |
| Decisions | AD-019 (button disabled, not hidden), AD-020 (landscape) |

---

## 1. Orientation and canvas

The app ships **landscape** — phone held horizontally (AD-020). The kit is authored landscape at
**1183 × 670** reference pixels, so its RectTransform table transfers directly.

- `ProjectSettings.asset` is narrowed to landscape (left + right, no portrait autorotation).
- The CanvasScaler is Scale With Screen Size, Match 0.5. The reference resolution number is an implementation
  choice, not a requirement — AD-020 pins the *look*, not the numbers. Using the kit's 1183 × 670 is simply the
  cheapest way to make the handoff's pixel values transfer without arithmetic.
- The portrait 1170 × 2532 scaler that `SurfaceListPanel` used to build is gone.

## 2. Sprites

36 PNGs in `sprites/`, all **white** — every element is tinted through the Image `color` field, so one set
covers every state. Each sprite ships at three densities (`-1x`, `-2x`, `-3x`); **iOS ships the -3x set**.

All files: Texture Type **Sprite (2D and UI)**, Mesh Type **Full Rect**, Filter **Bilinear**, Compression
**None**, sRGB **on**. Pixels Per Unit **100 / 200 / 300** for -1x / -2x / -3x, so a border that measures 8 px
on the -1x file still measures 8 *reference* px on the -3x file.

| Sprite | Border L T R B (-1x) | Image type | Used for |
| --- | --- | --- | --- |
| `panel_fill_9s` | 8 8 8 8 | Sliced | panels, buttons, toggle, readout |
| `panel_border_9s` | 8 8 8 8 | Sliced | 1 px outline layer over the fill |
| `row_fill_9s` | 6 6 6 6 | Sliced | surface rows, toggle segments |
| `pill_fill_9s` | 24 24 24 24 | Sliced | bottom hint pill |
| `window_border_9s` | 6 6 6 6 | Sliced | placed window outline (2 px) |
| `window_fill_grad` | — | Simple | window fill, top→bottom fade |
| `rule_fade` | — | Simple | hairline dividers, 1 px tall |
| `glow_radial_128` / `_256` | — | Simple | glow behind an active button / selected row |
| `scanline_tile` | — | **Tiled**, Wrap Repeat | full-screen overlay |
| `handle` | — | Simple | window corner handles, 9 × 9 |
| `icon_window` / `icon_trash` / `icon_ar` | — | Simple | 16 px glyphs (use the 64 or 96 file) |

Border values double for -2x and triple for -3x.

## 3. Colour roles

Paste straight into the Image colour field (RGBA hex).

| Hex | Role |
| --- | --- |
| `030A06EE` | panel fill |
| `030A06FC` | rail fill (near-opaque — see [watch out](#6-watch-out)) |
| `35F08A38` | panel border, active button fill |
| `35F08A24` | selected row fill |
| `35F08AFF` | accent: selection mark, header dot, SELECTED tag, active icon, **window mode state dot** |
| `35F08A73` | enabled button border |
| `35F08A1A` | enabled button fill |
| `35F08A33` | glow tint |
| `E9FFF22E` / `E9FFF208` | neutral border / fill (Clear button) |
| `E9FFF21A` / `E9FFF205` | **disabled** border / fill |
| `FF8C8C80` / `FF9D9DFF` | window delete border / icon |
| `EAFFF3FF` | text on an active element, window handles |
| `CFE9DBFF` | row label |
| `7F9D8DFF` | idle label, helper text |
| `5F8271FF` | index numbers, captions |
| `4C6558FF` | **disabled** label |
| `04140BFF` | text inside the green SELECTED tag |

## 4. Typography

| Element | Size / tracking |
| --- | --- |
| Button label | 12.5 · +100 · ALL CAPS · centre |
| Toggle 2D / 3D | 13 · +100 · centre |
| Row label | 13 · +40 · left |
| Panel header | 11 · +140 · ALL CAPS |
| SELECTED tag | 9 · +120 · ALL CAPS |
| Readout / dimensions | mono 15 (11 for the unit) · tracking 0 |
| Hint pill | 11.5 · +50 |

**Known deviation:** the kit specifies Inter Medium / Mono. The project ships only `LiberationSans SDF`
(`Assets/TextMesh Pro/Fonts`), and adding a font family is a licensing and asset decision, not a restyle. The
pass keeps the kit's sizes, tracking, casing and alignment on the default TMP font. Never use TMP faux-bold to
fake the weight — it distorts the SDF.

## 5. Kit node → the script that builds it

The HUD has no prefab: every view is constructed in code, so the kit is applied in these files.

| Kit node | Built by |
| --- | --- |
| `HUD` canvas, `SurfacesPanel`, `Rows`, header, collapse control | `Assets/Scripts/UI/SurfaceListPanel.cs` |
| `Row` / `RowSelected` (mark + SELECTED tag) | `Assets/Scripts/UI/SurfaceRow.cs` |
| `ViewToggle` (`Seg2D` / `Seg3D`) | `Assets/Scripts/UI/ViewSwitchButtons.cs` |
| `RightRail`, `BtnWindowMode` (+ state dot, glow) | `Assets/Scripts/UI/WindowModeButton.cs`, `Presentation/RoomBootstrap.cs` |
| `BtnClear` | `Assets/Scripts/UI/ClearButton.cs` |
| `BtnAR` | `Assets/Scripts/UI/ArToggleButton.cs` |
| `WorldOverlay`, `WindowRect`, handles, `BtnDelete` | `Assets/Scripts/Presentation/WindowView.cs` |
| `Scanlines` overlay, shared sprite/colour helpers | `Assets/Scripts/UI/HudTheme.cs` |

`Readout` and `HintPill` are kit nodes with no behaviour behind them in this build; they are decoration only.

## 6. Watch out

- **Raycast Target off on every decorative Image** — scanlines, glows, borders, dividers. The scanline overlay
  stretches the whole screen; left on, it eats every tap and selection silently stops working everywhere
  (HUD-01 AC4 exists for exactly this, and a PlayMode regression guards it).
- **No backdrop blur in uGUI.** The panels are 93 % opaque; do not fake blur with a stretched screenshot.
- **The rail fill stays near-opaque** (`FC`) or the 3D render reads through it and the layout looks dirty.
- **Windows live in wall space, not screen space** — parent them to a RectTransform matching the wall quad so
  they track the 2D/3D switch.

## Adopted / not adopted

| Kit behaviour | Verdict |
| --- | --- |
| Window mode button interactable only while a wall is selected, disabled palette + state dot | **Adopted** — AD-019 made it the spec rule (WIN-01 AC1); T30 implements, T32 paints |
| Clear asks for confirmation (`CLEAR ALL?`, red palette) | **Not adopted** — CLR-01 says Clear is the single idempotent path to the empty state; a confirmation step is a product change, not a restyle. HUD-01 AC5 records the rule |
