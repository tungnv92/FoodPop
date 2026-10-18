using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Debug bot that plays the current level by itself: it picks board dishes whose food a customer still needs, and digs into other columns when nothing useful is on top.
    // VI: Bot debug tự chơi level hiện tại: chọn các đĩa trên board có món mà khách còn cần, và mở dần các cột khác khi trên cùng không có món hữu ích.
    public class AutoPlayer : MonoBehaviour
    {
        [Tooltip("Thời gian (giây) giữa hai lần bot chọn đĩa.")]
        [Min(0.1f)] [SerializeField] private float pickInterval = 1.2f;
        [Tooltip("Số slot trống tối thiểu trên băng để bot được chọn đĩa không cần thiết (để mở cột).")]
        [Min(0)] [SerializeField] private int minFreeSlotsToDig = 6;

        private readonly HashSet<FoodType> beltTypes = new HashSet<FoodType>();
        private float nextPickTime;

        // EN: Picks one dish every pickInterval until every customer has been served.
        // VI: Chọn một đĩa sau mỗi pickInterval cho tới khi phục vụ xong mọi khách.
        private void Update()
        {
            CustomerManager customers = CustomerManager.Instance;
            FoodBoardManager board = FoodBoardManager.Instance;
            if (customers == null || board == null || customers.AllServed || Time.time < nextPickTime)
                return;
            nextPickTime = Time.time + pickInterval;
            int freeSlots = CollectBeltTypes();
            if (!TryPickNeeded(board, customers) && freeSlots >= minFreeSlotsToDig)
                TryPickAny(board);
        }

        // EN: Records which food types are already riding the belt and returns how many slots are empty.
        // VI: Ghi lại các loại món đang nằm trên băng và trả về số slot trống.
        private int CollectBeltTypes()
        {
            beltTypes.Clear();
            int free = 0;
            ChefManager chef = ChefManager.Instance;
            if (chef == null)
                return 0;
            foreach (ConveyorSlot slot in chef.Slots)
            {
                if (slot == null)
                    continue;
                if (slot.isEmpty || slot.food == null)
                    free++;
                else
                    beltTypes.Add(slot.food.foodType);
            }
            return free;
        }

        // EN: Picks the first column head that a customer needs and that is not already on the belt.
        // VI: Chọn đĩa đầu cột đầu tiên mà khách đang cần và chưa có trên băng.
        private bool TryPickNeeded(FoodBoardManager board, CustomerManager customers)
        {
            for (int c = 0; c < board.ColumnCount; c++)
            {
                FoodType type = board.GetColumnHeadType(c);
                if (type != FoodType.None && !beltTypes.Contains(type) && customers.TryFindCustomer(type) && board.TryPickColumn(c))
                    return true;
            }
            return false;
        }

        // EN: Picks any non-empty column head to reveal the dishes underneath.
        // VI: Chọn bất kỳ đĩa đầu cột nào để mở các đĩa bên dưới.
        private void TryPickAny(FoodBoardManager board)
        {
            for (int c = 0; c < board.ColumnCount; c++)
            {
                if (board.GetColumnHeadType(c) != FoodType.None && board.TryPickColumn(c))
                    return;
            }
        }
    }
}
