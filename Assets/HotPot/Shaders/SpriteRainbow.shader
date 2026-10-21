// EN: Sprite shader that tints the sprite with a moving rainbow band (used on dish lids while the Shuffle booster runs).
// VI: Shader sprite phủ dải cầu vồng chạy ngang (dùng cho nắp đĩa khi booster Xáo trộn đang chạy).
Shader "FoodPop/SpriteRainbow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Strength ("Độ mạnh cầu vồng", Range(0, 1)) = 0.65
        _Speed ("Tốc độ chạy", Float) = 1.5
        _Scale ("Mật độ dải màu", Float) = 1.2
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Strength;
            float _Speed;
            float _Scale;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; float2 world : TEXCOORD1; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.world = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                float hue = frac((i.world.x + i.world.y) * _Scale * 0.5 - _Time.y * _Speed);
                float3 rainbow = saturate(abs(frac(hue + float3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 1.0);
                float luminance = dot(col.rgb, float3(0.299, 0.587, 0.114));
                float3 tinted = rainbow * (0.45 + 0.75 * luminance) + col.rgb * 0.25;
                col.rgb = lerp(col.rgb, tinted, _Strength);
                return col;
            }
            ENDCG
        }
    }
}
