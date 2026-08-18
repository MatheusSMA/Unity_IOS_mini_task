using System.Collections;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.TestTools;

namespace Formify.Tests.PlayMode
{
    /// <summary>T14 — SEL-03 AC5, EDGE-01, EDGE-02. Scene is built in code; touches come from a virtual
    /// Touchscreen driven by InputTestFixture, so nothing depends on focus, rendering or a real device.</summary>
    public class InputRouterTests : InputTestFixture
    {
        static readonly Vector2 TapPosition = new Vector2(120f, 240f);
        static readonly Vector2 DragFrom = new Vector2(100f, 100f);
        static readonly Vector2 DragTo = new Vector2(300f, 100f);

        GameObject routerObject;
        GameObject uiObject;
        InputRouter router;

        int tapped, dragStarted, dragDeltas, dragEnded;
        Vector2 lastTap, lastDragStart, lastDragEnd, deltaSum;

        public override void Setup()
        {
            base.Setup();

            routerObject = new GameObject(nameof(InputRouter));
            router = routerObject.AddComponent<InputRouter>();


            InputSystem.AddDevice<Touchscreen>();

            tapped = dragStarted = dragDeltas = dragEnded = 0;
            lastTap = lastDragStart = lastDragEnd = deltaSum = Vector2.zero;

            router.Tapped += p => { tapped++; lastTap = p; };
            router.DragStart += p => { dragStarted++; lastDragStart = p; };
            router.DragDelta += d => { dragDeltas++; deltaSum += d; };
            router.DragEnd += p => { dragEnded++; lastDragEnd = p; };
        }

