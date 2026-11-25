using System.Collections.Generic;
using GamePackages;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// EN: Daily reward popup opened by FoodPop.UIManager: shows the free stage and the ad stages, grants a stage's reward immediately on claim and refreshes after every claim.
// VI: Popup quà hằng ngày do FoodPop.UIManager mở: hiện stage miễn phí và các stage quảng cáo, cộng thưởng của stage ngay khi nhận và làm mới sau mỗi lượt nhận.
[DisallowMultipleComponent]
public sealed class PopupDailyReward : FoodPop.UICanvasPrime
{
    [Tooltip("Ô phần thưởng miễn phí.")]
    [FormerlySerializedAs("_FreeDailyRewardInfo")]
    [SerializeField] private UIDailyRewardInfo freeRewardInfo;
    [Tooltip("Các ô phần thưởng xem quảng cáo, theo thứ tự stage 1, 2, 3...")]
    [FormerlySerializedAs("l_AdsDailyRewardInfo")]
    [SerializeField] private List<UIDailyRewardInfo> adsRewardInfos;
    [Tooltip("Nút đóng popup.")]
    [SerializeField] private Button closeButton;

    private DailyRewardFeature dailyReward;

    // EN: Caches managers and the feature, binds the close button and listens for claims while the popup is active.
    // VI: Cache manager và tính năng, nối nút đóng và lắng nghe lượt nhận khi popup đang bật.
    private void OnEnable()
    {
        if (!Application.isPlaying) return;
        CacheFeedbackManagers();
        dailyReward = DailyRewardFeature.Instance;
        closeButton.onClick.AddListener(OnClickClose);
        dailyReward.onClaimedAction += ReloadData;
    }

    // EN: Removes the listeners added in OnEnable.
    // VI: Gỡ các listener đã thêm trong OnEnable.
    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        closeButton.onClick.RemoveListener(OnClickClose);
        dailyReward.onClaimedAction -= ReloadData;
    }

    // EN: Activates the popup, fills every stage from the feature's configs and plays the shared open animation.
    // VI: Bật popup, hiển thị mọi stage theo config của tính năng và chạy hiệu ứng mở dùng chung.
    public override void Open()
    {
        base.Open();
        InitData();
        AnimateOpen();
    }

    // EN: Claims the free stage.
    // VI: Nhận stage miễn phí.
    public void OnFreeClaim(DailyRewardConfig config)
    {
        OnClaimReward(config);
    }

    // EN: Claims an ad stage; temporarily treats the ad as watched until the ads SDK is wired.
    // VI: Nhận một stage quảng cáo; tạm coi như đã xem quảng cáo cho tới khi nối SDK quảng cáo.
    public void OnAdsClaim(DailyRewardConfig config)
    {
        OnClaimAdsSuccess(config);
    }

    // EN: Fills the free stage and every ad stage from the feature's configs.
    // VI: Hiển thị stage miễn phí và mọi stage quảng cáo theo config của tính năng.
    private void InitData()
    {
        freeRewardInfo.SetupStage(dailyReward.GetConfig(0), this);
        for (int i = 0; i < adsRewardInfos.Count; i++)
        {
            adsRewardInfos[i].SetupStage(dailyReward.GetConfig(i + 1), this);
        }
    }

    // EN: Refreshes the free stage (its reward changes after each claim) and the state of every ad stage.
    // VI: Làm mới stage miễn phí (phần thưởng đổi sau mỗi lần nhận) và trạng thái mọi stage quảng cáo.
    private void ReloadData()
    {
        freeRewardInfo.SetupStage(dailyReward.GetConfig(0), this);
        for (int i = 0; i < adsRewardInfos.Count; i++)
        {
            adsRewardInfos[i].OnReloadStage();
        }
    }

    // EN: Grants the stage reward and records the ad completion through optional IRecordTracking after a rewarded ad finishes.
    // VI: Cộng thưởng của stage và ghi nhận hoàn tất quảng cáo qua IRecordTracking tùy chọn sau khi xem xong quảng cáo nhận thưởng.
    private void OnClaimAdsSuccess(DailyRewardConfig config)
    {
        OnClaimReward(config);
        ProfileManager.instance.Features.Get<IRecordTracking>()?.RecordAdsReward();
    }

    // EN: Grants and saves the stage's reward immediately, then records the claim on the feature (which refreshes the popup).
    // VI: Cộng và lưu thưởng của stage ngay, rồi ghi nhận lượt nhận vào tính năng (tính năng sẽ làm mới popup).
    private void OnClaimReward(DailyRewardConfig config)
    {
        PlayHaptic();
        const string placement = "daily_reward";
        ResourceType type = config.reward.type;
        int value = config.reward.value;
        switch (type)
        {
            case ResourceType.Coin:
                playerData.OnAddCoin(value, placement);
                break;
            case ResourceType.UnlimitedLives:
                playerData.AddUnlimitedLiveTime(value, placement);
                break;
            default:
                playerData.OnAddBooster(type, value, placement);
                break;
        }
        PlayUISound(FoodPop.IngameSoundManager.Event.ItemBought);
        dailyReward.OnClaimStage(config.stageID);
    }

    // EN: Closes the popup with the shared exit animation.
    // VI: Đóng popup bằng hiệu ứng thoát dùng chung.
    private void OnClickClose()
    {
        if (closing) return;
        PlayHaptic();
        CloseWithAnimation();
    }
}
