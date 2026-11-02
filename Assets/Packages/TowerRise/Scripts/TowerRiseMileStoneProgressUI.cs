using AssetKits.ParticleImage;
using DG.Tweening;
using GamePackages;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TowerRiseMileStoneProgressUI : MonoBehaviour
{
    public int index;
    [Header("MILESTONE:")]
    public List<RectTransform> targetMilestones = new();
    [Header("CLAIM:")]
    public Image rewardPlaceholderImg;
    [Header("DOT:")]
    public TMP_Text winAmountText;
    public Image dotImg;
    public Sprite lockDotSp;
    public Sprite unlockDotSp;
    public ParticleImage fxStar;
    [Header("GIFT BUBBLE:")]
    [FormerlySerializedAs("giftBubbleHoneyRise")]
    public TowerRiseGiftBubble giftBubbleTowerRise;
    public TowerRiseGiftBubble giftBubbleHoneyRise => giftBubbleTowerRise;
    private Sprite chestSprite;
    private Sprite chestOpenSprite;
    public void OnInitt(int index, TowerRiseMileStoneInfo mileStoneInfo, Sprite chestSprite, Sprite chestOpenSprite)
    {
        this.index = index;
        this.chestSprite = chestSprite;
        this.chestOpenSprite = chestOpenSprite;
        RefreshLabel();
        SetupRewardDisplay(mileStoneInfo);
    }

    void SetupRewardDisplay(TowerRiseMileStoneInfo mileStoneInfo)
    {
        var rewards = mileStoneInfo?.rewards;
        if (giftBubbleTowerRise != null)
        {
            giftBubbleTowerRise.OnInitt(rewards, -1);
        }

        bool claimed = TowerRiseManager.Instance.IsMilestoneClaimed(index);
        rewardPlaceholderImg.sprite = claimed ? chestOpenSprite : chestSprite;
    }

    public void Refresh(bool refreshClaimState = true)
    {
        var mgr = TowerRiseManager.Ins;
        if (mgr == null) return;

        RefreshAtProgress(mgr.GetTotalCurrentWins(), refreshClaimState);
    }

    public void RefreshAtProgress(float totalProgress, bool refreshClaimState = true, bool playClaimRevealAnim = false)
    {
        var mgr = TowerRiseManager.Ins;
        if (mgr == null) return;
        bool reached = mgr.IsMilestoneReachedAtProgress(index, totalProgress);

        if (dotImg != null && lockDotSp != null && unlockDotSp != null)
            dotImg.sprite = reached ? unlockDotSp : lockDotSp;

        ApplyWinAmountMaterial(reached);

        if (!refreshClaimState) return;

        bool claimable = mgr.IsMilestoneClaimableAtProgress(index, totalProgress);
        bool claimed = mgr.IsMilestoneClaimed(index);

        if (playClaimRevealAnim && claimable)
        {

        }
            
        else SetClaimVisualState(claimable, claimed);
    }

    void SetClaimVisualState(bool claimable, bool claimed)
    {
        if (fxStar != null)
            fxStar.gameObject.SetActive(claimable);


        rewardPlaceholderImg.sprite = claimed ? chestOpenSprite : chestSprite;
    }

    void OnDisable()
    {
        giftBubbleTowerRise?.ShowBubble(false);
    }

    public void ButtonClaim()
    {
        if (TowerRiseManager.Ins == null || !TowerRiseManager.Ins.IsMilestoneClaimable(index)) return;

        TowerRiseManager.Ins.ClaimMilestone(index, () =>
        {
            Refresh();
            if (UIManager.Ins != null && UIManager.Ins.IsPopupOpen(PopupId.TowerRise))
            {
                UIManager.Ins.GetPopup<TowerRise>(PopupId.TowerRise)?.RefreshTower();
            }
        });
    }

    public float GetCheckpointWorldY()
    {
        RectTransform checkpoint = GetCheckpointRect();
        return checkpoint != null ? checkpoint.position.y : transform.position.y;
    }

    public RectTransform GetCheckpointRect()
    {
        if (dotImg != null) return dotImg.rectTransform;
        if (winAmountText != null) return winAmountText.rectTransform;
        return transform as RectTransform;
    }

    void RefreshLabel()
    {
        if (winAmountText == null) return;

        var config = TowerRiseManager.Ins?.towerRiseConfig;
        if (config == null) return;

        winAmountText.text = config.GetCumulativeWinAmount(index).ToString();

        var mgr = TowerRiseManager.Ins;
        if (mgr != null)
            ApplyWinAmountMaterial(mgr.IsMilestoneReachedAtProgress(index, mgr.GetTotalCurrentWins()));
    }

    void ApplyWinAmountMaterial(bool reached)
    {

    }
}

public class HoneyRiseMileStoneProgressUI : TowerRiseMileStoneProgressUI
{
}
