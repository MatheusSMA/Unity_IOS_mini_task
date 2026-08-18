using TMPro;
using UnityEngine;

namespace Formify.Presentation
{
    /// <summary>
    /// One row of the surfaces list (HUD-01 AC3). The selected state is a field on the row, not a suffix parsed
    /// out of the label text, so the visual pass can swap the marker for the art kit's green tag without any
    /// test having to know how the row is painted. <see cref="SurfaceListPanel"/> owns the rows and is the only
    /// caller of <see cref="SetSelected"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class SurfaceRow : MonoBehaviour
    {
        /// <summary>Placeholder marker, rendered until the art kit's tag replaces it.</summary>
        public const string SelectedMarker = "  [SELECTED]";

        private TextMeshProUGUI _label;
        private string _surfaceName;

        /// <summary>The row's own state — the source of truth for "this surface is the selected one".</summary>
        public bool IsSelected { get; private set; }

        /// <summary>The text actually rendered, or null before <see cref="Initialize"/>.</summary>
        public string Text => _label != null ? _label.text : null;

        public TextMeshProUGUI Label => _label;

        public void Initialize(string surfaceName, TextMeshProUGUI label)
        {
            _surfaceName = surfaceName;
            _label = label;
            Repaint();
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            Repaint();
        }

        private void Repaint()
        {
            if (_label == null) return;

            _label.text = IsSelected ? _surfaceName + SelectedMarker : _surfaceName;
        }
    }
}
