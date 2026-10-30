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

    // EN: One ingredient-based order group; its first entry is the base and repeated entries retain their quantities.
    // VI: Một nhóm order theo nguyên liệu; mục đầu là đế và mục lặp giữ nguyên số lượng.
    [Serializable]
    public class IngredientOrder
    {
        [Tooltip("Danh sách nguyên liệu theo thứ tự, nguyên liệu đầu tiên được phục vụ trước.")]
        public List<FoodType> ingredients = new List<FoodType>();

        // EN: Validates a playable group against available ingredient IDs and supported card size.
        // VI: Kiểm tra nhóm chơi được theo ID nguyên liệu và số ô thẻ được hỗ trợ.
        public bool IsValid()
        {
            if (ingredients == null || ingredients.Count < 1 || ingredients.Count > FinalFoodConfig.MaxIngredients) return false;
            foreach (FoodType type in ingredients)
                if (type == FoodType.None || !Enum.IsDefined(typeof(FoodType), type)) return false;
            return true;
        }
    }

    // EN: Stores ingredient groups as authoritative orders while retaining final-food IDs only for legacy levels.
    // VI: Lưu nhóm nguyên liệu làm dữ liệu order chính, giữ ID món để đọc level cũ.
    [Serializable]
    public class CustomerOrder
    {
        public const int MaxDishes = 4;
        [Tooltip("Loại khách: thường, khách đặc biệt hoặc người giao hàng.")]
        public CustomerType customerType = CustomerType.Normal;
        [Tooltip("Thời gian phục vụ khách Boss hoặc Shipper, tính bằng giây; khách thường không sử dụng.")]
        [Min(0.1f)] public float timeServe = 30f;
        [Tooltip("Ưu tiên danh sách nguyên liệu kể cả khi danh sách đang trống.")]
        public bool useIngredientOrders;
        [Tooltip("Các nhóm nguyên liệu khách yêu cầu, tối đa bốn nhóm; không cần khớp món hoàn chỉnh.")]
        public List<IngredientOrder> ingredientOrders = new List<IngredientOrder>();
        [Tooltip("Dữ liệu món của level cũ; chỉ dùng khi chưa có order nguyên liệu.")]
        public List<FinalFoodType> finalFoods = new List<FinalFoodType> { FinalFoodType.SakeNigiri2 };

        // EN: Copies explicit ingredients or resolves legacy final-food recipes without mutating the source level.
        // VI: Sao chép nguyên liệu trực tiếp hoặc tra công thức món cũ, không sửa level nguồn.
        public List<IngredientOrder> GetIngredientOrders(FinalFoodConfig config)
        {
            var result = new List<IngredientOrder>();
            if (useIngredientOrders || (ingredientOrders != null && ingredientOrders.Count > 0))
            {
                if (ingredientOrders != null)
                    foreach (IngredientOrder order in ingredientOrders)
                        result.Add(new IngredientOrder { ingredients = order?.ingredients != null ? new List<FoodType>(order.ingredients) : new List<FoodType>() });
                return result;
            }
            if (finalFoods != null)
                foreach (FinalFoodType type in finalFoods)
                    result.Add(new IngredientOrder { ingredients = config != null ? new List<FoodType>(config.GetIngredients(type)) : new List<FoodType>() });
            return result;
        }
    }
}
