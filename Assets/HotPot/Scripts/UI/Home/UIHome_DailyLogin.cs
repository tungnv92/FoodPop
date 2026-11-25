using System.Collections;
using GamePackages;
using HomeFeatures.DailyLogin;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: Daily login button on the Home screen: shows its alert badge while today's gift can be claimed (rechecked every second so it follows claims and day changes) and opens the Daily login popup when tapped.
    // VI: Nút quà đăng nhập trên màn Home: hiện chấm báo khi quà hôm nay còn nhận được (kiểm tra lại mỗi giây để theo kịp lượt nhận và sang ngày mới) và mở popup quà đăng nhập khi bấm.
    [DisallowMultipleComponent]
    public sealed class UIHome_DailyLogin : MonoBehaviour
    {
        [Tooltip("Nút quà đăng nhập.")]
        [SerializeField] private Button dailyLoginButton;
        [Tooltip("Chấm báo, hiện khi quà đăng nhập hôm nay còn nhận được.")]
        [SerializeField] private GameObject badge;

        private DailyGiftFeature dailyGift;
        private PlayerDataManager playerData;
        private Coroutine refreshRoutine;

        // EN: Caches the feature and player data, binds the button, shows the badge state and starts the once-per-second recheck.
        // VI: Cache tính năng và dữ liệu người chơi, nối nút, hiện trạng thái chấm báo và bắt đầu kiểm tra lại mỗi giây.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            dailyGift = DailyGiftFeature.Instance;
            playerData = ProfileManager.instance.PlayerDataManager;
            dailyLoginButton.onClick.AddListener(OnClickDailyLogin);
            RefreshBadge();
            refreshRoutine = StartCoroutine(RefreshEverySecond());
        }

        // EN: Stops the recheck and removes the button listener.
        // VI: Dừng kiểm tra lại và gỡ listener của nút.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            StopCoroutine(refreshRoutine);
            dailyLoginButton.onClick.RemoveListener(OnClickDailyLogin);
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

        // EN: Shows the badge only while today's login gift can be claimed.
        // VI: Chỉ hiện chấm báo khi quà đăng nhập hôm nay còn nhận được.
        private void RefreshBadge()
        {
            badge.SetActive(dailyGift.IsGoodToClaim());
        }

        // EN: Plays haptics and opens the Daily login popup.
        // VI: Phát rung và mở popup quà đăng nhập.
        private void OnClickDailyLogin()
        {
            if (playerData.isVibrateOn) HapticPatterns.PlayEmphasis(0.8f, 0.5f);
            UIManager.Instance.OpenUI<PopupDailyLogin>();
        }
    }
}
