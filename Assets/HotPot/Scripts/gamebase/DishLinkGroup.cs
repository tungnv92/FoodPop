using System.Collections.Generic;

namespace FoodPop
{
    // EN: Tracks live dish identities independently of their changing row positions.
    // VI: Theo dõi định danh đĩa đang chơi độc lập với vị trí hàng thay đổi.
    public sealed class DishLinkGroup
    {
        private readonly List<Food> members;
        // EN: Members retain the configured A–B–C chain order.
        // VI: Thành viên giữ thứ tự chuỗi A–B–C đã cấu hình.
        public IReadOnlyList<Food> Members => members;

        // EN: Copies an ordered group so later board movement does not alter membership.
        // VI: Sao chép nhóm có thứ tự để việc dồn board không đổi thành viên.
        public DishLinkGroup(IEnumerable<Food> dishes)
        {
            members = new List<Food>(dishes);
        }

        // EN: Tests identity membership, including repeated footprint cells of double dishes.
        // VI: Kiểm tra thành viên theo định danh, kể cả các ô chiếm cùng đĩa đôi.
        public bool Contains(Food dish)
        {
            return members.Contains(dish);
        }

        // EN: Removes an exhausted dish while preserving the chain order of remaining members.
        // VI: Gỡ đĩa hết nguyên liệu, giữ thứ tự chuỗi của các thành viên còn lại.
        public void Remove(Food dish)
        {
            members.Remove(dish);
        }

        // EN: Requires every member's occupied column prefix to contain only this group or empty cells.
        // VI: Yêu cầu đoạn đầu cột phía trên mỗi thành viên chỉ có đĩa cùng nhóm hoặc ô trống.
        public bool IsExposed(IReadOnlyList<List<Food>> columns)
        {
            HashSet<Food> seen = new HashSet<Food>();
            foreach (List<Food> column in columns)
            {
                bool blocked = false;
                foreach (Food dish in column)
                {
                    if (dish == null)
                        continue;
                    if (!Contains(dish))
                        blocked = true;
                    else
                    {
                        if (blocked)
                            return false;
                        seen.Add(dish);
                    }
                }
            }
            return members.Count > 0 && seen.Count == members.Count;
        }

        // EN: Checks all members' ice, quantities and optional lids; PickupHand may reveal lids separately.
        // VI: Kiểm tra băng, số lượng và nắp tùy chọn của mọi thành viên; PickupHand có thể mở nắp riêng.
        public bool CanTake(bool allowHidden)
        {
            foreach (Food dish in members)
                if (dish == null || dish.TotalAmount <= 0 || dish.IsFrozen || (!allowHidden && dish.isHidden))
                    return false;
            return members.Count > 0;
        }

        // EN: Sums every ingredient portion, including both halves of linked double dishes.
        // VI: Cộng mọi phần nguyên liệu, gồm cả hai nửa của đĩa đôi được nối.
        public int GetTotalAmount()
        {
            int amount = 0;
            foreach (Food dish in members)
                if (dish != null)
                    amount += dish.TotalAmount;
            return amount;
        }
    }
}
