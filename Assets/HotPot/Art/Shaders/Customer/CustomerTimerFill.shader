Shader "FoodPop/CustomerTimerFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _FillAmount ("Fill amount", Range(0,1)) = 1
        [PerRendererData] _FillRange ("Local bottom and height", Vector) = (-0.5,1,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" "DisableBatching"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; float height : TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _Color;
            float _FillAmount;
            float4 _FillRange;

            // EN: Keeps the sprite geometry unchanged and normalizes local height independently of atlas UVs.
            // VI: Giữ nguyên hình học sprite và chuẩn hóa chiều cao cục bộ độc lập với UV của atlas.
            Output vert(Input v)
            {
                Output o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                o.height = (v.vertex.y - _FillRange.x) / max(_FillRange.y, 0.00001);
                return o;
            }

            // EN: Clips pixels above the bottom-up fill fraction and outputs premultiplied sprite alpha.
            // VI: Cắt pixel phía trên tỉ lệ đầy từ dưới lên và xuất màu sprite nhân sẵn alpha.
            fixed4 frag(Output i) : SV_Target
            {
                clip(_FillAmount - i.height);
                fixed4 color = tex2D(_MainTex, i.uv) * i.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
