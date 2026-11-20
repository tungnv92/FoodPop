using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace GamePackages
{
    public class UIDailyRewardInfo : MonoBehaviour
    {
        DailyRewardFeature DailyReward => DailyRewardFeature.Instance;
        [SerializeField] private Image imgIcon;
        [SerializeField] private TextMeshProUGUI txtAmount;
        [SerializeField] private Button btnFreeClaim, btnAdsClaim;
        [SerializeField] private GameObject objFreeCapping;
        [SerializeField] private TextMeshProUGUI txtFreeCapping;
        [SerializeField] private GameObject objAdsClaimed;
        [SerializeField] private GameObject objAdsLock;
        [SerializeField] private DailyRewardConfig config;
        PopupDailyReward controller;

        // EN: Keeps the free stage's claim button, cooldown text and claimed mark in sync with the daily reward feature.
        // VI: Đồng bộ nút nhận, chữ thời gian chờ và dấu đã nhận của stage miễn phí với tính năng thưởng hằng ngày.
        private void Update()
        {
            if (config != null && config.isFree)
            {
                bool showClaimedStage0 = DailyReward.TotalFreeClaimed >= DailyRewardFeature.totalAbleFreeClaim;
                if (objAdsClaimed) objAdsClaimed.gameObject.SetActive(showClaimedStage0);

                bool isClaimed = DailyReward.IsClaimedStage(config.stageID, out string sCappingTime);
                if (isClaimed)
                {
                    if (btnFreeClaim) btnFreeClaim.gameObject.SetActive(false);
                    if (objFreeCapping) objFreeCapping.gameObject.SetActive(true);
                    if (objAdsClaimed != null && objAdsClaimed.gameObject.activeSelf)
                    {
                        if (objFreeCapping) objFreeCapping.gameObject.SetActive(false);

                    }
                    if (txtFreeCapping) txtFreeCapping.text = sCappingTime;
                } else
                {
                    if (btnFreeClaim) btnFreeClaim.gameObject.SetActive(true);
                    if (objFreeCapping) objFreeCapping.gameObject.SetActive(false);
                    if (txtFreeCapping) txtFreeCapping.text = "";
                }

            }
        }

        // EN: Shows the stage's current reward icon and amount, refreshes its claim state and remembers the popup.
        // VI: Hiển thị icon và số lượng phần thưởng hiện tại của stage, làm mới trạng thái nhận và ghi nhớ popup.
        public void SetupStage(DailyRewardConfig config, PopupDailyReward popupDailyReward)
        {
            this.config = config;
            config = DailyReward.GetConfig(config.stageID);
            imgIcon.sprite = config.sprIcon;
            txtAmount.text = config.reward.GetNormalValue();
            LoadStageInfo(config.stageID);
            controller = popupDailyReward;
        }
        public void OnReloadStage()
        {
            LoadStageInfo(config.stageID);
        }
        public void OnFreeClaim()
        {
            controller.OnFreeClaim(config);
        }
        public void OnAdsClaim()
        {
            controller.OnAdsClaim(config);
        }

        // EN: Shows the claim button, claimed mark or lock of an ad stage from the feature state.
        // VI: Hiện nút nhận, dấu đã nhận hoặc khóa của stage quảng cáo theo trạng thái tính năng.
        private void LoadStageInfo(int stage)
        {
            bool isClaimed = DailyReward.IsClaimedStage(stage, out string sCappingTime);
            bool isEnableClaim = DailyReward.IsEnableClaimAds(stage);
            if (config.isAds)
            {
                if (btnAdsClaim) btnAdsClaim.gameObject.SetActive(isEnableClaim);
                if (objAdsClaimed) objAdsClaimed.gameObject.SetActive(isClaimed);
                if (objAdsLock) objAdsLock.gameObject.SetActive(!isEnableClaim);
                if (objAdsClaimed != null && objAdsClaimed.gameObject.activeSelf)
                {
                    if (objAdsLock) objAdsLock.gameObject.SetActive(false);
                }

            }
        }
    }
}
