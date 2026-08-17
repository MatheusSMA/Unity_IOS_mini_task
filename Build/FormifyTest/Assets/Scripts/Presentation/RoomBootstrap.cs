using System.Collections.Generic;
using Formify.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Formify.Presentation
{
    /// <summary>
    /// Builds the synthetic room on Awake (ROOM-01) and owns the references the controllers share —
    /// plain serialized fields and properties, no DI framework (AD-002).
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

        /// <summary>Off in tests that want the bare room without controllers or UI.</summary>
        [SerializeField] private bool buildRuntimeComposition = true;

        private readonly List<SurfaceView> _views = new List<SurfaceView>();

        private Material _runtimeMaterial;

        public RoomModel Model { get; private set; }

        public ModeManager Modes { get; private set; }

        public IReadOnlyList<SurfaceView> SurfaceViews => _views;

        public SurfaceView CeilingView { get; private set; }

        public InputRouter Input { get; private set; }

        public OrbitCameraController OrbitCamera { get; private set; }

        public SelectionController Selection { get; private set; }

        public SurfaceListPanel ListPanel { get; private set; }

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
                Input.DragDelta -= OnDragDelta;
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
            ListPanel.Configure(Model);

            GameObject clearGo = CreateButton(ListPanel.Canvas, "Clear", new Vector2(1f, 1f), new Vector2(-16f, -16f));
            clearGo.AddComponent<ClearButton>().Configure(Model, Modes);

            Input.Tapped += OnTapped;
            Input.DragDelta += OnDragDelta;
        }

        private void OnTapped(Vector2 screenPosition)
        {
            if (Selection != null) Selection.OnTap(screenPosition);
        }

        /// <summary>Only Orbit drives the camera from drags: AR owns the pose and WindowDraw owns the gesture.</summary>
        private void OnDragDelta(Vector2 delta)
        {
            if (OrbitCamera != null && Modes.Current == Mode.Orbit) OrbitCamera.OnDrag(delta);
        }

        /// <summary>
        /// A labelled uGUI button on the shared canvas. Anchor is the corner it sticks to (0,0 bottom-left,
        /// 1,1 top-right); offset is the pixel nudge away from it.
        /// </summary>
        public static GameObject CreateButton(Canvas canvas, string label, Vector2 anchor, Vector2 offset)
        {
            var go = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvas.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.sizeDelta = new Vector2(140f, 48f);
            rect.anchoredPosition = offset;
            go.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.18f, 0.9f);

            var textGo = new GameObject("Label", typeof(RectTransform));
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.text = label;
            text.fontSize = 22f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;

            return go;
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
