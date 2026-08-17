using Formify.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace Formify.Presentation
{
    /// <summary>
    /// CLR-01 — the only way back to "nothing selected". In WindowDraw the mode is forced back to Orbit first
    /// (raising ModeManager.DrawCancelRequested so the in-progress draw dies), and only then is the selection
    /// cleared (CLR AC3). Idempotency lives in <see cref="RoomModel.ClearSelection"/>; this adds nothing on top.
    /// </summary>
    public class ClearButton : MonoBehaviour
    {
        private RoomModel _model;
        private ModeManager _modes;

        /// <summary>Binds the model/modes and wires this button's onClick, if it has a Button.</summary>
        public void Configure(RoomModel model, ModeManager modes)
        {
            _model = model;
            _modes = modes;

            Button button = GetComponent<Button>();
            if (button == null) return;

            button.onClick.RemoveListener(OnClick);
            button.onClick.AddListener(OnClick);
        }

        public void OnClick()
        {
            if (_model == null) return;

            if (_modes != null && _modes.Current == Mode.WindowDraw) _modes.TrySet(Mode.Orbit);

            _model.ClearSelection();
        }
    }
}
