using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Card layout for a three-dish order: one column per dish on a single row, centred with columnSpacing between them; each dish fits its column and the card is as wide as the three columns.
    // VI: Bố cục thẻ cho order 3 món: mỗi món một cột trên cùng một hàng, căn giữa, cách nhau columnSpacing; mỗi món co vừa cột và thẻ rộng bằng tổng ba cột.
    [DisallowMultipleComponent]
    public class OrderLayout3Dish : OrderLayout
    {
        public override int DishCount => 3;

        // EN: Places the three dishes in their columns and fits the background to them.
        // VI: Đặt ba món vào ba cột và co nền theo tổng ba cột.
        public override void Arrange(OrderView view, IReadOnlyList<OrderItemView> items)
        {
            ArrangeColumns(view, items, 3);
        }
    }
}
