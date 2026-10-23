// EN: Unlit sprite shader with additive blending: colours add light to what is behind them, so overlapping glows brighten instead of turning muddy (used by booster light effects).
// VI: Shader sprite không chiếu sáng với chế độ cộng sáng: màu cộng thêm ánh sáng vào phía sau, nên các quầng sáng chồng lên nhau sáng rực thay vì bị đục (dùng cho hiệu ứng ánh sáng của booster).
Shader "FoodPop/SpriteAdditive"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Lighting Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex;

            // EN: Passes position, UV and the sprite colour through.
            // VI: Chuyển tiếp vị trí, UV và màu sprite.
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            // EN: Texture times sprite colour; the blend mode adds it to the screen.
            // VI: Texture nhân màu sprite; chế độ blend cộng nó vào màn hình.
            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * i.color;
            }
            ENDCG
        }
    }
}
