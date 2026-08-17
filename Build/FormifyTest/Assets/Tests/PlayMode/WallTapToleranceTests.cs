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
    /// TOP-02 AC8 / AD-016 — edge-on in the plan a 0.15 m wall is about 10 px wide, so a floor tap within the
    /// serialized screen-space tolerance of a wall's base edge selects that wall instead of the floor. The
    /// nearest wall in range wins, and no 3D mode gets the tolerance at all.
    ///
    /// Only the floor carries a collider here: every tap in this fixture physically hits the FLOOR, so a pass
    /// can only come from the tolerance branch and never from a lucky direct wall hit.
    /// </summary>
    [TestFixture]
    public class WallTapToleranceTests
    {
        private const float RoomWidth = 4f;    // world X
        private const float RoomDepth = 3f;    // world Z
        private const float RoomHeight = 2.8f;
        private const float Thickness = 0.15f;

        /// <summary>The serialized default under test (SelectionController.wallTapTolerancePixels).</summary>
        private const float TolerancePixels = 30f;

        /// <summary>
        /// Fixed viewport and framing, so screen pixels map to metres at a known rate instead of following the
        /// test runner's screen. 600 px over 2 x 3 m of orthographic height = 100 px/m; SetUp asserts it.
        /// </summary>
        private const float ViewportWidth = 900f;

        private const float ViewportHeight = 600f;
        private const float OrthographicSize = 3f;
        /// <summary>
        /// Measured from the live camera rather than derived from the requested viewport: in batch mode the
        /// render target, not the assigned pixelRect, decides the framing. Only the offsets below depend on it,
        /// and they are expressed in pixels, so the exact rate does not matter as long as both axes agree.
        /// </summary>
        private float _pixelsPerMetre;

        /// <summary>Inside the tolerance — the "untappable wall" case AC8 exists for.</summary>
        private const float NearOffsetPixels = 10f;

        /// <summary>Also inside the tolerance, but further out than <see cref="NearOffsetPixels"/>.</summary>
        private const float MidOffsetPixels = 20f;

        /// <summary>Outside the tolerance, so the floor keeps the tap.</summary>
        private const float OutsideOffsetPixels = 40f;

        // RoomBuilder's ordering for the footprint below: walls follow the footprint edges, then floor, ceiling.
        private const int SouthWallId = 0;   // base edge along z = -RoomDepth / 2, spanning X
        private const int WestWallId = 3;    // base edge along x = -RoomWidth / 2, spanning Z
        private const int FloorId = 4;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        private Camera _camera;
        private RoomModel _model;
        private ModeManager _modes;
        private SelectionController _controller;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            RoomDefinition room = RoomBuilder.Build(Footprint(), RoomHeight, Thickness);
            Assert.IsNotNull(room, "RoomBuilder produced the room the rest of the fixture assumes");
            _model = new RoomModel(room.surfaces);
            _modes = new ModeManager(IsWallSelected);

            Assert.AreEqual(SurfaceKind.Wall, _model.GetSurface(SouthWallId).kind, "id 0 is a wall");
            Assert.AreEqual(SurfaceKind.Wall, _model.GetSurface(WestWallId).kind, "id 3 is a wall");
            Assert.AreEqual(SurfaceKind.Floor, _model.GetSurface(FloorId).kind, "id 4 is the floor");

            // The plan camera, built by hand: no TopDownController in this fixture, so nothing moves it.
            // Straight down from above the room, orthographic — world +X is screen +X, world +Z is screen +Y.
            GameObject cameraObject = Spawn("Plan Camera");
            cameraObject.transform.SetPositionAndRotation(
                new Vector3(0f, 5f, 0f), Quaternion.Euler(90f, 0f, 0f));
            _camera = cameraObject.AddComponent<Camera>();
            _camera.pixelRect = new Rect(0f, 0f, ViewportWidth, ViewportHeight);
            // Aspect is left derived from the live pixel rect. Forcing it makes WorldToScreenPoint scale X and
            // Y differently in batch mode, where the render target, not the requested rect, decides the size.
            _camera.orthographic = true;
            _camera.orthographicSize = OrthographicSize;

            CreateFloor(_model.GetSurface(FloorId));

            _controller = Spawn("SelectionController").AddComponent<SelectionController>();
            _controller.Configure(_model, _modes, _camera);

            yield return new WaitForFixedUpdate();

            // Every pixel offset below is only meaningful if the framing maps metres to pixels as assumed.
            _pixelsPerMetre = ScreenOf(new Vector3(0f, 0f, 1f)).y - ScreenOf(Vector3.zero).y;

            Assert.Greater(_pixelsPerMetre, 1f, "the plan camera must map world metres onto screen pixels");
            Assert.AreEqual(_pixelsPerMetre, ScreenOf(new Vector3(1f, 0f, 0f)).x - ScreenOf(Vector3.zero).x, 0.01f,
                "world X and world Z map to screen pixels at the same rate");
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
            _camera = null;
            _modes = null;
            _model = null;
        }

        [UnityTest]
        public IEnumerator In2D_AFloorTapJustInsideTheToleranceSelectsTheWall()
        {
            Assert.IsTrue(_modes.TrySet(Mode.TopDown));
            Assert.Less(NearOffsetPixels, TolerancePixels, "the offset is inside the tolerance under test");
            yield return null;

            // Straight into the room from the middle of the south wall's base edge: the offset IS the distance.
            Vector2 tap = ScreenOf(SouthWallMidpoint()) + new Vector2(0f, NearOffsetPixels);

            _controller.OnTap(tap);

            Assert.AreEqual(SouthWallId, _model.SelectedSurfaceId, "the wall took the floor's tap");
        }

        [UnityTest]
        public IEnumerator In2D_AFloorTapAtTheRoomCentreSelectsTheFloor()
        {
            Assert.IsTrue(_modes.TrySet(Mode.TopDown));
            yield return null;

            // The centre is 1.5 m from the nearest wall — 150 px, five times the tolerance.
            _controller.OnTap(ScreenOf(Vector3.zero));

            Assert.AreEqual(FloorId, _model.SelectedSurfaceId, "no wall is in range, so the floor keeps the tap");
        }

        [UnityTest]
        public IEnumerator In2D_AFloorTapOutsideTheToleranceSelectsTheFloor()
        {
            Assert.IsTrue(_modes.TrySet(Mode.TopDown));
            Assert.Greater(OutsideOffsetPixels, TolerancePixels, "the offset is outside the tolerance under test");
            yield return null;

            Vector2 tap = ScreenOf(SouthWallMidpoint()) + new Vector2(0f, OutsideOffsetPixels);

            _controller.OnTap(tap);

            Assert.AreEqual(FloorId, _model.SelectedSurfaceId, "40 px is past the 30 px default");
        }

        [UnityTest]
        public IEnumerator InOrbit_TheSameNearWallTapSelectsTheFloor()
        {
            Assert.AreEqual(Mode.Orbit, _modes.Current, "the tolerance is a 2D-only rule");
            yield return null;

            Vector2 tap = ScreenOf(SouthWallMidpoint()) + new Vector2(0f, NearOffsetPixels);

            _controller.OnTap(tap);

            Assert.AreEqual(FloorId, _model.SelectedSurfaceId, "3D resolves the hit surface and nothing else");
        }

        [UnityTest]
        public IEnumerator In2D_TheNearerOfTwoWallsInRangeWins()
        {
            Assert.IsTrue(_modes.TrySet(Mode.TopDown));
            Assert.Less(MidOffsetPixels, TolerancePixels, "both walls are inside the tolerance");
            Assert.Less(NearOffsetPixels, MidOffsetPixels, "the west wall is the nearer of the two");
            yield return null;

            // The corner shared by the south and west walls. From there +X only moves away from the WEST wall
            // and +Z only away from the SOUTH wall, so the two offsets are the two distances outright:
            // 10 px to the west wall, 20 px to the south wall.
            Vector2 corner = ScreenOf(new Vector3(-RoomWidth * 0.5f, 0f, -RoomDepth * 0.5f));

            _controller.OnTap(corner + new Vector2(NearOffsetPixels, MidOffsetPixels));

            Assert.AreEqual(WestWallId, _model.SelectedSurfaceId, "10 px beats 20 px");
        }

        private Vector2 ScreenOf(Vector3 worldPoint) => _camera.WorldToScreenPoint(worldPoint);

        /// <summary>Middle of the south wall's plan segment: origin -> origin + right * width, at the base.</summary>
        private Vector3 SouthWallMidpoint()
        {
            SurfaceDefinition wall = _model.GetSurface(SouthWallId);
            return wall.origin + wall.right * (wall.width * 0.5f);
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

        /// <summary>
        /// The floor view plus an explicit box collider covering the whole plan area, as in
        /// SelectionControllerTests: the generated MeshCollider is disabled so the raycast does not depend on
        /// mesh building, and the collider sits on a child so the tap also exercises GetComponentInParent.
        /// </summary>
        private void CreateFloor(SurfaceDefinition floor)
        {
            Assert.IsNotNull(floor, "the fixture's surface ids match RoomBuilder's ordering");

            GameObject viewObject = Spawn(floor.name);
            viewObject.AddComponent<SurfaceView>().Initialize(floor, _model);

            MeshCollider generated = viewObject.GetComponent<MeshCollider>();
            if (generated != null) generated.enabled = false;

            GameObject colliderObject = new GameObject(floor.name + " Collider");
            colliderObject.transform.SetParent(viewObject.transform, false);
            colliderObject.transform.SetPositionAndRotation(
                new Vector3(0f, -Thickness * 0.5f, 0f), Quaternion.identity);
            colliderObject.AddComponent<BoxCollider>().size =
                new Vector3(RoomWidth, Thickness, RoomDepth);
        }

        private GameObject Spawn(string name)
        {
            GameObject created = new GameObject(name);
            _spawned.Add(created);
            return created;
        }
    }
}
