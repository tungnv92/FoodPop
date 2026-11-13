using System.Collections.Generic;

namespace FoodPop
{
    // EN: Read-only decision shared by picking and deadlock detection; evaluating never reserves resources.
    // VI: Quyết định dùng chung cho lấy đĩa và phát hiện bế tắc; đánh giá không giữ tài nguyên.
    public sealed class DishPickPlan
    {
        public enum Result { FitsConveyor, DirectToOrder, InsufficientCapacity, ChefUnavailable, NoCustomerNeed, InvalidGroup }

        public Result Outcome { get; private set; }
        public bool CanPick => Outcome == Result.FitsConveyor || Outcome == Result.DirectToOrder;
        public int TotalPortions { get; private set; }
        public Food DirectDish { get; private set; }
        public FoodType DirectType { get; private set; }
        public bool DirectSecond { get; private set; }
        public IReadOnlyList<Food> Members => members;
        public string FailureReason => Outcome switch
        {
            Result.InsufficientCapacity => "tổng phần của nhóm vượt số slot trống cộng một phần đi thẳng",
            Result.ChefUnavailable => "thiếu một slot nhưng đầu bếp chưa thể nhận phần đi thẳng",
            Result.NoCustomerNeed => "thiếu một slot và không order nào nhận được phần dư",
            Result.InvalidGroup => "nhóm đĩa hoặc dữ liệu nguyên liệu không hợp lệ",
            _ => null
        };

        private readonly List<Food> members = new List<Food>();

        // EN: Distinguish full belt allocation, one direct portion and every rejection without changing gameplay.
        // VI: Phân biệt đủ chỗ trên băng, một phần đi thẳng và từng lý do từ chối mà không đổi gameplay.
        public static DishPickPlan Evaluate(IReadOnlyList<Food> dishes, int emptySlots, ChefManager chef, CustomerManager customers)
        {
            var plan = new DishPickPlan { Outcome = Result.InvalidGroup };
            var unique = new HashSet<Food>();
            foreach (Food dish in dishes)
            {
                if (dish == null || dish.TotalAmount <= 0 ||
                    (dish.amount > 0 && dish.foodType == FoodType.None) ||
                    (dish.SecondAmount > 0 && dish.SecondFoodType == FoodType.None)) return plan;
                if (!unique.Add(dish)) continue;
                plan.members.Add(dish);
                plan.TotalPortions += dish.TotalAmount;
            }
            if (plan.members.Count == 0 || chef == null) return plan;
            plan.Outcome = Classify(plan.TotalPortions, emptySlots, chef.CanPrepareDirect, false);
            if (plan.Outcome != Result.NoCustomerNeed) return plan;
            foreach (Food dish in plan.members)
            {
                if (dish.amount > 0 && customers.TryFindCustomer(dish.foodType))
                {
                    plan.DirectDish = dish;
                    plan.DirectType = dish.foodType;
                }
                else if (dish.SecondAmount > 0 && customers.TryFindCustomer(dish.SecondFoodType))
                {
                    plan.DirectDish = dish;
                    plan.DirectType = dish.SecondFoodType;
                    plan.DirectSecond = true;
                }
                else continue;
                plan.Outcome = Result.DirectToOrder;
                return plan;
            }
            return plan;
        }

        // EN: Apply the capacity rule: N<=S fits, N=S+1 needs one idle-chef/order route, N>S+1 fails.
        // VI: Áp dụng sức chứa: N<=S đủ chỗ, N=S+1 cần một lượt đầu bếp/order, N>S+1 từ chối.
        public static Result Classify(int portions, int emptySlots, bool chefAvailable, bool orderAccepts)
        {
            if (portions <= 0 || emptySlots < 0) return Result.InvalidGroup;
            if (portions <= emptySlots) return Result.FitsConveyor;
            if (portions - emptySlots > 1) return Result.InsufficientCapacity;
            if (!chefAvailable) return Result.ChefUnavailable;
            return orderAccepts ? Result.DirectToOrder : Result.NoCustomerNeed;
        }
    }
}
