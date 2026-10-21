using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: One customer's order: the final dishes they want (up to MaxDishes, all shown on the card at once); each dish's ingredients come from FinalFoodConfig. Serializable so levels can list orders.
    // VI: Order của một khách: các món hoàn chỉnh (final food) khách muốn (tối đa MaxDishes, hiện cùng lúc trên thẻ); nguyên liệu từng món lấy từ FinalFoodConfig. Serializable để level liệt kê các order.
    [Serializable]
    public class CustomerOrder
    {
        public const int MaxDishes = 4;

        [Tooltip("Các món hoàn chỉnh khách order (tối đa 4), hiện cùng lúc trên thẻ order; nguyên liệu mỗi món lấy theo FinalFoodConfig.")]
        public List<FinalFoodType> finalFoods = new List<FinalFoodType> { FinalFoodType.SakeNigiri2 };
    }
}
