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
        public GameFeatureContext(ProfileManager context)
        {
            DailyGiftManager.DailyGiftGlobalConfig = context.DailyGiftRewardConfig;
            DailyRewardManager.DailyRewardGlobalConfig = context.DailyRewardGlobalConfig;
            WinStreakManager.WinStreakGlobalConfig = context.WinStreakGlobalConfig;
            LavaQuestManager.LavaQuestConfig = context.LavaQuestConfig;
            MissionControlManager.MissionControlConfig = context.MissionControlConfig;
            TowerRiseManager.TowerRiseConfig = context.TowerRiseConfig;

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