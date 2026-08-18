using System.Collections.Generic;
using Formify.Domain;
using NUnit.Framework;
using UnityEngine;

namespace Formify.Tests.EditMode
{
    /// <summary>
    /// WIN-03 placement rules. Reference wall: 4.0 m x 2.8 m, so with the 0.1 m edge margin the
    /// allowed region is x in [0.1, 3.9], y in [0.1, 2.7].
    /// </summary>
    [TestFixture]
    public class WindowPlacementValidatorTests
    {
        const float Tolerance = 1e-4f;

        WindowPlacementValidator _validator;
        SurfaceDefinition _wall;

        [SetUp]
        public void SetUp()
        {
            _validator = new WindowPlacementValidator();
            _wall = MakeSurface(SurfaceKind.Wall, 4.0f, 2.8f);
        }

        static SurfaceDefinition MakeSurface(SurfaceKind kind, float width, float height)
        {
            return new SurfaceDefinition
            {
                id = 1,
                name = kind.ToString(),
                kind = kind,
                origin = Vector3.zero,
                right = Vector3.right,
                up = Vector3.up,
                width = width,
                height = height,
                thickness = 0.15f
            };
        }

        static void AssertRect(Rect2D expected, Rect2D actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, "x");
            Assert.AreEqual(expected.y, actual.y, Tolerance, "y");
            Assert.AreEqual(expected.width, actual.width, Tolerance, "width");
            Assert.AreEqual(expected.height, actual.height, Tolerance, "height");
        }

        static readonly IReadOnlyList<Rect2D> NoWindows = new List<Rect2D>();

        [Test]
        public void RectWellInsideWall_IsAcceptedUnchanged()
        {
            var result = _validator.Validate(_wall, NoWindows, new Rect2D(1f, 1f, 1f, 1f));

            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(WindowRejection.None, result.Rejection);
            AssertRect(new Rect2D(1f, 1f, 1f, 1f), result.Rect);
        }

        [Test]
        public void RectCrossingTheMarginBand_IsClampedToTheAllowedRegion()
        {
            var result = _validator.Validate(_wall, NoWindows, new Rect2D(0f, 0f, 1f, 1f));

            Assert.IsTrue(result.IsValid);
            AssertRect(new Rect2D(0.1f, 0.1f, 0.9f, 0.9f), result.Rect);
        }

        [Test]
        public void FloorSurface_IsRejectedAsInvalidSurfaceKind()
        {
            var floor = MakeSurface(SurfaceKind.Floor, 6f, 4f);

            var result = _validator.Validate(floor, NoWindows, new Rect2D(1f, 1f, 1f, 1f));

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(WindowRejection.InvalidSurfaceKind, result.Rejection);
        }

        [Test]
        public void CeilingSurface_IsRejectedAsInvalidSurfaceKind()
        {
            var ceiling = MakeSurface(SurfaceKind.Ceiling, 6f, 4f);

            var result = _validator.Validate(ceiling, NoWindows, new Rect2D(1f, 1f, 1f, 1f));

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(WindowRejection.InvalidSurfaceKind, result.Rejection);
        }

        [Test]
        public void RectBelowMinimumSize_IsRejectedAsTooSmall()
        {
            var result = _validator.Validate(_wall, NoWindows, new Rect2D(1f, 1f, 0.1f, 0.5f));

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(WindowRejection.TooSmall, result.Rejection);
        }

        [Test]
        public void RectAboveMaximumSize_IsRejectedAsTooLarge()
        {
            var result = _validator.Validate(_wall, NoWindows, new Rect2D(0.5f, 0.2f, 2.5f, 2.2f));

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(WindowRejection.TooLarge, result.Rejection);
        }

        [Test]
        public void RectEntirelyInsideTheMarginBand_IsRejectedAsMarginViolation()
        {
            var result = _validator.Validate(_wall, NoWindows, new Rect2D(0f, 0f, 0.05f, 0.05f));

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(WindowRejection.MarginViolation, result.Rejection);
        }

