using Formify.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Formify.Presentation
{
    /// <summary>
    /// TOP-01 — the visible switch: "2D | 3D" at the top of the T19 canvas. Each button does exactly one thing,
    /// <see cref="ModeManager.TrySet"/>; the camera, the ceiling and the selection clear all ride on
    /// <see cref="ModeManager.ModeChanged"/> inside <see cref="TopDownController"/>.
    /// The list panel and the Clear button need nothing from here: they subscribe to <see cref="RoomModel"/>,
    /// which is mode-agnostic, so the plan is a full working view for free (TOP-02 AC6).
    /// </summary>
    public class ViewSwitchButtons : MonoBehaviour
    {
        [SerializeField] private float buttonWidth = 66f;
        [SerializeField] private float buttonHeight = 38f;
        [SerializeField] private float spacing = 8f;
        [SerializeField] private float topMargin = 12f;
        [SerializeField] private float fontSize = 13f;

        private ModeManager _modes;
        private Canvas _canvas;
        private RectTransform _root;

        /// <summary>The "2D" button. Non-null once <see cref="Configure"/> has been given a canvas.</summary>
        public Button TwoDButton { get; private set; }

        /// <summary>The "3D" button. Non-null once <see cref="Configure"/> has been given a canvas.</summary>
        public Button ThreeDButton { get; private set; }

        /// <summary>The mode is the authority, not which button was pressed last.</summary>
        public bool IsTwoDActive => _modes != null && _modes.Current == Mode.TopDown;

        /// <summary>
        /// Builds the pair under <paramref name="uiCanvas"/> the first time, then binds the modes. Safe to call
        /// again: the previous subscription and the previous onClick listeners are removed first, so
        /// re-configuring can never double-fire.
        /// </summary>
        public void Configure(ModeManager modes, Canvas uiCanvas)
        {
            Unsubscribe();
            _modes = modes;

            if (uiCanvas != null && _canvas != uiCanvas)
            {
                _canvas = uiCanvas;
                if (_root == null) BuildButtons();
                else _root.SetParent(_canvas.transform, false);
            }

            Wire(TwoDButton, OnTwoD);
            Wire(ThreeDButton, OnThreeD);

            if (_modes != null) _modes.ModeChanged += OnModeChanged;

            Refresh();
        }

        /// <summary>TopDownController does the camera and ceiling work off ModeChanged.</summary>
        public void OnTwoD()
        {
            if (_modes != null) _modes.TrySet(Mode.TopDown);
        }

        public void OnThreeD()
        {
            if (_modes != null) _modes.TrySet(Mode.Orbit);
        }

        private void OnModeChanged(Mode previous, Mode current) => Refresh();

        /// <summary>The active view's button is the highlighted, unpressable one.</summary>
        private void Refresh()
        {
            bool twoD = IsTwoDActive;
            SetState(TwoDButton, twoD);
            SetState(ThreeDButton, !twoD);
        }

        private void SetState(Button button, bool active)
        {
            if (button == null) return;

            button.interactable = !active;
            if (button.image != null) button.image.color = active ? HudTheme.ActiveFill : HudTheme.NeutralFill;

            // Only the active segment is outlined; the idle one is a bare fill (kit ViewToggle).
            Transform border = button.transform.Find("Border");
            if (border != null)
            {
                var borderImage = border.GetComponent<Image>();
                if (borderImage != null) borderImage.color = active ? HudTheme.Accent : Color.clear;
            }

            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.color = active ? HudTheme.ActiveText : HudTheme.IdleLabel;
        }

        private static void Wire(Button button, UnityEngine.Events.UnityAction handler)
        {
            if (button == null) return;

            button.onClick.RemoveListener(handler);
            button.onClick.AddListener(handler);
        }

        /// <summary>The kit's ViewToggle: one panel, two 66 x 38 segments, 3 px of inner padding.</summary>
        private void BuildButtons()
        {
            _root = NewUiObject("ViewToggle", _canvas.transform);
            _root.anchorMin = new Vector2(0.5f, 1f);
            _root.anchorMax = new Vector2(0.5f, 1f);
            _root.pivot = new Vector2(0.5f, 1f);
            _root.anchoredPosition = new Vector2(0f, -topMargin);
            _root.sizeDelta = new Vector2(buttonWidth * 2f + spacing + 6f, buttonHeight + 6f);
            HudTheme.AddPanelBackground(_root, HudTheme.PanelFill, HudTheme.PanelBorder);

            float offset = (buttonWidth + spacing) * 0.5f;
            TwoDButton = CreateButton("Seg2D", "2D", -offset);
            ThreeDButton = CreateButton("Seg3D", "3D", offset);
        }

        private Button CreateButton(string objectName, string label, float x)
        {
            RectTransform rt = NewUiObject(objectName, _root);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, 0f);
            rt.sizeDelta = new Vector2(buttonWidth, buttonHeight);

            Image background = rt.gameObject.AddComponent<Image>();
            background.sprite = HudTheme.Sprite("row_fill_9s");
            if (background.sprite != null) background.type = Image.Type.Sliced;
            background.color = HudTheme.NeutralFill;

            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            // The active/inactive colour IS the state readout; the built-in tint would multiply on top of it and
            // grey out the highlight the moment the active button goes non-interactable.
            button.transition = Selectable.Transition.None;

            HudTheme.AddImage(rt, "Border", "panel_border_9s", Color.clear);

            RectTransform labelRt = NewUiObject("Label", rt);
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            TextMeshProUGUI text = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.characterSpacing = HudTheme.Tracking(100f);
            text.color = HudTheme.IdleLabel;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.text = label;

            return button;
        }

        private static RectTransform NewUiObject(string objectName, Transform parent)
        {
            GameObject go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private void Unsubscribe()
        {
            if (_modes == null) return;
            _modes.ModeChanged -= OnModeChanged;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            _modes = null;
        }
    }
}
