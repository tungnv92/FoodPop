using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    public partial class FoodBoardManager
    {
        private readonly Dictionary<Food, DishLinkGroup> dishLinks = new Dictionary<Food, DishLinkGroup>();
        private readonly Dictionary<DishLinkGroup, DishLinkView> linkViews = new Dictionary<DishLinkGroup, DishLinkView>();
        private readonly Stack<DishLinkView> linkViewPool = new Stack<DishLinkView>();
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

        // EN: Takes a Dish Link view from the pool (instantiating the prefab only when empty), with one straight segment for A–B and another for B–C.
        // VI: Lấy view Dish Link từ pool (chỉ tạo từ prefab khi pool rỗng), một đoạn thẳng A–B và một đoạn B–C.
        private void CreateLinkView(DishLinkGroup group)
        {
            DishLinkView view = linkViewPool.Count > 0 ? linkViewPool.Pop() : Instantiate(dishLinkPrefab, transform, false);
            view.name = "Dish Link · A–B–C";
            view.gameObject.SetActive(true);
            view.Initialize(group.Members.Count);
            linkViews.Add(group, view);
            RefreshLinkView(group, view);
        }

        // EN: Redraws one chain with its members' current board depths and front-row highlight.
        // VI: Vẽ lại một chuỗi theo độ sâu hàng hiện tại của các thành viên và trạng thái sáng ở hàng đầu.
        private void RefreshLinkView(DishLinkGroup group, DishLinkView view)
        {
            List<int> rows = new List<int>(group.Members.Count);
            foreach (Food dish in group.Members)
                rows.Add(GetDishDepth(dish));
            view.Refresh(group.Members, rows, HasHeadMember(group));
        }

        // EN: Returns the deepest board row a dish occupies (vertical doubles span two rows), or -1 when it is off the board.
        // VI: Trả về hàng sâu nhất mà đĩa chiếm trên board (đĩa đôi dọc chiếm hai hàng), hoặc -1 khi đĩa không còn trên board.
        private int GetDishDepth(Food dish)
        {
            int depth = -1;
            foreach (List<Food> dishes in columnDishes)
                for (int r = 0; r < dishes.Count; r++)
                    if (dishes[r] == dish)
                        depth = Mathf.Max(depth, r);
            return depth;
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
                RefreshLinkView(entry.Key, entry.Value);
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

        // EN: Removes a dish leaving the board (picked, AutoComplete or returned); remaining A–C reconnect on the same view and singleton chains dissolve, returning their view to the pool.
        // VI: Gỡ đĩa rời board (được lấy, AutoComplete hoặc trả về); A–C còn lại nối thẳng trên cùng view, chuỗi còn một đĩa tự giải thể và trả view về pool.
        private void DetachDishLink(Food dish)
        {
            if (dish == null || !dishLinks.TryGetValue(dish, out DishLinkGroup group))
                return;
            dishLinks.Remove(dish);
            group.Remove(dish);
            if (group.Members.Count >= 2)
            {
                RefreshLinkView(group, linkViews[group]);
                return;
            }
            ReleaseLinkView(group);
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

        // EN: Returns the members' food to its normal layer, then hides the chain's view and keeps it in the pool during play; editor previews destroy it so no hidden view is saved into the scene.
        // VI: Trả món của các thành viên về lớp vẽ thường, rồi ẩn view của chuỗi và cất vào pool khi đang chơi; xem trước editor thì hủy để không lưu view ẩn vào scene.
        private void ReleaseLinkView(DishLinkGroup group)
        {
            if (!linkViews.TryGetValue(group, out DishLinkView view))
                return;
            linkViews.Remove(group);
            if (view == null)
                return;
            view.ResetIngredientLayers(group.Members);
            view.gameObject.SetActive(false);
            if (Application.isPlaying)
                linkViewPool.Push(view);
            else
                DestroyImmediate(view.gameObject);
        }

        // EN: Releases all live links before replay or manager destruction so pooled dishes never keep old chain references.
        // VI: Trả mọi liên kết về pool trước replay hoặc hủy manager để đĩa trong pool không giữ tham chiếu chuỗi cũ.
        private void ClearDishLinks()
        {
            foreach (DishLinkGroup group in new List<DishLinkGroup>(linkViews.Keys))
                ReleaseLinkView(group);
            dishLinks.Clear();
        }
    }
}
