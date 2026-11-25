using System.Collections;
using GamePackages;
using Lofelt.NiceVibrations;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: Life block on the Home screen: shows the heart count and next-life timer (Max when full), refreshes every second while visible, and opens the buy-lives popup when any part of the block is tapped.
    // VI: Khối mạng trên màn Home: hiện số mạng và thời gian hồi mạng kế tiếp (Max khi đầy), làm mới mỗi giây khi đang hiện, và mở popup mua mạng khi bấm vào bất kỳ đâu trong khối.
    [DisallowMultipleComponent]
    public sealed class UIHome_Life : MonoBehaviour
    {
        [Tooltip("Chữ số mạng hiện có, đặt trên hình trái tim.")]
        [SerializeField] private TMP_Text heartCountText;
        [Tooltip("Chữ thời gian hồi mạng kế tiếp; hiện Max khi đầy mạng.")]
        [SerializeField] private TMP_Text heartTimerText;
        [Tooltip("Nút phủ cả khối mạng; bấm vào bất kỳ đâu trong khối sẽ mở popup mua mạng.")]
        [FormerlySerializedAs("addLifeButton")]
        [SerializeField] private Button lifeButton;

        private HeartFeature heart;
        private PlayerDataManager playerData;
        private Coroutine refreshRoutine;

        // EN: Caches the heart feature and player data, binds the block button, shows the current hearts and starts the once-per-second refresh.
        // VI: Cache tính năng mạng và dữ liệu người chơi, nối nút của khối, hiện số mạng hiện tại và bắt đầu làm mới mỗi giây.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            heart = HeartFeature.Instance;
            playerData = ProfileManager.instance.PlayerDataManager;
            lifeButton.onClick.AddListener(OnClickAddLife);
            Refresh();
            refreshRoutine = StartCoroutine(RefreshEverySecond());
        }

        // EN: Stops the refresh and removes the block-button listener.
        // VI: Dừng làm mới và gỡ listener nút của khối.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            StopCoroutine(refreshRoutine);
            lifeButton.onClick.RemoveListener(OnClickAddLife);
        }

        // EN: Refreshes the hearts every second in unscaled time.
        // VI: Làm mới số mạng mỗi giây theo thời gian thực.
        private IEnumerator RefreshEverySecond()
        {
            var wait = new WaitForSecondsRealtime(1f);
            while (true)
            {
                yield return wait;
                Refresh();
            }
        }

        // EN: Shows the heart count and the timer, or Max when hearts are full.
        // VI: Hiện số mạng và thời gian, hoặc Max khi đầy mạng.
        private void Refresh()
        {
            (string count, string timer) = heart.GetHeartDisplay();
            heartCountText.text = count;
            heartTimerText.text = heart.IsMaxHeart() ? "Max" : timer;
        }

        // EN: Plays haptics and opens the buy-lives popup unless lives are unlimited or full.
        // VI: Phát rung và mở popup mua mạng trừ khi đang vô hạn hoặc đã đầy.
        private void OnClickAddLife()
        {
            if (playerData.isVibrateOn) HapticPatterns.PlayEmphasis(0.8f, 0.5f);
            if (heart.IsUnlimitedLives() || heart.IsMaxHeart()) return;
            UIManager.Instance.OpenUI<PopupMoreLife>();
        }
    }
}
