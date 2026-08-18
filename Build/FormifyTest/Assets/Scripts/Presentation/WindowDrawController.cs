using System;
using Formify.Domain;
using UnityEngine;

namespace Formify.Presentation
{
    /// <summary>
    /// WIN-01 / WIN-02: drag a rectangle on the selected wall to cut a window hole.
    ///
    /// Self-gates on <see cref="ModeManager.Current"/> (AD-013): a drag only starts while the mode is
    /// <see cref="Mode.WindowDraw"/> AND the raycast lands on the wall that is currently selected — a start on
    /// empty space, the floor, the ceiling or another wall creates nothing (WIN AC11). The live preview follows
    /// the finger clamped into the wall bounds minus the edge margin, so an off-wall finger clamps instead of
    /// switching walls (EDGE-03). Release hands the rectangle to <see cref="RoomModel.TryAddWindow"/>, which has
    /// the final word: a tap-like drag is refused by the validator's minimum size (EDGE-04) and any rejection
    /// leaves the wall untouched (WIN AC7-AC9). Every exit from window mode cancels the draw through
    /// <see cref="ModeManager.DrawCancelRequested"/> (CLR-01 AC3).
    /// </summary>
    [DisallowMultipleComponent]
    public class WindowDrawController : MonoBehaviour
    {
        static readonly int[] PreviewTriangles = { 0, 1, 2, 0, 2, 3 };

        [SerializeField]
        [Tooltip("Mirrors WindowPlacementValidator.EdgeMargin (0.1 m). RoomModel keeps its validator private, " +
                 "so the live preview clamps with this copy; the validator still decides on release.")]
        float edgeMargin = 0.1f;

        [SerializeField]
        [Tooltip("Metres along the wall normal, so the preview quad does not z-fight with the wall face.")]
        float previewOffset = 0.01f;

        [SerializeField]
        [Tooltip("Preview material. Left empty a simple unlit fallback is created at runtime.")]
        Material previewMaterial;

        [SerializeField]
        [Tooltip("Art kit accent 35F08A, translucent (HUD-01 AC2). The preview is a world-space quad, not uGUI, " +
                 "so it carries the colour itself rather than a HudTheme sprite.")]
        Color previewColor = new Color(0.208f, 0.941f, 0.541f, 0.28f);

        RoomModel model;
        ModeManager modes;
        Camera roomCamera;

        SurfaceDefinition target;
        Vector2 startLocal;
        Vector2 currentLocal;
        Vector2 lastScreenPosition;

        GameObject preview;
        Mesh previewMesh;
        Material fallbackMaterial;
        readonly Vector3[] previewVertices = new Vector3[4];

        public bool IsDrawing { get; private set; }

        /// <summary>
        /// The live rectangle: on start, on every move, and once more with surface -1 when the draw is over —
        /// placed, refused or cancelled. The HUD readout reports the drag off this rather than polling the
        /// controller every frame (AD-007, AD-022).
        /// </summary>
        public event Action<int, Rect2D> DrawRectChanged;

        /// <summary>The clamped rectangle in wall-local metres. Keeps the last drawn value after end/cancel.</summary>
        public Rect2D PreviewRect { get; private set; }

        /// <summary>The wall being drawn on, -1 while no draw is in progress.</summary>
        public int TargetSurfaceId { get; private set; } = -1;

        public void Configure(RoomModel model, ModeManager modes, Camera camera)
        {
            if (this.modes != null) this.modes.DrawCancelRequested -= CancelDraw;

            this.model = model;
            this.modes = modes;
            roomCamera = camera;

            if (this.modes != null) this.modes.DrawCancelRequested += CancelDraw;
        }

        void OnDestroy()
        {
            if (modes != null) modes.DrawCancelRequested -= CancelDraw;

            CancelDraw();
            if (fallbackMaterial != null) Destroy(fallbackMaterial);
        }

        /// <summary>Fixes the target wall and the first corner. Anything but the selected wall is ignored.</summary>
        public void OnDragStart(Vector2 screenPosition)
        {
            CancelDraw();
            lastScreenPosition = screenPosition;

            if (model == null || modes == null || roomCamera == null) return;
            if (modes.Current != Mode.WindowDraw) return;

            RaycastHit hit;
            if (!Physics.Raycast(roomCamera.ScreenPointToRay(screenPosition), out hit)) return;

            SurfaceView view = hit.collider.GetComponentInParent<SurfaceView>();
            if (view == null || view.Surface == null) return;

            SurfaceDefinition surface = view.Surface;
            if (surface.kind != SurfaceKind.Wall) return;
            if (model.SelectedSurfaceId != surface.id) return;

            target = surface;
            TargetSurfaceId = surface.id;
            startLocal = ClampToWall(surface, ToWallLocal(surface, hit.point));
            currentLocal = startLocal;
            IsDrawing = true;

            RefreshRect();
            CreatePreview();
            RaiseDrawRectChanged();
        }

        /// <summary>InputRouter.DragDelta entry point: accumulates onto the last known pointer position.</summary>
        public void OnDragDelta(Vector2 delta)
        {
            if (!IsDrawing) return;

            OnDragMove(lastScreenPosition + delta);
        }

