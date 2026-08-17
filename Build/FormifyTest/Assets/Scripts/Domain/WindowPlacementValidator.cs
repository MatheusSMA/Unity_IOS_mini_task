using System.Collections.Generic;
using UnityEngine;

namespace Formify.Domain
{
    /// <summary>Outcome of validating a window rectangle: the clamped rect, or why it was refused.</summary>
    public readonly struct ValidationResult
    {
        public readonly bool IsValid;
        public readonly Rect2D Rect;
        public readonly WindowRejection Rejection;

        ValidationResult(bool isValid, Rect2D rect, WindowRejection rejection)
        {
            IsValid = isValid;
            Rect = rect;
            Rejection = rejection;
        }

        public static ValidationResult Accept(Rect2D rect) => new ValidationResult(true, rect, WindowRejection.None);

        public static ValidationResult Reject(WindowRejection reason) => new ValidationResult(false, default, reason);
    }

    /// <summary>
    /// The single gatekeeper for window placement (WIN-03). Rules run in order: surface kind,
    /// clamp into the wall bounds minus the edge margin, size limits, then overlap.
    /// </summary>
    public class WindowPlacementValidator
    {
        public float MinSize { get; set; } = 0.2f;
        public float MaxSize { get; set; } = 2.0f;
        public float EdgeMargin { get; set; } = 0.1f;

        public ValidationResult Validate(SurfaceDefinition surface, IReadOnlyList<Rect2D> existing, Rect2D candidate)
        {
            if (surface == null)
                return ValidationResult.Reject(WindowRejection.OutOfBounds);

            // Windows belong to walls only (AD-008).
            if (surface.kind != SurfaceKind.Wall)
                return ValidationResult.Reject(WindowRejection.InvalidSurfaceKind);

            candidate = Normalize(candidate);

            // Nothing of the rectangle touches the wall at all.
            if (candidate.XMax <= 0f || candidate.x >= surface.width ||
                candidate.YMax <= 0f || candidate.y >= surface.height)
                return ValidationResult.Reject(WindowRejection.OutOfBounds);

            // Allowed region: wall bounds shrunk by the edge margin on all four sides (WIN AC6).
            float allowedMinX = EdgeMargin;
            float allowedMinY = EdgeMargin;
            float allowedMaxX = surface.width - EdgeMargin;
            float allowedMaxY = surface.height - EdgeMargin;
            if (allowedMaxX <= allowedMinX || allowedMaxY <= allowedMinY)
                return ValidationResult.Reject(WindowRejection.MarginViolation);

            float x0 = Mathf.Max(candidate.x, allowedMinX);
            float y0 = Mathf.Max(candidate.y, allowedMinY);
            float x1 = Mathf.Min(candidate.XMax, allowedMaxX);
            float y1 = Mathf.Min(candidate.YMax, allowedMaxY);

            // The rectangle lies entirely inside the margin band, so nothing survives the clamp (WIN AC9).
            if (x1 <= x0 || y1 <= y0)
                return ValidationResult.Reject(WindowRejection.MarginViolation);

            var clamped = new Rect2D(x0, y0, x1 - x0, y1 - y0);

            if (clamped.width < MinSize || clamped.height < MinSize)
                return ValidationResult.Reject(WindowRejection.TooSmall);

            if (clamped.width > MaxSize || clamped.height > MaxSize)
                return ValidationResult.Reject(WindowRejection.TooLarge);

            if (existing != null)
            {
                for (int i = 0; i < existing.Count; i++)
                {
                    if (clamped.Overlaps(existing[i]))
                        return ValidationResult.Reject(WindowRejection.Overlap);
                }
            }

            return ValidationResult.Accept(clamped);
        }

        /// <summary>A drag can produce negative extents; make the rectangle bottom-left based.</summary>
        static Rect2D Normalize(Rect2D rect)
        {
            if (rect.width < 0f)
            {
                rect.x += rect.width;
                rect.width = -rect.width;
            }

            if (rect.height < 0f)
            {
                rect.y += rect.height;
                rect.height = -rect.height;
            }

            return rect;
        }
    }
}
