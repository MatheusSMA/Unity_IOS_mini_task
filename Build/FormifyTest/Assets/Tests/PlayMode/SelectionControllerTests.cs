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

        [UnityTest]
        public IEnumerator A_window_target_takes_the_tap_and_the_selection_stands()
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
