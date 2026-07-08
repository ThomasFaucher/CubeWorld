// Shader de l'eau voxel — variante transparente de CubeWorld/VoxelTerrain :
// même éclairage lambert + couleurs de vertex, mais avec alpha blending et
// sans écriture de profondeur, pour laisser voir le terrain immergé.
Shader "CubeWorld/VoxelWater"
{
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

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

            // Même éclairage en paliers que CubeWorld/VoxelTerrain (voir ce
            // shader pour le détail) : garde l'eau cohérente avec le style
            // cartoon du reste du terrain.
            half CelBand(half ndotl, half shadowAtten, half distAtten)
            {
                half lightAmount = saturate(ndotl * shadowAtten * distAtten);

                if (lightAmount > 0.5h) { return 1.05h; }
                if (lightAmount > 0.12h) { return 0.78h; }
                return 0.58h;
            }

            // Voir VoxelTerrain.shader : évite qu'une couleur unie assombrie
            // par l'éclairage ne vire au gris terne.
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
                return half4(finalColor, input.color.a);
            }
            ENDHLSL
        }
    }
}
