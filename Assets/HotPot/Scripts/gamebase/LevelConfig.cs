using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Level data: the customers to serve in the two counter queues, plus the board of raw ingredients the level starts with.
    // VI: Dữ liệu level: các khách cần phục vụ ở hai hàng trên quầy và bảng nguyên liệu thô ban đầu của level.
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Hot Pot/Level Config")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Customers")]
        [Tooltip("Khách ở hàng trái; phần tử 0 được phục vụ trước.")]
        public List<CustomerOrder> leftQueue = new List<CustomerOrder>();

        [Tooltip("Khách ở hàng phải; phần tử 0 được phục vụ trước.")]
        public List<CustomerOrder> rightQueue = new List<CustomerOrder>();

        [Header("Ingredient board (raw foods)")]
        [Tooltip("Số hàng = số nguyên liệu trong mỗi cột.")]
        [Min(1)] public int boardRows = 3;

        [Tooltip("Số cột của bảng nguyên liệu.")]
        [Min(1)] public int boardColumns = 4;

        [Tooltip("Mỗi phần tử là một cột (trái sang phải), liệt kê nguyên liệu thô từ trên xuống dưới.")]
        public List<BoardColumn> ingredientBoard = new List<BoardColumn>();

        // EN: Total number of customers to serve in this level (both queues).
        // VI: Tổng số khách cần phục vụ trong level (cả hai hàng).
        public int TotalCustomers => leftQueue.Count + rightQueue.Count;

        // EN: Returns the queue for a counter side: 0 = left, 1 = right.
        // VI: Trả về hàng khách theo phía quầy: 0 = trái, 1 = phải.
        public List<CustomerOrder> GetQueue(int side)
        {
            return side == 0 ? leftQueue : rightQueue;
        }

        // EN: Returns the ingredient at a board cell (column 0 = left, row 0 = top), or null when out of range.
        // VI: Trả về nguyên liệu tại một ô (cột 0 = bên trái, hàng 0 = trên cùng), hoặc null nếu ngoài phạm vi.
        public OrderFood GetBoardCell(int column, int row)
        {
            if (column < 0 || column >= ingredientBoard.Count)
                return null;
            List<OrderFood> foods = ingredientBoard[column]?.foods;
            if (foods == null || row < 0 || row >= foods.Count)
                return null;
            return foods[row];
        }

        // EN: Total raw count of a food type on the board (useful to check it covers all customer orders).
        // VI: Tổng số lượng nguyên liệu thô của một loại trên bảng (dùng để kiểm tra có đủ cho mọi order của khách).
        public int GetBoardCount(FoodType foodType)
        {
            int total = 0;
            foreach (BoardColumn column in ingredientBoard)
            {
                if (column?.foods == null)
                    continue;
                foreach (OrderFood cell in column.foods)
                {
                    if (cell != null && cell.foodType == foodType)
                        total += cell.requiredCount;
                }
            }
            return total;
        }

        // EN: Keeps the board at boardColumns columns of boardRows foods whenever values change in the Inspector.
        // VI: Giữ bảng luôn có boardColumns cột, mỗi cột boardRows nguyên liệu, mỗi khi đổi giá trị trong Inspector.
        private void OnValidate()
        {
            ResizeBoard();
        }

        // EN: Adds or trims columns, then adds or trims foods inside each column, to match the board size.
        // VI: Thêm hoặc bớt cột, rồi thêm hoặc bớt nguyên liệu trong từng cột, cho khớp kích thước bảng.
        public void ResizeBoard()
        {
            int columns = Mathf.Max(1, boardColumns);
            int rows = Mathf.Max(1, boardRows);
            while (ingredientBoard.Count < columns)
                ingredientBoard.Add(new BoardColumn());
            if (ingredientBoard.Count > columns)
                ingredientBoard.RemoveRange(columns, ingredientBoard.Count - columns);
            foreach (BoardColumn column in ingredientBoard)
                column.Resize(rows);
        }
    }

    // EN: One column of the ingredient board: its raw foods from top to bottom (a list inside the board list).
    // VI: Một cột của bảng nguyên liệu: các nguyên liệu thô từ trên xuống dưới (list nằm trong list của bảng).
    [System.Serializable]
    public class BoardColumn
    {
        public List<OrderFood> foods = new List<OrderFood>();

        // EN: Adds empty foods or trims extra ones so the column holds exactly the given number of rows.
        // VI: Thêm ô trống hoặc cắt bớt để cột có đúng số hàng cho trước.
        public void Resize(int rows)
        {
            if (foods == null)
                foods = new List<OrderFood>();
            while (foods.Count < rows)
                foods.Add(new OrderFood());
            if (foods.Count > rows)
                foods.RemoveRange(rows, foods.Count - rows);
        }
    }
}
