using System.Collections.Generic;
using Formify.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Formify.EditorTools
{
    /// <summary>
    /// Authors the HUD into the open scene as real GameObjects (AD-025). The kit has no prefab and every view is
    /// described in code, so the bake runs that description once, here, instead of on every play:
    /// <see cref="RoomBootstrap.Compose"/> builds the HUD on a throwaway scaffold, the HUD root is lifted into
    /// the scene, and the scaffold (bootstrap, controllers, camera) is deleted. What stays is the live HUD —
    /// canvas, buttons and TextMeshPro labels the inspector can edit — and play only binds the model to it.
    /// Everything here works on the active scene alone, so a bake never reaches into another loaded scene.
    /// Re-bake after changing the kit in code: the scene copy does not follow code.
    /// </summary>
    public static class HudSceneBaker
    {
        private const string HudName = "HUD";
        private const string ScaffoldName = "~HudBakeScaffold";

        [MenuItem("Formify/Bake HUD Into Scene")]
        public static void Bake()
        {
            Clear();
            EnsureEventSystem();

            HudRoot hud = null;
            var scaffold = new GameObject(ScaffoldName);
            try
            {
                RoomBootstrap bootstrap = scaffold.AddComponent<RoomBootstrap>();

                // Compose falls back to Camera.main when no camera is assigned, and would hang the orbit
                // controller off the scene's real camera. A throwaway camera keeps every side effect inside
                // the scaffold we are about to delete.
                var camera = new GameObject("~BakeCamera", typeof(Camera));
                camera.transform.SetParent(scaffold.transform, false);
                camera.GetComponent<Camera>().enabled = false;

                var serialized = new SerializedObject(bootstrap);
                serialized.FindProperty("roomCamera").objectReferenceValue = camera.GetComponent<Camera>();
                serialized.ApplyModifiedPropertiesWithoutUndo();

                bootstrap.Compose();

                hud = bootstrap.Hud;
                hud.name = HudName;
                hud.transform.SetParent(null, false);
            }
            finally
            {
                Object.DestroyImmediate(scaffold);
            }

            WireSceneBootstrap(hud);
            Selection.activeGameObject = hud.gameObject;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("Formify/Remove HUD From Scene")]
        public static void Clear()
        {
            List<HudRoot> baked = FindAllInActiveScene<HudRoot>();

            for (int i = 0; i < baked.Count; i++) Object.DestroyImmediate(baked[i].gameObject);
            if (baked.Count > 0) EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// Points the scene's bootstrap at what was just baked. It would find the HUD on its own, but a wired
        /// reference names which one in the inspector and holds even if the scene later grows a second canvas.
        /// </summary>
        private static void WireSceneBootstrap(HudRoot hud)
        {
            RoomBootstrap bootstrap = FindFirstInActiveScene<RoomBootstrap>();
            if (bootstrap == null) return;

            var serialized = new SerializedObject(bootstrap);
            serialized.FindProperty("hud").objectReferenceValue = hud;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// SurfaceListPanel creates its own EventSystem when the scene has none, and tears it down with
        /// <c>Destroy</c> — which throws in edit mode. A scene-owned EventSystem is what a uGUI scene wants
        /// anyway, and it keeps the bake's teardown silent.
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (FindFirstInActiveScene<EventSystem>() != null) return;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private static T FindFirstInActiveScene<T>() where T : Component
        {
            List<T> found = FindAllInActiveScene<T>();
            return found.Count > 0 ? found[0] : null;
        }

        /// <summary>
        /// The active scene only. <c>FindObjectsByType</c> spans every loaded scene, and under the test runner
        /// (or any additive setup) that would let a bake delete the HUD out of a scene it was never aimed at.
        /// </summary>
        private static List<T> FindAllInActiveScene<T>() where T : Component
        {
            var found = new List<T>();
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++) found.AddRange(roots[i].GetComponentsInChildren<T>(true));
            return found;
        }
    }
}
