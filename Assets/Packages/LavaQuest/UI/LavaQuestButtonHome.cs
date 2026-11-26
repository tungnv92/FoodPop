using GamePackages;
using TMPro;
using UnityEngine;

public class LavaQuestButtonHome : MonoBehaviour
{
    [SerializeField] private GameObject unlockedObject;
    [SerializeField] private GameObject lockedObject;
    [SerializeField] private TMP_Text unlock_tmp;
    [SerializeField] private GameObject notiGo;

    public TextMeshProUGUI countDownText;

    private LavaQuestFeature Manager => LavaQuestFeature.Instance;

    // EN: Initializes the button state from the feature unlock level.
    // VI: Khởi tạo trạng thái nút theo level mở tính năng.
    private void Start()
    {
        RefreshState();
    }

    // EN: Shows milestone progress, "Finished", the rejoin countdown or the time left today, and the notification dot.
    // VI: Hiển thị tiến trình mốc, "Finished", thời gian chờ tham gia lại hoặc thời gian còn lại trong ngày, và chấm thông báo.
    private void Update()
    {
        if (Manager == null || !Manager.IsEnabledFeature()) return;

        if (countDownText != null)
        {
            if (Manager.IsAttemptActive)
            {
                countDownText.text = Manager.GetWinStreakString();
            }
            else if (Manager.IsCompletedToday)
            {
                countDownText.text = "Finished";
            }
            else if (Manager.IsWaitingToRejoin())
            {
                countDownText.text = Manager.GetCountDownRejoinString();
            }
            else
            {
                countDownText.text = Manager.GetCountDownString();
            }
        }

        if (notiGo != null)
        {
            notiGo.SetActive(Manager.IsNoti());
        }
    }

    // EN: Hides a missing feature; otherwise shows the locked/unlocked objects and the remote or local unlock level.
    // VI: Ẩn khi không có tính năng; nếu có thì hiển thị object khóa/mở và level mở từ remote hoặc config local.
    public void RefreshState()
    {
        if (Manager == null)
        {
            gameObject.SetActive(false);
            return;
        }
        bool unlocked = Manager.IsEnabledFeature();
        if (unlockedObject != null) unlockedObject.SetActive(unlocked);
        if (lockedObject != null) lockedObject.SetActive(!unlocked);
        if (unlock_tmp != null) unlock_tmp.text = $"Level {Manager.UnlockLevel}";
    }

    // EN: Opens the LavaQuest popup during an attempt, otherwise the introduce popup; ignored while the feature is inactive.
    // VI: Mở popup LavaQuest khi đang có lượt chơi, nếu không thì mở popup giới thiệu; bỏ qua khi tính năng chưa hoạt động.
    public void OnClick()
    {
        if (Manager == null || !Manager.IsEnabledFeature()) return;

        if (Manager.IsAttemptActive)
        {
            FoodPop.UIManager.Instance.OpenUI<LavaQuest>();
        }
        else
        {
            FoodPop.UIManager.Instance.OpenUI<PopupLavaQuestIntroduce>();
        }
    }
}
