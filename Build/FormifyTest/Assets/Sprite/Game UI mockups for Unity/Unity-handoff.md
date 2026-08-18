# Room Scanner HUD — uGUI handoff

**ROOM SCANNER HUD · uGUI HANDOFF**

Rebuild 1:1 in uGUI. Every value below is in reference pixels at **1183 × 670**. Sprites are white so a single set covers all states — you tint per element. Files: `unity/sprites/`

---

## 1 · Canvas

| | |
|---|---|
| Canvas | Render Mode · Screen Space – Overlay · Pixel Perfect off |
| CanvasScaler | Scale With Screen Size · Reference 1183 × 670 · Match 0.5 |
| Root object | HUD (RectTransform stretched, anchors 0,0 → 1,1, offsets 0) |
| Sort order | Scanlines on top (Raycast Target off), then Rail, Panels, WorldOverlay |

## 2 · Sprite import settings

| Sprite | Border (L T R B) | Image type | Used for |
|---|---|---|---|
| panel_fill_9s | 8 8 8 8 | Sliced | panels, buttons, toggle, readout |
| panel_border_9s | 8 8 8 8 | Sliced | 1px outline layer over the fill |
| row_fill_9s | 6 6 6 6 | Sliced | surface rows, toggle segments |
| pill_fill_9s | 24 24 24 24 | Sliced | bottom hint pill |
| window_border_9s | 6 6 6 6 | Sliced | placed window outline (2px) |
| window_fill_grad | — | Simple | window fill, top→bottom fade |
| rule_fade | — | Simple | hairline dividers (1px tall) |
| glow_radial_256 | — | Simple | behind active button / selected row |
| scanline_tile | — | Tiled · Wrap Repeat | full-screen overlay |
| handle | — | Simple | window corner handles 9×9 |
| icon_window / _trash / _ar | — | Simple | 16px glyphs, use the 64 or 96 file |

All: Texture Type **Sprite (2D and UI)**, Mesh Type **Full Rect**, Filter Bilinear, Compression None, sRGB on. Border values above are for the **-1x** files — double them for **-2x**, triple for **-3x**, and set Pixels Per Unit to 100 / 200 / 300 to match so the 8px radius still measures 8 reference px. iOS: ship the -3x set.

## 3 · Hierarchy & RectTransforms

```
HUD
├─ SurfacesPanel      anchor 0,1 · pivot 0,1 · pos   8,-8   · size 250×312
│  ├─ Fill/Border      stretch, offsets 0            (two Images, same rect)
│  ├─ Header            anchor stretch-top · height 38 · padding 12
│  │  ├─ Dot           6×6 · pos 12,-16 · color 35F08A
│  │  └─ Label         "SURFACES" · 11 / +140 tracking
│  └─ Rows (VerticalLayoutGroup, spacing 2, padding 6)
│     ├─ Row           height 40 · row_fill_9s · Index 14 wide · Label 13
│     ├─ RowSelected   + Mark 2×40 left · + Tag "SELECTED" 9 / +120
│     └─ Divider       height 1 · rule_fade · margin 5 10
├─ Readout            anchor 0,1 · pivot 0,1 · pos   8,-404 · size 250×92
├─ ViewToggle         anchor .5,1 · pivot .5,1 · pos 0,-12 · size 146×44
│  └─ Seg2D / Seg3D    66×38 · row_fill_9s · HorizontalLayout padding 3
├─ RightRail          anchor 1,0→1,1 · pivot 1,.5 · width 264 · offset 0
│  ├─ RailFill         stretch · panel_fill_9s · 030A06FC
│  ├─ BtnWindowMode    anchor 1,1 · pivot 1,1 · pos -14,-12  · size 212×46
│  ├─ BtnClear         anchor 1,1 · pivot 1,1 · pos -14,-68  · size 212×46
│  ├─ Divider          anchor 1,1 · pivot 1,1 · pos -14,-124 · size 212×1
│  └─ BtnAR            anchor 1,1 · pivot 1,1 · pos -14,-132 · size 212×46
├─ HintPill           anchor .5,0 · pivot .5,0 · pos 0,16 · height 36 · pill_fill_9s
├─ WorldOverlay       stretch (windows live here, in wall space)
│  └─ WindowRect       size 300×190 · window_border_9s + window_fill_grad
│     ├─ Handle ×4     9×9 · pivot centre · corner offset -4,-4 · EAFFF3
│     └─ BtnDelete     46×46 · anchor 1,1 · pivot .5,.5 · pos 23,56 · icon_trash
└─ Scanlines          stretch · scanline_tile · Tiled · Raycast Target off
```

Button internals: HorizontalLayoutGroup, padding L/R 14, spacing 9, icon 16×16, label vertically centred. Every button gets Fill + Border Images and a Glow child (glow_radial_256, 1.3× the rect, behind) that is only enabled in the active state.

## 4 · Colors — paste into the Image color field (RGBA hex)

| Hex | Role |
|---|---|
| 030A06EE | panel fill |
| 030A06FC | rail fill (opaque) |
| 35F08A38 | panel border, active fill |
| 35F08A24 | selected row fill |
| 35F08AFF | mark, dot, tag, icon accent |
| 35F08A73 | enabled button border |
| 35F08A1A | enabled button fill |
| 35F08A33 | glow tint |
| E9FFF22E | neutral border (Clear) |
| E9FFF208 | neutral fill (Clear) |
| FF8C8C80 | delete border |
| FF9D9DFF | delete icon |
| EAFFF3FF | text on active, handles |
| CFE9DBFF | row label default |
| 7F9D8DFF | idle label, helper text |
| 5F8271FF | index numbers, captions |
| 4C6558FF | disabled label |
| 04140BFF | text inside the green tag |

## 5 · TextMeshPro

| | |
|---|---|
| Button label | Inter Medium · 12.5 · tracking +100 · ALL CAPS · centre |
| Toggle 2D / 3D | Inter Medium · 13 · tracking +100 · centre |
| Row label | Inter Medium · 13 · tracking +40 · left |
| Panel header | Inter Medium · 11 · tracking +140 · ALL CAPS |
| SELECTED tag | Inter Medium · 9 · tracking +120 · ALL CAPS |
| Readout / dims | Mono Regular · 15 (11 for the unit) · tracking 0 |
| Hint pill | Inter Regular · 11.5 · tracking +50 |
| Atlas | Inter SDF, 8-pixel padding, no bold/italic styles — never use TMP faux-bold |

## 6 · Watch out

- No backdrop blur in uGUI. The panels are 93% opaque, so skip it — don't fake it with a stretched blurred screenshot.
- The rail fill must stay near-opaque (FC) or the render behind it reads through and the layout looks dirty.
- Window Mode is interactable only while a wall is selected — disabled state is border E9FFF21A, fill E9FFF205, label 4C6558.
- Clear asks for confirmation: same button, border FF8C8C80, fill 3C0A0A73, label "CLEAR ALL?".
- Set every decorative Image's Raycast Target to off — scanlines, glows, borders, dividers — or they eat touches.
- Windows live in wall space, not screen space: parent them to a RectTransform that matches the wall quad so they track the 2D/3D switch.
