using Formify.Domain;
using UnityEngine;

namespace Formify.Presentation
{
    /// <summary>A tap target that answers instead of the selection. T23's WindowView implements it (AD-014).</summary>
    public interface IWindowTapTarget
    {
        void OnTapped();
    }

    /// <summary>
    /// Tap to select a surface (SEL-01, SEL-03). Misses never clear the selection — the Clear button is the
    /// only path to empty (CLR-02 retired).
    /// </summary>
    public class SelectionController : MonoBehaviour
    {
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private LayerMask raycastMask = ~0;
        [SerializeField] private float maxRayDistance = 100f;

        private RoomModel _model;
        private ModeManager _modes;
        private int _mask;

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

            // 2. A window owns its own tap and never touches the selection (AD-014, SEL AC7).
            IWindowTapTarget window = hit.collider.GetComponentInParent<IWindowTapTarget>();
            if (window != null)
            {
                window.OnTapped();
                return;
            }

            // 3. Surface hit (SEL AC1-AC2). Select() is a no-op for an already-selected id.
            SurfaceView view = hit.collider.GetComponentInParent<SurfaceView>();
            if (view != null && view.Surface != null) _model.Select(view.Surface.id);
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
