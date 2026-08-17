using System.Collections.Generic;
using Formify.Domain;
using NUnit.Framework;
using UnityEngine;

namespace Formify.Tests.EditMode
{
    [TestFixture]
    public class RoomBuilderTests
    {
        const float Height = 2.8f;
        const float Thickness = 0.15f;
        const float Tol = 1e-4f;

        // Shipped default: 6 m x 4 m, counter-clockwise in XZ.
        static List<Vector2> Rect() => new List<Vector2>
        {
            new Vector2(0f, 0f), new Vector2(6f, 0f), new Vector2(6f, 4f), new Vector2(0f, 4f)
        };

        static List<Vector2> Triangle() => new List<Vector2>
        {
            new Vector2(0f, 0f), new Vector2(4f, 0f), new Vector2(0f, 3f)
        };

        static List<Vector2> Pentagon() => new List<Vector2>
        {
            new Vector2(0f, 0f), new Vector2(4f, 0f), new Vector2(5f, 3f),
            new Vector2(2f, 5f), new Vector2(-1f, 3f)
        };

        static Vector3 Centroid(IReadOnlyList<Vector2> p)
        {
            var sum = Vector2.zero;
            foreach (var v in p) sum += v;
            sum /= p.Count;
            return new Vector3(sum.x, 0f, sum.y);
        }

        static void AssertNames(RoomDefinition room, int wallCount)
        {
            for (int i = 0; i < wallCount; i++)
            {
                Assert.AreEqual(SurfaceKind.Wall, room.surfaces[i].kind, "kind at " + i);
                Assert.AreEqual("Wall " + (i + 1), room.surfaces[i].name);
            }
            Assert.AreEqual(SurfaceKind.Floor, room.surfaces[wallCount].kind);
            Assert.AreEqual("Floor", room.surfaces[wallCount].name);
            Assert.AreEqual(SurfaceKind.Ceiling, room.surfaces[wallCount + 1].kind);
            Assert.AreEqual("Ceiling", room.surfaces[wallCount + 1].name);
        }

        [Test]
        public void DefaultRectangle_Yields4WallsFloorAndCeiling()
        {
            var room = RoomBuilder.Build(Rect(), Height, Thickness);

            Assert.IsNotNull(room);
            Assert.AreEqual(6, room.surfaces.Count);
            AssertNames(room, 4);
        }

        [Test]
        public void VertexCount_DrivesSurfaceCountAndNaming()
        {
            var tri = RoomBuilder.Build(Triangle(), Height, Thickness);
            Assert.AreEqual(5, tri.surfaces.Count);
            AssertNames(tri, 3);

            var pent = RoomBuilder.Build(Pentagon(), Height, Thickness);
            Assert.AreEqual(7, pent.surfaces.Count);
            AssertNames(pent, 5);
        }

        [Test]
        public void WallDimensions_MatchEdgeLengthHeightAndThickness()
        {
            var footprint = Rect();
            var room = RoomBuilder.Build(footprint, Height, Thickness);

            for (int i = 0; i < footprint.Count; i++)
            {
                float expected = Vector2.Distance(footprint[i], footprint[(i + 1) % footprint.Count]);
                var wall = room.surfaces[i];
                Assert.AreEqual(expected, wall.width, Tol, "width of Wall " + (i + 1));
                Assert.AreEqual(Height, wall.height, Tol, "height of Wall " + (i + 1));
                Assert.AreEqual(Thickness, wall.thickness, Tol, "thickness of Wall " + (i + 1));
            }
        }

        [Test]
        public void WallNormals_PointInward_ForBothWindings()
        {
            var ccw = Rect();
            var cw = new List<Vector2>(ccw);
            cw.Reverse();

            foreach (var footprint in new[] { ccw, cw })
            {
                var room = RoomBuilder.Build(footprint, Height, Thickness);
                var centre = Centroid(footprint);

                for (int i = 0; i < footprint.Count; i++)
                {
                    var wall = room.surfaces[i];
                    Vector3 mid = wall.origin + wall.right * wall.width * 0.5f;
                    Assert.Greater(Vector3.Dot(wall.Normal, centre - mid), 0f,
                        "Wall " + (i + 1) + " faces outward");
                }
            }
        }

        [Test]
        public void FloorNormalPointsUp_CeilingNormalPointsDown()
        {
            var room = RoomBuilder.Build(Rect(), Height, Thickness);
            var floor = room.surfaces[4];
            var ceiling = room.surfaces[5];

            Assert.Less(Vector3.Distance(floor.Normal, Vector3.up), Tol, "floor normal " + floor.Normal);
            Assert.Less(Vector3.Distance(ceiling.Normal, Vector3.down), Tol, "ceiling normal " + ceiling.Normal);
            Assert.AreEqual(0f, floor.origin.y, Tol);
            Assert.AreEqual(Height, ceiling.origin.y, Tol);
            Assert.AreEqual(6f, floor.width, Tol);
            Assert.AreEqual(4f, floor.height, Tol);
        }

        [Test]
        public void InvalidInput_ReturnsNull()
        {
            var twoVerts = new List<Vector2> { new Vector2(0f, 0f), new Vector2(6f, 0f) };

            Assert.IsNull(RoomBuilder.Build(twoVerts, Height, Thickness));
            Assert.IsNull(RoomBuilder.Build(Rect(), 0f, Thickness));
            Assert.IsNull(RoomBuilder.Build(Rect(), Height, 0f));
        }

        [Test]
        public void Ids_AreSequentialAndUnique()
        {
            var room = RoomBuilder.Build(Pentagon(), Height, Thickness);

            Assert.AreEqual(7, room.surfaces.Count);
            for (int i = 0; i < room.surfaces.Count; i++)
                Assert.AreEqual(i, room.surfaces[i].id);
        }
    }
}
