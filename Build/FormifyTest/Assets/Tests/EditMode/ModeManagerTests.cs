using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;

namespace Formify.Tests.EditMode
{
    [TestFixture]
    public class ModeManagerTests
    {
        ModeManager manager;
        bool wallSelected;
        int drawCancel, selectionClear, arStart, arEnd, modeChanged;
        Mode lastPrevious, lastCurrent;

        [SetUp]
        public void SetUp()
        {
            wallSelected = true;
            manager = new ModeManager(() => wallSelected);
            manager.DrawCancelRequested += () => drawCancel++;
            manager.SelectionClearRequested += () => selectionClear++;
            manager.ArSessionStartRequested += () => arStart++;
            manager.ArSessionEndRequested += () => arEnd++;
            manager.ModeChanged += (p, c) => { modeChanged++; lastPrevious = p; lastCurrent = c; };
            ResetCounters();
        }

        void ResetCounters() => drawCancel = selectionClear = arStart = arEnd = modeChanged = 0;

        /// <summary>Drives the manager to a starting mode through legal transitions, then zeroes the counters.</summary>
        void StartFrom(Mode mode)
        {
            Assert.IsTrue(manager.TrySet(mode), "setup transition to " + mode);
            Assert.AreEqual(mode, manager.Current);
            ResetCounters();
        }

        void AssertEvents(int expectedDrawCancel, int expectedSelectionClear,
                          int expectedArStart, int expectedArEnd, int expectedModeChanged)
        {
            Assert.AreEqual(expectedDrawCancel, drawCancel, "DrawCancelRequested");
            Assert.AreEqual(expectedSelectionClear, selectionClear, "SelectionClearRequested");
            Assert.AreEqual(expectedArStart, arStart, "ArSessionStartRequested");
            Assert.AreEqual(expectedArEnd, arEnd, "ArSessionEndRequested");
            Assert.AreEqual(expectedModeChanged, modeChanged, "ModeChanged");
        }

        // --- From Orbit -------------------------------------------------------

        [Test]
        public void OrbitToWindowDraw_WithWallSelected_IsLegalAndSilent()
        {
            Assert.IsTrue(manager.TrySet(Mode.WindowDraw));
            Assert.AreEqual(Mode.WindowDraw, manager.Current);
            AssertEvents(0, 0, 0, 0, 1);
            Assert.AreEqual(Mode.Orbit, lastPrevious);
            Assert.AreEqual(Mode.WindowDraw, lastCurrent);
        }

        [Test]
        public void OrbitToWindowDraw_WithoutWallSelected_IsRejected()
        {
            wallSelected = false;

            Assert.IsFalse(manager.TrySet(Mode.WindowDraw));
            Assert.AreEqual(Mode.Orbit, manager.Current);
            AssertEvents(0, 0, 0, 0, 0);
        }

        [Test]
        public void OrbitToAr_StartsArSession()
        {
            Assert.IsTrue(manager.TrySet(Mode.Ar));
            Assert.AreEqual(Mode.Ar, manager.Current);
            AssertEvents(0, 0, 1, 0, 1);
        }

        [Test]
        public void OrbitToTopDown_ClearsSelection()
        {
            Assert.IsTrue(manager.TrySet(Mode.TopDown));
            Assert.AreEqual(Mode.TopDown, manager.Current);
            AssertEvents(0, 1, 0, 0, 1);
        }

        // --- From WindowDraw --------------------------------------------------

        [Test]
        public void WindowDrawToOrbit_CancelsDraw()
        {
            StartFrom(Mode.WindowDraw);

            Assert.IsTrue(manager.TrySet(Mode.Orbit));
            Assert.AreEqual(Mode.Orbit, manager.Current);
            AssertEvents(1, 0, 0, 0, 1);
        }

        [Test]
        public void WindowDrawToAr_IsIllegal()
        {
            StartFrom(Mode.WindowDraw);

            Assert.IsFalse(manager.TrySet(Mode.Ar));
            Assert.AreEqual(Mode.WindowDraw, manager.Current);
            AssertEvents(0, 0, 0, 0, 0);
        }

        [Test]
        public void WindowDrawToTopDown_CancelsDrawAndClearsSelection()
        {
            StartFrom(Mode.WindowDraw);

            Assert.IsTrue(manager.TrySet(Mode.TopDown));
            Assert.AreEqual(Mode.TopDown, manager.Current);
            AssertEvents(1, 1, 0, 0, 1);
        }

        // --- From Ar ----------------------------------------------------------

        [Test]
        public void ArToOrbit_EndsArSession()
        {
            StartFrom(Mode.Ar);

            Assert.IsTrue(manager.TrySet(Mode.Orbit));
            Assert.AreEqual(Mode.Orbit, manager.Current);
            AssertEvents(0, 0, 0, 1, 1);
        }

        [Test]
        public void ArToWindowDraw_IsIllegal_EvenWithWallSelected()
        {
            StartFrom(Mode.Ar);

            Assert.IsFalse(manager.TrySet(Mode.WindowDraw));
            Assert.AreEqual(Mode.Ar, manager.Current);
            AssertEvents(0, 0, 0, 0, 0);
        }

        [Test]
        public void ArToTopDown_EndsArSessionAndClearsSelection()
        {
            StartFrom(Mode.Ar);

            Assert.IsTrue(manager.TrySet(Mode.TopDown));
            Assert.AreEqual(Mode.TopDown, manager.Current);
            AssertEvents(0, 1, 0, 1, 1);
        }

        // --- From TopDown -----------------------------------------------------

        [Test]
        public void TopDownToOrbit_IsLegalAndSilent()
        {
            StartFrom(Mode.TopDown);

            Assert.IsTrue(manager.TrySet(Mode.Orbit));
            Assert.AreEqual(Mode.Orbit, manager.Current);
            AssertEvents(0, 0, 0, 0, 1);
        }

        [Test]
        public void TopDownToWindowDraw_IsIllegal()
        {
            StartFrom(Mode.TopDown);

            Assert.IsFalse(manager.TrySet(Mode.WindowDraw));
            Assert.AreEqual(Mode.TopDown, manager.Current);
            AssertEvents(0, 0, 0, 0, 0);
        }

        [Test]
        public void TopDownToAr_IsIllegal()
        {
            StartFrom(Mode.TopDown);

            Assert.IsFalse(manager.TrySet(Mode.Ar));
            Assert.AreEqual(Mode.TopDown, manager.Current);
            AssertEvents(0, 0, 0, 0, 0);
        }

        // --- Misc -------------------------------------------------------------

        [Test]
        public void StartsInOrbit()
        {
            Assert.AreEqual(Mode.Orbit, manager.Current);
        }

        [Test]
        public void SettingCurrentMode_ReturnsTrueAndRaisesNothing()
        {
            Assert.IsTrue(manager.TrySet(Mode.Orbit));
            AssertEvents(0, 0, 0, 0, 0);

            StartFrom(Mode.TopDown);
            Assert.IsTrue(manager.TrySet(Mode.TopDown));
            Assert.AreEqual(Mode.TopDown, manager.Current);
            AssertEvents(0, 0, 0, 0, 0);
        }

        [Test]
        public void NullPredicate_BlocksWindowDrawEntry()
        {
            var noSelection = new ModeManager(null);

            Assert.IsFalse(noSelection.TrySet(Mode.WindowDraw));
            Assert.AreEqual(Mode.Orbit, noSelection.Current);
        }
    }
}
