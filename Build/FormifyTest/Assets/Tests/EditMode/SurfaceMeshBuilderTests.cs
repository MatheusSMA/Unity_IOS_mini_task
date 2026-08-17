using System.Collections.Generic;
using Formify.Domain;
using NUnit.Framework;
using UnityEngine;

namespace Formify.Tests.EditMode
{
    [TestFixture]
    public class SurfaceMeshBuilderTests
    {
        private const float W = 4f;
        private const float H = 2.8f;
        private const float T = 0.15f;
        private const float Tol = 1e-4f;

        // Same winding convention as the builder: for a triangle (v0, v1, v2),
        // Cross(v1 - v0, v2 - v0) points along the visible side of the face.
        private const int QuadIndices = 6;

        private static SurfaceDefinition Wall(float width = W, float height = H, float thickness = T)
        {
            return new SurfaceDefinition
            {
                id = 1,
                name = "Wall",
                kind = SurfaceKind.Wall,
                origin = Vector3.zero,
                right = Vector3.right,
                up = Vector3.up,
                width = width,
                height = height,
                thickness = thickness
            };
        }

        private static readonly Rect2D[] NoHoles = new Rect2D[0];

        private static bool StrictlyInside(Rect2D r, float x, float y)
        {
            return x > r.x + Tol && x < r.XMax - Tol && y > r.y + Tol && y < r.YMax - Tol;
        }

        /// <summary>
        /// Asserts that no triangle lying entirely on the given z plane has its centroid inside
        /// any hole - the "you can see through it" invariant.
        /// </summary>
        private static void AssertNoFaceTriangleCoversHoles(MeshData mesh, float z, IReadOnlyList<Rect2D> holes)
        {
            int checkedTriangles = 0;
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                Vector3 a = mesh.vertices[mesh.triangles[i]];
                Vector3 b = mesh.vertices[mesh.triangles[i + 1]];
                Vector3 c = mesh.vertices[mesh.triangles[i + 2]];
                if (Mathf.Abs(a.z - z) > Tol || Mathf.Abs(b.z - z) > Tol || Mathf.Abs(c.z - z) > Tol) continue;

                checkedTriangles++;
                float cx = (a.x + b.x + c.x) / 3f;
                float cy = (a.y + b.y + c.y) / 3f;
                for (int k = 0; k < holes.Count; k++)
                {
                    Assert.IsFalse(StrictlyInside(holes[k], cx, cy),
                        "Triangle " + (i / 3) + " at z=" + z + " has centroid (" + cx + ", " + cy + ") inside hole " + k);
                }
            }
            Assert.Greater(checkedTriangles, 0, "no triangles found on the z=" + z + " plane");
        }

        [Test]
        public void ZeroHoles_ProducesSixQuads()
        {
            MeshData mesh = SurfaceMeshBuilder.Build(Wall(), NoHoles);

            Assert.IsNotNull(mesh);
            // front + back + 4 outer sides = 6 quads, each 4 unshared vertices and 6 indices.
            Assert.AreEqual(6 * QuadIndices, mesh.triangles.Length);
            Assert.AreEqual(6 * 4, mesh.vertices.Length);
            Assert.AreEqual(mesh.vertices.Length, mesh.uvs.Length);
        }

        [Test]
        public void ZeroHoles_AllVerticesInsideLocalSlabBounds()
        {
            MeshData mesh = SurfaceMeshBuilder.Build(Wall(), NoHoles);

            Assert.IsNotNull(mesh);
            foreach (Vector3 v in mesh.vertices)
            {
                Assert.IsTrue(Mathf.Abs(v.z) <= Tol || Mathf.Abs(v.z + T) <= Tol, "unexpected z: " + v.z);
                Assert.GreaterOrEqual(v.x, -Tol);
                Assert.LessOrEqual(v.x, W + Tol);
                Assert.GreaterOrEqual(v.y, -Tol);
                Assert.LessOrEqual(v.y, H + Tol);
            }
        }

        [Test]
        public void OneHole_AddsRevealQuadsAndSlicedFaces()
        {
            var holes = new[] { new Rect2D(1f, 0.9f, 1.2f, 1.1f) };

            MeshData zero = SurfaceMeshBuilder.Build(Wall(), NoHoles);
            MeshData one = SurfaceMeshBuilder.Build(Wall(), holes);

            Assert.IsNotNull(one);
            // Cuts: x = {0, 1.0, 2.2, 4} and y = {0, 0.9, 2.0, 2.8} -> a 3x3 grid = 9 tiles,
            // minus the 1 tile that IS the hole = 8 tiles kept per face.
            // Quads = 8 front + 8 back + 4 outer sides + 4 reveals = 24.
            const int expectedQuads = 8 + 8 + 4 + 4;
            Assert.AreEqual(expectedQuads * QuadIndices, one.triangles.Length);
            Assert.AreEqual(expectedQuads * 4, one.vertices.Length);
            Assert.AreEqual(one.vertices.Length, one.uvs.Length);

            // Delta = 4 reveal quads (24 indices) + the grid growth on both faces,
            // (8 - 1) tiles x 2 faces = 14 quads (84 indices) = 108 indices.
            Assert.AreEqual(4 * QuadIndices + 14 * QuadIndices, one.triangles.Length - zero.triangles.Length);
        }

