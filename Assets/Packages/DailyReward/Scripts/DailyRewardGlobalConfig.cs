using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace GamePackages
{
    [CreateAssetMenu(fileName = "DailyRewardGlobalConfig", menuName = "Scriptable Objects/DailyRewardGlobalConfig")]
    public class DailyRewardGlobalConfig : ScriptableObject
    {
        public List<DailyRewardConfig> free_rewards;
        public List<DailyRewardConfig> ads_rewards;
        public DailyRewardConfig GetFreeReward(int index)
        {
            if (index >= free_rewards.Count) return free_rewards[free_rewards.Count - 1];
            return free_rewards[index];
        }
        public DailyRewardConfig GetAdsReward(int index)
        {
            if (index >= ads_rewards.Count) return ads_rewards[ads_rewards.Count - 1];
            return ads_rewards[index];
        }
    }
    [Serializable]
    public class DailyRewardConfig
    {
        public int stageID;
        public bool isFree;
        [ShowIf(@"isFree")]
        public int cappingTimeFree;
        public bool isAds;
        public ResourceGame reward;
        public Sprite sprIcon;
    }
}