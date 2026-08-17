using System;
using System.Collections.Generic;
using Formify.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Formify.Presentation
{
    /// <summary>
    /// The state readout (LIST-01, LIST-02): one row per surface in <see cref="RoomModel.Surfaces"/> order,
    /// live-bound to <see cref="RoomModel.SelectionChanged"/> — exactly the two affected rows are rewritten per
    /// event (AD-007). Collapsing hides the row container, never the binding (EDGE-06); the collapse control
    /// itself stays visible (LIST AC4).
    /// Owns the application Canvas built entirely in code — later UI tasks attach to <see cref="Canvas"/>.
    /// </summary>
    public class SurfaceListPanel : MonoBehaviour
    {
        /// <summary>Suffix appended to a row label while that surface is the selected one.</summary>
        public const string SelectedMarker = "  [SELECTED]";

        private const string RowNamePrefix = "Row_";

        [SerializeField] private float panelWidth = 260f;
        [SerializeField] private float rowHeight = 32f;
        [SerializeField] private float fontSize = 20f;
        [SerializeField] private float panelMargin = 16f;

        private readonly Dictionary<int, TextMeshProUGUI> _rows = new Dictionary<int, TextMeshProUGUI>();

        private RoomModel _model;
        private RectTransform _panelRoot;
        private GameObject _rowContainer;
        private TextMeshProUGUI _collapseLabel;
        private GameObject _createdEventSystem;

        /// <summary>The screen-space canvas this panel owns. Non-null from Awake onwards.</summary>
        public Canvas Canvas { get; private set; }

        public bool IsCollapsed => _rowContainer != null && !_rowContainer.activeSelf;

        private void Awake()
        {
            if (Canvas == null) BuildCanvas();
        }

        /// <summary>Builds one row per surface and binds to the model. Safe to call again with another model.</summary>
        public void Configure(RoomModel model)
        {
            if (Canvas == null) BuildCanvas();

            if (_model != null) _model.SelectionChanged -= OnSelectionChanged;

            foreach (TextMeshProUGUI row in _rows.Values)
            {
                if (row != null) Destroy(row.gameObject);
            }
            _rows.Clear();

            _model = model;
            if (_model == null) return;

            IReadOnlyList<SurfaceDefinition> surfaces = _model.Surfaces;
            for (int i = 0; i < surfaces.Count; i++)
            {
                SurfaceDefinition surface = surfaces[i];
                if (surface == null) continue;
                _rows[surface.id] = CreateRow(surface);
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
            if (_collapseLabel != null) _collapseLabel.text = collapsing ? "+ Surfaces" : "- Surfaces";
        }

        /// <summary>Row state as rendered, not as modelled: reads the row's own label text.</summary>
        public bool IsRowSelected(int surfaceId)
        {
            return _rows.TryGetValue(surfaceId, out TextMeshProUGUI label)
                   && label != null
                   && label.text.EndsWith(SelectedMarker, StringComparison.Ordinal);
        }

        /// <summary>The exact text shown for that surface, or null when there is no such row.</summary>
        public string GetRowLabel(int surfaceId)
        {
            return _rows.TryGetValue(surfaceId, out TextMeshProUGUI label) && label != null ? label.text : null;
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
            if (!_rows.TryGetValue(surfaceId.Value, out TextMeshProUGUI label) || label == null) return;

            SurfaceDefinition surface = _model.GetSurface(surfaceId.Value);
            if (surface == null) return;

            label.text = LabelFor(surface, selected);
        }

        private static string LabelFor(SurfaceDefinition surface, bool selected)
        {
            return selected ? surface.name + SelectedMarker : surface.name;
        }

        private TextMeshProUGUI CreateRow(SurfaceDefinition surface)
        {
            RectTransform row = NewUiObject(RowNamePrefix + surface.name, _rowContainer.transform);

            LayoutElement element = row.gameObject.AddComponent<LayoutElement>();
            element.minHeight = rowHeight;
            element.preferredHeight = rowHeight;

            TextMeshProUGUI label = row.gameObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.raycastTarget = false;
            label.text = LabelFor(surface, false);
            return label;
        }

        private void BuildCanvas()
        {
            RectTransform canvasRt = NewUiObject("FormifyCanvas", transform);
            Canvas = canvasRt.gameObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasRt.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1170f, 2532f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRt.gameObject.AddComponent<GraphicRaycaster>();

            EnsureEventSystem();

            _panelRoot = NewUiObject("SurfaceList", canvasRt);
            _panelRoot.anchorMin = new Vector2(0f, 1f);
            _panelRoot.anchorMax = new Vector2(0f, 1f);
            _panelRoot.pivot = new Vector2(0f, 1f);
            _panelRoot.anchoredPosition = new Vector2(panelMargin, -panelMargin);
            _panelRoot.sizeDelta = new Vector2(panelWidth, rowHeight * 8f);
            ApplyVerticalLayout(_panelRoot.gameObject, 4f);

            BuildCollapseControl();

            RectTransform rows = NewUiObject("Rows", _panelRoot);
            _rowContainer = rows.gameObject;
            ApplyVerticalLayout(_rowContainer, 2f);
        }

        private void BuildCollapseControl()
        {
            RectTransform control = NewUiObject("CollapseControl", _panelRoot);
            control.sizeDelta = new Vector2(panelWidth, rowHeight);

            LayoutElement element = control.gameObject.AddComponent<LayoutElement>();
            element.minHeight = rowHeight;
            element.preferredHeight = rowHeight;

            Image background = control.gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.6f);

            Button button = control.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(ToggleCollapsed);

            RectTransform labelRt = NewUiObject("Label", control);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            _collapseLabel = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
            _collapseLabel.fontSize = fontSize;
            _collapseLabel.color = Color.white;
            _collapseLabel.alignment = TextAlignmentOptions.MidlineLeft;
            _collapseLabel.raycastTarget = false;
            _collapseLabel.text = "- Surfaces";
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

        private static void ApplyVerticalLayout(GameObject target, float spacing)
        {
            VerticalLayoutGroup layout = target.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }

        private static RectTransform NewUiObject(string objectName, Transform parent)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
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
