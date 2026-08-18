using System.Collections.Generic;
using Formify.Domain;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace Formify.Presentation
{
    /// <summary>
    /// Builds the synthetic room on Awake (ROOM-01) and owns the references the controllers share —
    /// plain serialized fields and properties, no DI framework (AD-002).
    /// </summary>
    public class RoomBootstrap : MonoBehaviour
    {
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        // The art kit's RightRail block, in its reference pixels (HUD-01 AC2).
        private const float RailWidth = 264f;
        private const float RailInset = 14f;
        private static readonly Vector2 RailButtonSize = new Vector2(212f, 46f);
        private static readonly Vector2 ReadoutPosition = new Vector2(8f, -404f);
        private static readonly Vector2 ReadoutSize = new Vector2(250f, 92f);

        [SerializeField] private Vector2 roomSize = new Vector2(6f, 4f);
        [SerializeField] private float height = 2.8f;
        [SerializeField] private float thickness = 0.15f;

        /// <summary>Assigned material wins; a runtime URP Lit is created when this is left empty.</summary>
        [SerializeField] private Material surfaceMaterial;

        [SerializeField] private Camera roomCamera;

        /// <summary>Off in tests that want the bare room without controllers or UI.</summary>
        [SerializeField] private bool buildRuntimeComposition = true;

        private readonly List<SurfaceView> _views = new List<SurfaceView>();

        private Material _runtimeMaterial;
        private Vector2 _dragPosition;
        private GameObject _arSessionRoot;

        public RoomModel Model { get; private set; }

        public ModeManager Modes { get; private set; }

        public IReadOnlyList<SurfaceView> SurfaceViews => _views;

        public SurfaceView CeilingView { get; private set; }

        public InputRouter Input { get; private set; }

        public OrbitCameraController OrbitCamera { get; private set; }

        public SelectionController Selection { get; private set; }

        public SurfaceListPanel ListPanel { get; private set; }

        /// <summary>The art kit's right-hand action rail. Null until <see cref="Compose"/> has run.</summary>
        public RectTransform Rail { get; private set; }

        public WindowModeButton WindowMode { get; private set; }

        public HudReadout Readout { get; private set; }

        public HudHintPill HintPill { get; private set; }

        public WindowDrawController WindowDraw { get; private set; }

        public WindowViewFactory Windows { get; private set; }

        public ArPoseCameraController ArCamera { get; private set; }

        public TopDownController TopDown { get; private set; }

        public Camera RoomCamera => roomCamera != null ? roomCamera : Camera.main;

        /// <summary>The footprint is generated centred on the origin, so its centre at floor level is the origin.</summary>
        public Vector3 RoomCentre => Vector3.zero;

        public Bounds RoomBounds =>
            new Bounds(RoomCentre + Vector3.up * (height * 0.5f), new Vector3(roomSize.x, height, roomSize.y));

        private void Awake()
        {
            RoomDefinition room = RoomBuilder.Build(Footprint(), height, thickness);
            Model = new RoomModel(room.surfaces);
            Modes = new ModeManager(IsWallSelected);

            Material material = ResolveMaterial();

            for (int i = 0; i < room.surfaces.Count; i++)
            {
                SurfaceDefinition surface = room.surfaces[i];

                GameObject go = new GameObject(surface.name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
                go.transform.SetParent(transform, false);
                go.GetComponent<MeshRenderer>().sharedMaterial = material;

                SurfaceView view = go.AddComponent<SurfaceView>();
                view.Initialize(surface, Model);

                _views.Add(view);
                if (surface.kind == SurfaceKind.Ceiling) CeilingView = view;
            }

            if (buildRuntimeComposition) Compose();
        }

        private void OnDestroy()
        {
            if (Input != null)
            {
                Input.Tapped -= OnTapped;
                Input.DragStart -= OnDragStart;
                Input.DragDelta -= OnDragDelta;
                Input.DragEnd -= OnDragEnd;
            }

            if (Modes != null)
            {
                Modes.ArSessionStartRequested -= StartArSession;
                Modes.ArSessionEndRequested -= StopArSession;
            }

            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
        }

        /// <summary>
        /// Creates and wires the runtime object graph: one input doorway, the orbit camera, the tap selection
        /// and the UI. Tests that only need the room switch it off through <see cref="buildRuntimeComposition"/>.
        /// </summary>
        private void Compose()
        {
            Input = gameObject.AddComponent<InputRouter>();

            Camera camera = RoomCamera;
            if (camera != null)
            {
                OrbitCamera = camera.gameObject.AddComponent<OrbitCameraController>();
                OrbitCamera.ConfigureRoom(RoomCentre, RoomBounds);

                Selection = gameObject.AddComponent<SelectionController>();
                Selection.Configure(Model, Modes, camera);
            }

            ListPanel = gameObject.AddComponent<SurfaceListPanel>();
            // AD-015: while a window is being drawn the target wall is locked, and that has to hold for a tap on
            // the list exactly as it holds for a tap on the wall itself.
            ListPanel.Configure(Model, () => Modes == null || Modes.Current != Mode.WindowDraw);

            Rail = BuildRightRail(ListPanel.Canvas);

            HudButton windowModeHud = RailButton("BtnWindowMode", "icon_window", "Window mode", -12f, true);
            WindowModeButton windowMode = windowModeHud.gameObject.AddComponent<WindowModeButton>();
            windowMode.StateDot = windowModeHud.Dot;
            windowMode.Configure(Model, Modes);
            // The button paints itself from its own state; only the click still has to be wired by hand, because
            // WindowModeButton (unlike Clear and AR) does not wire its own onClick in Configure.
            windowModeHud.ActiveSource = () => windowMode.IsActive;
            windowModeHud.Button.onClick.AddListener(windowMode.OnClick);
            WindowMode = windowMode;

            HudButton clearHud = RailButton("BtnClear", "icon_trash", "Clear", -68f, false);
            clearHud.UseNeutralPalette = true;
            clearHud.Apply();
            clearHud.gameObject.AddComponent<ClearButton>().Configure(Model, Modes);

            RailDivider(-124f);

            if (camera != null)
            {
                WindowDraw = gameObject.AddComponent<WindowDrawController>();
                WindowDraw.Configure(Model, Modes, camera);
            }

            Windows = gameObject.AddComponent<WindowViewFactory>();
            Windows.Configure(Model, Model.GetSurface, ListPanel.Canvas);

            if (camera != null)
            {
                ArCamera = gameObject.AddComponent<ArPoseCameraController>();
                ArCamera.Configure(Modes, OrbitCamera, camera.transform);
                ArCamera.ConfigureRoom(RoomBounds);
                Modes.ArSessionStartRequested += StartArSession;
                Modes.ArSessionEndRequested += StopArSession;

                TopDown = gameObject.AddComponent<TopDownController>();
                TopDown.Configure(Modes, Model, OrbitCamera, camera, CeilingView, RoomBounds);
            }

            HudButton arHud = RailButton("BtnAR", "icon_ar", "View in AR", -132f, false);
            // Configure wires its own onClick; adding it here too would fire OnClick twice and undo the toggle.
            ArToggleButton arToggle = arHud.gameObject.AddComponent<ArToggleButton>();
            arToggle.Configure(Modes, ArCamera);
            arHud.ActiveSource = () => Modes != null && Modes.Current == Mode.Ar;

            gameObject.AddComponent<ViewSwitchButtons>().Configure(Modes, ListPanel.Canvas);

            Readout = HudReadout.Create(ListPanel.Canvas.transform, ReadoutPosition, ReadoutSize);
            Readout.Configure(Model);

            HintPill = HudHintPill.Create(ListPanel.Canvas.transform);
            HintPill.Configure(Modes);

            // Last, so it covers the HUD. Raycast Target is off inside AddScanlines (HUD-01 AC4).
            HudTheme.AddScanlines(ListPanel.Canvas);

            Input.Tapped += OnTapped;
            Input.DragStart += OnDragStart;
            Input.DragDelta += OnDragDelta;
            Input.DragEnd += OnDragEnd;
        }

        private void OnTapped(Vector2 screenPosition)
        {
            if (Selection != null) Selection.OnTap(screenPosition);
        }

        /// <summary>
        /// AR-01: without a session and an XROrigin nothing produces a device pose, so AR mode would be inert.
        /// The rig is created on first entry and only enabled while the mode is Ar; the tracking camera does not
        /// render (the synthetic room stays the world, AR only drives the pose).
        /// </summary>
        private void StartArSession()
        {
            if (_arSessionRoot == null) _arSessionRoot = CreateArRig();
            _arSessionRoot.SetActive(true);
        }

        private void StopArSession()
        {
            if (_arSessionRoot != null) _arSessionRoot.SetActive(false);
        }

        private GameObject CreateArRig()
        {
            var root = new GameObject("AR Rig");
            root.transform.SetParent(transform, false);
            root.SetActive(false);

            var session = new GameObject("AR Session", typeof(ARSession), typeof(ARInputManager));
            session.transform.SetParent(root.transform, false);

            var originGo = new GameObject("XR Origin");
            originGo.transform.SetParent(root.transform, false);
            var origin = originGo.AddComponent<XROrigin>();

            var offset = new GameObject("Camera Offset");
            offset.transform.SetParent(originGo.transform, false);

            var trackingCameraGo = new GameObject("AR Tracking Camera", typeof(Camera));
            trackingCameraGo.transform.SetParent(offset.transform, false);
            var trackingCamera = trackingCameraGo.GetComponent<Camera>();
            trackingCamera.enabled = false;

            var driver = trackingCameraGo.AddComponent<TrackedPoseDriver>();
            driver.positionInput = new InputActionProperty(PoseAction("AR Position", "Vector3",
                "<HandheldARInputDevice>/devicePosition", "<XRHMD>/centerEyePosition"));
            driver.rotationInput = new InputActionProperty(PoseAction("AR Rotation", "Quaternion",
                "<HandheldARInputDevice>/deviceRotation", "<XRHMD>/centerEyeRotation"));

            origin.CameraFloorOffsetObject = offset;
            origin.Camera = trackingCamera;
            return root;
        }

        private static InputAction PoseAction(string name, string controlType, string primaryBinding, string fallbackBinding)
        {
            var action = new InputAction(name, InputActionType.Value, primaryBinding, expectedControlType: controlType);
            action.AddBinding(fallbackBinding);
            return action;
        }

        private void OnDragStart(Vector2 screenPosition)
        {
            _dragPosition = screenPosition;
            if (WindowDraw != null) WindowDraw.OnDragStart(screenPosition);
        }

        /// <summary>Only Orbit drives the camera from drags: AR owns the pose and WindowDraw owns the gesture.</summary>
        private void OnDragDelta(Vector2 delta)
        {
            // InputRouter reports deltas; WindowDrawController wants the live position, so keep the running sum.
            _dragPosition += delta;

            if (WindowDraw != null && Modes.Current == Mode.WindowDraw) WindowDraw.OnDragMove(_dragPosition);
            else if (OrbitCamera != null && Modes.Current == Mode.Orbit) OrbitCamera.OnDrag(delta);
        }

        private void OnDragEnd(Vector2 screenPosition)
        {
            if (WindowDraw != null) WindowDraw.OnDragEnd(screenPosition);
        }

        /// <summary>
        /// The art kit's `RightRail` (HUD-01 AC2): a near-opaque column down the right edge holding the three
        /// action buttons. Unlike the decoration on the canvas, the rail fill IS a raycast target — it is a
        /// panel, so a tap on it must not fall through into the room behind it (EDGE-02).
        /// </summary>
        private RectTransform BuildRightRail(Canvas canvas)
        {
            RectTransform rail = HudTheme.NewUi("RightRail", canvas.transform);
            rail.anchorMin = new Vector2(1f, 0f);
            rail.anchorMax = new Vector2(1f, 1f);
            rail.pivot = new Vector2(1f, 0.5f);
            rail.offsetMin = new Vector2(-RailWidth, 0f);
            rail.offsetMax = Vector2.zero;

            HudTheme.AddImage(rail, "RailFill", "panel_fill_9s", HudTheme.RailFill, Image.Type.Sliced,
                raycastTarget: true);
            return rail;
        }

        /// <summary>One 212 x 46 rail button, `top` px below the rail's top edge and inset 14 px from its right.</summary>
        private HudButton RailButton(string objectName, string iconSprite, string label, float top, bool withStateDot)
        {
            HudButton button = HudButton.Create(Rail, objectName, iconSprite, label, RailButtonSize, withStateDot);
            ((RectTransform)button.transform).anchoredPosition = new Vector2(-RailInset, top);
            return button;
        }

        private void RailDivider(float top)
        {
            RectTransform band = HudTheme.NewUi("Divider", Rail);
            band.anchorMin = new Vector2(1f, 1f);
            band.anchorMax = new Vector2(1f, 1f);
            band.pivot = new Vector2(1f, 1f);
            band.anchoredPosition = new Vector2(-RailInset, top);
            band.sizeDelta = new Vector2(RailButtonSize.x, 1f);
            HudTheme.AddDivider(band);
        }

        /// <summary>Footprint corners in the world XZ plane, centred on the origin.</summary>
        private Vector2[] Footprint()
        {
            float x = roomSize.x * 0.5f;
            float z = roomSize.y * 0.5f;
            return new[]
            {
                new Vector2(-x, -z),
                new Vector2(x, -z),
                new Vector2(x, z),
                new Vector2(-x, z)
            };
        }

        private Material ResolveMaterial()
        {
            if (surfaceMaterial != null) return surfaceMaterial;

            Shader shader = Shader.Find(LitShaderName);
            if (shader == null) return null;

            _runtimeMaterial = new Material(shader);
            return _runtimeMaterial;
        }

        /// <summary>WindowDraw entry predicate (AD-013): only a selected Wall may be drawn on.</summary>
        private bool IsWallSelected()
        {
            if (Model.SelectedSurfaceId == null) return false;
            SurfaceDefinition selected = Model.GetSurface(Model.SelectedSurfaceId.Value);
            return selected != null && selected.kind == SurfaceKind.Wall;
        }
    }
}