        [Test]
        public void WallTooSmallForAnyMargin_IsRejectedAsMarginViolation()
        {
            var tinyWall = MakeSurface(SurfaceKind.Wall, 0.15f, 0.15f);

            var result = _validator.Validate(tinyWall, NoWindows, new Rect2D(0f, 0f, 0.15f, 0.15f));

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(WindowRejection.MarginViolation, result.Rejection);
        }

        [Test]
        public void RectOverlappingAnExistingWindow_IsRejectedAsOverlap()
        {
            var existing = new List<Rect2D> { new Rect2D(1f, 1f, 1f, 1f) };

            var result = _validator.Validate(_wall, existing, new Rect2D(1.5f, 1.5f, 1f, 1f));

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(WindowRejection.Overlap, result.Rejection);
        }

        [Test]
        public void RectTouchingAnExistingWindowEdge_IsAccepted()
        {
            var existing = new List<Rect2D> { new Rect2D(1f, 1f, 1f, 1f) };

            var result = _validator.Validate(_wall, existing, new Rect2D(2f, 1f, 0.5f, 0.5f));

            Assert.IsTrue(result.IsValid);
            AssertRect(new Rect2D(2f, 1f, 0.5f, 0.5f), result.Rect);
        }

        /// <summary>
        /// Both origins draw the same 0.2 m window, but the clamped extent is a float subtraction: at x = 1.0
        /// it comes out 0.20000005, at x = 0.5 it comes out 0.19999999. The minimum has to accept both, or the
        /// same drag succeeds on one part of the wall and fails on another.
        /// </summary>
        [TestCase(1f, TestName = "RectAtExactlyTheMinimumSize_IsAccepted_DriftingUp")]
        [TestCase(0.5f, TestName = "RectAtExactlyTheMinimumSize_IsAccepted_DriftingDown")]
        public void RectAtExactlyTheMinimumSize_IsAccepted(float origin)
        {
            var result = _validator.Validate(_wall, NoWindows, new Rect2D(origin, origin, 0.2f, 0.2f));

            Assert.IsTrue(result.IsValid, "rejected as " + result.Rejection);
            AssertRect(new Rect2D(origin, origin, 0.2f, 0.2f), result.Rect);
        }

        [Test]
        public void RectJustBelowTheMinimumSize_IsStillRejected()
        {
            var result = _validator.Validate(_wall, NoWindows, new Rect2D(1f, 1f, 0.19f, 0.19f));

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(WindowRejection.TooSmall, result.Rejection);
        }

        [Test]
        public void RectJustAboveTheMaximumSize_IsStillRejected()
        {
            var result = _validator.Validate(_wall, NoWindows, new Rect2D(0.5f, 0.3f, 2.01f, 2.01f));

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(WindowRejection.TooLarge, result.Rejection);
        }

        [Test]
        public void RectAtExactlyTheMaximumSize_IsAccepted()
        {
            var result = _validator.Validate(_wall, NoWindows, new Rect2D(0.5f, 0.3f, 2f, 2f));

            Assert.IsTrue(result.IsValid, "rejected as " + result.Rejection);
            AssertRect(new Rect2D(0.5f, 0.3f, 2f, 2f), result.Rect);
        }

        [Test]
        public void RectSittingExactlyOnTheMargin_IsAccepted()
        {
            var result = _validator.Validate(_wall, NoWindows, new Rect2D(0.1f, 0.1f, 1f, 1f));

            Assert.IsTrue(result.IsValid);
            AssertRect(new Rect2D(0.1f, 0.1f, 1f, 1f), result.Rect);
        }

        [Test]
        public void RectCompletelyOffTheWall_IsRejectedAsOutOfBounds()
        {
            var result = _validator.Validate(_wall, NoWindows, new Rect2D(5f, 1f, 1f, 1f));

            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(WindowRejection.OutOfBounds, result.Rejection);
        }

        [Test]
        public void RectDraggedRightToLeft_IsNormalisedBeforeValidation()
        {
            var result = _validator.Validate(_wall, NoWindows, new Rect2D(2f, 2f, -1f, -1f));

            Assert.IsTrue(result.IsValid);
            AssertRect(new Rect2D(1f, 1f, 1f, 1f), result.Rect);
        }
    }
}
