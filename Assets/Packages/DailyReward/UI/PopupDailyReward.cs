using DG.Tweening;
using GamePackages;
using Longvm.RewardAnimator;
using Longvm.UIAnimator;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupDailyReward : Popup
{
    DailyRewardFeature DailyReward => DailyRewardFeature.Instance;
    PlayerDataManager PlayerDataManager => ProfileManager.instance.PlayerDataManager;
    [SerializeField] UIAnimator _openAnim;
    [SerializeField] UIAnimator _closeAnim;
    [SerializeField] RectTransform _freeTag;
    [SerializeField] RectTransform _topRectTf;
    [SerializeField] private UIDailyRewardInfo _FreeDailyRewardInfo;
    [SerializeField] private List<UIDailyRewardInfo> l_AdsDailyRewardInfo;
    RewardAnimator rewardAnimator;
    // EN: Moves the coin/life displays onto the popup, plays the open animation, listens for claims and fills every stage.
    // VI: Đưa hiển thị coin/mạng lên popup, chạy hiệu ứng mở, lắng nghe lượt nhận và hiển thị mọi stage.
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
        DailyReward.onClaimedAction += ReloadData;
        InitData();
    }

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

    // EN: Fills the free stage and every ad stage from the feature's configs.
    // VI: Hiển thị stage miễn phí và mọi stage quảng cáo theo config của tính năng.
    void InitData()
    {
        _FreeDailyRewardInfo.SetupStage(DailyReward.GetConfig(0), this);
        for (int i = 0; i < l_AdsDailyRewardInfo.Count; i++)
        {
            l_AdsDailyRewardInfo[i].SetupStage(DailyReward.GetConfig(i + 1), this);

        }
    }
    // EN: Refreshes the free stage (its reward changes after each claim) and the state of every ad stage.
    // VI: Làm mới stage miễn phí (phần thưởng đổi sau mỗi lần nhận) và trạng thái mọi stage quảng cáo.
    void ReloadData()
    {
        _FreeDailyRewardInfo.SetupStage(DailyReward.GetConfig(0), this);
        for (int i = 0; i < l_AdsDailyRewardInfo.Count; i++)
        {
            l_AdsDailyRewardInfo[i].OnReloadStage();
        }
    }

    // EN: Grants the daily reward and records ad completion through optional IRecordTracking after a rewarded ad finishes.
    // VI: Trao phần thưởng hàng ngày và ghi nhận hoàn tất quảng cáo qua IRecordTracking tùy chọn sau khi xem xong quảng cáo nhận thưởng.
    private void OnClaimAdsSuccess(DailyRewardConfig config)
    {
        OnClaimReward(config);
        ProfileManager.instance.Features.Get<IRecordTracking>()?.RecordAdsReward();
        //SDKBridge.TrackAdsReward_Complete(GamePlacement.DailyReward.ToString(), ButtonPlacement.BtnClaimDailyReward.ToString(), PlayerDataManager.GetCurrentLevel(), new List<ResourceGame>() { config.reward });
    }

    // EN: Plays the reward animation that grants the stage's reward, then records the claim on the feature.
    // VI: Chạy hiệu ứng thưởng để cộng phần thưởng của stage, rồi ghi nhận lượt nhận vào tính năng.
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
        DailyReward.OnClaimStage(config.stageID);
    }

    #region UI Events
    // EN: Closes the popup, returns the coin/life displays to Home and stops listening for claims.
    // VI: Đóng popup, trả hiển thị coin/mạng về Home và ngừng lắng nghe lượt nhận.
    public void CloseButton()
    {
        _closeAnim.Play(Close);
        UIManager.Ins.GetPanel<HomePanel>().ResetCoinLife();
        DailyReward.onClaimedAction -= ReloadData;
    }

    #endregion
}
