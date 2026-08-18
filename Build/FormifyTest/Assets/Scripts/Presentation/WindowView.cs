using System;
using System.Collections.Generic;
using Formify.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Formify.Presentation
{
    /// <summary>
    /// One window opening (WIN-04). The BoxCollider is the only thing living inside the hole, so an empty
    /// opening stays tappable (AC1) — SelectionController finds this component on the hit and routes the tap
    /// here, which is why no dedicated layer is needed.
    /// B2: that tap now SELECTS this window in the model, superseding AD-014 / SEL-03 AC7 which had it leave
    /// the selection alone. Everything the view shows hangs off the selection, not off the tap: the accent
    /// border over the opening and the kit's trash button at the top-right (AC2) appear when this window
    /// becomes the selection and go with it when it moves on. From there the flow is unchanged — confirmation
    /// popup (AC3) -> confirm removes the window (AC4) / cancel changes nothing (AC5).
    /// </summary>
    public class WindowView : MonoBehaviour, IWindowTapTarget
    {
        private const float IconSize = 16f;

        [SerializeField] private float deleteButtonSize = 46f;
        [SerializeField] private float fontSize = 13f;
        [SerializeField] private Vector2 popupSize = new Vector2(320f, 150f);
        [SerializeField] private string confirmationMessage = "Delete this window?";

        private SurfaceDefinition _surface;
        private RoomModel _model;
        private Canvas _canvas;
        private Camera _camera;
        private RectTransform _deleteRoot;
        private RectTransform _highlight;
        private GameObject _popupRoot;
        private bool _selected;

        public WindowSpec Spec { get; private set; }

        /// <summary>The "X" itself, so tests and owners can raise its click without a pointer event.</summary>
        public Button DeleteButton { get; private set; }

        public bool IsDeleteButtonShown => _deleteRoot != null && _deleteRoot.gameObject.activeSelf;

        /// <summary>B2 — the accent border laid over the opening while this window is the selection.</summary>
        public bool IsSelectionBorderShown => _highlight != null && _highlight.gameObject.activeSelf;

        public bool IsConfirmationShown => _popupRoot != null && _popupRoot.activeSelf;

        /// <summary>
        /// Places the opening in world space with the surface basis and gives it its collider. The slab is
        /// extruded backwards from the front face, so the collider centre sits half a thickness behind it.
        /// </summary>
        public void Initialize(WindowSpec spec, SurfaceDefinition surface, RoomModel model, Canvas uiCanvas)
        {
            Spec = spec;
            _surface = surface;
            _model = model;
            _canvas = uiCanvas;
            _camera = Camera.main;

            // B2: the affordances follow the SELECTION, so the model event is what raises and drops them.
            // Removed first — Initialize may be run again on the same view.
            if (_model != null)
            {
                _model.WindowSelectionChanged -= OnWindowSelectionChanged;
                _model.WindowSelectionChanged += OnWindowSelectionChanged;
            }

            if (spec == null || surface == null) return;

            Rect2D rect = spec.rect;
            transform.position = surface.LocalToWorld(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f)
                                 - surface.Normal * (surface.thickness * 0.5f);
            // Same basis as SurfaceView: local +Z -> Normal, +Y -> up, +X -> right (the triple is cyclic).
            transform.rotation = Quaternion.LookRotation(surface.Normal, surface.up);

            // The collider is the whole point: without it the hole swallows every tap (AC1).
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null) box = gameObject.AddComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(rect.width, rect.height, surface.thickness);

            if (_canvas != null && _deleteRoot == null) BuildUi();
        }

        /// <summary>
        /// Routed here by SelectionController. B2: the tap makes this window the selection, which clears a
        /// selected surface on the way (AD-026); the border and the X follow from the event, not from here.
        /// Supersedes AD-014 / SEL-03 AC7 ("a window tap never touches the selection").
        /// </summary>
        public void OnTapped()
        {
            if (_model != null && Spec != null) _model.SelectWindow(Spec.id);
        }

        private void OnWindowSelectionChanged(int? previous, int? current)
        {
            _selected = Spec != null && current == Spec.id;

            if (!_selected)
            {
                // The affordance belongs to the selected window, so it leaves with the selection — popup too.
                CloseUi();
                return;
            }

            if (_deleteRoot != null) _deleteRoot.gameObject.SetActive(true);
            Track();   // Track raises the border once it knows the opening is in front of the camera
        }

        /// <summary>
        /// AC4: the model removal is the whole job — SurfaceView rebuilds mesh + collider off the event.
        /// TryRemoveWindow clears the window selection before it announces the removal, so the deselect lands
        /// on a view whose UI CloseUi has already taken down; both orders are idempotent.
        /// </summary>
        public void ConfirmDelete()
        {
            CloseUi();
            if (_model != null && Spec != null) _model.TryRemoveWindow(Spec.id);
        }

        /// <summary>AC5: the popup goes away, the window does not.</summary>
        public void CancelDelete()
        {
            if (_popupRoot != null) _popupRoot.SetActive(false);
        }

        /// <summary>The X and the border are screen-space widgets over a world-space opening: re-project both
        /// every frame for as long as this window is the selection.</summary>
        private void LateUpdate()
        {
            if (_selected) Track();
        }

        private void Track()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null || _surface == null || Spec == null) return;

            Rect2D rect = Spec.rect;
            Vector3 topRight = _camera.WorldToScreenPoint(_surface.LocalToWorld(rect.XMax, rect.YMax));
            // An overlay canvas lives in screen pixels; the projected depth would only push the widget off it.
            if (_deleteRoot != null) _deleteRoot.position = new Vector3(topRight.x, topRight.y, 0f);

            if (_highlight == null) return;

            Vector3 bottomLeft = _camera.WorldToScreenPoint(_surface.LocalToWorld(rect.x, rect.y));
            Vector3 bottomRight = _camera.WorldToScreenPoint(_surface.LocalToWorld(rect.XMax, rect.y));
            Vector3 topLeft = _camera.WorldToScreenPoint(_surface.LocalToWorld(rect.x, rect.YMax));

            // Behind the camera WorldToScreenPoint mirrors the point through the viewport centre, so the border
            // would smear across the screen once the opening is behind you. Hide it rather than draw that.
            bool inFront = topRight.z > 0f && bottomLeft.z > 0f && bottomRight.z > 0f && topLeft.z > 0f;
            _highlight.gameObject.SetActive(inFront);
            if (!inFront) return;

            float minX = Mathf.Min(Mathf.Min(bottomLeft.x, bottomRight.x), Mathf.Min(topLeft.x, topRight.x));
            float maxX = Mathf.Max(Mathf.Max(bottomLeft.x, bottomRight.x), Mathf.Max(topLeft.x, topRight.x));
            float minY = Mathf.Min(Mathf.Min(bottomLeft.y, bottomRight.y), Mathf.Min(topLeft.y, topRight.y));
            float maxY = Mathf.Max(Mathf.Max(bottomLeft.y, bottomRight.y), Mathf.Max(topLeft.y, topRight.y));

            // position is screen pixels on an overlay canvas, but sizeDelta is canvas-local, hence the divide.
            float scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            _highlight.position = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
            _highlight.sizeDelta = new Vector2(maxX - minX, maxY - minY) / scale;
        }

        private void CloseUi()
        {
            // Drops the tracking too, so LateUpdate cannot put the border back up after a delete.
            _selected = false;
            if (_deleteRoot != null) _deleteRoot.gameObject.SetActive(false);
            if (_highlight != null) _highlight.gameObject.SetActive(false);
            if (_popupRoot != null) _popupRoot.SetActive(false);
        }

        /// <summary>The kit's `BtnDelete`: 46 x 46, destructive palette, icon_trash — the only red in the HUD.</summary>
        private void BuildUi()
        {
            // B2 feedback in the room view: the kit's own window border in the selected accent, laid over the
            // opening's projected rect. OUT-01's outline is a layer trick on a surface MESH and an opening has
            // no renderer to put on a layer, so reusing it would be a rewrite. Built first so it draws under
            // the X, and never a raycast target (HUD-01 AC4) — it must not eat taps meant for the room.
            _highlight = HudTheme.NewUi("WindowSelectionBorder", _canvas.transform);
            HudTheme.AddImage(_highlight, "Border", "window_border_9s", HudTheme.Accent);
            _highlight.gameObject.SetActive(false);

            _deleteRoot = HudTheme.NewUi("WindowDeleteButton", _canvas.transform);
            _deleteRoot.sizeDelta = new Vector2(deleteButtonSize, deleteButtonSize);

            Image background = HudTheme.AddImage(_deleteRoot, "Fill", "panel_fill_9s", HudTheme.DeleteFill,
                Image.Type.Sliced, raycastTarget: true);
            HudTheme.AddImage(_deleteRoot, "Border", "panel_border_9s", HudTheme.DeleteBorder);

            float inset = (deleteButtonSize - IconSize) * 0.5f;
            RectTransform iconRect = HudTheme.Stretch(HudTheme.NewUi("Icon", _deleteRoot));
            iconRect.offsetMin = new Vector2(inset, inset);
            iconRect.offsetMax = new Vector2(-inset, -inset);
            var icon = iconRect.gameObject.AddComponent<Image>();
            icon.sprite = HudTheme.Sprite("icon_trash");
            icon.color = HudTheme.DeleteIcon;
            icon.raycastTarget = false;

            DeleteButton = _deleteRoot.gameObject.AddComponent<Button>();
            DeleteButton.targetGraphic = background;
            // The palette IS the state readout; uGUI's tint would multiply on top of it.
            DeleteButton.transition = Selectable.Transition.None;
            DeleteButton.onClick.AddListener(ShowConfirmation);

            _deleteRoot.gameObject.SetActive(false);

            BuildPopup();
        }

        private void BuildPopup()
        {
            RectTransform popup = HudTheme.NewUi("WindowDeletePopup", _canvas.transform);
            popup.anchorMin = new Vector2(0.5f, 0.5f);
            popup.anchorMax = new Vector2(0.5f, 0.5f);
            popup.anchoredPosition = Vector2.zero;
            popup.sizeDelta = popupSize;

            // The fill is a raycast target so nothing behind the popup can be reached while it is open.
            HudTheme.AddImage(popup, "Fill", "panel_fill_9s", HudTheme.PanelFill, Image.Type.Sliced,
                raycastTarget: true);
            HudTheme.AddImage(popup, "Border", "panel_border_9s", HudTheme.DeleteBorder);

            RectTransform message = HudTheme.NewUi("Message", popup);
            message.anchorMin = new Vector2(0f, 0.42f);
            message.anchorMax = Vector2.one;
            message.offsetMin = new Vector2(18f, 0f);
            message.offsetMax = new Vector2(-18f, -12f);
            TextMeshProUGUI messageLabel = message.gameObject.AddComponent<TextMeshProUGUI>();
            messageLabel.text = confirmationMessage;
            messageLabel.fontSize = fontSize;
            messageLabel.color = HudTheme.ActiveText;
            messageLabel.alignment = TextAlignmentOptions.Center;
            messageLabel.raycastTarget = false;

            AddPopupButton(popup, "ConfirmButton", "Delete", HudTheme.DeleteFill, HudTheme.DeleteBorder,
                HudTheme.DeleteIcon, new Vector2(0.06f, 0.12f), new Vector2(0.48f, 0.36f), ConfirmDelete);
            AddPopupButton(popup, "CancelButton", "Cancel", HudTheme.NeutralFill, HudTheme.NeutralBorder,
                HudTheme.ActiveText, new Vector2(0.52f, 0.12f), new Vector2(0.94f, 0.36f), CancelDelete);

            _popupRoot = popup.gameObject;
            _popupRoot.SetActive(false);
        }

        private void AddPopupButton(RectTransform parent, string objectName, string text, Color fill, Color border,
            Color labelColor, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = HudTheme.NewUi(objectName, parent);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image background = HudTheme.AddImage(rect, "Fill", "panel_fill_9s", fill, Image.Type.Sliced,
                raycastTarget: true);
            HudTheme.AddImage(rect, "Border", "panel_border_9s", border);
            HudTheme.AddLabel(rect, "Label", text.ToUpperInvariant(), 12.5f, 100f, labelColor,
                TextAlignmentOptions.Center);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(onClick);
        }

        private void ShowConfirmation()
        {
            if (_popupRoot != null) _popupRoot.SetActive(true);
        }

        private void OnDestroy()
        {
            if (_model != null) _model.WindowSelectionChanged -= OnWindowSelectionChanged;

            DestroyUiObject(_highlight == null ? null : _highlight.gameObject);
            DestroyUiObject(_deleteRoot == null ? null : _deleteRoot.gameObject);
            DestroyUiObject(_popupRoot);
            _highlight = null;
            _deleteRoot = null;
            _popupRoot = null;
            DeleteButton = null;
        }

        /// <summary>The UI lives on the shared canvas, so it has to be cleaned up by hand in both modes.</summary>
        private static void DestroyUiObject(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }
    }

    /// <summary>
    /// One <see cref="WindowView"/> per window, for as long as the model says the window exists: created on
    /// WindowAdded, destroyed on WindowRemoved. Keeps the view lifetime a pure function of model events, so
    /// no caller has to remember to clean up after a deletion.
    /// </summary>
    public class WindowViewFactory : MonoBehaviour
    {
        private readonly List<WindowView> _views = new List<WindowView>();

        private RoomModel _model;
        private Func<int, SurfaceDefinition> _surfaceLookup;
        private Canvas _uiCanvas;

        public IReadOnlyList<WindowView> Views => _views;

        /// <summary>Wiring without prefabs, for RoomBootstrap and tests. Safe to call again with another model.</summary>
        public void Configure(RoomModel model, Func<int, SurfaceDefinition> surfaceLookup, Canvas uiCanvas)
        {
            Unsubscribe();

            _model = model;
            _surfaceLookup = surfaceLookup;
            _uiCanvas = uiCanvas;

            if (_model == null) return;
            _model.WindowAdded += OnWindowAdded;
            _model.WindowRemoved += OnWindowRemoved;
        }

        private void OnWindowAdded(WindowSpec spec)
        {
            if (spec == null || _surfaceLookup == null) return;

            SurfaceDefinition surface = _surfaceLookup(spec.surfaceId);
            if (surface == null) return;

            GameObject go = new GameObject("Window " + spec.id);
            go.transform.SetParent(transform, false);

            WindowView view = go.AddComponent<WindowView>();
            view.Initialize(spec, surface, _model, _uiCanvas);
            _views.Add(view);
        }

        private void OnWindowRemoved(WindowSpec spec)
        {
            if (spec == null) return;

            for (int i = 0; i < _views.Count; i++)
            {
                WindowView view = _views[i];
                if (view == null || view.Spec == null || view.Spec.id != spec.id) continue;

                _views.RemoveAt(i);
                Destroy(view.gameObject);
                return;   // window ids are unique, so there is never a second match
            }
        }

        private void Unsubscribe()
        {
            if (_model == null) return;
            _model.WindowAdded -= OnWindowAdded;
            _model.WindowRemoved -= OnWindowRemoved;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _model = null;
            _views.Clear();
        }
    }
}
