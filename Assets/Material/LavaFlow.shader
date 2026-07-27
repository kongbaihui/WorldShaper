Shader "Custom/2D/LavaFlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _DeepColor ("Deep Lava", Color) = (0.18,0.005,0.001,1)
        [HDR] _HotColor ("Hot Lava", Color) = (1.0,0.12,0.005,1)
        [HDR] _CoreColor ("Molten Core", Color) = (1.0,0.72,0.08,1)
        _FlowSpeed ("Flow Speed", Range(0,4)) = 0.65
        _Scale ("Pattern Scale", Range(0.05,2)) = 0.28
        _Distortion ("Distortion", Range(0,2)) = 0.8
        _Glow ("Core Glow", Range(0,3)) = 1.35
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _DeepColor;
            fixed4 _HotColor;
            fixed4 _CoreColor;
            float _FlowSpeed;
            float _Scale;
            float _Distortion;
            float _Glow;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                o.color = fixed4(_Color.rgb, _Color.a * v.color.a);
                #ifdef PIXELSNAP_ON
                o.vertex = UnityPixelSnap(o.vertex);
                #endif
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed spriteAlpha = tex2D(_MainTex, i.texcoord).a * i.color.a;
                float t = _Time.y * _FlowSpeed;
                float2 p = i.worldPos * float2(_Scale, _Scale * 1.75);

                float warpA = sin(p.x * 0.72 + t * 0.82) * cos(p.y * 1.18 - t * 0.55);
                float warpB = sin(p.x * 1.63 - p.y * 0.91 - t * 0.38);
                float warp = (warpA + warpB * 0.45) * _Distortion;

                float broadFlow = sin(p.x * 1.08 + p.y * 0.52 + t + warp * 1.42);
                float moltenCells = sin((p.x + warp) * 2.36 - t * 0.74) *
                                    cos((p.y - warp * 0.55) * 2.94 + t * 0.46);
                float heat = saturate(0.48 + broadFlow * 0.27 + moltenCells * 0.25);

                float veinSignal = sin(p.x * 3.15 - p.y * 1.28 + t * 1.16 + warp * 2.1);
                float hotVeins = pow(saturate(1.0 - abs(veinSignal)), 7.0);
                float ember = pow(saturate(0.5 + 0.5 * sin(p.x * 5.1 + p.y * 3.7 - t * 1.7)), 10.0);

                fixed3 lava = lerp(_DeepColor.rgb, _HotColor.rgb, smoothstep(0.12, 0.88, heat));
                lava = lerp(lava, _CoreColor.rgb, saturate(hotVeins * _Glow + ember * 0.35));

                fixed4 result = fixed4(lava * spriteAlpha, spriteAlpha);
                return result;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
