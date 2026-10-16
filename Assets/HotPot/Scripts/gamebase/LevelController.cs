using UnityEngine;

namespace FoodPop
{
    // EN: Coordinates level loading and delegates ingredient-board setup to its scene manager.
    // VI: Điều phối nạp level và giao việc thiết lập board nguyên liệu cho manager của scene.
    public class LevelController : MonoBehaviour
    {
        public int currentLevel;
        [SerializeField] private LevelConfig levelConfig;

        // EN: Loads the assigned level after scene managers have initialized.
        // VI: Nạp level đã gán sau khi các manager của scene khởi tạo.
        private void Start()
        {
            LoadLevel(levelConfig);
        }

        // EN: Selects the level configuration and delegates its ingredient board to FoodBoardManager.
        // VI: Chọn cấu hình level và giao bảng nguyên liệu cho FoodBoardManager xử lý.
        public void LoadLevel(LevelConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning("[LevelController] Chưa gán LevelConfig.", this);
                return;
            }
            levelConfig = config;
            FoodBoardManager boardManager = FoodBoardManager.Instance;
            if (boardManager == null)
            {
                Debug.LogWarning("[LevelController] Chưa có FoodBoardManager trong scene.", this);
                return;
            }
            boardManager.LoadIngredientBoard(config.ingredientBoard);
        }
    }
}
