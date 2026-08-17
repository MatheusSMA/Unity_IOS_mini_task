using System.Collections;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Formify.Tests.PlayMode
{
    public class OrbitCameraControllerTests
    {
        // The tuning fields are serialized and private; these mirror the spec defaults the tests assert against.
        private const float DegreesPerPixel = 0.2f;
        private const float EyeHeight = 1.6f;
        private const float MinPitch = -60f;
        private const float MaxPitch = 60f;

        private static readonly Vector3 RoomCentre = Vector3.zero;

        // 6 x 2.8 x 4 interior standing on the floor plane, so the centre is at half the room height.
        private static readonly Bounds RoomBounds =
            new Bounds(new Vector3(0f, 1.4f, 0f), new Vector3(6f, 2.8f, 4f));

        private GameObject _rig;
        private OrbitCameraController _controller;

        [SetUp]
        public void SetUp()
        {
            _rig = new GameObject("OrbitRig");
            _controller = _rig.AddComponent<OrbitCameraController>();
            _controller.ConfigureRoom(RoomCentre, RoomBounds);
        }

        [TearDown]
        public void TearDown()
        {
            if (_rig != null) Object.DestroyImmediate(_rig);
        }

        private static Vector2 HorizontalDrag(float degrees) => new Vector2(degrees / DegreesPerPixel, 0f);

        private static Vector2 VerticalDrag(float degrees) => new Vector2(0f, degrees / DegreesPerPixel);

        [UnityTest]
        public IEnumerator Yaw_is_free_and_wraps_past_a_full_turn()
        {
            yield return null;

            _controller.OnDrag(HorizontalDrag(400f));
            Assert.AreEqual(40f, _controller.Yaw, 0.01f);

            _controller.OnDrag(HorizontalDrag(400f));
            Assert.AreEqual(80f, _controller.Yaw, 0.01f);
            Assert.That(_controller.Yaw, Is.InRange(0f, 360f));
        }

        [UnityTest]
        public IEnumerator Pitch_clamps_at_both_ends()
        {
            yield return null;

            _controller.OnDrag(VerticalDrag(1000f));
            Assert.AreEqual(MaxPitch, _controller.Pitch, 0.0001f);
            // Sign convention: an upward drag looks up.
            Assert.Greater(_rig.transform.forward.y, 0f);

            _controller.OnDrag(VerticalDrag(-2000f));
            Assert.AreEqual(MinPitch, _controller.Pitch, 0.0001f);
            Assert.Less(_rig.transform.forward.y, 0f);
        }

        [UnityTest]
        public IEnumerator Position_stays_in_the_room_at_eye_height_after_drags()
        {
            yield return null;

            _controller.OnDrag(new Vector2(1500f, 900f));
            _controller.OnDrag(new Vector2(-4000f, -3000f));
            _controller.OnDrag(new Vector2(250f, 120f));

            Vector3 position = _rig.transform.position;
            Assert.IsTrue(RoomBounds.Contains(position), $"{position} left the room");
            Assert.AreEqual(EyeHeight, position.y, 0.0001f);
            Assert.AreEqual(RoomCentre.x, position.x, 0.0001f);
            Assert.AreEqual(RoomCentre.z, position.z, 0.0001f);
        }

        [UnityTest]
        public IEnumerator SetRotation_applies_exactly_and_still_clamps_pitch()
        {
            yield return null;

            _controller.SetRotation(123f, 45f);
            Assert.AreEqual(123f, _controller.Yaw, 0.0001f);
            Assert.AreEqual(45f, _controller.Pitch, 0.0001f);
            Assert.Less(Quaternion.Angle(_rig.transform.rotation, Quaternion.Euler(-45f, 123f, 0f)), 0.01f);

            _controller.SetRotation(10f, 500f);
            Assert.AreEqual(MaxPitch, _controller.Pitch, 0.0001f);
            Assert.AreEqual(10f, _controller.Yaw, 0.0001f);
        }

        [UnityTest]
        public IEnumerator ResetToRoomCentre_restores_the_eye_position()
        {
            yield return null;

            _rig.transform.position = new Vector3(100f, 100f, 100f);
            _controller.ResetToRoomCentre();

            Assert.AreEqual(RoomCentre.x, _rig.transform.position.x, 0.0001f);
            Assert.AreEqual(EyeHeight, _rig.transform.position.y, 0.0001f);
            Assert.AreEqual(RoomCentre.z, _rig.transform.position.z, 0.0001f);
        }

        [UnityTest]
        public IEnumerator Eye_height_is_clamped_into_a_low_room()
        {
            yield return null;

            // 1 m tall interior: the 1.6 m eye must be pulled down to the ceiling.
            _controller.ConfigureRoom(RoomCentre, new Bounds(new Vector3(0f, 0.5f, 0f), new Vector3(6f, 1f, 4f)));

            Assert.AreEqual(1f, _rig.transform.position.y, 0.0001f);
        }
    }
}
