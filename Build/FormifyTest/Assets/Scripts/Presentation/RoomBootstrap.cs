using System.Collections.Generic;
using Formify.Domain;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.ARFoundation;

namespace Formify.Presentation
{
    /// <summary>
    /// Builds the synthetic room on Awake (ROOM-01) and owns the references the controllers share —
    /// plain serialized fields and properties, no DI framework (AD-002).
    /// The room is generated; the HUD is not. <see cref="Compose"/> binds the model and the modes to the
    /// <see cref="HudRoot"/> the scene already holds (AD-025), and builds one only when the scene has none.
    /// </summary>
    public class RoomBootstrap : MonoBehaviour
    {
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        [SerializeField] private Vector2 roomSize = new Vector2(6f, 4f);
        [SerializeField] private float height = 2.8f;
        [SerializeField] private float thickness = 0.15f;

        /// <summary>Assigned material wins; a runtime URP Lit is created when this is left empty.</summary>
        [SerializeField] private Material surfaceMaterial;

        [SerializeField] private Camera roomCamera;

        /// <summary>The HUD authored into the scene (AD-025). Found in the scene, then built, when left empty.</summary>
        [SerializeField] private HudRoot hud;

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

        /// <summary>The HUD this bootstrap drives. Null until <see cref="Compose"/> has run.</summary>
        public HudRoot Hud { get; private set; }

        public SurfaceListPanel ListPanel => Hud != null ? Hud.ListPanel : null;

        /// <summary>The art kit's right-hand action rail. Null until <see cref="Compose"/> has run.</summary>
        public RectTransform Rail => Hud != null ? Hud.Rail : null;

        public WindowModeButton WindowMode => Hud != null ? Hud.WindowMode : null;

        public HudReadout Readout => Hud != null ? Hud.Readout : null;

        public HudHintPill HintPill => Hud != null ? Hud.HintPill : null;

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
            EnsureModel();

            Material material = ResolveMaterial();

            IReadOnlyList<SurfaceDefinition> surfaces = Model.Surfaces;
            for (int i = 0; i < surfaces.Count; i++)
            {
                SurfaceDefinition surface = surfaces[i];

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
        /// Model and modes are plain C# and need no GameObject, so building them apart from the meshes lets the
        /// editor bake call <see cref="Compose"/> for the HUD on its own.
        /// </summary>
        private void EnsureModel()
        {
            if (Model != null) return;

            RoomDefinition room = RoomBuilder.Build(Footprint(), height, thickness);
            Model = new RoomModel(room.surfaces);
            Modes = new ModeManager(IsWallSelected);
        }

        /// <summary>
        /// Creates and wires the runtime object graph: one input doorway, the orbit camera, the tap selection
        /// and the HUD's bindings. Tests that only need the room switch it off through
        /// <see cref="buildRuntimeComposition"/>.
        /// No view is built here — the HUD comes from the scene (AD-025) and this only hands it the model, the
        /// modes and the handlers. Public because the editor bake (HudSceneBaker) runs exactly this to author
        /// that scene copy in the first place, so what is baked and what runs are one wiring.
        /// </summary>
        public void Compose()
        {
            EnsureModel();

            Hud = ResolveHud();
            Input = gameObject.AddComponent<InputRouter>();

            Camera camera = RoomCamera;
            if (camera != null)
            {
                OrbitCamera = camera.gameObject.AddComponent<OrbitCameraController>();
                OrbitCamera.ConfigureRoom(RoomCentre, RoomBounds);

                Selection = gameObject.AddComponent<SelectionController>();
                Selection.Configure(Model, Modes, camera);
            }

            // AD-015: while a window is being drawn the target wall is locked, and that has to hold for a tap on
            // the list exactly as it holds for a tap on the wall itself.
            Hud.ListPanel.Configure(Model, () => Modes == null || Modes.Current != Mode.WindowDraw);

            HudButton windowModeHud = Hud.WindowHudButton;
            WindowModeButton windowMode = Hud.WindowMode;
            windowMode.StateDot = windowModeHud.Dot;
            windowMode.Configure(Model, Modes);
            // The button paints itself from its own state; only the click still has to be wired by hand, because
            // WindowModeButton (unlike Clear and AR) does not wire its own onClick in Configure. Removed first:
            // a scene HUD is bound again on every play, and the listener would stack.
            windowModeHud.ActiveSource = () => windowMode.IsActive;
            windowModeHud.Button.onClick.RemoveListener(windowMode.OnClick);
            windowModeHud.Button.onClick.AddListener(windowMode.OnClick);

            Hud.Clear.Configure(Model, Modes);

            if (camera != null)
            {
                WindowDraw = gameObject.AddComponent<WindowDrawController>();
                WindowDraw.Configure(Model, Modes, camera);
            }

            Windows = gameObject.AddComponent<WindowViewFactory>();
            Windows.Configure(Model, Model.GetSurface, Hud.Canvas);

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

            // ArToggleButton.Configure wires its own onClick; wiring it here too would fire OnClick twice and
            // undo the toggle.
            Hud.ArToggle.Configure(Modes, ArCamera);
            Hud.ArHudButton.ActiveSource = () => Modes != null && Modes.Current == Mode.Ar;

            Hud.ViewSwitch.Configure(Modes, Hud.Canvas);

            Hud.Readout.Configure(Model);
            Hud.HintPill.Configure(Modes);

            Input.Tapped += OnTapped;
            Input.DragStart += OnDragStart;
            Input.DragDelta += OnDragDelta;
            Input.DragEnd += OnDragEnd;
        }

        /// <summary>
        /// The HUD to drive (AD-025): the one wired in the inspector, else the one baked into the open scene,
        /// else a fresh build for a scene that holds none — a bare test scene, or the bake itself.
        /// </summary>
        private HudRoot ResolveHud()
        {
            if (hud != null) return hud;

            HudRoot inScene = FindFirstObjectByType<HudRoot>(FindObjectsInactive.Include);
            return inScene != null ? inScene : HudRoot.Build(transform);
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
