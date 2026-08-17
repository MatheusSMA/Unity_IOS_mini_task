using UnityEngine;

namespace Formify.Presentation
{
    /// <summary>
    /// First-person look-around rig (CAM-01, CAM-02): one-finger drag rotates, yaw is free, pitch is clamped,
    /// there is no zoom. The rig sits at the room centre at eye height and never translates; whoever owns the
    /// wiring (RoomBootstrap / InputRouter) feeds <see cref="OnDrag"/>.
    /// </summary>
    public class OrbitCameraController : MonoBehaviour
    {
        [SerializeField] private float minPitch = -60f;
        [SerializeField] private float maxPitch = 60f;
        [SerializeField] private float eyeHeight = 1.6f;
        [SerializeField] private float degreesPerPixel = 0.2f;
        [SerializeField] private RoomBootstrap roomBootstrap;

        private Vector3 _roomCentre;
        // Generous default so an unconfigured rig is not pinned to the origin by a zero-sized Bounds.
        private Bounds _roomBounds = new Bounds(Vector3.zero, Vector3.one * 100f);

        public float Yaw { get; private set; }

        public float Pitch { get; private set; }

        private void Start()
        {
            if (roomBootstrap != null) ConfigureRoom(roomBootstrap.RoomCentre, roomBootstrap.RoomBounds);
        }

        /// <summary>Room source when there is no RoomBootstrap (tests, manual wiring). Addition to the contract.</summary>
        public void ConfigureRoom(Vector3 centre, Bounds bounds)
        {
            _roomCentre = centre;
            _roomBounds = bounds;
            ResetToRoomCentre();
        }

        public void OnDrag(Vector2 delta)
        {
            Yaw = Mathf.Repeat(Yaw + delta.x * degreesPerPixel, 360f);
            Pitch = Mathf.Clamp(Pitch + delta.y * degreesPerPixel, minPitch, maxPitch);
            ApplyRotation();
        }

        /// <summary>AR to touch handoff (AR-01 AC3): applies the pose and snaps nothing else.</summary>
        public void SetRotation(float yaw, float pitch)
        {
            Yaw = Mathf.Repeat(yaw, 360f);
            Pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            ApplyRotation();
        }

        /// <summary>TopDown to 3D return (TOP AC5). The eye is clamped on every set, so while Orbit owns the
        /// rig the camera can never end up outside the room (AD-012).</summary>
        public void ResetToRoomCentre()
        {
            transform.position = _roomBounds.ClosestPoint(_roomCentre + Vector3.up * eyeHeight);
        }

        // Sign convention: positive Pitch means "looking up", so it enters the euler X as -Pitch. An upward
        // drag (delta.y > 0) raises Pitch and tilts the rig up.
        private void ApplyRotation() => transform.rotation = Quaternion.Euler(-Pitch, Yaw, 0f);
    }
}
