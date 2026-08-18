using System.Collections;
using System.Collections.Generic;
using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Formify.Tests.PlayMode
{
    public class SelectionControllerTests
    {
        // Two walls side by side on the z = 2 plane, seen by a camera at the origin looking down +Z.
        private static readonly Vector3 WallACentre = new Vector3(-1.75f, 1.4f, 2f);
        private static readonly Vector3 WallBCentre = new Vector3(1.75f, 1.4f, 2f);
        private static readonly Vector3 WallSize = new Vector3(2.5f, 2.8f, 0.15f);

        // A window on wall A, wall-local. Its centre is (1.25, 1.3) -> world (-1.75, 1.3, 2).
        private static readonly Rect2D WindowRect = new Rect2D(0.8f, 0.8f, 0.9f, 1.0f);

        // Where the test parks that window's collider: same centre, pulled a metre off the wall so the ray
        // meets it before the fixture's wall box (which, unlike the real slab, has no hole cut in it).
        private static readonly Vector3 WindowInFront = new Vector3(-1.75f, 1.3f, 1f);

        private readonly List<GameObject> _spawned = new List<GameObject>();

        private Camera _camera;
        private RoomModel _model;
        private ModeManager _modes;
        private SelectionController _controller;
        private SurfaceDefinition _wallA;
        private SurfaceDefinition _wallB;

        private int _events;
        private int? _previous;
        private int? _current;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _wallA = Wall(0, "Wall A", new Vector3(-0.5f, 0f, 2f));
            _wallB = Wall(1, "Wall B", new Vector3(3f, 0f, 2f));
            _model = new RoomModel(new[] { _wallA, _wallB });
            _modes = new ModeManager(IsWallSelected);

            GameObject cameraObject = Spawn("Camera");
            cameraObject.transform.position = new Vector3(0f, 1.4f, 0f);
            _camera = cameraObject.AddComponent<Camera>();
            // Fixed viewport: the screen/ray roundtrip must not depend on the headless screen size.
            _camera.pixelRect = new Rect(0f, 0f, 900f, 600f);
            _camera.aspect = 1.5f;

            CreateWall(_wallA, WallACentre);
            CreateWall(_wallB, WallBCentre);

            _controller = Spawn("SelectionController").AddComponent<SelectionController>();
            _controller.Configure(_model, _modes, _camera);

            _events = 0;
            _previous = null;
            _current = null;
            _model.SelectionChanged += OnSelectionChanged;

            yield return new WaitForFixedUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            // Immediate: a deferred Destroy would leave these colliders alive into the next test's raycast.
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) Object.DestroyImmediate(_spawned[i]);
            }
            _spawned.Clear();
        }

        [UnityTest]
        public IEnumerator Tapping_a_wall_selects_it()
        {
            _controller.OnTap(ScreenPositionOf(WallACentre));

            Assert.AreEqual(_wallA.id, _model.SelectedSurfaceId);
            Assert.AreEqual(1, _events);
            yield break;
        }

        [UnityTest]
        public IEnumerator Tapping_a_second_wall_moves_the_selection_in_one_event()
        {
            _controller.OnTap(ScreenPositionOf(WallACentre));
            _controller.OnTap(ScreenPositionOf(WallBCentre));

            Assert.AreEqual(_wallB.id, _model.SelectedSurfaceId);
            Assert.AreEqual(2, _events);
            Assert.AreEqual(_wallA.id, _previous);
            Assert.AreEqual(_wallB.id, _current);
            yield break;
        }

        [UnityTest]
        public IEnumerator Tapping_the_selected_wall_raises_nothing()
        {
            _controller.OnTap(ScreenPositionOf(WallACentre));
            _events = 0;

            _controller.OnTap(ScreenPositionOf(WallACentre));

            Assert.AreEqual(0, _events);
            Assert.AreEqual(_wallA.id, _model.SelectedSurfaceId);
            yield break;
        }

        [UnityTest]
        public IEnumerator Tapping_empty_space_keeps_the_selection()
        {
            _controller.OnTap(ScreenPositionOf(WallACentre));
            _events = 0;

            // Far above the walls: the ray leaves the room without hitting anything.
            _controller.OnTap(ScreenPositionOf(new Vector3(0f, 12f, 2f)));

            Assert.AreEqual(0, _events);
            Assert.AreEqual(_wallA.id, _model.SelectedSurfaceId);
            yield break;
        }

        [UnityTest]
        public IEnumerator WindowDraw_mode_swallows_the_tap()
        {
            _controller.OnTap(ScreenPositionOf(WallACentre));
            Assert.IsTrue(_modes.TrySet(Mode.WindowDraw));
            Assert.AreEqual(Mode.WindowDraw, _modes.Current);
            _events = 0;

            _controller.OnTap(ScreenPositionOf(WallBCentre));

            Assert.AreEqual(0, _events);
            Assert.AreEqual(_wallA.id, _model.SelectedSurfaceId);
            yield break;
        }

        /// <summary>AR-01 AC5 — the mirror image of the WindowDraw case above: Ar owns the camera pose, not the
        /// tap, so a tap in Ar moves the selection exactly like it does in Orbit.</summary>
        [UnityTest]
        public IEnumerator Ar_mode_keeps_taps_selecting()
        {
            _controller.OnTap(ScreenPositionOf(WallACentre));
            Assert.IsTrue(_modes.TrySet(Mode.Ar), "Ar is legal from Orbit (AD-013)");
            Assert.AreEqual(Mode.Ar, _modes.Current);
            _events = 0;

            _controller.OnTap(ScreenPositionOf(WallBCentre));

            Assert.AreEqual(_wallB.id, _model.SelectedSurfaceId, "the tap moved the selection while in AR");
            Assert.AreEqual(1, _events);
            Assert.AreEqual(_wallA.id, _previous);
            Assert.AreEqual(_wallB.id, _current);
            yield break;
        }

        /// <summary>
        /// The routing half, with a target that does nothing: the window in front of the wall wins the tap and
        /// the controller itself never selects the surface behind it. What a real target does with the tap is
        /// its own business (B2) and is asserted below and in WindowViewTests.
        /// </summary>
        [UnityTest]
        public IEnumerator A_window_target_takes_the_tap_from_the_surface_behind_it()
        {
            _controller.OnTap(ScreenPositionOf(WallACentre));
            _events = 0;

            // In front of wall A, so the ray reaches the window first.
            Vector3 centre = new Vector3(WallACentre.x, WallACentre.y, 1f);
            GameObject windowObject = Spawn("Window");
            windowObject.transform.position = centre;
            windowObject.AddComponent<BoxCollider>().size = new Vector3(0.8f, 0.8f, 0.1f);
            WindowTapSpy spy = windowObject.AddComponent<WindowTapSpy>();
            yield return new WaitForFixedUpdate();

            _controller.OnTap(ScreenPositionOf(centre));

            Assert.AreEqual(1, spy.Taps);
            Assert.AreEqual(0, _events);
            Assert.AreEqual(_wallA.id, _model.SelectedSurfaceId);
        }

        /// <summary>
        /// B2 — a tap on a real window is a selection: the model's window selection moves to it and the surface
        /// selection is cleared on the way (AD-026). This is what AD-014 / SEL-03 AC7 used to forbid.
        /// </summary>
        [UnityTest]
        public IEnumerator Tapping_a_window_selects_it_and_clears_the_surface_selection()
        {
            _controller.OnTap(ScreenPositionOf(WallACentre));
            Assert.AreEqual(_wallA.id, _model.SelectedSurfaceId, "precondition: a surface is selected");
            _events = 0;

            WindowSpec spec = CreateWindowOnWallA();
            yield return new WaitForFixedUpdate();

            _controller.OnTap(ScreenPositionOf(WindowInFront));

            Assert.AreEqual(spec.id, _model.SelectedWindowId, "the window took the selection");
            Assert.IsNull(_model.SelectedSurfaceId, "AD-026: the surface selection went with it");
            Assert.AreEqual(1, _events, "one SelectionChanged, reporting the surface being cleared");
            Assert.AreEqual(_wallA.id, _previous);
            Assert.IsNull(_current);
        }

        /// <summary>The other direction of AD-026: the wall takes the selection back off the window.</summary>
        [UnityTest]
        public IEnumerator Tapping_a_wall_after_a_window_clears_the_window_selection()
        {
            WindowSpec spec = CreateWindowOnWallA();
            yield return new WaitForFixedUpdate();

            _controller.OnTap(ScreenPositionOf(WindowInFront));
            Assert.AreEqual(spec.id, _model.SelectedWindowId, "precondition: the window is selected");

            _controller.OnTap(ScreenPositionOf(WallBCentre));

            Assert.AreEqual(_wallB.id, _model.SelectedSurfaceId, "the wall took the selection");
            Assert.IsNull(_model.SelectedWindowId, "and the window lost it");
        }

        /// <summary>
        /// A real WindowView bound to a real window in the model, parked in front of wall A. No canvas: this
        /// fixture only cares that the tap reaches the model, not what the affordance looks like.
        /// </summary>
        private WindowSpec CreateWindowOnWallA()
        {
            Assert.IsTrue(_model.TryAddWindow(_wallA.id, WindowRect, out WindowRejection reason),
                "precondition: the window is accepted");
            Assert.AreEqual(WindowRejection.None, reason);

            WindowSpec spec = _model.GetWindows(_wallA.id)[0];

            GameObject windowObject = Spawn("Window View");
            WindowView view = windowObject.AddComponent<WindowView>();
            view.Initialize(spec, _wallA, _model, null);
            // Initialize embeds the collider in the wall slab; move it clear of the fixture's solid wall box.
            windowObject.transform.position = WindowInFront;
            Physics.SyncTransforms();

            return spec;
        }

        private void OnSelectionChanged(int? previous, int? current)
        {
            _events++;
            _previous = previous;
            _current = current;
        }

        private bool IsWallSelected()
        {
            int? id = _model.SelectedSurfaceId;
            return id.HasValue && _model.GetSurface(id.Value)?.kind == SurfaceKind.Wall;
        }

        private Vector2 ScreenPositionOf(Vector3 worldPoint) => _camera.WorldToScreenPoint(worldPoint);

        private static SurfaceDefinition Wall(int id, string name, Vector3 origin)
        {
            return new SurfaceDefinition
            {
                id = id,
                name = name,
                kind = SurfaceKind.Wall,
                origin = origin,
                right = new Vector3(-1f, 0f, 0f),
                up = Vector3.up,
                width = WallSize.x,
                height = WallSize.y,
                thickness = WallSize.z
            };
        }

        /// <summary>
        /// A SurfaceView with an explicit box collider on a child, so the tap also exercises the
        /// GetComponentInParent lookup. The generated MeshCollider belongs to T15/T16 and is disabled here to
        /// keep the raycast independent of mesh building.
        /// </summary>
        private void CreateWall(SurfaceDefinition surface, Vector3 centre)
        {
            GameObject viewObject = Spawn(surface.name);
            viewObject.transform.position = surface.origin;
            viewObject.AddComponent<SurfaceView>().Initialize(surface, _model);

            MeshCollider generated = viewObject.GetComponent<MeshCollider>();
            if (generated != null) generated.enabled = false;

            GameObject colliderObject = new GameObject(surface.name + " Collider");
            colliderObject.transform.SetParent(viewObject.transform, false);
            colliderObject.transform.SetPositionAndRotation(centre, Quaternion.identity);
            colliderObject.AddComponent<BoxCollider>().size = WallSize;
        }

        private GameObject Spawn(string name)
        {
            GameObject created = new GameObject(name);
            _spawned.Add(created);
            return created;
        }
    }

    internal class WindowTapSpy : MonoBehaviour, IWindowTapTarget
    {
        public int Taps { get; private set; }

        public void OnTapped() => Taps++;
    }
}
