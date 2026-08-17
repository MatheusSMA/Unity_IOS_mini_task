using System.Collections;
using System.Collections.Generic;
using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Formify.Tests.PlayMode
{
    /// <summary>
    /// TOP-01 AC1, TOP-02 AC6 — the switch flips the mode both ways, the active view's button is the disabled
    /// one, and the list panel plus the Clear button keep working while the plan is up.
    /// </summary>
    [TestFixture]
    public class ViewSwitchButtonsTests
    {
        private const int WallA = 0;
        private const int WallB = 1;
        private const int FloorId = 2;
        private const int CeilingId = 3;

        private RoomModel _model;
        private ModeManager _modes;
        private GameObject _panelGo;
        private SurfaceListPanel _panel;
        private GameObject _clearGo;
        private ClearButton _clearButton;
        private GameObject _switchGo;
        private ViewSwitchButtons _switch;

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
            _model = new RoomModel(new List<SurfaceDefinition>
            {
                Surface(WallA, "Wall 1", SurfaceKind.Wall),
                Surface(WallB, "Wall 2", SurfaceKind.Wall),
                Surface(FloorId, "Floor", SurfaceKind.Floor),
                Surface(CeilingId, "Ceiling", SurfaceKind.Ceiling)
            });
            _modes = new ModeManager(IsWallSelected);

            // The panel owns the application canvas (T19); everything else attaches to it.
            _panelGo = new GameObject("SurfaceListPanelHost");
            _panel = _panelGo.AddComponent<SurfaceListPanel>();
            _panel.Configure(_model);

            _clearGo = new GameObject("ClearButton", typeof(RectTransform));
            _clearGo.AddComponent<Button>();
            _clearButton = _clearGo.AddComponent<ClearButton>();
            _clearButton.Configure(_model, _modes);

            _switchGo = new GameObject("ViewSwitchButtonsHost");
            _switch = _switchGo.AddComponent<ViewSwitchButtons>();
            _switch.Configure(_modes, _panel.Canvas);
        }

        [TearDown]
        public void TearDown()
        {
            // The switch's own buttons live under the panel's canvas, so it goes first.
            if (_switchGo != null) Object.DestroyImmediate(_switchGo);
            if (_clearGo != null) Object.DestroyImmediate(_clearGo);
            if (_panelGo != null) Object.DestroyImmediate(_panelGo);

            _switchGo = null;
            _switch = null;
            _clearGo = null;
            _clearButton = null;
            _panelGo = null;
            _panel = null;
            _modes = null;
            _model = null;

            foreach (EventSystem eventSystem in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                if (eventSystem != null) Object.DestroyImmediate(eventSystem.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator Clicking2D_EntersTopDown_AndClicking3D_ReturnsToOrbit()
        {
            yield return null;

            Assert.AreEqual(Mode.Orbit, _modes.Current, "the app starts in 3D");
            Assert.IsFalse(_switch.IsTwoDActive);

            // Through the Button, so the Configure() wiring is under test too.
            _switch.TwoDButton.onClick.Invoke();

            Assert.AreEqual(Mode.TopDown, _modes.Current);
            Assert.IsTrue(_switch.IsTwoDActive);

            _switch.ThreeDButton.onClick.Invoke();

            Assert.AreEqual(Mode.Orbit, _modes.Current);
            Assert.IsFalse(_switch.IsTwoDActive);
        }

        [UnityTest]
        public IEnumerator ActiveViewsButton_IsTheNonInteractableOne_InBothStates()
        {
            yield return null;

            Assert.IsTrue(_switch.TwoDButton.interactable, "3D is active, so 2D is the pressable one");
            Assert.IsFalse(_switch.ThreeDButton.interactable, "3D is the active view");

            _switch.TwoDButton.onClick.Invoke();

            Assert.IsFalse(_switch.TwoDButton.interactable, "2D is the active view now");
            Assert.IsTrue(_switch.ThreeDButton.interactable, "3D is the way back");
        }

        [UnityTest]
        public IEnumerator InTopDown_ListRowFollowsSelection_AndClearButtonStillClears()
        {
            yield return null;

            _switch.TwoDButton.onClick.Invoke();
            Assert.AreEqual(Mode.TopDown, _modes.Current);

            _model.Select(WallA);

            Assert.AreEqual(WallA, _model.SelectedSurfaceId);
            Assert.IsTrue(_panel.IsRowSelected(WallA), "the list row repaints in 2D as well");
            Assert.IsFalse(_panel.IsRowSelected(WallB));

            _clearButton.OnClick();

            Assert.IsNull(_model.SelectedSurfaceId);
            Assert.IsFalse(_panel.IsRowSelected(WallA), "the row un-marks itself in 2D");
            Assert.AreEqual(Mode.TopDown, _modes.Current, "clearing does not drop out of the plan");
        }
    }
}
