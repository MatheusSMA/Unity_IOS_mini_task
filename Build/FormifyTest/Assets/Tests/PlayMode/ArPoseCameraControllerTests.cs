using System.Collections;
using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Formify.Tests.PlayMode
{
    /// <summary>
    /// AR-01 — pose-driven rotation, position clamped to the room, the no-snap handoff on the way out, and
    /// total silence while the mode is not Ar. The pose comes from a stub, so nothing here needs real tracking.
    /// </summary>
    [TestFixture]
    public class ArPoseCameraControllerTests
    {
        // Room interior: x in [-3, 3], y in [0, 2.8], z in [-2, 2].
        private static readonly Bounds RoomBounds = new Bounds(new Vector3(0f, 1.4f, 0f), new Vector3(6f, 2.8f, 4f));

        // Pitched down 20 degrees, turned 130 degrees. OrbitCameraController's Pitch is positive-is-up, so the
        // handoff must land on Pitch = -20.
        private static readonly Quaternion PoseRotation = Quaternion.Euler(20f, 130f, 0f);
        private const float ExpectedYaw = 130f;
        private const float ExpectedPitch = -20f;

        private sealed class StubPoseProvider : ArPoseCameraController.IPoseProvider
        {
            public Vector3 Position = Vector3.zero;
            public Quaternion Rotation = Quaternion.identity;
            public bool HasPose = true;
            public int Calls;

            public bool TryGetPose(out Vector3 position, out Quaternion rotation)
            {
                Calls++;
                position = Position;
                rotation = Rotation;
                return HasPose;
            }
        }

        private GameObject _rigGo;
        private GameObject _orbitGo;
        private GameObject _controllerGo;
        private OrbitCameraController _orbit;
        private ArPoseCameraController _controller;
        private ModeManager _modes;
        private StubPoseProvider _pose;

        [SetUp]
        public void SetUp()
        {
            _rigGo = new GameObject("CameraRig");
            _orbitGo = new GameObject("OrbitCamera");
            _orbit = _orbitGo.AddComponent<OrbitCameraController>();

            _controllerGo = new GameObject("ArPoseCamera");
            _controller = _controllerGo.AddComponent<ArPoseCameraController>();

            _modes = new ModeManager(() => false);
            _pose = new StubPoseProvider();

            _controller.Configure(_modes, _orbit, _rigGo.transform);
            _controller.ConfigureRoom(RoomBounds);
            _controller.PoseProvider = _pose;
        }

        [TearDown]
        public void TearDown()
        {
            if (_controllerGo != null) Object.DestroyImmediate(_controllerGo);
            if (_orbitGo != null) Object.DestroyImmediate(_orbitGo);
            if (_rigGo != null) Object.DestroyImmediate(_rigGo);

            _controllerGo = null;
            _orbitGo = null;
            _rigGo = null;
            _controller = null;
            _orbit = null;
            _modes = null;
            _pose = null;
        }

        [UnityTest]
        public IEnumerator InAr_RigRotationEqualsThePose()
        {
            Vector3 inside = new Vector3(0.5f, 1.6f, -0.5f);
            _pose.Position = inside;
            _pose.Rotation = PoseRotation;

            Assert.IsTrue(_modes.TrySet(Mode.Ar), "Ar is legal from Orbit (AD-013)");

            yield return null;

            Assert.Less(Quaternion.Angle(_rigGo.transform.rotation, PoseRotation), 0.01f,
                "rotation always comes straight from the pose");
            Assert.Less(Vector3.Distance(_rigGo.transform.position, inside), 0.0001f,
                "a pose already inside the room is not moved by the clamp");
        }

        [UnityTest]
        public IEnumerator InAr_PoseOutsideTheRoom_IsClampedToTheClosestInteriorPoint()
        {
            // 10 m down +X and 5 m up +Z, both far outside; the closest interior point is the corner-ish (3, 1.4, 2).
            _pose.Position = new Vector3(10f, 1.4f, 5f);
            _pose.Rotation = PoseRotation;

            Assert.IsTrue(_modes.TrySet(Mode.Ar));

            yield return null;

            Vector3 expected = new Vector3(3f, 1.4f, 2f);
            Assert.Less(Vector3.Distance(_rigGo.transform.position, expected), 0.0001f,
                "position is Bounds.ClosestPoint of the pose, so the camera cannot walk through a wall");
            Assert.IsTrue(RoomBounds.Contains(_rigGo.transform.position), "the clamped point is inside the room");
            Assert.Less(Quaternion.Angle(_rigGo.transform.rotation, PoseRotation), 0.01f,
                "clamping the position never touches the rotation");
        }

        [UnityTest]
        public IEnumerator LeavingAr_HandsTheFinalOrientationToTheOrbitCamera()
        {
            _pose.Position = new Vector3(0f, 1.6f, 0f);
            _pose.Rotation = PoseRotation;

            Assert.IsTrue(_modes.TrySet(Mode.Ar));

            yield return null;

            Assert.AreEqual(0f, _orbit.Yaw, 0.01f, "the orbit camera has not been touched yet");

            Assert.IsTrue(_modes.TrySet(Mode.Orbit));

            // AR-01 AC3: the orbit rig resumes from the last pose instead of snapping back to its old view.
            Assert.AreEqual(ExpectedYaw, _orbit.Yaw, 0.01f);
            Assert.AreEqual(ExpectedPitch, _orbit.Pitch, 0.01f);
            Assert.Less(Quaternion.Angle(_orbitGo.transform.rotation, PoseRotation), 0.05f,
                "the handed-over yaw/pitch reproduce the pose orientation, so there is no visible snap");
        }

        [UnityTest]
        public IEnumerator InOrbit_APoseNeverMovesTheRig()
        {
            _pose.Position = new Vector3(10f, 9f, 8f);
            _pose.Rotation = PoseRotation;

            Assert.AreEqual(Mode.Orbit, _modes.Current);

            yield return null;

            Assert.Less(Vector3.Distance(_rigGo.transform.position, Vector3.zero), 0.0001f, "rig position untouched");
            Assert.Less(Quaternion.Angle(_rigGo.transform.rotation, Quaternion.identity), 0.0001f, "rig rotation untouched");
            Assert.AreEqual(0, _pose.Calls, "outside Ar the controller does not even ask for a pose");
        }
    }
}
