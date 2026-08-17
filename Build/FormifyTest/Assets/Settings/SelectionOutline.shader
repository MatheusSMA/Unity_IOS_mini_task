// Override material for the two RenderObjects features that draw the selection outline (OUT-01, AD-010).
//
// Pass 0 "SelectionOutlineMask" is the MARK pass: it stamps stencil ref 1 over the selected surface's
// screen silhouette and writes no colour.
// Pass 1 "SelectionOutlineEdge" is the EDGE pass: it inflates the geometry and draws only where the
// stencil is NOT 1, i.e. the ring outside the silhouette.
//
// Both passes declare their own Stencil block, so the material still behaves correctly if a feature's
// stencil override is switched off. Both use ZTest Always on purpose: the mark and the edge must agree
// about occlusion, and an outline that vanishes at every wall-to-wall corner (which LEqual gives you,
// because the neighbouring wall is nearer than the selected wall's edge) fails "visible from any angle".
Shader "Formify/SelectionOutline"
{
    Properties
    {
        [HDR] _OutlineColor("Outline Colour", Color) = (1, 0.45, 0.05, 1)
        _OutlineWidth("Outline Width (metres)", Range(0, 0.25)) = 0.03
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    // One shared UnityPerMaterial block across both passes keeps the material SRP Batcher compatible.
    CBUFFER_START(UnityPerMaterial)
        half4 _OutlineColor;
        float _OutlineWidth;
    CBUFFER_END
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Name "SelectionOutlineMask"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Back
            ZWrite Off
            ZTest Always
            ColorMask 0

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma vertex MarkVertex
            #pragma fragment MarkFragment

            float4 MarkVertex(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half4 MarkFragment() : SV_Target
            {
                return half4(0, 0, 0, 0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "SelectionOutlineEdge"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite Off
            ZTest Always

            Stencil
            {
                Ref 1
                Comp NotEqual
                Pass Keep
            }

            HLSLPROGRAM
            #pragma vertex EdgeVertex
            #pragma fragment EdgeFragment

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            float4 EdgeVertex(Attributes IN) : SV_POSITION
            {
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);

                // A pure normal offset does NOT work on this project's meshes. SurfaceMeshBuilder emits
                // four fresh vertices per quad, so Mesh.RecalculateNormals produces FLAT face normals:
                // extruding the slab along them pushes the front face towards the camera and the side
                // quads edge-on, leaving the head-on silhouette exactly where it was -> no ring at all.
                // Blending in the outward direction from the renderer's world bounds centre is what
                // makes the edge appear. On a smoothly-normalled mesh the two directions agree, so this
                // degrades to the textbook normal extrusion.
                float3 centreWS = (unity_RendererBounds_Min.xyz + unity_RendererBounds_Max.xyz) * 0.5;
                float3 radialWS = SafeNormalize(positionWS - centreWS);
                positionWS += SafeNormalize(normalWS + radialWS) * _OutlineWidth;

                return TransformWorldToHClip(positionWS);
            }

            half4 EdgeFragment() : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
