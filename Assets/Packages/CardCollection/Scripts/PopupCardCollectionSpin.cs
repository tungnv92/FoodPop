using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using GamePackages;
using Longvm.RewardAnimator;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupCardCollectionSpin : Popup
{
    [Header("Top Set Progress (0/12 Spins)")]
    [Tooltip("Nhãn hiển thị tiến độ mốc thưởng vòng quay (ví dụ: Complete set after 0/12 spins).")]
    [SerializeField] private TMP_Text setProgressText;

    [Tooltip("Thanh tiến độ mốc thưởng 12 lượt quay (tùy chọn).")]
    [SerializeField] private Image setProgressFillImage;

    [Header("12 Spin Board Slots")]
    [Tooltip("Danh sách 12 ô phần thưởng xếp quanh nhân vật trung tâm.")]
    [SerializeField] private List<CardCollectionSpinSlotUI> slotUIs = new();

    [Header("Ticket Balance & Spin Button")]
    [Tooltip("Nhãn hiển thị số lượng vé hiện có của người chơi.")]
    [SerializeField] private TMP_Text ticketBalanceText;

    [Tooltip("Nhãn hiển thị giá vé cho mỗi lượt quay (mặc định: 50).")]
    [SerializeField] private TMP_Text spinCostText;

    [Tooltip("Nút bấm thực hiện quay.")]
    [SerializeField] private Button spinButton;

    [Tooltip("Số vòng quay tối thiểu chạy qua bàn 12 ô trước khi dừng ở ô trúng thưởng.")]
    [SerializeField] private int minFullLaps = 2;

    private bool _isSpinning;
    private int _currentHighlightIndex;
    private Coroutine _spinRoutine;

    private CardCollectionManager Manager => CardCollectionManager.Instance;

    // EN: Opens the Spin popup, initializes all 12 board slots, and refreshes ticket and milestone counters.
    // VI: Mở popup Vòng Quay, khởi tạo cả 12 ô phần thưởng trên bàn và cập nhật số vé cùng mốc thưởng.
    public override void Open(object args = null)
    {
        base.Open(args);
        _isSpinning = false;
        SetupBoardSlots();
        RefreshUI();
    }

    // EN: Stops any active spin coroutine if the popup is disabled.
    // VI: Dừng coroutine quay đang chạy nếu popup bị tắt.
    private void OnDisable()
    {
        if (_spinRoutine != null)
        {
            StopCoroutine(_spinRoutine);
            _spinRoutine = null;
        }
        _isSpinning = false;
    }

    // EN: Initializes the 12 board slot UI elements from CardCollectionConfig.spinSlots.
    // VI: Khởi tạo 12 phần tử giao diện ô quay từ cấu hình CardCollectionConfig.spinSlots.
    private void SetupBoardSlots()
    {
        if (Manager == null || Manager.config == null || slotUIs == null) return;
        Manager.config.EnsureDefaultSetup();

        int count = Mathf.Min(slotUIs.Count, Manager.config.spinSlots.Count);
        for (int i = 0; i < count; i++)
        {
            if (slotUIs[i] != null)
            {
                slotUIs[i].Setup(i, Manager.config.spinSlots[i]);
            }
        }
    }

    // EN: Updates the X/12 spin milestone text, ticket balance label, and spin button interactability.
    // VI: Cập nhật nhãn tiến độ X/12 lượt quay, số dư vé hiện tại và trạng thái nút bấm quay.
    public void RefreshUI()
    {
        if (Manager == null || Manager.Data == null) return;

        int currentSetSpins = Manager.Data.currentSpinSetCount;
        int targetSetSpins = Manager.SpinSetTargetCount;

        if (setProgressText != null)
        {
            setProgressText.text = $"Complete set after <color=#29E33C>{currentSetSpins}/{targetSetSpins}</color> spins";
        }

        if (setProgressFillImage != null)
        {
            setProgressFillImage.fillAmount = targetSetSpins > 0 ? (float)currentSetSpins / targetSetSpins : 0f;
        }

        if (ticketBalanceText != null)
        {
            ticketBalanceText.text = Manager.Data.ticketAmount.ToString();
        }

        if (spinCostText != null)
        {
            spinCostText.text = Manager.SpinCostTickets.ToString();
        }

        if (spinButton != null)
        {
            spinButton.interactable = !_isSpinning && Manager.CanSpin();
        }
    }

    // EN: Triggered when the player clicks the Spin button; deducts tickets and runs the board stepping animation.
    // VI: Được gọi khi người chơi nhấn nút Spin; trừ vé và chạy hiệu ứng nhảy ô trên bàn quay.
    public void ButtonSpin()
    {
        if (_isSpinning || Manager == null || !Manager.CanSpin()) return;

        if (!Manager.TryExecuteSpin(
                out int landedSlotIndex,
                out CardSpinSlotInfo landedSlot,
                out CardDropResult spinCardDrop,
                out CardDropResult setBonusCardDrop))
        {
            return;
        }

        RefreshUI();
        _spinRoutine = StartCoroutine(IESpinBoardAnimation(landedSlotIndex, landedSlot, spinCardDrop, setBonusCardDrop));
    }

    // EN: Animates the highlight stepping around the 12 board slots with deceleration before presenting the won reward.
    // VI: Diễn hoạt khung sáng chạy vòng quanh 12 ô với tốc độ giảm dần trước khi hiển thị phần thưởng trúng được.
    private IEnumerator IESpinBoardAnimation(
        int targetSlotIndex,
        CardSpinSlotInfo landedSlot,
        CardDropResult spinCardDrop,
        CardDropResult setBonusCardDrop)
    {
        _isSpinning = true;
        RefreshUI();

        int slotCount = slotUIs != null && slotUIs.Count > 0 ? slotUIs.Count : 12;
        targetSlotIndex = Mathf.Clamp(targetSlotIndex, 0, slotCount - 1);

        int stepsToTarget = (targetSlotIndex - _currentHighlightIndex + slotCount) % slotCount;
        int totalSteps = Mathf.Max(1, minFullLaps) * slotCount + stepsToTarget;

        for (int step = 0; step <= totalSteps; step++)
        {
            int idx = (_currentHighlightIndex + step) % slotCount;
            HighlightOnlySlot(idx);

            float t = (float)step / totalSteps;
            float delay = Mathf.Lerp(0.04f, 0.24f, t * t);
            yield return new WaitForSeconds(delay);
        }

        _currentHighlightIndex = targetSlotIndex;
        if (slotUIs != null && targetSlotIndex < slotUIs.Count && slotUIs[targetSlotIndex] != null)
        {
            slotUIs[targetSlotIndex].PlayLandedPulse();
        }

        yield return new WaitForSeconds(0.45f);

        PresentSpinRewards(landedSlot, spinCardDrop, setBonusCardDrop);

        _isSpinning = false;
        RefreshUI();
    }

    // EN: Highlights only the slot at activeIndex and clears highlights on all other slots.
    // VI: Chỉ bật khung sáng tại ô activeIndex và tắt khung sáng ở tất cả các ô còn lại.
    private void HighlightOnlySlot(int activeIndex)
    {
        if (slotUIs == null) return;
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null)
            {
                slotUIs[i].SetHighlight(i == activeIndex);
            }
        }
    }

    // EN: Displays the reward won from the spin slot (either via RewardAnimator for resources or PopupCardReward for cards).
    // VI: Hiển thị phần thưởng trúng được từ lượt quay (qua RewardAnimator cho tài nguyên hoặc PopupCardReward cho thẻ).
    private void PresentSpinRewards(
        CardSpinSlotInfo landedSlot,
        CardDropResult spinCardDrop,
        CardDropResult setBonusCardDrop)
    {
        List<CardDropResult> cardsToShow = new();
        if (spinCardDrop != null) cardsToShow.Add(spinCardDrop);
        if (setBonusCardDrop != null) cardsToShow.Add(setBonusCardDrop);

        if (landedSlot != null && landedSlot.slotType == CardSpinSlotType.Resource && landedSlot.resourceReward != null)
        {
            Panel panel = UIManager.Ins != null ? UIManager.Ins.GetPanel() : null;
            RewardAnimator rwAnimator = panel != null ? panel.RewardAnimator : null;
            if (rwAnimator != null)
            {
                rwAnimator.Init();
                rwAnimator.Add(
                    MissionControlManager.MapRewardToAnimatorType(landedSlot.resourceReward.type),
                    landedSlot.resourceReward.value);
                rwAnimator.Play();
            }
        }

        if (cardsToShow.Count > 0 && UIManager.Ins != null)
        {
            UIManager.Ins.OpenPopup(PopupId.CardCollectionReward, cardsToShow);
        }
    }

    // EN: Closes the Spin popup if a spin animation is not currently in progress.
    // VI: Đóng popup Vòng Quay nếu hiện không có lượt quay nào đang diễn hoạt.
    public void ButtonClose()
    {
        if (_isSpinning) return;
        Close();
    }
}

