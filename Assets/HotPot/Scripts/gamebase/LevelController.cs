using GamePackages;
using Lofelt.NiceVibrations;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FoodPop
{
    // EN: Coordinates level loading: hands the ingredient board to FoodBoardManager and the customer queues to CustomerManager.
    // VI: Điều phối nạp level: giao bảng nguyên liệu cho FoodBoardManager và hàng khách cho CustomerManager.
    public class LevelController : MonoBehaviour
    {
        public enum LevelState { NotLoaded, Playing, Won, Lost, ReviveOffered, Reviving }
        public enum LoseType { LoseGame, Replay, BackHome }
        public LevelState State { get; private set; }
        public event Action<LevelState> StateChanged;
        public event Action<LevelConfig> LevelLoaded;

        // EN: Exposes the active level configuration to event-driven HUD displays.
        // VI: Cung cấp cấu hình màn hiện tại cho HUD cập nhật theo sự kiện.
        public LevelConfig CurrentLevel => levelConfig;

        // EN: Reads the active level number for progression and tracking.
        // VI: Lấy số màn hiện tại để xử lý tiến độ và thống kê.
        private int LevelIndex => CurrentLevel.levelNumber;

        [Tooltip("Manager bảng nguyên liệu của scene.")]
        [SerializeField] private FoodBoardManager boardManager;
        [Tooltip("Manager khách hàng và order của scene.")]
        [SerializeField] private CustomerManager customerManager;
        [Tooltip("Manager bật tắt các canvas popup đã đặt trong scene.")]
        [SerializeField] private UIManager uiManager;
        [Tooltip("Hiện bảng kết quả với nút sang màn kế tiếp khi thắng hoặc chơi lại khi thua.")]
        [SerializeField] private bool showResultPanel = true;
        [SerializeField] private UnityEvent onWon = new UnityEvent();
        [SerializeField] private UnityEvent onLost = new UnityEvent();
        private LevelConfig levelConfig;
        private LevelConfig nextLevelConfig;
        private float previousTimeScale = 1f;
        private bool ownsTimePause;
        private CanvasRevive.ReviveCase reviveCase;
        private bool preWinActive;
        private float preWinBaseTimeScale = 1f;
        private bool isQuitLoseLife;
        private PlayerDataManager PlayerDataManager;
        private RecordTrackingManager RecordManager;
        DateTime dStartGame, dEndGame;

        // EN: Caches initialized profile managers and loads the saved level with its retry-dependent difficulty.
        // VI: Cache manager hồ sơ đã khởi tạo và nạp màn đã lưu theo độ khó ứng với số lần chơi lại.
        private void Start()
        {
            PlayerDataManager = ProfileManager.instance.PlayerDataManager;
            RecordManager = ProfileManager.instance.GameFeatureContext.RecordManager;
            LoadLevel(LoadSavedLevel());
        }

        // EN: Checks victory, serving timeout and deadlock in priority order, and recovers interrupted cleaning.
        // VI: Kiểm tra thắng, hết giờ phục vụ và bế tắc theo thứ tự ưu tiên, đồng thời xử lý lượt dọn bị ngắt.
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
            else if (customerManager.ServeTimeExpired)
                OfferRevive(CanvasRevive.ReviveCase.OutOfTime);
            else if (IsConveyorBlocked())
                OfferRevive();
            else if (!preWinActive && IsPreWin())
                EnterPreWin();
        }

        // EN: Releases the owned global pause and the pre-win speed-up when the controller is disabled.
        // VI: Giải phóng tạm dừng toàn cục và mức tăng tốc pre-win khi controller bị tắt.
        private void OnDisable()
        {
            RestoreTimeScale();
            ExitPreWin();
        }

        // EN: Resolves authored Hard levels against the saved retry count before resetting gameplay; Edit Mode does not read the runtime profile.
        // VI: Chọn bản màn Hard theo số lần chơi lại đã lưu trước khi đặt lại gameplay; Edit Mode không đọc hồ sơ runtime.
        public void LoadLevel(LevelConfig config)
        {
            if (config != null && Application.isPlaying)
                config = config.ForRetry(RecordManager.GetRetryCount(config.levelNumber));
            if (config == null)
            {
                Debug.LogWarning("[LevelController] Không tìm thấy LevelConfig cần nạp.", this);
                return;
            }
            levelConfig = config;
            nextLevelConfig = null;
            if (ResolveUIManager()) uiManager.HideAll();
            RestoreTimeScale();
            ExitPreWin();
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
                LevelLoaded?.Invoke(config);
                StateChanged?.Invoke(State);
            }
            dStartGame = DateTime.Now;
        }

        // EN: Records the replay before loading so the second retry immediately selects the Easy variant of an authored Hard level.
        // VI: Ghi nhận chơi lại trước khi nạp để lần chơi lại thứ hai chọn ngay biến thể Easy của màn được cấu hình Hard.
        public void ReplayLevel()
        {
            TrackingLose(LoseType.Replay);
            LoadLevel(levelConfig);
            //SDKBridge.ShowInter(placementBack, InterPlacement.BackHome, LevelIndex);
        }

        public void BackHome()
        {
            TrackingLose(LoseType.BackHome);
            //SDKBridge.ShowInter(placementBack, InterPlacement.BackHome, LevelIndex);
        }

        // EN: Advances only from a won level to the next numbered level loaded from Resources when the win was shown, reusing the complete level reset flow.
        // VI: Chỉ chuyển từ màn đã thắng sang màn có số kế tiếp đã nạp từ Resources lúc hiện popup thắng, dùng lại toàn bộ luồng đặt lại màn.
        public void NextLevel()
        {
            if (State != LevelState.Won || nextLevelConfig == null)
                return;
            LevelConfig next = nextLevelConfig;
            nextLevelConfig = null;
            LoadLevel(next);
            Resources.UnloadUnusedAssets();
            //SDKBridge.ShowInter(GamePlacement.EndGame, InterPlacement.Win, LevelIndex);
            //SDKBridge.TrackUserProperty(UserProperty.Level, LevelIndex.ToString());
        }

        // EN: Adds thirty seconds to expired customers for timeout revival, otherwise starts conveyor cleaning.
        // VI: Cộng ba mươi giây cho khách hết giờ khi hồi sinh thời gian, trường hợp khác bắt đầu dọn băng chuyền.
        public void ReviveLevel()
        {
            if (State != LevelState.ReviveOffered) return;
            if (reviveCase == CanvasRevive.ReviveCase.OutOfTime)
            {
                customerManager.ResetExpiredServeTimers();
                if (ResolveUIManager()) uiManager.HideAll();
                RestoreTimeScale();
                State = LevelState.Playing;
                StateChanged?.Invoke(State);
                return;
            }
            BoosterController boosters = BoosterController.Instance;
            if (boosters == null || !boosters.CanCleanForRevive()) return;
            State = LevelState.Reviving;
            if (ResolveUIManager()) uiManager.HideAll();
            RestoreTimeScale();
            StateChanged?.Invoke(State);
            if (!boosters.TryCleanForRevive(CompleteRevive)) CompleteRevive(false);
        }

        // EN: Confirms defeat only when the player refuses the pending revive offer.
        // VI: Chỉ xác nhận thua khi người chơi từ chối lời mời hồi sinh đang mở.
        public void DeclineRevive()
        {
            if (State == LevelState.ReviveOffered) FinishLevel(LevelState.Lost);
        }

        public void PlayHaptic()
        {
            if (PlayerDataManager.isVibrateOn) HapticPatterns.PlayEmphasis(0.8f, 0.5f);
        }

        // EN: Loads only the Resources level matching the saved PlayerData.currentLevel; past the last existing level it steps back to the highest level that exists.
        // VI: Chỉ nạp level trong Resources đúng với PlayerData.currentLevel đã lưu; nếu vượt quá level cuối thì lùi về level cao nhất còn tồn tại.
        private LevelConfig LoadSavedLevel()
        {
            LevelConfig config = null;
            for (int number = PlayerDataManager.GetCurrentLevel(); config == null && number >= 1; number--)
                config = LevelConfig.Load(number);
            return config;
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

        // EN: Pauses and offers the matching timeout or conveyor revive without resetting progress.
        // VI: Tạm dừng và mời hồi sinh đúng trường hợp hết giờ hoặc kẹt băng, giữ tiến độ hiện tại.
        private void OfferRevive(CanvasRevive.ReviveCase reason = CanvasRevive.ReviveCase.OutOfSlots)
        {
            reviveCase = reason;
            State = LevelState.ReviveOffered;
            PauseGameplay();
            BoosterController boosters = BoosterController.Instance;
            if (ResolveUIManager())
                uiManager.ShowRevive(ReviveLevel, DeclineRevive,
                    reason == CanvasRevive.ReviveCase.OutOfTime || (boosters != null && boosters.CanCleanForRevive()), reason: reason);
            StateChanged?.Invoke(State);
        }

        // EN: Declare deadlock only after all work settles, no belt food can serve and no exposed group fits the shared pick rule.
        // VI: Chỉ xác định bế tắc khi mọi việc ổn định, không món trên băng phục vụ được và không nhóm lộ nào thỏa luật lấy chung.
        private bool IsConveyorBlocked()
        {
            ChefManager chef = ChefManager.Instance;
            if (chef == null || chef.IsBusy || chef.BoosterPaused || customerManager.BoosterPaused ||
                boardManager == null || boardManager.InputLocked || boardManager.IsAnimating ||
                customerManager.HasPendingService() ||
                (BoosterController.Instance != null && BoosterController.Instance.IsRunning))
                return false;
            int occupied = 0;
            foreach (ConveyorSlot slot in chef.Slots)
            {
                if (slot == null || !slot.isActiveAndEnabled) continue;
                if (slot.IsFoodArriving) return false;
                if (slot.isEmpty) continue;
                if (slot.food == null) return false;
                if (customerManager.TryFindCustomer(slot.food.foodType))
                    return false;
                occupied++;
            }
            return occupied > 0 && !boardManager.HasAvailablePick(chef, customerManager);
        }

        // EN: Enter pre-win when all remaining board portions fit into the empty belt slots, regardless of customer count.
        // VI: Vào pre-win khi mọi phần món còn trên board vừa số slot băng trống, không phụ thuộc số khách.
        private bool IsPreWin()
        {
            ChefManager chef = ChefManager.Instance;
            if (chef == null || boardManager == null)
                return false;
            return CountBoardPortions() <= chef.CountEmptySlots();
        }

        // EN: Speeds the whole game up by the configured pre-win multiplier, remembering the speed it started from, and logs the trigger values.
        // VI: Tăng tốc toàn bộ game theo hệ số pre-win đã cấu hình, ghi nhớ tốc độ trước khi tăng và log các giá trị kích hoạt.
        private void EnterPreWin()
        {
            preWinActive = true;
            preWinBaseTimeScale = Time.timeScale;
            Time.timeScale = preWinBaseTimeScale * IngameConfig.Instance.preWin.speedMultiplier;
            Debug.Log($"[LevelController] Vào pre-win ở level {LevelIndex}: còn {CountBoardPortions()} phần trên board, {ChefManager.Instance.CountEmptySlots()} slot trống; tốc độ {preWinBaseTimeScale:0.##} -> {Time.timeScale:0.##}.", this);
        }

        // EN: Restores only the time pause owned by this controller when reloading or leaving the scene.
        // VI: Chỉ khôi phục tạm dừng thời gian do controller này giữ khi nạp lại hoặc rời scene.
        private void RestoreTimeScale()
        {
            if (!ownsTimePause) return;
            Time.timeScale = previousTimeScale;
            ownsTimePause = false;
        }

        // EN: Returns to the speed recorded when pre-win started and logs it; does nothing outside pre-win.
        // VI: Trả về tốc độ đã ghi lúc bắt đầu pre-win và log lại; không làm gì khi không ở pre-win.
        private void ExitPreWin()
        {
            if (!preWinActive)
                return;
            preWinActive = false;
            Time.timeScale = preWinBaseTimeScale;
            Debug.Log($"[LevelController] Thoát pre-win, trả tốc độ về {preWinBaseTimeScale:0.##}.", this);
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

        private void TrackingLose(LoseType loseType)
        {
            dEndGame = DateTime.Now;
            int duration_fail = (int)(dEndGame - dStartGame).TotalSeconds;
            int remaining_heart = ProfileManager.instance.GameFeatureContext.HeartManager.GetRemainHeart();
            int remain_coin = PlayerDataManager.GetResourceValue(ResourceType.Coin);
            int booster_pickup = PlayerDataManager.GetResourceValue(ResourceType.BoosterPickup);
            int booster_clean = PlayerDataManager.GetResourceValue(ResourceType.BoosterClean);
            int booster_shuffle = PlayerDataManager.GetResourceValue(ResourceType.BoosterShuffle);
            int booster_autocomplete = PlayerDataManager.GetResourceValue(ResourceType.BoosterAutoComplete);
            //SDKBridge.TrackEventLose(LevelIndex, remaining_heart, duration_fail, remain_coin, booster_pickup, booster_shuffle, booster_autocomplete, loseType.ToString());
            RecordManager.ResetWinStreak();
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

        // EN: Delegates victory and defeat presentation to UIManager with the matching gameplay action; on victory it loads the next numbered level from Resources to enable Next.
        // VI: Giao hiển thị chiến thắng và thất bại cho UIManager cùng hành động gameplay tương ứng; khi thắng thì nạp level số kế tiếp từ Resources để bật nút Next.
        private void ShowResult(LevelState result)
        {
            if (!showResultPanel || !ResolveUIManager()) return;
            if (result == LevelState.Won)
            {
                nextLevelConfig = LevelConfig.Load(LevelIndex + 1);
                uiManager.ShowWin(NextLevel, nextLevelConfig != null);
                TrackingWin();
            } else
            {
                uiManager.ShowLose(RetryLevel);
                TrackingLose(LoseType.LoseGame);
            }
        }

        // EN: Sums the portions of every dish still on the board, counting a dish that spans two cells only once.
        // VI: Cộng số phần của mọi đĩa còn trên board, đĩa chiếm hai ô chỉ tính một lần.
        private int CountBoardPortions()
        {
            HashSet<Food> counted = new HashSet<Food>();
            int total = 0;
            for (int c = 0; c < boardManager.ColumnCount; c++)
            {
                foreach (Food dish in boardManager.GetColumnDishes(c))
                {
                    if (dish != null && counted.Add(dish))
                        total += dish.TotalAmount;
                }
            }
            return total;
        }

        private void TrackingWin()
        {
            dEndGame = DateTime.Now;
            int timePlay = (int)(dEndGame - dStartGame).TotalSeconds;
            Observer.OnGameWin?.Invoke();
            RecordManager.RecordWinStreak();
            if (!Application.isEditor)
            {
                int duration_win = timePlay;
                int winstreak = RecordManager.GetWinStreak();
                int remainCoin = PlayerDataManager.GetResourceValue(ResourceType.Coin);
                int booster_pickup = PlayerDataManager.GetResourceValue(ResourceType.BoosterPickup);
                int booster_clean = PlayerDataManager.GetResourceValue(ResourceType.BoosterClean);
                int booster_shuffle = PlayerDataManager.GetResourceValue(ResourceType.BoosterShuffle);
                int booster_autocomplete = PlayerDataManager.GetResourceValue(ResourceType.BoosterAutoComplete);
                int retryCount = RecordManager.GetRetryCount(LevelIndex);
                //SDKBridge.TrackEventComplete(LevelIndex, retryCount, winstreak, duration_win, remainCoin, booster_pickup, booster_shuffle, booster_super);
                RecordManager.RecordCheckPointEnd(LevelIndex, duration_win);
            }
            PlayerDataManager.IncreaseLevel();
            RecordManager.ResetWin();
            ResetQuitLoseStreak();
        }

        private void RetryLevel()
        {
            LoadLevel(levelConfig);
            //SDKBridge.ShowInter(placementBack, InterPlacement.BackHome, LevelIndex);
        }

        private void ResetQuitLoseStreak()
        {
            isQuitLoseLife = false;
            PlayerDataManager.isLoseStreakQuit = false;
        }

        private void MarkQuitLoseStreak()
        {
            isQuitLoseLife = true;
            PlayerDataManager.isLoseStreakQuit = true;
        }
    }
}
