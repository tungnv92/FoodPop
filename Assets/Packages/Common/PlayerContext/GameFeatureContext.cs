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
        // EN: Wires feature managers to private profile data supplied only during internal initialization.
        // VI: Gắn manager tính năng với dữ liệu hồ sơ riêng được truyền trong khởi tạo nội bộ.
        internal GameFeatureContext(ProfileManager context, PlayerData data, SaveService saveService)
        {
            DailyGiftManager.DailyGiftGlobalConfig = context.DailyGiftRewardConfig;
            DailyRewardManager.DailyRewardGlobalConfig = context.DailyRewardGlobalConfig;
            WinStreakManager.WinStreakGlobalConfig = context.WinStreakGlobalConfig;
            LavaQuestManager.LavaQuestConfig = context.LavaQuestConfig;
            MissionControlManager.MissionControlConfig = context.MissionControlConfig;
            TowerRiseManager.TowerRiseConfig = context.TowerRiseConfig;

            RegistryFeatureContext();
            RegistryExtraFeatureContext(context, saveService);

            DailyGiftManager.SetContext(data.DailyGiftData);
            DailyRewardManager.SetContext(data.DailyRewardData);
            RecordManager.SetContext(data.RecordTrackingData);
            HeartManager.SetContext(data.HeartData);
            WinStreakManager.SetContext(data.WinStreakData);
            LavaQuestManager.SetContext(data.LavaQuestData);
            MissionControlManager.SetContext(data.MissionControlData);
            TowerRiseManager.SetContext(data.TowerRiseData);
        }
        /// <summary>
        /// Registry All Feature
        /// </summary>
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
        }
        /// <summary>
        /// Registry Extra Context (Time, Data)
        /// </summary>
        /// <param name="context"></param>
        // EN: Injects the supplied save service and shared managers without reading a public player model.
        // VI: Gắn dịch vụ lưu được truyền vào cùng manager dùng chung mà không đọc model người chơi public.
        void RegistryExtraFeatureContext(ProfileManager context, SaveService saveService)
        {
            l_FeatureTime.ForEach(x => x.SetTimeManager(context.TimeManager));
            l_FeatureSave.ForEach(x => x.SetSaveService(saveService));
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