// Full screen dim with a rounded hole. Works in Built-in, URP and HDRP because it is a plain UI shader.
Shader "Hidden/SmartTooltip/Cutout"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Hole ("Hole (xMin, yMin, xMax, yMax) in pixels", Vector) = (0, 0, 0, 0)
        _ScreenSize ("Screen Size in pixels", Vector) = (1, 1, 0, 0)
        _Radius ("Corner Radius in pixels", Float) = 16
        _Softness ("Edge Softness in pixels", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            float4 _Hole;
            float4 _ScreenSize;
            float _Radius;
            float _Softness;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 p = i.uv * _ScreenSize.xy;
                float2 center = (_Hole.xy + _Hole.zw) * 0.5;
                float2 halfSize = max((_Hole.zw - _Hole.xy) * 0.5, 0);
                float radius = min(_Radius, min(halfSize.x, halfSize.y));
                float2 q = abs(p - center) - halfSize + radius;
                float distance = length(max(q, 0)) + min(max(q.x, q.y), 0) - radius;
                float softness = max(_Softness, 0.001);
                float dim = smoothstep(-softness, softness, distance);

                fixed4 c = i.color;
                c.a *= dim;
                return c;
            }
            ENDCG
        }
    }
}
