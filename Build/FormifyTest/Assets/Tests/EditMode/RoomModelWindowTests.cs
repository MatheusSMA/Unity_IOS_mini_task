using System.Collections.Generic;
using Formify.Domain;
using NUnit.Framework;
using UnityEngine;

namespace Formify.Tests.EditMode
{
    [TestFixture]
    public class RoomModelWindowTests
    {
        private const int WallId = 1;
        private const int FloorId = 2;
        private const float Eps = 1e-4f;

        // Wall is 4.0 x 2.8 with a 0.1 edge margin, so the validator's allowed
        // region is [0.1, 3.9] x [0.1, 2.7].
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
                Surface(WallId, SurfaceKind.Wall, 4f, 2.8f),
                Surface(FloorId, SurfaceKind.Floor, 4f, 4f)
            });
        }

        [Test]
        public void TryAddWindow_ValidRect_StoresClampedRectAndRaisesWindowAdded()
        {
            RoomModel room = NewRoom();
            int added = 0;
            WindowSpec raised = null;
            room.WindowAdded += spec => { added++; raised = spec; };

            WindowRejection reason;
            bool ok = room.TryAddWindow(WallId, new Rect2D(0f, 0f, 1f, 1f), out reason);

            Assert.IsTrue(ok);
            Assert.AreEqual(WindowRejection.None, reason);
            Assert.AreEqual(1, added);
            Assert.AreEqual(1, room.GetWindows(WallId).Count);

            WindowSpec stored = room.GetWindows(WallId)[0];
            Assert.AreSame(stored, raised);
            Assert.AreEqual(WallId, stored.surfaceId);
            Assert.AreEqual(0.1f, stored.rect.x, Eps);
            Assert.AreEqual(0.1f, stored.rect.y, Eps);
            Assert.AreEqual(0.9f, stored.rect.width, Eps);
            Assert.AreEqual(0.9f, stored.rect.height, Eps);
        }

        [Test]
        public void TryAddWindow_OnFloor_RejectsWithInvalidSurfaceKind()
        {
            RoomModel room = NewRoom();
            int added = 0;
            room.WindowAdded += spec => added++;

            WindowRejection reason;
            bool ok = room.TryAddWindow(FloorId, new Rect2D(1f, 1f, 0.5f, 0.5f), out reason);

            Assert.IsFalse(ok);
            Assert.AreEqual(WindowRejection.InvalidSurfaceKind, reason);
            Assert.AreEqual(0, added);
            Assert.AreEqual(0, room.GetWindows(FloorId).Count);
        }

        [Test]
        public void TryAddWindow_OverlappingRect_RejectsAndKeepsSingleWindow()
        {
            RoomModel room = NewRoom();
            WindowRejection reason;
            Assert.IsTrue(room.TryAddWindow(WallId, new Rect2D(1f, 1f, 0.5f, 0.5f), out reason));

            int added = 0;
            room.WindowAdded += spec => added++;

            bool ok = room.TryAddWindow(WallId, new Rect2D(1.2f, 1.2f, 0.5f, 0.5f), out reason);

            Assert.IsFalse(ok);
            Assert.AreEqual(WindowRejection.Overlap, reason);
            Assert.AreEqual(0, added);
            Assert.AreEqual(1, room.GetWindows(WallId).Count);
        }

        [Test]
        public void TryAddWindow_BelowMinimumSize_RejectsWithTooSmall()
        {
            RoomModel room = NewRoom();

            WindowRejection reason;
            bool ok = room.TryAddWindow(WallId, new Rect2D(2f, 1f, 0.1f, 0.1f), out reason);

            Assert.IsFalse(ok);
            Assert.AreEqual(WindowRejection.TooSmall, reason);
            Assert.AreEqual(0, room.GetWindows(WallId).Count);
        }

        [Test]
        public void TryAddWindow_TwoNonOverlappingRects_BothStoredWithDistinctIds()
        {
            RoomModel room = NewRoom();

            WindowRejection reason;
            Assert.IsTrue(room.TryAddWindow(WallId, new Rect2D(0.5f, 0.5f, 0.6f, 0.6f), out reason));
            Assert.IsTrue(room.TryAddWindow(WallId, new Rect2D(2f, 0.5f, 0.6f, 0.6f), out reason));

            IReadOnlyList<WindowSpec> windows = room.GetWindows(WallId);
            Assert.AreEqual(2, windows.Count);
            Assert.AreEqual(1, windows[0].id);
            Assert.AreEqual(2, windows[1].id);
            Assert.AreEqual(0.5f, windows[0].rect.x, Eps);
            Assert.AreEqual(2f, windows[1].rect.x, Eps);
        }

        [Test]
        public void TryRemoveWindow_ExistingId_RemovesAndRaisesWindowRemoved()
        {
            RoomModel room = NewRoom();
            WindowRejection reason;
            room.TryAddWindow(WallId, new Rect2D(0.5f, 0.5f, 0.6f, 0.6f), out reason);
            WindowSpec stored = room.GetWindows(WallId)[0];

            int removed = 0;
            WindowSpec raised = null;
            room.WindowRemoved += spec => { removed++; raised = spec; };

            bool ok = room.TryRemoveWindow(stored.id);

            Assert.IsTrue(ok);
            Assert.AreEqual(1, removed);
            Assert.AreSame(stored, raised);
            Assert.AreEqual(0, room.GetWindows(WallId).Count);
        }

        [Test]
        public void TryRemoveWindow_UnknownId_ReturnsFalseAndKeepsList()
        {
            RoomModel room = NewRoom();
            WindowRejection reason;
            room.TryAddWindow(WallId, new Rect2D(0.5f, 0.5f, 0.6f, 0.6f), out reason);

            int removed = 0;
            room.WindowRemoved += spec => removed++;

            bool ok = room.TryRemoveWindow(4242);

            Assert.IsFalse(ok);
            Assert.AreEqual(0, removed);
            Assert.AreEqual(1, room.GetWindows(WallId).Count);
        }

        [Test]
        public void TryAddWindow_UnknownSurface_RejectsWithOutOfBounds()
        {
            RoomModel room = NewRoom();

            WindowRejection reason;
            bool ok = room.TryAddWindow(777, new Rect2D(1f, 1f, 0.5f, 0.5f), out reason);

            Assert.IsFalse(ok);
            Assert.AreEqual(WindowRejection.OutOfBounds, reason);
            Assert.AreEqual(0, room.GetWindows(777).Count);
        }

        [Test]
        public void Rollback_AfterAddAndAfterRemove_RestoresListWithoutEvents()
        {
            RoomModel room = NewRoom();
            WindowRejection reason;
            room.TryAddWindow(WallId, new Rect2D(0.5f, 0.5f, 0.6f, 0.6f), out reason);
            room.TryAddWindow(WallId, new Rect2D(2f, 0.5f, 0.6f, 0.6f), out reason);
            WindowSpec first = room.GetWindows(WallId)[0];
            WindowSpec second = room.GetWindows(WallId)[1];

            int added = 0;
            int removed = 0;
            room.WindowAdded += spec => added++;
            room.WindowRemoved += spec => removed++;

            // rollback of a failed add: the second window disappears silently
            room.RollbackWindowAdd(second.id);

            Assert.AreEqual(1, room.GetWindows(WallId).Count);
            Assert.AreEqual(first.id, room.GetWindows(WallId)[0].id);
            Assert.AreEqual(0, added);
            Assert.AreEqual(0, removed);

            // rollback of a failed remove: the same entry comes back silently
            Assert.IsTrue(room.TryRemoveWindow(first.id));
            Assert.AreEqual(0, room.GetWindows(WallId).Count);
            room.RollbackWindowRemove(first);

            Assert.AreEqual(1, room.GetWindows(WallId).Count);
            WindowSpec restored = room.GetWindows(WallId)[0];
            Assert.AreEqual(first.id, restored.id);
            Assert.AreEqual(first.rect.x, restored.rect.x, Eps);
            Assert.AreEqual(first.rect.y, restored.rect.y, Eps);
            Assert.AreEqual(first.rect.width, restored.rect.width, Eps);
            Assert.AreEqual(first.rect.height, restored.rect.height, Eps);
            Assert.AreEqual(0, added);
            Assert.AreEqual(1, removed);
        }
    }
}
