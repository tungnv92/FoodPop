using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace GamePackages
{
    // EN: One reward row of the Daily reward popup (the free row or an ad row): shows the stage reward and its claim, cooldown, claimed or locked state.
    // VI: Một dòng phần thưởng của popup quà hằng ngày (dòng miễn phí hoặc dòng quảng cáo): hiện phần thưởng của stage và trạng thái nhận, chờ, đã nhận hoặc khóa.
    public class UIDailyRewardInfo : MonoBehaviour
    {
        DailyRewardFeature DailyReward => DailyRewardFeature.Instance;

        [Tooltip("Icon phần thưởng.")]
        [SerializeField] private Image imgIcon;
        [Tooltip("Chữ số lượng phần thưởng.")]
        [SerializeField] private TextMeshProUGUI txtAmount;
        [Tooltip("Nút nhận của dòng miễn phí.")]
        [SerializeField] private Button btnFreeClaim;
        [Tooltip("Nút nhận của dòng quảng cáo.")]
        [SerializeField] private Button btnAdsClaim;
        [Tooltip("Khối đếm ngược của dòng miễn phí, hiện khi đang chờ lượt kế tiếp.")]
        [SerializeField] private GameObject objFreeCapping;
        [Tooltip("Chữ thời gian chờ còn lại của dòng miễn phí.")]
        [SerializeField] private TextMeshProUGUI txtFreeCapping;
        [Tooltip("Dấu đã nhận.")]
        [SerializeField] private GameObject objAdsClaimed;
        [Tooltip("Nút xám khóa của dòng quảng cáo, hiện khi chưa tới lượt.")]
        [SerializeField] private GameObject objAdsLock;

        private DailyRewardConfig config;
        private PopupDailyReward controller;

        // EN: Rechecks the free row once per second while the row is visible, because its cooldown unlocks by time.
        // VI: Kiểm tra lại dòng miễn phí mỗi giây khi dòng đang hiện, vì thời gian chờ mở theo giờ.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            StartCoroutine(RefreshFreeEverySecond());
        }

        // EN: Shows the stage's current reward icon and amount, refreshes its claim state and remembers the popup.
        // VI: Hiển thị icon và số lượng phần thưởng hiện tại của stage, làm mới trạng thái nhận và ghi nhớ popup.
        public void SetupStage(DailyRewardConfig stageConfig, PopupDailyReward popupDailyReward)
        {
            config = stageConfig;
            controller = popupDailyReward;
            imgIcon.sprite = config.sprIcon;
            txtAmount.text = config.reward.GetNormalValue();
            RefreshState();
        }

        // EN: Refreshes the row's claim state after a claim or a new day.
        // VI: Làm mới trạng thái nhận của dòng sau một lượt nhận hoặc khi sang ngày mới.
        public void OnReloadStage()
        {
            RefreshState();
        }

        // EN: Forwards the free row's Claim button to the popup.
        // VI: Chuyển nút Nhận của dòng miễn phí cho popup xử lý.
        public void OnFreeClaim()
        {
            controller.OnFreeClaim(config);
        }

        // EN: Forwards the ad row's Claim button to the popup.
        // VI: Chuyển nút Nhận của dòng quảng cáo cho popup xử lý.
        public void OnAdsClaim()
        {
            controller.OnAdsClaim(config);
        }

        // EN: Refreshes the free row every second in unscaled time.
        // VI: Làm mới dòng miễn phí mỗi giây theo thời gian thực.
        private IEnumerator RefreshFreeEverySecond()
        {
            var wait = new WaitForSecondsRealtime(1f);
            while (true)
            {
                yield return wait;
                if (config != null && config.isFree) RefreshFreeState();
            }
        }

        // EN: Applies the free or ad state depending on the stage type.
        // VI: Áp trạng thái dòng miễn phí hoặc dòng quảng cáo theo loại stage.
        private void RefreshState()
        {
            if (config.isFree) RefreshFreeState();
            else if (config.isAds) RefreshAdsState();
        }

        // EN: Shows the free row's Claim button, its cooldown countdown, or the claimed mark once every free reward of the day is claimed.
        // VI: Hiện nút Nhận của dòng miễn phí, đồng hồ đếm ngược, hoặc dấu đã nhận khi đã nhận hết phần thưởng miễn phí trong ngày.
        private void RefreshFreeState()
        {
            bool allClaimed = DailyReward.TotalFreeClaimed >= DailyReward.TotalAbleFreeClaim;
            bool blocked = DailyReward.IsClaimedStage(config.stageID, out string cappingTime);
            objAdsClaimed.SetActive(allClaimed);
            btnFreeClaim.gameObject.SetActive(!blocked);
            objFreeCapping.SetActive(blocked && !allClaimed);
            txtFreeCapping.text = cappingTime;
        }

        // EN: Shows the ad row's Claim button when it is the next stage, the claimed mark once claimed, otherwise the locked button.
        // VI: Hiện nút Nhận của dòng quảng cáo khi là stage kế tiếp, dấu đã nhận khi đã nhận, còn lại hiện nút khóa.
        private void RefreshAdsState()
        {
            bool isClaimed = DailyReward.IsClaimedStage(config.stageID, out _);
            bool isEnableClaim = DailyReward.IsEnableClaimAds(config.stageID);
            btnAdsClaim.gameObject.SetActive(isEnableClaim);
            objAdsClaimed.SetActive(isClaimed);
            objAdsLock.SetActive(!isEnableClaim && !isClaimed);
        }
    }
}
