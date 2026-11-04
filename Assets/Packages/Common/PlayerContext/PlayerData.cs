using System.Collections.Generic;
using UnityEngine;
namespace GamePackages
{
    [System.Serializable]
    public class PlayerData
    {
        public bool isFirstAppOpen;
        public int currentLevel;
        public bool isPlaying;
        public bool isRemoveAds;
        public bool isBoughtStarterPack;
        public bool isInitSaleStarterPack;
        public bool isSetupProfile;
        public bool isFirstHomeOpenInDay;
        public string playerName;
        public int avatar_id, frame_avatar_id;
        public bool isSoundOn, isMusicOn, isVibrateOn;
        public List<ResourceGame> l_ResourceGame = new();
        public int PendingCoinAmount;
        public DailyGiftData DailyGiftData;
        public DailyRewardData DailyRewardData;
        public RecordTrackingData RecordTrackingData;
        public HeartData HeartData;
        public WinStreakData WinStreakData;
        public LavaQuestData LavaQuestData;
        public LavaQuestData lavaQuestData => LavaQuestData;
        public MissionControlData MissionControlData;
        public MissionControlData missionControlData
        {
            get => MissionControlData;
            set => MissionControlData = value;
        }
        public TowerRiseData TowerRiseData;
        public CardCollectionData CardCollectionData;
        public CardCollectionData cardCollectionData { get => CardCollectionData; set => CardCollectionData = value; }
        public TowerRiseData towerRiseData
        {
            get => TowerRiseData;
            set => TowerRiseData = value;
        }
        // EN: Initializes default player data, resources, settings, and feature save states for a new profile.
        // VI: Khởi tạo dữ liệu người chơi mặc định, tài nguyên, cài đặt và trạng thái lưu của các tính năng cho hồ sơ mới.
        public void OnInit()
        {
            isFirstAppOpen = true;
            playerName = "User" + Random.Range(1000, 9999);
            currentLevel = 1;
            l_ResourceGame.Add(new ResourceGame() { type = ResourceType.Coin, value = 1000 });
            l_ResourceGame.Add(new ResourceGame() { type = ResourceType.BoosterAddSlot, value = 0 });
            l_ResourceGame.Add(new ResourceGame() { type = ResourceType.BoosterShuffle, value = 0 });
            l_ResourceGame.Add(new ResourceGame() { type = ResourceType.BoosterPickup, value = 0 });
            l_ResourceGame.Add(new ResourceGame() { type = ResourceType.BoosterSuperBear, value = 0 });
            isSoundOn = true;
            isMusicOn = true;
            isVibrateOn = true;
            DailyGiftData = new();
            DailyRewardData = new();
            RecordTrackingData = new();
            HeartData = new();
            WinStreakData = new();
            LavaQuestData = new();
            MissionControlData = new();
            TowerRiseData = new();
            CardCollectionData = new();
        }

        public void OnNewDay()
        {
            isFirstHomeOpenInDay = true;
        }

    }
    [System.Serializable]
    public class ResourceGame
    {
        public ResourceType type;
        public int value;
        public string GetNormalValue()
        {
            string sValue = "";
            switch (type)
            {
                case ResourceType.BoosterAddSlot:
                case ResourceType.BoosterShuffle:
                case ResourceType.BoosterPickup:
                case ResourceType.BoosterSuperBear:
                    sValue = "x" + value.ToString();
                    break;
                case ResourceType.UnlimitedLives:
                    if (value >= 60)
                    {
                        int mi = value % 60;
                        sValue = (int)(value / 60) + "h" + (mi > 0 ? $"{mi}m" : "");
                    } else
                        sValue = value.ToString() + "m";
                    break;
                default:
                    sValue = value.ToString();
                    break;
            }
            return sValue;
        }
        public Sprite GetRewardSprite()
        {
            return ProfileManager.instance.RewardIconMapping.RewardIconDict[type];
        }
    }
    public enum ResourceType
    {
        Coin,
        UnlimitedLives,
        BoosterAddSlot,
        BoosterShuffle,
        BoosterPickup,
        BoosterSuperBear,
    }

    [System.Serializable]
    public class WinStreakData
    {
        public int keyAmount;   // root saved amount
        public int pendingKeyAmount;    // temp save for play collect anim
        public int numberWinsteak; // 0..4 (multiplier tier)
        public int winstreak; // total consecutive wins
        public string treasureExpire;
        public string currentRoundId;
        public int currentRound;
        public bool isShownFirstTut;
        public bool isShownFirstInfo;
        public bool isCompleted;
        public List<int> claimedMilestoneIndexes = new();

        public WinStreakData()
        {
            Reset();
        }

        public void Reset()
        {
            keyAmount = 0;
            pendingKeyAmount = 0;
            numberWinsteak = 0;
            winstreak = 0;
            treasureExpire = "";
            currentRoundId = "";
            currentRound = 0;
            isShownFirstInfo = false;
            isCompleted = false;
            claimedMilestoneIndexes.Clear();
        }
    }

