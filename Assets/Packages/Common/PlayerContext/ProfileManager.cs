using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
namespace GamePackages
{
    public class ProfileManager : Singleton<ProfileManager>
    {
        /// <summary>
        /// Module features registered from FeatureModule assets.
        /// </summary>
        public FeatureRegistry Features => featureRegistry;
        // EN: Exposes the persistent core Internet gate to startup and UI flows.
        // VI: Cung cấp màn chặn Internet core xuyên scene cho luồng khởi tạo và UI.
        public InternetConnection Connection { get; private set; }
        // EN: Allows gameplay only after profile startup and an active verified connection, or cheat.
        // VI: Chỉ cho gameplay chạy sau khởi tạo hồ sơ và có kết nối đã xác thực, hoặc cheat.
        public bool IsReady => startupComplete && Connection.IsReady;
        /// <summary>
        ///  Save Manager, Save Game Data
        /// </summary>
        [SerializeField] private SaveService SaveService;
        /// <summary>
        /// Game Data
        /// </summary>
        [SerializeField] private PlayerData PlayerData;
        /// <summary>
        /// Time Manager
        /// </summary>
        public TimeManager TimeManager = new();
        /// <summary>
        /// PlayerDataManager
        /// </summary>
        public PlayerDataManager PlayerDataManager = new();
        /// <summary>
        /// Feature modules (new system)
        /// </summary>
        [SerializeField] private FeatureRegistry featureRegistry = new();
        public RemoteConfigBase RemoteConfigBase;
        public static bool isCheckingUpdate = false;
        private bool startupComplete;

        // EN: Loads core data and creates the persistent Internet gate after registering the profile singleton.
        // VI: Nạp dữ liệu core và tạo màn chặn Internet xuyên scene sau khi đăng ký singleton hồ sơ.
        protected override void Awake()
        {
            base.Awake();
            if (!isAwakable) return;
            InitializeProfile();
        }
        // EN: Waits until time verification has handled the day, or cheat is enabled, then catches up features, applies the session and tracking, and saves before play.
        // VI: Chờ xác thực giờ đã xử lý ngày hoặc bật cheat, rồi cập nhật bù tính năng, áp dụng phiên chơi và tracking, lưu trước khi cho chơi.
        private IEnumerator Start()
        {
            yield return new WaitUntil(() => Connection.IsReady);
            featureRegistry.UpdateFeatures();
            ApplySessionChanges();
            featureRegistry.TrackAppOpen();
            SaveData();
            startupComplete = true;
        }

        // EN: Advances registered features only after startup and while the Internet gate permits play.
        // VI: Chỉ cập nhật các tính năng đã đăng ký sau khởi tạo và khi màn chặn Internet cho phép chơi.
        private void Update()
        {
            if (!IsReady) return;
            TimeManager.CheckForNewDay();
            featureRegistry.UpdateFeatures();
        }

        // EN: Saves the current profile and trusted clock when entering the background.
        // VI: Lưu hồ sơ hiện tại và đồng hồ đáng tin khi ứng dụng vào nền.
        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveData();
        }

        // EN: Saves profile and trusted clock when the application quits.
        // VI: Lưu hồ sơ và đồng hồ đáng tin khi ứng dụng thoát.
        private void OnApplicationQuit()
        {
            SaveData();
        }

        // EN: Reads the saved current level directly from core player data.
        // VI: Đọc level hiện tại đã lưu trực tiếp từ dữ liệu người chơi core.
        public int GetCurrentLevel()
        {
            return PlayerData.currentLevel;
        }
        // EN: Changes the level through the player manager so progress is saved and announced.
        // VI: Đổi level qua manager người chơi để lưu và thông báo tiến trình.
        public void SetCurrentLevel(int level)
        {
            PlayerDataManager.SetCurrentLevel(level);
        }
        // EN: Saves core and module data together with the trusted clock.
        // VI: Lưu dữ liệu core, module và đồng hồ đáng tin.
        [Button]
        public void SaveData()
        {
            SaveService.SaveData();
            TimeManager.SaveData();
        }
        // EN: Deletes persisted player, feature and clock keys without resetting the active in-memory profile.
        // VI: Xóa khóa lưu người chơi, tính năng và đồng hồ, không reset hồ sơ đang giữ trong bộ nhớ.
        [Button]
        public void DeleteSavedData()
        {
            TimeManager.ClearData();
            PlayerPrefs.DeleteAll();
        }

        // EN: Coordinates core restoration, feature initialization and day subscription before starting Internet verification.
        // VI: Điều phối khôi phục core, khởi tạo tính năng và đăng ký ngày mới trước khi bắt đầu xác thực Internet.
        private void InitializeProfile()
        {
            RestoreCoreData();
            featureRegistry.InitializeFeatures(new FeatureServices(SaveService, PlayerDataManager, TimeManager, RemoteConfigBase));
            TimeManager.RegisterNewDayCallback("Newday", HandleNewDay);
            Connection = Instantiate(Resources.Load<InternetConnection>("InternetConnection"), transform);
            Connection.Initialize(TimeManager);
        }

        // EN: Restores core player and clock data, then binds the player service before any feature is created.
        // VI: Khôi phục dữ liệu người chơi core và đồng hồ rồi gắn dịch vụ người chơi trước khi tạo tính năng.
        private void RestoreCoreData()
        {
            SaveService ??= new SaveService();
            PlayerData = SaveService.Load();
            SaveService = new SaveService(PlayerData);
            TimeManager.LoadData();
            PlayerDataManager.SetSaveService(SaveService);
            PlayerDataManager.SetContext(PlayerData);
        }

        // EN: Resets core and all feature day state, then saves the complete profile before TimeManager persists the new-day boundary.
        // VI: Reset trạng thái ngày của core và mọi tính năng rồi lưu toàn bộ hồ sơ trước khi TimeManager lưu mốc ngày mới.
        private void HandleNewDay()
        {
            Debug.Log("OnNewDay");
            featureRegistry.OnNewDay();
            SaveService.SaveData();
        }

        // EN: Applies first-open rewards and interrupted-level recovery without committing the startup save.
        // VI: Áp dụng quà lần đầu và xử lý màn bị bỏ dở, không tự ghi lần lưu khởi động.
        private void ApplySessionChanges()
        {
            GrantFirstOpenGift();
            RecoverInterruptedLevel();
        }

        // EN: Consumes the first-open flag and grants thirty minutes of unlimited lives before the shared startup save.
        // VI: Tắt cờ lần mở đầu và cộng ba mươi phút mạng vô hạn trước lần lưu chung lúc khởi động.
        private void GrantFirstOpenGift()
        {
            if (!PlayerData.isFirstAppOpen) return;
            PlayerData.isFirstAppOpen = false;
            Features.Get<IHeart>()?.AddUnlimitedLiveTime(30);
        }

        // EN: Clears an abandoned level, spends a heart, resets its win streak and notifies features without increasing retries.
        // VI: Xóa trạng thái màn bỏ dở, trừ mạng, reset chuỗi thắng và báo tính năng mà không tăng retry.
        private void RecoverInterruptedLevel()
        {
            if (!PlayerData.isPlaying) return;
            PlayerData.isPlaying = false;
            Features.Get<IHeart>()?.UseHeart();
            Features.Get<IRecordTracking>()?.ResetWinStreak();
            featureRegistry.OnLevelLose(null);
        }
    }
}
