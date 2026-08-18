using System;
using System.Collections.Generic;

namespace Formify.Domain
{
    /// <summary>
    /// Mutable room state. One thing is selected at a time and it is either a surface or a window — selecting
    /// either clears the other, so "what is selected" is never ambiguous (AD-026).
    /// </summary>
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

            ClearWindowSelection();

            int? previous = SelectedSurfaceId;
            SelectedSurfaceId = surfaceId;
            SelectionChanged?.Invoke(previous, surfaceId);
        }

        /// <summary>Idempotent: clearing an empty selection raises nothing. Clears a selected window too.</summary>
        public void ClearSelection()
        {
            ClearWindowSelection();

            if (SelectedSurfaceId == null) return;

            int? previous = SelectedSurfaceId;
            SelectedSurfaceId = null;
            SelectionChanged?.Invoke(previous, null);
        }

        private void ClearSurfaceSelection()
        {
            if (SelectedSurfaceId == null) return;

            int? previous = SelectedSurfaceId;
            SelectedSurfaceId = null;
            SelectionChanged?.Invoke(previous, null);
        }

        // ---- windows ----

        private static readonly WindowSpec[] NoWindows = new WindowSpec[0];

        private readonly Dictionary<int, List<WindowSpec>> _windows = new Dictionary<int, List<WindowSpec>>();
        private int _nextWindowId = 1;

        public event Action<WindowSpec> WindowAdded;
        public event Action<WindowSpec> WindowRemoved;

        /// <summary>The selected window, or null. Never set at the same time as <see cref="SelectedSurfaceId"/>.</summary>
        public int? SelectedWindowId { get; private set; }

        /// <summary>(previous, current), the window counterpart of <see cref="SelectionChanged"/>.</summary>
        public event Action<int?, int?> WindowSelectionChanged;

        public IReadOnlyList<WindowSpec> GetWindows(int surfaceId)
        {
            List<WindowSpec> list;
            return _windows.TryGetValue(surfaceId, out list) ? (IReadOnlyList<WindowSpec>)list : NoWindows;
        }

        /// <summary>The window with this id, from whichever surface holds it. Null when there is none.</summary>
        public WindowSpec GetWindow(int windowId)
        {
            foreach (KeyValuePair<int, List<WindowSpec>> entry in _windows)
            {
                List<WindowSpec> list = entry.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].id == windowId) return list[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Selects a window. Unknown or already selected id is a no-op. A surface selection is cleared first
        /// (AD-026), so a window row and a surface row can never both read as selected.
        /// </summary>
        public void SelectWindow(int windowId)
        {
            if (SelectedWindowId == windowId) return;
            if (GetWindow(windowId) == null) return;

            ClearSurfaceSelection();

            int? previous = SelectedWindowId;
            SelectedWindowId = windowId;
            WindowSelectionChanged?.Invoke(previous, windowId);
        }

        private void ClearWindowSelection()
        {
            if (SelectedWindowId == null) return;

            int? previous = SelectedWindowId;
            SelectedWindowId = null;
            WindowSelectionChanged?.Invoke(previous, null);
        }

        /// <summary>Stores the CLAMPED rect returned by the validator. Rejection leaves the model untouched.</summary>
        public bool TryAddWindow(int surfaceId, Rect2D rect, out WindowRejection reason)
        {
            SurfaceDefinition surface = GetSurface(surfaceId);
            if (surface == null)
            {
                reason = WindowRejection.OutOfBounds;
                return false;
            }

            List<WindowSpec> list;
            _windows.TryGetValue(surfaceId, out list);

            int count = list == null ? 0 : list.Count;
            List<Rect2D> existing = new List<Rect2D>(count);
            for (int i = 0; i < count; i++) existing.Add(list[i].rect);

            ValidationResult result = Validator.Validate(surface, existing, rect);
            if (!result.IsValid)
            {
                reason = result.Rejection;
                return false;
            }

            if (list == null)
            {
                list = new List<WindowSpec>();
                _windows[surfaceId] = list;
            }

            WindowSpec spec = new WindowSpec { id = _nextWindowId++, surfaceId = surfaceId, rect = result.Rect };
            list.Add(spec);
            reason = WindowRejection.None;
            WindowAdded?.Invoke(spec);
            return true;
        }

        public bool TryRemoveWindow(int windowId)
        {
            WindowSpec spec = RemoveWindow(windowId);
            if (spec == null) return false;

            // A selection pointing at a window that no longer exists would outlive it in every view.
            if (SelectedWindowId == windowId) ClearWindowSelection();

            WindowRemoved?.Invoke(spec);
            return true;
        }

        /// <summary>EDGE-05: undo a failed add without echoing a WindowRemoved event to listeners.</summary>
        public void RollbackWindowAdd(int windowId)
        {
            if (SelectedWindowId == windowId) ClearWindowSelection();

            RemoveWindow(windowId);
        }

        /// <summary>EDGE-05: undo a failed remove without echoing a WindowAdded event to listeners.</summary>
        public void RollbackWindowRemove(WindowSpec spec)
        {
            if (spec == null) return;

            List<WindowSpec> list;
            if (!_windows.TryGetValue(spec.surfaceId, out list))
            {
                list = new List<WindowSpec>();
                _windows[spec.surfaceId] = list;
            }

            // ids are monotonic, so inserting by id restores the original ordering.
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].id == spec.id) return;
                if (list[i].id > spec.id)
                {
                    list.Insert(i, spec);
                    return;
                }
            }
            list.Add(spec);
        }

        private WindowSpec RemoveWindow(int windowId)
        {
            foreach (KeyValuePair<int, List<WindowSpec>> entry in _windows)
            {
                List<WindowSpec> list = entry.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].id != windowId) continue;
                    WindowSpec spec = list[i];
                    list.RemoveAt(i);
                    return spec;
                }
            }
            return null;
        }
    }
}
