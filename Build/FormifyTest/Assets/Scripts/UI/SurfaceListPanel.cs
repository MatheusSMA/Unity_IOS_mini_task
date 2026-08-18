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
    /// A window is a row too (LIST-03): indented under the wall it was cut into, live on
    /// <see cref="RoomModel.WindowAdded"/> / <see cref="RoomModel.WindowRemoved"/>, selectable like any row, and
    /// collapsible per wall through the disclosure dot the wall row carries.
    /// Owns the application Canvas — the HUD is baked from it into the scene (AD-025) and later UI attaches to
    /// <see cref="Canvas"/> — and paints itself with the art kit (HUD-01) through <see cref="HudTheme"/>. The
    /// canvas is landscape: the scaler matches the kit's reference resolution (AD-020).
    /// </summary>
    public class SurfaceListPanel : MonoBehaviour
    {
        private const string RowNamePrefix = "Row_";
        private const string WindowRowNamePrefix = "WindowRow_";

        /// <summary>The VerticalLayoutGroup's own spacing, needed to measure the rows before they lay out.</summary>
        private const float RowSpacing = 2f;

        /// <summary>
        /// The kit puts the Readout at y = -404 and the panel starts at -8, so this is where the list stops
        /// growing before it would sit on top of it.
        /// ponytail: past this the rows spill; a ScrollRect is the upgrade if a room ever has that many windows.
        /// </summary>
        private const float MaxPanelHeight = 388f;

        [SerializeField] private float panelWidth = 250f;
        [SerializeField] private float panelHeight = 312f;
        [SerializeField] private float rowHeight = 40f;
        // A window row is visibly a child: shorter than a wall row and indented well past the index column, so
        // the two depths never read as one flat list.
        [SerializeField] private float windowRowHeight = 26f;
        [SerializeField] private float windowRowIndent = 34f;
        [SerializeField] private float headerHeight = 38f;
        [SerializeField] private float panelMargin = 8f;

        // Serialized: the HUD baked into the scene (AD-025) brings its own canvas and panel, and play binds to
        // what the scene holds instead of building a second one.
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private GameObject rowContainer;
        [SerializeField] private Button headerButton;
        [SerializeField] private Image headerDot;
        [SerializeField] private TMPro.TextMeshProUGUI headerCount;

        private readonly Dictionary<int, SurfaceRow> _rows = new Dictionary<int, SurfaceRow>();
        private readonly Dictionary<int, SurfaceRow> _windowRows = new Dictionary<int, SurfaceRow>();
        private readonly HashSet<int> _collapsedWalls = new HashSet<int>();

        private RoomModel _model;
        private Func<bool> _canSelect;
        private GameObject _createdEventSystem;

        /// <summary>The screen-space canvas this panel paints on. Non-null once EnsureCanvas has run.</summary>
        public Canvas Canvas => canvas;

        public bool IsCollapsed => rowContainer != null && !rowContainer.activeSelf;

        private void Awake() => EnsureCanvas();

        /// <summary>
        /// The canvas the panel paints on: the one baked into the scene (AD-025) when there is one, a fresh one
        /// built here when there is not. It also re-wires the header's collapse click — a scene keeps the Button
        /// but never the delegate, so the baked header would otherwise be dead.
        /// </summary>
        public Canvas EnsureCanvas()
        {
            if (canvas == null) BuildCanvas();

            if (headerButton != null)
            {
                headerButton.onClick.RemoveListener(ToggleCollapsed);
                headerButton.onClick.AddListener(ToggleCollapsed);
            }

            return canvas;
        }

        /// <summary>
        /// Builds one row per surface, one indented row per window, and binds to the model. Safe to call again
        /// with another model. <paramref name="canSelect"/> gates row taps: the caller owns the mode rules, so
        /// the panel does not have to know about <see cref="ModeManager"/> to honour AD-015. Left null, every
        /// row tap selects.
        /// </summary>
        public void Configure(RoomModel model, Func<bool> canSelect = null)
        {
            EnsureCanvas();

            Unsubscribe();

            // Group dividers are children too and belong to the old model, so the container is cleared wholesale.
            // Destroy only lands at the end of the frame, so the old children are unparented first: left in the
            // layout they would stack under the new rows for a frame — visible on a baked scene's first frame.
            var stale = new List<Transform>();
            foreach (Transform child in rowContainer.transform) stale.Add(child);
            for (int i = 0; i < stale.Count; i++)
            {
                stale[i].SetParent(null, false);
                Destroy(stale[i].gameObject);
            }

            _rows.Clear();
            _windowRows.Clear();
            _collapsedWalls.Clear();

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
                bool isWall = surface.kind == SurfaceKind.Wall;

                _rows[surfaceId] = SurfaceRow.Create(rowContainer.transform, RowNamePrefix + surface.name, i + 1,
                    surface.name, rowHeight, () => SelectRow(surfaceId), 0f, isWall);

                if (!isWall) continue;

                // Re-configuring an already-populated model has to redraw its windows too.
                IReadOnlyList<WindowSpec> windows = _model.GetWindows(surfaceId);
                for (int w = 0; w < windows.Count; w++) AddWindowRow(windows[w]);
                RefreshWallDisclosure(surfaceId);
            }

            _model.SelectionChanged += OnSelectionChanged;
            _model.WindowSelectionChanged += OnWindowSelectionChanged;
            _model.WindowAdded += OnWindowAdded;
            _model.WindowRemoved += OnWindowRemoved;

            SetRow(_model.SelectedSurfaceId, true);
            SetWindowRow(_model.SelectedWindowId, true);
            ResizePanel();
        }

        /// <summary>
        /// The kit's panel is a fixed 250 x 312, which the six surfaces fill exactly — the first window would
        /// spill its row out of the bottom. The panel grows with what it holds instead, and folding a wall wins
        /// the space back.
        /// </summary>
        private void ResizePanel()
        {
            if (panelRoot == null || rowContainer == null) return;

            float content = 0f;
            foreach (Transform child in rowContainer.transform)
            {
                if (!child.gameObject.activeSelf) continue;

                LayoutElement element = child.GetComponent<LayoutElement>();
                if (element != null) content += element.preferredHeight + RowSpacing;
            }

            float wanted = headerHeight + content + 6f;
            panelRoot.sizeDelta = new Vector2(panelWidth, Mathf.Clamp(wanted, panelHeight, MaxPanelHeight));
        }

        /// <summary>Hides/shows the rows. The collapse control stays active either way (LIST AC4).</summary>
        public void ToggleCollapsed()
        {
            if (rowContainer == null) return;

            bool collapsing = rowContainer.activeSelf;
            rowContainer.SetActive(!collapsing);
            if (headerDot != null) headerDot.color = collapsing ? HudTheme.IdleLabel : HudTheme.Accent;
        }

        /// <summary>
        /// LIST-03: folds a wall's windows away without touching the wall row or any binding — the same rule
        /// EDGE-06 sets for the panel-wide collapse, one level down.
        /// </summary>
        public void ToggleWall(int surfaceId)
        {
            // A wall with nothing under it has nothing to fold, and folding it silently would leave the state
            // waiting to bite the first window that lands there.
            if (_model == null || _model.GetWindows(surfaceId).Count == 0) return;

            if (!_collapsedWalls.Remove(surfaceId)) _collapsedWalls.Add(surfaceId);

            ApplyWallCollapse(surfaceId);
        }

        /// <summary>Whether that wall is showing its windows. A wall with no windows is expanded and bare.</summary>
        public bool IsWallExpanded(int surfaceId) => !_collapsedWalls.Contains(surfaceId);

        /// <summary>
        /// LIST-01: the kit draws the rows as list items, so tapping one selects that surface. The panel is opaque
        /// HUD and stops the tap (EDGE-02), so without this a finger on the list reaches nothing at all.
        /// LIST-03 AC6: on a wall the same tap folds its windows. The row is the control — a 6 px dot beside the
        /// label would be both a poor touch target and a dead spot inside the row's own hit area.
        /// </summary>
        private void SelectRow(int surfaceId)
        {
            if (_model == null) return;
            if (_canSelect != null && !_canSelect()) return;

            _model.Select(surfaceId);
            ToggleWall(surfaceId);
        }

        /// <summary>Same rule for a window row, and the model makes the two selections mutually exclusive.</summary>
        private void SelectWindowRow(int windowId)
        {
            if (_model == null) return;
            if (_canSelect != null && !_canSelect()) return;

            _model.SelectWindow(windowId);
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

        public bool IsWindowRowSelected(int windowId)
        {
            return _windowRows.TryGetValue(windowId, out SurfaceRow row) && row != null && row.IsSelected;
        }

        public string GetWindowRowLabel(int windowId)
        {
            return _windowRows.TryGetValue(windowId, out SurfaceRow row) && row != null ? row.Text : null;
        }

        /// <summary>Whether that window's row is on screen — false while its wall is folded.</summary>
        public bool IsWindowRowShown(int windowId)
        {
            return _windowRows.TryGetValue(windowId, out SurfaceRow row) && row != null &&
                   row.gameObject.activeSelf;
        }

        /// <summary>Both ids arrive together, so one call repaints both rows and nothing else (LIST AC2).</summary>
        private void OnSelectionChanged(int? previous, int? current)
        {
            SetRow(previous, false);
            SetRow(current, true);
        }

        private void OnWindowSelectionChanged(int? previous, int? current)
        {
            SetWindowRow(previous, false);
            SetWindowRow(current, true);
        }

        private void OnWindowAdded(WindowSpec spec)
        {
            if (spec == null) return;

            AddWindowRow(spec);
            RenumberWindows(spec.surfaceId);
            RefreshWallDisclosure(spec.surfaceId);
            ResizePanel();
        }

        private void OnWindowRemoved(WindowSpec spec)
        {
            if (spec == null) return;

            if (_windowRows.TryGetValue(spec.id, out SurfaceRow row) && row != null)
            {
                // Unparented first for the same reason Configure does it: Destroy lands a frame later, and the
                // rows below would keep its gap and its sibling index until then.
                row.transform.SetParent(null, false);
                Destroy(row.gameObject);
            }

            _windowRows.Remove(spec.id);
            RenumberWindows(spec.surfaceId);
            RefreshWallDisclosure(spec.surfaceId);
            ResizePanel();
        }

        /// <summary>
        /// One indented row directly under its wall, in the model's own window order. No index column: the
        /// indent says whose it is and the label says which one.
        /// </summary>
        private void AddWindowRow(WindowSpec spec)
        {
            if (spec == null || _model == null) return;
            if (!_rows.TryGetValue(spec.surfaceId, out SurfaceRow wallRow) || wallRow == null) return;

            int windowId = spec.id;
            int ordinal = OrdinalOf(spec);

            SurfaceRow row = SurfaceRow.Create(rowContainer.transform, WindowRowNamePrefix + windowId, 0,
                WindowLabel(ordinal), windowRowHeight, () => SelectWindowRow(windowId), windowRowIndent);

            row.transform.SetSiblingIndex(wallRow.transform.GetSiblingIndex() + 1 + ordinal);
            row.gameObject.SetActive(IsWallExpanded(spec.surfaceId));
            _windowRows[windowId] = row;
        }

        /// <summary>The labels count per wall, so removing the first window renames the ones that stay.</summary>
        private void RenumberWindows(int surfaceId)
        {
            if (_model == null) return;

            IReadOnlyList<WindowSpec> windows = _model.GetWindows(surfaceId);
            for (int i = 0; i < windows.Count; i++)
            {
                if (windows[i] == null) continue;
                if (_windowRows.TryGetValue(windows[i].id, out SurfaceRow row) && row != null)
                    row.SetLabel(WindowLabel(i));
            }
        }

        /// <summary>The disclosure dot only means something on a wall that has windows to fold.</summary>
        private void RefreshWallDisclosure(int surfaceId)
        {
            if (_model == null) return;
            if (!_rows.TryGetValue(surfaceId, out SurfaceRow wallRow) || wallRow == null) return;

            wallRow.SetDiscloseVisible(_model.GetWindows(surfaceId).Count > 0);
            wallRow.SetExpanded(IsWallExpanded(surfaceId));
        }

        private void ApplyWallCollapse(int surfaceId)
        {
            if (_model == null) return;

            bool expanded = IsWallExpanded(surfaceId);
            IReadOnlyList<WindowSpec> windows = _model.GetWindows(surfaceId);

            for (int i = 0; i < windows.Count; i++)
            {
                if (windows[i] == null) continue;
                if (_windowRows.TryGetValue(windows[i].id, out SurfaceRow row) && row != null)
                    row.gameObject.SetActive(expanded);
            }

            if (_rows.TryGetValue(surfaceId, out SurfaceRow wallRow) && wallRow != null)
                wallRow.SetExpanded(expanded);

            ResizePanel();
        }

        private int OrdinalOf(WindowSpec spec)
        {
            IReadOnlyList<WindowSpec> windows = _model.GetWindows(spec.surfaceId);
            for (int i = 0; i < windows.Count; i++)
            {
                if (windows[i] != null && windows[i].id == spec.id) return i;
            }
            return windows.Count;
        }

        private static string WindowLabel(int ordinal) => "Window " + (ordinal + 1);

        private void SetRow(int? surfaceId, bool selected)
        {
            if (surfaceId == null || _model == null) return;
            if (!_rows.TryGetValue(surfaceId.Value, out SurfaceRow row) || row == null) return;

            row.SetSelected(selected);
        }

        private void SetWindowRow(int? windowId, bool selected)
        {
            if (windowId == null || _model == null) return;
            if (!_windowRows.TryGetValue(windowId.Value, out SurfaceRow row) || row == null) return;

            row.SetSelected(selected);
        }

        private void SetHeaderCount(int count)
        {
            if (headerCount != null) headerCount.text = count.ToString();
        }

        /// <summary>The kit's in-list rule: 1 px of rule_fade inside an 11 px band, decoration only.</summary>
        private void AddGroupDivider()
        {
            RectTransform band = HudTheme.NewUi("Divider", rowContainer.transform);
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
            canvas = canvasRt.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            HudTheme.ApplyColorSpace(canvas);

            CanvasScaler scaler = canvasRt.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = HudTheme.ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;

            canvasRt.gameObject.AddComponent<GraphicRaycaster>();

            EnsureEventSystem();

            panelRoot = HudTheme.NewUi("SurfacesPanel", canvasRt);
            panelRoot.anchorMin = new Vector2(0f, 1f);
            panelRoot.anchorMax = new Vector2(0f, 1f);
            panelRoot.pivot = new Vector2(0f, 1f);
            panelRoot.anchoredPosition = new Vector2(panelMargin, -panelMargin);
            panelRoot.sizeDelta = new Vector2(panelWidth, panelHeight);
            HudTheme.AddPanelBackground(panelRoot, HudTheme.PanelFill, HudTheme.PanelBorder);

            BuildHeader();

            RectTransform rows = HudTheme.NewUi("Rows", panelRoot);
            rows.anchorMin = new Vector2(0f, 0f);
            rows.anchorMax = new Vector2(1f, 1f);
            rows.offsetMin = new Vector2(6f, 6f);
            rows.offsetMax = new Vector2(-6f, -headerHeight);
            rowContainer = rows.gameObject;

            var layout = rowContainer.AddComponent<VerticalLayoutGroup>();
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
            RectTransform header = HudTheme.NewUi("Header", panelRoot);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(0f, -headerHeight);
            header.offsetMax = new Vector2(0f, 0f);

            Image hit = HudTheme.AddImage(header, "HeaderFill", "row_fill_9s", HudTheme.NeutralFill,
                Image.Type.Sliced, raycastTarget: true);

            // The click itself is wired in EnsureCanvas, so the built and the baked header behave the same.
            headerButton = header.gameObject.AddComponent<Button>();
            headerButton.targetGraphic = hit;

            RectTransform dot = HudTheme.NewUi("Dot", header);
            dot.anchorMin = new Vector2(0f, 1f);
            dot.anchorMax = new Vector2(0f, 1f);
            dot.pivot = new Vector2(0f, 1f);
            dot.sizeDelta = new Vector2(6f, 6f);
            dot.anchoredPosition = new Vector2(12f, -16f);
            headerDot = dot.gameObject.AddComponent<Image>();
            headerDot.color = HudTheme.Accent;
            headerDot.raycastTarget = false;

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
            headerCount = countRect.gameObject.AddComponent<TMPro.TextMeshProUGUI>();
            headerCount.fontSize = 11f;
            headerCount.characterSpacing = HudTheme.Tracking(140f);
            headerCount.color = HudTheme.Caption;
            headerCount.alignment = TMPro.TextAlignmentOptions.MidlineRight;
            headerCount.raycastTarget = false;

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

        private void Unsubscribe()
        {
            if (_model == null) return;

            _model.SelectionChanged -= OnSelectionChanged;
            _model.WindowSelectionChanged -= OnWindowSelectionChanged;
            _model.WindowAdded -= OnWindowAdded;
            _model.WindowRemoved -= OnWindowRemoved;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _model = null;

            if (_createdEventSystem != null) Destroy(_createdEventSystem);
            _createdEventSystem = null;
        }
    }
}
