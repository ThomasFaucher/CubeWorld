// Shader des personnages voxel — même famille que CubeWorld/VoxelTerrain
// (couleurs de vertex, cel shading, ombres) mais réglé pour le CONTRASTE :
// bandes d'éclairage plus creuses, ambiant réduit, et occlusion ambiante
// bakée lue dans l'ALPHA des couleurs de vertex (calculée par VoxelGridMesher
// depuis la grille voxel de chaque pièce : creuse les jointures, aisselles,
// sous le menton...). Les normales par face restent dures : aucun lissage.
Shader "CubeWorld/VoxelCharacter"
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

            // Bandes cel plus CREUSES que le terrain : sur un personnage vu de
            // près, les faces à l'ombre doivent être franchement sombres pour
            // que chaque cube se détache — c'est ce contraste par face qui
            // donne le relief "CubeWorld", pas la géométrie.
            half CelBand(half ndotl, half shadowAtten, half distAtten)
            {
                half lightAmount = saturate(ndotl * shadowAtten * distAtten);

                if (lightAmount > 0.5h) { return 1.08h; }
                if (lightAmount > 0.12h) { return 0.66h; }
                return 0.40h;
            }

            // Ressature la couleur finale autour de sa luminance : garde des
            // teintes franches façon cartoon quel que soit le palier de lumière.
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

                // Ambiant réduit (x0.55) : c'est lui qui "remontait" toutes les
                // ombres du terrain ; ici on le contient pour garder les creux.
                half3 lighting = SampleSH(normalWS) * 0.55h + mainLight.color * band;

                // Occlusion ambiante bakée (alpha vertex) : assombrit les
                // recoins quelle que soit la direction de la lumière.
                half ao = lerp(0.45h, 1.0h, input.color.a);
                lighting *= ao;

                half3 finalColor = BoostSaturation(input.color.rgb * lighting, 1.35h);
                return half4(finalColor, 1.0h);
            }
            ENDHLSL
        }

        // Projette les ombres du personnage (sur lui-même et sur le monde).
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
