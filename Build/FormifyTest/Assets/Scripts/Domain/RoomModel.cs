using System;
using System.Collections.Generic;

namespace Formify.Domain
{
    /// <summary>Mutable room state. At most one surface is selected at any time.</summary>
    public class RoomModel
    {
        private readonly IReadOnlyList<SurfaceDefinition> _surfaces;

        private WindowPlacementValidator Validator { get; }

        public RoomModel(IReadOnlyList<SurfaceDefinition> surfaces, WindowPlacementValidator validator = null)
        {
            _surfaces = surfaces ?? new SurfaceDefinition[0];
            Validator = validator ?? new WindowPlacementValidator();
        }

        public IReadOnlyList<SurfaceDefinition> Surfaces => _surfaces;

        public int? SelectedSurfaceId { get; private set; }

        /// <summary>(previous, current). current is null when the selection was cleared.</summary>
        public event Action<int?, int?> SelectionChanged;

        public SurfaceDefinition GetSurface(int id)
        {
            for (int i = 0; i < _surfaces.Count; i++)
            {
                if (_surfaces[i] != null && _surfaces[i].id == id) return _surfaces[i];
            }
            return null;
        }

        /// <summary>Unknown or already selected id is a no-op: no state change, no event.</summary>
        public void Select(int surfaceId)
        {
            if (SelectedSurfaceId == surfaceId) return;
            if (GetSurface(surfaceId) == null) return;

            int? previous = SelectedSurfaceId;
            SelectedSurfaceId = surfaceId;
            SelectionChanged?.Invoke(previous, surfaceId);
        }

        /// <summary>Idempotent: clearing an empty selection raises nothing.</summary>
        public void ClearSelection()
        {
            if (SelectedSurfaceId == null) return;

            int? previous = SelectedSurfaceId;
            SelectedSurfaceId = null;
            SelectionChanged?.Invoke(previous, null);
        }
    }
}
