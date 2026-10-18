// EN: Sprite shader for the ice block covering frozen dishes: frosty edge glow, a shine band sweeping across the surface, twinkling sparkles and procedural cracks that spread as _Damage rises (0 = intact, 1 = about to break).
// VI: Shader sprite cho khối băng phủ đĩa đóng băng: viền sương sáng, dải ánh sáng quét ngang bề mặt, đốm lấp lánh và vết nứt sinh theo thủ tục lan rộng khi _Damage tăng (0 = nguyên vẹn, 1 = sắp vỡ).
Shader "FoodPop/IceSurface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Màu phủ", Color) = (1,1,1,1)
        _FrostColor ("Màu sương ở viền", Color) = (0.85,0.95,1,1)
        _FrostStrength ("Độ đậm sương", Range(0,1)) = 0.35
        _ShineColor ("Màu dải sáng", Color) = (1,1,1,1)
        _ShineWidth ("Độ rộng dải sáng", Range(0.01,0.5)) = 0.12
        _ShineSpeed ("Tốc độ dải sáng", Float) = 0.35
        _ShineStrength ("Độ sáng dải sáng", Range(0,2)) = 0.6
        _SparkleScale ("Mật độ lấp lánh", Float) = 9
        _SparkleStrength ("Độ sáng lấp lánh", Range(0,2)) = 0.8
        _CrackScale ("Kích thước vết nứt", Float) = 5
        _CrackColor ("Màu vết nứt", Color) = (1,1,1,1)
        _Damage ("Mức hư hại", Range(0,1)) = 0
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
            float4 _MainTex_ST;
            fixed4 _Color, _FrostColor, _ShineColor, _CrackColor;
            float _FrostStrength, _ShineWidth, _ShineSpeed, _ShineStrength;
            float _SparkleScale, _SparkleStrength, _CrackScale, _Damage;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            // EN: Passes the sprite UV and vertex colour (SpriteRenderer tint) to the fragment stage.
            // VI: Chuyển UV sprite và màu đỉnh (màu tint của SpriteRenderer) sang bước fragment.
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // EN: Cheap 2D hash returning a pseudo-random 2D vector in [0,1).
            // VI: Hàm băm 2D rẻ trả về vector ngẫu nhiên giả trong [0,1).
            float2 hash2 (float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            // EN: Voronoi distance to the nearest cell border, plus the random id of the closest cell (used to reveal cracks gradually).
            // VI: Khoảng cách Voronoi tới biên ô gần nhất, kèm id ngẫu nhiên của ô gần nhất (dùng để lộ dần vết nứt).
            float2 voronoiEdge (float2 p)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                float best = 8.0, second = 8.0, id = 0.0;
                for (int y = -1; y <= 1; y++)
                for (int x = -1; x <= 1; x++)
                {
                    float2 g = float2(x, y);
                    float2 r = g + hash2(cell + g) - f;
                    float d = dot(r, r);
                    if (d < best) { second = best; best = d; id = hash2(cell + g + 7.3).x; }
                    else if (d < second) { second = d; }
                }
                return float2(sqrt(second) - sqrt(best), id);
            }

            // EN: Combines the sprite with frost, the moving shine band, sparkles and damage cracks, keeping the sprite's alpha shape.
            // VI: Kết hợp sprite với sương, dải sáng di chuyển, đốm lấp lánh và vết nứt theo hư hại, giữ nguyên hình dạng alpha của sprite.
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * i.color;
                float2 c = i.uv - 0.5;
                float edge = saturate(length(max(abs(c) - 0.28, 0)) * 6.0);
                tex.rgb = lerp(tex.rgb, _FrostColor.rgb, edge * _FrostStrength);

                float band = frac((i.uv.x + i.uv.y) * 0.5 - _Time.y * _ShineSpeed);
                float shine = smoothstep(_ShineWidth, 0.0, abs(band - 0.5));
                tex.rgb += _ShineColor.rgb * shine * _ShineStrength;

                float2 sp = i.uv * _SparkleScale;
                float2 sh = hash2(floor(sp));
                float sd = length(frac(sp) - sh);
                float twinkle = saturate(sin(_Time.y * 3.0 + sh.x * 40.0) * 0.5 + 0.5);
                tex.rgb += smoothstep(0.06, 0.0, sd) * twinkle * _SparkleStrength * step(0.6, sh.y);

                float2 v = voronoiEdge(i.uv * _CrackScale);
                float crack = smoothstep(0.05, 0.0, v.x) * step(v.y, _Damage);
                tex.rgb = lerp(tex.rgb, _CrackColor.rgb, crack * 0.85);
                tex.a = saturate(tex.a + crack * 0.2 * tex.a);
                return tex;
            }
            ENDCG
        }
    }
}
