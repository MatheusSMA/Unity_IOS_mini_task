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
    /// B5: the mode still flips instantly — only the camera TRANSFORM flies between the two poses, and the
    /// plan sits a notch further back than the bare fit.
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

        /// <summary>
        /// B5 — the plan opens one notch wider than the bare fit so the room reads as a plan with air around
        /// it instead of pressed against the viewport edge. Multiplies the fit size, and is clamped into the
        /// same band pinch lives in (AD-016) so the view can never open outside its own zoom range.
        /// </summary>
        [SerializeField] private float planZoomOut = 1.25f;

        /// <summary>B5 — seconds the camera takes to fly between the 3D pose and the plan pose, both ways.
        /// 0 restores the plain snap.</summary>
        [SerializeField] private float transitionSeconds = 0.35f;

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

        private bool _transitioning;
        private bool _transitionToPlan;
        private float _transitionElapsed;
        private Vector3 _fromPosition;
        private Quaternion _fromRotation;
        private Vector3 _toPosition;
        private Quaternion _toRotation;

        /// <summary>
        /// True while the plan owns the camera. The MODE is the authority (TOP AC9 gates pinch on the mode,
        /// not on who called <see cref="EnterTopDown"/>); with no ModeManager wired it falls back to the last
        /// Enter/Exit call so a manually driven rig still behaves.
        /// </summary>
        public bool IsTopDown => _modes != null ? _modes.Current == Mode.TopDown : _active;

        /// <summary>
        /// B5 — true while the camera is still flying between the two poses. The mode has ALREADY flipped and
        /// every ModeChanged side effect has already run; only the transform is catching up.
        /// </summary>
        public bool IsTransitioning => _transitioning;

        /// <summary>
        /// The orthographic size that frames the whole room plus <c>fitMargin</c> — the bare fit. The plan
        /// opens further back than this (see <see cref="PlanOrthographicSize"/>) and the pinch clamps are
        /// measured against it (AD-016).
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

        /// <summary>B5 — the size the plan actually opens at: the fit pushed back by <c>planZoomOut</c>, kept
        /// honest against the pinch clamps so entry can never start outside [minZoom, maxZoom] (AD-016).</summary>
        public float PlanOrthographicSize
        {
            get
            {
                float fit = FitOrthographicSize;
                return Mathf.Clamp(fit * planZoomOut, minZoom * fit, maxZoom * fit);
            }
        }

        /// <summary>Straight above the room centre, high enough to keep the slab in front of the near plane.</summary>
        private Vector3 PlanPosition => new Vector3(
            _roomBounds.center.x, _roomBounds.max.y + heightAboveCeiling, _roomBounds.center.z);

        private static readonly Quaternion PlanRotation = Quaternion.Euler(90f, 0f, 0f);

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
        /// Orthographic camera above the room centre looking straight down, ceiling out of the way. The
        /// ceiling goes on the instant the mode does; the camera flies there (B5).
        /// The selection clear is NOT duplicated here: it rides on ModeManager.SelectionClearRequested so the
        /// rule lives in one place, and the in-progress draw dies through WindowDrawController's own
        /// DrawCancelRequested subscription (TOP AC3). Both fire on TrySet(Mode.TopDown).
        /// </summary>
        public void EnterTopDown()
        {
            // Capture only on the not-active -> active edge, so a repeated Enter never records the plan's own
            // orthographic state as the thing to restore. Mid-flight is not an edge either: on the way back
            // out the camera is parked in mid-air, and capturing THAT would make it the 3D pose to restore.
            if (!_active && !_transitioning) CaptureCameraState();
            _active = true;

            SetCeilingEnabled(false);

            BeginTransition(true);
        }

        /// <summary>Restores the ceiling and the perspective camera, then flies the rig back to the room centre.</summary>
        public void ExitToOrbit()
        {
            SetCeilingEnabled(true);
            _active = false;

            BeginTransition(false);
        }

        /// <summary>
        /// Plan pinch zoom (TOP AC9). <paramref name="pinchDelta"/> is expressed in FIT units: +1 zooms all the
        /// way in by one full fit size, -1 zooms out by the same amount — the caller converts its finger-distance
        /// delta into that unit. Zooming IN means a SMALLER orthographic size, hence the subtraction. The result
        /// is clamped to [minZoom, maxZoom] x <see cref="FitOrthographicSize"/> (AD-016).
        /// Does nothing outside TopDown, and nothing while the camera is in the air (B5): a pinch landing
        /// mid-transition would be overwritten by the landing snap anyway.
        /// </summary>
        public void ApplyPinch(float pinchDelta)
        {
            if (!IsTopDown || _transitioning || _camera == null) return;

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

        /// <summary>
        /// B5 — drives the flight. LateUpdate, after every input handler has had its Update: the flight is the
        /// last writer of the pose, so nothing can tear it even if a source is added that forgets the lock.
        /// </summary>
        private void LateUpdate()
        {
            if (!_transitioning) return;

            if (_camera == null)
            {
                EndTransition();
                return;
            }

            _transitionElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_transitionElapsed / transitionSeconds);
            if (t >= 1f)
            {
                EndTransition();
                // Re-applied rather than left on the last lerp step, so the landing is EXACTLY the pose this
                // used to jump to — no interpolation residue in the final transform.
                Snap(_transitionToPlan);
                return;
            }

            float eased = Mathf.SmoothStep(0f, 1f, t);
            _camera.transform.SetPositionAndRotation(
                Vector3.Lerp(_fromPosition, _toPosition, eased),
                Quaternion.Slerp(_fromRotation, _toRotation, eased));
        }

        /// <summary>
        /// B5 — reads where the camera is now, applies the snap this mode change used to do outright, reads
        /// THAT back as the target and rewinds. Two things fall out of doing it in that order: the flight
        /// starts wherever the camera actually is (a switch arriving mid-flight retargets from mid-air instead
        /// of fighting or restarting) and the target is the real pose rather than a second derivation of it.
        /// </summary>
        private void BeginTransition(bool toPlan)
        {
            if (_camera == null || transitionSeconds <= 0f)
            {
                EndTransition();
                Snap(toPlan);
                return;
            }

            _camera.transform.GetPositionAndRotation(out _fromPosition, out _fromRotation);
            Snap(toPlan);
            _camera.transform.GetPositionAndRotation(out _toPosition, out _toRotation);

            // The flight is flown in the 3D projection both ways: an orthographic camera at eye height inside
            // the room shows nothing readable, so the plan's projection arrives with the plan pose.
            if (_restoreCaptured)
            {
                _camera.orthographic = _originalOrthographic;
                _camera.fieldOfView = _originalFieldOfView;
            }
            _camera.transform.SetPositionAndRotation(_fromPosition, _fromRotation);

            _transitionToPlan = toPlan;
            _transitionElapsed = 0f;
            _transitioning = true;

            // Nothing else may drive the camera in the air: a drag and the flight writing the same transform
            // in one frame is exactly how the pose tears.
            if (_orbitCamera != null) _orbitCamera.InputLocked = true;
        }

        private void EndTransition()
        {
            _transitioning = false;
            if (_orbitCamera != null) _orbitCamera.InputLocked = false;
        }

        private void Snap(bool toPlan)
        {
            if (toPlan) SnapToPlan();
            else SnapToOrbit();
        }

        private void SnapToPlan()
        {
            if (_camera == null) return;

            _camera.orthographic = true;
            _camera.orthographicSize = PlanOrthographicSize;
            _camera.transform.SetPositionAndRotation(PlanPosition, PlanRotation);
        }

        private void SnapToOrbit()
        {
            if (_camera != null && _restoreCaptured)
            {
                _camera.orthographic = _originalOrthographic;
                _camera.fieldOfView = _originalFieldOfView;
                // Local, not world: the camera may be a child of the orbit rig.
                _camera.transform.localPosition = _originalLocalPosition;
                _camera.transform.localRotation = _originalLocalRotation;
            }

            // Last, so the rig owns the final position whether or not it is the camera's own transform (TOP AC5).
            if (_orbitCamera != null) _orbitCamera.ResetToRoomCentre();
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
            // Dying mid-flight must not leave the orbit rig deaf to input for the rest of the session.
            EndTransition();
            Unsubscribe();
            _modes = null;
        }
    }
}
