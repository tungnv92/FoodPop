using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace FoodPop
{
    // EN: One food in an order: its type and how many are required.
    // VI: Một món trong order: loại đồ ăn và số lượng yêu cầu.
    [Serializable]
    public sealed class OrderFood
    {
        [FormerlySerializedAs("itemType")]
        [Tooltip("Đồ ăn được yêu cầu.")]
        public FoodType foodType;

        [Min(1)]
        [Tooltip("Số lượng yêu cầu.")]
        public int requiredCount = 1;
        [Tooltip("Che nguyên liệu trên board cho đến khi đĩa đến đầu cột; không áp dụng cho order khách.")]
        public bool isHidden;

        // EN: Creates an editable entry with default quantity and visible food.
        // VI: Tạo dòng có thể chỉnh với số lượng mặc định và nguyên liệu không bị che.
        public OrderFood() { }

        // EN: Creates an entry with quantity and optional board concealment; quantity is at least one.
        // VI: Tạo dòng có số lượng và tùy chọn che trên board; số lượng tối thiểu là một.
        public OrderFood(FoodType foodType, int requiredCount, bool isHidden = false)
        {
            this.foodType = foodType;
            this.requiredCount = Mathf.Max(1, requiredCount);
            this.isHidden = isHidden;
        }
    }
}
