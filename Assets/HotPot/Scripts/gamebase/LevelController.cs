using UnityEngine;

namespace FoodPop
{
    // EN: Coordinates level loading: hands the ingredient board to FoodBoardManager and the customer queues to CustomerManager.
    // VI: Điều phối nạp level: giao bảng nguyên liệu cho FoodBoardManager và hàng khách cho CustomerManager.
    public class LevelController : MonoBehaviour
    {
        [Tooltip("Cấu hình level được nạp khi bắt đầu.")]
        [SerializeField] private LevelConfig levelConfig;
        [Tooltip("Manager bảng nguyên liệu của scene.")]
        [SerializeField] private FoodBoardManager boardManager;
        [Tooltip("Manager khách hàng và order của scene.")]
        [SerializeField] private CustomerManager customerManager;

        // EN: Loads the assigned level after scene managers have initialized.
        // VI: Nạp level đã gán sau khi các manager của scene khởi tạo.
        private void Start()
        {
            LoadLevel(levelConfig);
        }

        // EN: Cancels collection and resets holding storage before loading the board links and customer queues.
        // VI: Hủy lượt thu và đặt lại kho giữ món trước khi nạp liên kết board cùng hàng khách.
        public void LoadLevel(LevelConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning("[LevelController] Chưa gán LevelConfig.", this);
                return;
            }
            levelConfig = config;
            if (BoosterController.Instance != null)
                BoosterController.Instance.CancelCleanCollection();
            FoodHoldingStorage storage = FindFirstObjectByType<FoodHoldingStorage>();
            if (storage != null) storage.ResetForLevel();
            if (boardManager != null)
                boardManager.LoadIngredientBoard(config.ingredientBoard, config.dishLinks);
            else
                Debug.LogWarning("[LevelController] Chưa gán FoodBoardManager.", this);
            if (customerManager != null)
                customerManager.LoadCustomers(config);
            else
                Debug.LogWarning("[LevelController] Chưa gán CustomerManager.", this);
        }
    }
}
