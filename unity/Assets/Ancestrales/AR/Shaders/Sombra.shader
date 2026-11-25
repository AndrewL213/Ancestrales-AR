// Sombra de contacto: oscurece el video de la cámara bajo la criatura.
Shader "Ancestrales/AR/Sombra"
{
    Properties
    {
        _MainTex ("Degradado", 2D) = "white" {}
        _Color ("Color", Color) = (0,0,0,0.55)
    }
    SubShader
    {
        Tags { "Queue"="Transparent-10" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Offset -1, -1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; fixed4 _Color;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert (appdata_base v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord.xy; return o; }
            fixed4 frag (v2f i) : SV_Target { fixed a = tex2D(_MainTex, i.uv).a; return fixed4(_Color.rgb, _Color.a * a); }
            ENDCG
        }
    }
}
