using System.Collections;
using System.Collections.Generic;
using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.TestTools;

namespace Formify.Tests.PlayMode
{
    /// <summary>
    /// WIN-01 AC2 and AR-01 AC2 — RoomBootstrap.OnDragDelta is the single place that decides where a drag goes,
    /// so the drag is driven end to end: a virtual Touchscreen through the composed InputRouter into the
    /// mode-dependent branch. The composition only builds its camera branch when a camera exists, which is why
    /// the fixture creates one before Awake runs.
    /// </summary>
    public class RoomBootstrapDragRoutingTests : InputTestFixture
    {
        // Serialized and private on OrbitCameraController; mirrors the spec default the assertions use.
        const float DegreesPerPixel = 0.2f;

        // Wall 3 is the wall on the z = 2 plane of the default 6 x 4 room; the composed rig sits at the room
        // centre looking down +Z, so both drag points land on it well inside the edge margin.
        const int Wall3Id = 2;
        static readonly Vector3 DragFromWorld = new Vector3(-0.5f, 1.0f, 2f);
        static readonly Vector3 DragToWorld = new Vector3(0.5f, 1.8f, 2f);

        // Wall 3 has origin (3, 0, 2) and right = -X, so the two world points above are these wall-local metres.
        static readonly Rect2D ExpectedRect = new Rect2D(2.5f, 1.0f, 1.0f, 0.8f);

        GameObject cameraObject;
        GameObject bootstrapObject;
        Camera roomCamera;
        RoomBootstrap bootstrap;

        public override void Setup()
        {
            base.Setup();

            // Every screen position below is projected through this camera and fed straight back into its own
            // ScreenPointToRay, so nothing here depends on the headless screen size.
            cameraObject = new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
            roomCamera = cameraObject.GetComponent<Camera>();

            bootstrapObject = new GameObject(nameof(RoomBootstrap));
            bootstrap = bootstrapObject.AddComponent<RoomBootstrap>();   // Awake builds the room and composes

            // InputRouter turns TouchSimulation on in the Editor, and that adds a second "Simulated Touchscreen"
            // which would outrank the virtual one below as Touchscreen.current. Drop it for the duration of the test.
            TouchSimulation.Destroy();
            InputSystem.AddDevice<Touchscreen>();

            // EDGE-02's gate is InputRouterTests' subject; here it only has to be out of the way of the drag.
            bootstrap.Input.IsPointerOverUi = _ => false;

            Assert.IsNotNull(bootstrap.OrbitCamera, "precondition: Compose ran its camera branch");
            Assert.IsNotNull(bootstrap.WindowDraw, "precondition: the window-draw gesture was composed");
            Assert.AreEqual("Wall 3", bootstrap.Model.GetSurface(Wall3Id).name, "precondition: the wall the rig faces");
        }

        public override void TearDown()
        {
            // DestroyImmediate so OnDisable/OnDestroy run before base.TearDown() resets the input system under it.
            if (bootstrapObject != null) UnityEngine.Object.DestroyImmediate(bootstrapObject);
            if (cameraObject != null) UnityEngine.Object.DestroyImmediate(cameraObject);
            base.TearDown();
        }

        Vector2 ScreenOf(Vector3 worldPoint) => roomCamera.WorldToScreenPoint(worldPoint);

        /// <summary>
        /// One press-move-release, spread over frames so InputRouter classifies it as a drag. The two screen
        /// points are passed in, never re-projected: in Orbit the camera turns mid-drag, so a projection taken
        /// afterwards no longer describes the gesture that was performed.
        /// </summary>
        IEnumerator Drag(Vector2 from, Vector2 to)
        {
            yield return null;

            BeginTouch(1, from);
            yield return null;

            MoveTouch(1, to);
            yield return null;

            EndTouch(1, to);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InOrbit_TheDragTurnsTheCameraAndCutsNoWindow()
        {
            Assert.AreEqual(Mode.Orbit, bootstrap.Modes.Current);
            Assert.AreEqual(0f, bootstrap.OrbitCamera.Yaw, 0.0001f, "the rig starts unturned");

            Vector2 from = ScreenOf(DragFromWorld);
            Vector2 to = ScreenOf(DragToWorld);
            float expectedYaw = Mathf.Repeat((to.x - from.x) * DegreesPerPixel, 360f);
            Assert.Greater(expectedYaw, 1f, "the drag is a real turn, not a rounding error");

            yield return Drag(from, to);

            Assert.AreEqual(expectedYaw, bootstrap.OrbitCamera.Yaw, 0.2f, "CAM-01: Orbit routes the drag to the camera");
            Assert.AreEqual(0, bootstrap.Model.GetWindows(Wall3Id).Count, "and never to the window-draw gesture");
        }

        [UnityTest]
        public IEnumerator InWindowDraw_TheDragCutsAWindowAndLeavesTheCameraAlone()
        {
            bootstrap.Model.Select(Wall3Id);
            Assert.IsTrue(bootstrap.Modes.TrySet(Mode.WindowDraw), "a wall is selected, so WindowDraw is legal");

            yield return Drag(ScreenOf(DragFromWorld), ScreenOf(DragToWorld));

            // WIN-01 AC2: the same drag goes to window drawing instead of camera orbit.
            Assert.AreEqual(0f, bootstrap.OrbitCamera.Yaw, 0.0001f, "the camera never saw the drag");

            IReadOnlyList<WindowSpec> windows = bootstrap.Model.GetWindows(Wall3Id);
            Assert.AreEqual(1, windows.Count, "the drag reached the window-draw gesture");
            Assert.AreEqual(ExpectedRect.x, windows[0].rect.x, 0.02f, "rect x in wall-local metres");
            Assert.AreEqual(ExpectedRect.y, windows[0].rect.y, 0.02f, "rect y in wall-local metres");
            Assert.AreEqual(ExpectedRect.width, windows[0].rect.width, 0.02f, "rect width in wall-local metres");
            Assert.AreEqual(ExpectedRect.height, windows[0].rect.height, 0.02f, "rect height in wall-local metres");
        }

        [UnityTest]
        public IEnumerator InAr_TheDragGoesNowhereBecauseArOwnsThePose()
        {
            Assert.IsTrue(bootstrap.Modes.TrySet(Mode.Ar), "Ar is legal from Orbit (AD-013)");

            yield return Drag(ScreenOf(DragFromWorld), ScreenOf(DragToWorld));

            // AR-01 AC2: touch-drag camera input is ignored while AR drives the rig.
            Assert.AreEqual(0f, bootstrap.OrbitCamera.Yaw, 0.0001f, "the drag did not turn the camera");
            Assert.AreEqual(0f, bootstrap.OrbitCamera.Pitch, 0.0001f, "and did not tilt it either");
            Assert.AreEqual(0, bootstrap.Model.GetWindows(Wall3Id).Count, "and cut no window");
        }
    }
}
