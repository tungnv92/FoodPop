using UnityEngine;

namespace FoodPop
{
    // EN: Ingredient layout entry for one ingredient count: the designed layout prefab to load and the collapse options.
    // VI: Mẫu layout nguyên liệu cho một số lượng nguyên liệu: prefab layout thiết kế sẵn cần nạp và tuỳ chọn collapse.
    [System.Serializable]
    public struct IngredientGridForm
    {
        [Tooltip("Số nguyên liệu mà mẫu này áp dụng (không có mẫu đúng số thì dùng mẫu gần nhất).")]
        [Min(1)] public int ingredientCount;
        [Tooltip("Layout nguyên liệu thiết kế sẵn (prefab OrderPreview_x_y có IngredientPreviewView) nạp khi chạm món.")]
        public IngredientPreviewView template;
        [Tooltip("Bật: chạm xem nguyên liệu → ẩn các món khác, layout mở sang phải từ cột của món (món cuối mở sang trái) và chỉ ẩn các món bị che; hết collapseTime → hiện lại các món khác, nguyên liệu của món chọn hiện theo mẫu của OrderLayout1Dish, nằm giữa món; chạm lại để đóng.")]
        public bool collapseLayout;
        [Tooltip("Số giây trước khi thu (hiện lại các món khác, chuyển nguyên liệu sang mẫu của OrderLayout1Dish).")]
        [Sirenix.OdinInspector.ShowIf(nameof(collapseLayout))]
        [Min(0f)] public float collapseTime;
        [Tooltip("Chỉ khi bật collapseLayout: lệch thêm (local của thẻ) cộng vào vị trí tự động của giai đoạn đầu (mở sang phải từ cột của món; món cuối mở sang trái). Sau khi thu, layout luôn nằm giữa món.")]
        [Sirenix.OdinInspector.ShowIf(nameof(collapseLayout))]
        public Vector2 collapseLayoutPosition;

        public IngredientGridForm(int ingredientCount)
        {
            this.ingredientCount = ingredientCount;
            template = null;
            collapseLayout = false;
            collapseTime = 2f;
            collapseLayoutPosition = Vector2.zero;
        }

        // EN: Empty entries for 2-6 ingredients (layout prefabs are assigned by Tools → HotPot → Setup Ingredient Previews).
        // VI: Mẫu trống cho 2-6 nguyên liệu (prefab layout được gán bằng Tools → HotPot → Setup Ingredient Previews).
        public static IngredientGridForm[] Defaults() => new[]
        {
            new IngredientGridForm(2),
            new IngredientGridForm(3),
            new IngredientGridForm(4),
            new IngredientGridForm(5),
            new IngredientGridForm(6),
        };
    }

    // EN: Shared ingredient grid presets (one entry per ingredient count) used by an OrderLayout; one asset per layout type (1, 2, 3 dishes) shared by every order card.
    // VI: Bộ mẫu lưới nguyên liệu dùng chung (mỗi số nguyên liệu một mẫu) cho một OrderLayout; mỗi loại bố cục (1, 2, 3 món) một asset, dùng chung cho mọi thẻ order.
    [CreateAssetMenu(fileName = "IngredientGridForms", menuName = "HotPot/Ingredient Grid Forms")]
    public class IngredientGridFormConfig : ScriptableObject
    {
        [Tooltip("Mẫu theo số nguyên liệu. Số nguyên liệu không có mẫu thì dùng mẫu gần nhất.")]
        public IngredientGridForm[] forms = IngredientGridForm.Defaults();

#if UNITY_EDITOR
        // EN: Redraws open ingredient views in Play Mode when the presets change.
        // VI: Vẽ lại lưới nguyên liệu đang mở lúc Play khi bộ mẫu thay đổi.
        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;
            foreach (OrderView view in FindObjectsByType<OrderView>(FindObjectsSortMode.None))
                view.RefreshExpanded();
        }
#endif
    }
}
