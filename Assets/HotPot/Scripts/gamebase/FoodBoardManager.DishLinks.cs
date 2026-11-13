using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    public partial class FoodBoardManager
    {
        private readonly Dictionary<Food, DishLinkGroup> dishLinks = new Dictionary<Food, DishLinkGroup>();
        private readonly Dictionary<DishLinkGroup, DishLinkView> linkViews = new Dictionary<DishLinkGroup, DishLinkView>();
        private readonly DishPickBatch pickBatch = new DishPickBatch();

        // EN: Reports live chain membership, independent of the dish's current row or column.
        // VI: Cho biết thành viên chuỗi hiện tại, độc lập với hàng hoặc cột của đĩa.
        public bool IsLinked(Food dish)
        {
            return dish != null && dishLinks.ContainsKey(dish);
        }

        // EN: Identifies dishes that must be highlighted or picked together, including an unlinked dish itself.
        // VI: Xác định các đĩa cần làm sáng hoặc lấy cùng nhau, gồm chính đĩa chưa nối.
        public bool SharesPickGroup(Food dish, Food other)
        {
            return dish != null && (dish == other || (dishLinks.TryGetValue(dish, out DishLinkGroup group) && group.Contains(other)));
        }

        // EN: Uses the same whole-group rejection rules as PickupHand feedback and selection.
        // VI: Dùng chung quy tắc từ chối cả nhóm với phản hồi và lựa chọn PickupHand.
        public bool CanPickupGroup(Food dish, int freeSlots)
        {
            return GetPickupGroupRejection(dish, freeSlots) == null;
        }

        // EN: Explain whole-group rejection, checking ice before the shared belt/direct-order capacity rule.
        // VI: Giải thích từ chối cả nhóm, kiểm tra băng trước luật sức chứa chung trên băng/đi thẳng order.
        public string GetPickupGroupRejection(Food dish, int freeSlots)
        {
            if (dish == null)
                return IngameConfig.Instance.Boosters.GetShortDes(BoosterConfig.ShortDesType.PlateEmpty);
            List<Food> members = GetPickMembers(dish);
            bool linked = members.Count > 1;
            foreach (Food member in members)
                if (member != null && member.IsFrozen)
                    return IngameConfig.Instance.Boosters.GetShortDes(linked ? BoosterConfig.ShortDesType.LinkedPlateFrozen : BoosterConfig.ShortDesType.PlateFrozen);
            int total = 0;
            foreach (Food member in members)
            {
                if (member == null || member.TotalAmount <= 0)
                    return IngameConfig.Instance.Boosters.GetShortDes(BoosterConfig.ShortDesType.NoPlateToPick);
                total += member.TotalAmount;
            }
            if (total > freeSlots && !DishPickPlan.Evaluate(members, freeSlots, ChefManager.Instance, CustomerManager.Instance).CanPick)
                return IngameConfig.Instance.Boosters.GetShortDes(linked ? BoosterConfig.ShortDesType.LinkedPlatesNoSpace : BoosterConfig.ShortDesType.BeltFull);
            return null;
        }

        // EN: Check each exposed pick group once using exactly the same capacity and direct-order rules as a real tap.
        // VI: Kiểm tra mỗi nhóm lấy đã lộ một lần bằng đúng luật sức chứa/order đi thẳng của thao tác thật.
        public bool HasAvailablePick(ChefManager chef, CustomerManager customers)
        {
            var visited = new HashSet<Food>();
            int empty = chef.CountEmptySlots();
            for (int column = 0; column < columnDishes.Count; column++)
            {
                Food dish = GetColumnHead(column);
                if (dish == null || !visited.Add(dish)) continue;
                List<Food> members = GetPickMembers(dish);
                foreach (Food member in members) visited.Add(member);
                if (CanPickGroup(dish, false) && DishPickPlan.Evaluate(members, empty, chef, customers).CanPick) return true;
            }
            return false;
        }

        // EN: Reveals every hidden member before a PickupHand group pick, preserving a shared cancellation owner in the booster.
        // VI: Mở mọi thành viên đang ẩn trước khi PickupHand lấy nhóm, giữ quyền hủy coroutine ở booster.
        public IEnumerator RevealPickGroup(Food dish, float duration)
        {
            List<Food> members = GetPickMembers(dish);
            foreach (Food member in members)
                if (member != null && member.isHidden)
                    yield return member.FadeOutLid(duration);
        }

        // EN: Copies the current members so removal or callbacks cannot invalidate an in-progress pick.
        // VI: Sao chép thành viên hiện tại để việc gỡ hoặc callback không làm hỏng lượt lấy đang chạy.
        public List<Food> GetPickMembers(Food dish)
        {
            return dish != null && dishLinks.TryGetValue(dish, out DishLinkGroup group) ?
                new List<Food>(group.Members) : new List<Food> { dish };
        }

        // EN: Requires the tapped dish at the front for normal picks, then checks group exposure, ice and lids; PickupHand may bypass depth.
        // VI: Yêu cầu đĩa được chạm ở hàng đầu khi chọn thường, rồi kiểm tra độ lộ nhóm, băng và nắp; PickupHand được bỏ qua độ sâu.
        private bool CanPickGroup(Food dish, bool ignoreDepth)
        {
            if (dish == null || (!ignoreDepth && !IsDishAtHead(dish)))
                return false;
            if (dishLinks.TryGetValue(dish, out DishLinkGroup group))
                return group.CanTake(false) && (ignoreDepth || group.IsExposed(columnDishes));
            return !dish.IsFrozen && !dish.isHidden && dish.TotalAmount > 0 && (ignoreDepth || IsDishAtHead(dish));
        }

        // EN: Validates the group and reserves its slots, committing any booster cost before launches; failed payment leaves the board unchanged.
        // VI: Kiểm tra nhóm và giữ slot, chốt chi phí booster trước khi bay; thanh toán thất bại giữ nguyên board.
        // EN: Uses the shared ingredient configuration instead of per-food catalogue references.
        // VI: Dùng cấu hình nguyên liệu chung thay cho tham chiếu bảng riêng trên từng food.
        private bool PickGroup(Food dish, bool ignoreDepth, System.Func<bool> commitCost = null)
        {
            if (Time.timeScale <= 0f)
            {
                LogPickFailure("game đang tạm dừng (Time.timeScale = 0)", dish);
                return false;
            }
            List<Food> members = GetPickMembers(dish);
            if (!CanPickGroup(dish, ignoreDepth))
            {
                LogPickFailure(DescribePickBlock(dish), dish);
                foreach (Food member in members)
                    if (member != null)
                        member.Selection?.PlayRejected();
                return false;
            }
            if (!pickBatch.TryFill(members, ChefManager.Instance, commitCost))
            {
                LogPickFailure(pickBatch.LastFailure ?? "không giữ được slot băng chuyền", dish);
                foreach (Food member in members)
                    member.Selection?.PlayRejected();
                return false;
            }
            List<DishPickBatch.Entry> entries = new List<DishPickBatch.Entry>(pickBatch.Entries);
            foreach (DishPickBatch.Entry entry in entries)
                LaunchPickAnimation(entry);
            foreach (Transform column in ingredientColumns)
                FinishColumnShift(column);
            foreach (Food member in members)
            {
                ClearDishFootprint(member);
                RemoveFromBoard(member);
            }
            SettleBoard();
            ReduceFrozenBlocks();
            foreach (DishPickBatch.Entry entry in entries)
                DishPicked?.Invoke(entry.type, entry.slots);
            return true;
        }

        // EN: Explains why CanPickGroup refused a dish (link group not exposed or blocked, ice, lid, no portion left), for the pick-failure log.
        // VI: Giải thích vì sao CanPickGroup từ chối đĩa (nhóm nối chưa lộ hoặc bị chặn, băng, nắp, hết phần), dùng cho log chọn thất bại.
        private string DescribePickBlock(Food dish)
        {
            if (dishLinks.TryGetValue(dish, out DishLinkGroup group))
                return group.IsExposed(columnDishes) ? "nhóm liên kết còn thành viên đóng băng/đang ẩn/hết phần" : "nhóm liên kết chưa lộ hết ra hàng đầu";
            if (dish.IsFrozen)
                return $"đĩa đang đóng băng (còn {dish.blockValue})";
            if (dish.isHidden)
                return "đĩa còn nắp (đang mở nắp hoặc chưa tới hàng đầu)";
            if (dish.TotalAmount <= 0)
                return "đĩa không còn phần nguyên liệu";
            return "đĩa không ở hàng đầu";
        }

        // EN: Loads chains from original anchor coordinates before gravity changes their rows, skipping invalid groups atomically.
        // VI: Nạp chuỗi theo tọa độ ô gốc trước khi dồn đổi hàng, bỏ qua nguyên nhóm không hợp lệ.
        private void BuildDishLinks(IReadOnlyList<DishLinkConfig> config)
        {
            if (config == null)
                return;
            foreach (DishLinkConfig chain in config)
            {
                if (!TryResolveChain(chain, out List<Food> members))
                {
                    Debug.LogWarning("[FoodBoardManager] Bỏ qua nhóm Dish Link không hợp lệ; cần 2–3 ô gốc khác nhau, có đĩa và chưa thuộc nhóm khác.", this);
                    continue;
                }
                DishLinkGroup group = new DishLinkGroup(members);
                foreach (Food dish in members)
                    dishLinks.Add(dish, group);
                CreateLinkView(group);
            }
        }

        // EN: Resolves distinct real anchors and prevents a dish from belonging to multiple chains.
        // VI: Tra các ô gốc thật khác nhau và ngăn một đĩa thuộc nhiều chuỗi.
        private bool TryResolveChain(DishLinkConfig chain, out List<Food> members)
        {
            members = new List<Food>();
            if (chain?.dishes == null || chain.dishes.Count < 2 || chain.dishes.Count > 3)
                return false;
            foreach (Vector2Int cell in chain.dishes)
            {
                if (!IsDishAnchor(cell.x, cell.y))
                    return false;
                Food dish = columnDishes[cell.x][cell.y];
                if (dishLinks.ContainsKey(dish) || members.Contains(dish))
                    return false;
                members.Add(dish);
            }
            return true;
        }

        // EN: Creates one view per chain, with exactly one straight segment for A–B and another for B–C.
        // VI: Tạo một hình hiển thị mỗi chuỗi, đúng một đoạn thẳng A–B và một đoạn B–C.
        private void CreateLinkView(DishLinkGroup group)
        {
            GameObject root = new GameObject("Dish Link · A–B–C");
            root.transform.SetParent(transform, false);
            DishLinkView view = root.AddComponent<DishLinkView>();
            view.Initialize(group.Members.Count);
            linkViews.Add(group, view);
            view.Refresh(group.Members, HasHeadMember(group));
        }

        // EN: Reports whether any linked dish is at the front, independently of whole-group pick eligibility.
        // VI: Cho biết có đĩa nối nào ở đầu hàng, độc lập với điều kiện lấy cả nhóm.
        private bool HasHeadMember(DishLinkGroup group)
        {
            foreach (Food dish in group.Members)
                if (IsDishAtHead(dish))
                    return true;
            return false;
        }

        // EN: Lights the whole straight cord when any member is at the front, but only front dishes receive resting highlights.
        // VI: Làm sáng toàn bộ dây thẳng khi có thành viên ở đầu hàng, nhưng chỉ đĩa hàng đầu có viền sáng khi nghỉ.
        private void UpdateDishLinkViews()
        {
            foreach (KeyValuePair<DishLinkGroup, DishLinkView> entry in linkViews)
            {
                entry.Value.Refresh(entry.Key.Members, HasHeadMember(entry.Key));
                if (!InputLocked)
                    foreach (Food dish in entry.Key.Members)
                        dish.Selection?.SetSelectable(IsDishAtHead(dish));
            }
        }

        // EN: Opens lower same-column group lids once only members remain above them, avoiding permanent hidden-group deadlocks.
        // VI: Mở nắp thành viên thấp cùng cột khi phía trên chỉ còn đĩa trong nhóm, tránh nhóm ẩn bị kẹt vĩnh viễn.
        private bool IsDishExposedForReveal(Food dish)
        {
            return IsDishAtHead(dish) || (dishLinks.TryGetValue(dish, out DishLinkGroup group) && group.IsExposed(columnDishes));
        }

        // EN: Removes exhausted members for AutoComplete; remaining A–C reconnect directly and singleton chains dissolve.
        // VI: Gỡ thành viên hết nguyên liệu do AutoComplete; A–C còn lại nối thẳng và chuỗi còn một đĩa tự giải thể.
        private void DetachDishLink(Food dish)
        {
            if (dish == null || !dishLinks.TryGetValue(dish, out DishLinkGroup group))
                return;
            dishLinks.Remove(dish);
            DestroyLinkView(group);
            group.Remove(dish);
            if (group.Members.Count >= 2)
                CreateLinkView(group);
            else
                foreach (Food remaining in group.Members)
                    dishLinks.Remove(remaining);
        }

        // EN: Clears every placeholder of a dish without shifting any column until the entire group has been removed.
        // VI: Xóa mọi ô giữ chỗ của đĩa mà chưa dồn cột nào cho đến khi gỡ xong cả nhóm.
        private void ClearDishFootprint(Food dish)
        {
            foreach (List<Food> dishes in columnDishes)
                for (int r = 0; r < dishes.Count; r++)
                    if (dishes[r] == dish)
                        dishes[r] = null;
        }

        // EN: Disables a chain immediately before releasing its generated visuals in play or editor previews.
        // VI: Tắt chuỗi ngay trước khi giải phóng hình sinh trong gameplay hoặc xem trước editor.
        private void DestroyLinkView(DishLinkGroup group)
        {
            if (!linkViews.TryGetValue(group, out DishLinkView view))
                return;
            linkViews.Remove(group);
            if (view == null)
                return;
            view.gameObject.SetActive(false);
            if (Application.isPlaying)
                Destroy(view.gameObject);
            else
                DestroyImmediate(view.gameObject);
        }

        // EN: Cancels all live links before replay or manager destruction so pooled dishes never keep old chain references.
        // VI: Hủy mọi liên kết trước replay hoặc hủy manager để đĩa trong pool không giữ tham chiếu chuỗi cũ.
        private void ClearDishLinks()
        {
            foreach (DishLinkGroup group in new List<DishLinkGroup>(linkViews.Keys))
                DestroyLinkView(group);
            dishLinks.Clear();
        }
    }
}
