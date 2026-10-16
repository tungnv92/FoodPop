using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: One customer's order: the list of foods they want, each with a required count. Serializable so levels can list orders.
    // VI: Order của một khách: danh sách đồ ăn mong muốn, mỗi món có số lượng yêu cầu. Serializable để level liệt kê các order.
    [Serializable]
    public class CustomerOrder
    {
        [Tooltip("Mỗi loại chỉ một dòng; chỉnh số lượng cần.")]
        public List<OrderFood> foods = new List<OrderFood>();

        // EN: Returns how many of the given food the order requires (0 when it is not requested).
        // VI: Trả về số lượng order cần cho loại đồ ăn đó (0 nếu không yêu cầu).
        public int GetRequiredCount(FoodType foodType)
        {
            foreach (OrderFood food in foods)
            {
                if (food != null && food.foodType == foodType)
                    return food.requiredCount;
            }
            return 0;
        }
    }
}
