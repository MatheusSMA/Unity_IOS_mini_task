using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Formify.Tests.PlayMode
{
    /// <summary>ROOM-01: the bootstrap assembles the room in code, no scene load and no prefabs.</summary>
    public class RoomBootstrapTests
    {
        private const float RoomWidth = 6f;
        private const float RoomDepth = 4f;
        private const float RoomHeight = 2.8f;
        private const float Thickness = 0.15f;
        private const float Tolerance = 0.0001f;

        private static readonly string[] ExpectedNames = { "Wall 1", "Wall 2", "Wall 3", "Wall 4", "Floor", "Ceiling" };

        private GameObject _go;
        private RoomBootstrap _bootstrap;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("RoomBootstrap");
            _bootstrap = _go.AddComponent<RoomBootstrap>(); // Awake runs here
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public void Awake_CreatesOneViewPerSurface_MatchingTheRoomDefinition()
        {
            RoomDefinition expected = RoomBuilder.Build(Footprint(), RoomHeight, Thickness);
            Assert.AreEqual(6, expected.surfaces.Count);
            Assert.AreEqual(6, _bootstrap.SurfaceViews.Count);

            int walls = 0, floors = 0, ceilings = 0;

            for (int i = 0; i < expected.surfaces.Count; i++)
            {
                SurfaceView view = _bootstrap.SurfaceViews[i];
                SurfaceDefinition surface = expected.surfaces[i];

                Assert.AreEqual(surface.id, view.Surface.id, "id at index " + i);
                Assert.AreEqual(surface.name, view.Surface.name, "surface name at index " + i);
                Assert.AreEqual(ExpectedNames[i], view.Surface.name, "spec name at index " + i);
                Assert.AreEqual(ExpectedNames[i], view.name, "GameObject name at index " + i);
                Assert.AreEqual(surface.kind, view.Surface.kind, "kind at index " + i);

                if (view.Surface.kind == SurfaceKind.Wall) walls++;
                else if (view.Surface.kind == SurfaceKind.Floor) floors++;
                else ceilings++;
            }

            Assert.AreEqual(4, walls);
            Assert.AreEqual(1, floors);
            Assert.AreEqual(1, ceilings);
        }

        [Test]
        public void EveryView_HasCollidableMesh_SharedWithTheRenderer()
        {
            foreach (SurfaceView view in _bootstrap.SurfaceViews)
            {
                MeshCollider collider = view.GetComponent<MeshCollider>();
                MeshFilter filter = view.GetComponent<MeshFilter>();

                Assert.IsNotNull(collider, view.name + " has no MeshCollider");
                Assert.IsNotNull(filter, view.name + " has no MeshFilter");
                Assert.IsNotNull(collider.sharedMesh, view.name + " collider mesh is null");
                Assert.Greater(collider.sharedMesh.triangles.Length, 0, view.name + " collider mesh is empty");
                Assert.AreSame(collider.sharedMesh, filter.sharedMesh, view.name + " renderer and collider disagree");
            }
        }

        [Test]
        public void Bootstrap_ExposesModelModesAndCeiling_InTheirInitialState()
        {
            Assert.AreEqual(6, _bootstrap.Model.Surfaces.Count);
            Assert.AreEqual(Mode.Orbit, _bootstrap.Modes.Current);
            Assert.IsNull(_bootstrap.Model.SelectedSurfaceId);

            Assert.IsNotNull(_bootstrap.CeilingView);
            Assert.AreEqual(SurfaceKind.Ceiling, _bootstrap.CeilingView.Surface.kind);
            Assert.AreEqual("Ceiling", _bootstrap.CeilingView.name);

            Assert.AreEqual(Vector3.zero, _bootstrap.RoomCentre);

            Bounds bounds = _bootstrap.RoomBounds;
            Assert.AreEqual(RoomWidth, bounds.size.x, Tolerance, "bounds width");
            Assert.AreEqual(RoomHeight, bounds.size.y, Tolerance, "bounds height");
            Assert.AreEqual(RoomDepth, bounds.size.z, Tolerance, "bounds depth");
            Assert.AreEqual(RoomHeight * 0.5f, bounds.center.y, Tolerance, "bounds centre height");
        }

        /// <summary>Same footprint the bootstrap generates from its serialized 6 m x 4 m default.</summary>
        private static Vector2[] Footprint()
        {
            float x = RoomWidth * 0.5f;
            float z = RoomDepth * 0.5f;
            return new[]
            {
                new Vector2(-x, -z),
                new Vector2(x, -z),
                new Vector2(x, z),
                new Vector2(-x, z)
            };
        }
    }
}
