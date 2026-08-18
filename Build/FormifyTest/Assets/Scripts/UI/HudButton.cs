using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Formify.Presentation
{
    /// <summary>
    /// A rail button painted in the art kit's three states (HUD-01 AC1): enabled, disabled and active. The
    /// button's own <see cref="Button.interactable"/> is the source of truth for enabled/disabled and
    /// <see cref="ActiveSource"/> is asked whether the mode this button opens is the current one — no owner has
    /// to remember to repaint. Only the fill, border, label, icon and glow change; nothing here decides
    /// behaviour, and the state dot belongs to whoever wired it (<see cref="WindowModeButton.StateDot"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public class HudButton : MonoBehaviour
    {
        private const float DotSize = 6f;

        private Image _fill;
        private Image _border;
        private Image _icon;
        private Image _glow;
        private Image _dot;
        private TextMeshProUGUI _label;
        private Button _button;

        private bool _painted;
        private bool _lastInteractable;
        private bool _lastActive;

        /// <summary>Asked every frame whether the mode this button opens is the current one. Optional.</summary>
        public Func<bool> ActiveSource { get; set; }

        /// <summary>Whether the button last painted itself as the active one.</summary>
        public bool IsActive => _lastActive;

        /// <summary>Clear uses the kit's neutral palette instead of the green one.</summary>
        public bool UseNeutralPalette { get; set; }

        public Button Button => _button;

        /// <summary>The state dot, or null when the button was built without one. Lit while active.</summary>
        public Graphic Dot => _dot;

        public TextMeshProUGUI Label => _label;

        /// <summary>
        /// Builds the kit's button: Fill + Border, a glow behind it, then icon and label inside a horizontal
        /// layout (padding 14, spacing 9, icon 16 x 16), and optionally the 6 x 6 state dot on the right.
        /// </summary>
        public static HudButton Create(Transform parent, string objectName, string iconSprite, string text,
            Vector2 size, bool withStateDot = false)
        {
            RectTransform root = HudTheme.NewUi(objectName, parent);
            root.anchorMin = new Vector2(1f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(1f, 1f);
            root.sizeDelta = size;

            var hud = root.gameObject.AddComponent<HudButton>();

            // Behind everything, 1.3x the rect, off unless the button is the active one.
            RectTransform glowRect = HudTheme.NewUi("Glow", root);
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = size * 1.3f;
            hud._glow = glowRect.gameObject.AddComponent<Image>();
            hud._glow.sprite = HudTheme.Sprite("glow_radial");
            hud._glow.color = HudTheme.GlowTint;
            hud._glow.raycastTarget = false;
            hud._glow.enabled = false;

            // The fill is the button's raycast target: the border and the artwork must not eat the touch.
            hud._fill = HudTheme.AddImage(root, "Fill", "panel_fill_9s", HudTheme.ButtonFill,
                Image.Type.Sliced, raycastTarget: true);
            hud._border = HudTheme.AddImage(root, "Border", "panel_border_9s", HudTheme.ButtonBorder);

            RectTransform content = HudTheme.Stretch(HudTheme.NewUi("Content", root));
            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 0, 0);
            layout.spacing = 9f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            if (!string.IsNullOrEmpty(iconSprite))
            {
                RectTransform iconRect = HudTheme.NewUi("Icon", content);
                hud._icon = iconRect.gameObject.AddComponent<Image>();
                hud._icon.sprite = HudTheme.Sprite(iconSprite);
                hud._icon.color = HudTheme.Accent;
                hud._icon.raycastTarget = false;
                var iconLayout = iconRect.gameObject.AddComponent<LayoutElement>();
                iconLayout.preferredWidth = 16f;
                iconLayout.preferredHeight = 16f;
                iconLayout.flexibleWidth = 0f;
            }

            hud._label = HudTheme.AddLabel(content, "Label", text.ToUpperInvariant(), 12.5f, 100f,
                HudTheme.ActiveText, TextAlignmentOptions.MidlineLeft);
            hud._label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            if (withStateDot)
            {
                RectTransform dotRect = HudTheme.NewUi("StateDot", root);
                dotRect.anchorMin = new Vector2(1f, 0.5f);
                dotRect.anchorMax = new Vector2(1f, 0.5f);
                dotRect.pivot = new Vector2(1f, 0.5f);
                dotRect.sizeDelta = new Vector2(DotSize, DotSize);
                dotRect.anchoredPosition = new Vector2(-14f, 0f);
                hud._dot = dotRect.gameObject.AddComponent<Image>();
                hud._dot.color = HudTheme.Accent;
                hud._dot.raycastTarget = false;
                hud._dot.enabled = false;
            }

            hud._button = root.gameObject.AddComponent<Button>();
            hud._button.targetGraphic = hud._fill;
            // The palette below IS the state readout; the built-in tint would multiply on top of it.
            hud._button.transition = Selectable.Transition.None;

            hud.Apply();
            return hud;
        }

        /// <summary>Repaints when the button's own state changed — cheap enough to check every frame.</summary>
        private void LateUpdate()
        {
            bool interactable = _button == null || _button.interactable;
            bool active = ActiveSource != null && ActiveSource();
            if (_painted && interactable == _lastInteractable && active == _lastActive) return;

            Apply();
        }

        public void Apply()
        {
            bool interactable = _button == null || _button.interactable;
            bool active = ActiveSource != null && ActiveSource();

            if (_fill != null)
                _fill.color = !interactable ? HudTheme.DisabledFill
                    : active ? HudTheme.ActiveFill
                    : UseNeutralPalette ? HudTheme.NeutralFill : HudTheme.ButtonFill;

            if (_border != null)
                _border.color = !interactable ? HudTheme.DisabledBorder
                    : active ? HudTheme.Accent
                    : UseNeutralPalette ? HudTheme.NeutralBorder : HudTheme.ButtonBorder;

            if (_label != null)
                _label.color = !interactable ? HudTheme.DisabledLabel
                    : active ? HudTheme.ActiveText
                    : UseNeutralPalette ? HudTheme.ActiveText : HudTheme.RowLabel;

            if (_icon != null)
                _icon.color = !interactable ? HudTheme.DisabledLabel : HudTheme.Accent;

            if (_glow != null) _glow.enabled = active;

            _painted = true;
            _lastInteractable = interactable;
            _lastActive = active;
        }
    }
}