        public override void TearDown()
        {
            // DestroyImmediate so OnDisable runs before base.TearDown() resets the input system under it.
            if (routerObject != null) UnityEngine.Object.DestroyImmediate(routerObject);
            if (uiObject != null) UnityEngine.Object.DestroyImmediate(uiObject);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator ShortStillTouchRaisesTappedOnce()
        {
            yield return null;

            BeginTouch(1, TapPosition);
            yield return null;

            EndTouch(1, TapPosition);
            yield return null;

            Assert.That(tapped, Is.EqualTo(1));
            Assert.That(lastTap.x, Is.EqualTo(TapPosition.x).Within(0.5f));
            Assert.That(lastTap.y, Is.EqualTo(TapPosition.y).Within(0.5f));
            Assert.That(dragStarted, Is.EqualTo(0));
            Assert.That(dragDeltas, Is.EqualTo(0));
            Assert.That(dragEnded, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator TouchMovedPastThresholdRaisesDragAndNeverTaps()
        {
            yield return null;

            BeginTouch(1, DragFrom);
            yield return null;

            MoveTouch(1, DragTo);
            yield return null;

            EndTouch(1, DragTo);
            yield return null;

            Assert.That(dragStarted, Is.EqualTo(1));
            Assert.That(lastDragStart.x, Is.EqualTo(DragFrom.x).Within(0.5f));
            Assert.That(lastDragStart.y, Is.EqualTo(DragFrom.y).Within(0.5f));
            Assert.That(dragDeltas, Is.GreaterThanOrEqualTo(1));
            Assert.That(deltaSum.x, Is.EqualTo(DragTo.x - DragFrom.x).Within(0.5f));
            Assert.That(deltaSum.y, Is.EqualTo(DragTo.y - DragFrom.y).Within(0.5f));
            Assert.That(dragEnded, Is.EqualTo(1));
            Assert.That(lastDragEnd.x, Is.EqualTo(DragTo.x).Within(0.5f));
            Assert.That(lastDragEnd.y, Is.EqualTo(DragTo.y).Within(0.5f));
            Assert.That(tapped, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator TouchHeldPastTapDurationIsClassifiedAsDrag()
        {
            // The hold is on InputRouter's clock seam, not on wall-clock time: spinning on Time.unscaledTime
            // kept the stationary touch alive for ~30 frames, and EnhancedTouch sometimes dropped it from
            // activeTouches first, which ends the gesture as a tap. Here the cutoff is crossed on one known frame.
            Assert.That(router.UnscaledTime, Is.Not.Null, "the seam has a real default");
            var now = 0f;
            router.UnscaledTime = () => now;

            yield return null;

            BeginTouch(1, TapPosition);
            yield return null;

            Assert.That(dragStarted, Is.EqualTo(0), "still inside the tap window at t = 0");

            // tapDurationSeconds defaults to 0.3; step clear of it without moving a pixel.
            now = 0.45f;
            yield return null;

            Assert.That(dragStarted, Is.EqualTo(1));
            Assert.That(lastDragStart.x, Is.EqualTo(TapPosition.x).Within(0.5f));
            Assert.That(lastDragStart.y, Is.EqualTo(TapPosition.y).Within(0.5f));

            EndTouch(1, TapPosition);
            yield return null;

            Assert.That(dragEnded, Is.EqualTo(1));
            Assert.That(lastDragEnd.x, Is.EqualTo(TapPosition.x).Within(0.5f));
            Assert.That(lastDragEnd.y, Is.EqualTo(TapPosition.y).Within(0.5f));
            Assert.That(dragDeltas, Is.EqualTo(0));
            Assert.That(tapped, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator SecondFingerProducesNoEvents()
        {
            yield return null;

            BeginTouch(1, TapPosition);
            yield return null;

            BeginTouch(2, DragTo);
            yield return null;

            EndTouch(1, TapPosition);
            yield return null;

            // Everything the first finger alone would have produced, and nothing else (EDGE-01).
            Assert.That(tapped, Is.EqualTo(1));
            Assert.That(lastTap.x, Is.EqualTo(TapPosition.x).Within(0.5f));
            Assert.That(lastTap.y, Is.EqualTo(TapPosition.y).Within(0.5f));

            EndTouch(2, DragTo);
            yield return null;

            Assert.That(tapped, Is.EqualTo(1));
            Assert.That(dragStarted, Is.EqualTo(0));
            Assert.That(dragDeltas, Is.EqualTo(0));
            Assert.That(dragEnded, Is.EqualTo(0));
        }

        // ---- AD-030: the same gestures from a mouse, for the Game view and any desktop build ----

        [UnityTest]
        public IEnumerator MouseClickRaisesTappedOnce()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            yield return null;

            Set(mouse.position, TapPosition);
            Press(mouse.leftButton);
            yield return null;

            Release(mouse.leftButton);
            yield return null;

            Assert.That(tapped, Is.EqualTo(1));
            Assert.That(lastTap.x, Is.EqualTo(TapPosition.x).Within(0.5f));
            Assert.That(lastTap.y, Is.EqualTo(TapPosition.y).Within(0.5f));
            Assert.That(dragStarted, Is.EqualTo(0));
            Assert.That(dragEnded, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator MouseDraggedPastThresholdRaisesDragAndNeverTaps()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            yield return null;

            Set(mouse.position, DragFrom);
            Press(mouse.leftButton);
            yield return null;

            Set(mouse.position, DragTo);
            yield return null;

            Release(mouse.leftButton);
            yield return null;

            Assert.That(dragStarted, Is.EqualTo(1));
            Assert.That(dragEnded, Is.EqualTo(1));
            Assert.That(tapped, Is.EqualTo(0), "a drag never taps");
            Assert.That(lastDragStart.x, Is.EqualTo(DragFrom.x).Within(0.5f));
            Assert.That(lastDragEnd.x, Is.EqualTo(DragTo.x).Within(0.5f));
        }

        /// <summary>A finger wins: the mouse must never open a second gesture underneath a touch (EDGE-01).</summary>
        [UnityTest]
        public IEnumerator MouseIsIgnoredWhileAFingerIsDown()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            yield return null;

            BeginTouch(1, TapPosition);
            yield return null;

            Set(mouse.position, DragTo);
            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return null;

            EndTouch(1, TapPosition);
            yield return null;

            Assert.That(tapped, Is.EqualTo(1), "the finger's tap, and nothing from the mouse");
            Assert.That(lastTap.x, Is.EqualTo(TapPosition.x).Within(0.5f));
            Assert.That(dragStarted, Is.EqualTo(0));
        }

        /// <summary>EDGE-02 holds for the mouse too, asked with uGUI's own left-button pointer id.</summary>
        [UnityTest]
        public IEnumerator MousePressOverUiProducesNoEvents()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            router.IsPointerOverUi = id => id == -1;

            yield return null;

            Set(mouse.position, TapPosition);
            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return null;

            Assert.That(tapped, Is.EqualTo(0));
            Assert.That(dragStarted, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator TouchBeginningOverUiProducesNoEvents()
        {
            // EDGE-02. A real Canvas + Image + EventSystem cannot make IsPointerOverGameObject answer true here:
            // it only answers true once a uGUI input module has processed that pointer id, and no module consumes
            // a virtual Touchscreen in batch mode (see DefaultUiGateAsksTheEventSystem). So the gate is injected
            // through InputRouter.IsPointerOverUi, the seam whose default is that same EventSystem lookup.
            Assert.That(router.IsPointerOverUi, Is.Not.Null);
            router.IsPointerOverUi = _ => true;

            yield return null;

            BeginTouch(1, TapPosition);
            yield return null;

            MoveTouch(1, DragTo);
            yield return null;

            EndTouch(1, DragTo);
            yield return null;

            Assert.That(tapped, Is.EqualTo(0));
            Assert.That(dragStarted, Is.EqualTo(0));
            Assert.That(dragDeltas, Is.EqualTo(0));
            Assert.That(dragEnded, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator DefaultUiGateAsksTheEventSystem()
        {
            uiObject = new GameObject("EventSystem", typeof(EventSystem));
            yield return null;

            // No input module has processed touch 1, so the default gate is open and the tap goes through.
            Assert.That(EventSystem.current, Is.Not.Null);
            Assert.That(EventSystem.current.IsPointerOverGameObject(1), Is.False);

            BeginTouch(1, TapPosition);
            yield return null;

            EndTouch(1, TapPosition);
            yield return null;

            Assert.That(tapped, Is.EqualTo(1));
            Assert.That(lastTap.x, Is.EqualTo(TapPosition.x).Within(0.5f));
            Assert.That(lastTap.y, Is.EqualTo(TapPosition.y).Within(0.5f));
        }
    }
}
