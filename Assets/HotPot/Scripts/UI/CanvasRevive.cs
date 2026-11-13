using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FoodPop
{
    [DisallowMultipleComponent]
    public sealed class CanvasRevive : UICanvasPrime
    {
        public enum ReviveCase { OutOfSlots, OutOfTime }

        private const int FirstCoinPrice = 900;
        private const int CoinPriceStep = 1000;

        [Tooltip("Tiêu đề lời mời hồi sinh.")]
        [SerializeField] private TMP_Text titleLabel;
        [Tooltip("Nhóm nội dung đã thiết kế cho trường hợp băng chuyền hết chỗ.")]
        [SerializeField] private GameObject outOfSlot;
        [Tooltip("Nhóm nội dung đã thiết kế cho trường hợp hết thời gian phục vụ.")]
        [SerializeField] private GameObject outOfTime;
        [Tooltip("Chữ giá hồi sinh bằng Coin.")]
        [SerializeField] private TMP_Text priceLabel;
        [Tooltip("Nút hồi sinh bằng Coin.")]
        [SerializeField] private Button coinButton;
        [Tooltip("Nút hồi sinh qua quảng cáo.")]
        [SerializeField] private Button watchButton;
        [Tooltip("Nút từ chối hồi sinh và xác nhận thua.")]
        [SerializeField] private Button closeButton;
        [Tooltip("Tiêu đề khi băng chuyền hết chỗ.")]
        [SerializeField] private string title = "Out Of Slots!";
        [Tooltip("Tiêu đề khi khách hết thời gian phục vụ.")]
        [SerializeField] private string outOfTimeTitle = "Out Of Time!";

        private LevelController levelController;
        private int completedRevives;
        private int revivePrice;
        private UnityAction reviveAction;
        private UnityAction declineAction;
        private Vector2 coinPosition;
        private Vector2 watchPosition;
        private bool capturedLayout;
        private bool available;

        // EN: Watches level progression throughout the cached popup lifetime, including while hidden.
        // VI: Theo dõi tiến độ màn suốt vòng đời popup cache, kể cả khi đang ẩn.
        private void Awake()
        {
            if (!Application.isPlaying) return;
            levelController = FindFirstObjectByType<LevelController>();
            levelController.LevelLoaded += OnLevelLoaded;
            levelController.StateChanged += OnLevelStateChanged;
        }

        // EN: Captures the authored layout and binds the three buttons with cached runtime managers.
        // VI: Ghi bố cục thiết kế và nối ba nút với các manager runtime đã cache.
        private void OnEnable()
        {
            CaptureLayout();
            if (!Application.isPlaying) return;
            CacheFeedbackManagers();
            coinButton.onClick.AddListener(ReviveWithCoin);
            watchButton.onClick.AddListener(WatchAds);
            closeButton.onClick.AddListener(Decline);
        }

        // EN: Cancels animation and pending choices and releases only the owned button listeners.
        // VI: Hủy hoạt cảnh/lựa chọn còn chờ và chỉ gỡ listener nút do canvas sở hữu.
        private void OnDisable()
        {
            ResetAnimation();
            if (Application.isPlaying)
            {
                coinButton.onClick.RemoveListener(ReviveWithCoin);
                watchButton.onClick.RemoveListener(WatchAds);
                closeButton.onClick.RemoveListener(Decline);
            }
            reviveAction = null;
            declineAction = null;
        }

        // EN: Removes persistent level listeners when this cached canvas is destroyed.
        // VI: Gỡ listener màn tồn tại xuyên lúc ẩn khi canvas cache bị hủy.
        private void OnDestroy()
        {
            if (!Application.isPlaying) return;
            levelController.LevelLoaded -= OnLevelLoaded;
            levelController.StateChanged -= OnLevelStateChanged;
        }

        // EN: Selects the authored timeout or conveyor content, updates the shared title and retains Ads/Coin availability and pricing.
        // VI: Chọn nhóm nội dung hết giờ hoặc kẹt băng đã thiết kế, cập nhật tiêu đề chung và giữ trạng thái cùng giá Ads/Coin.
        public void Show(UnityAction revive, UnityAction decline, bool available = true, bool animate = true, ReviveCase reason = ReviveCase.OutOfSlots)
        {
            reviveAction = revive;
            declineAction = decline;
            this.available = available;
            CaptureLayout();
            titleLabel.text = reason == ReviveCase.OutOfTime ? outOfTimeTitle : title;
            outOfSlot.SetActive(reason == ReviveCase.OutOfSlots);
            outOfTime.SetActive(reason == ReviveCase.OutOfTime);
            revivePrice = FirstCoinPrice + CoinPriceStep * completedRevives;
            priceLabel.text = revivePrice.ToString("N0");
            bool firstRevive = completedRevives == 0;
            watchButton.gameObject.SetActive(firstRevive);
            coinButton.gameObject.SetActive(true);
            ((RectTransform)coinButton.transform).anchoredPosition = firstRevive ? coinPosition : new Vector2(0f, coinPosition.y);
            ((RectTransform)watchButton.transform).anchoredPosition = watchPosition;
            coinButton.interactable = available;
            watchButton.interactable = available;
            base.Open();
            AnimateOpen(animate);
        }

        // EN: Temporarily accepts ad success immediately; the SDK can call this after a completed reward later.
        // VI: Tạm nhận thành công quảng cáo ngay; sau này SDK gọi hàm này khi hoàn tất quảng cáo thưởng.
        public void OnWatchAdsSuccess()
        {
            if (completedRevives != 0 || !available || closing || !isActiveAndEnabled) return;
            AnimateClose(CompleteFreeRevive);
        }

        // EN: Resets revive progression on any load, including replay and next level while hidden.
        // VI: Reset tiến độ hồi sinh khi nạp màn, gồm chơi lại và sang màn khi popup đang ẩn.
        private void OnLevelLoaded(LevelConfig config)
        {
            ResetReviveProgress();
        }

        // EN: Resets revive progression when the level ends in victory or confirmed loss.
        // VI: Reset tiến độ hồi sinh khi màn kết thúc bằng thắng hoặc thua xác nhận.
        private void OnLevelStateChanged(LevelController.LevelState state)
        {
            if (state == LevelController.LevelState.Won || state == LevelController.LevelState.Lost)
                ResetReviveProgress();
        }

        // EN: Captures the authored button positions once, including previews.
        // VI: Ghi vị trí các nút một lần, gồm cả xem trước.
        private void CaptureLayout()
        {
            if (capturedLayout) return;
            coinPosition = ((RectTransform)coinButton.transform).anchoredPosition;
            watchPosition = ((RectTransform)watchButton.transform).anchoredPosition;
            capturedLayout = true;
        }

        // EN: Validates Coin funds on any revive before the deferred spend.
        // VI: Kiểm tra tiền Coin ở mọi lần hồi sinh trước khi trừ sau hiệu ứng đóng.
        private void ReviveWithCoin()
        {
            if (!available || closing) return;
            PlayClickFeedback();
            if (playerData.GetResourceValue(ResourceType.Coin) < revivePrice)
            {
                ShowInsufficientCoin();
                return;
            }
            AnimateClose(CompleteCoinRevive);
        }

        // EN: Temporarily routes Watch directly to success as requested, without spending Coin.
        // VI: Tạm chuyển Watch thẳng tới thành công theo yêu cầu, không tiêu Coin.
        private void WatchAds()
        {
            if (completedRevives != 0 || !available || closing) return;
            PlayClickFeedback();
            OnWatchAdsSuccess();
        }

        // EN: Plays haptics and opens the loss popup after exit without click audio.
        // VI: Phát rung và mở popup thua sau hoạt cảnh đóng, không phát tiếng nhấn.
        private void Decline()
        {
            if (closing) return;
            PlayHaptic();
            AnimateClose(CompleteDecline);
        }

        // EN: Counts the first successful ad revive and plays its cue without changing wallet or booster inventory.
        // VI: Ghi nhận hồi sinh Ads đầu và phát âm tương ứng, không đổi ví hoặc kho booster.
        private void CompleteFreeRevive()
        {
            completedRevives++;
            PlayUISound(IngameSoundManager.Event.Revive);
            CompleteChoice(reviveAction);
        }

        // EN: Restores the first-ad offer and the initial Coin price for the next run.
        // VI: Khôi phục đề nghị Ads đầu và giá Coin ban đầu cho lượt chơi mới.
        private void ResetReviveProgress()
        {
            completedRevives = 0;
        }

        // EN: Displays an insufficient-Coin message and keeps the revive choice available.
        // VI: Hiện thông báo thiếu Coin và giữ lựa chọn hồi sinh.
        private void ShowInsufficientCoin()
        {
            Owner.ShowShortMessage($"Not enough gold. You need {revivePrice:N0} gold to revive.");
        }

        // EN: Spends once, advances total progression, plays the revive cue and invokes revival.
        // VI: Trừ đủ giá một lần, tăng tiến độ, phát âm hồi sinh và gọi hồi sinh.
        private void CompleteCoinRevive()
        {
            if (!playerData.OnRemoveCoin(revivePrice, "revive"))
            {
                ResetAnimation();
                canvasGroup.interactable = true;
                ShowInsufficientCoin();
                return;
            }
            completedRevives++;
            PlayUISound(IngameSoundManager.Event.Revive);
            CompleteChoice(reviveAction);
        }

        // EN: Confirms refusal through the existing loss flow.
        // VI: Xác nhận từ chối qua luồng thua hiện có.
        private void CompleteDecline()
        {
            CompleteChoice(declineAction);
        }

        // EN: Closes through UIManager before executing the copied choice exactly once.
        // VI: Đóng qua UIManager trước khi thực hiện lựa chọn đã sao chép đúng một lần.
        private void CompleteChoice(UnityAction choice)
        {
            Owner.CloseUI<CanvasRevive>();
            choice.Invoke();
        }

    }
}
