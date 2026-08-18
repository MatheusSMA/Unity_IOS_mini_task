using System.Collections;
using System.Globalization;
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
    /// HUD-01 regression, not appearance — how the kit looks is a human check (`validation.md` section 7).
    /// What is asserted here is what a restyle can silently break: decoration that starts eating taps (AC4), a
    /// rail button wired twice so one press cancels itself, and the readout/hint copy going stale (AC2).
    /// </summary>
    public class HudArtKitTests
    {
        private GameObject _cameraGo;
        private GameObject _go;
        private RoomBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _cameraGo = new GameObject("Main Camera", typeof(Camera));
            _cameraGo.tag = "MainCamera";

            _go = new GameObject("RoomBootstrap");
            _bootstrap = _go.AddComponent<RoomBootstrap>();   // Awake composes the HUD here
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            if (_cameraGo != null) Object.DestroyImmediate(_cameraGo);
        }

        /// <summary>
        /// AC4: the scanline overlay covers the whole screen. With its Raycast Target left on it swallows every
        /// touch and selection stops working everywhere, and nothing else in the suite would notice.
        /// </summary>
        [UnityTest]
        public IEnumerator Decoration_OverTheWholeScreen_ConsumesNoTap()
        {
            yield return null;
            yield return null;

            Assert.IsNotNull(Scanlines(), "the kit's scanline overlay is on the canvas");

            List<RaycastResult> hits = RaycastAt(ScreenCentre());
            Assert.AreEqual(0, hits.Count,
                "nothing decorative may sit between the finger and the room at the centre of the screen; hit: " +
                (hits.Count > 0 ? hits[0].gameObject.name : "-"));
        }

        /// <summary>
        /// The counterpart, and the other half of EDGE-02: opaque HUD DOES stop the tap. It also keeps the test
        /// above honest — without a point that hits, "nothing was hit at the centre" would pass on an inert
        /// raycaster.
        /// </summary>
        [UnityTest]
        public IEnumerator Panels_DoConsumeTaps_SoNoTapFallsThroughIntoTheRoom()
        {
            yield return null;
            yield return null;

            AssertBlocks(Panel("SurfacesPanel"), "the surfaces panel");
            AssertBlocks(_bootstrap.Rail, "the right rail");
            AssertBlocks((RectTransform)_bootstrap.Readout.transform, "the readout");
            AssertBlocks((RectTransform)_bootstrap.HintPill.transform, "the hint pill");
            AssertBlocks(Panel("ViewToggle"), "the 2D/3D toggle");
        }

        private void AssertBlocks(RectTransform rect, string what)
        {
            Assert.Greater(RaycastAt(CentreOf(rect)).Count, 0,
                what + " blocks taps (EDGE-02); screen=" + Screen.width + "x" + Screen.height +
                " point=" + CentreOf(rect));
        }

        /// <summary>
        /// One press enters window mode. Both Clear and AR wire their own onClick inside Configure, so a rail
        /// that also adds the listener by hand fires twice — and a double-fired window mode toggle goes
        /// Orbit -> WindowDraw -> Orbit and looks like a dead button.
        /// </summary>
        [UnityTest]
        public IEnumerator RailWindowModeButton_EntersWindowMode_InASingleClick()
        {
            yield return null;

            SurfaceDefinition wall = FirstWall();
            _bootstrap.Model.Select(wall.id);
            yield return null;

            Button button = RailButton("BtnWindowMode");
            Assert.IsTrue(button.interactable, "a wall is selected, so the rail button is live (WIN-01 AC1)");

            button.onClick.Invoke();

            Assert.AreEqual(Mode.WindowDraw, _bootstrap.Modes.Current, "one press, one transition");
            Assert.IsTrue(_bootstrap.WindowMode.IsActive);
            Assert.IsTrue(_bootstrap.WindowMode.StateDot != null && _bootstrap.WindowMode.StateDot.enabled,
                "the art kit's state dot is lit while window mode runs (AD-019)");
        }

        /// <summary>AC2: the readout reports the selected surface, not a mock-up's frozen numbers.</summary>
        [UnityTest]
        public IEnumerator Readout_TracksTheSelection_AndTheWindowCount()
        {
            yield return null;

            HudReadout readout = _bootstrap.Readout;
            Assert.IsNotNull(readout, "the kit's Readout panel is on the canvas");
            Assert.AreEqual("NO SURFACE", readout.Caption, "nothing selected yet");
            Assert.IsFalse(readout.IsCountChipShown);

            SurfaceDefinition wall = FirstWall();
            _bootstrap.Model.Select(wall.id);
            yield return null;

            Assert.AreEqual(wall.name.ToUpperInvariant(), readout.Caption);
            StringAssert.Contains(wall.width.ToString("0.00", CultureInfo.InvariantCulture), readout.Dimensions);
            StringAssert.Contains(wall.height.ToString("0.00", CultureInfo.InvariantCulture), readout.Dimensions);
            Assert.AreEqual("0 windows placed", readout.Helper);
            Assert.IsFalse(readout.IsCountChipShown, "the green count chip only appears from the first window on");

            WindowRejection reason;
            Assert.IsTrue(_bootstrap.Model.TryAddWindow(wall.id, new Rect2D(1f, 1f, 1.2f, 0.9f), out reason),
                "window rejected: " + reason);
            yield return null;

            Assert.AreEqual("window placed", readout.Helper);
            Assert.IsTrue(readout.IsCountChipShown);
        }

        /// <summary>AC2: the hint pill says what the current mode does, so it follows ModeChanged.</summary>
        [UnityTest]
        public IEnumerator HintPill_FollowsTheMode()
        {
            yield return null;

            HudHintPill pill = _bootstrap.HintPill;
            Assert.IsNotNull(pill, "the kit's HintPill is on the canvas");

            string orbit = pill.Text;
            StringAssert.Contains("orbit", orbit);

            Assert.IsTrue(_bootstrap.Modes.TrySet(Mode.TopDown), "TopDown is enterable from Orbit (AD-013)");
            yield return null;

            Assert.AreNotEqual(orbit, pill.Text, "the copy changed with the mode");
            StringAssert.Contains("Plan view", pill.Text);
        }

        // ---- helpers ----

        private SurfaceDefinition FirstWall()
        {
            foreach (SurfaceDefinition surface in _bootstrap.Model.Surfaces)
            {
                if (surface.kind == SurfaceKind.Wall) return surface;
            }

            Assert.Fail("the generated room has no wall");
            return null;
        }

        private Button RailButton(string objectName)
        {
            Transform child = _bootstrap.Rail.Find(objectName);
            Assert.IsNotNull(child, objectName + " is missing from the rail");

            var button = child.GetComponent<Button>();
            Assert.IsNotNull(button, objectName + " has no Button");
            return button;
        }

        private RectTransform Panel(string objectName)
        {
            Transform child = _bootstrap.ListPanel.Canvas.transform.Find(objectName);
            Assert.IsNotNull(child, objectName + " is missing from the canvas");
            return (RectTransform)child;
        }

        private Image Scanlines()
        {
            Transform child = _bootstrap.ListPanel.Canvas.transform.Find("Scanlines");
            return child == null ? null : child.GetComponent<Image>();
        }

        private static Vector2 ScreenCentre() => new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        /// <summary>Screen point of a RectTransform's centre. The canvas is Screen Space - Overlay, so no camera.</summary>
        private static Vector2 CentreOf(RectTransform rect)
        {
            return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        }

        private List<RaycastResult> RaycastAt(Vector2 screenPoint)
        {
            var raycaster = _bootstrap.ListPanel.Canvas.GetComponent<GraphicRaycaster>();
            Assert.IsNotNull(raycaster, "the HUD canvas has a GraphicRaycaster");

            var data = new PointerEventData(EventSystem.current) { position = screenPoint };
            var results = new List<RaycastResult>();
            raycaster.Raycast(data, results);
            return results;
        }
    }
}
