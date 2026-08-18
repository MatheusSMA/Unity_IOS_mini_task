using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Formify.Presentation
{
    /// <summary>
    /// One row of the surfaces list (HUD-01 AC3). The selected state is a field on the row, not a suffix parsed
    /// out of the label text, so the art kit's green tag and left mark can replace the old "[SELECTED]" suffix
    /// without a single test having to know how the row is painted. <see cref="SurfaceListPanel"/> owns the rows
    /// and is the only caller of <see cref="SetSelected"/>.
    /// The same row draws a window (LIST-03): indented, without an index column, and a wall that carries windows
    /// gets the disclosure dot that collapses them — same shape, two depths.
    /// </summary>
    [DisallowMultipleComponent]
    public class SurfaceRow : MonoBehaviour
    {
        private const float MarkWidth = 2f;
        private const float TagWidth = 62f;
        private const float DiscloseHit = 24f;
        private const float DiscloseDot = 6f;

        private TextMeshProUGUI _label;
        private Button _button;
        private Image _fill;
        private Image _mark;
        private RectTransform _tag;
        private Image _discloseDot;

        /// <summary>The row's own state — the source of truth for "this surface is the selected one".</summary>
        public bool IsSelected { get; private set; }

        /// <summary>The text actually rendered, or null before the row is built.</summary>
        public string Text => _label != null ? _label.text : null;

        public TextMeshProUGUI Label => _label;

        /// <summary>The row's own button, or null when it was built without a click handler.</summary>
        public Button Button => _button;

        /// <summary>
        /// The fold indicator, or null on a row that can hold nothing. It is a readout, not a control — the
        /// whole wall row folds its windows, so a 6 px target would only steal taps from the row itself.
        /// </summary>
        public Graphic DiscloseIndicator => _discloseDot;

        /// <summary>
        /// The kit's row: fill, 2 px selection mark, index, name and the green SELECTED tag.
        /// <paramref name="index"/> below 1 leaves the index column out (a window row is identified by its
        /// indent and its label, not by a number). <paramref name="withDisclosure"/> adds the fold indicator.
        /// </summary>
        public static SurfaceRow Create(Transform parent, string objectName, int index, string surfaceName,
            float height, UnityAction onClick = null, float indent = 0f, bool withDisclosure = false)
        {
            RectTransform root = HudTheme.NewUi(objectName, parent);

            LayoutElement element = root.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;

            var row = root.gameObject.AddComponent<SurfaceRow>();

            // The fill is the row's hit area: the kit draws these as list items, so they behave like list items.
            row._fill = HudTheme.AddImage(root, "Fill", "row_fill_9s", HudTheme.NeutralFill, Image.Type.Sliced,
                raycastTarget: true);

            if (onClick != null)
            {
                row._button = root.gameObject.AddComponent<Button>();
                row._button.targetGraphic = row._fill;
                // The palette below IS the state readout; uGUI's tint would multiply on top of it.
                row._button.transition = Selectable.Transition.None;
                row._button.onClick.AddListener(onClick);
            }

            RectTransform markRect = HudTheme.NewUi("Mark", root);
            markRect.anchorMin = new Vector2(0f, 0f);
            markRect.anchorMax = new Vector2(0f, 1f);
            markRect.pivot = new Vector2(0f, 0.5f);
            markRect.offsetMin = new Vector2(indent, 0f);
            markRect.offsetMax = new Vector2(indent, 0f);
            markRect.sizeDelta = new Vector2(MarkWidth, 0f);
            row._mark = markRect.gameObject.AddComponent<Image>();
            row._mark.color = HudTheme.Accent;
            row._mark.raycastTarget = false;
            row._mark.enabled = false;

            float labelLeft = indent + 12f;

            if (index >= 1)
            {
                RectTransform indexRect = HudTheme.NewUi("Index", root);
                indexRect.anchorMin = new Vector2(0f, 0f);
                indexRect.anchorMax = new Vector2(0f, 1f);
                indexRect.pivot = new Vector2(0f, 0.5f);
                indexRect.offsetMin = new Vector2(indent + 10f, 0f);
                indexRect.offsetMax = new Vector2(indent + 24f, 0f);
                var indexLabel = indexRect.gameObject.AddComponent<TextMeshProUGUI>();
                indexLabel.text = index.ToString("00");
                indexLabel.fontSize = 11f;
                indexLabel.characterSpacing = HudTheme.Tracking(40f);
                indexLabel.color = HudTheme.Caption;
                indexLabel.alignment = TextAlignmentOptions.MidlineLeft;
                indexLabel.raycastTarget = false;

                labelLeft = indent + 30f;
            }

            float tagInset = withDisclosure ? 8f + DiscloseHit : 8f;

            RectTransform labelRect = HudTheme.NewUi("Label", root);
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(labelLeft, 0f);
            labelRect.offsetMax = new Vector2(-(TagWidth + tagInset + 2f), 0f);
            row._label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            row._label.text = surfaceName;
            row._label.fontSize = index >= 1 ? 13f : 11f;
            row._label.characterSpacing = HudTheme.Tracking(40f);
            row._label.color = HudTheme.RowLabel;
            row._label.alignment = TextAlignmentOptions.MidlineLeft;
            row._label.raycastTarget = false;

            row._tag = HudTheme.NewUi("Tag", root);
            row._tag.anchorMin = new Vector2(1f, 0.5f);
            row._tag.anchorMax = new Vector2(1f, 0.5f);
            row._tag.pivot = new Vector2(1f, 0.5f);
            row._tag.sizeDelta = new Vector2(TagWidth, 16f);
            row._tag.anchoredPosition = new Vector2(-tagInset, 0f);
            // row_fill_9s, not the hint pill's sprite: pill_fill_9s carries a 24 px corner, which on a 16 px tall
            // tag leaves no straight edge at all and the SELECTED tag renders as an ellipse.
            HudTheme.AddImage(row._tag, "TagFill", "row_fill_9s", HudTheme.Accent);
            HudTheme.AddLabel(row._tag, "TagLabel", "SELECTED", 9f, 120f, HudTheme.TagText,
                TextAlignmentOptions.Center);
            row._tag.gameObject.SetActive(false);

            if (withDisclosure) BuildDisclosure(row, root);

            return row;
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            Repaint();
        }

        /// <summary>Window rows are numbered per wall, so the text changes when a sibling is deleted.</summary>
        public void SetLabel(string text)
        {
            if (_label != null) _label.text = text;
        }

        /// <summary>
        /// The disclosure control is built once and hidden while the wall carries no windows: building it later
        /// would reflow the row the moment the first window lands, and the label would jump.
        /// </summary>
        public void SetDiscloseVisible(bool visible)
        {
            if (_discloseDot != null) _discloseDot.gameObject.SetActive(visible);
        }

        /// <summary>Paints the disclosure control. Lit means the children are showing, like the panel header.</summary>
        public void SetExpanded(bool expanded)
        {
            if (_discloseDot != null) _discloseDot.color = expanded ? HudTheme.Accent : HudTheme.IdleLabel;
        }

        /// <summary>
        /// The kit has no chevron, and the panel header already says "collapsed" with a dimmed dot — so the row
        /// borrows that vocabulary instead of inventing art. It is an indicator only: the wall row itself folds
        /// its windows, so a control here would carve a dead spot out of the row's own hit area.
        /// </summary>
        private static void BuildDisclosure(SurfaceRow row, RectTransform root)
        {
            RectTransform dot = HudTheme.NewUi("Disclose", root);
            dot.anchorMin = new Vector2(1f, 0.5f);
            dot.anchorMax = new Vector2(1f, 0.5f);
            dot.pivot = new Vector2(1f, 0.5f);
            dot.sizeDelta = new Vector2(DiscloseDot, DiscloseDot);
            dot.anchoredPosition = new Vector2(-(DiscloseHit - DiscloseDot) * 0.5f - 4f, 0f);

            row._discloseDot = dot.gameObject.AddComponent<Image>();
            row._discloseDot.color = HudTheme.Accent;
            row._discloseDot.raycastTarget = false;
        }

        private void Repaint()
        {
            if (_fill != null) _fill.color = IsSelected ? HudTheme.SelectedRowFill : HudTheme.NeutralFill;
            if (_mark != null) _mark.enabled = IsSelected;
            if (_label != null) _label.color = IsSelected ? HudTheme.ActiveText : HudTheme.RowLabel;
            if (_tag != null) _tag.gameObject.SetActive(IsSelected);
        }
    }
}
