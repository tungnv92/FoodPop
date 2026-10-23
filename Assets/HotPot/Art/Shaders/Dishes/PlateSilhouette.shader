Shader "FoodPop/PlateSilhouette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex;

            // EN: Passes sprite geometry and tint to the silhouette renderer.
            // VI: Chuyển hình học sprite và màu tới bộ vẽ bóng hình.
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            // EN: Uses only the plate alpha so white selection and red rejection follow its exact silhouette.
            // VI: Chỉ dùng alpha đĩa để viền chọn trắng và báo từ chối đỏ bám đúng hình dáng.
            fixed4 frag(v2f i) : SV_Target
            {
                fixed alpha = smoothstep(0.3, 0.6, tex2D(_MainTex, i.uv).a);
                return fixed4(i.color.rgb, i.color.a * alpha);
            }
            ENDCG
        }
    }
}
