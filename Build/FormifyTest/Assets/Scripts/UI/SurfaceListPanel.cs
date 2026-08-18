using System;
using System.Collections.Generic;
using Formify.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Formify.Presentation
{
    /// <summary>
    /// The state readout (LIST-01, LIST-02): one row per surface in <see cref="RoomModel.Surfaces"/> order,
    /// live-bound to <see cref="RoomModel.SelectionChanged"/> — exactly the two affected rows are rewritten per
    /// event (AD-007). Collapsing hides the row container, never the binding (EDGE-06); the header doubles as
    /// the collapse control and stays visible either way (LIST AC4).
    /// Owns the application Canvas built entirely in code — later UI tasks attach to <see cref="Canvas"/> — and
    /// paints itself with the art kit (HUD-01) through <see cref="HudTheme"/>. The canvas is landscape: the
    /// scaler matches the kit's reference resolution (AD-020).
    /// </summary>
    public class SurfaceListPanel : MonoBehaviour
    {
        private const string RowNamePrefix = "Row_";

        [SerializeField] private float panelWidth = 250f;
        [SerializeField] private float panelHeight = 312f;
        [SerializeField] private float rowHeight = 40f;
        [SerializeField] private float headerHeight = 38f;
        [SerializeField] private float panelMargin = 8f;

        private readonly Dictionary<int, SurfaceRow> _rows = new Dictionary<int, SurfaceRow>();

        private RoomModel _model;
        private Func<bool> _canSelect;
        private RectTransform _panelRoot;
        private GameObject _rowContainer;
        private Image _headerDot;
        private TMPro.TextMeshProUGUI _headerCount;
        private GameObject _createdEventSystem;

        /// <summary>The screen-space canvas this panel owns. Non-null from Awake onwards.</summary>
        public Canvas Canvas { get; private set; }

        public bool IsCollapsed => _rowContainer != null && !_rowContainer.activeSelf;

        private void Awake()
        {
            if (Canvas == null) BuildCanvas();
        }

        /// <summary>
        /// Builds one row per surface and binds to the model. Safe to call again with another model.
        /// <paramref name="canSelect"/> gates row taps: the caller owns the mode rules, so the panel does not
        /// have to know about <see cref="ModeManager"/> to honour AD-015. Left null, every row tap selects.
        /// </summary>
        public void Configure(RoomModel model, Func<bool> canSelect = null)
        {
            if (Canvas == null) BuildCanvas();

            if (_model != null) _model.SelectionChanged -= OnSelectionChanged;

            // Group dividers are children too and belong to the old model, so the container is cleared wholesale.
            foreach (Transform child in _rowContainer.transform) Destroy(child.gameObject);
            _rows.Clear();

            _model = model;
            _canSelect = canSelect;
            SetHeaderCount(_model == null ? 0 : _model.Surfaces.Count);
            if (_model == null) return;

            IReadOnlyList<SurfaceDefinition> surfaces = _model.Surfaces;
            for (int i = 0; i < surfaces.Count; i++)
            {
                SurfaceDefinition surface = surfaces[i];
                if (surface == null) continue;

                // The kit rules a hairline where the walls end and floor/ceiling begin.
                if (i > 0 && surfaces[i - 1] != null &&
                    surfaces[i - 1].kind == SurfaceKind.Wall && surface.kind != SurfaceKind.Wall)
                {
                    AddGroupDivider();
                }

                int surfaceId = surface.id;   // captured per row, not per loop variable
                _rows[surfaceId] = SurfaceRow.Create(_rowContainer.transform, RowNamePrefix + surface.name, i,
                    surface.name, rowHeight, () => SelectRow(surfaceId));
            }

            _model.SelectionChanged += OnSelectionChanged;
            SetRow(_model.SelectedSurfaceId, true);
        }

        /// <summary>Hides/shows the rows. The collapse control stays active either way (LIST AC4).</summary>
        public void ToggleCollapsed()
        {
            if (_rowContainer == null) return;

            bool collapsing = _rowContainer.activeSelf;
            _rowContainer.SetActive(!collapsing);
            if (_headerDot != null) _headerDot.color = collapsing ? HudTheme.IdleLabel : HudTheme.Accent;
        }

        /// <summary>
        /// LIST-01: the kit draws the rows as list items, so tapping one selects that surface. The panel is opaque
        /// HUD and stops the tap (EDGE-02), so without this a finger on the list reaches nothing at all.
        /// </summary>
        private void SelectRow(int surfaceId)
        {
            if (_model == null) return;
            if (_canSelect != null && !_canSelect()) return;

            _model.Select(surfaceId);
        }

        /// <summary>Row state as the row itself holds it — no label parsing (HUD-01 AC3).</summary>
        public bool IsRowSelected(int surfaceId)
        {
            return _rows.TryGetValue(surfaceId, out SurfaceRow row) && row != null && row.IsSelected;
        }

        /// <summary>The exact text shown for that surface, or null when there is no such row.</summary>
        public string GetRowLabel(int surfaceId)
        {
            return _rows.TryGetValue(surfaceId, out SurfaceRow row) && row != null ? row.Text : null;
        }

        /// <summary>Both ids arrive together, so one call repaints both rows and nothing else (LIST AC2).</summary>
        private void OnSelectionChanged(int? previous, int? current)
        {
            SetRow(previous, false);
            SetRow(current, true);
        }

        private void SetRow(int? surfaceId, bool selected)
        {
            if (surfaceId == null || _model == null) return;
            if (!_rows.TryGetValue(surfaceId.Value, out SurfaceRow row) || row == null) return;

            row.SetSelected(selected);
        }

        private void SetHeaderCount(int count)
        {
            if (_headerCount != null) _headerCount.text = count.ToString();
        }

        /// <summary>The kit's in-list rule: 1 px of rule_fade inside an 11 px band, decoration only.</summary>
        private void AddGroupDivider()
        {
            RectTransform band = HudTheme.NewUi("Divider", _rowContainer.transform);
            LayoutElement element = band.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 11f;
            element.preferredHeight = 11f;

            RectTransform rule = HudTheme.AddDivider(band).rectTransform;
            rule.anchorMin = new Vector2(0f, 0.5f);
            rule.anchorMax = new Vector2(1f, 0.5f);
            rule.pivot = new Vector2(0.5f, 0.5f);
            rule.offsetMin = new Vector2(10f, -0.5f);
            rule.offsetMax = new Vector2(-10f, 0.5f);
        }

        private void BuildCanvas()
        {
            RectTransform canvasRt = HudTheme.NewUi("FormifyCanvas", transform);
            Canvas = canvasRt.gameObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            HudTheme.ApplyColorSpace(Canvas);

            CanvasScaler scaler = canvasRt.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = HudTheme.ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            canvasRt.gameObject.AddComponent<GraphicRaycaster>();

            EnsureEventSystem();

            _panelRoot = HudTheme.NewUi("SurfacesPanel", canvasRt);
            _panelRoot.anchorMin = new Vector2(0f, 1f);
            _panelRoot.anchorMax = new Vector2(0f, 1f);
            _panelRoot.pivot = new Vector2(0f, 1f);
            _panelRoot.anchoredPosition = new Vector2(panelMargin, -panelMargin);
            _panelRoot.sizeDelta = new Vector2(panelWidth, panelHeight);
            HudTheme.AddPanelBackground(_panelRoot, HudTheme.PanelFill, HudTheme.PanelBorder);

            BuildHeader();

            RectTransform rows = HudTheme.NewUi("Rows", _panelRoot);
            rows.anchorMin = new Vector2(0f, 0f);
            rows.anchorMax = new Vector2(1f, 1f);
            rows.offsetMin = new Vector2(6f, 6f);
            rows.offsetMax = new Vector2(-6f, -headerHeight);
            _rowContainer = rows.gameObject;

            var layout = _rowContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 2f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        /// <summary>The kit's header — dot plus "SURFACES" — and the collapse control in one (LIST-02 AC3).</summary>
        private void BuildHeader()
        {
            RectTransform header = HudTheme.NewUi("Header", _panelRoot);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(0f, -headerHeight);
            header.offsetMax = new Vector2(0f, 0f);

            Image hit = HudTheme.AddImage(header, "HeaderFill", "row_fill_9s", HudTheme.NeutralFill,
                Image.Type.Sliced, raycastTarget: true);

            var button = header.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.onClick.AddListener(ToggleCollapsed);

            RectTransform dot = HudTheme.NewUi("Dot", header);
            dot.anchorMin = new Vector2(0f, 1f);
            dot.anchorMax = new Vector2(0f, 1f);
            dot.pivot = new Vector2(0f, 1f);
            dot.sizeDelta = new Vector2(6f, 6f);
            dot.anchoredPosition = new Vector2(12f, -16f);
            _headerDot = dot.gameObject.AddComponent<Image>();
            _headerDot.color = HudTheme.Accent;
            _headerDot.raycastTarget = false;

            RectTransform labelRect = HudTheme.NewUi("Label", header);
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(26f, 0f);
            labelRect.offsetMax = new Vector2(-34f, 0f);
            var label = labelRect.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            label.text = "SURFACES";
            label.fontSize = 11f;
            label.characterSpacing = HudTheme.Tracking(140f);
            label.color = HudTheme.IdleLabel;
            label.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;

            RectTransform countRect = HudTheme.NewUi("Count", header);
            countRect.anchorMin = new Vector2(1f, 0f);
            countRect.anchorMax = new Vector2(1f, 1f);
            countRect.pivot = new Vector2(1f, 0.5f);
            countRect.offsetMin = new Vector2(-34f, 0f);
            countRect.offsetMax = new Vector2(-12f, 0f);
            _headerCount = countRect.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            _headerCount.fontSize = 11f;
            _headerCount.characterSpacing = HudTheme.Tracking(140f);
            _headerCount.color = HudTheme.Caption;
            _headerCount.alignment = TMPro.TextAlignmentOptions.MidlineRight;
            _headerCount.raycastTarget = false;

            // Hairline under the header: 1 px tall, inset 10 px each side, decoration only.
            RectTransform divider = HudTheme.AddDivider(header).rectTransform;
            divider.anchorMin = new Vector2(0f, 0f);
            divider.anchorMax = new Vector2(1f, 0f);
            divider.pivot = new Vector2(0.5f, 0f);
            divider.offsetMin = new Vector2(10f, 0f);
            divider.offsetMax = new Vector2(-10f, 1f);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            if (FindFirstObjectByType<EventSystem>() != null) return;

            _createdEventSystem = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            _createdEventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            _createdEventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private void OnDestroy()
        {
            if (_model != null) _model.SelectionChanged -= OnSelectionChanged;
            _model = null;

            if (_createdEventSystem != null) Destroy(_createdEventSystem);
            _createdEventSystem = null;
        }
    }
}
