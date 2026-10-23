using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Reserves and fills all portions of a linked pick atomically before any dish leaves the board.
    // VI: Giữ và điền mọi phần của lượt lấy nhóm cùng lúc trước khi đĩa nào rời board.
    public sealed class DishPickBatch
    {
        public sealed class Entry
        {
            public Food dish;
            public FoodType type;
            public readonly List<ConveyorSlot> first = new List<ConveyorSlot>();
            public readonly List<ConveyorSlot> second = new List<ConveyorSlot>();
            public readonly List<ConveyorSlot> slots = new List<ConveyorSlot>();
        }

        private readonly List<ConveyorSlot> reserved = new List<ConveyorSlot>();
        private readonly List<Entry> entries = new List<Entry>();
        // EN: Stable per-dish flight destinations for the current batch.
        // VI: Đích bay riêng từng đĩa của lượt lấy hiện tại.
        public IReadOnlyList<Entry> Entries => entries;

        // EN: Checks all members and capacity first, rolling back already-filled slots on any fill failure.
        // VI: Kiểm tra mọi thành viên và sức chứa trước, hoàn tác slot đã điền nếu có lỗi.
        public bool TryFill(IReadOnlyList<Food> dishes, ChefManager chef, FoodIconConfig icons)
        {
            entries.Clear();
            reserved.Clear();
            int total = 0;
            HashSet<Food> unique = new HashSet<Food>();
            foreach (Food dish in dishes)
            {
                if (dish == null || !unique.Add(dish) || dish.TotalAmount <= 0 ||
                    (dish.amount > 0 && dish.foodType == FoodType.None) ||
                    (dish.SecondAmount > 0 && dish.SecondFoodType == FoodType.None))
                    return false;
                total += dish.TotalAmount;
            }
            if (dishes.Count == 0 || chef == null || !chef.TryGetNearestEmptySlots(dishes[0].transform.position, total, reserved))
                return false;
            int next = 0;
            foreach (Food dish in dishes)
            {
                Entry entry = new Entry { dish = dish, type = dish.foodType };
                entries.Add(entry);
                for (int i = 0; i < dish.TotalAmount; i++)
                {
                    ConveyorSlot slot = reserved[next];
                    bool first = i < dish.amount;
                    if (!slot.TryFillFood(first ? dish.foodType : dish.SecondFoodType, icons))
                    {
                        Rollback(next);
                        return false;
                    }
                    entry.slots.Add(slot);
                    (first ? entry.first : entry.second).Add(slot);
                    next++;
                }
            }
            return true;
        }

        // EN: Empties only slots filled by this failed batch, leaving every dish and unrelated slot untouched.
        // VI: Chỉ xóa slot đã điền bởi lượt lỗi này, giữ nguyên mọi đĩa và slot không liên quan.
        private void Rollback(int filled)
        {
            for (int i = 0; i < filled; i++)
                reserved[i].ClearFood();
            entries.Clear();
            reserved.Clear();
        }
    }
}
