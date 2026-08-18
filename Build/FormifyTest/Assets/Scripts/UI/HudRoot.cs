using UnityEngine;
using UnityEngine.UI;

namespace Formify.Presentation
{
    /// <summary>
    /// The HUD as a scene object (AD-025). Every view under this root is authored into the scene by the editor
    /// bake (<c>Formify/Bake HUD Into Scene</c>) and keeps its own serialized references, so play builds none of
    /// it: <see cref="RoomBootstrap"/> finds this root and binds the model and the modes to what the scene
    /// already holds. The room itself stays generated (ROOM-01) — only the HUD is authored.
    /// <see cref="Build"/> is the single construction path left. The bake runs it, and a scene with no baked HUD
    /// (a bare test scene) falls back to it, so the built and the baked HUD are the same tree by construction.
    /// </summary>
    [DisallowMultipleComponent]
    public class HudRoot : MonoBehaviour
    {
        // The art kit's RightRail block, in its reference pixels (HUD-01 AC2).
        private const float RailWidth = 264f;
        private const float RailInset = 14f;
        private static readonly Vector2 RailButtonSize = new Vector2(212f, 46f);
        private static readonly Vector2 ReadoutPosition = new Vector2(8f, -404f);
        private static readonly Vector2 ReadoutSize = new Vector2(250f, 92f);

        [SerializeField] private Canvas canvas;
        [SerializeField] private SurfaceListPanel listPanel;
        [SerializeField] private RectTransform rail;
        [SerializeField] private HudButton windowHudButton;
        [SerializeField] private WindowModeButton windowMode;
        [SerializeField] private HudButton clearHudButton;
        [SerializeField] private ClearButton clear;
        [SerializeField] private HudButton arHudButton;
        [SerializeField] private ArToggleButton arToggle;
        [SerializeField] private ViewSwitchButtons viewSwitch;
        [SerializeField] private HudReadout readout;
        [SerializeField] private HudHintPill hintPill;

        /// <summary>The screen-space canvas every HUD view and every window overlay hangs off.</summary>
        public Canvas Canvas => canvas;

        public SurfaceListPanel ListPanel => listPanel;

        /// <summary>The art kit's right-hand action rail.</summary>
        public RectTransform Rail => rail;

        public HudButton WindowHudButton => windowHudButton;

        public WindowModeButton WindowMode => windowMode;

        public HudButton ClearHudButton => clearHudButton;

        public ClearButton Clear => clear;

        public HudButton ArHudButton => arHudButton;

        public ArToggleButton ArToggle => arToggle;

        public ViewSwitchButtons ViewSwitch => viewSwitch;

        public HudReadout Readout => readout;

        public HudHintPill HintPill => hintPill;

        /// <summary>
        /// Builds the whole kit under a fresh root: canvas and surfaces panel, the right rail with its three
        /// buttons, the view toggle, the readout, the hint pill and the scanline overlay, in that draw order.
        /// Nothing is bound to a model here — <see cref="RoomBootstrap.Compose"/> owns every binding, baked or
        /// built, so both paths behave the same.
        /// </summary>
        public static HudRoot Build(Transform parent)
        {
            var go = new GameObject("HUD");
            if (parent != null) go.transform.SetParent(parent, false);

            var hud = go.AddComponent<HudRoot>();
            hud.listPanel = go.AddComponent<SurfaceListPanel>();
            hud.canvas = hud.listPanel.EnsureCanvas();

            hud.rail = BuildRightRail(hud.canvas);

            hud.windowHudButton = RailButton(hud.rail, "BtnWindowMode", "icon_window", "Window mode", -12f, true);
            hud.windowMode = hud.windowHudButton.gameObject.AddComponent<WindowModeButton>();
            hud.windowMode.StateDot = hud.windowHudButton.Dot;

            hud.clearHudButton = RailButton(hud.rail, "BtnClear", "icon_trash", "Clear", -68f, false);
            hud.clearHudButton.UseNeutralPalette = true;
            hud.clearHudButton.Apply();
            hud.clear = hud.clearHudButton.gameObject.AddComponent<ClearButton>();

            RailDivider(hud.rail, -124f);

            hud.arHudButton = RailButton(hud.rail, "BtnAR", "icon_ar", "View in AR", -132f, false);
            hud.arToggle = hud.arHudButton.gameObject.AddComponent<ArToggleButton>();

            hud.viewSwitch = go.AddComponent<ViewSwitchButtons>();
            // No modes yet: this only puts the toggle on the canvas. Compose binds it.
            hud.viewSwitch.Configure(null, hud.canvas);

            hud.readout = HudReadout.Create(hud.canvas.transform, ReadoutPosition, ReadoutSize);
            hud.hintPill = HudHintPill.Create(hud.canvas.transform);

            // Last, so it covers the HUD. Raycast Target is off inside AddScanlines (HUD-01 AC4).
            HudTheme.AddScanlines(hud.canvas);
            return hud;
        }

        /// <summary>
        /// The art kit's `RightRail` (HUD-01 AC2): a near-opaque column down the right edge holding the three
        /// action buttons. Unlike the decoration on the canvas, the rail fill IS a raycast target — it is a
        /// panel, so a tap on it must not fall through into the room behind it (EDGE-02).
        /// </summary>
        private static RectTransform BuildRightRail(Canvas canvas)
        {
            RectTransform rail = HudTheme.NewUi("RightRail", canvas.transform);
            rail.anchorMin = new Vector2(1f, 0f);
            rail.anchorMax = new Vector2(1f, 1f);
            rail.pivot = new Vector2(1f, 0.5f);
            rail.offsetMin = new Vector2(-RailWidth, 0f);
            rail.offsetMax = Vector2.zero;

            HudTheme.AddImage(rail, "RailFill", "panel_fill_9s", HudTheme.RailFill, Image.Type.Sliced,
                raycastTarget: true);
            return rail;
        }

        /// <summary>One 212 x 46 rail button, `top` px below the rail's top edge and inset 14 px from its right.</summary>
        private static HudButton RailButton(RectTransform rail, string objectName, string iconSprite, string label,
            float top, bool withStateDot)
        {
            HudButton button = HudButton.Create(rail, objectName, iconSprite, label, RailButtonSize, withStateDot);
            ((RectTransform)button.transform).anchoredPosition = new Vector2(-RailInset, top);
            return button;
        }

        private static void RailDivider(RectTransform rail, float top)
        {
            RectTransform band = HudTheme.NewUi("Divider", rail);
            band.anchorMin = new Vector2(1f, 1f);
            band.anchorMax = new Vector2(1f, 1f);
            band.pivot = new Vector2(1f, 1f);
            band.anchoredPosition = new Vector2(-RailInset, top);
            band.sizeDelta = new Vector2(RailButtonSize.x, 1f);
            HudTheme.AddDivider(band);
        }
    }
}
