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
    /// <summary>LIST-01, LIST-02, EDGE-06 — the panel mirrors RoomModel selection, collapsed or not.</summary>
    [TestFixture]
    public class SurfaceListPanelTests
    {
        private const int WallA = 0;
        private const int WallB = 1;
        private const int WallC = 2;
        private const int WallD = 3;
        private const int FloorId = 4;
        private const int CeilingId = 5;

        /// <summary>Mirrors SurfaceListPanel's row GameObject naming.</summary>
        private const string RowNamePrefix = "Row_";

        private static readonly string[] ExpectedNames = { "Wall 1", "Wall 2", "Wall 3", "Wall 4", "Floor", "Ceiling" };

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
                Surface(WallA, ExpectedNames[0], SurfaceKind.Wall),
                Surface(WallB, ExpectedNames[1], SurfaceKind.Wall),
                Surface(WallC, ExpectedNames[2], SurfaceKind.Wall),
                Surface(WallD, ExpectedNames[3], SurfaceKind.Wall),
                Surface(FloorId, ExpectedNames[4], SurfaceKind.Floor),
                Surface(CeilingId, ExpectedNames[5], SurfaceKind.Ceiling)
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

        /// <summary>Asserts EVERY row, not only the two the last event touched.</summary>
        private void AssertOnlySelected(int? expectedSelectedId)
        {
            foreach (SurfaceDefinition surface in _model.Surfaces)
            {
                bool expected = expectedSelectedId.HasValue && surface.id == expectedSelectedId.Value;
                Assert.AreEqual(expected, _panel.IsRowSelected(surface.id), "IsRowSelected for " + surface.name);
                Assert.AreEqual(
                    expected ? surface.name + SurfaceListPanel.SelectedMarker : surface.name,
                    _panel.GetRowLabel(surface.id),
                    "GetRowLabel for " + surface.name);
            }
        }

        private Dictionary<int, string> SnapshotLabels()
        {
            Dictionary<int, string> snapshot = new Dictionary<int, string>();
            foreach (SurfaceDefinition surface in _model.Surfaces)
            {
                snapshot[surface.id] = _panel.GetRowLabel(surface.id);
            }
            return snapshot;
        }

        [UnityTest]
        public IEnumerator Configure_ListsEverySurfaceOnceInModelOrder()
        {
            yield return null;

            int rowCount = 0;
            foreach (Transform child in _panel.Canvas.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.StartsWith(RowNamePrefix)) rowCount++;
            }

            Assert.AreEqual(6, rowCount, "one row per surface, no extras");
            Assert.AreEqual(6, _model.Surfaces.Count);

            for (int i = 0; i < _model.Surfaces.Count; i++)
            {
                SurfaceDefinition surface = _model.Surfaces[i];
                Assert.AreEqual(ExpectedNames[i], surface.name, "model order at index " + i);
                Assert.AreEqual(ExpectedNames[i], _panel.GetRowLabel(surface.id), "row label at index " + i);
                Assert.IsFalse(_panel.IsRowSelected(surface.id), "nothing is selected yet: " + surface.name);
            }

            Assert.IsNull(_panel.GetRowLabel(999), "no row for an unknown surface id");
            Assert.IsFalse(_panel.IsCollapsed);
        }

        [UnityTest]
        public IEnumerator Select_ThenSelectAnother_ChangesExactlyTheTwoAffectedRows()
        {
            yield return null;

            _model.Select(WallA);
            Assert.AreEqual("Wall 1" + SurfaceListPanel.SelectedMarker, _panel.GetRowLabel(WallA));
            AssertOnlySelected(WallA);

            Dictionary<int, string> before = SnapshotLabels();

            _model.Select(WallB);
            yield return null;

            List<int> changed = new List<int>();
            foreach (SurfaceDefinition surface in _model.Surfaces)
            {
                if (before[surface.id] != _panel.GetRowLabel(surface.id)) changed.Add(surface.id);
            }

            CollectionAssert.AreEquivalent(new[] { WallA, WallB }, changed, "only the two affected rows repaint");
            Assert.AreEqual("Wall 1", _panel.GetRowLabel(WallA));
            Assert.AreEqual("Wall 2" + SurfaceListPanel.SelectedMarker, _panel.GetRowLabel(WallB));
            AssertOnlySelected(WallB);
        }

        [UnityTest]
        public IEnumerator ClearSelection_TurnsTheSelectedRowOffAndLeavesAllRowsUnselected()
        {
            yield return null;

            _model.Select(FloorId);
            AssertOnlySelected(FloorId);

            _model.ClearSelection();
            yield return null;

            Assert.IsNull(_model.SelectedSurfaceId);
            Assert.AreEqual("Floor", _panel.GetRowLabel(FloorId));
            AssertOnlySelected(null);
        }

        [UnityTest]
        public IEnumerator Collapsed_RowsKeepTrackingSelectionAndTheCollapseControlStaysVisible()
        {
            yield return null;

            _model.Select(WallA);
            AssertOnlySelected(WallA);

            _panel.ToggleCollapsed();
            yield return null;

            Assert.IsTrue(_panel.IsCollapsed);

            Button collapseControl = _panel.Canvas.GetComponentInChildren<Button>(true);
            Assert.IsNotNull(collapseControl, "the collapse control exists");
            Assert.IsTrue(collapseControl.gameObject.activeInHierarchy, "collapse control stays visible (LIST AC4)");

            _model.Select(WallC);
            yield return null;

            // EDGE-06: collapsing hides the layout, never the binding.
            Assert.IsTrue(_panel.IsRowSelected(WallC), "row updated while collapsed");
            Assert.IsFalse(_panel.IsRowSelected(WallA), "previous row cleared while collapsed");

            _panel.ToggleCollapsed();
            yield return null;

            Assert.IsFalse(_panel.IsCollapsed);
            Assert.IsTrue(collapseControl.gameObject.activeInHierarchy);
            AssertOnlySelected(WallC);
        }
    }
}
