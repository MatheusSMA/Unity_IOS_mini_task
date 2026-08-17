using System.Collections.Generic;
using UnityEngine;

namespace Formify.Domain
{
    /// <summary>
    /// Builds a solid slab mesh for a surface with real openings cut out for each hole
    /// (WIN-02 geometry, AD-009). Openings are produced by grid slicing the face along the
    /// hole edges and dropping the tiles that fall inside a hole, then adding reveal faces.
    ///
    /// Output is SURFACE-LOCAL: X along <c>surface.right</c> in [0, width], Y along
    /// <c>surface.up</c> in [0, height], Z along <c>surface.Normal</c>. The front face sits at
    /// z = 0 and faces +Z; the slab is extruded back to z = -thickness. The presentation layer
    /// places the GameObject with that basis, so origin/right/up are never applied here.
    ///
    /// WINDING CONVENTION: for every emitted triangle (v0, v1, v2),
    /// <c>Vector3.Cross(v1 - v0, v2 - v0)</c> points along the visible side of that face.
    /// In Unity's left-handed space that is the clockwise-from-the-front ordering, so the front
    /// face (cross = +Z) is visible from the +Normal side, i.e. from inside the room.
    /// </summary>
    public static class SurfaceMeshBuilder
    {
        private const float Eps = 1e-4f;

        /// <summary>
        /// Returns the slab mesh, or null when the input cannot produce a usable mesh (EDGE-05).
        /// Null is the signal for the caller to keep the previous mesh and collider.
        /// </summary>
        public static MeshData Build(SurfaceDefinition surface, IReadOnlyList<Rect2D> holes)
        {
            if (surface == null || surface.width <= 0f || surface.height <= 0f || surface.thickness <= 0f)
                return null;

            float w = surface.width;
            float h = surface.height;
            float t = surface.thickness;
            int holeCount = holes == null ? 0 : holes.Count;

            // Cut coordinates: the surface bounds plus every hole edge.
            var xs = new List<float> { 0f, w };
            var ys = new List<float> { 0f, h };
            for (int i = 0; i < holeCount; i++)
            {
                Rect2D r = holes[i];
                if (r.width <= 0f || r.height <= 0f) return null;
                if (r.x < -Eps || r.y < -Eps || r.XMax > w + Eps || r.YMax > h + Eps) return null;

                xs.Add(r.x);
                xs.Add(r.XMax);
                ys.Add(r.y);
                ys.Add(r.YMax);
            }
            SortAndDedupe(xs);
            SortAndDedupe(ys);

            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();

            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
                      Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                int i0 = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
                uvs.Add(ua); uvs.Add(ub); uvs.Add(uc); uvs.Add(ud);
                tris.Add(i0); tris.Add(i0 + 1); tris.Add(i0 + 2);
                tris.Add(i0); tris.Add(i0 + 2); tris.Add(i0 + 3);
            }

            // Side/reveal quad with unit UVs. `flip` reverses the loop, flipping the facing.
            void Side(Vector3 a, Vector3 b, Vector3 c, Vector3 d, bool flip)
            {
                Vector2 u0 = new Vector2(0f, 0f);
                Vector2 u1 = new Vector2(1f, 0f);
                Vector2 u2 = new Vector2(1f, 1f);
                Vector2 u3 = new Vector2(0f, 1f);
                if (flip) Quad(d, c, b, a, u0, u1, u2, u3);
                else Quad(a, b, c, d, u0, u1, u2, u3);
            }

            // The 4 quads joining the front edge of a rectangle to its back edge.
            // As written they face INTO the rectangle (reveal orientation); `outward` flips them.
            void Ring(float x0, float y0, float x1, float y1, bool outward)
            {
                Side(new Vector3(x0, y0, -t), new Vector3(x0, y1, -t), new Vector3(x0, y1, 0f), new Vector3(x0, y0, 0f), outward); // left   -> +X
                Side(new Vector3(x1, y0, -t), new Vector3(x1, y0, 0f), new Vector3(x1, y1, 0f), new Vector3(x1, y1, -t), outward); // right  -> -X
                Side(new Vector3(x0, y0, -t), new Vector3(x0, y0, 0f), new Vector3(x1, y0, 0f), new Vector3(x1, y0, -t), outward); // bottom -> +Y
                Side(new Vector3(x0, y1, -t), new Vector3(x1, y1, -t), new Vector3(x1, y1, 0f), new Vector3(x0, y1, 0f), outward); // top    -> -Y
            }

            // 1 + 2: front and back faces, tile by tile. Holes are axis-aligned and
            // non-overlapping, so "centre inside a hole" is an exact test for the whole tile.
            for (int xi = 0; xi + 1 < xs.Count; xi++)
            {
                float x0 = xs[xi];
                float x1 = xs[xi + 1];
                for (int yi = 0; yi + 1 < ys.Count; yi++)
                {
                    float y0 = ys[yi];
                    float y1 = ys[yi + 1];
                    if (InsideAnyHole((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, holes, holeCount)) continue;

                    Vector2 uv00 = new Vector2(x0 / w, y0 / h);
                    Vector2 uv10 = new Vector2(x1 / w, y0 / h);
                    Vector2 uv11 = new Vector2(x1 / w, y1 / h);
                    Vector2 uv01 = new Vector2(x0 / w, y1 / h);

                    // front, faces +Z
                    Quad(new Vector3(x0, y0, 0f), new Vector3(x1, y0, 0f), new Vector3(x1, y1, 0f), new Vector3(x0, y1, 0f),
                         uv00, uv10, uv11, uv01);
                    // back, reversed loop -> faces -Z
                    Quad(new Vector3(x0, y1, -t), new Vector3(x1, y1, -t), new Vector3(x1, y0, -t), new Vector3(x0, y0, -t),
                         uv01, uv11, uv10, uv00);
                }
            }

            // 3: outer slab sides, facing away from the surface.
            Ring(0f, 0f, w, h, true);

            // 4: reveal faces, facing into each opening.
            for (int i = 0; i < holeCount; i++)
            {
                Rect2D r = holes[i];
                Ring(r.x, r.y, r.XMax, r.YMax, false);
            }

            if (tris.Count == 0) return null;
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                if (!IsFinite(v.x) || !IsFinite(v.y) || !IsFinite(v.z)) return null;
            }

            return new MeshData
            {
                vertices = verts.ToArray(),
                triangles = tris.ToArray(),
                uvs = uvs.ToArray()
            };
        }

        private static void SortAndDedupe(List<float> values)
        {
            values.Sort();
            for (int i = values.Count - 1; i > 0; i--)
            {
                if (values[i] - values[i - 1] <= Eps) values.RemoveAt(i);
            }
        }

        private static bool InsideAnyHole(float x, float y, IReadOnlyList<Rect2D> holes, int holeCount)
        {
            for (int i = 0; i < holeCount; i++)
            {
                Rect2D r = holes[i];
                if (x > r.x && x < r.XMax && y > r.y && y < r.YMax) return true;
            }
            return false;
        }

        private static bool IsFinite(float f)
        {
            return !float.IsNaN(f) && !float.IsInfinity(f);
        }
    }
}
