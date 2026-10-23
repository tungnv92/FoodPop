Shader "FoodPop/FoodHoldingTrayClip"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ClipTop ("World clip height", Float) = 10000
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
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
            struct Output { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; float worldY : TEXCOORD1; };
            sampler2D _MainTex;
            fixed4 _Color;
            float _ClipTop;
            // EN: Passes sprite tint, UV and world height to the clipping fragment stage.
            // VI: Truyền màu sprite, UV và độ cao world sang bước cắt pixel.
            Output vert(Input v)
            {
                Output o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                o.worldY = mul(unity_ObjectToWorld, v.vertex).y;
                return o;
            }
            // EN: Hides the part inside the wall and outputs premultiplied sprite alpha.
            // VI: Che phần nằm trong vách và xuất màu sprite nhân sẵn alpha.
            fixed4 frag(Output i) : SV_Target
            {
                clip(_ClipTop - i.worldY);
                fixed4 color = tex2D(_MainTex, i.uv) * i.color;
                color.rgb *= color.a;
                return color;
            }
            ENDCG
        }
    }
}
