using Formify.Domain;
using UnityEngine;

namespace Formify.Presentation
{
    /// <summary>
    /// WIN-01 AC1: the door into window mode. Per AD-015 the button only exists while it can actually work —
    /// a Wall is selected AND the mode is Orbit — so it is never visible-but-dead. Visibility is recomputed on
    /// both <see cref="RoomModel.SelectionChanged"/> and <see cref="ModeManager.ModeChanged"/> and applied by
    /// toggling this GameObject. Wire the uGUI Button's onClick to <see cref="OnClick"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class WindowModeButton : MonoBehaviour
    {
        RoomModel model;
        ModeManager modes;

        public bool IsVisible { get; private set; }

        public void Configure(RoomModel model, ModeManager modes)
        {
            Unsubscribe();

            this.model = model;
            this.modes = modes;

            if (this.model != null) this.model.SelectionChanged += OnSelectionChanged;
            if (this.modes != null) this.modes.ModeChanged += OnModeChanged;

            Refresh();
        }

        void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>Enters window mode from Orbit; acts as the exit while window mode is active.</summary>
        public void OnClick()
        {
            if (modes == null) return;

            modes.TrySet(modes.Current == Mode.WindowDraw ? Mode.Orbit : Mode.WindowDraw);
        }

        void OnSelectionChanged(int? previous, int? current) => Refresh();

        void OnModeChanged(Mode previous, Mode current) => Refresh();

        void Refresh()
        {
            SurfaceDefinition selected = null;
            if (model != null && model.SelectedSurfaceId.HasValue)
                selected = model.GetSurface(model.SelectedSurfaceId.Value);

            IsVisible = selected != null
                        && selected.kind == SurfaceKind.Wall
                        && modes != null
                        && modes.Current == Mode.Orbit;

            if (gameObject.activeSelf != IsVisible) gameObject.SetActive(IsVisible);
        }

        void Unsubscribe()
        {
            if (model != null) model.SelectionChanged -= OnSelectionChanged;
            if (modes != null) modes.ModeChanged -= OnModeChanged;
        }
    }
}
