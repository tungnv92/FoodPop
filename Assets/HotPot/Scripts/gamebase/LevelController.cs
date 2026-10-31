using System;
using UnityEngine;
using UnityEngine.Events;

namespace FoodPop
{
    // EN: Coordinates level loading: hands the ingredient board to FoodBoardManager and the customer queues to CustomerManager.
    // VI: Điều phối nạp level: giao bảng nguyên liệu cho FoodBoardManager và hàng khách cho CustomerManager.
    public class LevelController : MonoBehaviour
    {
        public enum LevelState { NotLoaded, Playing, Won, Lost, ReviveOffered, Reviving }
        public LevelState State { get; private set; }
        public event Action<LevelState> StateChanged;
        [SerializeField] private UnityEvent onWon = new UnityEvent();
        [SerializeField] private UnityEvent onLost = new UnityEvent();
        [Tooltip("Hiện bảng kết quả với nút sang màn kế tiếp khi thắng hoặc chơi lại khi thua.")]
        [SerializeField] private bool showResultPanel = true;
        private float previousTimeScale = 1f;
        private bool ownsTimePause;
        [Tooltip("Manager bật tắt các canvas popup đã đặt trong scene.")]
        [SerializeField] private UIManager uiManager;

        // EN: Checks outcomes after gameplay settles and recovers the revive offer if collection is interrupted.
        // VI: Kiểm tra kết quả khi gameplay ổn định và mở lại lời mời hồi sinh nếu lượt thu bị ngắt.
        private void LateUpdate()
        {
            if (State == LevelState.Reviving)
            {
                if (BoosterController.Instance == null || !BoosterController.Instance.IsRunning)
                    CompleteRevive(false);
                return;
            }
            if (State != LevelState.Playing || Time.timeScale <= 0f || customerManager == null)
                return;
            if (customerManager.AllServed)
                FinishLevel(LevelState.Won);
            else if (IsConveyorBlocked())
                OfferRevive();
        }

        // EN: Declares a deadlock only on a full settled belt with no pending customer work or acceptable food.
        // VI: Chỉ xác định bế tắc khi băng đầy ổn định, không còn xử lý khách đang chờ hoặc món khách có thể nhận.
        private bool IsConveyorBlocked()
        {
            ChefManager chef = ChefManager.Instance;
            if (chef == null || chef.IsBusy || chef.BoosterPaused || customerManager.BoosterPaused ||
                customerManager.HasPendingService() ||
                (BoosterController.Instance != null && BoosterController.Instance.IsRunning))
                return false;
            int occupied = 0;
            foreach (ConveyorSlot slot in chef.Slots)
            {
                if (slot == null || !slot.isActiveAndEnabled) continue;
                if (slot.isEmpty || slot.IsFoodArriving || slot.food == null || !slot.food.IsPrepared)
                    return false;
                if (customerManager.TryFindCustomer(slot.food.foodType))
                    return false;
                occupied++;
            }
            return occupied > 0;
        }

        // EN: Publishes victory or a declined defeat once, retaining the original time scale across the revive offer.
        // VI: Phát chiến thắng hoặc thất bại đã từ chối hồi sinh một lần, giữ tốc độ thời gian gốc qua lời mời hồi sinh.
        private void FinishLevel(LevelState result)
        {
            if (State != LevelState.Playing && !(State == LevelState.ReviveOffered && result == LevelState.Lost)) return;
            State = result;
            PauseGameplay();
            ShowResult(result);
            StateChanged?.Invoke(State);
            if (result == LevelState.Won) onWon.Invoke();
            else onLost.Invoke();
        }

        // EN: Freezes gameplay while recording its original speed only when acquiring the pause.
        // VI: Dừng gameplay và chỉ lưu tốc độ gốc khi bắt đầu giữ quyền tạm dừng.
        private void PauseGameplay()
        {
            if (!ownsTimePause)
            {
                previousTimeScale = Time.timeScale;
                ownsTimePause = true;
            }
            Time.timeScale = 0f;
        }

        // EN: Asks the scene UIManager to offer revive before loss without resetting level progress.
        // VI: Yêu cầu UIManager của scene mời hồi sinh trước khi thua mà không đặt lại tiến độ màn.
        private void OfferRevive()
        {
            State = LevelState.ReviveOffered;
            PauseGameplay();
            BoosterController boosters = BoosterController.Instance;
            if (ResolveUIManager())
                uiManager.ShowRevive(ReviveLevel, DeclineRevive, boosters != null && boosters.CanCleanForRevive());
            StateChanged?.Invoke(State);
        }

