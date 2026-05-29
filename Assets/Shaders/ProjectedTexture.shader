Shader "BlobPreviz/ProjectedTexture"
{
    // Simulates a top-down projector casting a texture onto any surface.
    // Works on static meshes and skinned characters.
    //
    // Globals (set by ProjectorController.cs — no per-material assignment needed):
    //   _ProjectorTex  (Texture2D)  — the projected image
    //   _ProjectorVP   (float4x4)   — projector view-projection matrix
    //   _ProjectorPos  (float3)     — projector world position (for facing test)
    //
    // Per-material:
    //   _BaseColor / _BaseMap       — surface base appearance
    //   _ProjectionStrength         — 0 = base only, 1 = full projection replaces base
    //   _ProjectionTint             — colour-correct the projected image
    //   _FlipProjectionY            — toggle if projection appears upside down (platform-specific)

    Properties
    {
        [MainColor] _BaseColor          ("Base Color",          Color)          = (0.5, 0.5, 0.5, 1)
        [MainTexture] _BaseMap          ("Base Texture",        2D)             = "white" {}
        _ProjectionStrength             ("Projection Strength", Range(0, 1))    = 1.0
        _ProjectionTint                 ("Projection Tint",     Color)          = (1, 1, 1, 1)
        _FacingSharpness                ("Facing Sharpness",    Range(1, 16))   = 4.0
        [Toggle] _FlipProjectionY       ("Flip Projection Y",   Float)          = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"        = "Opaque"
            "RenderPipeline"    = "UniversalPipeline"
            "Queue"             = "Geometry"
        }

        // ── Main forward pass ────────────────────────────────────────────────
        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── Per-material properties ──────────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float  _ProjectionStrength;
                float4 _ProjectionTint;
                float  _FacingSharpness;
                float  _FlipProjectionY;
            CBUFFER_END

            TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);

            // ── Global projector properties (set by ProjectorController.cs) ──
            TEXTURE2D(_ProjectorTex);   SAMPLER(sampler_ProjectorTex);
            float4x4 _ProjectorVP;
            float3   _ProjectorPos;

            // ── Vertex ───────────────────────────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                // GetVertexPositionInputs handles skinned meshes correctly —
                // GPU skinning has already run by this point.
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            // ── Fragment ─────────────────────────────────────────────────────
            float4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                // Base surface colour
                float4 base = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                // Transform world position into projector clip space
                float4 projPos = mul(_ProjectorVP, float4(IN.positionWS, 1.0));

                // Perspective divide → NDC (-1..1)
                float2 ndc = projPos.xy / projPos.w;

                // Remap NDC to UV (0..1)
                float2 projUV = ndc * 0.5 + 0.5;

                // Optional Y-flip for render-texture sources or platform differences
                projUV.y = lerp(projUV.y, 1.0 - projUV.y, _FlipProjectionY);

                // Frustum mask: discard projection outside projector bounds and behind it
                float inBounds = step(0.0, projPos.w)
                               * step(0.0, projUV.x) * step(projUV.x, 1.0)
                               * step(0.0, projUV.y) * step(projUV.y, 1.0);

                // Facing mask: surfaces whose normal points away from the projector
                // receive no projection — just like a real projector can't light a back face.
                // Perspective-correct: direction is per-fragment from surface to projector.
                float3 toProjector  = normalize(_ProjectorPos - IN.positionWS);
                float3 normal       = normalize(IN.normalWS);
                float  facing       = dot(normal, toProjector);
                // Raise to _FacingSharpness for a soft rolloff near grazing angles.
                // At sharpness=1 it's a linear fade; higher values give a harder edge.
                float  facingMask   = pow(saturate(facing), _FacingSharpness);

                float4 projSample = SAMPLE_TEXTURE2D(_ProjectorTex, sampler_ProjectorTex, projUV);
                float4 projColor  = projSample * _ProjectionTint;

                // Blend: all masks combined → lerp toward projected colour
                float blend = _ProjectionStrength * inBounds * facingMask * projSample.a;
                return lerp(base, projColor, blend);
            }
            ENDHLSL
        }

        // ── Depth-only pass (required by URP depth prepass) ──────────────────
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float  _ProjectionStrength;
                float4 _ProjectionTint;
                float  _FlipProjectionY;
            CBUFFER_END

            struct DA { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct DV { float4 positionCS : SV_POSITION; };

            DV DepthVert(DA IN)
            {
                DV OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }
            half4 DepthFrag(DV IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // ── Shadow caster pass ───────────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float  _ProjectionStrength;
                float4 _ProjectionTint;
                float  _FlipProjectionY;
            CBUFFER_END

            float3 _LightDirection;

            struct SA { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct SV { float4 positionCS : SV_POSITION; };

            SV ShadowVert(SA IN)
            {
                SV OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                float3 posWS  = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionCS = TransformWorldToHClip(ApplyShadowBias(posWS, normWS, _LightDirection));
                return OUT;
            }
            half4 ShadowFrag(SV IN) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
