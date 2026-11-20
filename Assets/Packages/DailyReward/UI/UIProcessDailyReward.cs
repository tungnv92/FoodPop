using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
namespace GamePackages
{
    public class UIProcessDailyReward : MonoBehaviour
    {
        DailyRewardFeature DailyReward => DailyRewardFeature.Instance;
        public Image imgProcess;
        public List<GameObject> checkers;
        // EN: Shows the current ad-claim progress and listens for new claims.
        // VI: Hiển thị tiến trình nhận thưởng quảng cáo hiện tại và lắng nghe lượt nhận mới.
        private void OnEnable()
        {
            LoadProcess();
            DailyReward.onClaimedAction += UpdateProcess;
        }
        // EN: Stops listening for claims.
        // VI: Ngừng lắng nghe lượt nhận.
        private void OnDisable()
        {
            DailyReward.onClaimedAction -= UpdateProcess;
        }
        // EN: Sets the progress bar and checkers instantly from the claimed ad count.
        // VI: Đặt ngay thanh tiến trình và các dấu tick theo số thưởng quảng cáo đã nhận.
        void LoadProcess()
        {
            float process = DailyReward.GetProcessAdsClaim();
            imgProcess.fillAmount = process;
            int totalClaimed = DailyReward.GetTotalAdsClaimed();
            for (int i = 0; i < checkers.Count; i++)
            {
                checkers[i].gameObject.SetActive(i < totalClaimed);
            }
        }
        // EN: Animates the progress bar to the new value, then updates the checkers.
        // VI: Chạy hiệu ứng thanh tiến trình tới giá trị mới rồi cập nhật các dấu tick.
        void UpdateProcess()
        {
            float process = DailyReward.GetProcessAdsClaim();
            imgProcess.DOFillAmount(process, 1).SetSpeedBased(true).OnComplete(() =>
            {
                int totalClaimed = DailyReward.GetTotalAdsClaimed();
                for (int i = 0; i < checkers.Count; i++)
                {
                    checkers[i].gameObject.SetActive(i < totalClaimed);
                }
            });

        }
    }
}
