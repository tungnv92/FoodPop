using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace FoodPop
{
    // EN: One board or customer-order food entry, with optional board concealment and frozen-block data.
    // VI: Một món trên board hoặc order khách, có thể kèm trạng thái che và dữ liệu khối băng trên board.
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
        [Tooltip("Đóng băng nguyên liệu trên board: không chọn được cho tới khi giá trị khối băng về 0; không áp dụng cho order khách.")]
        public bool isFrozen;
        [Min(1)]
        [Tooltip("Giá trị khối băng: mỗi lần chọn được một đĩa bất kỳ, mọi đĩa đang đóng băng giảm 1; về 0 thì tan băng.")]
        public int blockValue = 1;

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
