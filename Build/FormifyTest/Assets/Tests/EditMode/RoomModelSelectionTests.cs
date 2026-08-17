using System.Collections.Generic;
using Formify.Domain;
using NUnit.Framework;
using UnityEngine;

namespace Formify.Tests.EditMode
{
    [TestFixture]
    public class RoomModelSelectionTests
    {
        private const int WallA = 1;
        private const int WallB = 2;
        private const int Floor = 3;

        private static SurfaceDefinition Surface(int id, SurfaceKind kind, float width, float height)
        {
            return new SurfaceDefinition
            {
                id = id,
                name = kind + "_" + id,
                kind = kind,
                origin = Vector3.zero,
                right = Vector3.right,
                up = Vector3.up,
                width = width,
                height = height,
                thickness = 0.15f
            };
        }

        private static RoomModel NewRoom()
        {
            return new RoomModel(new List<SurfaceDefinition>
            {
                Surface(WallA, SurfaceKind.Wall, 4f, 2.8f),
                Surface(WallB, SurfaceKind.Wall, 4f, 2.8f),
                Surface(Floor, SurfaceKind.Floor, 4f, 4f)
            });
        }

        [Test]
        public void Select_FromEmptyState_SetsSelectionAndRaisesOnce()
        {
            RoomModel room = NewRoom();
            int calls = 0;
            int? previous = -1;
            int? current = -1;
            room.SelectionChanged += (p, c) => { calls++; previous = p; current = c; };

            room.Select(WallA);

            Assert.AreEqual(WallA, room.SelectedSurfaceId);
            Assert.AreEqual(1, calls);
            Assert.IsNull(previous);
            Assert.AreEqual(WallA, current);
        }

        [Test]
        public void Select_DifferentSurface_ReplacesSelectionAndReportsBothIds()
        {
            RoomModel room = NewRoom();
            room.Select(WallA);

            int calls = 0;
            int? previous = null;
            int? current = null;
            room.SelectionChanged += (p, c) => { calls++; previous = p; current = c; };

            room.Select(WallB);

            Assert.AreEqual(WallB, room.SelectedSurfaceId);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(WallA, previous);
            Assert.AreEqual(WallB, current);
        }

        [Test]
        public void Select_AlreadySelectedId_RaisesNothing()
        {
            RoomModel room = NewRoom();
            room.Select(WallA);

            int calls = 0;
            room.SelectionChanged += (p, c) => calls++;

            room.Select(WallA);

            Assert.AreEqual(0, calls);
            Assert.AreEqual(WallA, room.SelectedSurfaceId);
        }

        [Test]
        public void ClearSelection_WithSelection_ClearsAndRaisesWithNullCurrent()
        {
            RoomModel room = NewRoom();
            room.Select(WallB);

            int calls = 0;
            int? previous = null;
            int? current = WallB;
            room.SelectionChanged += (p, c) => { calls++; previous = p; current = c; };

            room.ClearSelection();

            Assert.IsNull(room.SelectedSurfaceId);
            Assert.AreEqual(1, calls);
            Assert.AreEqual(WallB, previous);
            Assert.IsNull(current);
        }

        [Test]
        public void ClearSelection_WhenEmpty_RaisesNothing()
        {
            RoomModel room = NewRoom();
            int calls = 0;
            room.SelectionChanged += (p, c) => calls++;

            room.ClearSelection();
            room.ClearSelection();

            Assert.AreEqual(0, calls);
            Assert.IsNull(room.SelectedSurfaceId);
        }

        [Test]
        public void Select_Sequence_KeepsExactlyTheLastSelectedId()
        {
            RoomModel room = NewRoom();
            int calls = 0;
            room.SelectionChanged += (p, c) => calls++;

            room.Select(WallA);
            room.Select(Floor);
            room.Select(WallB);
            room.Select(WallA);

            Assert.AreEqual(WallA, room.SelectedSurfaceId);
            Assert.AreEqual(4, calls);
        }

        [Test]
        public void Select_UnknownId_LeavesSelectionUntouched()
        {
            RoomModel room = NewRoom();
            room.Select(WallA);

            int calls = 0;
            room.SelectionChanged += (p, c) => calls++;

            room.Select(999);

            Assert.AreEqual(0, calls);
            Assert.AreEqual(WallA, room.SelectedSurfaceId);
            Assert.IsNull(room.GetSurface(999));
        }
    }
}
