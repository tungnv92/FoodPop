using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Level data: the customers to serve in the two counter queues, plus the board of raw ingredients the level starts with.
    // VI: Dữ liệu level: các khách cần phục vụ ở hai hàng trên quầy và bảng nguyên liệu thô ban đầu của level.
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Hot Pot/Level Config")]
    public partial class LevelConfig : ScriptableObject
    {
        // EN: Folder under a Resources directory that holds every playable level (Assets/HotPot/Config/Resources/Levels).
        // VI: Thư mục trong Resources chứa mọi level chơi được (Assets/HotPot/Config/Resources/Levels).
        public const string ResourceFolder = "Levels";

        // EN: Counts customers in the shared queue or legacy fallback.
        // VI: Đếm khách trong hàng chung hoặc dữ liệu cũ.
        public int TotalCustomers => GetCustomerQueue().Count;

        [Tooltip("Số màn hiển thị trên thanh giao diện phía trên; phải khớp số trong tên file.")]
        [Min(1)] public int levelNumber = 1;

        [Tooltip("Độ khó của màn; màn Khó dùng bản cùng số có hậu tố _Easy khi số lần chơi lại từ hai trở lên.")]
        public DifficultyMode difficultyMode = DifficultyMode.Normal;

        [Header("Customers")]
        [Tooltip("Dùng danh sách chung kể cả khi trống.")]
        public bool useUnifiedCustomerQueue;
        [Tooltip("Khách theo thứ tự, chỗ trống nhận khách kế tiếp.")]
        public List<CustomerOrder> customerQueue = new List<CustomerOrder>();
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

        [Header("Dish Link")]
        [Tooltip("Nhóm 2–3 đĩa nối theo thứ tự A–B–C; mỗi đoạn dây thẳng, tọa độ ô gốc bắt đầu từ 0.")]
        public List<DishLinkConfig> dishLinks = new List<DishLinkConfig>();

        // EN: Keeps the board at boardColumns columns of boardRows foods whenever values change in the Inspector.
        // VI: Giữ bảng luôn có boardColumns cột, mỗi cột boardRows nguyên liệu, mỗi khi đổi giá trị trong Inspector.
        private void OnValidate()
        {
            ResizeBoard();
        }

        // EN: Standard asset name for a level number (two digits minimum), e.g. LevelConfig_03 or LevelConfig_120.
        // VI: Tên asset chuẩn theo số level (tối thiểu hai chữ số), ví dụ LevelConfig_03 hoặc LevelConfig_120.
        public static string AssetName(int number) => $"LevelConfig_{number:00}";

        // EN: Loads only the requested level from Resources/Levels; returns null when that level does not exist.
        // VI: Chỉ nạp level được yêu cầu từ Resources/Levels; trả về null khi level đó không tồn tại.
        public static LevelConfig Load(int number) =>
            number < 1 ? null : Resources.Load<LevelConfig>($"{ResourceFolder}/{AssetName(number)}");

        // EN: Selects an authored Hard level's Easy variant after two retries, keeping the original configuration when the Easy asset is absent.
        // VI: Chọn biến thể Easy của màn được cấu hình Hard sau hai lần chơi lại, giữ bản gốc khi không có asset Easy.
        public LevelConfig ForRetry(int retryCount)
        {
            if (difficultyMode != DifficultyMode.Hard || retryCount < 2)
                return this;
            LevelConfig easy = Resources.Load<LevelConfig>($"{ResourceFolder}/{AssetName(levelNumber)}_Easy");
            return easy != null ? easy : this;
        }

        // EN: Copies the shared queue, interleaving old left/right data when necessary.
        // VI: Sao chép hàng chung, xen kẽ dữ liệu trái/phải cũ khi cần.
        public List<CustomerOrder> GetCustomerQueue()
        {
            if (useUnifiedCustomerQueue || (customerQueue != null && customerQueue.Count > 0))
                return customerQueue == null ? new List<CustomerOrder>() : new List<CustomerOrder>(customerQueue);
            var result = new List<CustomerOrder>();
            for (int i = 0; i < Mathf.Max(leftQueue?.Count ?? 0, rightQueue?.Count ?? 0); i++)
            {
                if (i < (leftQueue?.Count ?? 0)) result.Add(leftQueue[i]);
                if (i < (rightQueue?.Count ?? 0)) result.Add(rightQueue[i]);
            }
            return result;
        }

        // EN: Returns the queue for a counter side: 0 = left, 1 = right.
        // VI: Trả về hàng khách theo phía quầy: 0 = trái, 1 = phải.
        public List<CustomerOrder> GetQueue(int side)
        {
            return side == 0 ? leftQueue : rightQueue;
        }

        // EN: Total raw count of a food type on the board, including both parts of double dishes and skipping cells covered by a double dish (useful to check it covers all customer orders).
        // VI: Tổng số lượng nguyên liệu thô của một loại trên bảng, tính cả hai phần của đĩa đôi và bỏ qua ô bị đĩa đôi chiếm (dùng để kiểm tra có đủ cho mọi order của khách).
        public int GetBoardCount(FoodType foodType)
        {
            int total = 0;
            for (int c = 0; c < ingredientBoard.Count; c++)
            {
                List<OrderFood> foods = ingredientBoard[c]?.foods;
                if (foods == null)
                    continue;
                for (int r = 0; r < foods.Count; r++)
                {
                    if (foods[r] != null && !IsCoveredCell(c, r))
                        total += foods[r].CountOf(foodType);
                }
            }
            return total;
        }

        // EN: Returns the cell at a column and row, or null when it is outside the board.
        // VI: Trả về ô tại cột và hàng đã cho, hoặc null nếu nằm ngoài bảng.
        public OrderFood GetCell(int column, int row)
        {
            if (column < 0 || column >= ingredientBoard.Count)
                return null;
            List<OrderFood> foods = ingredientBoard[column]?.foods;
            return foods != null && row >= 0 && row < foods.Count ? foods[row] : null;
        }

        // EN: Finds the double dish that covers a cell: a vertical double right above it or a horizontal double right to its left. Returns false when the cell is not covered.
        // VI: Tìm đĩa đôi đang chiếm một ô: đĩa đôi dọc ngay phía trên hoặc đĩa đôi ngang ngay bên trái. Trả về false khi ô không bị chiếm.
        public bool TryGetCoveringDish(int column, int row, out int anchorColumn, out int anchorRow)
        {
            OrderFood above = GetCell(column, row - 1);
            if (above != null && above.dishShape == DishShape.Vertical)
            {
                anchorColumn = column;
                anchorRow = row - 1;
                return true;
            }
            OrderFood left = GetCell(column - 1, row);
            if (left != null && left.dishShape == DishShape.Horizontal)
            {
                anchorColumn = column - 1;
                anchorRow = row;
                return true;
            }
            anchorColumn = -1;
            anchorRow = -1;
            return false;
        }

        // EN: True when the cell is the second cell of a double dish anchored above or to the left of it.
        // VI: True khi ô là ô thứ hai của một đĩa đôi đặt ở phía trên hoặc bên trái.
        public bool IsCoveredCell(int column, int row)
        {
            return TryGetCoveringDish(column, row, out _, out _);
        }

        // EN: Returns the cell a double dish anchored at (column, row) also covers, or false for a single dish or when that cell is outside the board.
        // VI: Trả về ô thứ hai mà đĩa đôi đặt tại (column, row) chiếm thêm; trả về false nếu là đĩa đơn hoặc ô đó nằm ngoài bảng.
        public bool TryGetCoveredCell(int column, int row, out int coveredColumn, out int coveredRow)
        {
            OrderFood cell = GetCell(column, row);
            coveredColumn = column;
            coveredRow = row;
            if (cell == null || !cell.IsDouble)
                return false;
            if (cell.dishShape == DishShape.Vertical)
                coveredRow = row + 1;
            else
                coveredColumn = column + 1;
            return GetCell(coveredColumn, coveredRow) != null;
        }

        // EN: Lists placement problems of double dishes: a double sticking out of the board, a double whose second cell is covered twice or holds its own dish, or a double without a second ingredient.
        // VI: Liệt kê lỗi đặt đĩa đôi: đĩa đôi tràn ra ngoài bảng, ô thứ hai bị hai đĩa đôi cùng chiếm hoặc vẫn có đĩa riêng, hoặc đĩa đôi chưa chọn nguyên liệu phần hai.
        public void CollectBoardProblems(List<string> problems)
        {
            for (int c = 0; c < ingredientBoard.Count; c++)
            {
                List<OrderFood> foods = ingredientBoard[c]?.foods;
                if (foods == null)
                    continue;
                for (int r = 0; r < foods.Count; r++)
                    CollectCellProblems(c, r, problems);
            }
        }

        // EN: Normalizes dimensions and repairs missing lists or columns before resizing the rectangular board.
        // VI: Chuẩn hóa kích thước và sửa danh sách hoặc cột bị thiếu trước khi đổi kích thước bảng chữ nhật.
        public void ResizeBoard()
        {
            int columns = boardColumns = Mathf.Max(1, boardColumns);
            int rows = boardRows = Mathf.Max(1, boardRows);
            if (ingredientBoard == null)
                ingredientBoard = new List<BoardColumn>();
            while (ingredientBoard.Count < columns)
                ingredientBoard.Add(new BoardColumn());
            if (ingredientBoard.Count > columns)
                ingredientBoard.RemoveRange(columns, ingredientBoard.Count - columns);
            for (int c = 0; c < ingredientBoard.Count; c++)
            {
                if (ingredientBoard[c] == null)
                    ingredientBoard[c] = new BoardColumn();
                ingredientBoard[c].Resize(rows);
            }
        }

        // EN: Reports footprint conflicts, unsupported shapes and nonpositive portions before a level is played.
        // VI: Báo xung đột vùng chiếm, kiểu đĩa không hợp lệ và số phần không dương trước khi chơi level.
        private void CollectCellProblems(int column, int row, List<string> problems)
        {
            OrderFood cell = GetCell(column, row);
            if (cell == null)
                return;
            string name = $"Cột {column + 1} hàng {row + 1}";
            bool aboveVertical = GetCell(column, row - 1)?.dishShape == DishShape.Vertical;
            bool leftHorizontal = GetCell(column - 1, row)?.dishShape == DishShape.Horizontal;
            if (aboveVertical && leftHorizontal)
                problems.Add($"{name}: bị cả đĩa đôi dọc và đĩa đôi ngang cùng chiếm.");
            if ((aboveVertical || leftHorizontal) && (cell.foodType != FoodType.None || cell.IsDouble))
                problems.Add($"{name}: đang bị đĩa đôi chiếm nhưng vẫn có dữ liệu riêng (sẽ bị bỏ qua).");
            if (!cell.IsDouble)
                return;
            if (cell.dishShape != DishShape.Vertical && cell.dishShape != DishShape.Horizontal)
                problems.Add($"{name}: kiểu đĩa không hợp lệ.");
            if (!TryGetCoveredCell(column, row, out _, out _))
                problems.Add($"{name}: đĩa đôi {(cell.dishShape == DishShape.Vertical ? "dọc ở hàng cuối" : "ngang ở cột cuối")}, tràn ra ngoài bảng.");
            if (cell.foodType == FoodType.None || cell.secondFoodType == FoodType.None)
                problems.Add($"{name}: đĩa đôi chưa chọn đủ nguyên liệu cho cả hai phần.");
            if (cell.requiredCount <= 0 || cell.secondCount <= 0)
                problems.Add($"{name}: số lượng mỗi phần của đĩa đôi phải lớn hơn 0.");
        }
    }

    // EN: One column of the ingredient board: its raw foods from top to bottom (a list inside the board list).
    // VI: Một cột của bảng nguyên liệu: các nguyên liệu thô từ trên xuống dưới (list nằm trong list của bảng).
    [System.Serializable]
    public class BoardColumn
    {
        public List<OrderFood> foods = new List<OrderFood>();

        // EN: Resizes a column and repairs null cells so every footprint can reference an existing board cell.
        // VI: Đổi kích thước cột và sửa ô null để mọi vùng chiếm đều tham chiếu tới ô board tồn tại.
        public void Resize(int rows)
        {
            if (foods == null)
                foods = new List<OrderFood>();
            while (foods.Count < rows)
                foods.Add(new OrderFood());
            if (foods.Count > rows)
                foods.RemoveRange(rows, foods.Count - rows);
            for (int r = 0; r < foods.Count; r++)
                if (foods[r] == null)
                    foods[r] = new OrderFood();
        }
    }
}
