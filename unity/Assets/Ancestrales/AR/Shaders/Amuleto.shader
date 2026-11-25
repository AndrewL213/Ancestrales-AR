// Amuleto: dorado con luz falsa y borde brillante (no depende de las luces de la escena).
Shader "Ancestrales/AR/Amuleto"
{
    Properties
    {
        _Color ("Color", Color) = (0.95,0.72,0.25,1)
        _Borde ("Color del borde", Color) = (1,0.95,0.7,1)
    }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color; fixed4 _Borde;
            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; };
            v2f vert (appdata_base v)
            {
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.v = normalize(WorldSpaceViewDir(v.vertex)); return o;
            }
            fixed4 frag (v2f i) : SV_Target
            {
                float3 n = normalize(i.n);
                float luz = saturate(dot(n, normalize(float3(0.3, 0.9, -0.3)))) * 0.7 + 0.35;
                float borde = pow(1 - saturate(dot(n, normalize(i.v))), 3);
                float brillo = pow(saturate(dot(reflect(-normalize(i.v), n), normalize(float3(0.3, 0.9, -0.3)))), 24);
                return fixed4(_Color.rgb * luz + _Borde.rgb * (borde * 0.8 + brillo), 1);
            }
            ENDCG
        }
    }
}
