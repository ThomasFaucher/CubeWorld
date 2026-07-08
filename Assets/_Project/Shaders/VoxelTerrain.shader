// Shader du terrain voxel — style CubeWorld : la couleur vient des couleurs
// de vertex (aucune texture), éclairage lambert + ombres de la lumière
// principale. Les normales par face fournissent le flat shading.
Shader "CubeWorld/VoxelTerrain"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionHCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                return output;
            }

            // Éclairage en paliers (cel shading) plutôt qu'un dégradé lambert
            // continu : avec des couleurs de vertex unies, un dégradé lisse
            // aplatit visuellement le relief des cubes. Des bandes franches
            // (plein jour / pénombre / ombre) font au contraire ressortir
            // chaque face selon son orientation à la lumière, sans avoir
            // besoin de varier la couleur elle-même.
            half CelBand(half ndotl, half shadowAtten, half distAtten)
            {
                half lightAmount = saturate(ndotl * shadowAtten * distAtten);

                // Bandes resserrées (jamais trop sombres) : le relief doit se
                // voir sans que les faces à l'ombre virent au gris terne.
                if (lightAmount > 0.5h) { return 1.05h; }
                if (lightAmount > 0.12h) { return 0.78h; }
                return 0.58h;
            }

            // Ressature la couleur finale autour de sa luminance : compense le
            // fait qu'une couleur unie multipliée par un éclairage < 1 tend
            // vers le gris et paraît "fade" — garde des teintes franches façon
            // cartoon quel que soit le palier de lumière.
            half3 BoostSaturation(half3 color, half amount)
            {
                half luma = dot(color, half3(0.299h, 0.587h, 0.114h));
                return lerp(luma.xxx, color, amount);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);

                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half band = CelBand(ndotl, mainLight.shadowAttenuation, mainLight.distanceAttenuation);
                half3 lighting = SampleSH(normalWS) + mainLight.color * band;

                half3 finalColor = BoostSaturation(input.color.rgb * lighting, 1.35h);
                return half4(finalColor, 1.0h);
            }
            ENDHLSL
        }

        // Projette les ombres du terrain (sur lui-même et sur le reste).
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                // Décale la position le long de la normale/lumière pour éviter l'acné d'ombre.
                float4 positionHCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                positionHCS.z = min(positionHCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                positionHCS.z = max(positionHCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                Varyings output;
                output.positionHCS = positionHCS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        // Écrit la profondeur (requise par certains effets URP, ex. SSAO, depth priming).
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
}
