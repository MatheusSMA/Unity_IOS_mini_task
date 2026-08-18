# Room Scanner HUD — Unity uGUI asset set

Reference resolution: **1183 × 670** (CanvasScaler: Scale With Screen Size, Match 0.5).
All sprites are **white** — tint them per element with the Image color field. Ship the `-3x` set for iOS (files are named e.g. `panel_fill_9s-3x.png`).

## Import settings (all files)
Texture Type: Sprite (2D and UI) · Mesh Type: Full Rect · Filter: Bilinear · Compression: None · sRGB: on
Pixels Per Unit: 100 (-1x) / 200 (-2x) / 300 (-3x)

## 9-slice borders (-1x values — double for -2x, triple for -3x)
| Sprite | Border L T R B | Image type | Used for |
|---|---|---|---|
| panel_fill_9s | 8 8 8 8 | Sliced | panels, buttons, toggle, readout |
| panel_border_9s | 8 8 8 8 | Sliced | 1px outline layer over the fill |
| row_fill_9s | 6 6 6 6 | Sliced | surface rows, toggle segments |
| pill_fill_9s | 24 24 24 24 | Sliced | bottom hint pill |
| window_border_9s | 6 6 6 6 | Sliced | placed-window outline (2px) |
| window_fill_grad | — | Simple | window fill, top→bottom fade |
| rule_fade | — | Simple | hairline dividers (1px tall) |
| glow_radial_128 / _256 | — | Simple | glow behind active button / selected row |
| scanline_tile | — | Tiled, Wrap Mode Repeat | full-screen overlay |
| handle | — | Simple | window corner handles, 9×9 |
| icon_window / icon_trash / icon_ar | — | Simple | 16px glyphs (use the 64 or 96 file) |

## Colors (RGBA hex)
030A06EE panel fill · 030A06FC rail fill · 35F08A38 panel border + active fill · 35F08A24 selected row ·
35F08AFF accent mark/dot/tag · 35F08A73 button border · 35F08A1A button fill · 35F08A33 glow ·
E9FFF22E / E9FFF208 neutral (Clear) · FF8C8C80 delete border · FF9D9DFF delete icon ·
EAFFF3FF active text + handles · CFE9DBFF row label · 7F9D8DFF idle · 5F8271FF caption · 4C6558FF disabled · 04140BFF text on green tag

## Type (TextMeshPro, Inter Medium)
Button label 12.5 / +100 caps · toggle 13 / +100 · row 13 / +40 · header 11 / +140 caps ·
tag 9 / +120 caps · readout mono 15 · hint 11.5 / +50. No faux-bold.

Full RectTransform table: see `Unity Handoff.dc.html` in the project.
