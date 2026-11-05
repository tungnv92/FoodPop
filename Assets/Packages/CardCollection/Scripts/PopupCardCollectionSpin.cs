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

    [Tooltip("Số vòng quay tối thiểu chạy qua các ô còn hoạt động trước khi dừng ở ô trúng thưởng.")]
    [SerializeField] private int minFullLaps = 2;

    private bool _isSpinning;
    private int _currentHighlightIndex;
    private Coroutine _spinRoutine;

    private CardCollectionManager Manager => CardCollectionManager.Instance;

    // EN: Opens the Spin popup, initializes all 12 board slots with their disabled states, and refreshes ticket and milestone counters.
    // VI: Mở popup Vòng Quay, khởi tạo cả 12 ô phần thưởng với trạng thái bật/vô hiệu hóa và cập nhật số vé cùng mốc thưởng.
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

    // EN: Initializes the 12 board slot UI elements from CardCollectionConfig.spinSlots and applies their claimed/disabled state.
    // VI: Khởi tạo 12 phần tử giao diện ô quay từ cấu hình CardCollectionConfig.spinSlots và áp dụng trạng thái đã nhận/vô hiệu hóa.
    private void SetupBoardSlots()
    {
        if (Manager == null || Manager.config == null || slotUIs == null) return;
        Manager.config.EnsureDefaultSetup();

        int count = Mathf.Min(slotUIs.Count, Manager.config.spinSlots.Count);
        for (int i = 0; i < count; i++)
        {
            if (slotUIs[i] != null)
            {
                bool isClaimed = Manager.IsSpinSlotClaimed(i);
                slotUIs[i].Setup(i, Manager.config.spinSlots[i], isClaimed);
            }
        }

        _currentHighlightIndex = GetNextActiveSlotIndex(_currentHighlightIndex);
    }

    // EN: Updates the X/12 spin milestone text, ticket balance label, spin button interactability, and slot disabled states.
    // VI: Cập nhật nhãn tiến độ X/12 lượt quay, số dư vé hiện tại, trạng thái nút bấm quay và trạng thái vô hiệu hóa của các ô.
    public void RefreshUI()
    {
        if (Manager == null || Manager.Data == null) return;

        int currentSetSpins = Manager.Data.currentSpinSetCount;
        int targetSetSpins = Manager.SpinSetTargetCount;

        if (setProgressText != null)
        {
            setProgressText.text = $"Complete set after <color=#29E33C>{currentSetSpins}/{targetSetSpins}</color> spins";
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

        if (!_isSpinning)
        {
            RefreshSlotsDisabledState();
        }
    }

    // EN: Synchronizes the disabled state of all 12 slot UI items with the manager's claimed spin slots list.
    // VI: Đồng bộ trạng thái vô hiệu hóa của cả 12 ô trên bàn quay với danh sách ô đã nhận thưởng trong Manager.
    private void RefreshSlotsDisabledState()
    {
        if (slotUIs == null || Manager == null) return;
        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null)
            {
                slotUIs[i].SetDisabled(Manager.IsSpinSlotClaimed(i));
            }
        }
    }

    // EN: Triggered when the player clicks the Spin button; deducts tickets and runs the board stepping animation across active slots.
    // VI: Được gọi khi người chơi nhấn nút Spin; trừ vé và chạy hiệu ứng nhảy qua các ô còn hoạt động trên bàn quay.
    public void ButtonSpin()
    {
        if (_isSpinning || Manager == null || !Manager.CanSpin()) return;

        List<int> activeSlotIndices = GetActiveSlotIndices();

        if (!Manager.TryExecuteSpin(
                out int landedSlotIndex,
                out CardSpinSlotInfo landedSlot,
                out CardDropResult spinCardDrop,
                out CardDropResult setBonusCardDrop))
        {
            return;
        }

        if (!activeSlotIndices.Contains(landedSlotIndex))
        {
            activeSlotIndices.Add(landedSlotIndex);
            activeSlotIndices.Sort();
        }

        _isSpinning = true;
        RefreshUI();
        _spinRoutine = StartCoroutine(IESpinBoardAnimation(
            activeSlotIndices,
            landedSlotIndex,
            landedSlot,
            spinCardDrop,
            setBonusCardDrop));
    }

    // EN: Returns the ordered list of slot indices that are currently enabled (not yet rewarded).
    // VI: Trả về danh sách theo thứ tự các chỉ số ô hiện đang hoạt động (chưa nhận thưởng).
    private List<int> GetActiveSlotIndices()
    {
        List<int> activeIndices = new();
        if (slotUIs == null) return activeIndices;

        for (int i = 0; i < slotUIs.Count; i++)
        {
            if (slotUIs[i] != null && !slotUIs[i].IsDisabled)
            {
                activeIndices.Add(i);
            }
        }
        return activeIndices;
    }

    // EN: Finds the position in activeSlotIndices corresponding to the first active slot at or after startSlotIndex.
    // VI: Tìm vị trí trong danh sách activeSlotIndices tương ứng với ô hoạt động đầu tiên từ startSlotIndex trở đi.
    private int GetStartActiveListPos(List<int> activeSlotIndices, int startSlotIndex)
    {
        for (int i = 0; i < activeSlotIndices.Count; i++)
        {
            if (activeSlotIndices[i] >= startSlotIndex)
            {
                return i;
            }
        }
        return 0;
    }

    // EN: Finds the next enabled slot index clockwise starting after fromSlotIndex.
    // VI: Tìm chỉ số ô còn hoạt động kế tiếp theo chiều kim đồng hồ tính từ sau fromSlotIndex.
    private int GetNextActiveSlotIndex(int fromSlotIndex)
    {
        if (slotUIs == null || slotUIs.Count == 0) return 0;
        int count = slotUIs.Count;
        for (int offset = 1; offset <= count; offset++)
        {
            int idx = (fromSlotIndex + offset) % count;
            if (slotUIs[idx] != null && !slotUIs[idx].IsDisabled)
            {
                return idx;
            }
        }
        return 0;
    }

    // EN: Animates the highlight stepping only across unclaimed slots with deceleration, then disables the rewarded slot.
    // VI: Diễn hoạt khung sáng chỉ nhảy qua các ô chưa nhận với tốc độ giảm dần, sau đó vô hiệu hóa ô vừa trúng thưởng.
    private IEnumerator IESpinBoardAnimation(
        List<int> activeSlotIndices,
        int targetSlotIndex,
        CardSpinSlotInfo landedSlot,
        CardDropResult spinCardDrop,
        CardDropResult setBonusCardDrop)
    {
        int activeCount = activeSlotIndices != null ? activeSlotIndices.Count : 0;

        if (activeCount > 1)
        {
            int startPos = GetStartActiveListPos(activeSlotIndices, _currentHighlightIndex);
            int targetPos = activeSlotIndices.IndexOf(targetSlotIndex);
            if (targetPos < 0) targetPos = 0;

            int stepsToTarget = (targetPos - startPos + activeCount) % activeCount;
            int totalSteps = Mathf.Max(1, minFullLaps) * activeCount + stepsToTarget;

            for (int step = 0; step <= totalSteps; step++)
            {
                int slotIdx = activeSlotIndices[(startPos + step) % activeCount];
                HighlightOnlySlot(slotIdx);

                float t = (float)step / totalSteps;
                float delay = Mathf.Lerp(0.04f, 0.24f, t * t);
                yield return new WaitForSeconds(delay);
            }
        }
        else
        {
            HighlightOnlySlot(targetSlotIndex);
        }

        if (slotUIs != null && targetSlotIndex >= 0 && targetSlotIndex < slotUIs.Count && slotUIs[targetSlotIndex] != null)
        {
            slotUIs[targetSlotIndex].PlayLandedPulse();
        }

        yield return new WaitForSeconds(0.45f);

        if (slotUIs != null && targetSlotIndex >= 0 && targetSlotIndex < slotUIs.Count && slotUIs[targetSlotIndex] != null)
        {
            slotUIs[targetSlotIndex].SetDisabled(true);
        }

        _currentHighlightIndex = GetNextActiveSlotIndex(targetSlotIndex);

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