    [System.Serializable]
    public class LavaQuestData
    {
        // Legacy fields for backward compatibility
        public int winStreak;
        public bool isCompeting;
        public int playersLeftAmount = 100;
        public string expire = "";
        public string rejoinExpire = "";
        public bool isCompeteFromLastJourney;
        public bool isQuitLevelLastSession;
        public bool isShowedInfo;
        public bool isDoneTutorial;

        // Persistent fields (LavaQuest_Event_Rules.md Section 11)
        public string eventDate = "";
        public string attemptId = "";
        public string attemptDate = "";
        public string attemptStartTime = "";
        public string attemptEndTime = "";
        public int attemptStatus; // LavaQuestAttemptStatus
        public int currentMilestone;

        public string lastFailTime = "";
        public string retryAvailableAt = "";

        public int targetFinalParticipants = 10;
        public int activeBotCount = 99;
        public int failedBotCount;
        public int completedBotCount;

        public bool isCompleted;
        public bool isFailed;
        public bool rewardGranted;
        public string rewardTransactionId = "";
        public string lastProcessedGameplayResultId = "";
        public string completedEventDate = "";

        public List<LavaQuestBotData> bots = new();

        public LavaQuestData()
        {
            Reset();
        }

        public void Reset()
        {
            winStreak = 0;
            isCompeting = false;
            playersLeftAmount = 100;
            expire = "";
            rejoinExpire = "";
            isCompeteFromLastJourney = false;
            isQuitLevelLastSession = false;
            isShowedInfo = false;
            isDoneTutorial = false;

            eventDate = "";
            attemptId = "";
            attemptDate = "";
            attemptStartTime = "";
            attemptEndTime = "";
            attemptStatus = (int)LavaQuestAttemptStatus.NotStarted;
            currentMilestone = 0;

            lastFailTime = "";
            retryAvailableAt = "";

            targetFinalParticipants = 10;
            activeBotCount = 99;
            failedBotCount = 0;
            completedBotCount = 0;

            isCompleted = false;
            isFailed = false;
            rewardGranted = false;
            rewardTransactionId = "";
            lastProcessedGameplayResultId = "";
            completedEventDate = "";

            bots.Clear();
        }

        public void ClearDataNewEvent()
        {
            winStreak = 0;
            isCompeting = false;
            playersLeftAmount = 100;
            isCompeteFromLastJourney = false;

            attemptId = "";
            attemptDate = "";
            attemptStartTime = "";
            attemptEndTime = "";
            attemptStatus = (int)LavaQuestAttemptStatus.NotStarted;
            currentMilestone = 0;

            activeBotCount = 99;
            failedBotCount = 0;
            completedBotCount = 0;

            isCompleted = false;
            isFailed = false;
            rewardGranted = false;
            rewardTransactionId = "";
            lastProcessedGameplayResultId = "";

            bots.Clear();
        }

        public void ClearAllData()
        {
            Reset();
        }

        public void SaveData()
        {
            if (ProfileManager.instance != null)
            {
                ProfileManager.instance.SaveData();
            }
        }
    }

    [System.Serializable]
    public class TowerRiseData
    {
        public int currentMilestone;
        public int lastMilestone;
        public int totalMilestone;
        public bool isStarted;
        public bool isEventFinishedPending;
        public bool isDoneTutorial;
        public bool isShowedInfo;
        public bool isQuitLevelLastSession;
        public string expire = "";
        public List<int> claimedIndexes = new();
        public List<int> finishedPendingClaimMilestoneIndexes = new();
        public List<ResourceGame> unclaimedRewardInfos = new();

        public TowerRiseData()
        {
            Reset();
        }

        public void Reset()
        {
            currentMilestone = 0;
            lastMilestone = 0;
            totalMilestone = 0;
            isStarted = false;
            isEventFinishedPending = false;
            isDoneTutorial = false;
            isShowedInfo = false;
            isQuitLevelLastSession = false;
            expire = "";
            claimedIndexes.Clear();
            finishedPendingClaimMilestoneIndexes.Clear();
            unclaimedRewardInfos.Clear();
        }

        public void ClearDataNewWeek()
        {
            currentMilestone = 0;
            lastMilestone = 0;
            totalMilestone = 0;
            isStarted = false;
            isEventFinishedPending = false;
            isQuitLevelLastSession = false;
            claimedIndexes.Clear();
            finishedPendingClaimMilestoneIndexes.Clear();
            unclaimedRewardInfos.Clear();
        }

        public void ClearAllData()
        {
            Reset();
        }

        public void SaveData()
        {
            if (ProfileManager.instance != null)
            {
                ProfileManager.instance.SaveData();
            }
        }
    }
}