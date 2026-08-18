using Formify.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Formify.Presentation
{
    /// <summary>
    /// The art kit's `HintPill` (HUD-01 AC2): one line at the bottom of the screen saying what this mode does.
    /// It follows <see cref="ModeManager.ModeChanged"/> and nothing else.
    /// The copy describes what the app actually does, not what the kit's mock-up implies — the mock shows a
    /// tap-to-place window with resize handles, while WIN-02 cuts the opening from a single drag (HUD-01 AC5:
    /// where the kit's copy implies behaviour the spec does not have, the spec wins).
    /// </summary>
    [DisallowMultipleComponent]
    public class HudHintPill : MonoBehaviour
    {
        private const float DotSize = 6f;

        // Serialized: the baked scene HUD (AD-025) carries the line; Configure only rebinds the modes.
        [SerializeField] private TextMeshProUGUI label;

        private ModeManager _modes;

        /// <summary>The line currently rendered. Null before the pill is built.</summary>
        public string Text => label != null ? label.text : null;

        /// <summary>The kit's HintPill node: anchor .5,0 · pivot .5,0 · pos 0,16 · height 36 · pill_fill_9s.</summary>
        public static HudHintPill Create(Transform parent, float bottomMargin = 16f, float height = 36f)
        {
            RectTransform root = HudTheme.NewUi("HintPill", parent);
            root.anchorMin = new Vector2(0.5f, 0f);
            root.anchorMax = new Vector2(0.5f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = new Vector2(0f, bottomMargin);
            root.sizeDelta = new Vector2(0f, height);

            var fill = root.gameObject.AddComponent<Image>();
            fill.sprite = HudTheme.Sprite("pill_fill_9s");
            if (fill.sprite != null) fill.type = Image.Type.Sliced;
            fill.color = HudTheme.PanelFill;
            // Opaque HUD, so it swallows the tap rather than letting it through to the room (EDGE-02).
            fill.raycastTarget = true;

            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 0, 0);
            layout.spacing = 9f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // The pill hugs its copy: every mode's line is a different length.
            var fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            RectTransform dotRect = HudTheme.NewUi("Dot", root);
            var dot = dotRect.gameObject.AddComponent<Image>();
            dot.color = HudTheme.Accent;
            dot.raycastTarget = false;
            LayoutElement dotLayout = dotRect.gameObject.AddComponent<LayoutElement>();
            dotLayout.preferredWidth = DotSize;
            dotLayout.preferredHeight = DotSize;
            dotLayout.flexibleWidth = 0f;

            var pill = root.gameObject.AddComponent<HudHintPill>();
            pill.label = HudTheme.AddLabel(root, "Label", TextFor(Mode.Orbit), 11.5f, 50f, HudTheme.RowLabel,
                TextAlignmentOptions.Center);
            return pill;
        }

        /// <summary>Binds the modes. Safe to call again: the previous subscription is dropped first.</summary>
        public void Configure(ModeManager modes)
        {
            Unsubscribe();

            _modes = modes;
            if (_modes != null) _modes.ModeChanged += OnModeChanged;

            Refresh();
        }

        private void OnModeChanged(Mode previous, Mode current) => Refresh();

        private void Refresh()
        {
            if (label != null) label.text = TextFor(_modes != null ? _modes.Current : Mode.Orbit);
        }

        private static string TextFor(Mode mode)
        {
            switch (mode)
            {
                case Mode.WindowDraw:
                    return "Window mode · drag on the wall to cut an opening";
                case Mode.TopDown:
                    return "Plan view · tap a wall to select · pinch to zoom";
                case Mode.Ar:
                    return "AR · move the device to look around the room";
                default:
                    return "Tap a surface to select · drag to orbit";
            }
        }

        private void Unsubscribe()
        {
            if (_modes != null) _modes.ModeChanged -= OnModeChanged;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _modes = null;
        }
    }
}
