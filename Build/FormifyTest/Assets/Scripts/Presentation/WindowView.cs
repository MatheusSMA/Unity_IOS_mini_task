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
    /// here without touching the selection (AD-014, SEL-03 AC7), which is why no dedicated layer is needed.
    /// Owns the delete affordance end to end: X at the top-right (AC2) -> confirmation popup (AC3) ->
    /// confirm removes the window (AC4) / cancel changes nothing (AC5).
    /// </summary>
    public class WindowView : MonoBehaviour, IWindowTapTarget
    {
        [SerializeField] private float deleteButtonSize = 56f;
        [SerializeField] private float fontSize = 22f;
        [SerializeField] private Vector2 popupSize = new Vector2(460f, 220f);
        [SerializeField] private string confirmationMessage = "Delete this window?";

        private SurfaceDefinition _surface;
        private RoomModel _model;
        private Canvas _canvas;
        private Camera _camera;
        private RectTransform _deleteRoot;
        private GameObject _popupRoot;

        public WindowSpec Spec { get; private set; }

        /// <summary>The "X" itself, so tests and owners can raise its click without a pointer event.</summary>
        public Button DeleteButton { get; private set; }

        public bool IsDeleteButtonShown => _deleteRoot != null && _deleteRoot.gameObject.activeSelf;

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

        /// <summary>Routed here by SelectionController. Shows the X only — the selection is none of our business.</summary>
        public void OnTapped()
        {
            if (_deleteRoot == null) return;

            _deleteRoot.gameObject.SetActive(true);
            TrackCorner();
        }

        /// <summary>AC4: the model removal is the whole job — SurfaceView rebuilds mesh + collider off the event.</summary>
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

        /// <summary>The X is a screen-space widget over a world-space corner, so it re-projects every frame.</summary>
        private void LateUpdate()
        {
            if (IsDeleteButtonShown) TrackCorner();
        }

        private void TrackCorner()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null || _surface == null || Spec == null) return;

            Vector3 screen = _camera.WorldToScreenPoint(_surface.LocalToWorld(Spec.rect.XMax, Spec.rect.YMax));
            // An overlay canvas lives in screen pixels; the projected depth would only push the widget off it.
            screen.z = 0f;
            _deleteRoot.position = screen;
        }

        private void CloseUi()
        {
            if (_deleteRoot != null) _deleteRoot.gameObject.SetActive(false);
            if (_popupRoot != null) _popupRoot.SetActive(false);
        }

        private void BuildUi()
        {
            _deleteRoot = NewUiObject("WindowDeleteButton", _canvas.transform);
            _deleteRoot.sizeDelta = new Vector2(deleteButtonSize, deleteButtonSize);

            Image background = _deleteRoot.gameObject.AddComponent<Image>();
            background.color = new Color(0.85f, 0.15f, 0.1f, 0.95f);

            DeleteButton = _deleteRoot.gameObject.AddComponent<Button>();
            DeleteButton.targetGraphic = background;
            DeleteButton.onClick.AddListener(ShowConfirmation);

            AddLabel(_deleteRoot, "X", fontSize);
            _deleteRoot.gameObject.SetActive(false);

            BuildPopup();
        }

        private void BuildPopup()
        {
            RectTransform popup = NewUiObject("WindowDeletePopup", _canvas.transform);
            popup.anchorMin = new Vector2(0.5f, 0.5f);
            popup.anchorMax = new Vector2(0.5f, 0.5f);
            popup.anchoredPosition = Vector2.zero;
            popup.sizeDelta = popupSize;

            Image background = popup.gameObject.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.85f);

            RectTransform message = AddLabel(popup, confirmationMessage, fontSize);
            message.anchorMin = new Vector2(0f, 0.45f);
            message.anchorMax = Vector2.one;
            message.offsetMin = Vector2.zero;
            message.offsetMax = Vector2.zero;

            AddPopupButton(popup, "ConfirmButton", "Delete", new Color(0.85f, 0.15f, 0.1f, 1f),
                new Vector2(0.06f, 0.1f), new Vector2(0.48f, 0.38f), ConfirmDelete);
            AddPopupButton(popup, "CancelButton", "Cancel", new Color(0.3f, 0.3f, 0.32f, 1f),
                new Vector2(0.52f, 0.1f), new Vector2(0.94f, 0.38f), CancelDelete);

            _popupRoot = popup.gameObject;
            _popupRoot.SetActive(false);
        }

        private void AddPopupButton(RectTransform parent, string objectName, string text, Color color,
            Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = NewUiObject(objectName, parent);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image background = rect.gameObject.AddComponent<Image>();
            background.color = color;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(onClick);

            AddLabel(rect, text, fontSize);
        }

        private void ShowConfirmation()
        {
            if (_popupRoot != null) _popupRoot.SetActive(true);
        }

        private RectTransform AddLabel(RectTransform parent, string text, float size)
        {
            RectTransform rect = NewUiObject("Label", parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            return rect;
        }

        private static RectTransform NewUiObject(string objectName, Transform parent)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private void OnDestroy()
        {
            DestroyUiObject(_deleteRoot == null ? null : _deleteRoot.gameObject);
            DestroyUiObject(_popupRoot);
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
