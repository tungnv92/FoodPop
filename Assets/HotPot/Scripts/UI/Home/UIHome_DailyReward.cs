using System.Collections;
using GamePackages;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: Daily reward button on the Home screen: shows its alert badge while a reward can be claimed (rechecked every second because the free reward unlocks by time) and opens the Daily reward popup when tapped.
    // VI: Nút quà hằng ngày trên màn Home: hiện chấm báo khi còn quà nhận được (kiểm tra lại mỗi giây vì quà miễn phí mở theo thời gian) và mở popup quà hằng ngày khi bấm.
    [DisallowMultipleComponent]
    public sealed class UIHome_DailyReward : MonoBehaviour
    {
        [Tooltip("Nút quà hằng ngày.")]
        [SerializeField] private Button dailyRewardButton;
        [Tooltip("Chấm báo, hiện khi còn quà hằng ngày nhận được.")]
        [SerializeField] private GameObject badge;

        private DailyRewardFeature dailyReward;
        private PlayerDataManager playerData;
        private Coroutine refreshRoutine;

        // EN: Caches the feature and player data, binds the button, shows the badge state and starts the once-per-second recheck.
        // VI: Cache tính năng và dữ liệu người chơi, nối nút, hiện trạng thái chấm báo và bắt đầu kiểm tra lại mỗi giây.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            dailyReward = DailyRewardFeature.Instance;
            playerData = ProfileManager.instance.PlayerDataManager;
            dailyRewardButton.onClick.AddListener(OnClickDailyReward);
            RefreshBadge();
            refreshRoutine = StartCoroutine(RefreshEverySecond());
        }

        // EN: Stops the recheck and removes the button listener.
        // VI: Dừng kiểm tra lại và gỡ listener của nút.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            StopCoroutine(refreshRoutine);
            dailyRewardButton.onClick.RemoveListener(OnClickDailyReward);
        }

        // EN: Rechecks the badge every second in unscaled time.
        // VI: Kiểm tra lại chấm báo mỗi giây theo thời gian thực.
        private IEnumerator RefreshEverySecond()
        {
            var wait = new WaitForSecondsRealtime(1f);
            while (true)
            {
                yield return wait;
                RefreshBadge();
            }
        }

        // EN: Shows the badge only while a daily reward can be claimed.
        // VI: Chỉ hiện chấm báo khi còn quà hằng ngày nhận được.
        private void RefreshBadge()
        {
            badge.SetActive(dailyReward.IsGoodToClaim());
        }

        // EN: Plays haptics and opens the Daily reward popup.
        // VI: Phát rung và mở popup quà hằng ngày.
        private void OnClickDailyReward()
        {
            if (playerData.isVibrateOn) HapticPatterns.PlayEmphasis(0.8f, 0.5f);
            UIManager.Instance.OpenUI<PopupDailyReward>();
        }
    }
}
