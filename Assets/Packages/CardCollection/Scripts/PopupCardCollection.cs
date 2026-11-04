using System.Collections.Generic;
using DG.Tweening;
using GamePackages;
using Longvm.RewardAnimator;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupCardCollection : Popup
{
    public static PopupCardCollection Instance { get; private set; }

    [Header("Safe Area & Top Bar")]
    [Tooltip("Transform vùng an toàn để gắn hiển thị tiền tệ khi mở popup.")]
    public Transform safeAreaTf;

    [Tooltip("Nhãn hiển thị thời gian đếm ngược 14 ngày của sự kiện.")]
    public TMP_Text countdownText;

    [Header("Grand Reward & Overall Progress (0/54)")]
    [Tooltip("Nhãn hiển thị tiến độ tổng số thẻ đã thu thập (ví dụ: 0/54).")]
    public TMP_Text totalProgressText;

    [Tooltip("Thanh tiến độ tổng số thẻ đã thu thập trên 54 thẻ.")]
    public Image totalProgressFillImage;

    [Tooltip("Nhãn hiển thị số lượng xu trong phần thưởng lớn (ví dụ: 6000).")]
    public TMP_Text grandCoinAmountText;

    [Tooltip("Hình ảnh khung avatar đặc biệt trong phần thưởng lớn.")]
    public Image grandFramePreviewImage;

    [Tooltip("Nhãn hiển thị số lượng booster trong phần thưởng lớn (ví dụ: x6).")]
    public TMP_Text grandBoosterAmountText;

    [Tooltip("Nút nhận phần thưởng lớn khi đạt đủ 54/54 thẻ.")]
    public GameObject buttonClaimGrandReward;

    [Tooltip("Dấu tích hiển thị khi đã nhận phần thưởng lớn.")]
    public GameObject grandRewardClaimedObject;

    [Header("Side Feature Buttons (Pack & Spin)")]
    [Tooltip("Chấm đỏ thông báo trên nút Spin khi người chơi đủ vé quay.")]
    public GameObject spinNotiBadge;

    [Tooltip("Nhãn hiển thị số vé hiện có trên nút Spin (tùy chọn).")]
    public TMP_Text spinTicketCountText;

    [Header("6 Collections Grid")]
    [Tooltip("Danh sách 6 nút bộ sưu tập hiển thị ở nửa dưới popup.")]
    public List<CardCollectionSetItemUI> collectionItems = new();

    private CardCollectionManager Manager => CardCollectionManager.Instance;

    // EN: Caches the singleton popup instance on awake.
    // VI: Lưu tham chiếu singleton của popup khi khởi tạo.
    protected virtual void Awake()
    {
        Instance = this;
    }

    // EN: Subscribes to data change notifications when the popup becomes enabled.
    // VI: Đăng ký lắng nghe sự kiện thay đổi dữ liệu khi popup được bật.
    private void OnEnable()
    {
        if (Manager != null)
        {
            Manager.OnDataChanged += RefreshUI;
        }
    }

    // EN: Unsubscribes from data change notifications when the popup is disabled.
    // VI: Hủy đăng ký lắng nghe sự kiện thay đổi dữ liệu khi popup bị tắt.
    private void OnDisable()
    {
        if (Manager != null)
        {
            Manager.OnDataChanged -= RefreshUI;
        }
    }

    // EN: Opens the main Card Collection popup, initializes all 6 collection items, and checks first-time info display.
    // VI: Mở popup chính của sự kiện Sưu Tập Thẻ, khởi tạo 6 bộ sưu tập và kiểm tra hiển thị hướng dẫn lần đầu.
    public override void Open(object args = null)
    {
        base.Open(args);
        Instance = this;

        SetupCollectionButtons();
        RefreshUI();
        HandleFirstOpenTutorialAndInfo();
    }

    // EN: Updates the countdown timer every frame while the popup is open.
    // VI: Cập nhật bộ đếm thời gian mỗi khung hình trong khi popup đang mở.
    private void Update()
    {
        if (Manager == null) return;
        if (countdownText != null)
        {
            countdownText.text = Manager.GetCountDownString();
        }
    }

    // EN: Initializes the 6 collection buttons with their respective collection IDs and click callbacks.
    // VI: Khởi tạo 6 nút bộ sưu tập với chỉ số tương ứng và hàm xử lý khi nhấn.
    private void SetupCollectionButtons()
    {
        if (collectionItems == null) return;
        for (int i = 0; i < collectionItems.Count; i++)
        {
            if (collectionItems[i] != null)
            {
                collectionItems[i].Setup(i, OnClickCollectionSet);
            }
        }
    }

    // EN: Marks tutorial state as seen and automatically opens the Info popup on the player's first visit.
    // VI: Đánh dấu đã xem hướng dẫn và tự động mở popup Thông tin trong lần đầu người chơi mở sự kiện.
    private void HandleFirstOpenTutorialAndInfo()
    {
        if (Manager == null || Manager.Data == null) return;

        bool needSave = false;
        if (!Manager.Data.isDoneTutorial)
        {
            Manager.Data.isDoneTutorial = true;
            needSave = true;
        }

        if (!Manager.Data.isShowedInfo)
        {
            Manager.Data.isShowedInfo = true;
            needSave = true;
            DOVirtual.DelayedCall(Time.deltaTime, () =>
            {
                if (UIManager.Ins != null)
                {
                    UIManager.Ins.OpenPopup(PopupId.CardCollectionInfo);
                }
            });
        }

        if (needSave)
        {
            Manager.Save();
        }
    }

    // EN: Refreshes the overall progress bar (X/54), grand reward preview, spin notification badge, and all 6 collection buttons.
    // VI: Làm mới thanh tiến độ tổng (X/54), phần thưởng lớn, chấm thông báo vòng quay và cả 6 nút bộ sưu tập.
    public void RefreshUI()
    {
        if (Manager == null) return;

        RefreshOverallProgress();
        RefreshGrandRewardPreview();
        RefreshSideButtons();

        if (collectionItems != null)
        {
            for (int i = 0; i < collectionItems.Count; i++)
            {
                if (collectionItems[i] != null)
                {
                    collectionItems[i].RefreshUI();
                }
            }
        }
    }

    // EN: Updates the 0/54 card progress text, progress bar fill, and Grand Reward claim button visibility.
    // VI: Cập nhật nhãn tiến độ 0/54 thẻ, thanh lấp đầy và trạng thái hiển thị nút nhận Grand Reward.
    private void RefreshOverallProgress()
    {
        int totalOwned = Manager.GetTotalOwnedCardsCount();
        int maxCards = CardCollectionManager.TOTAL_CARDS;

        if (totalProgressText != null)
        {
            totalProgressText.text = $"{totalOwned}/{maxCards}";
        }

        if (totalProgressFillImage != null)
        {
            totalProgressFillImage.fillAmount = maxCards > 0 ? (float)totalOwned / maxCards : 0f;
        }

        bool canClaimGrand = Manager.CanClaimGrandReward();
        bool isGrandClaimed = Manager.Data != null && Manager.Data.isClaimedGrandReward;

        if (buttonClaimGrandReward != null) buttonClaimGrandReward.SetActive(canClaimGrand);
        if (grandRewardClaimedObject != null) grandRewardClaimedObject.SetActive(isGrandClaimed);
    }

    // EN: Populates the Grand Reward preview box (coins, special avatar frame, and boosters) from CardCollectionConfig.
    // VI: Hiển thị thông tin xem trước của phần thưởng lớn (xu, khung avatar đặc biệt và booster) từ CardCollectionConfig.
    private void RefreshGrandRewardPreview()
    {
        if (Manager.config == null) return;
        Manager.config.EnsureDefaultSetup();

        if (grandFramePreviewImage != null && Manager.config.grandRewardFrameSprite != null)
        {
            grandFramePreviewImage.sprite = Manager.config.grandRewardFrameSprite;
        }

        if (Manager.config.grandRewardResources != null)
        {
            for (int i = 0; i < Manager.config.grandRewardResources.Count; i++)
            {
                ResourceGame res = Manager.config.grandRewardResources[i];
                if (res.type == ResourceType.Coin && grandCoinAmountText != null)
                {
                    grandCoinAmountText.text = res.value.ToString();
                }
                else if (res.type != ResourceType.Coin && grandBoosterAmountText != null)
                {
                    grandBoosterAmountText.text = $"x{res.value}";
                }
            }
        }
    }

    // EN: Updates the Spin button notification badge and ticket amount label.
    // VI: Cập nhật chấm thông báo trên nút Spin và nhãn số lượng vé hiện có.
    private void RefreshSideButtons()
    {
        if (spinNotiBadge != null)
        {
            spinNotiBadge.SetActive(Manager.CanSpin());
        }

        if (spinTicketCountText != null && Manager.Data != null)
        {
            spinTicketCountText.text = Manager.Data.ticketAmount.ToString();
        }
    }

    // EN: Opens the Collection Album popup for the clicked collection index (0 to 5).
    // VI: Mở popup Album chi tiết cho bộ sưu tập được nhấn (0 đến 5).
    private void OnClickCollectionSet(int collectionId)
    {
        if (UIManager.Ins != null)
        {
            UIManager.Ins.OpenPopup(PopupId.CardCollectionAlbum, collectionId);
        }
    }

    // EN: Claims the Grand Reward when all 54 cards are collected and plays the reward animation.
    // VI: Nhận phần thưởng lớn khi đã thu thập đủ 54 thẻ và phát hiệu ứng nhận thưởng.
    [Button]
    public void ButtonClaimGrandReward()
    {
        if (Manager == null || !Manager.CanClaimGrandReward()) return;

        List<ResourceGame> rewards = Manager.ClaimGrandReward();
        RefreshUI();
        PlayRewardAnimatorForList(rewards);
    }

    // EN: Plays the RewardAnimator sequence for a list of granted ResourceGame rewards.
    // VI: Phát hiệu ứng RewardAnimator cho danh sách các phần thưởng tài nguyên vừa nhận.
    private void PlayRewardAnimatorForList(List<ResourceGame> rewards)
    {
        if (rewards == null || rewards.Count == 0 || UIManager.Ins == null) return;
        Panel panel = UIManager.Ins.GetPanel();
        RewardAnimator rwAnimator = panel != null ? panel.RewardAnimator : null;
        if (rwAnimator == null) return;

        rwAnimator.Init();
        for (int i = 0; i < rewards.Count; i++)
        {
            rwAnimator.Add(MissionControlManager.MapRewardToAnimatorType(rewards[i].type), rewards[i].value);
        }
        rwAnimator.Play();
    }

    // EN: Opens the Card Collection Info popup (Picture 2).
    // VI: Mở popup thông tin hướng dẫn sự kiện Sưu Tập Thẻ (Hình 2).
    public void ButtonInfo()
    {
        if (UIManager.Ins != null)
        {
            UIManager.Ins.OpenPopup(PopupId.CardCollectionInfo);
        }
    }

    // EN: Opens the Card Pack IAP Shop popup (Picture 4).
    // VI: Mở popup cửa hàng mua gói thẻ IAP (Hình 4).
    public void ButtonOpenPackShop()
    {
        if (UIManager.Ins != null)
        {
            UIManager.Ins.OpenPopup(PopupId.CardCollectionShop);
        }
    }

    // EN: Opens the Lucky Spin popup (Picture 3).
    // VI: Mở popup Vòng Quay May Mắn bằng vé (Hình 3).
    public void ButtonOpenSpin()
    {
        if (UIManager.Ins != null)
        {
            UIManager.Ins.OpenPopup(PopupId.CardCollectionSpin);
        }
    }

    // EN: Closes the main Card Collection popup.
    // VI: Đóng popup chính của sự kiện Sưu Tập Thẻ.
    public void ButtonClose()
    {
        Close();
    }
}

