using System.Collections;
using System.Collections.Generic;
using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Formify.Tests.PlayMode
{
    /// <summary>SEL-02 tint and WIN-02 AC5/AC12 collider sync, built in code and driven through RoomModel.</summary>
    public class SurfaceViewTests
    {
        private const int WallId = 0;
        private const float WallWidth = 4f;
        private const float WallHeight = 2.8f;
        private const float WallThickness = 0.15f;
        private const float ColourTolerance = 0.001f;

        /// <summary>1 m x 1 m, well inside the validator's 0.1 m edge margin so the stored rect is not clamped.</summary>
        private static readonly Rect2D Opening = new Rect2D(1.5f, 1f, 1f, 1f);

        private static readonly Vector2 OpeningCentre = new Vector2(2f, 1.5f);
        private static readonly Vector2 SolidPoint = new Vector2(0.5f, 0.5f);

        private SurfaceDefinition _wall;
        private RoomModel _model;
        private GameObject _go;
        private SurfaceView _view;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private Material _material;

        [SetUp]
        public void SetUp()
        {
            _wall = new SurfaceDefinition
            {
                id = WallId,
                name = "Wall 1",
                kind = SurfaceKind.Wall,
                origin = new Vector3(-WallWidth * 0.5f, 0f, 0f),
                right = Vector3.right,
                up = Vector3.up,
                width = WallWidth,
                height = WallHeight,
                thickness = WallThickness
            };
            _model = new RoomModel(new List<SurfaceDefinition> { _wall });

            _go = new GameObject("Wall 1", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider));
            _meshFilter = _go.GetComponent<MeshFilter>();
            _meshRenderer = _go.GetComponent<MeshRenderer>();
            _meshCollider = _go.GetComponent<MeshCollider>();

            _material = new Material(LitShader());
            _meshRenderer.sharedMaterial = _material;

            _view = _go.AddComponent<SurfaceView>();
            _view.Initialize(_wall, _model);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            if (_material != null) Object.DestroyImmediate(_material);
        }

        [Test]
        public void Selection_TintsThroughPropertyBlock_AndDeselectionRestoresBaseColour()
        {
            Material sharedBefore = _meshRenderer.sharedMaterial;
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            _model.Select(WallId);

            Assert.IsTrue(_view.IsTinted, "selected surface should report a tint");
            _meshRenderer.GetPropertyBlock(block);
            AssertColour(_view.TintColor, block.GetColor("_BaseColor"), "tinted");

            _model.ClearSelection();

            Assert.IsFalse(_view.IsTinted, "deselected surface should drop the tint");
            _meshRenderer.GetPropertyBlock(block);
            AssertColour(_view.BaseColor, block.GetColor("_BaseColor"), "untinted");

            // A MaterialPropertyBlock must never instantiate the shared material (AD-010).
            Assert.AreSame(sharedBefore, _meshRenderer.sharedMaterial);
            Assert.AreSame(_material, _meshRenderer.sharedMaterial);
        }

        /// <summary>
        /// SEL-02 in the kit palette (HUD-01): the selection tint is the kit's selected green, not a hand-picked
        /// hex. It is Accent composited at SelectedRowFill's alpha, because the surface material is opaque URP
        /// Lit and drops _BaseColor's alpha — a raw SelectedRowFill would repaint the wall solid green.
        /// </summary>
        [Test]
        public void TintColour_IsTheKitAccent_DarkenedIntoADeepGreen()
        {
            Color tint = _view.TintColor;

            // Green-dominant AND darker than the surface itself. Either half alone would pass for something the
            // owner asked against: a pale mint wash, or a grey wall.
            Assert.Greater(tint.g, tint.r, "the tint has to be green-dominant");
            Assert.Greater(tint.g, tint.b, "the tint has to be green-dominant");
            Assert.Less(tint.grayscale, _view.BaseColor.grayscale,
                "a selected wall reads darker than an unselected one, not paler");

            // Still a tint, not a paint: the raw accent spreads its channels 0.73 wide.
            float spread = tint.maxColorComponent - Mathf.Min(tint.r, Mathf.Min(tint.g, tint.b));
            Assert.Less(spread, 0.5f,
                "the wall must read tinted, not painted - a full-strength accent would be a solid green slab");
        }

        [UnityTest]
        public IEnumerator Raycast_ThroughOpening_MissesCollider_AfterWindowAdded()
        {
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(RayHitsCollider(OpeningCentre), "solid wall must block the ray before the window exists");

            Assert.IsTrue(_model.TryAddWindow(WallId, Opening, out WindowRejection reason), "rejected: " + reason);
            Assert.AreEqual(WindowRejection.None, reason);

            yield return new WaitForFixedUpdate();

            Assert.IsFalse(RayHitsCollider(OpeningCentre), "the opening must be ray-through, not just see-through");
        }

        [UnityTest]
        public IEnumerator Raycast_ThroughOpening_HitsColliderAgain_AfterWindowRemoved()
        {
            Assert.IsTrue(_model.TryAddWindow(WallId, Opening, out WindowRejection reason), "rejected: " + reason);
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(RayHitsCollider(OpeningCentre));

            IReadOnlyList<WindowSpec> windows = _model.GetWindows(WallId);
            Assert.AreEqual(1, windows.Count);
            int windowId = windows[0].id;

            Assert.IsTrue(_model.TryRemoveWindow(windowId));
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(RayHitsCollider(OpeningCentre), "removing the window must restore the solid collider");
        }

        [UnityTest]
        public IEnumerator Raycast_AtSolidWall_HitsCollider_WithAndWithoutWindow()
        {
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(RayHitsCollider(SolidPoint));

            Assert.IsTrue(_model.TryAddWindow(WallId, Opening, out WindowRejection reason), "rejected: " + reason);
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(RayHitsCollider(SolidPoint), "solid wall away from the opening must still be hit");
            Assert.IsFalse(RayHitsCollider(OpeningCentre), "control: the same ray geometry misses through the opening");
        }

        [Test]
        public void Rebuild_WithDegenerateSurface_KeepsPreviousMeshAndCollider()
        {
            Mesh previous = _meshCollider.sharedMesh;
            Assert.IsNotNull(previous);
            int triangles = previous.triangles.Length;
            Assert.Greater(triangles, 0);

            _wall.width = 0f; // degenerate input: SurfaceMeshBuilder returns null (EDGE-05)

            Assert.IsFalse(_view.Rebuild());
            Assert.AreSame(previous, _meshCollider.sharedMesh);
            Assert.AreSame(previous, _meshFilter.sharedMesh);
            Assert.AreEqual(triangles, _meshCollider.sharedMesh.triangles.Length);
        }

        /// <summary>
        /// EDGE-05, the other half: a rebuild the builder rejects must also reject the operation, so the model
        /// stops claiming an opening the wall does not have. Thickness 0 fails SurfaceMeshBuilder but not the
        /// placement validator, so the add is accepted by the model and only the geometry refuses it.
        /// </summary>
        [Test]
        public void FailedRebuild_AfterAdd_RollsTheWindowBackOutOfTheModel()
        {
            Mesh previous = _meshCollider.sharedMesh;
            _wall.thickness = 0f;

            Assert.IsTrue(_model.TryAddWindow(WallId, Opening, out WindowRejection reason), "rejected: " + reason);

            Assert.AreEqual(0, _model.GetWindows(WallId).Count, "the model must not keep a window the wall refused");
            Assert.AreSame(previous, _meshCollider.sharedMesh);
            Assert.AreSame(previous, _meshFilter.sharedMesh);
        }

        [Test]
        public void FailedRebuild_AfterRemove_RestoresTheWindowInTheModel()
        {
            Assert.IsTrue(_model.TryAddWindow(WallId, Opening, out WindowRejection reason), "rejected: " + reason);
            int windowId = _model.GetWindows(WallId)[0].id;
            Mesh previous = _meshCollider.sharedMesh;

            _wall.thickness = 0f;
            Assert.IsTrue(_model.TryRemoveWindow(windowId));

            IReadOnlyList<WindowSpec> windows = _model.GetWindows(WallId);
            Assert.AreEqual(1, windows.Count, "the window is still cut into the live mesh, so it stays in the model");
            Assert.AreEqual(windowId, windows[0].id);
            Assert.AreSame(previous, _meshCollider.sharedMesh);
        }

        /// <summary>Fires from 1 m in front of the surface point, straight along -Normal, through the slab.</summary>
        private bool RayHitsCollider(Vector2 surfaceLocal)
        {
            Vector3 normal = _wall.Normal;
            Vector3 target = _wall.LocalToWorld(surfaceLocal.x, surfaceLocal.y);
            RaycastHit[] hits = Physics.RaycastAll(target + normal, -normal, 2f);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].collider == _meshCollider) return true;
            }
            return false;
        }

        private static void AssertColour(Color expected, Color actual, string what)
        {
            Assert.AreEqual(expected.r, actual.r, ColourTolerance, what + " red");
            Assert.AreEqual(expected.g, actual.g, ColourTolerance, what + " green");
            Assert.AreEqual(expected.b, actual.b, ColourTolerance, what + " blue");
            Assert.AreEqual(expected.a, actual.a, ColourTolerance, what + " alpha");
        }

        private static Shader LitShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            return shader != null ? shader : Shader.Find("Unlit/Color");
        }
    }
}
