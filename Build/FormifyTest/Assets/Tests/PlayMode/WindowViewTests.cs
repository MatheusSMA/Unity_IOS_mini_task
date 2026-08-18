using System.Collections;
using System.Collections.Generic;
using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Formify.Tests.PlayMode
{
    /// <summary>WIN-04 AC1-AC5 and SEL-03 AC7 — the opening is tappable and owns its own deletion flow.</summary>
    [TestFixture]
    public class WindowViewTests
    {
        private const int WallId = 0;
        private const float WallWidth = 4f;
        private const float WallHeight = 2.8f;
        private const float WallThickness = 0.15f;

        private static readonly Rect2D WindowRect = new Rect2D(1f, 0.8f, 1.2f, 1.4f);

        private SurfaceDefinition _surface;
        private RoomModel _model;
        private WindowSpec _spec;
        private GameObject _cameraGo;
        private GameObject _canvasGo;
        private GameObject _eventSystemGo;
        private GameObject _factoryGo;
        private WindowViewFactory _factory;
        private Canvas _canvas;
        private Camera _camera;

        [SetUp]
        public void SetUp()
        {
            _surface = new SurfaceDefinition
            {
                id = WallId,
                name = "Wall 1",
                kind = SurfaceKind.Wall,
                origin = Vector3.zero,
                right = Vector3.right,
                up = Vector3.up,
                width = WallWidth,
                height = WallHeight,
                thickness = WallThickness
            };

            _model = new RoomModel(new List<SurfaceDefinition> { _surface });

            _cameraGo = new GameObject("TestCamera", typeof(Camera));
            _cameraGo.tag = "MainCamera";
            // The wall spans the XY plane at z = 0 with its normal along +Z, so the room side is +Z.
            _cameraGo.transform.position = new Vector3(WallWidth * 0.5f, 1.6f, 3f);
            _cameraGo.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            _camera = _cameraGo.GetComponent<Camera>();
            // Fixed viewport: the corner projection must not depend on the headless screen size.
            _camera.pixelRect = new Rect(0f, 0f, 900f, 600f);
            _camera.aspect = 1.5f;

            _canvasGo = new GameObject("TestCanvas", typeof(RectTransform));
            _canvas = _canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvasGo.AddComponent<CanvasScaler>();
            _canvasGo.AddComponent<GraphicRaycaster>();

            _eventSystemGo = new GameObject("TestEventSystem", typeof(EventSystem));

            _factoryGo = new GameObject("WindowViewFactory");
            _factory = _factoryGo.AddComponent<WindowViewFactory>();
            _factory.Configure(_model, id => _model.GetSurface(id), _canvas);

            Assert.IsTrue(_model.TryAddWindow(WallId, WindowRect, out WindowRejection reason), "window accepted");
            Assert.AreEqual(WindowRejection.None, reason);

            _spec = _model.GetWindows(WallId)[0];
        }

        [TearDown]
        public void TearDown()
        {
            if (_factoryGo != null) Object.DestroyImmediate(_factoryGo);
            if (_canvasGo != null) Object.DestroyImmediate(_canvasGo);
            if (_eventSystemGo != null) Object.DestroyImmediate(_eventSystemGo);
            if (_cameraGo != null) Object.DestroyImmediate(_cameraGo);

            _factoryGo = null;
            _canvasGo = null;
            _eventSystemGo = null;
            _cameraGo = null;
            _factory = null;
            _canvas = null;
            _camera = null;
            _model = null;
            _surface = null;
            _spec = null;
        }

        /// <summary>Centre of the opening at mid-thickness — where Initialize puts the collider.</summary>
        private Vector3 OpeningCentre()
        {
            return _surface.LocalToWorld(
                       WindowRect.x + WindowRect.width * 0.5f,
                       WindowRect.y + WindowRect.height * 0.5f)
                   - _surface.Normal * (WallThickness * 0.5f);
        }

        private WindowView SingleView()
        {
            Assert.AreEqual(1, _factory.Views.Count, "exactly one view for one window");
            return _factory.Views[0];
        }

        [UnityTest]
        public IEnumerator WindowAdded_CreatesOneViewWithAColliderSizedToTheOpening()
        {
            yield return null;

            WindowView[] all = Object.FindObjectsByType<WindowView>(FindObjectsSortMode.None);
            Assert.AreEqual(1, all.Length, "one WindowView in the scene");

            WindowView view = SingleView();
            Assert.AreSame(all[0], view, "the factory owns the scene's only view");
            Assert.AreEqual(_spec.id, view.Spec.id, "view is bound to the added window");
            Assert.AreEqual(WallId, view.Spec.surfaceId);

            BoxCollider box = view.GetComponent<BoxCollider>();
            Assert.IsNotNull(box, "AC1: the opening carries its own collider");
            Assert.AreEqual(WindowRect.width, box.size.x, 0.0001f, "collider width == rect width");
            Assert.AreEqual(WindowRect.height, box.size.y, 0.0001f, "collider height == rect height");
            Assert.AreEqual(WallThickness, box.size.z, 0.0001f, "collider depth == wall thickness");
        }

        [UnityTest]
        public IEnumerator Collider_SitsInTheOpeningAndIsHitByARayFromTheRoom()
        {
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();

            WindowView view = SingleView();
            Vector3 centre = OpeningCentre();
            Vector3 origin = centre + _surface.Normal * 2f;

            Assert.IsTrue(Physics.Raycast(origin, -_surface.Normal, out RaycastHit hit, 5f),
                "a ray aimed at the opening centre hits something");

            // AD-014: SelectionController identifies the target by this component, not by a layer.
            WindowView hitView = hit.collider.GetComponentInParent<WindowView>();
            Assert.IsNotNull(hitView, "the hit collider belongs to a WindowView");
            Assert.AreSame(view, hitView, "and it is this window's view");
            Assert.AreEqual(_spec.id, hitView.Spec.id);
        }

        [UnityTest]
        public IEnumerator OnTapped_ShowsTheDeleteButtonAndLeavesTheSelectionUntouched()
        {
            yield return null;

            _model.Select(WallId);
            int? before = _model.SelectedSurfaceId;
            int selectionEvents = 0;
            _model.SelectionChanged += (previous, current) => selectionEvents++;

            WindowView view = SingleView();
            Assert.IsFalse(view.IsDeleteButtonShown, "no X before the tap");

            view.OnTapped();
            yield return null;

            Assert.IsTrue(view.IsDeleteButtonShown, "AC2: the X is shown");
            Assert.IsNotNull(view.DeleteButton, "the X is a uGUI button");
            Assert.IsTrue(view.DeleteButton.gameObject.activeInHierarchy, "the X GameObject is active");
            Assert.AreSame(_canvas.transform, view.DeleteButton.transform.parent, "the X lives on the UI canvas");
            Assert.IsFalse(view.IsConfirmationShown, "the popup waits for the X click");

            // SEL-03 AC7: routing a tap to a window never moves the selection.
            Assert.AreEqual(WallId, before, "the wall was selected before the tap");
            Assert.AreEqual(before, _model.SelectedSurfaceId, "selection unchanged after the tap");
            Assert.AreEqual(0, selectionEvents, "no SelectionChanged was raised");
        }

        /// <summary>Screen point of one opening corner, in surface-local metres.</summary>
        private Vector2 ScreenCorner(float localX, float localY)
        {
            Vector3 screen = _camera.WorldToScreenPoint(_surface.LocalToWorld(localX, localY));
            return new Vector2(screen.x, screen.y);
        }

        /// <summary>
        /// WIN-04 AC2 — "anchored to the opening's top-right corner", asserted as a position. The X is a
        /// screen-space widget on an overlay canvas, so its transform position IS its screen point.
        /// </summary>
        [UnityTest]
        public IEnumerator DeleteButton_IsAnchoredToTheOpeningsTopRightCorner()
        {
            yield return null;

            WindowView view = SingleView();
            view.OnTapped();
            yield return null;   // LateUpdate re-projects the corner

            Vector3 position = view.DeleteButton.transform.position;
            Vector2 actual = new Vector2(position.x, position.y);

            Vector2 topRight = ScreenCorner(WindowRect.XMax, WindowRect.YMax);
            Assert.AreEqual(topRight.x, actual.x, 0.5f, "AC2: the X sits on the opening's top-right corner (x)");
            Assert.AreEqual(topRight.y, actual.y, 0.5f, "AC2: the X sits on the opening's top-right corner (y)");

            // The other three corners are ~200 px away each, so no corner mix-up can pass the assertion above.
            Assert.Greater(Vector2.Distance(ScreenCorner(WindowRect.x, WindowRect.y), actual), 50f, "not bottom-left");
            Assert.Greater(Vector2.Distance(ScreenCorner(WindowRect.XMax, WindowRect.y), actual), 50f, "not bottom-right");
            Assert.Greater(Vector2.Distance(ScreenCorner(WindowRect.x, WindowRect.YMax), actual), 50f, "not top-left");
        }

        [UnityTest]
        public IEnumerator DeleteButtonClick_ShowsTheConfirmationPopup()
        {
            yield return null;

            WindowView view = SingleView();
            view.OnTapped();
            Assert.IsFalse(view.IsConfirmationShown, "popup hidden until the X is clicked");

            view.DeleteButton.onClick.Invoke();
            yield return null;

            Assert.IsTrue(view.IsConfirmationShown, "AC3: the confirmation popup is shown");
            Assert.AreEqual(1, _model.GetWindows(WallId).Count, "nothing removed yet");
        }

        [UnityTest]
        public IEnumerator CancelDelete_ClosesThePopupAndKeepsTheWindow()
        {
            yield return null;

            WindowView view = SingleView();
            view.OnTapped();
            view.DeleteButton.onClick.Invoke();
            Assert.IsTrue(view.IsConfirmationShown);

            view.CancelDelete();
            yield return null;

            Assert.IsFalse(view.IsConfirmationShown, "AC5: the popup is closed");
            Assert.AreEqual(1, _model.GetWindows(WallId).Count, "AC5: the window is untouched");
            Assert.AreEqual(_spec.id, _model.GetWindows(WallId)[0].id);
            Assert.IsTrue(view != null, "the view survives a cancel");
            Assert.AreEqual(1, _factory.Views.Count);
        }

        [UnityTest]
        public IEnumerator ConfirmDelete_RemovesTheWindowAndDestroysTheView()
        {
            yield return null;

            WindowView view = SingleView();
            view.OnTapped();
            view.DeleteButton.onClick.Invoke();

            view.ConfirmDelete();

            Assert.AreEqual(0, _model.GetWindows(WallId).Count, "AC4: the window is gone from the model");
            Assert.AreEqual(0, _factory.Views.Count, "the factory dropped the view");

            yield return null;

            Assert.IsTrue(view == null, "the view GameObject was destroyed");
            Assert.AreEqual(0, Object.FindObjectsByType<WindowView>(FindObjectsSortMode.None).Length,
                "no WindowView left in the scene");
        }

        [UnityTest]
        public IEnumerator TryRemoveWindow_OnTheModelAlsoDestroysTheView()
        {
            yield return null;

            WindowView view = SingleView();

            Assert.IsTrue(_model.TryRemoveWindow(_spec.id), "the model removed the window");
            Assert.AreEqual(0, _factory.Views.Count, "the factory reacted to WindowRemoved");

            yield return null;

            Assert.IsTrue(view == null, "the view GameObject was destroyed");
            Assert.AreEqual(0, _model.GetWindows(WallId).Count);
        }
    }
}
