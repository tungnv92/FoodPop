using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FoodPop
{
    // EN: Refills all ingredient portions from order totals while retaining double-dish footprints.
    // VI: Điền lại mọi phần nguyên liệu theo tổng order, giữ vùng chiếm của đĩa đôi.
    public static class LevelBoardOrderFiller
    {
        private static readonly FoodType[] Types = (FoodType[])Enum.GetValues(typeof(FoodType));

        private sealed class Portion
        {
            public readonly OrderFood cell;
            public readonly bool second;
            private readonly bool preserveState;

            // EN: Identifies one half and optionally preserves shared state for linked single dishes.
            // VI: Xác định một nửa và tùy chọn giữ trạng thái chung của đĩa đơn liên kết.
            public Portion(OrderFood cell, bool second, bool preserveState = false)
            {
                this.cell = cell;
                this.second = second;
                this.preserveState = preserveState;
            }

            // EN: Writes a half without changing double footprints or linked dishes' concealment and ice.
            // VI: Ghi một nửa mà không đổi vùng chiếm đĩa đôi hoặc nắp và băng của đĩa nối.
            public void Write(FoodType type, int count)
            {
                if (!cell.IsDouble)
                {
                    bool hidden = cell.isHidden;
                    bool frozen = cell.isFrozen;
                    int block = cell.blockValue;
                    cell.Clear();
                    if (preserveState)
                    {
                        cell.isHidden = hidden;
                        cell.isFrozen = frozen;
                        cell.blockValue = block;
                    }
                }
                if (second)
                {
                    cell.secondFoodType = type;
                    cell.secondCount = count;
                }
                else
                {
                    cell.foodType = type;
                    cell.requiredCount = count;
                }
            }
        }

        // EN: Validates before editing and guarantees food for all double halves and linked dishes before ordinary singles.
        // VI: Kiểm tra trước khi sửa, bảo đảm nguyên liệu cho mọi nửa đĩa đôi và đĩa nối trước đĩa đơn thường.
        public static bool TryFill(LevelConfig level, int[] needed, out string message)
        {
            message = null;
            if (level == null || needed == null || needed.Length != Types.Length)
            {
                message = "Dữ liệu level hoặc tổng nguyên liệu không hợp lệ.";
                return false;
            }
            List<string> linkProblems = new List<string>();
            level.CollectDishLinkProblems(linkProblems, false);
            if (linkProblems.Count > 0)
            {
                message = string.Join("\n", linkProblems);
                return false;
            }
            if (!CollectPortions(level, out List<Portion> portions, out List<OrderFood> coveredCells, out int doublePortions, out message))
                return false;
            List<int> types = new List<int>();
            int total = 0;
            for (int i = 1; i < needed.Length; i++)
            {
                if (needed[i] <= 0)
                    continue;
                types.Add(i);
                total += needed[i];
            }
            if (types.Count > portions.Count)
            {
                message = $"Order cần {types.Count} loại nguyên liệu nhưng bảng chỉ có {portions.Count} vị trí, đã tính cả hai phần đĩa đôi. Hãy tăng kích thước bảng.";
                return false;
            }
            if (total < doublePortions)
            {
                message = $"Order chỉ cần {total} phần nhưng các đĩa đôi và đĩa nối cần ít nhất {doublePortions} phần. Hãy thêm order hoặc giảm số đĩa đặc biệt. Bảng chưa bị thay đổi.";
                return false;
            }
            int[] allocation = AllocatePortions(types, needed, portions.Count);
            Undo.RecordObject(level, "Điền bảng theo order");
            int next = 0;
            foreach (int type in types)
            {
                int count = allocation[type];
                int amount = needed[type] / count;
                int remainder = needed[type] % count;
                for (int i = 0; i < count; i++)
                    portions[next++].Write(Types[type], amount + (i < remainder ? 1 : 0));
            }
            int unused = portions.Count - next;
            for (; next < portions.Count; next++)
                portions[next].cell.Clear();
            foreach (OrderFood covered in coveredCells)
                covered.Clear();
            EditorUtility.SetDirty(level);
            if (unused > 0)
                message = $"Đã điền đủ đĩa đôi và nhóm nối theo tổng order; còn {unused} ô đơn trống.";
            return true;
        }

        // EN: Prioritizes doubles and linked singles, rejecting invalid footprints without mutating board data.
        // VI: Ưu tiên đĩa đôi và đĩa đơn nối, từ chối vùng chiếm sai mà không đổi dữ liệu bảng.
        private static bool CollectPortions(LevelConfig level, out List<Portion> portions, out List<OrderFood> coveredCells, out int doublePortions, out string message)
        {
            portions = new List<Portion>();
            coveredCells = new List<OrderFood>();
            List<Portion> singles = new List<Portion>();
            HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
            HashSet<Vector2Int> linked = new HashSet<Vector2Int>();
            if (level.dishLinks != null)
                foreach (DishLinkConfig chain in level.dishLinks)
                    if (chain?.dishes != null)
                        foreach (Vector2Int position in chain.dishes)
                            linked.Add(position);
            doublePortions = 0;
            message = null;
            for (int c = 0; c < level.ingredientBoard.Count; c++)
            {
                List<OrderFood> foods = level.ingredientBoard[c]?.foods;
                if (foods == null)
                    continue;
                for (int r = 0; r < foods.Count; r++)
                {
                    OrderFood cell = foods[r];
                    if (cell == null || level.IsCoveredCell(c, r))
                        continue;
                    if (!occupied.Add(new Vector2Int(c, r)))
                    {
                        message = "Có vùng chiếm đĩa đôi bị chồng nhau. Hãy sửa bố cục trước khi điền.";
                        return false;
                    }
                    if (!cell.IsDouble)
                    {
                        if (linked.Contains(new Vector2Int(c, r)))
                            portions.Add(new Portion(cell, false, true));
                        else
                            singles.Add(new Portion(cell, false));
                        continue;
                    }
                    if ((cell.dishShape != DishShape.Vertical && cell.dishShape != DishShape.Horizontal) ||
                        !level.TryGetCoveredCell(c, r, out int x, out int y) || !occupied.Add(new Vector2Int(x, y)))
                    {
                        message = $"Đĩa đôi tại cột {c + 1}, hàng {r + 1} vượt bảng hoặc chồng lên đĩa khác. Hãy sửa bố cục trước khi điền.";
                        return false;
                    }
                    portions.Add(new Portion(cell, false));
                    portions.Add(new Portion(cell, true));
                    coveredCells.Add(level.GetCell(x, y));
                }
            }
            doublePortions = portions.Count;
            portions.AddRange(singles);
            return true;
        }

        // EN: Distributes slots across types by highest quantity per slot, never allocating more slots than available portions.
        // VI: Chia vị trí cho các loại theo số lượng mỗi vị trí cao nhất, không chia nhiều vị trí hơn số phần hiện có.
        private static int[] AllocatePortions(List<int> types, int[] needed, int capacity)
        {
            int[] allocation = new int[needed.Length];
            foreach (int type in types)
                allocation[type] = 1;
            for (int remaining = capacity - types.Count; remaining > 0; remaining--)
            {
                int best = -1;
                foreach (int type in types)
                {
                    if (allocation[type] >= needed[type])
                        continue;
                    if (best < 0 || (long)needed[type] * allocation[best] > (long)needed[best] * allocation[type] ||
                        ((long)needed[type] * allocation[best] == (long)needed[best] * allocation[type] && needed[type] > needed[best]))
                        best = type;
                }
                if (best < 0)
                    break;
                allocation[best]++;
            }
            return allocation;
        }
    }
}
