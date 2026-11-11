// El cerro del horizonte. Se dibuja antes que todo lo demás (cola Background) y sin escribir ni probar profundidad, así lo
// que esté en la avenida siempre queda enfrente aunque el cerro esté más cerca de la cámara que algunos edificios. El color
// sale de los vértices (las caras ya traen su sombra, al estilo low poly) y se mezcla con el de la niebla, que LuzDelDia
// cambia con el cielo de cada etapa: la bruma de la distancia. Los colores de los vértices se guardaron como se ven (sRGB) y
// el proyecto calcula en espacio lineal: se pasan a lineal antes de mezclarlos, o el cerro sale más claro de lo que es.
Shader "Ruta/Horizonte"
{
    Properties
    {
        _Bruma ("Bruma", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "Horizonte"
            Tags { "LightMode" = "UniversalForward" }
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half _Bruma;
            CBUFFER_END

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.color = i.color;
                return o;
            }

            // De sRGB a lineal, con la aproximación que usa Unity para los colores en espacio lineal.
            half3 ALineal(half3 c)
            {
            #if defined(UNITY_COLORSPACE_GAMMA)
                return c;
            #else
                return c * (c * (c * 0.305306011h + 0.682171111h) + 0.012522878h);
            #endif
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(lerp(ALineal(i.color.rgb), unity_FogColor.rgb, _Bruma), 1);
            }
            ENDHLSL
        }
    }
}
