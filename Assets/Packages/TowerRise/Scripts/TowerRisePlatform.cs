using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerRisePlatform : MonoBehaviour
{
    public int index;
    public RectTransform rectTransform;
    [Header("LABELS:")]
    public TextMeshProUGUI checkpointLabelText;
    public TextMeshProUGUI winAmountText;
    public GameObject claimButtonObj;
    public Image rewardPlaceholderImg;

    bool isFinishLine;
    int rewardMilestoneIndex = -1;

    public void OnInitt(int index, TowerRiseMileStoneInfo mileStoneInfo)
    {
        isFinishLine = false;
        this.index = index;
        rewardMilestoneIndex = index > 0 ? index - 1 : -1;
        if (checkpointLabelText != null)
            checkpointLabelText.text = index == 0 ? string.Empty : "Checkpoint " + index.ToString();
        RefreshStaticLabels();
    }

    public void OnInittFinishLine(int index, int finishWinAmount, TowerRiseMileStoneInfo mileStoneInfo)
    {
        isFinishLine = true;
        this.index = index;
        rewardMilestoneIndex = index - 1;
        if (checkpointLabelText != null) checkpointLabelText.text = "Checkpoint " + index.ToString();
        if (winAmountText != null) winAmountText.text = finishWinAmount.ToString();
    }

    void RefreshStaticLabels()
    {
        if (winAmountText == null) return;
        int stacked = 0;
        if (index > 0
            && TowerRiseManager.Ins != null
            && TowerRiseManager.Ins.towerRiseConfig != null)
        {
            stacked = TowerRiseManager.Ins.towerRiseConfig.GetCumulativeWinAmount(index - 1);
        }
        winAmountText.text = stacked.ToString();
    }

    public void Refresh()
    {
        if (TowerRiseManager.Ins == null) return;

        if (index == 0 && !isFinishLine)
        {
            HideClaimAndReward();
            if (!TowerRiseManager.Ins.IsMilestoneReached(0))
                RefreshStaticLabels();
            return;
        }

        RefreshRewardPlatform();
    }

    void RefreshRewardPlatform()
    {
        bool isClaimable = rewardMilestoneIndex >= 0
            && TowerRiseManager.Ins.IsMilestoneClaimable(rewardMilestoneIndex);
        bool isClaimed = rewardMilestoneIndex >= 0
            && TowerRiseManager.Ins.IsMilestoneClaimed(rewardMilestoneIndex);

        if (claimButtonObj != null) claimButtonObj.SetActive(isClaimable);
        if (rewardPlaceholderImg != null) rewardPlaceholderImg.gameObject.SetActive(!isClaimed);

        if (!isFinishLine) RefreshStaticLabels();
    }

    void HideClaimAndReward()
    {
        if (claimButtonObj != null) claimButtonObj.SetActive(false);
        if (rewardPlaceholderImg != null) rewardPlaceholderImg.gameObject.SetActive(false);
    }

    public void ButtonClaim()
    {
        if (rewardMilestoneIndex < 0 || TowerRiseManager.Ins == null) return;
        if (!TowerRiseManager.Ins.IsMilestoneClaimable(rewardMilestoneIndex)) return;
        TowerRiseManager.Ins.ClaimMilestone(rewardMilestoneIndex, () =>
        {
            Refresh();
            if (UIManager.Ins != null && UIManager.Ins.IsPopupOpen(PopupId.TowerRise))
            {
                UIManager.Ins.GetPopup<TowerRise>(PopupId.TowerRise)?.RefreshTower();
            }
        });
    }
}

public class HoneyRisePlatform : TowerRisePlatform
{
}
