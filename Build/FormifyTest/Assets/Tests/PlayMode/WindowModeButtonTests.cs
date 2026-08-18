using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Formify.Tests.PlayMode
{
    /// <summary>WIN-01 AC1 / AD-015: the window mode button exists only while a Wall is selected in Orbit.</summary>
    public class WindowModeButtonTests
    {
        SurfaceDefinition wall;
        SurfaceDefinition floor;
        SurfaceDefinition ceiling;
        RoomModel model;
        ModeManager modes;

        GameObject buttonObject;
        WindowModeButton button;

        [SetUp]
        public void SetUp()
        {
            wall = new SurfaceDefinition
            {
                id = 0,
                name = "Wall 1",
                kind = SurfaceKind.Wall,
                origin = Vector3.zero,
                right = Vector3.right,
                up = Vector3.up,
                width = 4f,
                height = 2.8f
            };

            floor = new SurfaceDefinition
            {
                id = 1,
                name = "Floor",
                kind = SurfaceKind.Floor,
                origin = Vector3.zero,
                right = Vector3.right,
                up = Vector3.back,
                width = 4f,
                height = 4f
            };

            ceiling = new SurfaceDefinition
            {
                id = 2,
                name = "Ceiling",
                kind = SurfaceKind.Ceiling,
                origin = new Vector3(0f, 2.8f, 0f),
                right = Vector3.right,
                up = Vector3.forward,
                width = 4f,
                height = 4f
            };

            model = new RoomModel(new[] { wall, floor, ceiling });
            modes = new ModeManager(IsWallSelected);

            buttonObject = new GameObject("Window Mode Button");
            button = buttonObject.AddComponent<WindowModeButton>();
            button.Configure(model, modes);
        }

        [TearDown]
        public void TearDown()
        {
            if (buttonObject != null) Object.DestroyImmediate(buttonObject);
        }

        [Test]
        public void Visible_when_a_wall_is_selected_in_orbit()
        {
            model.Select(wall.id);

            Assert.AreEqual(Mode.Orbit, modes.Current);
            Assert.IsTrue(button.IsVisible);
            Assert.IsTrue(buttonObject.activeSelf);
        }

        [Test]
        public void Hidden_when_the_floor_is_selected()
        {
            model.Select(wall.id);
            Assert.IsTrue(button.IsVisible, "Precondition: the button is up for the wall.");

            model.Select(floor.id);

            Assert.AreEqual(floor.id, model.SelectedSurfaceId);
            Assert.IsFalse(button.IsVisible);
            Assert.IsFalse(buttonObject.activeSelf);
        }

        [Test]
        public void Hidden_when_the_ceiling_is_selected()
        {
            model.Select(wall.id);
            Assert.IsTrue(button.IsVisible, "Precondition: the button is up for the wall.");

            model.Select(ceiling.id);

            Assert.AreEqual(ceiling.id, model.SelectedSurfaceId);
            Assert.IsFalse(button.IsVisible);
            Assert.IsFalse(buttonObject.activeSelf);
        }

        [Test]
        public void Hidden_when_nothing_is_selected()
        {
            Assert.IsNull(model.SelectedSurfaceId);
            Assert.IsFalse(button.IsVisible);
            Assert.IsFalse(buttonObject.activeSelf);

            model.Select(wall.id);
            model.ClearSelection();

            Assert.IsFalse(button.IsVisible);
            Assert.IsFalse(buttonObject.activeSelf);
        }

        [Test]
        public void Hidden_when_a_wall_is_selected_but_the_mode_is_top_down()
        {
            model.Select(wall.id);
            Assert.IsTrue(modes.TrySet(Mode.TopDown));

            Assert.AreEqual(Mode.TopDown, modes.Current);
            Assert.AreEqual(wall.id, model.SelectedSurfaceId, "The wall is still the selected surface.");
            Assert.IsFalse(button.IsVisible);
            Assert.IsFalse(buttonObject.activeSelf);
        }

        [Test]
        public void Hidden_when_a_wall_is_selected_but_the_mode_is_ar()
        {
            model.Select(wall.id);
            Assert.IsTrue(modes.TrySet(Mode.Ar), "Ar is legal from Orbit (AD-013)");

            Assert.AreEqual(Mode.Ar, modes.Current);
            Assert.AreEqual(wall.id, model.SelectedSurfaceId, "The wall is still the selected surface.");
            Assert.IsFalse(button.IsVisible);
            Assert.IsFalse(buttonObject.activeSelf);
        }

        [Test]
        public void Click_while_visible_enters_window_draw()
        {
            model.Select(wall.id);
            Assert.IsTrue(button.IsVisible, "Precondition: the button is up.");

            button.OnClick();

            Assert.AreEqual(Mode.WindowDraw, modes.Current);
            Assert.IsFalse(button.IsVisible, "AD-015 hides the button outside Orbit.");
            Assert.IsFalse(buttonObject.activeSelf);
        }

        [Test]
        public void Click_again_returns_to_orbit()
        {
            model.Select(wall.id);
            button.OnClick();
            Assert.AreEqual(Mode.WindowDraw, modes.Current, "Precondition: window mode is active.");

            button.OnClick();

            Assert.AreEqual(Mode.Orbit, modes.Current);
            Assert.IsTrue(button.IsVisible);
            Assert.IsTrue(buttonObject.activeSelf);
        }

        bool IsWallSelected()
        {
            int? id = model.SelectedSurfaceId;
            if (!id.HasValue) return false;

            SurfaceDefinition surface = model.GetSurface(id.Value);
            return surface != null && surface.kind == SurfaceKind.Wall;
        }
    }
}
