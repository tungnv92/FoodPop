using GamePackages;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinStreakRewardUI : MonoBehaviour
{
    public Image rewardIcon;
    public TMP_Text rewardAmountTmp;
    public GameObject singleRewardObject;
    public GiftBubble gift;

    private WinStreakManager Manager => WinStreakManager.Instance;
    public void InitReward(List<ResourceGame> rewards)
    {
        if (rewards.Count > 1)
        {
            singleRewardObject.SetActive(false);
            gift.gameObject.SetActive(true);
            gift.OnInit(rewards);
        }
        else
        {
            singleRewardObject.SetActive(true);
            gift.gameObject.SetActive(false);

            ResourceGame reward = rewards[0];
            rewardIcon.sprite = Manager.WinStreakGlobalConfig.GetResourceSprite(reward.type);
            rewardAmountTmp.text = reward.GetNormalValue();
        }
    }
}

public class Treasure_RewardUI : WinStreakRewardUI
{
}
