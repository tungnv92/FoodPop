using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace GamePackages
{
    public class UIDailyRewardInfo : MonoBehaviour
    {
        [SerializeField] private Image imgIcon;
        [SerializeField] private TextMeshProUGUI txtAmount;
        [SerializeField] private Button btnFreeClaim, btnAdsClaim;
        [SerializeField] private GameObject objFreeCapping;
        [SerializeField] private TextMeshProUGUI txtFreeCapping;
        [SerializeField] private GameObject objAdsClaimed;
        [SerializeField] private GameObject objAdsLock;
        [SerializeField] private DailyRewardConfig config;
        DailyRewardManager DailyRewardManager => ProfileManager.instance.GameFeatureContext.DailyRewardManager;
        PopupDailyReward controller;

        public void SetupStage(DailyRewardConfig config, PopupDailyReward popupDailyReward)
        {
            this.config = config;
            config = DailyRewardManager.GetConfig(config.stageID);
            imgIcon.sprite = config.sprIcon;
            txtAmount.text = config.reward.GetNormalValue();
            LoadStageInfo(config.stageID);
            controller = popupDailyReward;
        }
        public void OnReloadStage()
        {
            LoadStageInfo(config.stageID);
        }
        private void LoadStageInfo(int stage)
        {
            bool isClaimed = DailyRewardManager.IsClaimedStage(stage, out string sCappingTime);
            bool isEnableClaim = DailyRewardManager.IsEnableClaimAds(stage);
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

        // EN: Coordinates Update using encapsulated player and feature APIs instead of accessing live save models.
        // VI: Điều phối Update qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
        private void Update()
        {
            if (config != null && config.isFree)
            {
                bool showClaimedStage0 = DailyRewardManager.TotalFreeClaimed >= DailyRewardManager.totalAbleFreeClaim;
                if (objAdsClaimed) objAdsClaimed.gameObject.SetActive(showClaimedStage0);

                bool isClaimed = DailyRewardManager.IsClaimedStage(config.stageID, out string sCappingTime);
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
        public void OnFreeClaim()
        {
            controller.OnFreeClaim(config);
        }
        public void OnAdsClaim()
        {
            controller.OnAdsClaim(config);
        }
    }
}