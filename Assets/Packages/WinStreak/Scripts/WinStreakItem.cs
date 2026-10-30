using GamePackages;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinStreakItem : MonoBehaviour
{
    public RectTransform rectTransform;
    [Header("NUMBER:")]
    public int id;
    public TextMeshProUGUI numberText;
    public Image numberImg;
    public Sprite numberPassSprite;
    public Sprite numberCurrentSprite;
    public GameObject glowObj;
    [Header("REWARDS:")]
    public List<ResourceGame> rewards;
    public WinStreakRewardUI rewardUI;
    public Image rewardImg;
    public Sprite rewardPassSprite;
    public Sprite rewardCurrentSprite;
    public Sprite rewardUpcomingSprite;
    public GameObject lockObj;
    public GameObject tickObj;
    public GameObject grandPrizeObj;

    private WinStreakManager Manager => WinStreakManager.Instance;
    public Transform MilestoneTextTf => numberImg.transform;

    public void OnInitt(int id, List<ResourceGame> rewardList)
    {
        this.id = id;
        this.rewards = rewardList != null ? new List<ResourceGame>(rewardList) : new List<ResourceGame>();
        if (numberText != null) numberText.text = (id + 1).ToString();

        rewardUI.InitReward(rewards);

        var config = Manager != null ? Manager.WinStreakGlobalConfig : null;
        int currentMileStoneIndex = Manager != null && Manager.GetCurrentMileStoneInfo() != null
            ? Manager.GetCurrentMileStoneInfo().index
            : 0;

        int totalCount = config != null && config.winStreakMileStoneInfos != null ? config.winStreakMileStoneInfos.Count : 0;
        if (grandPrizeObj != null)
        {
            grandPrizeObj.SetActive(totalCount > 0 && id == totalCount - 1);
        }

        if (Manager != null && Manager.IsCompleted)
        {
            OnPass();
        }
        else if (id < currentMileStoneIndex || (id == currentMileStoneIndex && totalCount > 0 && id == totalCount - 1 && Manager != null && Manager.IsClaimedAllRewards()))
        {
            OnPass();
        }
        else if (id == currentMileStoneIndex)
        {
            OnCurrent();
        }
        else
        {
            OnLock();
        }
    }

    public void OnPass()
    {
        if (glowObj != null) glowObj.SetActive(false);
        if (numberImg != null) numberImg.sprite = numberPassSprite;
        if (rewardImg != null) rewardImg.sprite = rewardPassSprite;
        if (rewardUI != null) rewardUI.gameObject.SetActive(false);
        if (tickObj != null) tickObj.SetActive(true);
        if (lockObj != null) lockObj.SetActive(false);
    }

    public void OnCurrent()
    {
        if (glowObj != null) glowObj.SetActive(true);
        if (numberImg != null) numberImg.sprite = numberCurrentSprite;
        if (rewardImg != null) rewardImg.sprite = rewardCurrentSprite;
        if (rewardUI != null) rewardUI.gameObject.SetActive(true);
        if (tickObj != null) tickObj.SetActive(false);
        if (lockObj != null) lockObj.SetActive(false);
    }

    public void OnLock()
    {
        if (glowObj != null) glowObj.SetActive(false);
        if (numberImg != null) numberImg.sprite = numberCurrentSprite;
        if (rewardImg != null) rewardImg.sprite = rewardUpcomingSprite;
        if (rewardUI != null) rewardUI.gameObject.SetActive(true);
        if (tickObj != null) tickObj.SetActive(false);
        if (lockObj != null) lockObj.SetActive(true);
    }
}

public class TreasureItem : WinStreakItem
{
}
