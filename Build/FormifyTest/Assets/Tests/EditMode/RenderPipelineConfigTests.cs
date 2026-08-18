using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;

namespace Formify.Tests.EditMode
{
    /// <summary>
    /// OUT-01 AC1. The two stencil RenderObjects features that draw the outline live on Mobile_Renderer only
    /// (C-07: iOS is the shipping target, so the Mobile pair is the one that is maintained). A quality level
    /// pointing at PC_RPAsset therefore renders no outline at all, and AD-001 makes the Editor the validation
    /// target — the setting is part of the feature, not cosmetics.
    /// </summary>
    public class RenderPipelineConfigTests
    {
        [Test]
        public void ActiveRenderPipeline_IsTheOneHostingTheOutlineFeatures()
        {
            RenderPipelineAsset active = QualitySettings.renderPipeline != null
                ? QualitySettings.renderPipeline
                : GraphicsSettings.defaultRenderPipeline;

            Assert.IsNotNull(active, "URP asset missing: the project would fall back to the built-in pipeline");
            Assert.AreEqual("Mobile_RPAsset", active.name,
                "the active quality level must use the renderer that carries the outline passes");
        }

        [Test]
        public void SelectedSurfaceLayer_Exists()
        {
            Assert.AreNotEqual(-1, LayerMask.NameToLayer("SelectedSurface"),
                "the outline features filter on this layer; without it the swap silently does nothing");
        }
    }
}
