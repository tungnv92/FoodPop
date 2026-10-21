using UnityEngine;

namespace FoodPop
{
    // EN: Ingredient grid preset for one ingredient count: columns (GridLayout, filled row by row), the icon localScale and centre-to-centre spacing. The values are written as-is into the icons' transforms (they live under an unscaled "Ingredient Grid" object on the card).
    // VI: Mẫu lưới nguyên liệu cho một số lượng nguyên liệu: số cột (kiểu GridLayout, điền theo hàng), localScale của ảnh và khoảng cách giữa tâm các ô. Giá trị được gán nguyên vào transform của ảnh (nằm dưới object "Ingredient Grid" không scale trên thẻ).
    [System.Serializable]
    public struct IngredientGridForm
    {
        [Tooltip("Số nguyên liệu mà mẫu này áp dụng.")]
        [Min(1)] public int ingredientCount;
        [Tooltip("Số cột của lưới (điền theo hàng, trái sang phải rồi xuống dưới).")]
        [Min(1)] public int columns;
        [Tooltip("Scale gán thẳng vào localScale của từng ảnh nguyên liệu.")]
        [UnityEngine.Serialization.FormerlySerializedAs("iconSize")]
        [Min(0.001f)] public float iconScale;
        [Tooltip("Khoảng cách giữa tâm hai ô kề nhau (x: theo cột, y: theo hàng); localPosition của ô = đúng các bội số của khoảng cách này.")]
        public Vector2 spacing;
        [Tooltip("Kích thước nền tối của mẫu này (local của thẻ, gán thẳng vào size của nền). Để 0 = phủ cả cột trừ ingredientBackgroundInset.")]
        public Vector2 backgroundSize;
        [Tooltip("Bật: chạm xem nguyên liệu → ẩn các món khác, lưới đặt tại collapseLayoutPosition; hết collapseTime → hiện lại các món khác, nguyên liệu của món chọn vẫn hiện trong cột của nó theo mẫu nguyên liệu của OrderLayout1Dish; chạm lại để đóng.")]
        public bool collapseLayout;
        [Tooltip("Số giây trước khi thu (hiện lại các món khác, chuyển nguyên liệu sang mẫu của OrderLayout1Dish).")]
        [Sirenix.OdinInspector.ShowIf(nameof(collapseLayout))]
        [Min(0f)] public float collapseTime;
        [Tooltip("Vị trí tuyệt đối của layout nguyên liệu (object Ingredient Grid) trên thẻ, local của thẻ: (0,0) = tâm thẻ; cộng thêm ingredientGridOffset chung. Chỉ dùng khi bật collapseLayout (khi tắt, lưới nằm tại tâm ô món).")]
        [Sirenix.OdinInspector.ShowIf(nameof(collapseLayout))]
        public Vector2 collapseLayoutPosition;

        public IngredientGridForm(int ingredientCount, int columns, float iconScale, Vector2 spacing)
        {
            backgroundSize = Vector2.zero;
            collapseLayout = false;
            collapseTime = 2f;
            collapseLayoutPosition = Vector2.zero;
            this.ingredientCount = ingredientCount;
            this.columns = columns;
            this.iconScale = iconScale;
            this.spacing = spacing;
        }

        // EN: Default presets for 2-6 ingredients (1-3: one column, 4+: two columns).
        // VI: Mẫu mặc định cho 2-6 nguyên liệu (1-3: một cột, từ 4: hai cột).
        public static IngredientGridForm[] Defaults() => new[]
        {
            new IngredientGridForm(2, 1, 0.12f, new Vector2(0.36f, 0.36f)),
            new IngredientGridForm(3, 1, 0.11f, new Vector2(0.34f, 0.34f)),
            new IngredientGridForm(4, 2, 0.11f, new Vector2(0.34f, 0.34f)),
            new IngredientGridForm(5, 2, 0.1f, new Vector2(0.32f, 0.32f)),
            new IngredientGridForm(6, 2, 0.1f, new Vector2(0.32f, 0.32f)),
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
