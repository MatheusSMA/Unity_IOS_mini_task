using System.Collections;
using System.Collections.Generic;
using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Formify.Tests.PlayMode
{
    /// <summary>
    /// OUT-01. The outline itself is two RenderObjects passes on the SelectedSurface layer, so what is
    /// assertable here is the layer swap that feeds them and the raycast guard that keeps the selected
    /// surface tappable. The rendered edge is a human eye check in the Editor (UAT), not a unit assert.
    /// </summary>
    public class SelectionOutlineTests
    {
        // Two walls on the z = 2 plane, seen by a camera at the origin looking down +Z.
        private static readonly Vector3 WallACentre = new Vector3(-1.75f, 1.4f, 2f);
        private static readonly Vector3 WallBCentre = new Vector3(1.75f, 1.4f, 2f);
        // Wall C sits on the z = 4 plane, centred exactly where the tap ray at Wall A's centre would carry on
        // to if it went through Wall A. Only built by the mask test.
        private static readonly Vector3 WallCOrigin = new Vector3(-2.25f, 0f, 4f);
        private const float WallWidth = 2.5f;
        private const float WallHeight = 2.8f;
        private const float WallThickness = 0.15f;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        private Camera _camera;
        private RoomModel _model;
        private ModeManager _modes;
        private SelectionController _controller;
        private SurfaceDefinition _wallA;
        private SurfaceDefinition _wallB;
        private SurfaceDefinition _wallC;
        private SurfaceView _viewA;
        private SurfaceView _viewB;
        private int _outlineLayer;
        private int _events;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _outlineLayer = LayerMask.NameToLayer("SelectedSurface");

            _wallA = Wall(0, "Wall A", new Vector3(-0.5f, 0f, 2f));
            _wallB = Wall(1, "Wall B", new Vector3(3f, 0f, 2f));
            _wallC = Wall(2, "Wall C", WallCOrigin);
            _model = new RoomModel(new[] { _wallA, _wallB, _wallC });
            _modes = new ModeManager(IsWallSelected);

            GameObject cameraObject = Spawn("Camera");
            cameraObject.transform.position = new Vector3(0f, 1.4f, 0f);
            _camera = cameraObject.AddComponent<Camera>();
            // Fixed viewport: the screen/ray roundtrip must not depend on the headless screen size.
            _camera.pixelRect = new Rect(0f, 0f, 900f, 600f);
            _camera.aspect = 1.5f;

            _viewA = CreateWall(_wallA);
            _viewB = CreateWall(_wallB);

            _controller = Spawn("SelectionController").AddComponent<SelectionController>();
            _controller.Configure(_model, _modes, _camera);

            _events = 0;
            _model.SelectionChanged += OnSelectionChanged;

            // Let PhysX cook and register the two generated MeshColliders before anything raycasts.
            yield return new WaitForFixedUpdate();
        }

        [TearDown]
        public void TearDown()
        {
            // Immediate: a deferred Destroy would leave these colliders alive into the next test's raycast.
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null) Object.DestroyImmediate(_spawned[i]);
            }
            _spawned.Clear();
        }

        [UnityTest]
        public IEnumerator Selecting_moves_the_surface_to_the_outline_layer_and_deselecting_restores_it()
        {
            // Not a skip: the whole feature is the layer, so a missing layer is a failure.
            Assert.AreNotEqual(-1, _outlineLayer,
                "The 'SelectedSurface' layer is missing from the Tag Manager - the outline features " +
                "filter on it, so OUT-01 cannot work. Run OutlineSetup.Configure.");

            int originalA = _viewA.gameObject.layer;
            int originalB = _viewB.gameObject.layer;
            Assert.AreNotEqual(_outlineLayer, originalA, "Wall A starts off the outline layer.");

            _model.Select(_wallA.id);

            Assert.AreEqual(_outlineLayer, _viewA.gameObject.layer);
            Assert.AreEqual(originalB, _viewB.gameObject.layer);

            // Selecting the other wall deselects A, which must hand A its original layer back.
            _model.Select(_wallB.id);

            Assert.AreEqual(originalA, _viewA.gameObject.layer);
            Assert.AreEqual(_outlineLayer, _viewB.gameObject.layer);
            yield return null;
        }

        /// <summary>
        /// OUT-01 in the kit palette (HUD-01). The two RenderObjects passes draw the selected surface with the
        /// shared SelectionOutline material, so the only per-surface hook is the renderer's property block —
        /// the same one SEL-02's tint rides. Asserting the block is the closest a test gets to the ring's
        /// colour; the drawn edge itself stays a human eye check in the Editor.
        /// </summary>
        [UnityTest]
        public IEnumerator The_outline_colour_comes_from_the_kit_accent()
        {
            var block = new MaterialPropertyBlock();

            _model.Select(_wallA.id);
            _viewA.GetComponent<MeshRenderer>().GetPropertyBlock(block);

            Color outline = block.GetColor("_OutlineColor");
            Assert.AreEqual(HudTheme.Accent.r, outline.r, 0.001f, "outline red");
            Assert.AreEqual(HudTheme.Accent.g, outline.g, 0.001f, "outline green");
            Assert.AreEqual(HudTheme.Accent.b, outline.b, 0.001f, "outline blue");
            Assert.AreEqual(HudTheme.Accent.a, outline.a, 0.001f, "outline alpha");
            yield return null;
        }

        [UnityTest]
        public IEnumerator The_selected_surface_stays_hittable_and_re_tapping_it_raises_nothing()
        {
            _controller.OnTap(ScreenPositionOf(WallACentre));

            Assert.AreEqual(_wallA.id, _model.SelectedSurfaceId);
            Assert.AreEqual(1, _events);
            // OUT-01 AC2 only means something if the re-tap really goes at an object that moved layer.
            Assert.AreEqual(_outlineLayer, _viewA.gameObject.layer,
                "Wall A must be on the outline layer before the re-tap, otherwise this asserts nothing.");

            _events = 0;
            yield return new WaitForFixedUpdate();

            _controller.OnTap(ScreenPositionOf(WallACentre));

            // Still hit (SEL AC1) and Select() is a no-op for the current id, so no event (SEL AC3).
            Assert.AreEqual(_wallA.id, _model.SelectedSurfaceId);
            Assert.AreEqual(0, _events);
            Assert.AreEqual(_outlineLayer, _viewA.gameObject.layer);
        }

        /// <summary>
        /// OUT-01 AC2 proper. The default mask is ~0, so the OR in SelectionController.ResolveMask is invisible
        /// there — every layer is already in it. Here the mask is narrowed to the plain surface layer ONLY, and
        /// a second wall is parked directly behind Wall A on the same ray: with the OR the ray stops on the
        /// selected Wall A, without it the ray goes straight through A (now on the excluded outline layer) and
        /// lands on Wall C, moving the selection.
        /// </summary>
        [UnityTest]
        public IEnumerator A_mask_without_the_outline_layer_still_stops_on_the_selected_surface()
        {
            Assert.AreNotEqual(-1, _outlineLayer,
                "The 'SelectedSurface' layer is missing from the Tag Manager - run OutlineSetup.Configure.");

            SurfaceView viewC = CreateWall(_wallC);
            yield return new WaitForFixedUpdate();

            int plainLayer = _viewA.gameObject.layer;
            Assert.AreEqual(plainLayer, viewC.gameObject.layer, "both walls start on the same plain layer");
            Assert.AreNotEqual(_outlineLayer, plainLayer, "and that layer is not the outline one");

            // The narrow mask a project with a real layer budget would ship: surfaces only, no outline layer.
            _controller.RaycastMask = 1 << plainLayer;

            Vector2 tap = ScreenPositionOf(WallACentre);
            _controller.OnTap(tap);

            Assert.AreEqual(_wallA.id, _model.SelectedSurfaceId, "the first tap lands on A while A is still plain");
            Assert.AreEqual(_outlineLayer, _viewA.gameObject.layer, "and the selection moved A onto the outline layer");

            _events = 0;
            yield return new WaitForFixedUpdate();

            _controller.OnTap(tap);

            Assert.AreEqual(_wallA.id, _model.SelectedSurfaceId, "the ray still stops on A, so the selection stands");
            Assert.AreEqual(0, _events, "nothing moved the selection");
            Assert.IsFalse(viewC.IsTinted, "the wall behind A was never selected");
            Assert.AreEqual(plainLayer, viewC.gameObject.layer, "and never took the outline layer either");
        }

        private void OnSelectionChanged(int? previous, int? current) => _events++;

        private bool IsWallSelected()
        {
            int? id = _model.SelectedSurfaceId;
            return id.HasValue && _model.GetSurface(id.Value)?.kind == SurfaceKind.Wall;
        }

        private Vector2 ScreenPositionOf(Vector3 worldPoint) => _camera.WorldToScreenPoint(worldPoint);

        private static SurfaceDefinition Wall(int id, string name, Vector3 origin)
        {
            return new SurfaceDefinition
            {
                id = id,
                name = name,
                kind = SurfaceKind.Wall,
                origin = origin,
                right = new Vector3(-1f, 0f, 0f),
                up = Vector3.up,
                width = WallWidth,
                height = WallHeight,
                thickness = WallThickness
            };
        }

        /// <summary>
        /// MeshFilter + MeshRenderer + MeshCollider come from SurfaceView's [RequireComponent], and
        /// Initialize builds the slab into both, so the tap hits the real generated geometry.
        /// </summary>
        private SurfaceView CreateWall(SurfaceDefinition surface)
        {
            SurfaceView view = Spawn(surface.name).AddComponent<SurfaceView>();
            view.Initialize(surface, _model);

            Assert.IsNotNull(view.GetComponent<MeshCollider>().sharedMesh,
                $"{surface.name} has no collider mesh - the tap would miss for the wrong reason.");
            return view;
        }

        private GameObject Spawn(string name)
        {
            GameObject created = new GameObject(name);
            _spawned.Add(created);
            return created;
        }
    }
}
