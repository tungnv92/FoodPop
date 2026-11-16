using DG.Tweening;
using GamePackages;
using Longvm.RewardAnimator;
using Longvm.UIAnimator;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupDailyReward : Popup
{
    [SerializeField] UIAnimator _openAnim;
    [SerializeField] UIAnimator _closeAnim;
    [SerializeField] RectTransform _freeTag;
    [SerializeField] RectTransform _topRectTf;
    RewardAnimator rewardAnimator;
    [SerializeField] private UIDailyRewardInfo _FreeDailyRewardInfo;
    [SerializeField] private List<UIDailyRewardInfo> l_AdsDailyRewardInfo;
    DailyRewardManager DailyRewardManager => ProfileManager.instance.GameFeatureContext.DailyRewardManager;
    PlayerDataManager PlayerDataManager => ProfileManager.instance.PlayerDataManager;
    public override void Open(object args = null)
    {
        base.Open(args);
        UIManager.Ins.GetPanel<HomePanel>().CoinDisplay.transform.SetParent(_topRectTf);
        UIManager.Ins.GetPanel<HomePanel>().LifeDisplay.transform.SetParent(_topRectTf);
        _openAnim.Init();
        _openAnim.Play(() =>
        {
            //if (DailyRewardManager.Ins.CurrentRewardIndex == 0)
            //{
            //    _freeTag.DOShakeRotation(0.35f, Vector3.forward * 10, 10, 0);
            //}
        });
        DailyRewardManager.onClaimedAction += ReloadData;
        InitData();
    }
    void InitData()
    {
        _FreeDailyRewardInfo.SetupStage(DailyRewardManager.GetConfig(0), this);
        for (int i = 0; i < l_AdsDailyRewardInfo.Count; i++)
        {
            l_AdsDailyRewardInfo[i].SetupStage(DailyRewardManager.GetConfig(i + 1), this);

        }
    }
    void ReloadData()
    {
        _FreeDailyRewardInfo.SetupStage(DailyRewardManager.GetConfig(0), this);
        for (int i = 0; i < l_AdsDailyRewardInfo.Count; i++)
        {
            l_AdsDailyRewardInfo[i].OnReloadStage();
        }
    }
    #region UI Events
    public void CloseButton()
    {
        _closeAnim.Play(Close);
        UIManager.Ins.GetPanel<HomePanel>().ResetCoinLife();
        DailyRewardManager.onClaimedAction -= ReloadData;
    }

    #endregion

    public void OnFreeClaim(DailyRewardConfig config)
    {
        OnClaimReward(config);
    }

    // EN: Initiates an ad-based daily reward claim for the specified configuration.
    // VI: Bắt đầu nhận phần thưởng hàng ngày qua quảng cáo cho cấu hình chỉ định.
    public void OnAdsClaim(DailyRewardConfig config)
    {
        //SDKBridge.TrackAdsReward_Click(GamePlacement.DailyReward.ToString(), ButtonPlacement.BtnClaimDailyReward.ToString(), PlayerDataManager.GetCurrentLevel());
        //SDKBridge.ShowReward(() => OnClaimAdsSuccess(config), null);
    }

    // EN: Grants the daily reward and records ad completion after a rewarded ad finishes.
    // VI: Trao phần thưởng hàng ngày và ghi nhận hoàn tất quảng cáo sau khi xem xong quảng cáo nhận thưởng.
    private void OnClaimAdsSuccess(DailyRewardConfig config)
    {
        OnClaimReward(config);
        ProfileManager.instance.GameFeatureContext.RecordManager.RecordAdsReward();
        //SDKBridge.TrackAdsReward_Complete(GamePlacement.DailyReward.ToString(), ButtonPlacement.BtnClaimDailyReward.ToString(), PlayerDataManager.GetCurrentLevel(), new List<ResourceGame>() { config.reward });
    }

    void OnClaimReward(DailyRewardConfig config)
    {
        int level = PlayerDataManager.GetCurrentLevel();
        //string placement = GamePlacement.DailyReward.ToString();
        string placement = "daily_reward";
        ResourceType type = config.reward.type;
        int value = config.reward.value;
        if (rewardAnimator == null) rewardAnimator = UIManager.Ins.GetPanel().RewardAnimator;
        rewardAnimator.Init();
        rewardAnimator.Add((int)type, value, onReceiveReward: () =>
        {
            switch (type)
            {
                case ResourceType.Coin:
                    PlayerDataManager.OnAddCoin(value, placement);
                    break;
                case ResourceType.BoosterClean:
                case ResourceType.BoosterPickup:
                case ResourceType.BoosterShuffle:
                case ResourceType.BoosterAutoComplete:
                    PlayerDataManager.OnAddBooster(type, value, placement);
                    break;
                case ResourceType.UnlimitedLives:
                    PlayerDataManager.AddUnlimitedLiveTime(value, placement);
                    break;

            }
        });
        rewardAnimator.Play();
        DailyRewardManager.OnClaimStage(config.stageID);
    }
}
