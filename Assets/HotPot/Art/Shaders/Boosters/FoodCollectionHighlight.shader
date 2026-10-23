Shader "FoodPop/FoodCollectionHighlight"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _FlashStrength ("Flash", Range(0,1)) = 0
        _GlowColor ("Glow", Color) = (1,0.88,0.4,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _FlashStrength;
            fixed4 _GlowColor;
            // EN: Passes sprite UV and tint to the collection highlight shader.
            // VI: Truyền UV và màu sprite cho shader làm sáng khi thu món.
            Output vert(Input v)
            {
                Output o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }
            // EN: Adds a warm edge and brief white flash while preserving the food's alpha silhouette.
            // VI: Thêm viền ấm và nháy trắng ngắn, giữ hình alpha của food.
            fixed4 frag(Output i) : SV_Target
            {
                fixed4 food = tex2D(_MainTex, i.uv) * i.color;
                float2 step = _MainTex_TexelSize.xy * 3;
                float edge = max(max(tex2D(_MainTex, i.uv + float2(step.x,0)).a, tex2D(_MainTex, i.uv - float2(step.x,0)).a),
                                 max(tex2D(_MainTex, i.uv + float2(0,step.y)).a, tex2D(_MainTex, i.uv - float2(0,step.y)).a));
                float glow = saturate(edge - food.a) * _FlashStrength;
                food.rgb = lerp(food.rgb, fixed3(1,1,0.95), _FlashStrength * 0.35) * food.a;
                food.rgb += _GlowColor.rgb * glow;
                food.a = saturate(food.a + glow);
                return food;
            }
            ENDCG
        }
    }
}
