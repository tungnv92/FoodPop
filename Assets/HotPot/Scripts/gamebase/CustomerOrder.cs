using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Stable customer classifications; all currently share the same serving rules.
    // VI: Phân loại khách cố định; hiện mọi loại dùng chung quy tắc phục vụ.
    public enum CustomerType
    {
        Normal = 0,
        Boss = 1,
        Shipper = 2
    }

    // EN: One ingredient-based order group; its first entry is the base and repeated entries retain their quantities. The optional dish prefab is the picture whose ingredient list matches this group (assigned by the Level Editor).
    // VI: Một nhóm order theo nguyên liệu; mục đầu là đế và mục lặp giữ nguyên số lượng. Prefab món tùy chọn là hình có danh sách nguyên liệu khớp nhóm này (Level Editor tự gán).
    [Serializable]
    public class IngredientOrder
    {
        public const int MaxIngredients = 6;
        [Tooltip("Danh sách nguyên liệu theo thứ tự, nguyên liệu đầu tiên được phục vụ trước.")]
        public List<FoodType> ingredients = new List<FoodType>();
        [Tooltip("Prefab hình món có cùng danh sách nguyên liệu; để trống thì thẻ order và khay không hiện hình món.")]
        public FinalFoodView dish;

        // EN: Validates a playable group against available ingredient IDs and supported card size.
        // VI: Kiểm tra nhóm chơi được theo ID nguyên liệu và số ô thẻ được hỗ trợ.
        public bool IsValid()
        {
            if (ingredients == null || ingredients.Count < 1 || ingredients.Count > MaxIngredients) return false;
            foreach (FoodType type in ingredients)
                if (type == FoodType.None || !Enum.IsDefined(typeof(FoodType), type)) return false;
            return true;
        }
    }

    // EN: One customer's order: customer type, special serving time and up to MaxDishes ingredient groups.
    // VI: Order của một khách: loại khách, thời gian phục vụ đặc biệt và tối đa MaxDishes nhóm nguyên liệu.
    [Serializable]
    public class CustomerOrder
    {
        public const int MaxDishes = 4;
        [Tooltip("Loại khách: thường, khách đặc biệt hoặc người giao hàng.")]
        public CustomerType customerType = CustomerType.Normal;
        [Tooltip("Thời gian phục vụ khách Boss hoặc Shipper, tính bằng giây; khách thường không sử dụng.")]
        [Min(0.1f)] public float timeServe = 30f;
        [Tooltip("Các nhóm nguyên liệu khách yêu cầu, tối đa bốn nhóm.")]
        public List<IngredientOrder> ingredientOrders = new List<IngredientOrder>();
    }
}
