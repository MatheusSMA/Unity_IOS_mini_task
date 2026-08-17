using System.Collections.Generic;
using Formify.Domain;
using UnityEngine;

namespace Formify.Presentation
{
    /// <summary>
    /// The visible body of one surface. Renders the slab and keeps the MeshCollider in lockstep with the
    /// rendered mesh, so a cut window is see-through AND ray-through (AD-009 — a renderer-only update leaves
    /// a ghost collider inside the opening).
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class SurfaceView : MonoBehaviour
    {
        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color tintColor = new Color(1f, 0.45f, 0.05f, 1f);

        private readonly List<Rect2D> _holes = new List<Rect2D>();

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private MaterialPropertyBlock _block;
        private RoomModel _model;
        private Mesh _mesh;

        public SurfaceDefinition Surface { get; private set; }

        /// <summary>Tint state, so a test can assert selection feedback without reading GPU state.</summary>
        public bool IsTinted { get; private set; }

        /// <summary>Serialized tuning, read-only so tests assert against the configured value, not a copy of it.</summary>
        public Color BaseColor => baseColor;

        public Color TintColor => tintColor;

        private void Awake() => Cache();

        private void OnDestroy()
        {
            if (_model != null)
            {
                _model.SelectionChanged -= OnSelectionChanged;
                _model.WindowAdded -= OnWindowChanged;
                _model.WindowRemoved -= OnWindowChanged;
            }

            if (_mesh != null) Destroy(_mesh);
        }

        public void Initialize(SurfaceDefinition surface, RoomModel model)
        {
            Cache();
            Surface = surface;
            _model = model;

            // Surface-local space must land on world space as +X -> right, +Y -> up, +Z -> Normal.
            // LookRotation(forward, upwards) maps local +Z to forward, local +Y to upwards (already
            // perpendicular here) and local +X to Cross(upwards, forward). Since Normal == Cross(right, up),
            // the triple (right, up, Normal) is cyclic exactly like (X, Y, Z), so Cross(up, Normal) == right.
            // No handedness correction is needed: LookRotation(Normal, up) is the basis verbatim.
            transform.position = surface.origin;
            transform.rotation = Quaternion.LookRotation(surface.Normal, surface.up);

            if (_model != null)
            {
                _model.SelectionChanged += OnSelectionChanged;
                _model.WindowAdded += OnWindowChanged;
                _model.WindowRemoved += OnWindowChanged;
            }

            SetTint(_model != null && _model.SelectedSurfaceId == surface.id);
            Rebuild();
        }

        /// <summary>Tint through a MaterialPropertyBlock only — never instantiates the shared material (AD-010).</summary>
        public void SetTint(bool selected)
        {
            Cache();
            IsTinted = selected;
            _meshRenderer.GetPropertyBlock(_block);
            _block.SetColor(BaseColorProperty, selected ? tintColor : baseColor);
            _meshRenderer.SetPropertyBlock(_block);
        }

        /// <summary>
        /// Rebuilds the slab from the surface plus its current windows. Returns false and leaves the previous
        /// mesh AND collider live when the builder rejects the input (EDGE-05), so the caller can roll back.
        /// </summary>
        public bool Rebuild()
        {
            if (Surface == null) return false;
            Cache();

            _holes.Clear();
            IReadOnlyList<WindowSpec> windows = _model?.GetWindows(Surface.id);
            if (windows != null)
            {
                for (int i = 0; i < windows.Count; i++) _holes.Add(windows[i].rect);
            }

            MeshData data = SurfaceMeshBuilder.Build(Surface, _holes);
            if (data == null) return false;

            if (_mesh == null) _mesh = new Mesh { name = Surface.name };

            _mesh.Clear();
            _mesh.vertices = data.vertices;
            _mesh.uv = data.uvs;
            _mesh.triangles = data.triangles;
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();

            _meshFilter.sharedMesh = _mesh;
            // Same operation as the renderer update. Reassigning through null forces PhysX to re-cook even
            // when the Mesh instance is unchanged; skipping it is the named ghost-collider bug.
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = _mesh;
            return true;
        }

        private void Cache()
        {
            if (_block != null) return;
            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
            _block = new MaterialPropertyBlock();
        }

        /// <summary>Only the surface losing and the surface gaining the selection change appearance.</summary>
        private void OnSelectionChanged(int? previous, int? current)
        {
            if (Surface == null) return;
            if (previous != Surface.id && current != Surface.id) return;
            SetTint(current == Surface.id);
        }

        private void OnWindowChanged(WindowSpec spec)
        {
            if (Surface == null || spec == null || spec.surfaceId != Surface.id) return;
            Rebuild();
        }
    }
}
