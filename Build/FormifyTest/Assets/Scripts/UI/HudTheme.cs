using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Formify.Presentation
{
    /// <summary>
    /// The Room Scanner HUD art kit in code (HUD-01). Sprites, palette and type live here so every view paints
    /// from one place; the kit's own handoff is `Assets/Sprite/Game UI mockups for Unity/Unity-handoff.md` and
    /// the reference notes are `.specs/features/hud-art-kit/context.md`.
    /// The shipped density is the kit's -3x set, moved to `Assets/Resources/HUD/` under its base name so the
    /// runtime can load it without an inspector reference — the HUD is built in code, not from a prefab.
    /// </summary>
    public static class HudTheme
    {
        /// <summary>The kit is authored landscape at this size (AD-020).</summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(1183f, 670f);

        public static readonly Color PanelFill = Hex(0x030A06EEu);
        public static readonly Color RailFill = Hex(0x030A06FCu);
        public static readonly Color PanelBorder = Hex(0x35F08A38u);
        public static readonly Color ActiveFill = Hex(0x35F08A38u);
        public static readonly Color SelectedRowFill = Hex(0x35F08A24u);
        public static readonly Color Accent = Hex(0x35F08AFFu);
        public static readonly Color ButtonBorder = Hex(0x35F08A73u);
        public static readonly Color ButtonFill = Hex(0x35F08A1Au);
        public static readonly Color GlowTint = Hex(0x35F08A33u);
        public static readonly Color NeutralBorder = Hex(0xE9FFF22Eu);
        public static readonly Color NeutralFill = Hex(0xE9FFF208u);
        public static readonly Color DisabledBorder = Hex(0xE9FFF21Au);
        public static readonly Color DisabledFill = Hex(0xE9FFF205u);
        public static readonly Color DisabledLabel = Hex(0x4C6558FFu);
        public static readonly Color DeleteBorder = Hex(0xFF8C8C80u);
        public static readonly Color DeleteIcon = Hex(0xFF9D9DFFu);

        /// <summary>The kit's destructive fill. It paints window deletion (WIN-04), never Clear — see HUD-01 AC5.</summary>
        public static readonly Color DeleteFill = Hex(0x3C0A0A73u);

        public static readonly Color ActiveText = Hex(0xEAFFF3FFu);
        public static readonly Color RowLabel = Hex(0xCFE9DBFFu);
        public static readonly Color IdleLabel = Hex(0x7F9D8DFFu);
        public static readonly Color Caption = Hex(0x5F8271FFu);
        public static readonly Color TagText = Hex(0x04140BFFu);
        public static readonly Color ScanlineTint = Hex(0xFFFFFF0Du);

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        /// <summary>RGBA hex exactly as the kit writes it, e.g. 0x35F08AFF — sRGB, the way the kit quotes it.</summary>
        public static Color Hex(uint rgba)
        {
            return new Color(
                ((rgba >> 24) & 0xFF) / 255f,
                ((rgba >> 16) & 0xFF) / 255f,
                ((rgba >> 8) & 0xFF) / 255f,
                (rgba & 0xFF) / 255f);
        }

        /// <summary>
        /// The project renders in Linear colour space, and the two halves of uGUI disagree about what a vertex
        /// colour means there: TextMeshPro treats it as sRGB, plain Image treats it as linear. Left alone, the
        /// same kit hex paints correct text over panels lifted from near-black to grey — the palette only looks
        /// right on one of them at a time. <c>vertexColorAlwaysGammaSpace</c> settles it for the whole canvas:
        /// every colour here is read as the sRGB the kit wrote.
        /// </summary>
        public static void ApplyColorSpace(Canvas canvas)
        {
            if (canvas != null) canvas.vertexColorAlwaysGammaSpace = true;
        }

        /// <summary>
        /// The kit quotes tracking the way design tools do (thousandths of an em); TMP counts hundredths, so
        /// "+140" is 14 here. Returns null-safe values for a missing sprite: an Image with no sprite still
        /// renders its colour, so a stripped Resources folder degrades to flat boxes instead of throwing.
        /// </summary>
        public static float Tracking(float kitTracking) => kitTracking / 10f;

        public static Sprite Sprite(string spriteName)
        {
            Sprite sprite;
            if (Sprites.TryGetValue(spriteName, out sprite) && sprite != null) return sprite;

            sprite = Resources.Load<Sprite>("HUD/" + spriteName);
            Sprites[spriteName] = sprite;
            return sprite;
        }

        public static RectTransform NewUi(string objectName, Transform parent)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        /// <summary>A stretched child Image. Decoration by default: HUD-01 AC4 keeps taps off the artwork.</summary>
        public static Image AddImage(Transform parent, string objectName, string spriteName, Color color,
            Image.Type type = Image.Type.Sliced, bool raycastTarget = false)
        {
            Image image = Stretch(NewUi(objectName, parent)).gameObject.AddComponent<Image>();
            image.sprite = Sprite(spriteName);
            image.type = image.sprite != null ? type : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        /// <summary>
        /// Fill + border, the kit's two-Image recipe for every panel and button. The fill IS a raycast target:
        /// a panel is opaque HUD, so a tap on it must stop there instead of falling through into the room
        /// (EDGE-02). The border on top of it is decoration and never blocks (HUD-01 AC4).
        /// </summary>
        public static void AddPanelBackground(Transform parent, Color fill, Color border,
            string fillSprite = "panel_fill_9s", string borderSprite = "panel_border_9s")
        {
            AddImage(parent, "Fill", fillSprite, fill, Image.Type.Sliced, raycastTarget: true);
            AddImage(parent, "Border", borderSprite, border);
        }

        public static TextMeshProUGUI AddLabel(Transform parent, string objectName, string text, float size,
            float kitTracking, Color color, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI label = Stretch(NewUi(objectName, parent)).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.characterSpacing = Tracking(kitTracking);
            label.color = color;
            label.alignment = alignment;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            return label;
        }

        /// <summary>The kit's hairline divider: 1 px of rule_fade, never a raycast target.</summary>
        public static Image AddDivider(Transform parent, string objectName = "Divider")
        {
            Image divider = AddImage(parent, objectName, "rule_fade", PanelBorder, Image.Type.Simple);
            return divider;
        }

        /// <summary>
        /// The full-screen scanline overlay. It covers everything, so its Raycast Target must stay off or it
        /// eats every touch and selection stops working across the whole app (HUD-01 AC4).
        /// </summary>
        public static Image AddScanlines(Canvas canvas)
        {
            Image scanlines = AddImage(canvas.transform, "Scanlines", "scanline_tile", ScanlineTint, Image.Type.Tiled);
            scanlines.transform.SetAsLastSibling();
            return scanlines;
        }
    }
}
