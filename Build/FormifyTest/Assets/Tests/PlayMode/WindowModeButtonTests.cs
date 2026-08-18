using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Formify.Tests.PlayMode
{
    /// <summary>
    /// WIN-01 AC1 / AD-019: the window mode button is always on screen. It is enabled only while a Wall is
    /// selected and the mode is Orbit or WindowDraw, and the state dot marks window mode as active (AD-021:
    /// clicking it while active is the way out).
    /// </summary>
    public class WindowModeButtonTests
    {
        SurfaceDefinition wall;
        SurfaceDefinition floor;
        SurfaceDefinition ceiling;
        RoomModel model;
        ModeManager modes;

        GameObject buttonObject;
        WindowModeButton button;
        Button uiButton;
        Image stateDot;

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

            // A real uGUI Button and a dot Graphic, so the test reads what the user would see, not a bare flag.
            buttonObject = new GameObject("Window Mode Button", typeof(RectTransform), typeof(Image), typeof(Button));
            uiButton = buttonObject.GetComponent<Button>();

            var dotObject = new GameObject("StateDot", typeof(RectTransform));
            dotObject.transform.SetParent(buttonObject.transform, false);
            stateDot = dotObject.AddComponent<Image>();

            button = buttonObject.AddComponent<WindowModeButton>();
            button.StateDot = stateDot;
            button.Configure(model, modes);
        }

        [TearDown]
        public void TearDown()
        {
            if (buttonObject != null) Object.DestroyImmediate(buttonObject);
        }

        /// <summary>AD-019: whatever the state, the button never leaves the screen.</summary>
        void AssertOnScreen()
        {
            Assert.IsTrue(buttonObject.activeSelf, "the button stays on screen (AD-019)");
            Assert.IsTrue(buttonObject.activeInHierarchy);
        }

        void AssertState(bool interactable, bool active)
        {
            AssertOnScreen();
            Assert.AreEqual(interactable, button.IsInteractable, "IsInteractable");
            Assert.AreEqual(interactable, uiButton.interactable, "the uGUI Button follows IsInteractable");
            Assert.AreEqual(active, button.IsActive, "IsActive");
            Assert.AreEqual(active, stateDot.enabled, "the state dot follows IsActive");
        }

        [Test]
        public void Enabled_when_a_wall_is_selected_in_orbit()
        {
            model.Select(wall.id);

            Assert.AreEqual(Mode.Orbit, modes.Current);
            AssertState(interactable: true, active: false);
        }

        [Test]
        public void Disabled_when_the_floor_is_selected()
        {
            model.Select(wall.id);
            Assert.IsTrue(button.IsInteractable, "Precondition: the button is live for the wall.");

            model.Select(floor.id);

            Assert.AreEqual(floor.id, model.SelectedSurfaceId);
            AssertState(interactable: false, active: false);
        }

        [Test]
        public void Disabled_when_the_ceiling_is_selected()
        {
            model.Select(wall.id);
            Assert.IsTrue(button.IsInteractable, "Precondition: the button is live for the wall.");

            model.Select(ceiling.id);

            Assert.AreEqual(ceiling.id, model.SelectedSurfaceId);
            AssertState(interactable: false, active: false);
        }

        [Test]
        public void Disabled_when_nothing_is_selected()
        {
            Assert.IsNull(model.SelectedSurfaceId);
            AssertState(interactable: false, active: false);

            model.Select(wall.id);
            model.ClearSelection();

            AssertState(interactable: false, active: false);
        }

        [Test]
        public void Disabled_when_a_wall_is_selected_but_the_mode_is_top_down()
        {
            model.Select(wall.id);
            Assert.IsTrue(modes.TrySet(Mode.TopDown));

            Assert.AreEqual(Mode.TopDown, modes.Current);
            Assert.AreEqual(wall.id, model.SelectedSurfaceId, "The wall is still the selected surface.");
            AssertState(interactable: false, active: false);
        }

        [Test]
        public void Disabled_when_a_wall_is_selected_but_the_mode_is_ar()
        {
            model.Select(wall.id);
            Assert.IsTrue(modes.TrySet(Mode.Ar), "Ar is legal from Orbit (AD-013)");

            Assert.AreEqual(Mode.Ar, modes.Current);
            Assert.AreEqual(wall.id, model.SelectedSurfaceId, "The wall is still the selected surface.");
            AssertState(interactable: false, active: false);
        }

        [Test]
        public void Click_while_enabled_enters_window_draw_and_lights_the_dot()
        {
            model.Select(wall.id);
            Assert.IsTrue(button.IsInteractable, "Precondition: the button is live.");

            button.OnClick();

            Assert.AreEqual(Mode.WindowDraw, modes.Current);
            // AD-021: still pressable, because pressing it again is the way out.
            AssertState(interactable: true, active: true);
        }

        [Test]
        public void Click_again_exits_window_draw()
        {
            model.Select(wall.id);
            button.OnClick();
            Assert.AreEqual(Mode.WindowDraw, modes.Current, "Precondition: window mode is active.");

            button.OnClick();

            Assert.AreEqual(Mode.Orbit, modes.Current);
            AssertState(interactable: true, active: false);
        }

        [Test]
        public void A_disabled_button_does_nothing_when_pressed()
        {
            model.Select(floor.id);
            Assert.IsFalse(button.IsInteractable, "Precondition: the floor leaves the button disabled.");

            button.OnClick();

            Assert.AreEqual(Mode.Orbit, modes.Current, "a disabled button cannot enter window mode");
            AssertState(interactable: false, active: false);
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
