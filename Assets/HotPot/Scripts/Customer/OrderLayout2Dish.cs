using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Card layout for a two-dish order: one column per dish on a single row, centred with columnSpacing between them; each dish fits its column and the card is as wide as both columns.
    // VI: Bố cục thẻ cho order 2 món: mỗi món một cột trên cùng một hàng, căn giữa, cách nhau columnSpacing; mỗi món co vừa cột và thẻ rộng bằng tổng hai cột.
    [DisallowMultipleComponent]
    public class OrderLayout2Dish : OrderLayout
    {
        public override int DishCount => 2;

        // EN: Places the two dishes in their columns and fits the background to both.
        // VI: Đặt hai món vào hai cột và co nền theo tổng hai cột.
        public override void Arrange(OrderView view, IReadOnlyList<OrderItemView> items)
        {
            ArrangeColumns(view, items, 2);
        }
    }
}
