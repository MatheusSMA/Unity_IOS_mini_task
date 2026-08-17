using System.Collections.Generic;
using Formify.Domain;
using UnityEngine;

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

        private readonly List<SurfaceView> _views = new List<SurfaceView>();

        private Material _runtimeMaterial;

        public RoomModel Model { get; private set; }

        public ModeManager Modes { get; private set; }

        public IReadOnlyList<SurfaceView> SurfaceViews => _views;

        public SurfaceView CeilingView { get; private set; }

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
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
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
