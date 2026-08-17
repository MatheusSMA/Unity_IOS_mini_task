using Formify.Domain;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

namespace Formify.Presentation
{
    /// <summary>
    /// AR-01 AC1/AC3/AC4 — the switch into and out of AR. Session start/end and the orientation handoff are
    /// ModeManager / ArPoseCameraController side effects, not button logic. The button greys out when AR is
    /// unavailable (touch camera keeps working) and while the current mode is one AR cannot be entered from
    /// (AD-013: Ar is reachable only from Orbit), so the UI never offers an illegal transition.
    /// </summary>
    public class ArToggleButton : MonoBehaviour
    {
        private ModeManager _modes;
        private ArPoseCameraController _arCamera;
        private Button _button;

        public bool IsInteractable =>
            _modes != null && _arCamera != null && _arCamera.IsAvailable &&
            (_modes.Current == Mode.Orbit || _modes.Current == Mode.Ar);

        /// <summary>Binds the modes and the AR camera, wires this button's onClick, and greys it appropriately.</summary>
        public void Configure(ModeManager modes, ArPoseCameraController arCamera)
        {
            if (_modes != null) _modes.ModeChanged -= OnModeChanged;
            _modes = modes;
            if (_modes != null) _modes.ModeChanged += OnModeChanged;

            _arCamera = arCamera;

            _button = GetComponent<Button>();
            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClick);
                _button.onClick.AddListener(OnClick);
            }

            // Availability is still unknown on the first frame (ARSession.state starts at None), and no mode
            // change would ever follow to re-check it, so the session's own state change re-greys the button.
            ARSession.stateChanged -= OnArSessionStateChanged;
            ARSession.stateChanged += OnArSessionStateChanged;

            Refresh();
        }

        public void OnClick()
        {
            if (!IsInteractable) return;

            _modes.TrySet(_modes.Current == Mode.Ar ? Mode.Orbit : Mode.Ar);
        }

        private void OnDestroy()
        {
            if (_modes != null) _modes.ModeChanged -= OnModeChanged;
            _modes = null;
            ARSession.stateChanged -= OnArSessionStateChanged;
        }

        private void OnModeChanged(Mode previous, Mode current) => Refresh();

        private void OnArSessionStateChanged(ARSessionStateChangedEventArgs args) => Refresh();

        private void Refresh()
        {
            if (_button != null) _button.interactable = IsInteractable;
        }
    }
}
