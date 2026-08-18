using Formify.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Formify.Presentation
{
    /// <summary>
    /// WIN-01 AC1: the door into window mode, and the way back out of it. Per AD-019 the button is always on
    /// screen — it carries its state instead of appearing and disappearing (that supersedes AD-015's visibility
    /// rule): interactable while a Wall is selected and the mode is Orbit or WindowDraw, disabled otherwise,
    /// with <see cref="IsActive"/> lighting the art kit's state dot while window mode runs. Clicking it while
    /// active exits back to Orbit (AD-021) — the only visible way out short of finishing the drag.
    /// State is recomputed on both <see cref="RoomModel.SelectionChanged"/> and
    /// <see cref="ModeManager.ModeChanged"/>. Wire the uGUI Button's onClick to <see cref="OnClick"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class WindowModeButton : MonoBehaviour
    {
        // Serialized: the baked scene HUD (AD-025) keeps the dot, so Configure only rebinds model and modes.
        [SerializeField] Graphic stateDot;

        RoomModel model;
        ModeManager modes;
        Button button;

        /// <summary>The art kit's state dot, lit while window mode is active. Optional; the view assigns it.</summary>
        public Graphic StateDot
        {
            get { return stateDot; }
            set { stateDot = value; }
        }

        /// <summary>Whether the button can be pressed right now. The GameObject stays active either way.</summary>
        public bool IsInteractable { get; private set; }

        /// <summary>Whether window mode is the current mode — what the state dot shows.</summary>
        public bool IsActive { get; private set; }

        public void Configure(RoomModel model, ModeManager modes)
        {
            Unsubscribe();

            this.model = model;
            this.modes = modes;
            button = GetComponent<Button>();

            if (this.model != null) this.model.SelectionChanged += OnSelectionChanged;
            if (this.modes != null) this.modes.ModeChanged += OnModeChanged;

            Refresh();
        }

        void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>Enters window mode from Orbit; exits back to Orbit while window mode is active (AD-021).</summary>
        public void OnClick()
        {
            if (modes == null || !IsInteractable) return;

            modes.TrySet(modes.Current == Mode.WindowDraw ? Mode.Orbit : Mode.WindowDraw);
        }

        void OnSelectionChanged(int? previous, int? current) => Refresh();

        void OnModeChanged(Mode previous, Mode current) => Refresh();

        void Refresh()
        {
            SurfaceDefinition selected = null;
            if (model != null && model.SelectedSurfaceId.HasValue)
                selected = model.GetSurface(model.SelectedSurfaceId.Value);

            bool wallSelected = selected != null && selected.kind == SurfaceKind.Wall;

            IsActive = modes != null && modes.Current == Mode.WindowDraw;
            IsInteractable = wallSelected && modes != null && (modes.Current == Mode.Orbit || IsActive);

            if (button != null) button.interactable = IsInteractable;
            if (StateDot != null) StateDot.enabled = IsActive;
        }

        void Unsubscribe()
        {
            if (model != null) model.SelectionChanged -= OnSelectionChanged;
            if (modes != null) modes.ModeChanged -= OnModeChanged;
        }
    }
}
