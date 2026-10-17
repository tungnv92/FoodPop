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

        // EN: Selects the level configuration, fills the ingredient board and loads the customer queues.
        // VI: Chọn cấu hình level, điền bảng nguyên liệu và nạp hàng khách.
        public void LoadLevel(LevelConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning("[LevelController] Chưa gán LevelConfig.", this);
                return;
            }
            levelConfig = config;
            if (boardManager != null)
                boardManager.LoadIngredientBoard(config.ingredientBoard);
            else
                Debug.LogWarning("[LevelController] Chưa gán FoodBoardManager.", this);
            if (customerManager != null)
                customerManager.LoadCustomers(config);
            else
                Debug.LogWarning("[LevelController] Chưa gán CustomerManager.", this);
        }
    }
}
