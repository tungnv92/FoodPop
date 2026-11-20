using GamePackages;
using Lofelt.NiceVibrations;
using System;
using System.Collections;
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
        private Coroutine winRoutine;
        private PlayerDataManager PlayerDataManager;
        private IRecordTracking RecordManager;
        DateTime dStartGame, dEndGame;

        // EN: Waits for verified Internet and profile startup, then caches core managers and optional tracking and loads the saved level with retry-dependent difficulty.
        // VI: Chờ Internet đã xác thực và hồ sơ khởi tạo xong, rồi cache manager core, thống kê tùy chọn và nạp màn đã lưu theo độ khó ứng với số lần chơi lại.
        private IEnumerator Start()
        {
            yield return new WaitUntil(() => ProfileManager.instance.IsReady);
            PlayerDataManager = ProfileManager.instance.PlayerDataManager;
            RecordManager = ProfileManager.instance.Features.Get<IRecordTracking>();
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

        // EN: Cancels a pending win presentation and releases the owned global pause and the pre-win speed-up when the controller is disabled.
        // VI: Hủy phần trình diễn thắng đang chờ và giải phóng tạm dừng toàn cục cùng mức tăng tốc pre-win khi controller bị tắt.
        private void OnDisable()
        {
            StopWinPresentation();
            RestoreTimeScale();
            ExitPreWin();
        }

        // EN: Resolves retry difficulty and resets gameplay, cancelling a pending win presentation and any running booster, unlocking board taps and the settings pause, and restoring plates and chef animation after a result. Unless behindCurtain is set, every popup is hidden and the gameplay pause is released; behind the WinPopup curtain both stay until StartNextLevel.
        // VI: Chọn độ khó theo lần chơi lại và đặt lại gameplay, hủy phần trình diễn thắng đang chờ và booster đang chạy, mở lại chạm board và nút tạm dừng cài đặt, khôi phục đĩa và animation đầu bếp sau khi có kết quả. Nếu không bật behindCurtain thì ẩn mọi popup và bỏ tạm dừng gameplay; khi nạp sau rèm WinPopup thì giữ cả hai tới StartNextLevel.
        public void LoadLevel(LevelConfig config, bool behindCurtain = false)
        {
            StopWinPresentation();
            if (config != null && Application.isPlaying)
                config = config.ForRetry(RecordManager?.GetRetryCount(config.levelNumber) ?? -1);
            if (config == null)
            {
                Debug.LogWarning("[LevelController] Không tìm thấy LevelConfig cần nạp.", this);
                return;
            }
            levelConfig = config;
            nextLevelConfig = null;
            if (ResolveUIManager())
            {
                if (!behindCurtain) uiManager.HideAll();
                uiManager.PauseLocked = false;
            }
            if (!behindCurtain) RestoreTimeScale();
            ExitPreWin();
            State = LevelState.NotLoaded;
            if (boardManager == null) boardManager = FoodBoardManager.Instance;
            if (customerManager == null) customerManager = CustomerManager.Instance;
            if (BoosterController.Instance != null)
                BoosterController.Instance.CancelForReplay();
            if (ChefManager.Instance != null)
            {
                ChefManager.Instance.CancelPreparation();
                if (Application.isPlaying) ChefManager.Instance.ResetForLevel();
                foreach (ConveyorSlot slot in ChefManager.Instance.Slots)
                    if (slot != null) slot.ClearFood();
            }
            FoodHoldingStorage storage = FindFirstObjectByType<FoodHoldingStorage>();
            if (storage != null) storage.ResetForLevel();
            if (boardManager != null)
            {
                boardManager.ResultLocked = false;
                boardManager.LoadIngredientBoard(config.ingredientBoard, config.dishLinks);
            }
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

        // EN: Tracks the abandoned attempt (resets the win streak without counting a retry) and reloads the current level.
        // VI: Ghi nhận lượt chơi bị bỏ dở (reset chuỗi thắng nhưng không tính lần chơi lại) rồi nạp lại màn hiện tại.
        public void ReplayLevel()
        {
            TrackingLose(LoseType.Replay);
            LoadLevel(levelConfig);
            //SDKBridge.ShowInter(placementBack, InterPlacement.BackHome, LevelIndex);
        }

        // EN: Tracks leaving the level for Home as an abandoned attempt; CanvasPause loads the Home scene afterwards.
        // VI: Ghi nhận việc rời màn về Home là lượt chơi bị bỏ dở; CanvasPause tự nạp scene Home sau đó.
        public void BackHome()
        {
            TrackingLose(LoseType.BackHome);
            //SDKBridge.ShowInter(placementBack, InterPlacement.BackHome, LevelIndex);
        }

        // EN: Advances only from a won level to the next numbered level loaded from Resources when the win was shown, reusing the complete level reset flow behind the closed WinPopup curtain: the popup stays open and gameplay stays paused until StartNextLevel.
        // VI: Chỉ chuyển từ màn đã thắng sang màn có số kế tiếp đã nạp từ Resources lúc hiện popup thắng, dùng lại toàn bộ luồng đặt lại màn phía sau rèm WinPopup đang đóng: popup vẫn mở và gameplay vẫn tạm dừng tới StartNextLevel.
        public void NextLevel()
        {
            if (State != LevelState.Won || nextLevelConfig == null)
                return;
            LevelConfig next = nextLevelConfig;
            nextLevelConfig = null;
            LoadLevel(next, true);
            Resources.UnloadUnusedAssets();
            //SDKBridge.ShowInter(GamePlacement.EndGame, InterPlacement.Win, LevelIndex);
            //SDKBridge.TrackUserProperty(UserProperty.Level, LevelIndex.ToString());
        }

        // EN: Starts the level loaded by NextLevel once the WinPopup curtain has fully opened, releasing the pause kept during the transition.
        // VI: Bắt đầu màn do NextLevel nạp khi rèm WinPopup đã mở hẳn, bỏ tạm dừng được giữ trong lúc chuyển cảnh.
        public void StartNextLevel()
        {
            RestoreTimeScale();
        }

        // EN: Plays an emphasis vibration when the player has vibration enabled.
        // VI: Phát rung nhấn mạnh khi người chơi đang bật rung.
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

        // EN: Publishes the result and locks board taps at once; defeat pauses and shows at once, victory restores the pre-win speed, locks the settings pause so Replay/Home cannot interrupt, hides the conveyor plates immediately and defers its presentation.
        // VI: Phát kết quả và khóa chạm board ngay; thua thì tạm dừng và hiện ngay, thắng thì trả tốc độ pre-win về bình thường, khóa nút tạm dừng cài đặt để không bấm Replay/Home chen ngang, ẩn ngay các đĩa băng chuyền rồi mới trình diễn sau.
        private void FinishLevel(LevelState result)
        {
            if (State != LevelState.Playing && !(State == LevelState.ReviveOffered && result == LevelState.Lost)) return;
            State = result;
            boardManager.ResultLocked = true;
            if (result == LevelState.Won)
            {
                ExitPreWin();
                uiManager.PauseLocked = true;
                ChefManager.Instance.HideDishes();
                StateChanged?.Invoke(State);
                winRoutine = StartCoroutine(PresentWin());
                return;
            }
            PauseGameplay();
            ShowResult(result);
            StateChanged?.Invoke(State);
            onLost.Invoke();
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

        // EN: Stops the delayed win presentation if it has not started yet.
        // VI: Dừng phần trình diễn thắng bị trì hoãn nếu nó chưa bắt đầu.
        private void StopWinPresentation()
        {
            if (winRoutine != null)
                StopCoroutine(winRoutine);
            winRoutine = null;
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

        // EN: Waits until every customer has walked out of the camera view plus the configured delay at normal speed, then pauses, plays the chef's unscaled celebration and shows the win panel.
        // VI: Chờ tới khi mọi khách đã đi ra khỏi khung camera cộng thêm khoảng trễ cấu hình ở tốc độ thường, rồi tạm dừng, phát animation thắng không phụ thuộc timeScale của đầu bếp và hiện bảng thắng.
        private IEnumerator PresentWin()
        {
            yield return new WaitUntil(() => customerManager.AllCustomersGone);
            yield return new WaitForSeconds(IngameConfig.Instance.preWin.winPanelDelay);
            winRoutine = null;
            PauseGameplay();
            ChefManager.Instance.PlayWin();
            ShowResult(LevelState.Won);
            onWon.Invoke();
        }

        // EN: Delegates victory and defeat presentation to UIManager with the matching gameplay action; on victory it loads the next numbered level from Resources and continues to it only below winReward.levelBackHome (from that level on the player returns Home instead).
        // VI: Giao hiển thị chiến thắng và thất bại cho UIManager cùng hành động gameplay tương ứng; khi thắng thì nạp level số kế tiếp từ Resources và chỉ chơi tiếp khi level hiện tại nhỏ hơn winReward.levelBackHome (từ level đó trở đi quay về Home).
        private void ShowResult(LevelState result)
        {
            if (!showResultPanel || !ResolveUIManager()) return;
            if (result == LevelState.Won)
            {
                nextLevelConfig = LevelConfig.Load(LevelIndex + 1);
                bool continueToNext = nextLevelConfig != null && LevelIndex < IngameConfig.Instance.winReward.levelBackHome;
                uiManager.ShowWin(NextLevel, StartNextLevel, continueToNext);
                TrackingWin();
            } else
            {
                uiManager.ShowLose(RetryLevel);
                TrackingLose(LoseType.LoseGame);
            }
        }

        // EN: Reloads the current level from the lose popup; the defeat was already tracked when the popup opened.
        // VI: Nạp lại màn hiện tại từ popup thua; lần thua đã được ghi nhận khi popup mở.
        private void RetryLevel()
        {
            LoadLevel(levelConfig);
            //SDKBridge.ShowInter(placementBack, InterPlacement.BackHome, LevelIndex);
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

        #region Tracking

        // EN: Collects lose analytics and resets the win streak through optional IRecordTracking for every lose type; only a real defeat counts as a retry that drives the Easy variant.
        // VI: Thu thập số liệu thua và reset chuỗi thắng cho mọi kiểu thua; chỉ thua thật mới tính là một lần chơi lại để chọn biến thể Easy.
        private void TrackingLose(LoseType loseType)
        {
            dEndGame = DateTime.Now;
            int duration_fail = (int)(dEndGame - dStartGame).TotalSeconds;
            int remaining_heart = ProfileManager.instance.Features.Get<IHeart>()?.GetRemainHeart() ?? -1;
            int remain_coin = PlayerDataManager.GetResourceValue(ResourceType.Coin);
            int booster_pickup = PlayerDataManager.GetResourceValue(ResourceType.BoosterPickup);
            int booster_clean = PlayerDataManager.GetResourceValue(ResourceType.BoosterClean);
            int booster_shuffle = PlayerDataManager.GetResourceValue(ResourceType.BoosterShuffle);
            int booster_autocomplete = PlayerDataManager.GetResourceValue(ResourceType.BoosterAutoComplete);
            //SDKBridge.TrackEventLose(LevelIndex, remaining_heart, duration_fail, remain_coin, booster_pickup, booster_shuffle, booster_autocomplete, loseType.ToString());
            RecordManager?.ResetWinStreak();
            if (loseType == LoseType.LoseGame)
                RecordManager?.RecordRetry();
        }

        // EN: Records the win through optional IRecordTracking (win streak, checkpoint outside the Editor), advances saved progress and clears the module’s per-level retry and spend counters.
        // VI: Ghi nhận thắng qua IRecordTracking tùy chọn (chuỗi thắng, checkpoint ngoài Editor), tăng tiến trình đã lưu và xóa bộ đếm thử lại cùng chi tiêu theo màn của module.
        private void TrackingWin()
        {
            dEndGame = DateTime.Now;
            int timePlay = (int)(dEndGame - dStartGame).TotalSeconds;
            Observer.OnGameWin?.Invoke();
            RecordManager?.RecordWinStreak();
            if (!Application.isEditor)
            {
                int duration_win = timePlay;
                int winstreak = RecordManager?.GetWinStreak() ?? 0;
                int remainCoin = PlayerDataManager.GetResourceValue(ResourceType.Coin);
                int booster_pickup = PlayerDataManager.GetResourceValue(ResourceType.BoosterPickup);
                int booster_clean = PlayerDataManager.GetResourceValue(ResourceType.BoosterClean);
                int booster_shuffle = PlayerDataManager.GetResourceValue(ResourceType.BoosterShuffle);
                int booster_autocomplete = PlayerDataManager.GetResourceValue(ResourceType.BoosterAutoComplete);
                int retryCount = RecordManager?.GetRetryCount(LevelIndex) ?? -1;
                //SDKBridge.TrackEventComplete(LevelIndex, retryCount, winstreak, duration_win, remainCoin, booster_pickup, booster_shuffle, booster_super);
                RecordManager?.RecordCheckPointEnd(LevelIndex, duration_win);
            }
            PlayerDataManager.IncreaseLevel();
            RecordManager?.ResetWin();
        }

        #endregion

    }
}
