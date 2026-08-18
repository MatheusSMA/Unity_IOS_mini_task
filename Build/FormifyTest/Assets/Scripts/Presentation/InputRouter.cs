using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Formify.Presentation
{
    /// <summary>SEL-03 AC5, EDGE-01, EDGE-02. Single doorway for touch: classifies tap vs drag once so no
    /// controller ever re-classifies. It dispatches to nobody and never references ModeManager — controllers
    /// subscribe to these events and gate themselves on ModeManager.Current.</summary>
    public class InputRouter : MonoBehaviour
    {
        [SerializeField] float tapMovePixels = 20f;
        [SerializeField] float tapDurationSeconds = 0.3f;

        public event Action<Vector2> Tapped;
        public event Action<Vector2> DragStart;
        public event Action<Vector2> DragDelta;
        public event Action<Vector2> DragEnd;

        /// <summary>EDGE-02 gate, defaulting to the EventSystem lookup. It is settable only because
        /// IsPointerOverGameObject answers true only after a uGUI input module has processed that pointer id,
        /// which never happens for a virtual touchscreen in batch mode — PlayMode tests replace the gate here.</summary>
        public Func<int, bool> IsPointerOverUi { get; set; } = PointerOverUi;

        /// <summary>Clock behind the tapDurationSeconds cutoff, defaulting to the engine's unscaled time. It is
        /// settable because the only other way to cross that cutoff is to hold a real touch for real seconds,
        /// and EnhancedTouch may drop a stationary touch from activeTouches first — PlayMode tests move the
        /// clock instead, so the reclassification lands on a known frame.</summary>
        public Func<float> UnscaledTime { get; set; } = () => Time.unscaledTime;

        HashSet<int> currentIds = new HashSet<int>();
        HashSet<int> previousIds = new HashSet<int>();
        int trackedId = -1;
        Vector2 startPosition;
        Vector2 lastPosition;
        float startTime;
        bool isDrag;
        bool touchEnabled;

        float MoveThreshold => Screen.dpi > 0f ? tapMovePixels * (Screen.dpi / 160f) : tapMovePixels;

        void OnEnable()
        {
            if (touchEnabled) return;
            touchEnabled = true;
            EnhancedTouchSupport.Enable();
            #if UNITY_EDITOR
            TouchSimulation.Enable();   // EnhancedTouch has no native mouse events; the mouse stands in for touch.
            #endif
        }

        void OnDisable()
        {
            if (!touchEnabled) return;
            touchEnabled = false;
            #if UNITY_EDITOR
            TouchSimulation.Disable();      // no-op when there is no instance
            #endif
            EnhancedTouchSupport.Disable(); // no-op when the input system was already reset under us
            trackedId = -1;
            isDrag = false;
            currentIds.Clear();
            previousIds.Clear();
        }

        void Update()
        {
            if (!EnhancedTouchSupport.enabled) return;

            var touches = Touch.activeTouches;
            currentIds.Clear();
            for (var i = 0; i < touches.Count; i++) currentIds.Add(touches[i].touchId);

            if (trackedId < 0) TryTrack(touches);
            else UpdateTracked(touches);

            var recycled = previousIds;
            previousIds = currentIds;
            currentIds = recycled;
        }

        // EDGE-01: only a finger absent last frame can be picked up, and only while nothing is tracked, so a
        // second finger stays ignored for its whole life instead of being adopted when the first one lifts.
        void TryTrack(ReadOnlyArray<Touch> touches)
        {
            for (var i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch.ended || previousIds.Contains(touch.touchId)) continue;

                if (IsPointerOverUi == null || !IsPointerOverUi(touch.touchId))   // EDGE-02 swallows the whole touch
                {
                    trackedId = touch.touchId;
                    startPosition = lastPosition = touch.screenPosition;
                    startTime = UnscaledTime();
                    isDrag = false;
                }
                return;
            }
        }

        void UpdateTracked(ReadOnlyArray<Touch> touches)
        {
            for (var i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch.touchId != trackedId) continue;

                var position = touch.screenPosition;
                var delta = position - lastPosition;
                lastPosition = position;

                if (!isDrag && ((position - startPosition).magnitude > MoveThreshold ||
                                UnscaledTime() - startTime > tapDurationSeconds))
                {
                    isDrag = true;
                    DragStart?.Invoke(startPosition);
                }

                if (touch.ended) Finish(position);
                else if (isDrag && delta != Vector2.zero) DragDelta?.Invoke(delta);
                return;
            }

            // EnhancedTouch drops a finger from activeTouches one update after its Ended phase, so a touch can
            // vanish without Update ever seeing that phase. Disappearance ends the gesture just the same.
            Finish(lastPosition);
        }

        void Finish(Vector2 position)
        {
            if (isDrag) DragEnd?.Invoke(position);
            else Tapped?.Invoke(position);
            trackedId = -1;
            isDrag = false;
        }

        static bool PointerOverUi(int touchId) =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touchId);
    }
}
