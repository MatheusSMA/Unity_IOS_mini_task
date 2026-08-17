using System;
using System.Collections.Generic;
using UnityEngine;

namespace Formify.Domain
{
    /// <summary>What a surface is. Windows can only be cut into <see cref="Wall"/> (AD-008).</summary>
    public enum SurfaceKind
    {
        Wall,
        Floor,
        Ceiling
    }

    /// <summary>Axis-aligned rectangle in surface-local meters, origin at the bottom-left corner.</summary>
    [Serializable]
    public struct Rect2D
    {
        public float x;
        public float y;
        public float width;
        public float height;

        public Rect2D(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public float XMax => x + width;
        public float YMax => y + height;

        /// <summary>True when the two rectangles share area. Touching edges do not overlap.</summary>
        public bool Overlaps(Rect2D other)
        {
            return x < other.XMax && other.x < XMax && y < other.YMax && other.y < YMax;
        }

        public override string ToString() => $"({x:0.###}, {y:0.###}, {width:0.###} x {height:0.###})";
    }

    /// <summary>
    /// One selectable surface: a solid slab of <see cref="thickness"/> meters, spanned by
    /// <see cref="right"/> and <see cref="up"/> from <see cref="origin"/> (its local (0,0) corner).
    /// </summary>
    [Serializable]
    public class SurfaceDefinition
    {
        public int id;
        public string name;
        public SurfaceKind kind;
        public Vector3 origin;
        public Vector3 right;
        public Vector3 up;
        public float width;
        public float height;
        public float thickness = 0.15f;

        /// <summary>Front-face direction. The slab is extruded along the opposite direction (AD-009).</summary>
        public Vector3 Normal => Vector3.Cross(right, up).normalized;

        /// <summary>Surface-local 2D point to world space, on the front face.</summary>
        public Vector3 LocalToWorld(float localX, float localY) => origin + right * localX + up * localY;
    }

    /// <summary>A window opening in a wall. The id is stable so deletion can address it (WIN-04).</summary>
    [Serializable]
    public class WindowSpec
    {
        public int id;
        public int surfaceId;
        public Rect2D rect;
    }

    /// <summary>The generated room: N walls plus floor and ceiling.</summary>
    public class RoomDefinition
    {
        public List<SurfaceDefinition> surfaces = new List<SurfaceDefinition>();
    }

    /// <summary>Raw mesh arrays, so mesh generation stays testable without a UnityEngine.Mesh.</summary>
    public class MeshData
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector2[] uvs;
    }

    /// <summary>Why a window rectangle was refused (WIN-03).</summary>
    public enum WindowRejection
    {
        None,
        Overlap,
        TooSmall,
        TooLarge,
        MarginViolation,
        OutOfBounds,
        InvalidSurfaceKind
    }

    /// <summary>Which controller owns input right now (AD-013).</summary>
    public enum Mode
    {
        Orbit,
        WindowDraw,
        Ar,
        TopDown
    }
}
