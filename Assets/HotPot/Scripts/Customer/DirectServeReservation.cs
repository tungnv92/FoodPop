using UnityEngine;

namespace FoodPop
{
    public sealed class DirectServeReservation
    {
        public FoodType Ingredient { get; }
        // EN: Reject stale orders or an unavailable serving destination before dispatch.
        // VI: Loại order cũ hoặc đích phục vụ không còn nhận trước khi phóng.
        public bool IsValid => pending && lane.ordering && lane.dishes.Contains(progress) && lane.servingFood.CanServe(dish);

        private readonly Lane lane;
        private readonly DishProgress progress;
        private readonly int dish;
        private readonly bool basePreviouslyReserved;
        private bool pending = true;

        // EN: Own one already-reserved recipe portion until it is handed to serving or cancelled.
        // VI: Sở hữu một phần công thức đã giữ tới khi giao cho phục vụ hoặc bị hủy.
        internal DirectServeReservation(Lane lane, int dish, FoodType ingredient)
        {
            this.lane = lane;
            this.dish = dish;
            progress = lane.dishes[dish];
            Ingredient = ingredient;
            basePreviouslyReserved = progress.baseReserved;
            progress.Reserve(ingredient);
        }

        // EN: Send the prepared portion to its reserved order without using a conveyor slot.
        // VI: Gửi phần đã sơ chế tới order đã giữ mà không dùng slot băng chuyền.
        public bool TryDeliver(Sprite sprite, Vector3 position, Vector3 scale)
        {
            if (!IsValid || !lane.servingFood.LaunchBoosterServe(dish, Ingredient, sprite, position, scale)) return false;
            pending = false;
            return true;
        }

        // EN: Release only this pending portion, preserving an earlier base delivery; serving owns dispatched arrivals.
        // VI: Chỉ bỏ phần đang giữ, giữ nguyên đế đã giao trước đó; luồng phục vụ sở hữu lượt đã phóng.
        public void Cancel()
        {
            if (!pending) return;
            pending = false;
            progress.CancelReservation(Ingredient);
            progress.baseReserved = basePreviouslyReserved;
        }
    }
}
