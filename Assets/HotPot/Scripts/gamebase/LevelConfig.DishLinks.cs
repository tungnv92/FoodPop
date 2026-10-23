using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    public partial class LevelConfig
    {
        // EN: Reports invalid group sizes, coordinates, covered cells, empty members and shared membership across chains.
        // VI: Báo kích thước nhóm, tọa độ, ô bị chiếm, thành viên trống và đĩa thuộc nhiều chuỗi không hợp lệ.
        public void CollectDishLinkProblems(List<string> problems, bool requireFood = true)
        {
            if (dishLinks == null)
                return;
            HashSet<Vector2Int> claimed = new HashSet<Vector2Int>();
            for (int i = 0; i < dishLinks.Count; i++)
            {
                DishLinkConfig chain = dishLinks[i];
                if (chain?.dishes == null || chain.dishes.Count < 2 || chain.dishes.Count > 3)
                {
                    problems.Add($"Dish Link {i + 1}: cần hai hoặc ba đĩa theo thứ tự A–B–C.");
                    continue;
                }
                foreach (Vector2Int position in chain.dishes)
                {
                    OrderFood cell = GetCell(position.x, position.y);
                    string label = $"Dish Link {i + 1}, cột {position.x + 1}, hàng {position.y + 1}";
                    if (cell == null || IsCoveredCell(position.x, position.y))
                        problems.Add($"{label}: phải chọn ô gốc đĩa nằm trong bảng, không chọn ô bị đĩa đôi chiếm.");
                    else if (requireFood && (cell.foodType == FoodType.None || cell.requiredCount <= 0))
                        problems.Add($"{label}: chưa có nguyên liệu hợp lệ.");
                    if (!claimed.Add(position))
                        problems.Add($"{label}: đĩa xuất hiện nhiều lần trong một nhóm hoặc thuộc nhiều nhóm.");
                }
            }
        }
    }
}
