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

            // InputRouter turns TouchSimulation on in the Editor, and that adds a second "Simulated Touchscreen"
            // which would outrank the virtual one below as Touchscreen.current. Drop it for the duration of the test.
            TouchSimulation.Destroy();

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
            yield return null;

            BeginTouch(1, TapPosition);
            yield return null;

            // tapDurationSeconds defaults to 0.3; hold clear of it without moving a pixel.
            var until = Time.unscaledTime + 0.45f;
            while (Time.unscaledTime < until) yield return null;

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
