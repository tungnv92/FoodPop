using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: An ordered chain of two or three dish anchors; coordinates are zero-based (column, row).
    // VI: Chuỗi có thứ tự gồm hai hoặc ba ô gốc đĩa; tọa độ bắt đầu từ 0 (cột, hàng).
    [System.Serializable]
    public sealed class DishLinkConfig
    {
        [Tooltip("Các ô gốc theo thứ tự A–B–C; x là cột, y là hàng, bắt đầu từ 0.")]
        public List<Vector2Int> dishes = new List<Vector2Int>();
    }
}
