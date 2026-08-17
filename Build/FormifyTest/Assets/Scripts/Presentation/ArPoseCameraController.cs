using System;
using Formify.Domain;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Formify.Presentation
{
    /// <summary>
    /// AR-01 — while <see cref="Mode.Ar"/> is current the device pose drives the camera rig: rotation comes
    /// straight from the pose, position comes from the pose clamped into the room interior so the camera can
    /// never walk through a wall. The synthetic room stays the room source; plane detection is out of scope.
    /// This controller only ever WRITES the rig transform — it never reads, consumes or blocks input, so
    /// InputRouter taps keep reaching SelectionController while AR is active (AR-01 AC5).
    /// </summary>
    public class ArPoseCameraController : MonoBehaviour
    {
        /// <summary>
        /// Pose seam: the controller never talks to AR tracking directly, so tests drive it with a stub and
        /// XR Simulation drives it through the default implementation.
        /// </summary>
        public interface IPoseProvider
        {
            bool TryGetPose(out Vector3 position, out Quaternion rotation);
        }

        private ModeManager _modes;
        private OrbitCameraController _orbitCamera;
        private Transform _cameraRig;

        // Generous default so an unconfigured controller is not pinned to the origin by a zero-sized Bounds
        // (same guard as OrbitCameraController).
        private Bounds _roomBounds = new Bounds(Vector3.zero, Vector3.one * 100f);

        private Quaternion _lastPoseRotation = Quaternion.identity;
        private bool _hasPose;

        private IPoseProvider _poseProvider = new ArFoundationPoseProvider();

        /// <summary>The pose source. Defaults to the AR Foundation pose; swap it to drive the rig without tracking.</summary>
        public IPoseProvider PoseProvider
        {
            get => _poseProvider;
            set => _poseProvider = value;
        }

        /// <summary>Availability seam (AR-01 AC4). Defaults to the real ARSession check.</summary>
        public Func<bool> AvailabilityCheck { get; set; } = DefaultAvailabilityCheck;

        public bool IsAvailable => AvailabilityCheck != null && AvailabilityCheck();

        /// <summary>Binds the mode source, the orbit camera that receives the exit handoff, and the rig to drive.</summary>
        public void Configure(ModeManager modes, OrbitCameraController orbitCamera, Transform cameraRig)
        {
            if (_modes != null) _modes.ModeChanged -= OnModeChanged;
            _modes = modes;
            if (_modes != null) _modes.ModeChanged += OnModeChanged;

            _orbitCamera = orbitCamera;
            _cameraRig = cameraRig;
        }

        /// <summary>Interior bounds the AR position is clamped into (RoomBootstrap.RoomBounds in the scene).</summary>
        public void ConfigureRoom(Bounds roomBounds) => _roomBounds = roomBounds;

        private void OnDestroy()
        {
            if (_modes != null) _modes.ModeChanged -= OnModeChanged;
            _modes = null;
        }

        private void LateUpdate()
        {
            if (_modes == null || _modes.Current != Mode.Ar) return;
            if (_poseProvider == null) return;
            if (!_poseProvider.TryGetPose(out Vector3 position, out Quaternion rotation)) return;

            _lastPoseRotation = rotation;
            _hasPose = true;

            if (_cameraRig == null) return;

            // Rotation always from the pose; position clamped, because AR pose can otherwise walk through a wall.
            _cameraRig.SetPositionAndRotation(_roomBounds.ClosestPoint(position), rotation);
        }

        // AR-01 AC3 — no snap on the way out: the orbit rig picks up exactly the orientation AR left behind.
        private void OnModeChanged(Mode previous, Mode current)
        {
            if (previous != Mode.Ar || !_hasPose || _orbitCamera == null) return;

            Vector3 euler = _lastPoseRotation.eulerAngles;
            // OrbitCameraController applies Quaternion.Euler(-Pitch, Yaw, 0) (positive Pitch = looking up),
            // so the pose's euler X enters negated or the handoff would mirror the view.
            _orbitCamera.SetRotation(euler.y, -Normalize180(euler.x));
        }

        private static float Normalize180(float degrees)
        {
            float wrapped = Mathf.Repeat(degrees, 360f);
            return wrapped > 180f ? wrapped - 360f : wrapped;
        }

        // "Past Unsupported" — anything from CheckingAvailability upwards means AR can run here. In the Editor
        // XR Simulation drives the session to SessionTracking, so it counts as available.
        private static bool DefaultAvailabilityCheck() => ARSession.state > ARSessionState.Unsupported;

        /// <summary>
        /// Real pose source: the XR Origin's camera, which AR Foundation's TrackedPoseDriver has already moved
        /// to the device pose for this frame. Only queried while the mode is Ar.
        /// </summary>
        private sealed class ArFoundationPoseProvider : IPoseProvider
        {
            private XROrigin _origin;

            public bool TryGetPose(out Vector3 position, out Quaternion rotation)
            {
                position = Vector3.zero;
                rotation = Quaternion.identity;

                if (ARSession.state != ARSessionState.SessionTracking) return false;

                if (_origin == null) _origin = UnityEngine.Object.FindFirstObjectByType<XROrigin>();
                if (_origin == null || _origin.Camera == null) return false;

                _origin.Camera.transform.GetPositionAndRotation(out position, out rotation);
                return true;
            }
        }
    }
}
