using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Root of a layered final-dish picture: declares which child SpriteRenderer draws each ingredient; the ingredient list alone identifies the dish when compared with a customer's ingredient group.
    // VI: Gốc của hình món hoàn chỉnh ghép lớp: khai báo SpriteRenderer con nào vẽ từng nguyên liệu; chỉ cần danh sách nguyên liệu là nhận ra món khi so với nhóm nguyên liệu khách order.
    [DisallowMultipleComponent]
    public class FinalFoodView : MonoBehaviour
    {
        // EN: One picture layer and the ingredient it represents.
        // VI: Một lớp hình và nguyên liệu mà lớp đó thể hiện.
        [Serializable]
        public class IngredientLayer
        {
            [Tooltip("Nguyên liệu mà lớp hình này thể hiện.")]
            public FoodType ingredient;
            [Tooltip("SpriteRenderer vẽ lớp nguyên liệu này.")]
            public SpriteRenderer renderer;
        }

        // EN: Returns the ingredient layers in recipe order.
        // VI: Trả về các lớp nguyên liệu theo thứ tự công thức.
        public IReadOnlyList<IngredientLayer> Layers => layers;

        // EN: The dish's own container (e.g. the double dish for wasabi and soy sauce), drawn under every layer; null when the dish uses the tray slot's plate.
        // VI: Khay/đĩa riêng của món (ví dụ khay đôi đựng wasabi và nước tương), vẽ dưới mọi lớp; null khi món dùng đĩa của ô khay.
        public SpriteRenderer Plate => plate;

        // EN: True when the ingredients may arrive in any order (the dish has no base that must come first).
        // VI: True khi nguyên liệu được tới theo thứ tự bất kỳ (món không có đế phải tới trước).
        public bool AnyIngredientOrder => anyIngredientOrder;

        [Tooltip("Các lớp hình theo thứ tự công thức: lớp đầu là đế (cơm), các lớp sau xếp từ dưới lên.")]
        [SerializeField] private List<IngredientLayer> layers = new List<IngredientLayer>();
        [Tooltip("Khay/đĩa riêng của món (không phải nguyên liệu), vẽ dưới mọi lớp; để trống thì món dùng đĩa của ô khay.")]
        [SerializeField] private SpriteRenderer plate;
        [Tooltip("Bật để mọi nguyên liệu của món được nhận theo thứ tự bất kỳ (món không có đế phải tới trước).")]
        [SerializeField] private bool anyIngredientOrder;

        [Tooltip("Bật để chạy hoạt cảnh chế biến riêng trước khi món bay lên order.")]
        public bool isAnimate;
        [Tooltip("Component hoạt cảnh riêng của món, kế thừa FinalFoodAnimation.")]
        [SerializeField] private FinalFoodAnimation foodAnimation;

        private int[] orderOffsets;
        private int plateOffset;

        // EN: Run the optional preparation animation only when explicitly enabled on this prefab.
        // VI: Chỉ chạy hoạt cảnh chế biến tùy chọn khi được bật rõ trên prefab này.
        public IEnumerator PlayAnimation()
        {
            if (isAnimate && foodAnimation != null) yield return foodAnimation.Play();
        }

        // EN: Returns the ingredients of the picture in layer order.
        // VI: Trả về danh sách nguyên liệu của hình theo thứ tự lớp.
        public List<FoodType> GetIngredients()
        {
            List<FoodType> result = new List<FoodType>(layers.Count);
            foreach (IngredientLayer layer in layers)
                result.Add(layer.ingredient);
            return result;
        }

        // EN: True when the given ingredient list holds exactly this dish's ingredients (same quantities, any order).
        // VI: True khi danh sách nguyên liệu cho trước có đúng các nguyên liệu của món này (cùng số lượng, thứ tự bất kỳ).
        public bool Matches(IReadOnlyList<FoodType> ingredients)
        {
            if (ingredients == null || ingredients.Count != layers.Count)
                return false;
            List<FoodType> remaining = GetIngredients();
            foreach (FoodType type in ingredients)
            {
                if (!remaining.Remove(type))
                    return false;
            }
            return true;
        }

        // EN: Largest side of the picture in root-local units (union of the layer sprites and the dish's own plate), used to fit the dish into a slot.
        // VI: Cạnh lớn nhất của hình theo đơn vị local của gốc (hợp các sprite lớp và khay riêng của món), dùng để co món vừa một ô.
        public float GetLocalSize()
        {
            bool any = false;
            Bounds total = default;
            foreach (IngredientLayer layer in layers)
                EncapsulateRenderer(layer.renderer, ref total, ref any);
            EncapsulateRenderer(plate, ref total, ref any);
            return any ? Mathf.Max(total.size.x, total.size.y) : 0f;
        }

        // EN: Sort ingredient layers, the dish's own plate and preparation props together while preserving authored layer offsets.
        // VI: Sắp thứ tự lớp nguyên liệu, khay riêng của món và đạo cụ chế biến cùng nhau, giữ độ lệch lớp đã thiết kế.
        public void SetSorting(int sortingLayer, int baseOrder)
        {
            CaptureOrderOffsets();
            for (int i = 0; i < layers.Count; i++)
            {
                SpriteRenderer renderer = layers[i].renderer;
                if (renderer == null)
                    continue;
                renderer.sortingLayerID = sortingLayer;
                renderer.sortingOrder = baseOrder + orderOffsets[i];
            }
            if (plate != null)
            {
                plate.sortingLayerID = sortingLayer;
                plate.sortingOrder = baseOrder + plateOffset;
            }
            if (foodAnimation != null) foodAnimation.SetSorting(sortingLayer, baseOrder);
        }

        // EN: Adds one sprite's bounds, in root-local units, to the running union used by GetLocalSize.
        // VI: Cộng vùng bao của một sprite, theo đơn vị local của gốc, vào vùng hợp mà GetLocalSize đang tính.
        private static void EncapsulateRenderer(SpriteRenderer renderer, ref Bounds total, ref bool any)
        {
            if (renderer == null || renderer.sprite == null)
                return;
            Transform part = renderer.transform;
            Bounds bounds = renderer.sprite.bounds;
            Vector3 scale = part.localScale;
            Bounds local = new Bounds(part.localPosition + Vector3.Scale(bounds.center, scale), Vector3.Scale(bounds.size, new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), 1f)));
            if (any)
                total.Encapsulate(local);
            else
                total = local;
            any = true;
        }

        // EN: Remembers (once) the authored sorting order of each layer and of the dish's own plate relative to the lowest of them, before SetSorting overwrites them.
        // VI: Ghi nhớ (một lần) thứ tự vẽ gốc của từng lớp và của khay riêng so với thứ tự thấp nhất trong số đó, trước khi SetSorting ghi đè.
        private void CaptureOrderOffsets()
        {
            if (orderOffsets != null)
                return;
            orderOffsets = new int[layers.Count];
            int lowest = plate != null ? plate.sortingOrder : int.MaxValue;
            foreach (IngredientLayer layer in layers)
            {
                if (layer.renderer != null)
                    lowest = Mathf.Min(lowest, layer.renderer.sortingOrder);
            }
            for (int i = 0; i < layers.Count; i++)
                orderOffsets[i] = layers[i].renderer != null ? layers[i].renderer.sortingOrder - lowest : 0;
            plateOffset = plate != null ? plate.sortingOrder - lowest : 0;
        }
    }
}
