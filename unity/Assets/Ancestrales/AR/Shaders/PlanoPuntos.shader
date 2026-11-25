// Planos detectados: puntitos en coordenadas de mundo (no dependen del UV del plano).
Shader "Ancestrales/AR/PlanoPuntos"
{
    Properties
    {
        _Color ("Color", Color) = (0.94,0.89,0.78,0.7)
        _Densidad ("Puntos por metro", Float) = 14
        _Radio ("Radio del punto", Range(0.02,0.5)) = 0.13
        _Visible ("Visible", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent-20" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color; float _Densidad; float _Radio; float _Visible;
            struct v2f { float4 pos : SV_POSITION; float3 mundo : TEXCOORD0; };
            v2f vert (appdata_base v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.mundo = mul(unity_ObjectToWorld, v.vertex).xyz; return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float2 c = frac(i.mundo.xz * _Densidad) - 0.5;
                float d = length(c);
                float punto = 1 - smoothstep(_Radio - 0.04, _Radio, d);
                // Se desvanece con la distancia a la cámara para no saturar.
                float lejos = saturate(1.6 - distance(i.mundo, _WorldSpaceCameraPos) * 0.3);
                return fixed4(_Color.rgb, _Color.a * punto * lejos * _Visible);
            }
            ENDCG
        }
    }
}
