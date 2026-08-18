using System.Collections.Generic;
using Formify.Domain;
using UnityEngine;

namespace Formify.Presentation
{
    /// <summary>
    /// A tap target that answers before the surface under it. T23's WindowView implements it (AD-014).
    /// B2 changed what the answer is: the target now makes itself the selection instead of leaving the
    /// selection alone. This controller still only routes — it never learns what a window is.
    /// </summary>
    public interface IWindowTapTarget
    {
        void OnTapped();
    }

    /// <summary>
    /// Tap to select a surface (SEL-01, SEL-03). Misses never clear the selection — the Clear button is the
    /// only path to empty (CLR-02 retired).
    /// In the 2D plan a floor tap close enough to a wall selects that wall instead (TOP-02 AC8, AD-016).
    /// </summary>
    public class SelectionController : MonoBehaviour
    {
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private LayerMask raycastMask = ~0;
        [SerializeField] private float maxRayDistance = 100f;

        /// <summary>
        /// TOP-02 AC8 — how close (in screen pixels) a plan tap has to land to a wall's base edge for that wall
        /// to win the tap from the floor underneath it. Plan only; every 3D mode ignores it.
        /// </summary>
        [SerializeField] private float wallTapTolerancePixels = 30f;

        private RoomModel _model;
        private ModeManager _modes;
        private int _mask;

        /// <summary>
        /// The layers a tap ray may hit, before <see cref="ResolveMask"/> ORs the outline layer in. Settable —
        /// the serialized field stays the scene default — so a test can hand over a mask that deliberately
        /// excludes "SelectedSurface" and prove that OR is what keeps the selected surface hittable (OUT-01 AC2).
        /// </summary>
        public LayerMask RaycastMask
        {
            get => raycastMask;
            set
            {
                raycastMask = value;
                _mask = ResolveMask();
            }
        }

        private void Awake() => _mask = ResolveMask();

        /// <summary>Wiring without prefabs, for RoomBootstrap and tests. Addition to the contract.</summary>
        public void Configure(RoomModel model, ModeManager modes, Camera camera)
        {
            _model = model;
            _modes = modes;
            raycastCamera = camera;
            _mask = ResolveMask();
        }

        public void OnTap(Vector2 screenPosition)
        {
            // 1. Drawing locks the target wall: swallow the tap, dispatch nothing (AD-015, WIN AC13).
            if (_modes != null && _modes.Current == Mode.WindowDraw) return;
            if (raycastCamera == null || _model == null) return;

            // 4. A miss — empty space or a ray through a window opening — leaves the selection alone (SEL AC6).
            if (!Physics.Raycast(raycastCamera.ScreenPointToRay(screenPosition), out RaycastHit hit,
                    maxRayDistance, _mask)) return;

            // 2. A window wins the tap from the surface behind it and selects ITSELF through the model (B2),
            //    which clears any selected surface (AD-026). Supersedes AD-014 / SEL AC7, where the tap only
            //    raised the delete affordance and the surface selection stood.
            IWindowTapTarget window = hit.collider.GetComponentInParent<IWindowTapTarget>();
            if (window != null)
            {
                window.OnTapped();
                return;
            }

            // 3. Surface hit (SEL AC1-AC2). Select() is a no-op for an already-selected id.
            SurfaceView view = hit.collider.GetComponentInParent<SurfaceView>();
            if (view == null || view.Surface == null) return;

            // 3b. Plan only, floor hits only: edge-on a 0.15 m wall is about 10 px wide and so untappable, so a
            // floor tap within the tolerance of a wall's plan segment goes to that wall instead (TOP-02 AC8,
            // AD-016). A direct wall hit never reaches here, and no 3D mode ever takes this branch.
            SurfaceDefinition wall =
                _modes != null && _modes.Current == Mode.TopDown && view.Surface.kind == SurfaceKind.Floor
                    ? NearestWallWithinTolerance(screenPosition)
                    : null;

            _model.Select(wall != null ? wall.id : view.Surface.id);
        }

        /// <summary>
        /// The wall whose plan segment — the base edge from <c>origin</c> to <c>origin + right * width</c> —
        /// is nearest the tap in screen space, or null when none is inside <see cref="wallTapTolerancePixels"/>.
        /// Point-to-SEGMENT, not point-to-line: the projection parameter is clamped to [0, 1], so a tap past a
        /// wall's end measures to that end instead of to the infinite line the wall lies on.
        /// </summary>
        private SurfaceDefinition NearestWallWithinTolerance(Vector2 screenPosition)
        {
            SurfaceDefinition nearest = null;
            float nearestDistance = wallTapTolerancePixels;

            IReadOnlyList<SurfaceDefinition> surfaces = _model.Surfaces;
            for (int i = 0; i < surfaces.Count; i++)
            {
                SurfaceDefinition surface = surfaces[i];
                if (surface == null || surface.kind != SurfaceKind.Wall) continue;

                Vector3 start = raycastCamera.WorldToScreenPoint(surface.origin);
                Vector3 end = raycastCamera.WorldToScreenPoint(surface.origin + surface.right * surface.width);
                // Behind the camera WorldToScreenPoint mirrors the point through the viewport centre; trusting
                // that would drop a far-side wall right under the finger. Skip the wall instead.
                if (start.z <= 0f || end.z <= 0f) continue;

                Vector2 a = new Vector2(start.x, start.y);
                Vector2 ab = new Vector2(end.x - start.x, end.y - start.y);
                float lengthSquared = ab.sqrMagnitude;
                float t = lengthSquared > 0f
                    ? Mathf.Clamp01(Vector2.Dot(screenPosition - a, ab) / lengthSquared)
                    : 0f;

                float distance = Vector2.Distance(screenPosition, a + ab * t);
                if (distance > nearestDistance) continue;

                nearest = surface;
                nearestDistance = distance;
            }

            return nearest;
        }

        /// <summary>
        /// The selected surface moves onto the "SelectedSurface" layer for the P2 outline (T24, AD-010/OUT-01
        /// AC2), so the mask must carry that layer from day one or the selected surface becomes untappable.
        /// The layer only exists once T24 adds it — until then it resolves to -1 and is skipped.
        /// </summary>
        private int ResolveMask()
        {
            int selectedSurface = LayerMask.NameToLayer("SelectedSurface");
            return selectedSurface >= 0 ? raycastMask.value | (1 << selectedSurface) : raycastMask.value;
        }
    }
}
