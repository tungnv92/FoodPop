using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Commit all members together: conveyor destinations plus at most one reserved chef-to-order portion.
    // VI: Chốt mọi thành viên cùng lúc: các đích băng chuyền và tối đa một phần đã giữ qua đầu bếp tới order.
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

        public IReadOnlyList<Entry> Entries => entries;
        public string LastFailure { get; private set; }

        private readonly List<ConveyorSlot> reserved = new List<ConveyorSlot>();
        private readonly List<Entry> entries = new List<Entry>();
        private DirectServeReservation directReservation;

        // EN: Evaluate the shared rule, reserve destinations, pay optional cost and start exactly one chef path.
        // VI: Đánh giá luật chung, giữ đích, trả chi phí tùy chọn và bắt đầu đúng một nhánh đầu bếp.
        public bool TryFill(IReadOnlyList<Food> dishes, ChefManager chef, System.Func<bool> commitCost = null)
        {
            entries.Clear();
            reserved.Clear();
            LastFailure = null;
            CustomerManager customers = CustomerManager.Instance;
            DishPickPlan plan = DishPickPlan.Evaluate(dishes, chef != null ? chef.CountEmptySlots() : 0, chef, customers);
            if (!plan.CanPick)
            {
                LastFailure = plan.FailureReason;
                return false;
            }
            if (!ReserveDestinations(plan, chef, customers)) return false;
            if (!FillConveyor(plan)) return false;
            try
            {
                if (commitCost != null && !commitCost())
                {
                    LastFailure = "không thể thanh toán lượt booster";
                    Rollback(reserved.Count);
                    return false;
                }
            }
            catch
            {
                Rollback(reserved.Count);
                throw;
            }
            if (directReservation != null)
            {
                chef.BeginDirectPreparation(plan.DirectDish, plan.DirectSecond, directReservation);
                directReservation = null;
            }
            else chef.TryPreparePicked(entries);
            return true;
        }

        // EN: Reserve the live recipe before claiming the remaining belt slots; neither route launches before commit.
        // VI: Giữ công thức còn sống trước khi lấy các slot băng còn lại; chưa phóng nhánh nào trước khi chốt.
        private bool ReserveDestinations(DishPickPlan plan, ChefManager chef, CustomerManager customers)
        {
            if (plan.Outcome == DishPickPlan.Result.DirectToOrder &&
                !customers.TryReserveDirectServe(plan.DirectType, out directReservation))
            {
                LastFailure = "order không còn nhận phần đi thẳng";
                return false;
            }
            int count = plan.TotalPortions - (directReservation != null ? 1 : 0);
            if (count > 0 && !chef.TryGetNearestEmptySlots(plan.Members[0].transform.position, count, reserved))
            {
                LastFailure = "không giữ đủ slot cho toàn bộ phần còn lại";
                Rollback(0);
                return false;
            }
            return true;
        }

        // EN: Fill both halves of every unique dish, skipping only the single explicitly selected direct portion.
        // VI: Điền cả hai nửa của từng đĩa riêng biệt, chỉ bỏ đúng một phần đã chọn đi thẳng.
        private bool FillConveyor(DishPickPlan plan)
        {
            int next = 0;
            bool directAssigned = false;
            foreach (Food dish in plan.Members)
            {
                var entry = new Entry { dish = dish, type = dish.foodType };
                entries.Add(entry);
                for (int i = 0; i < dish.TotalAmount; i++)
                {
                    bool first = i < dish.amount;
                    if (!directAssigned && directReservation != null && dish == plan.DirectDish && first != plan.DirectSecond)
                    {
                        directAssigned = true;
                        continue;
                    }
                    ConveyorSlot slot = reserved[next];
                    if (!slot.TryFillFood(first ? dish.foodType : dish.SecondFoodType))
                    {
                        LastFailure = "slot không nhận được nguyên liệu: " + slot.name;
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

        // EN: Undo only this uncommitted batch's slot fills and recipe reservation on fill/payment failure.
        // VI: Chỉ hoàn tác slot và giữ công thức của lượt chưa chốt khi điền hoặc thanh toán thất bại.
        private void Rollback(int filled)
        {
            for (int i = 0; i < filled; i++) reserved[i].ClearFood();
            directReservation?.Cancel();
            directReservation = null;
            entries.Clear();
            reserved.Clear();
        }
    }
}