        // EN: Closes popups through UIManager, resumes time and starts Clean once for the accepted revive.
        // VI: Đóng popup qua UIManager, cho thời gian chạy lại và bắt đầu Clean một lần khi đồng ý hồi sinh.
        public void ReviveLevel()
        {
            if (State != LevelState.ReviveOffered) return;
            BoosterController boosters = BoosterController.Instance;
            if (boosters == null || !boosters.CanCleanForRevive()) return;
            State = LevelState.Reviving;
            if (ResolveUIManager()) uiManager.HideAll();
            RestoreTimeScale();
            StateChanged?.Invoke(State);
            if (!boosters.TryCleanForRevive(CompleteRevive)) CompleteRevive(false);
        }

        // EN: Continues the existing level after successful cleaning, or restores the offer if cleaning failed.
        // VI: Tiếp tục màn hiện tại sau khi dọn thành công, hoặc mở lại lời mời nếu dọn thất bại.
        private void CompleteRevive(bool success)
        {
            if (this == null || State != LevelState.Reviving) return;
            if (!success)
            {
                OfferRevive();
                return;
            }
            State = LevelState.Playing;
            StateChanged?.Invoke(State);
        }

        // EN: Confirms defeat only when the player refuses the pending revive offer.
        // VI: Chỉ xác nhận thua khi người chơi từ chối lời mời hồi sinh đang mở.
        public void DeclineRevive()
        {
            if (State == LevelState.ReviveOffered) FinishLevel(LevelState.Lost);
        }

        // EN: Restores only the time pause owned by this controller when reloading or leaving the scene.
        // VI: Chỉ khôi phục tạm dừng thời gian do controller này giữ khi nạp lại hoặc rời scene.
        private void RestoreTimeScale()
        {
            if (!ownsTimePause) return;
            Time.timeScale = previousTimeScale;
            ownsTimePause = false;
        }

        // EN: Releases the owned global pause when the controller is disabled.
        // VI: Giải phóng tạm dừng toàn cục do controller giữ khi bị tắt.
        private void OnDisable()
        {
            RestoreTimeScale();
        }

        // EN: Reloads the current configuration with a clean board, belt and customer queue.
        // VI: Nạp lại cấu hình hiện tại với board, băng chuyền và hàng khách được đặt lại.
        public void ReplayLevel()
        {
            LoadLevel(levelConfig);
        }

        // EN: Advances only from a won level to its linked successor, reusing the complete level reset flow.
        // VI: Chỉ chuyển từ màn đã thắng sang màn kế tiếp được liên kết, dùng lại toàn bộ luồng đặt lại màn.
        public void NextLevel()
        {
            if (State != LevelState.Won || levelConfig == null || levelConfig.nextLevel == null)
                return;
            LoadLevel(levelConfig.nextLevel);
        }

        // EN: Delegates victory and defeat presentation to UIManager with the matching gameplay action.
        // VI: Giao hiển thị chiến thắng và thất bại cho UIManager cùng hành động gameplay tương ứng.
        private void ShowResult(LevelState result)
        {
            if (!showResultPanel || !ResolveUIManager()) return;
            if (result == LevelState.Won)
                uiManager.ShowWin(NextLevel, levelConfig != null && levelConfig.nextLevel != null);
            else uiManager.ShowLose(ReplayLevel);
        }

        // EN: Resolves the existing scene UIManager without creating manager objects beneath the level controller.
        // VI: Lấy UIManager có sẵn trong scene mà không tạo object quản lý dưới controller màn chơi.
        private bool ResolveUIManager()
        {
            if (uiManager == null) uiManager = UIManager.Instance;
            if (uiManager == null || !uiManager.isActiveAndEnabled)
            {
                Debug.LogError("[LevelController] Chưa có UIManager hoạt động trong scene.", this);
                return false;
            }
            return true;
        }
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

        // EN: Hides popups and cancels Clean and Pickup before resetting the board and customers.
        // VI: Ẩn popup và hủy Clean cùng Pickup trước khi đặt lại board và khách.
        public void LoadLevel(LevelConfig config)
        {
            if (config == null)
            {
                Debug.LogWarning("[LevelController] Chưa gán LevelConfig.", this);
                return;
            }
            levelConfig = config;
            if (ResolveUIManager()) uiManager.HideAll();
            RestoreTimeScale();
            State = LevelState.NotLoaded;
            if (boardManager == null) boardManager = FoodBoardManager.Instance;
            if (customerManager == null) customerManager = CustomerManager.Instance;
            if (BoosterController.Instance != null)
            {
                BoosterController.Instance.CancelCleanCollection();
                BoosterController.Instance.CancelPickupForReplay();
            }
            if (ChefManager.Instance != null)
            {
                ChefManager.Instance.CancelPreparation();
                foreach (ConveyorSlot slot in ChefManager.Instance.Slots)
                    if (slot != null) slot.ClearFood();
            }
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
            if (boardManager != null && customerManager != null)
            {
                State = LevelState.Playing;
                StateChanged?.Invoke(State);
            }
        }
    }
}
