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
        private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");

        /// <summary>
        /// SEL-02 in the kit palette (HUD-01): Accent at SelectedRowFill's alpha, laid over the surface's own
        /// colour. Composited HERE because the surface material is opaque URP Lit and throws _BaseColor's alpha
        /// away — pushing SelectedRowFill straight in would repaint the wall solid green instead of tinting it.
        /// </summary>
        private static readonly Color KitTint =
            Color.Lerp(Color.white, HudTheme.Accent, HudTheme.SelectedRowFill.a);

        private const int LayerUnresolved = -2;

        private static int _outlineLayer = LayerUnresolved;

        /// <summary>
        /// The layer the two outline RenderObjects features filter on (T24, OUT-01). Resolved on first use,
        /// never in a field initializer: Unity forbids NameToLayer during MonoBehaviour construction. -1 when
        /// the project has no such layer, which disables the swap instead of breaking selection.
        /// </summary>
        private static int OutlineLayer
        {
            get
            {
                if (_outlineLayer == LayerUnresolved) _outlineLayer = LayerMask.NameToLayer("SelectedSurface");
                return _outlineLayer;
            }
        }

        [SerializeField] private Color baseColor = Color.white;
        [SerializeField] private Color tintColor = KitTint;

        private readonly List<Rect2D> _holes = new List<Rect2D>();

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private MaterialPropertyBlock _block;
        private RoomModel _model;
        private Mesh _mesh;
        private int _restoreLayer;

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
                _model.WindowAdded -= OnWindowAdded;
                _model.WindowRemoved -= OnWindowRemoved;
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
                _model.WindowAdded += OnWindowAdded;
                _model.WindowRemoved += OnWindowRemoved;
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
            // OUT-01 rides the SAME block: the two RenderObjects passes draw THIS renderer with the shared
            // SelectionOutline override material, and a per-renderer block still overrides that material's
            // _OutlineColor — so the ring turns the kit accent without instancing anything (AD-010). One
            // selection, one look: the ring is the accent, the tint is the same green at the kit's row alpha.
            _block.SetColor(OutlineColorProperty, HudTheme.Accent);
            _meshRenderer.SetPropertyBlock(_block);

            // OUT-01: the outline is a pair of RenderObjects passes filtered on the SelectedSurface
            // layer, so selection is a layer swap. SelectionController's mask carries that layer, so
            // the surface stays tappable while it is selected.
            if (OutlineLayer < 0) return;
            if (selected)
            {
                if (gameObject.layer != OutlineLayer) _restoreLayer = gameObject.layer;
                gameObject.layer = OutlineLayer;
            }
            else if (gameObject.layer == OutlineLayer)
            {
                gameObject.layer = _restoreLayer;
            }
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
            _restoreLayer = gameObject.layer;
            _block = new MaterialPropertyBlock();
        }

        /// <summary>Only the surface losing and the surface gaining the selection change appearance.</summary>
        private void OnSelectionChanged(int? previous, int? current)
        {
            if (Surface == null) return;
            if (previous != Surface.id && current != Surface.id) return;
            SetTint(current == Surface.id);
        }

        /// <summary>
        /// EDGE-05: the rebuild is the operation. When the builder rejects the new geometry the previous mesh
        /// and collider stay live, so the model has to drop the window entry too — otherwise it claims an
        /// opening the wall does not have.
        /// </summary>
        private void OnWindowAdded(WindowSpec spec)
        {
            if (!Owns(spec) || Rebuild()) return;
            _model.RollbackWindowAdd(spec.id);
        }

        private void OnWindowRemoved(WindowSpec spec)
        {
            if (!Owns(spec) || Rebuild()) return;
            _model.RollbackWindowRemove(spec);
        }

        private bool Owns(WindowSpec spec) => Surface != null && spec != null && spec.surfaceId == Surface.id;
    }
}
