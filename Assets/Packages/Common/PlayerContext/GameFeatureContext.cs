using System.Collections.Generic;
namespace GamePackages
{
    [System.Serializable]
    public class GameFeatureContext
    {
        public DailyGiftManager DailyGiftManager = new();
        public DailyRewardManager DailyRewardManager = new();
        public WinStreakManager WinStreakManager = new();
        public RecordTrackingManager RecordManager = new();
        public HeartManager HeartManager = new HeartManager();
        public LavaQuestManager LavaQuestManager = new();
        public MissionControlManager MissionControlManager = new();
        public TowerRiseManager TowerRiseManager = new();
        public CardCollectionManager CardCollectionManager = new();
        List<IFeatureSave> l_FeatureSave = new();
        List<IFeatureUpdate> l_FeatureUpdate = new();
        List<IFeatureNewDay> l_FeatureNewday = new();
        List<IFeatureTrackOpen> l_FeatureTrackingOpenApp = new();
        List<IRequireTime> l_FeatureTime = new();
        List<IRequirePlayerData> l_FeaturePlayerData = new();
        /// <summary>
        /// Init 
        /// </summary>
        /// <param name="context"></param>
        // EN: Initializes all feature managers, binds their configs and save contexts, and registers their lifecycle hooks.
        // VI: Khởi tạo tất cả manager tính năng, gắn cấu hình cùng dữ liệu lưu trữ và đăng ký các hook vòng đời của chúng.
        public GameFeatureContext(ProfileManager context)
        {
            DailyGiftManager.DailyGiftGlobalConfig = context.DailyGiftRewardConfig;
            DailyRewardManager.DailyRewardGlobalConfig = context.DailyRewardGlobalConfig;
            WinStreakManager.WinStreakGlobalConfig = context.WinStreakGlobalConfig;
            LavaQuestManager.LavaQuestConfig = context.LavaQuestConfig;
            MissionControlManager.MissionControlConfig = context.MissionControlConfig;
            TowerRiseManager.TowerRiseConfig = context.TowerRiseConfig;
            CardCollectionManager.CardCollectionConfig = context.CardCollectionConfig;

            RegistryFeatureContext();
            RegistryExtraFeatureContext(context);

            DailyGiftManager.SetContext(context.PlayerData.DailyGiftData);
            DailyRewardManager.SetContext(context.PlayerData.DailyRewardData);
            RecordManager.SetContext(context.PlayerData.RecordTrackingData);
            HeartManager.SetContext(context.PlayerData.HeartData);
            WinStreakManager.SetContext(context.PlayerData.WinStreakData);
            LavaQuestManager.SetContext(context.PlayerData.LavaQuestData);
            MissionControlManager.SetContext(context.PlayerData.MissionControlData);
            TowerRiseManager.SetContext(context.PlayerData.TowerRiseData);
            CardCollectionManager.SetContext(context.PlayerData.CardCollectionData);
        }
        /// <summary>
        /// Registry All Feature
        /// </summary>
        // EN: Registers all feature managers into their corresponding update, new-day, save, and tracking lists.
        // VI: Đăng ký tất cả manager tính năng vào các danh sách cập nhật, ngày mới, lưu trữ và theo dõi tương ứng.
        void RegistryFeatureContext()
        {
            l_FeatureSave.Clear();
            l_FeatureUpdate.Clear();
            l_FeatureNewday.Clear();
            l_FeatureTrackingOpenApp.Clear();
            l_FeatureTime.Clear();
            l_FeaturePlayerData.Clear();
            RegisterFeature(DailyGiftManager);
            RegisterFeature(DailyRewardManager);
            RegisterFeature(RecordManager);
            RegisterFeature(HeartManager);
            RegisterFeature(WinStreakManager);
            RegisterFeature(LavaQuestManager);
            RegisterFeature(MissionControlManager);
            RegisterFeature(TowerRiseManager);
            RegisterFeature(CardCollectionManager);
        }
        /// <summary>
        /// Registry Extra Context (Time, Data)
        /// </summary>
        /// <param name="context"></param>
        void RegistryExtraFeatureContext(ProfileManager context)
        {
            l_FeatureTime.ForEach(x => x.SetTimeManager(context.TimeManager));
            l_FeatureSave.ForEach(x => x.SetSaveService(context.SaveService));
            l_FeaturePlayerData.ForEach(x => x.SetPlayerDataManager(context.PlayerDataManager));
        }
        /// <summary>
        /// Registry and check type of feature
        /// </summary>
        /// <param name="feature"></param>
        void RegisterFeature(object feature)
        {
            if (feature is IFeatureUpdate a)
                l_FeatureUpdate.Add(a);
            if (feature is IFeatureNewDay b)
                l_FeatureNewday.Add(b);
            if (feature is IFeatureTrackOpen c)
                l_FeatureTrackingOpenApp.Add(c);
            if (feature is IRequireTime d)
                l_FeatureTime.Add(d);
            if (feature is IRequirePlayerData e)
                l_FeaturePlayerData.Add(e);
            if (feature is IFeatureSave g)
                l_FeatureSave.Add(g);

        }
        public void Update()
        {
            foreach (var u in l_FeatureUpdate)
                u.OnUpdate();

        }
        public void OnNewDay()
        {
            foreach (var u in l_FeatureNewday)
                u.OnNewDay();
        }
        public void OnTrackingOpenApp()
        {
            foreach (var u in l_FeatureTrackingOpenApp)
                u.OnTrackingOpenApp();
        }
    }
}