        [Test]
        public void OneHole_NoFaceTriangleCoversTheOpening()
        {
            var holes = new[] { new Rect2D(1f, 0.9f, 1.2f, 1.1f) };

            MeshData mesh = SurfaceMeshBuilder.Build(Wall(), holes);

            Assert.IsNotNull(mesh);
            AssertNoFaceTriangleCoversHoles(mesh, 0f, holes);
            AssertNoFaceTriangleCoversHoles(mesh, -T, holes);
        }

        [Test]
        public void TwoHoles_ProduceEightRevealQuadsAndTwoOpenings()
        {
            var holes = new[]
            {
                new Rect2D(0.5f, 0.9f, 1.0f, 1.2f),
                new Rect2D(2.2f, 0.9f, 1.0f, 1.2f)
            };

            MeshData mesh = SurfaceMeshBuilder.Build(Wall(), holes);

            Assert.IsNotNull(mesh);
            // Cuts: x = {0, 0.5, 1.5, 2.2, 3.2, 4} (5 columns), y = {0, 0.9, 2.1, 2.8} (3 rows)
            // -> 15 tiles, minus the 2 hole tiles = 13 kept per face.
            // Quads = 13 front + 13 back + 4 outer sides + 2 x 4 reveals = 38.
            const int expectedQuads = 13 + 13 + 4 + 8;
            Assert.AreEqual(expectedQuads * QuadIndices, mesh.triangles.Length);
            Assert.AreEqual(expectedQuads * 4, mesh.vertices.Length);

            AssertNoFaceTriangleCoversHoles(mesh, 0f, holes);
            AssertNoFaceTriangleCoversHoles(mesh, -T, holes);
        }

        [Test]
        public void AllTriangleIndicesAreInRange()
        {
            var holes = new[] { new Rect2D(1f, 0.9f, 1.2f, 1.1f) };

            MeshData mesh = SurfaceMeshBuilder.Build(Wall(), holes);

            Assert.IsNotNull(mesh);
            Assert.AreEqual(0, mesh.triangles.Length % 3);
            foreach (int index in mesh.triangles)
            {
                Assert.GreaterOrEqual(index, 0);
                Assert.Less(index, mesh.vertices.Length);
            }
        }

        [Test]
        public void DegenerateInputs_ReturnNull()
        {
            Assert.IsNull(SurfaceMeshBuilder.Build(null, NoHoles), "null surface");
            Assert.IsNull(SurfaceMeshBuilder.Build(Wall(0f), NoHoles), "zero width surface");
            Assert.IsNull(SurfaceMeshBuilder.Build(Wall(W, H, 0f), NoHoles), "zero thickness surface");
            Assert.IsNull(SurfaceMeshBuilder.Build(Wall(), new[] { new Rect2D(1f, 1f, 0.8f, 0f) }), "zero height hole");
            Assert.IsNull(SurfaceMeshBuilder.Build(Wall(), new[] { new Rect2D(3.8f, 1f, 0.8f, 0.8f) }), "hole past the right edge");
            Assert.IsNull(SurfaceMeshBuilder.Build(Wall(), new[] { new Rect2D(-0.2f, 1f, 0.8f, 0.8f) }), "hole past the left edge");
        }

        [Test]
        public void FrontFaceTrianglesFaceTowardsPositiveNormal()
        {
            MeshData mesh = SurfaceMeshBuilder.Build(Wall(), NoHoles);

            Assert.IsNotNull(mesh);
            int frontTriangles = 0;
            for (int i = 0; i < mesh.triangles.Length; i += 3)
            {
                Vector3 a = mesh.vertices[mesh.triangles[i]];
                Vector3 b = mesh.vertices[mesh.triangles[i + 1]];
                Vector3 c = mesh.vertices[mesh.triangles[i + 2]];
                if (Mathf.Abs(a.z) > Tol || Mathf.Abs(b.z) > Tol || Mathf.Abs(c.z) > Tol) continue;

                frontTriangles++;
                Vector3 n = Vector3.Cross(b - a, c - a).normalized;
                Assert.Greater(n.z, 0.99f, "front triangle " + (i / 3) + " faces the wrong way: " + n);
            }
            Assert.AreEqual(2, frontTriangles, "the hole-free front face is exactly one quad");
        }
    }
}
