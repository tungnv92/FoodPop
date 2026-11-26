using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
namespace GamePackages
{
    // EN: Daily reward config: the free rewards (one claim each per day, in order) and the ad reward chain.
    // VI: Config thưởng hằng ngày: các phần thưởng miễn phí (mỗi phần nhận một lần mỗi ngày, theo thứ tự) và chuỗi thưởng quảng cáo.
    [CreateAssetMenu(fileName = "DailyRewardGlobalConfig", menuName = "Scriptable Objects/DailyRewardGlobalConfig")]
    public class DailyRewardGlobalConfig : ScriptableObject
    {
        [Tooltip("Bật hoặc tắt tính năng.")]
        public bool isFeatureEnabled = true;
        [Tooltip("Phần thưởng miễn phí theo thứ tự nhận trong ngày; số phần tử là số lượt nhận miễn phí mỗi ngày (cần ít nhất 1).")]
        public List<DailyRewardConfig> free_rewards;
        [Tooltip("Phần thưởng xem quảng cáo, stageID từ 1; số phần tử là số lượt nhận quảng cáo mỗi ngày và phải bằng số dòng quảng cáo trong popup.")]
        public List<DailyRewardConfig> ads_rewards;

        // EN: Returns the free reward for the given claim index, or the last one once every free reward has been claimed.
        // VI: Trả phần thưởng miễn phí theo thứ tự lượt nhận, hoặc phần cuối cùng khi đã nhận hết.
        public DailyRewardConfig GetFreeReward(int index)
        {
            if (index >= free_rewards.Count) return free_rewards[free_rewards.Count - 1];
            return free_rewards[index];
        }
    }

    // EN: One daily reward stage: its id, claim type, cooldown, reward and icon.
    // VI: Một stage thưởng hằng ngày: id, kiểu nhận, thời gian chờ, phần thưởng và icon.
    [Serializable]
    public class DailyRewardConfig
    {
        [Tooltip("Mã stage: 0 cho phần thưởng miễn phí, 1, 2, 3... cho các dòng quảng cáo.")]
        public int stageID;
        [Tooltip("Phần thưởng miễn phí.")]
        public bool isFree;
        [Tooltip("Số giây phải chờ trước khi nhận phần thưởng này, tính từ lần nhận miễn phí trước đó; phần thưởng miễn phí đầu tiên trong ngày không dùng giá trị này.")]
        [ShowIf(@"isFree")]
        public int cappingTimeFree;
        [Tooltip("Phần thưởng nhận bằng cách xem quảng cáo.")]
        public bool isAds;
        [Tooltip("Loại và số lượng phần thưởng.")]
        public ResourceGame reward;
        [Tooltip("Icon hiển thị của phần thưởng.")]
        public Sprite sprIcon;
    }
}
