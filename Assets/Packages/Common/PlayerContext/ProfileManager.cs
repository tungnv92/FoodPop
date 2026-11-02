using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
namespace GamePackages
{
    public class ProfileManager : Singleton<ProfileManager>
    {
        /// <summary>
        ///  Save Manager, Save Game Data
        /// </summary>
        public SaveService SaveService;
        /// <summary>
        /// Game Data
        /// </summary>
        public PlayerData PlayerData;
        /// <summary>
        /// Time Manager
        /// </summary>
        public TimeManager TimeManager = new();
        /// <summary>
        /// PlayerDataManager
        /// </summary>
        public PlayerDataManager PlayerDataManager = new();
        /// <summary>
        /// Game Feature
        /// </summary>
        public GameFeatureContext GameFeatureContext;
        //public SaleManager SaleManager = new();
        public DailyGiftRewardConfig DailyGiftRewardConfig;
        public DailyRewardGlobalConfig DailyRewardGlobalConfig;
        [FormerlySerializedAs("TreasureGlobalConfig")]
        public WinStreakGlobalConfig WinStreakGlobalConfig;
        public WinStreakGlobalConfig TreasureGlobalConfig
        {
            get => WinStreakGlobalConfig;
            set => WinStreakGlobalConfig = value;
        }
        public LavaQuestConfig LavaQuestConfig;
        public MissionControlConfig MissionControlConfig;
        public TowerRiseConfig TowerRiseConfig;
        [Header("Avatar & Frame Configs")]
        public HomeFeatures.Profile.AvatarConfig AvatarConfig;
        public FrameConfig FrameConfig;
        //public ItemFeatureIntroConfig ItemFeatureConfig;
        public RemoteConfigBase RemoteConfigBase;
        public static bool isCheckingUpdate = false;
        public bool IsCheatLevel;
        public bool IsActiveBot;
        public int MaxLevel = 100;
        public RewardIconMapping RewardIconMapping;
        protected override void Awake()
        {
            base.Awake();
            LoadData();
        }
        private void Start()
        {
            if (PlayerData.isFirstAppOpen)
            {
                PlayerData.isFirstAppOpen = false;
                SaveData();

                PlayerDataManager.AddUnlimitedLiveTime(30, "first_app_open");
            }

            //SaleManager.InitSale();
            if (PlayerData.isPlaying)
            {
                PlayerData.isPlaying = false;
                GameFeatureContext.HeartManager.UseHeart();
                GameFeatureContext.WinStreakManager.OnLevelLose();
                GameFeatureContext.LavaQuestManager.OnLevelLose();
                SaveData();
            }

        }

        public void Update()
        {
            TimeManager.UpdateTime();
            GameFeatureContext.Update();
        }
        void LoadData()
        {
            SaveService ??= new SaveService();
            PlayerData = SaveService.Load();
            SaveService = new SaveService(PlayerData);
            TimeManager.LoadData();
            PlayerDataManager.SetSaveService(SaveService);
            PlayerDataManager.SetContext(PlayerData);
            GameFeatureContext = new GameFeatureContext(this);
            TimeManager.AddDayCallback("Newday", OnNewDay);
            TimeManager.CheckNewDayWhenLogin();
            GameFeatureContext.DailyGiftManager.DailyGiftGlobalConfig = DailyGiftRewardConfig;
            GameFeatureContext.DailyRewardManager.DailyRewardGlobalConfig = DailyRewardGlobalConfig;
            GameFeatureContext.WinStreakManager.WinStreakGlobalConfig = WinStreakGlobalConfig;
            GameFeatureContext.LavaQuestManager.LavaQuestConfig = LavaQuestConfig;
            GameFeatureContext.TowerRiseManager.TowerRiseConfig = TowerRiseConfig;
        }

        public Sprite GetAvatarSprite(int id)
        {
            if (AvatarConfig != null && AvatarConfig.AvatarSprites != null && id >= 0 && id < AvatarConfig.AvatarSprites.Length)
                return AvatarConfig.AvatarSprites[id];
            return null;
        }

        public Sprite GetFrameSprite(int id)
        {
            if (FrameConfig != null && FrameConfig.FrameSprites != null && id >= 0 && id < FrameConfig.FrameSprites.Length)
                return FrameConfig.FrameSprites[id];
            return null;
        }

        public Sprite GetRandomAvatarSprite()
        {
            if (AvatarConfig != null && AvatarConfig.AvatarSprites != null && AvatarConfig.AvatarSprites.Length > 0)
                return AvatarConfig.AvatarSprites[UnityEngine.Random.Range(0, AvatarConfig.AvatarSprites.Length)];
            return null;
        }

        public Sprite GetRandomFrameSprite()
        {
            if (FrameConfig != null && FrameConfig.FrameSprites != null && FrameConfig.FrameSprites.Length > 0)
                return FrameConfig.FrameSprites[UnityEngine.Random.Range(0, FrameConfig.FrameSprites.Length)];
            return null;
        }

        public void SetMaxLevel(int value)
        {
            value = 100;
            if (value > 0 && value >= GetMaxLevel() && IsNewVer())
            {
                MaxLevel = value;
                PlayerPrefs.SetInt("Max_Level", MaxLevel);
            }
        }

        // EN: Checks whether the current application version matches the server version.
        // VI: Kiểm tra phiên bản ứng dụng hiện tại có khớp với phiên bản máy chủ hay không.
        public bool IsNewVer()
        {
            string currentVersion = Application.version;
            //if (currentVersion.Equals(SDKBridge.serverVer)) return true;
            return false;
        }
        public int GetMaxLevel()
        {
            //return PlayerPrefs.GetInt("Max_Level", MaxLevel);
            return MaxLevel;
        }
        public int GetCurrentLevel()
        {
            //return PlayerPrefs.GetInt("Max_Level", MaxLevel);
            return PlayerData.currentLevel;
        }
        public void JumpToTriggerLevel(int triggerLevel)
        {
            PlayerData.currentLevel = triggerLevel;
            SaveData();
        }
        void OnNewDay()
        {
            Debug.Log("OnNewDay");
            PlayerData.OnNewDay();
            GameFeatureContext.OnNewDay();
            StartCoroutine(ITrackUserProperty());
        }

        // EN: Waits briefly before triggering open-app tracking across game features.
        // VI: Chờ một khoảng ngắn trước khi kích hoạt theo dõi mở ứng dụng cho các tính năng.
        IEnumerator ITrackUserProperty()
        {
            yield return new WaitForSeconds(1);
            //while (!SDKBridge.IsFirebaseReady())
            //{
            //    yield return new WaitForSeconds(0.5f);
            //}
            GameFeatureContext.OnTrackingOpenApp();
        }
        [Button]
        public void SaveData()
        {
            SaveService.SaveData();
            AwaiterActionManager.Instance.SaveData();
            TimeManager.SaveData();
        }
        void ReloadData()
        {
            GameFeatureContext.HeartManager.AsyncTime();
            AwaiterActionManager.Instance.LoadData();
        }
        [Button]
        public void ClearData()
        {
            TimeManager.ClearData();
            PlayerPrefs.DeleteAll();
        }
        private void OnApplicationQuit()
        {
            SaveData();
        }
        private void OnApplicationPause(bool pause)
        {
            if (!pause)
            {
                Debug.Log("ReloadData");
                ReloadData();
            } else
            {
                Debug.Log("OnPause");
                SaveData();
            }
        }
    }

}
