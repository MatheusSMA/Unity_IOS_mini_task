using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Formify.Presentation
{
    /// <summary>
    /// One row of the surfaces list (HUD-01 AC3). The selected state is a field on the row, not a suffix parsed
    /// out of the label text, so the art kit's green tag and left mark can replace the old "[SELECTED]" suffix
    /// without a single test having to know how the row is painted. <see cref="SurfaceListPanel"/> owns the rows
    /// and is the only caller of <see cref="SetSelected"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class SurfaceRow : MonoBehaviour
    {
        private const float MarkWidth = 2f;
        private const float TagWidth = 62f;

        private TextMeshProUGUI _label;
        private Image _fill;
        private Image _mark;
        private RectTransform _tag;

        /// <summary>The row's own state — the source of truth for "this surface is the selected one".</summary>
        public bool IsSelected { get; private set; }

        /// <summary>The text actually rendered, or null before the row is built.</summary>
        public string Text => _label != null ? _label.text : null;

        public TextMeshProUGUI Label => _label;

        /// <summary>The kit's row: fill, 2 px selection mark, index, name and the green SELECTED tag.</summary>
        public static SurfaceRow Create(Transform parent, string objectName, int index, string surfaceName,
            float height)
        {
            RectTransform root = HudTheme.NewUi(objectName, parent);

            LayoutElement element = root.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;

            var row = root.gameObject.AddComponent<SurfaceRow>();

            row._fill = HudTheme.AddImage(root, "Fill", "row_fill_9s", HudTheme.NeutralFill);

            RectTransform markRect = HudTheme.NewUi("Mark", root);
            markRect.anchorMin = new Vector2(0f, 0f);
            markRect.anchorMax = new Vector2(0f, 1f);
            markRect.pivot = new Vector2(0f, 0.5f);
            markRect.offsetMin = Vector2.zero;
            markRect.offsetMax = Vector2.zero;
            markRect.sizeDelta = new Vector2(MarkWidth, 0f);
            row._mark = markRect.gameObject.AddComponent<Image>();
            row._mark.color = HudTheme.Accent;
            row._mark.raycastTarget = false;
            row._mark.enabled = false;

            RectTransform indexRect = HudTheme.NewUi("Index", root);
            indexRect.anchorMin = new Vector2(0f, 0f);
            indexRect.anchorMax = new Vector2(0f, 1f);
            indexRect.pivot = new Vector2(0f, 0.5f);
            indexRect.offsetMin = new Vector2(10f, 0f);
            indexRect.offsetMax = new Vector2(24f, 0f);
            var indexLabel = indexRect.gameObject.AddComponent<TextMeshProUGUI>();
            indexLabel.text = (index + 1).ToString("00");
            indexLabel.fontSize = 11f;
            indexLabel.characterSpacing = HudTheme.Tracking(40f);
            indexLabel.color = HudTheme.Caption;
            indexLabel.alignment = TextAlignmentOptions.MidlineLeft;
            indexLabel.raycastTarget = false;

            RectTransform labelRect = HudTheme.NewUi("Label", root);
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(30f, 0f);
            labelRect.offsetMax = new Vector2(-(TagWidth + 10f), 0f);
            row._label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            row._label.text = surfaceName;
            row._label.fontSize = 13f;
            row._label.characterSpacing = HudTheme.Tracking(40f);
            row._label.color = HudTheme.RowLabel;
            row._label.alignment = TextAlignmentOptions.MidlineLeft;
            row._label.raycastTarget = false;

            row._tag = HudTheme.NewUi("Tag", root);
            row._tag.anchorMin = new Vector2(1f, 0.5f);
            row._tag.anchorMax = new Vector2(1f, 0.5f);
            row._tag.pivot = new Vector2(1f, 0.5f);
            row._tag.sizeDelta = new Vector2(TagWidth, 16f);
            row._tag.anchoredPosition = new Vector2(-8f, 0f);
            // row_fill_9s, not the hint pill's sprite: pill_fill_9s carries a 24 px corner, which on a 16 px tall
            // tag leaves no straight edge at all and the SELECTED tag renders as an ellipse.
            HudTheme.AddImage(row._tag, "TagFill", "row_fill_9s", HudTheme.Accent);
            HudTheme.AddLabel(row._tag, "TagLabel", "SELECTED", 9f, 120f, HudTheme.TagText,
                TextAlignmentOptions.Center);
            row._tag.gameObject.SetActive(false);

            return row;
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            Repaint();
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
