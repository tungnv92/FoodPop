using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Draws a straight braided cord and rim eyelets for each consecutive pair in an A–B–C chain.
    // VI: Vẽ dây bện thẳng và móc mép đĩa cho từng cặp liên tiếp trong chuỗi A–B–C.
    public sealed class DishLinkView : MonoBehaviour
    {
        private readonly List<LineRenderer> cords = new List<LineRenderer>();
        private readonly List<SpriteRenderer> eyelets = new List<SpriteRenderer>();
        private Material cordMaterial;
        private Texture2D cordTexture;
        private Texture2D eyeletTexture;
        private Sprite eyeletSprite;
        private static readonly Color Waiting = new Color(0.58f, 0.58f, 0.58f, 1f);

        // EN: Creates reusable renderers for each chain edge using native rope/ring geometry.
        // VI: Tạo bộ vẽ tái sử dụng cho mỗi đoạn chuỗi bằng hình học dây/vòng thuần.
        public void Initialize(int count)
        {
            BuildMaterials();
            for (int i = 0; i < count - 1; i++)
            {
                GameObject child = new GameObject($"Cord {i + 1}");
                child.transform.SetParent(transform, false);
                LineRenderer cord = child.AddComponent<LineRenderer>();
                cord.sharedMaterial = cordMaterial;
                cord.positionCount = 2;
                cord.useWorldSpace = true;
                cord.textureMode = LineTextureMode.Tile;
                cord.numCapVertices = 4;
                cord.widthMultiplier = 0.065f;
                cords.Add(cord);
                for (int j = 0; j < 2; j++)
                {
                    GameObject ring = new GameObject("Rim eyelet");
                    ring.transform.SetParent(transform, false);
                    SpriteRenderer renderer = ring.AddComponent<SpriteRenderer>();
                    renderer.sprite = eyeletSprite;
                    eyelets.Add(renderer);
                }
            }
        }

        // EN: Follows straight rim endpoints and lights every segment when any member is at the front, keeping braid proportions constant.
        // VI: Bám hai đầu mép đĩa theo đường thẳng và làm sáng mọi đoạn khi có thành viên ở đầu hàng, giữ tỷ lệ bện dây cố định.
        public void Refresh(IReadOnlyList<Food> members, bool highlighted)
        {
            Color tint = highlighted ? Color.white : Waiting;
            for (int i = 0; i < cords.Count; i++)
            {
                bool valid = i + 1 < members.Count && members[i] != null && members[i + 1] != null;
                Food a = valid ? members[i] : null;
                Food b = valid ? members[i + 1] : null;
                bool visible = valid && (a.gameObject.activeInHierarchy || b.gameObject.activeInHierarchy);
                cords[i].enabled = visible;
                eyelets[i * 2].enabled = eyelets[i * 2 + 1].enabled = visible;
                if (!valid)
                    continue;
                Bounds first = GetPlateBounds(a);
                Bounds second = GetPlateBounds(b);
                Vector3 start = GetRimPoint(a, first, second.center);
                Vector3 end = GetRimPoint(b, second, first.center);
                int sorting = GetSortingOrder(members);
                int layer = a.PlateRenderer != null ? a.PlateRenderer.sortingLayerID : 0;
                cords[i].SetPosition(0, start);
                cords[i].SetPosition(1, end);
                cords[i].sortingLayerID = layer;
                cords[i].sortingOrder = sorting;
                cords[i].startColor = cords[i].endColor = tint;
                float size = Mathf.Min(first.size.x, first.size.y, second.size.x, second.size.y);
                cords[i].widthMultiplier = size * 0.095f;
                cords[i].textureScale = new Vector2(1f / Mathf.Max(0.001f, cords[i].widthMultiplier * 2f), 1f);
                SetEyelet(eyelets[i * 2], start, size, tint, layer, sorting + 1);
                SetEyelet(eyelets[i * 2 + 1], end, size, tint, layer, sorting + 1);
            }
        }

        // EN: Gets reliable plate bounds even when offscreen pooling visibility disables a dish.
        // VI: Lấy giới hạn đĩa chính xác kể cả khi đĩa bị tắt do nằm ngoài màn hình.
        private static Bounds GetPlateBounds(Food dish)
        {
            SpriteRenderer plate = dish.PlateRenderer;
            if (plate == null || plate.sprite == null)
                return new Bounds(dish.transform.position, Vector3.one);
            Vector3 scale = plate.transform.lossyScale;
            scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            return new Bounds(plate.transform.TransformPoint(plate.sprite.bounds.center), Vector3.Scale(plate.sprite.bounds.size, scale));
        }

        // EN: Intersects the direction to the next dish with an elliptical or rectangular rim.
        // VI: Giao hướng tới đĩa kế tiếp với mép đĩa elip hoặc chữ nhật.
        private static Vector3 GetRimPoint(Food dish, Bounds bounds, Vector3 target)
        {
            Vector2 direction = (Vector2)(target - bounds.center);
            if (direction.sqrMagnitude < 0.000001f)
                return bounds.center;
            direction.Normalize();
            float x = Mathf.Max(0.001f, bounds.extents.x);
            float y = Mathf.Max(0.001f, bounds.extents.y);
            float distance = dish.IsDouble ? Mathf.Min(x / Mathf.Max(0.0001f, Mathf.Abs(direction.x)), y / Mathf.Max(0.0001f, Mathf.Abs(direction.y))) :
                1f / Mathf.Sqrt(direction.x * direction.x / (x * x) + direction.y * direction.y / (y * y));
            return bounds.center + (Vector3)(direction * distance * 0.96f);
        }

        // EN: Keeps cords above ingredients and lids, including any temporarily boosted member during PickupHand.
        // VI: Giữ dây trên nguyên liệu và nắp, kể cả thành viên tạm tăng thứ tự vẽ khi dùng PickupHand.
        private static int GetSortingOrder(IReadOnlyList<Food> members)
        {
            int order = 0;
            foreach (Food dish in members)
                if (dish != null)
                    foreach (SpriteRenderer renderer in dish.VisualRenderers)
                        if (renderer != null)
                            order = Mathf.Max(order, renderer.sortingOrder);
            return order + 12;
        }

        // EN: Positions a beveled gold rim ring at a constant world size even under scaled board parents.
        // VI: Đặt móc vàng có vát mép theo kích thước thế giới cố định kể cả khi board cha có scale.
        private static void SetEyelet(SpriteRenderer ring, Vector3 position, float size, Color tint, int layer, int order)
        {
            ring.transform.position = position;
            Vector3 parentScale = ring.transform.parent.lossyScale;
            float diameter = size * 0.19f;
            ring.transform.localScale = new Vector3(diameter / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
                diameter / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)), 1f);
            ring.color = tint;
            ring.sortingLayerID = layer;
            ring.sortingOrder = order;
        }

        // EN: Builds a rounded intertwined gold cord and beveled eyelets matching the approved demo's painted depth.
        // VI: Tạo dây vàng bện tròn và móc vát mép có chiều sâu như ảnh demo đã duyệt.
        private void BuildMaterials()
        {
            cordTexture = BuildCordTexture();
            cordMaterial = new Material(Shader.Find("Sprites/Default")) { mainTexture = cordTexture };
            eyeletTexture = BuildEyeletTexture();
            eyeletSprite = Sprite.Create(eyeletTexture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f), 128f);
        }

        // EN: Tiles two shaded curved strands with alternating overlap, dark edges and small cream highlights.
        // VI: Lặp hai sợi cong có đổ khối, đan trên dưới luân phiên, viền tối và ánh kem nhỏ.
        private static Texture2D BuildCordTexture()
        {
            const int width = 128;
            const int height = 64;
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.wrapModeU = TextureWrapMode.Repeat;
            texture.wrapModeV = TextureWrapMode.Clamp;
            Color[] pixels = new Color[width * height];
            for (int x = 0; x < width; x++)
            {
                float phase = (x + 0.5f) / width * Mathf.PI * 2f;
                float offset = Mathf.Cos(phase) * height * 0.19f;
                float slope = -Mathf.Sin(phase) * height * 0.19f * Mathf.PI * 2f / width;
                bool firstOnTop = Mathf.Sin(phase) >= 0f;
                for (int y = 0; y < height; y++)
                {
                    float localY = y + 0.5f - height * 0.5f;
                    Color first = ShadeStrand((localY - offset) / Mathf.Sqrt(1f + slope * slope), height * 0.225f);
                    Color second = ShadeStrand((localY + offset) / Mathf.Sqrt(1f + slope * slope), height * 0.225f);
                    Color back = firstOnTop ? second : first;
                    Color front = firstOnTop ? first : second;
                    float alpha = front.a + back.a * (1f - front.a);
                    Color color = alpha > 0f ? (front * front.a + back * back.a * (1f - front.a)) / alpha : Color.clear;
                    color.a = alpha;
                    pixels[y * width + x] = color;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // EN: Shades one round strand from its dark ink edge through amber to a narrow warm highlight.
        // VI: Tô một sợi tròn từ viền mực tối qua vàng hổ phách tới vệt sáng ấm hẹp.
        private static Color ShadeStrand(float distance, float radius)
        {
            float normal = distance / radius;
            float edge = Mathf.Abs(normal);
            float light = Mathf.Sqrt(Mathf.Max(0f, 1f - normal * normal));
            Color color = Color.Lerp(new Color(0.58f, 0.25f, 0.025f), new Color(1f, 0.76f, 0.08f), light);
            float highlight = Mathf.Exp(-Mathf.Pow((normal - 0.38f) / 0.19f, 2f));
            color = Color.Lerp(color, new Color(1f, 0.98f, 0.72f), highlight * 0.94f);
            color = Color.Lerp(color, new Color(0.24f, 0.095f, 0.012f), Mathf.SmoothStep(0f, 1f, (edge - 0.79f) / 0.17f));
            color.a = Mathf.Clamp01((1f - edge) * radius);
            return color;
        }

        // EN: Paints a thick gold eyelet with an ink contour, rounded bevel, inner shadow and upper-left reflection.
        // VI: Vẽ móc vàng dày có viền mực, vát tròn, bóng trong và phản sáng phía trên trái.
        private static Texture2D BuildEyeletTexture()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    Vector2 point = new Vector2(x - 63.5f, y - 63.5f) / 64f;
                    float radius = point.magnitude;
                    float band = Mathf.Clamp01((radius - 0.46f) / 0.48f);
                    float bevel = Mathf.Sin(band * Mathf.PI);
                    float reflection = Mathf.Clamp01(0.5f + (point.y - point.x) * 0.55f);
                    Color color = Color.Lerp(new Color(0.48f, 0.19f, 0.015f), new Color(1f, 0.72f, 0.045f), bevel);
                    color = Color.Lerp(color, new Color(1f, 0.98f, 0.76f), bevel * Mathf.Pow(reflection, 3f));
                    float contour = Mathf.Clamp01(Mathf.Max((radius - 0.87f) / 0.055f, (0.51f - radius) / 0.04f));
                    color = Color.Lerp(color, new Color(0.2f, 0.075f, 0.01f), contour);
                    color.a = Mathf.Clamp01((0.95f - radius) * 64f) * Mathf.Clamp01((radius - 0.45f) * 64f);
                    pixels[y * size + x] = color;
                }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // EN: Releases generated materials and textures when replay or destruction removes a link view.
        // VI: Giải phóng vật liệu và texture sinh khi replay hoặc hủy xóa hình nối.
        private void OnDestroy()
        {
            Release(cordMaterial);
            Release(eyeletSprite);
            Release(cordTexture);
            Release(eyeletTexture);
        }

        // EN: Uses the appropriate destruction mode for temporary preview scenes and live gameplay.
        // VI: Dùng cách hủy phù hợp cho scene xem trước tạm và gameplay đang chạy.
        private static void Release(Object asset)
        {
            if (asset == null)
                return;
            if (Application.isPlaying)
                Destroy(asset);
            else
                DestroyImmediate(asset);
        }
    }
}

