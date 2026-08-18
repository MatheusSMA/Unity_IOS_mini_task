using System.Collections.Generic;
using Formify.EditorTools;
using Formify.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Formify.Tests.EditMode
{
    /// <summary>
    /// The bake is what puts the HUD in the scene (AD-025), so what it leaves behind is the whole contract:
    /// one HUD, every reference on it already wired — play binds, it does not build — and no scaffold carried
    /// into the scene, or the game would come up with a second RoomBootstrap building a second room.
    /// </summary>
    public class HudSceneBakerTests
    {
        private Scene _scene;
        private Scene _previous;

        [SetUp]
        public void SetUp()
        {
            // Own scene: the bake edits whatever is active, and eating the user's open scene is not a test.
            _previous = SceneManager.GetActiveScene();
            _scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(_scene);
        }

        [TearDown]
        public void TearDown()
        {
            if (_previous.IsValid()) SceneManager.SetActiveScene(_previous);
            if (_scene.IsValid()) EditorSceneManager.CloseScene(_scene, true);
        }

        [Test]
        public void Bake_puts_the_hud_canvas_in_the_scene()
        {
            HudSceneBaker.Bake();

            HudRoot hud = FindFirst<HudRoot>();
            Assert.That(hud, Is.Not.Null, "the bake left no HUD root in the scene");

            Canvas canvas = hud.Canvas;
            Assert.That(canvas, Is.Not.Null, "the HUD root holds no canvas");
            Assert.That(canvas.transform.Find("RightRail"), Is.Not.Null, "the art kit's right rail is missing");
            Assert.That(canvas.transform.Find("SurfacesPanel"), Is.Not.Null, "the surfaces panel is missing");
            Assert.That(canvas.transform.Find("ViewToggle"), Is.Not.Null, "the 2D/3D toggle is missing");
            Assert.That(canvas.transform.Find("Readout"), Is.Not.Null, "the readout panel is missing");
            Assert.That(canvas.transform.Find("HintPill"), Is.Not.Null, "the hint pill is missing");
            Assert.That(canvas.transform.Find("Scanlines"), Is.Not.Null, "the scanline overlay is missing");
        }

        /// <summary>
        /// The point of baking: every view reference is serialized with the scene, so <c>Compose</c> only has to
        /// hand out the model. A reference lost here is a HUD that silently stops painting at play.
        /// </summary>
        [Test]
        public void Baked_hud_keeps_every_view_reference()
        {
            HudSceneBaker.Bake();

            HudRoot hud = FindFirst<HudRoot>();

            Assert.That(hud.ListPanel, Is.Not.Null, "surfaces panel");
            Assert.That(hud.ListPanel.Canvas, Is.Not.Null, "the panel lost its canvas");
            Assert.That(hud.Rail, Is.Not.Null, "right rail");
            Assert.That(hud.WindowHudButton, Is.Not.Null, "window mode button");
            Assert.That(hud.WindowHudButton.Button, Is.Not.Null, "the window mode button lost its Button");
            Assert.That(hud.WindowHudButton.Dot, Is.Not.Null, "the window mode button lost its state dot");
            Assert.That(hud.WindowMode, Is.Not.Null, "window mode behaviour");
            Assert.That(hud.WindowMode.StateDot, Is.Not.Null, "the state dot is not wired to the behaviour");
            Assert.That(hud.ClearHudButton, Is.Not.Null, "clear button");
            Assert.That(hud.ClearHudButton.UseNeutralPalette, Is.True, "Clear must keep the kit's neutral palette");
            Assert.That(hud.Clear, Is.Not.Null, "clear behaviour");
            Assert.That(hud.ArHudButton, Is.Not.Null, "AR button");
            Assert.That(hud.ArToggle, Is.Not.Null, "AR behaviour");
            Assert.That(hud.ViewSwitch, Is.Not.Null, "view switch");
            Assert.That(hud.ViewSwitch.TwoDButton, Is.Not.Null, "the view switch lost its 2D segment");
            Assert.That(hud.ViewSwitch.ThreeDButton, Is.Not.Null, "the view switch lost its 3D segment");
            Assert.That(hud.Readout, Is.Not.Null, "readout");
            Assert.That(hud.HintPill, Is.Not.Null, "hint pill");
        }

        [Test]
        public void Bake_leaves_no_bootstrap_or_camera_behind()
        {
            HudSceneBaker.Bake();

            Assert.That(FindFirst<RoomBootstrap>(), Is.Null,
                "the bake scaffold survived — the scene would build a second room at play");
            Assert.That(FindFirst<Camera>(), Is.Null, "the bake's throwaway camera survived");
        }

        /// <summary>The scene's own bootstrap is pointed at what was baked, so play binds without searching.</summary>
        [Test]
        public void Bake_wires_the_scene_bootstrap_to_the_baked_hud()
        {
            var go = new GameObject("Room");
            RoomBootstrap bootstrap = go.AddComponent<RoomBootstrap>();

            HudSceneBaker.Bake();

            SerializedProperty wired = new SerializedObject(bootstrap).FindProperty("hud");
            Assert.That(wired.objectReferenceValue, Is.SameAs(FindFirst<HudRoot>()));
        }

        [Test]
        public void Baking_twice_leaves_one_hud()
        {
            HudSceneBaker.Bake();
            HudSceneBaker.Bake();

            Assert.That(FindAll<HudRoot>().Count, Is.EqualTo(1));
        }

        [Test]
        public void Clear_removes_the_baked_hud()
        {
            HudSceneBaker.Bake();
            HudSceneBaker.Clear();

            Assert.That(FindFirst<HudRoot>(), Is.Null);
        }

        /// <summary>The active scene only — the test runner keeps other scenes loaded, and they are not ours.</summary>
        private List<T> FindAll<T>() where T : Component
        {
            var found = new List<T>();
            GameObject[] roots = _scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++) found.AddRange(roots[i].GetComponentsInChildren<T>(true));
            return found;
        }

        private T FindFirst<T>() where T : Component
        {
            List<T> found = FindAll<T>();
            return found.Count > 0 ? found[0] : null;
        }
    }
}
