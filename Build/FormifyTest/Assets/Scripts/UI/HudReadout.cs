using System.Globalization;
using Formify.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Formify.Presentation
{
    /// <summary>
    /// The art kit's `Readout` panel (HUD-01 AC2): which surface is selected, how big it is, and how many
    /// windows it carries. It reads <see cref="RoomModel"/> directly — selection and window events are the only
    /// inputs, so no controller has to remember to push to it, and it holds no state of its own.
    /// Decoration in the kit's hierarchy sense, but a readout that lies is worse than no readout, so the numbers
    /// are live.
    /// </summary>
    [DisallowMultipleComponent]
    public class HudReadout : MonoBehaviour
    {
        private const float ChipWidth = 18f;
        private const float TextInset = 14f;

        private RoomModel _model;
        private TextMeshProUGUI _caption;
        private TextMeshProUGUI _dimensions;
        private TextMeshProUGUI _helper;
        private RectTransform _helperRect;
        private RectTransform _chip;
        private TextMeshProUGUI _chipLabel;

        /// <summary>The surface name line, uppercased. Empty state included — never null once built.</summary>
        public string Caption => _caption != null ? _caption.text : null;

        public string Dimensions => _dimensions != null ? _dimensions.text : null;

        public string Helper => _helper != null ? _helper.text : null;

        /// <summary>Whether the green window-count chip is showing (kit: only from the first window on).</summary>
        public bool IsCountChipShown => _chip != null && _chip.gameObject.activeSelf;

        /// <summary>The kit's Readout node: anchor 0,1 · pivot 0,1 · pos 8,-404 · size 250x92.</summary>
        public static HudReadout Create(Transform parent, Vector2 anchoredPosition, Vector2 size)
        {
            RectTransform root = HudTheme.NewUi("Readout", parent);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.anchoredPosition = anchoredPosition;
            root.sizeDelta = size;
            HudTheme.AddPanelBackground(root, HudTheme.PanelFill, HudTheme.PanelBorder);

            var readout = root.gameObject.AddComponent<HudReadout>();

            readout._caption = Band(root, "Caption", 14f, 14f, 11f, 140f, HudTheme.Caption);
            readout._dimensions = Band(root, "Dimensions", 32f, 24f, 15f, 0f, HudTheme.Accent);

            readout._helperRect = BandRect(root, "Helper", 62f, 16f);
            readout._helper = Label(readout._helperRect, 11.5f, 50f, HudTheme.IdleLabel);

            readout._chip = HudTheme.NewUi("CountChip", root);
            readout._chip.anchorMin = new Vector2(0f, 1f);
            readout._chip.anchorMax = new Vector2(0f, 1f);
            readout._chip.pivot = new Vector2(0f, 1f);
            readout._chip.anchoredPosition = new Vector2(TextInset, -62f);
            readout._chip.sizeDelta = new Vector2(ChipWidth, 16f);
            // row_fill_9s for the same reason as the SELECTED tag: pill_fill_9s's 24 px corner has no room here.
            HudTheme.AddImage(readout._chip, "ChipFill", "row_fill_9s", HudTheme.Accent);
            readout._chipLabel = HudTheme.AddLabel(readout._chip, "ChipLabel", "0", 10f, 0f, HudTheme.TagText,
                TextAlignmentOptions.Center);
            readout._chip.gameObject.SetActive(false);

            readout.Refresh();
            return readout;
        }

        /// <summary>Binds the model. Safe to call again: the previous subscriptions are dropped first.</summary>
        public void Configure(RoomModel model)
        {
            Unsubscribe();

            _model = model;

            if (_model != null)
            {
                _model.SelectionChanged += OnSelectionChanged;
                _model.WindowAdded += OnWindowChanged;
                _model.WindowRemoved += OnWindowChanged;
            }

            Refresh();
        }

        private void OnSelectionChanged(int? previous, int? current) => Refresh();

        private void OnWindowChanged(WindowSpec spec) => Refresh();

        private void Refresh()
        {
            SurfaceDefinition selected = null;
            if (_model != null && _model.SelectedSurfaceId.HasValue)
                selected = _model.GetSurface(_model.SelectedSurfaceId.Value);

            if (selected == null)
            {
                SetText(_caption, "NO SURFACE");
                SetText(_dimensions, "--  ×  -- <size=11>m</size>");
                SetText(_helper, "tap a surface to select");
                ShowChip(false, 0);
                return;
            }

            SetText(_caption, selected.name.ToUpperInvariant());
            // Invariant, not the device locale: the kit shows a decimal point, and a phone set to pt-BR would
            // otherwise render "4,00 × 2,80".
            SetText(_dimensions,
                selected.width.ToString("0.00", CultureInfo.InvariantCulture) + "  ×  " +
                selected.height.ToString("0.00", CultureInfo.InvariantCulture) + " <size=11>m</size>");

            int windows = _model.GetWindows(selected.id).Count;
            SetText(_helper, windows == 0 ? "0 windows placed"
                : windows == 1 ? "window placed"
                : "windows placed");
            ShowChip(windows > 0, windows);
        }

        /// <summary>The kit puts the count in a green chip; with none placed the line reads as plain helper text.</summary>
        private void ShowChip(bool show, int windows)
        {
            if (_chip != null)
            {
                _chip.gameObject.SetActive(show);
                if (show && _chipLabel != null) _chipLabel.text = windows.ToString();
            }

            if (_helperRect == null) return;

            float inset = show ? TextInset + ChipWidth + 6f : TextInset;
            _helperRect.offsetMin = new Vector2(inset, _helperRect.offsetMin.y);
        }

        private static void SetText(TextMeshProUGUI label, string text)
        {
            if (label != null) label.text = text;
        }

        private static TextMeshProUGUI Band(RectTransform parent, string objectName, float top, float height,
            float size, float kitTracking, Color color)
        {
            return Label(BandRect(parent, objectName, top, height), size, kitTracking, color);
        }

        /// <summary>A left-aligned band `top` px below the panel's top edge, inset on both sides.</summary>
        private static RectTransform BandRect(RectTransform parent, string objectName, float top, float height)
        {
            RectTransform rect = HudTheme.NewUi(objectName, parent);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = new Vector2(TextInset, -(top + height));
            rect.offsetMax = new Vector2(-TextInset, -top);
            return rect;
        }

        private static TextMeshProUGUI Label(RectTransform rect, float size, float kitTracking, Color color)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = size;
            label.characterSpacing = HudTheme.Tracking(kitTracking);
            label.color = color;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            return label;
        }

        private void Unsubscribe()
        {
            if (_model == null) return;

            _model.SelectionChanged -= OnSelectionChanged;
            _model.WindowAdded -= OnWindowChanged;
            _model.WindowRemoved -= OnWindowChanged;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _model = null;
        }
    }
}
