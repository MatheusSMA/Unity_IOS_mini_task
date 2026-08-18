using System.Collections.Generic;
using Formify.Domain;
using NUnit.Framework;
using UnityEngine;

namespace Formify.Tests.EditMode
{
    /// <summary>
    /// AD-026 — one selection, two kinds. A window and a surface must never read as selected at the same time,
    /// or every view that paints "the selected thing" has to invent its own tie-break.
    /// </summary>
    [TestFixture]
    public class RoomModelWindowSelectionTests
    {
        private const int WallId = 1;
        private const int OtherWallId = 2;

        private static SurfaceDefinition Surface(int id, SurfaceKind kind)
        {
            return new SurfaceDefinition
            {
                id = id,
                name = kind + "_" + id,
                kind = kind,
                origin = Vector3.zero,
                right = Vector3.right,
                up = Vector3.up,
                width = 4f,
                height = 2.8f,
                thickness = 0.15f
            };
        }

        private static RoomModel NewRoom()
        {
            return new RoomModel(new List<SurfaceDefinition>
            {
                Surface(WallId, SurfaceKind.Wall),
                Surface(OtherWallId, SurfaceKind.Wall)
            });
        }

        /// <summary>Inside the allowed region of a 4.0 x 2.8 wall with a 0.1 margin.</summary>
        private static int AddWindow(RoomModel model, int surfaceId, float x)
        {
            Assert.IsTrue(model.TryAddWindow(surfaceId, new Rect2D(x, 1f, 1f, 1f), out WindowRejection reason),
                "fixture window was rejected: " + reason);

            IReadOnlyList<WindowSpec> windows = model.GetWindows(surfaceId);
            return windows[windows.Count - 1].id;
        }

        [Test]
        public void Selecting_a_window_clears_the_surface_selection()
        {
            RoomModel model = NewRoom();
            int windowId = AddWindow(model, WallId, 1f);
            model.Select(WallId);

            int? clearedTo = 0;
            model.SelectionChanged += (previous, current) => clearedTo = current;

            model.SelectWindow(windowId);

            Assert.AreEqual(windowId, model.SelectedWindowId);
            Assert.IsNull(model.SelectedSurfaceId, "the wall stayed selected next to the window");
            Assert.IsNull(clearedTo, "SelectionChanged must report the wall going away");
        }

        [Test]
        public void Selecting_a_surface_clears_the_window_selection()
        {
            RoomModel model = NewRoom();
            int windowId = AddWindow(model, WallId, 1f);
            model.SelectWindow(windowId);

            int? clearedTo = 0;
            model.WindowSelectionChanged += (previous, current) => clearedTo = current;

            model.Select(OtherWallId);

            Assert.AreEqual(OtherWallId, model.SelectedSurfaceId);
            Assert.IsNull(model.SelectedWindowId, "the window stayed selected next to the wall");
            Assert.IsNull(clearedTo, "WindowSelectionChanged must report the window going away");
        }

        [Test]
        public void Selecting_an_unknown_window_changes_nothing()
        {
            RoomModel model = NewRoom();
            model.Select(WallId);

            int events = 0;
            model.WindowSelectionChanged += (previous, current) => events++;

            model.SelectWindow(4242);

            Assert.IsNull(model.SelectedWindowId);
            Assert.AreEqual(WallId, model.SelectedSurfaceId, "an unknown window id must not clear the wall");
            Assert.AreEqual(0, events);
        }

        [Test]
        public void Selecting_the_same_window_twice_raises_one_event()
        {
            RoomModel model = NewRoom();
            int windowId = AddWindow(model, WallId, 1f);

            int events = 0;
            model.WindowSelectionChanged += (previous, current) => events++;

            model.SelectWindow(windowId);
            model.SelectWindow(windowId);

            Assert.AreEqual(1, events);
        }

        [Test]
        public void Clear_selection_clears_a_selected_window()
        {
            RoomModel model = NewRoom();
            int windowId = AddWindow(model, WallId, 1f);
            model.SelectWindow(windowId);

            model.ClearSelection();

            Assert.IsNull(model.SelectedWindowId);
            Assert.IsNull(model.SelectedSurfaceId);
        }

        [Test]
        public void Clearing_an_empty_selection_raises_nothing()
        {
            RoomModel model = NewRoom();
            AddWindow(model, WallId, 1f);

            int events = 0;
            model.SelectionChanged += (previous, current) => events++;
            model.WindowSelectionChanged += (previous, current) => events++;

            model.ClearSelection();

            Assert.AreEqual(0, events);
        }

        /// <summary>A selection pointing at a deleted window would outlive it in every view that paints it.</summary>
        [Test]
        public void Removing_the_selected_window_clears_the_selection_before_it_announces_the_removal()
        {
            RoomModel model = NewRoom();
            int windowId = AddWindow(model, WallId, 1f);
            model.SelectWindow(windowId);

            int? seenDuringRemoval = windowId;
            model.WindowRemoved += spec => seenDuringRemoval = model.SelectedWindowId;

            Assert.IsTrue(model.TryRemoveWindow(windowId));

            Assert.IsNull(model.SelectedWindowId);
            Assert.IsNull(seenDuringRemoval, "WindowRemoved fired while the model still pointed at the window");
        }

        [Test]
        public void Removing_another_window_leaves_the_selection_alone()
        {
            RoomModel model = NewRoom();
            int first = AddWindow(model, WallId, 1f);
            int second = AddWindow(model, WallId, 2.5f);
            model.SelectWindow(second);

            Assert.IsTrue(model.TryRemoveWindow(first));

            Assert.AreEqual(second, model.SelectedWindowId);
        }

        [Test]
        public void GetWindow_finds_a_window_on_any_surface()
        {
            RoomModel model = NewRoom();
            int onOther = AddWindow(model, OtherWallId, 1f);

            WindowSpec spec = model.GetWindow(onOther);

            Assert.IsNotNull(spec);
            Assert.AreEqual(OtherWallId, spec.surfaceId);
            Assert.IsNull(model.GetWindow(4242));
        }
    }
}
