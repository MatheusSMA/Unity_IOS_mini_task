using Formify.Domain;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Formify.Presentation
{
    /// <summary>
    /// TOP-01/TOP-02 — the 2D plan. Entering flips the room camera to an orthographic top-down rig framed to
    /// the room (fit-to-room, TOP AC7), takes the ceiling out of BOTH rendering and physics (TOP AC2, AD-012)
    /// and leaves the transient state cancelled (TOP AC3). Exiting restores everything and hands the rig back
    /// to <see cref="OrbitCameraController"/> (TOP AC5).
    /// Selection and taps are untouched: SelectionController keeps working through the same camera (TOP AC4).
    /// </summary>
    public class TopDownController : MonoBehaviour
    {
        /// <summary>Batch mode can report a zero/NaN camera aspect before the first render (no real screen).</summary>
        private const float FallbackAspect = 16f / 9f;

        [SerializeField] private float fitMargin = 0.1f;

        /// <summary>Zoom limits as multipliers of the fit size (AD-016). Smaller size = zoomed IN.</summary>
        [SerializeField] private float minZoom = 0.5f;

        [SerializeField] private float maxZoom = 2.0f;

        /// <summary>Metres between the top of the room and the plan camera. Framing is orthographic, so this
        /// only has to keep the whole slab in front of the near plane.</summary>
        [SerializeField] private float heightAboveCeiling = 5f;

        private ModeManager _modes;
        private RoomModel _model;
        private OrbitCameraController _orbitCamera;
        private Camera _camera;
        private SurfaceView _ceilingView;
        private Bounds _roomBounds;

        private bool _active;
        private float _pinchDistance;
        private bool _restoreCaptured;
        private bool _originalOrthographic;
        private float _originalFieldOfView;
        private Vector3 _originalLocalPosition;
        private Quaternion _originalLocalRotation;

        /// <summary>
        /// True while the plan owns the camera. The MODE is the authority (TOP AC9 gates pinch on the mode,
        /// not on who called <see cref="EnterTopDown"/>); with no ModeManager wired it falls back to the last
        /// Enter/Exit call so a manually driven rig still behaves.
        /// </summary>
        public bool IsTopDown => _modes != null ? _modes.Current == Mode.TopDown : _active;

        /// <summary>
        /// The orthographic size that frames the whole room plus <c>fitMargin</c> — the widest the plan ever
        /// gets on entry, never further out (TOP AC7).
        /// </summary>
        public float FitOrthographicSize
        {
            get
            {
                float aspect = _camera != null ? _camera.aspect : 0f;
                if (aspect <= 0f || float.IsNaN(aspect)) aspect = FallbackAspect;

                // Looking straight down with Euler(90,0,0) puts world +Z on the camera's vertical axis and
                // world +X on its horizontal one. orthographicSize is the HALF height, so the width has to be
                // divided by the aspect before the two are compared.
                float halfDepth = _roomBounds.extents.z;
                float halfWidth = _roomBounds.extents.x;
                return Mathf.Max(halfDepth, halfWidth / aspect) * (1f + fitMargin);
            }
        }

        /// <summary>Binds everything this needs and subscribes to the mode side-effects. Safe to call again.</summary>
        public void Configure(ModeManager modes, RoomModel model, OrbitCameraController orbitCamera,
                              Camera roomCamera, SurfaceView ceilingView, Bounds roomBounds)
        {
            Unsubscribe();

            _modes = modes;
            _model = model;
            _orbitCamera = orbitCamera;
            _camera = roomCamera;
            _ceilingView = ceilingView;
            _roomBounds = roomBounds;

            if (_modes == null) return;
            _modes.ModeChanged += OnModeChanged;
            _modes.SelectionClearRequested += OnSelectionClearRequested;
        }

        /// <summary>
        /// Orthographic camera above the room centre looking straight down, ceiling out of the way.
        /// The selection clear is NOT duplicated here: it rides on ModeManager.SelectionClearRequested so the
        /// rule lives in one place, and the in-progress draw dies through WindowDrawController's own
        /// DrawCancelRequested subscription (TOP AC3). Both fire on TrySet(Mode.TopDown).
        /// </summary>
        public void EnterTopDown()
        {
            // Capture only on the not-active -> active edge, so a repeated Enter never records the plan's own
            // orthographic state as the thing to restore.
            if (!_active) CaptureCameraState();
            _active = true;

            SetCeilingEnabled(false);

            if (_camera == null) return;

            _camera.orthographic = true;
            _camera.orthographicSize = FitOrthographicSize;
            _camera.transform.SetPositionAndRotation(
                new Vector3(_roomBounds.center.x, _roomBounds.max.y + heightAboveCeiling, _roomBounds.center.z),
                Quaternion.Euler(90f, 0f, 0f));
        }

        /// <summary>Restores the ceiling and the perspective camera, then puts the rig back at the room centre.</summary>
        public void ExitToOrbit()
        {
            SetCeilingEnabled(true);

            if (_camera != null && _restoreCaptured)
            {
                _camera.orthographic = _originalOrthographic;
                _camera.fieldOfView = _originalFieldOfView;
                // Local, not world: the camera may be a child of the orbit rig.
                _camera.transform.localPosition = _originalLocalPosition;
                _camera.transform.localRotation = _originalLocalRotation;
            }

            _active = false;

            // Last, so the rig owns the final position whether or not it is the camera's own transform (TOP AC5).
            if (_orbitCamera != null) _orbitCamera.ResetToRoomCentre();
        }

        /// <summary>
        /// Plan pinch zoom (TOP AC9). <paramref name="pinchDelta"/> is expressed in FIT units: +1 zooms all the
        /// way in by one full fit size, -1 zooms out by the same amount — the caller converts its finger-distance
        /// delta into that unit. Zooming IN means a SMALLER orthographic size, hence the subtraction. The result
        /// is clamped to [minZoom, maxZoom] x <see cref="FitOrthographicSize"/> (AD-016).
        /// Does nothing outside TopDown: pinch has no effect in the 3D views.
        /// </summary>
        public void ApplyPinch(float pinchDelta)
        {
            if (!IsTopDown || _camera == null) return;

            float fit = FitOrthographicSize;
            _camera.orthographicSize =
                Mathf.Clamp(_camera.orthographicSize - pinchDelta * fit, minZoom * fit, maxZoom * fit);
        }

        /// <summary>
        /// Reads the two-finger pinch itself (TOP AC9). InputRouter deliberately tracks only the primary touch
        /// (EDGE-01), so the second finger would never reach a controller through it. The gesture is converted
        /// into the fit units <see cref="ApplyPinch"/> expects: a spread of one screen height is one fit unit.
        /// </summary>
        private void Update()
        {
            if (!IsTopDown || !EnhancedTouchSupport.enabled) return;

            var touches = Touch.activeTouches;
            if (touches.Count < 2)
            {
                _pinchDistance = 0f;
                return;
            }

            float distance = Vector2.Distance(touches[0].screenPosition, touches[1].screenPosition);
            if (_pinchDistance > 0f)
            {
                float reference = Screen.height > 0 ? Screen.height : 1080f;
                ApplyPinch((distance - _pinchDistance) / reference);
            }

            _pinchDistance = distance;
        }

        private void OnModeChanged(Mode previous, Mode current)
        {
            if (current == Mode.TopDown) EnterTopDown();
            else if (previous == Mode.TopDown) ExitToOrbit();
        }

        private void OnSelectionClearRequested()
        {
            if (_model != null) _model.ClearSelection();
        }

        /// <summary>
        /// AD-012: the collider is the half everyone forgets — hiding the renderer alone leaves an invisible
        /// ceiling directly under the plan camera that swallows every tap.
        /// </summary>
        private void SetCeilingEnabled(bool enabled)
        {
            if (_ceilingView == null) return;

            MeshRenderer meshRenderer = _ceilingView.GetComponent<MeshRenderer>();
            if (meshRenderer != null) meshRenderer.enabled = enabled;

            MeshCollider meshCollider = _ceilingView.GetComponent<MeshCollider>();
            if (meshCollider != null) meshCollider.enabled = enabled;
        }

        private void CaptureCameraState()
        {
            if (_camera == null) return;

            _originalOrthographic = _camera.orthographic;
            _originalFieldOfView = _camera.fieldOfView;
            _originalLocalPosition = _camera.transform.localPosition;
            _originalLocalRotation = _camera.transform.localRotation;
            _restoreCaptured = true;
        }

        private void Unsubscribe()
        {
            if (_modes == null) return;
            _modes.ModeChanged -= OnModeChanged;
            _modes.SelectionClearRequested -= OnSelectionClearRequested;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _modes = null;
        }
    }
}
