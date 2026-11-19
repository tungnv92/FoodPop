using DG.Tweening;
using GamePackages;
using TMPro;
using UnityEngine;

namespace FoodPop
{
    // EN: Runs after the default-order ProfileManager Awake has initialized the wallet.
    // VI: Chạy sau Awake của ProfileManager có thứ tự mặc định để ví đã được khởi tạo.
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class UICoin : MonoBehaviour
    {
        [Tooltip("Chữ hiển thị số xu hiện có.")]
        [SerializeField] private TMP_Text amountLabel;
        private PlayerDataManager playerData;
        private bool observing;
        private Tween balanceTween;

        // EN: Subscribes to Coin changes and displays the initial balance through the same cached manager.
        // VI: Đăng ký thay đổi Coin và hiện số dư ban đầu qua cùng manager đã cache.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            playerData = ProfileManager.instance.PlayerDataManager;
            playerData.RegisterResourceChange(ResourceType.Coin, ShowAmount);
            observing = true;
            ShowAmount(playerData.GetResourceValue(ResourceType.Coin));
        }

        // EN: Cancel display animation and remove the listener from the manager used when enabling.
        // VI: Hủy hoạt cảnh hiển thị và gỡ listener khỏi manager đã dùng khi bật component.
        private void OnDisable()
        {
            balanceTween?.Kill();
            balanceTween = null;
            if (!observing) return;
            playerData.RemoveResourceChange(ResourceType.Coin, ShowAmount);
            observing = false;
        }

        // EN: Shows the balance from before an already credited reward and counts up to the saved balance after the requested unscaled delay; call it after crediting so the Coin event does not cancel the count.
        // VI: Hiện số dư trước khi cộng phần thưởng đã được cộng rồi tăng dần tới số dư đã lưu sau khoảng trễ unscaled; gọi sau khi cộng xu để sự kiện Coin không hủy hiệu ứng đếm.
        public void AnimateReward(int from, float duration, float delay = 0f)
        {
            int target = playerData.GetResourceValue(ResourceType.Coin);
            ShowAmount(from);
            balanceTween = DOTween.To(() => from, value => amountLabel.text = value.ToString(), target, duration)
                .SetEase(Ease.Linear).SetDelay(delay).SetUpdate(true);
        }

        // EN: Stop any pending reward preview and display the authoritative wallet amount.
        // VI: Dừng xem trước phần thưởng và hiển thị số dư ví thực tế.
        private void ShowAmount(int amount)
        {
            balanceTween?.Kill();
            balanceTween = null;
            amountLabel.text = amount.ToString();
        }
    }
}