        /// <summary>Preferred update entry point: the current pointer position.</summary>
        public void OnDragMove(Vector2 screenPosition)
        {
            lastScreenPosition = screenPosition;
            if (!IsDrawing) return;

            Vector2 local;
            if (!TryProjectOntoTarget(screenPosition, out local)) return;

            currentLocal = local;
            RefreshRect();
            RefreshPreviewMesh();
            RaiseDrawRectChanged();
        }

        /// <summary>Drops the preview either way and lets the model accept or refuse the rectangle.</summary>
        public void OnDragEnd(Vector2 screenPosition)
        {
            if (!IsDrawing) return;

            OnDragMove(screenPosition);

            int surfaceId = TargetSurfaceId;
            Rect2D rect = PreviewRect;

            // Same teardown as a cancel: state is clean before listeners of WindowAdded run.
            CancelDraw();

            if (model == null) return;

            WindowRejection reason;
            if (!model.TryAddWindow(surfaceId, rect, out reason)) return;

            // AD-027: one drag, one window. Staying in the mode also kept the surfaces list locked for as long
            // as it lasted (AD-015), so a freshly placed window could not be picked from the list that had just
            // grown a row for it.
            if (modes != null) modes.TrySet(Mode.Orbit);
        }

        /// <summary>Idempotent: destroys the preview and creates no window.</summary>
        public void CancelDraw()
        {
            bool wasDrawing = IsDrawing;

            DestroyPreview();
            IsDrawing = false;
            TargetSurfaceId = -1;
            target = null;

            // Only when something was actually being drawn: OnDragStart cancels first thing, and a start that
            // misses the selected wall must not make the readout blink.
            if (wasDrawing) RaiseDrawRectChanged();
        }

        /// <summary>TargetSurfaceId is -1 after a cancel, which is how listeners read "the draw is over".</summary>
        void RaiseDrawRectChanged() => DrawRectChanged?.Invoke(TargetSurfaceId, PreviewRect);

        // ---- geometry ----

        static Vector2 ToWallLocal(SurfaceDefinition surface, Vector3 worldPoint)
        {
            Vector3 delta = worldPoint - surface.origin;
            return new Vector2(Vector3.Dot(delta, surface.right), Vector3.Dot(delta, surface.up));
        }

        Vector2 ClampToWall(SurfaceDefinition surface, Vector2 local)
        {
            return new Vector2(
                Mathf.Clamp(local.x, edgeMargin, Mathf.Max(edgeMargin, surface.width - edgeMargin)),
                Mathf.Clamp(local.y, edgeMargin, Mathf.Max(edgeMargin, surface.height - edgeMargin)));
        }

        /// <summary>
        /// Projects the pointer ray onto the TARGET wall's plane, not onto whatever the ray happens to hit, so a
        /// finger that leaves the wall clamps to its bounds instead of jumping to another surface (EDGE-03).
        /// </summary>
        bool TryProjectOntoTarget(Vector2 screenPosition, out Vector2 local)
        {
            local = currentLocal;
            if (roomCamera == null || target == null) return false;

            Ray ray = roomCamera.ScreenPointToRay(screenPosition);
            var plane = new Plane(target.Normal, target.origin);

            float enter;
            if (!plane.Raycast(ray, out enter)) return false;

            local = ClampToWall(target, ToWallLocal(target, ray.GetPoint(enter)));
            return true;
        }

        void RefreshRect()
        {
            PreviewRect = new Rect2D(
                Mathf.Min(startLocal.x, currentLocal.x),
                Mathf.Min(startLocal.y, currentLocal.y),
                Mathf.Abs(currentLocal.x - startLocal.x),
                Mathf.Abs(currentLocal.y - startLocal.y));
        }

        // ---- preview ----

        void CreatePreview()
        {
            preview = new GameObject("Window Draw Preview");
            preview.transform.SetParent(transform, false);
            preview.transform.SetPositionAndRotation(
                target.origin + target.Normal * previewOffset,
                Quaternion.LookRotation(target.Normal, target.up));

            previewMesh = new Mesh { name = "Window Draw Preview" };
            preview.AddComponent<MeshFilter>().sharedMesh = previewMesh;
            preview.AddComponent<MeshRenderer>().sharedMaterial = ResolveMaterial();

            RefreshPreviewMesh();
        }

        /// <summary>The preview object sits on the wall basis, so its local XY is already wall-local metres.</summary>
        void RefreshPreviewMesh()
        {
            if (previewMesh == null) return;

            Rect2D rect = PreviewRect;
            previewVertices[0] = new Vector3(rect.x, rect.y, 0f);
            previewVertices[1] = new Vector3(rect.XMax, rect.y, 0f);
            previewVertices[2] = new Vector3(rect.XMax, rect.YMax, 0f);
            previewVertices[3] = new Vector3(rect.x, rect.YMax, 0f);

            previewMesh.vertices = previewVertices;
            previewMesh.triangles = PreviewTriangles;
            previewMesh.RecalculateBounds();
        }

        void DestroyPreview()
        {
            if (preview != null) Destroy(preview);
            if (previewMesh != null) Destroy(previewMesh);

            preview = null;
            previewMesh = null;
        }

        Material ResolveMaterial()
        {
            if (previewMaterial != null) return previewMaterial;

            if (fallbackMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader != null) fallbackMaterial = new Material(shader) { color = previewColor };
            }

            return fallbackMaterial;
        }
    }
}
