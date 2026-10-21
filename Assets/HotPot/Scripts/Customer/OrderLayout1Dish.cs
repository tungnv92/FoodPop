using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Card layout for a one-dish order: one column centred on the card's vertical axis; the dish fits the column and the card is as wide as the column.
    // VI: Bố cục thẻ cho order 1 món: một cột nằm giữa trục dọc của thẻ; món co vừa cột và thẻ rộng bằng cột.
    [DisallowMultipleComponent]
    public class OrderLayout1Dish : OrderLayout
    {
        public override int DishCount => 1;

        // EN: Places the single dish in its column and fits the background to it.
        // VI: Đặt món duy nhất vào cột của nó và co nền theo cột.
        public override void Arrange(OrderView view, IReadOnlyList<OrderItemView> items)
        {
            ArrangeColumns(view, items, 1);
        }
    }
}
