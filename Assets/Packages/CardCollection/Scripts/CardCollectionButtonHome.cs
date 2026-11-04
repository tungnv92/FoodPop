using System.Collections;
using GamePackages;
using TMPro;
using UnityEngine;

public class CardCollectionButtonHome : MonoBehaviour
{
    [Tooltip("Đối tượng hiển thị khi sự kiện đã được mở khóa.")]
    [SerializeField] private GameObject unlockedObject;

    [Tooltip("Đối tượng hiển thị khi sự kiện đang bị khóa (chưa thắng màn 40).")]
    [SerializeField] private GameObject lockedObject;

    [Tooltip("Nhãn hiển thị cấp độ mở khóa sự kiện (ví dụ: Level 40).")]
    [SerializeField] private TMP_Text unlockLevelText;

    [Tooltip("Biểu tượng chấm đỏ thông báo khi có lượt quay hoặc phần thưởng chưa nhận.")]
    [SerializeField] private GameObject notiGo;

    [Tooltip("Nhãn hiển thị thời gian đếm ngược của sự kiện trên nút màn hình chính.")]
    public TMP_Text countDownText;

    [Header("TUT")]
    [Tooltip("Đối tượng hướng dẫn khi mở khóa sự kiện lần đầu.")]
    [SerializeField] private GameObject tutObj;

    [Tooltip("Lớp mặt nạ tối làm nổi bật nút khi chạy hướng dẫn.")]
    [SerializeField] private GameObject maskObj;

    [Tooltip("Thành phần hiệu ứng bàn tay chỉ dẫn.")]
    [SerializeField] private HandTutAnim handAnim;

    private CardCollectionManager Manager => CardCollectionManager.Instance;

    // EN: Refreshes the home button state on startup.
    // VI: Cập nhật trạng thái nút trên màn hình chính khi khởi tạo.
    private void Start()
    {
        RefreshState();
    }

    // EN: Refreshes the home button state whenever it becomes enabled.
    // VI: Cập nhật trạng thái nút trên màn hình chính mỗi khi được bật.
    private void OnEnable()
    {
        RefreshState();
    }

    // EN: Updates the countdown timer and notification badge every frame while the event is active.
    // VI: Cập nhật bộ đếm thời gian và chấm thông báo mỗi khung hình khi sự kiện đang hoạt động.
    private void Update()
    {
        if (Manager == null || !Manager.isActive) return;

        if (countDownText != null)
        {
            countDownText.text = Manager.GetCountDownString();
        }

        if (notiGo != null)
        {
            notiGo.SetActive(Manager.IsNoti());
        }
    }

    // EN: Updates the locked/unlocked visuals, unlock level label, and notification indicator.
    // VI: Cập nhật giao diện khóa/mở khóa, nhãn cấp độ yêu cầu và chấm đỏ thông báo.
    public void RefreshState()
    {
        if (Manager == null)
        {
            gameObject.SetActive(false);
            return;
        }

        bool isUnlocked = Manager.IsLevelUnlocked;
        bool isExpired = Manager.IsExpired();

        gameObject.SetActive(!isExpired && (Manager.config == null || Manager.config.isFeatureEnabled));

        if (unlockedObject != null) unlockedObject.SetActive(isUnlocked);
        if (lockedObject != null) lockedObject.SetActive(!isUnlocked);
        if (unlockLevelText != null) unlockLevelText.text = $"Level {Manager.UnlockLevel}";
        if (notiGo != null) notiGo.SetActive(Manager.IsNoti());
    }

    // EN: Checks whether a card was just dropped from winning a level on the first try and waits to present it.
    // VI: Kiểm tra xem có thẻ vừa rơi ra từ việc thắng màn lần đầu hay không để hiển thị nhận thẻ.
    public bool HasPendingCardDrop()
    {
        return Manager != null &&
               Manager.isActive &&
               Manager.Data != null &&
               Manager.Data.pendingDroppedCardId >= 0;
    }

    // EN: Opens the Card Reward popup for the pending dropped card and waits until the player closes it.
    // VI: Mở popup nhận thẻ cho thẻ vừa rơi ra sau khi thắng màn và chờ cho đến khi người chơi đóng lại.
    public IEnumerator PresentPendingCardDrop()
    {
        if (!HasPendingCardDrop() || UIManager.Ins == null) yield break;

        UIManager.Ins.EnableBlocker(false);
        UIManager.Ins.OpenPopup(PopupId.CardCollectionReward);
        yield return new WaitUntil(() => !UIManager.Ins.IsPopupOpen(PopupId.CardCollectionReward));
    }

    // EN: Checks and triggers the first-time unlock tutorial hand on the Home screen if needed.
    // VI: Kiểm tra và kích hoạt bàn tay hướng dẫn lần đầu mở khóa sự kiện trên màn hình chính nếu cần.
    public bool CheckShowTut()
    {
        if (Manager == null || !Manager.isActive) return false;
        if (Manager.Data == null || Manager.Data.isDoneTutorial) return false;
        if (tutObj == null || handAnim == null) return false;

        tutObj.SetActive(true);
        if (unlockedObject != null) unlockedObject.SetActive(false);
        if (maskObj != null) maskObj.SetActive(true);

        handAnim.ShowTouchAnim(transform.position, Vector2.zero, () =>
        {
            OnClick();
            tutObj.SetActive(false);
            if (unlockedObject != null) unlockedObject.SetActive(true);
            if (maskObj != null) maskObj.SetActive(false);
            if (UIManager.Ins != null) UIManager.Ins.EnableBlocker(false);
        });

        return true;
    }

    // EN: Opens the main Card Collection event popup when the Home button is clicked.
    // VI: Mở popup chính của sự kiện Sưu Tập Thẻ khi người chơi nhấn nút trên màn hình chính.
    public void OnClick()
    {
        if (Manager == null || !Manager.isActive || !Manager.IsLevelUnlocked) return;
        if (UIManager.Ins != null)
        {
            UIManager.Ins.OpenPopup(PopupId.CardCollection);
        }
    }
}

