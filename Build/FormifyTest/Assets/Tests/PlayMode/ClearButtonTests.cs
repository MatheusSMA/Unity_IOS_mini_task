using System.Collections;
using System.Collections.Generic;
using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Formify.Tests.PlayMode
{
    /// <summary>CLR-01 — clear with a selection, clear with none, and the WindowDraw call order (CLR AC3).</summary>
    [TestFixture]
    public class ClearButtonTests
    {
        private const int WallA = 0;
        private const int WallB = 1;
        private const int FloorId = 2;

        private RoomModel _model;
        private ModeManager _modes;
        private GameObject _buttonGo;
        private ClearButton _clearButton;
        private Button _button;
        private readonly List<string> _sequence = new List<string>();

        private static SurfaceDefinition Surface(int id, string name, SurfaceKind kind)
        {
            return new SurfaceDefinition
            {
                id = id,
                name = name,
                kind = kind,
                origin = Vector3.zero,
                right = Vector3.right,
                up = Vector3.up,
                width = 4f,
                height = 2.8f,
                thickness = 0.15f
            };
        }

        private bool IsWallSelected()
        {
            int? selected = _model.SelectedSurfaceId;
            if (!selected.HasValue) return false;

            SurfaceDefinition surface = _model.GetSurface(selected.Value);
            return surface != null && surface.kind == SurfaceKind.Wall;
        }

        [SetUp]
        public void SetUp()
        {
            _sequence.Clear();

            _model = new RoomModel(new List<SurfaceDefinition>
            {
                Surface(WallA, "Wall 1", SurfaceKind.Wall),
                Surface(WallB, "Wall 2", SurfaceKind.Wall),
                Surface(FloorId, "Floor", SurfaceKind.Floor)
            });
            _modes = new ModeManager(IsWallSelected);

            _buttonGo = new GameObject("ClearButton", typeof(RectTransform));
            _button = _buttonGo.AddComponent<Button>();
            _clearButton = _buttonGo.AddComponent<ClearButton>();
            _clearButton.Configure(_model, _modes);
        }

        [TearDown]
        public void TearDown()
        {
            if (_buttonGo != null) Object.DestroyImmediate(_buttonGo);
            _buttonGo = null;
            _button = null;
            _clearButton = null;
            _modes = null;
            _model = null;
            _sequence.Clear();
        }

        [UnityTest]
        public IEnumerator Press_WithSelection_ClearsItAndFiresSelectionChangedOnce()
        {
            _model.Select(WallA);

            int calls = 0;
            int? previous = -1;
            int? current = -1;
            _model.SelectionChanged += (p, c) =>
            {
                calls++;
                previous = p;
                current = c;
            };

            yield return null;

            // through the Button, so the Configure() wiring is under test too
            _button.onClick.Invoke();

            Assert.IsNull(_model.SelectedSurfaceId);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(WallA, previous);
            Assert.IsNull(current);
            Assert.AreEqual(Mode.Orbit, _modes.Current);
        }

        [UnityTest]
        public IEnumerator Press_WithNothingSelected_FiresNoSelectionChangedAtAll()
        {
            int calls = 0;
            _model.SelectionChanged += (p, c) => calls++;

            yield return null;

            _button.onClick.Invoke();
            _clearButton.OnClick();

            Assert.AreEqual(0, calls, "clearing an empty selection is silent");
            Assert.IsNull(_model.SelectedSurfaceId);
            Assert.AreEqual(Mode.Orbit, _modes.Current);
        }

        [UnityTest]
        public IEnumerator Press_InWindowDraw_LeavesTheModeFirstThenClearsTheSelection()
        {
            _model.Select(WallA);
            Assert.IsTrue(_modes.TrySet(Mode.WindowDraw), "a wall is selected, so WindowDraw is legal");
            Assert.AreEqual(Mode.WindowDraw, _modes.Current);

            _modes.DrawCancelRequested += () => _sequence.Add("draw-cancel");
            _modes.ModeChanged += (p, c) => _sequence.Add("mode:" + p + "->" + c);
            _model.SelectionChanged += (p, c) => _sequence.Add("selection-cleared:" + p + "->" + (c.HasValue ? c.Value.ToString() : "null"));

            yield return null;

            _clearButton.OnClick();

            Assert.AreEqual(Mode.Orbit, _modes.Current);
            Assert.IsNull(_model.SelectedSurfaceId);

            // CLR AC3: everything the mode change raises happens BEFORE the selection is cleared.
            Assert.AreEqual(3, _sequence.Count, "draw-cancel + mode-changed + selection-cleared");
            Assert.AreEqual("selection-cleared:" + WallA + "->null", _sequence[2], "the clear is last");

            List<string> beforeTheClear = _sequence.GetRange(0, 2);
            CollectionAssert.Contains(beforeTheClear, "draw-cancel");
            CollectionAssert.Contains(beforeTheClear, "mode:WindowDraw->Orbit");
        }
    }
}
