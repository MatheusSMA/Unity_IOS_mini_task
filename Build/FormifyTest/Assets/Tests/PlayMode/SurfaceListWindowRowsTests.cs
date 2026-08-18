using System.Collections;
using System.Collections.Generic;
using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace Formify.Tests.PlayMode
{
    /// <summary>
    /// LIST-03 — a window is a row under the wall it was cut into: it appears when the window is placed, folds
    /// with its wall, selects like any row, and renumbers when a sibling is deleted. The fixture drives the
    /// model and the row Buttons directly, no pointer events: what is asserted is the binding, not uGUI.
    /// </summary>
    [TestFixture]
    public class SurfaceListWindowRowsTests
    {
        private const int WallA = 0;
        private const int WallB = 1;
        private const int FloorId = 2;

        private const string RowNamePrefix = "Row_";
        private const string WindowRowNamePrefix = "WindowRow_";

        private RoomModel _model;
        private GameObject _panelGo;
        private SurfaceListPanel _panel;

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

        [SetUp]
        public void SetUp()
        {
            _model = new RoomModel(new List<SurfaceDefinition>
            {
                Surface(WallA, "Wall 1", SurfaceKind.Wall),
                Surface(WallB, "Wall 2", SurfaceKind.Wall),
                Surface(FloorId, "Floor", SurfaceKind.Floor)
            });

            _panelGo = new GameObject("SurfaceListPanelHost");
            _panel = _panelGo.AddComponent<SurfaceListPanel>();
            _panel.Configure(_model);
        }

        [TearDown]
        public void TearDown()
        {
            if (_panelGo != null) Object.DestroyImmediate(_panelGo);
            _panelGo = null;
            _panel = null;
            _model = null;

            foreach (EventSystem eventSystem in Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
            {
                if (eventSystem != null) Object.DestroyImmediate(eventSystem.gameObject);
            }
        }

        /// <summary>Inside the allowed region of a 4.0 x 2.8 wall with the validator's 0.1 margin.</summary>
        private int AddWindow(int surfaceId, float x)
        {
            Assert.IsTrue(_model.TryAddWindow(surfaceId, new Rect2D(x, 1f, 1f, 1f), out WindowRejection reason),
                "fixture window was rejected: " + reason);

            IReadOnlyList<WindowSpec> windows = _model.GetWindows(surfaceId);
            return windows[windows.Count - 1].id;
        }

        private Transform Rows => _panel.Canvas.transform.Find("SurfacesPanel/Rows");

        private SurfaceRow WallRow(string surfaceName)
        {
            Transform row = Rows.Find(RowNamePrefix + surfaceName);
            Assert.IsNotNull(row, "no row for " + surfaceName);
            return row.GetComponent<SurfaceRow>();
        }

        private SurfaceRow WindowRow(int windowId)
        {
            Transform row = Rows.Find(WindowRowNamePrefix + windowId);
            Assert.IsNotNull(row, "no row for window " + windowId);
            return row.GetComponent<SurfaceRow>();
        }

        [UnityTest]
        public IEnumerator A_placed_window_becomes_a_row_directly_under_its_wall()
        {
            int windowId = AddWindow(WallA, 1f);

            yield return null;

            SurfaceRow wallRow = WallRow("Wall 1");
            SurfaceRow windowRow = WindowRow(windowId);

            Assert.AreEqual("Window 1", windowRow.Text);
            Assert.AreEqual(wallRow.transform.GetSiblingIndex() + 1, windowRow.transform.GetSiblingIndex(),
                "the window row must sit immediately under its own wall, not at the end of the list");
            Assert.IsTrue(_panel.IsWindowRowShown(windowId));
        }

        [UnityTest]
        public IEnumerator Windows_stay_under_their_own_wall()
        {
            int onA = AddWindow(WallA, 1f);
            int onB = AddWindow(WallB, 1f);

            yield return null;

            Assert.AreEqual(WallRow("Wall 1").transform.GetSiblingIndex() + 1,
                WindowRow(onA).transform.GetSiblingIndex());
            Assert.AreEqual(WallRow("Wall 2").transform.GetSiblingIndex() + 1,
                WindowRow(onB).transform.GetSiblingIndex());
        }

        [UnityTest]
        public IEnumerator Deleting_a_window_removes_its_row_and_renumbers_the_rest()
        {
            int first = AddWindow(WallA, 0.5f);
            int second = AddWindow(WallA, 2.5f);

            yield return null;
            Assert.AreEqual("Window 2", WindowRow(second).Text);

            _model.TryRemoveWindow(first);

            yield return null;

            Assert.IsNull(Rows.Find(WindowRowNamePrefix + first), "the deleted window kept its row");
            Assert.AreEqual("Window 1", WindowRow(second).Text, "the survivor kept the deleted window's number");
        }

        /// <summary>EDGE-06 one level down: folding hides the rows, never the binding.</summary>
        [UnityTest]
        public IEnumerator Collapsing_a_wall_hides_only_its_own_windows()
        {
            int onA = AddWindow(WallA, 1f);
            int onB = AddWindow(WallB, 1f);

            yield return null;

            _panel.ToggleWall(WallA);

            Assert.IsFalse(_panel.IsWallExpanded(WallA));
            Assert.IsFalse(_panel.IsWindowRowShown(onA), "the folded wall still shows its window");
            Assert.IsTrue(_panel.IsWindowRowShown(onB), "folding one wall hid another wall's window");
            Assert.IsTrue(WallRow("Wall 1").gameObject.activeSelf, "the wall row itself must stay visible");

            _panel.ToggleWall(WallA);

            Assert.IsTrue(_panel.IsWallExpanded(WallA));
            Assert.IsTrue(_panel.IsWindowRowShown(onA));
        }

        [UnityTest]
        public IEnumerator A_window_added_to_a_folded_wall_stays_folded()
        {
            AddWindow(WallA, 0.5f);
            yield return null;

            _panel.ToggleWall(WallA);
            int second = AddWindow(WallA, 2.5f);

            yield return null;

            Assert.IsFalse(_panel.IsWindowRowShown(second), "the new row appeared inside a folded wall");
        }

        [UnityTest]
        public IEnumerator The_disclosure_control_shows_up_with_the_first_window()
        {
            SurfaceRow wallRow = WallRow("Wall 1");
            Assert.IsNotNull(wallRow.DiscloseButton, "a wall row is built with its disclosure control");
            Assert.IsFalse(wallRow.DiscloseButton.gameObject.activeSelf,
                "a wall with no windows must not offer to fold them");

            AddWindow(WallA, 1f);

            yield return null;

            Assert.IsTrue(wallRow.DiscloseButton.gameObject.activeSelf);
            Assert.IsNull(WallRow("Floor").DiscloseButton, "only a wall can carry windows");
        }

        [UnityTest]
        public IEnumerator Tapping_a_window_row_selects_the_window_and_drops_the_wall()
        {
            int windowId = AddWindow(WallA, 1f);
            _model.Select(WallA);

            yield return null;

            WindowRow(windowId).Button.onClick.Invoke();

            Assert.AreEqual(windowId, _model.SelectedWindowId);
            Assert.IsNull(_model.SelectedSurfaceId);
            Assert.IsTrue(_panel.IsWindowRowSelected(windowId), "the window row is not marked");
            Assert.IsFalse(_panel.IsRowSelected(WallA), "the wall row stayed marked next to the window");
        }

        [UnityTest]
        public IEnumerator Selecting_a_wall_unmarks_the_window_row()
        {
            int windowId = AddWindow(WallA, 1f);
            _model.SelectWindow(windowId);

            yield return null;
            Assert.IsTrue(_panel.IsWindowRowSelected(windowId));

            WallRow("Wall 2").Button.onClick.Invoke();

            Assert.IsFalse(_panel.IsWindowRowSelected(windowId));
            Assert.IsTrue(_panel.IsRowSelected(WallB));
        }

        /// <summary>AD-015: the tap gate the panel is given covers window rows too, or the lock has a hole.</summary>
        [UnityTest]
        public IEnumerator A_blocked_panel_does_not_select_a_window_row()
        {
            _panel.Configure(_model, () => false);
            int windowId = AddWindow(WallA, 1f);

            yield return null;

            WindowRow(windowId).Button.onClick.Invoke();

            Assert.IsNull(_model.SelectedWindowId);
        }

        [UnityTest]
        public IEnumerator Reconfiguring_redraws_the_windows_already_in_the_model()
        {
            int windowId = AddWindow(WallA, 1f);

            _panel.Configure(_model);

            yield return null;

            Assert.AreEqual("Window 1", _panel.GetWindowRowLabel(windowId));
            Assert.AreEqual(WallRow("Wall 1").transform.GetSiblingIndex() + 1,
                WindowRow(windowId).transform.GetSiblingIndex());
        }
    }
}
