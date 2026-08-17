using System.Collections;
using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Formify.Tests.PlayMode
{
    /// <summary>
    /// AR-01 AC1/AC3/AC4 — the toggle enters and leaves AR, greys out when AR is unavailable, and never offers
    /// the illegal TopDown to Ar transition (AD-013). Availability is forced through the controller's seam.
    /// </summary>
    [TestFixture]
    public class ArToggleButtonTests
    {
        private GameObject _arGo;
        private GameObject _buttonGo;
        private ArPoseCameraController _arCamera;
        private ArToggleButton _toggle;
        private Button _button;
        private ModeManager _modes;
        private bool _available;

        [SetUp]
        public void SetUp()
        {
            _arGo = new GameObject("ArPoseCamera");
            _arCamera = _arGo.AddComponent<ArPoseCameraController>();
            _arCamera.AvailabilityCheck = () => _available;

            _buttonGo = new GameObject("ArToggleButton", typeof(RectTransform));
            _button = _buttonGo.AddComponent<Button>();
            _toggle = _buttonGo.AddComponent<ArToggleButton>();

            _modes = new ModeManager(() => false);
            _available = true;
        }

        [TearDown]
        public void TearDown()
        {
            if (_buttonGo != null) Object.DestroyImmediate(_buttonGo);
            if (_arGo != null) Object.DestroyImmediate(_arGo);

            _buttonGo = null;
            _arGo = null;
            _button = null;
            _toggle = null;
            _arCamera = null;
            _modes = null;
        }

        [UnityTest]
        public IEnumerator AvailabilityTrue_ClickEntersArAndClickingAgainReturnsToOrbit()
        {
            _available = true;
            _toggle.Configure(_modes, _arCamera);

            yield return null;

            Assert.IsTrue(_toggle.IsInteractable);
            Assert.IsTrue(_button.interactable);

            // through the Button, so the Configure() onClick wiring is under test too
            _button.onClick.Invoke();
            Assert.AreEqual(Mode.Ar, _modes.Current);
            Assert.IsTrue(_button.interactable, "the toggle stays live so the user can get back out");

            _button.onClick.Invoke();
            Assert.AreEqual(Mode.Orbit, _modes.Current);
            Assert.IsTrue(_button.interactable);
        }

        [UnityTest]
        public IEnumerator AvailabilityFalse_ButtonIsDisabledAndClickingChangesNothing()
        {
            _available = false;
            _toggle.Configure(_modes, _arCamera);

            yield return null;

            Assert.IsFalse(_toggle.IsInteractable);
            Assert.IsFalse(_button.interactable, "AR-01 AC4: no AR tracking here, touch camera stays the fallback");

            _button.onClick.Invoke();
            _toggle.OnClick();

            Assert.AreEqual(Mode.Orbit, _modes.Current);
        }

        [UnityTest]
        public IEnumerator InTopDown_ButtonIsDisabledBecauseArIsOnlyReachableFromOrbit()
        {
            _available = true;
            _toggle.Configure(_modes, _arCamera);

            yield return null;

            Assert.IsTrue(_button.interactable, "interactable while the mode is Orbit");

            Assert.IsTrue(_modes.TrySet(Mode.TopDown));

            // recomputed by the ModeChanged subscription, no frame needed
            Assert.IsFalse(_toggle.IsInteractable);
            Assert.IsFalse(_button.interactable);

            _toggle.OnClick();
            Assert.AreEqual(Mode.TopDown, _modes.Current, "AD-013: the UI never offers TopDown -> Ar");
        }
    }
}
