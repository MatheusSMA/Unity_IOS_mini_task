using System.Collections;
using System.Collections.Generic;
using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Formify.Tests.PlayMode
{
    /// <summary>
    /// TOP-01/TOP-02 (AC2, AC3, AC5, AC7, AC9) — the 2D plan takes the ceiling out of rendering AND physics,
    /// enters on a clean slate at fit size, pinches only while it owns the view, and gives everything back.
    /// </summary>
    [TestFixture]
    public class TopDownControllerTests
    {
        private const float RoomWidth = 6f;    // world X
        private const float RoomDepth = 4f;    // world Z
        private const float RoomHeight = 2.8f;
        private const float Thickness = 0.15f;

        /// <summary>Serialized defaults under test (fitMargin 0.1, minZoom 0.5, maxZoom 2.0 — AD-016).</summary>
        private const float FitMargin = 0.1f;
        private const float MinZoom = 0.5f;
        private const float MaxZoom = 2.0f;

        /// <summary>Not the Unity default (60), so "restored" cannot pass by accident.</summary>
        private const float OriginalFieldOfView = 55f;

        private const float Tolerance = 1e-3f;

        private const int WallId = 0;
        private const int FloorId = 4;
        private const int CeilingId = 5;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        private RoomModel _model;
        private ModeManager _modes;
        private Camera _camera;
        private OrbitCameraController _orbit;
        private TopDownController _controller;
        private Bounds _roomBounds;

        private MeshRenderer _ceilingRenderer;
        private MeshCollider _ceilingCollider;
        private MeshCollider _floorCollider;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            RoomDefinition room = RoomBuilder.Build(Footprint(), RoomHeight, Thickness);
            Assert.IsNotNull(room, "RoomBuilder produced the room the rest of the fixture assumes");
            _model = new RoomModel(room.surfaces);
            _modes = new ModeManager(IsWallSelected);

            _roomBounds = new Bounds(
                new Vector3(0f, RoomHeight * 0.5f, 0f),
                new Vector3(RoomWidth, RoomHeight, RoomDepth));

            // Camera and orbit rig on one GameObject, as in the scene: the exit path has to restore the camera
            // AND let the rig own the final position.
            GameObject rig = Spawn("RoomCamera");
            _camera = rig.AddComponent<Camera>();
            // Fixed square viewport: fit-to-room depends on the aspect, which must not follow the test runner's
            // screen. With aspect 1 the width is the binding dimension (3 / 1 > 4 / 2).
            _camera.pixelRect = new Rect(0f, 0f, 600f, 600f);
            _camera.aspect = 1f;
            _camera.orthographic = false;
            _camera.fieldOfView = OriginalFieldOfView;

            _orbit = rig.AddComponent<OrbitCameraController>();
            _orbit.ConfigureRoom(Vector3.zero, _roomBounds);

            SurfaceView ceiling = CreateView(_model.GetSurface(CeilingId));
            _ceilingRenderer = ceiling.GetComponent<MeshRenderer>();
            _ceilingCollider = ceiling.GetComponent<MeshCollider>();

            SurfaceView floor = CreateView(_model.GetSurface(FloorId));
            _floorCollider = floor.GetComponent<MeshCollider>();

            _controller = Spawn("TopDownController").AddComponent<TopDownController>();
            _controller.Configure(_modes, _model, _orbit, _camera, ceiling, _roomBounds);

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

            _controller = null;
            _orbit = null;
            _camera = null;
            _modes = null;
            _model = null;
            _ceilingRenderer = null;
            _ceilingCollider = null;
            _floorCollider = null;
        }

        [UnityTest]
        public IEnumerator Entering2D_FitsTheRoom_HidesTheCeiling_AndClearsTheSelection()
        {
            _model.Select(WallId);
            Assert.AreEqual(WallId, _model.SelectedSurfaceId, "a surface is selected before the switch");

            Assert.IsTrue(_modes.TrySet(Mode.TopDown));
            yield return null;

            Assert.IsTrue(_controller.IsTopDown);

            // TOP AC7: half-height 2, half-width/aspect 3 -> 3 * (1 + margin).
            float expectedFit = Mathf.Max(RoomDepth * 0.5f, RoomWidth * 0.5f / 1f) * (1f + FitMargin);
            Assert.AreEqual(3.3f, expectedFit, Tolerance, "the fixture's own arithmetic");
            Assert.AreEqual(expectedFit, _controller.FitOrthographicSize, Tolerance);

            Assert.IsTrue(_camera.orthographic);
            Assert.AreEqual(_controller.FitOrthographicSize, _camera.orthographicSize, Tolerance);

            // Straight down from above the room centre.
            Assert.AreEqual(0f, _camera.transform.position.x, Tolerance);
            Assert.AreEqual(0f, _camera.transform.position.z, Tolerance);
            Assert.Greater(_camera.transform.position.y, _roomBounds.max.y, "the camera is above the ceiling");
            Assert.AreEqual(0f, Vector3.Distance(_camera.transform.forward, Vector3.down), Tolerance);

            // TOP AC2 / AD-012 — both halves, not just the renderer.
            Assert.IsFalse(_ceilingRenderer.enabled, "ceiling MeshRenderer off");
            Assert.IsFalse(_ceilingCollider.enabled, "ceiling MeshCollider off");

            // TOP AC3.
            Assert.IsNull(_model.SelectedSurfaceId, "entering 2D is a clean slate");
        }

        [UnityTest]
        public IEnumerator In2D_ARayStraightDownReachesTheFloor_NotTheCeiling()
        {
            Assert.IsTrue(_modes.TrySet(Mode.TopDown));
            yield return new WaitForFixedUpdate();

            Vector3 origin = new Vector3(0f, 10f, 0f);
            Assert.IsTrue(Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 50f), "the ray hits something");

            Assert.AreSame(_floorCollider, hit.collider, "the plan tap reaches the floor");
            Assert.AreNotSame(_ceilingCollider, hit.collider, "the disabled ceiling does not eat the tap");
            Assert.AreEqual(0f, hit.point.y, Tolerance, "hit on the floor's top face");

            // Control: the very same ray DOES hit the ceiling once it is back, so this test could fail.
            Assert.IsTrue(_modes.TrySet(Mode.Orbit));
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(Physics.Raycast(origin, Vector3.down, out RaycastHit restored, 50f));
            Assert.AreSame(_ceilingCollider, restored.collider, "with the ceiling back the ray stops there");
        }

        [UnityTest]
        public IEnumerator Pinch_ClampsToHalfAndDoubleTheFitSize()
        {
            Assert.IsTrue(_modes.TrySet(Mode.TopDown));
            yield return null;

            float fit = _controller.FitOrthographicSize;
            Assert.AreEqual(fit, _camera.orthographicSize, Tolerance, "the plan opens at fit size");

            // Positive delta = zoom IN = smaller orthographic size.
            for (int i = 0; i < 10; i++) _controller.ApplyPinch(0.5f);
            Assert.AreEqual(MinZoom * fit, _camera.orthographicSize, Tolerance, "zoom-in clamp (0.5x fit)");

            for (int i = 0; i < 20; i++) _controller.ApplyPinch(-0.5f);
            Assert.AreEqual(MaxZoom * fit, _camera.orthographicSize, Tolerance, "zoom-out clamp (2.0x fit)");

            // One unclamped step, so the clamps above cannot hide a broken mapping.
            _controller.ApplyPinch(0.5f);
            Assert.AreEqual(MaxZoom * fit - 0.5f * fit, _camera.orthographicSize, Tolerance);
        }

        [UnityTest]
        public IEnumerator Pinch_InOrbit_ChangesNothing()
        {
            Assert.AreEqual(Mode.Orbit, _modes.Current);
            Assert.IsFalse(_controller.IsTopDown);
            yield return null;

            float sizeBefore = _camera.orthographicSize;

            _controller.ApplyPinch(1f);
            _controller.ApplyPinch(-1f);

            // TOP AC9: pinch has no effect in the 3D views.
            Assert.IsFalse(_camera.orthographic, "still perspective");
            Assert.AreEqual(OriginalFieldOfView, _camera.fieldOfView, Tolerance);
            Assert.AreEqual(sizeBefore, _camera.orthographicSize, Tolerance);
        }

        [UnityTest]
        public IEnumerator ExitingTo3D_RestoresTheCeiling_TheCamera_AndTheRig()
        {
            Assert.IsTrue(_modes.TrySet(Mode.TopDown));
            yield return null;

            Assert.IsTrue(_camera.orthographic, "precondition: the plan owned the camera");
            Assert.IsFalse(_ceilingRenderer.enabled);

            Assert.IsTrue(_modes.TrySet(Mode.Orbit));
            yield return null;

            Assert.IsFalse(_controller.IsTopDown);
            Assert.IsTrue(_ceilingRenderer.enabled, "ceiling MeshRenderer back (TOP AC5)");
            Assert.IsTrue(_ceilingCollider.enabled, "ceiling MeshCollider back (TOP AC5)");

            Assert.IsFalse(_camera.orthographic, "back to perspective");
            Assert.AreEqual(OriginalFieldOfView, _camera.fieldOfView, Tolerance, "the original field of view");

            // OrbitCameraController.ResetToRoomCentre: the room centre at the serialized eye height (1.6),
            // clamped into the room bounds.
            Assert.AreEqual(0f, _orbit.transform.position.x, Tolerance);
            Assert.AreEqual(1.6f, _orbit.transform.position.y, Tolerance);
            Assert.AreEqual(0f, _orbit.transform.position.z, Tolerance);
        }

        private static Vector2[] Footprint()
        {
            float x = RoomWidth * 0.5f;
            float z = RoomDepth * 0.5f;
            return new[]
            {
                new Vector2(-x, -z),
                new Vector2(x, -z),
                new Vector2(x, z),
                new Vector2(-x, z)
            };
        }

        private bool IsWallSelected()
        {
            int? id = _model.SelectedSurfaceId;
            if (!id.HasValue) return false;

            SurfaceDefinition surface = _model.GetSurface(id.Value);
            return surface != null && surface.kind == SurfaceKind.Wall;
        }

        /// <summary>A real SurfaceView, so the mesh and the MeshCollider are the generated ones.</summary>
        private SurfaceView CreateView(SurfaceDefinition surface)
        {
            Assert.IsNotNull(surface, "the fixture's surface ids match RoomBuilder's ordering");

            SurfaceView view = Spawn(surface.name).AddComponent<SurfaceView>();
            view.Initialize(surface, _model);
            return view;
        }

        private GameObject Spawn(string name)
        {
            GameObject created = new GameObject(name);
            _spawned.Add(created);
            return created;
        }
    }
}
