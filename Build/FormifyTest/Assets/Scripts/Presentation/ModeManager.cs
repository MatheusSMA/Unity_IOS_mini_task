using System;
using Formify.Domain;

namespace Formify.Presentation
{
    /// <summary>AD-013 mode transition matrix. Plain C# so it is EditMode-testable without a scene.</summary>
    public class ModeManager
    {
        readonly Func<bool> isWallSelected;

        public ModeManager(Func<bool> isWallSelected) => this.isWallSelected = isWallSelected;

        public Mode Current { get; private set; } = Mode.Orbit;

        public event Action<Mode, Mode> ModeChanged;
        public event Action DrawCancelRequested;
        public event Action SelectionClearRequested;
        public event Action ArSessionStartRequested;
        public event Action ArSessionEndRequested;

        public bool TrySet(Mode mode)
        {
            if (mode == Current) return true;
            if (!IsLegal(Current, mode)) return false;

            // Event order: side effects first (DrawCancel before SelectionClear), ModeChanged last so
            // every listener observes the final state.
            if (Current == Mode.WindowDraw) DrawCancelRequested?.Invoke();
            if (Current == Mode.Ar) ArSessionEndRequested?.Invoke();
            if (mode == Mode.TopDown) SelectionClearRequested?.Invoke();
            if (mode == Mode.Ar) ArSessionStartRequested?.Invoke();

            var previous = Current;
            Current = mode;
            ModeChanged?.Invoke(previous, mode);
            return true;
        }

        bool IsLegal(Mode from, Mode to)
        {
            switch (to)
            {
                case Mode.Orbit: return true;
                case Mode.TopDown: return true;
                case Mode.Ar: return from == Mode.Orbit;
                case Mode.WindowDraw: return from == Mode.Orbit && isWallSelected != null && isWallSelected();
                default: return false;
            }
        }
    }
}
