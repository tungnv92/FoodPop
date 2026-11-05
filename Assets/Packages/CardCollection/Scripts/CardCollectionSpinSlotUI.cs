using DG.Tweening;
using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardCollectionSpinSlotUI : MonoBehaviour
{
    [Tooltip("Hình ảnh biểu tượng phần thưởng trên ô quay.")]
    [SerializeField] private Image rewardIconImage;

    [Tooltip("Nhãn hiển thị số lượng phần thưởng (ví dụ: 1000 hoặc x1).")]
    [SerializeField] private TMP_Text amountText;

    [Tooltip("Khung sáng hiển thị khi con trỏ vòng quay đi qua hoặc dừng tại ô này.")]
    [SerializeField] private GameObject highlightObject;

    [Tooltip("Đối tượng phủ mờ hoặc dấu tích hiển thị khi ô quay đã nhận thưởng và bị vô hiệu hóa (tùy chọn).")]
    [SerializeField] private GameObject disabledObject;

    [Tooltip("Màu sắc làm tối ô quay khi đã nhận thưởng và bị vô hiệu hóa.")]
    [SerializeField] private Color disabledTintColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

    private Image _slotBgImage;

    public int SlotIndex { get; private set; }
    public bool IsDisabled { get; private set; }

    // EN: Populates the spin slot UI with the configured reward icon, amount text, and initial enabled/disabled state.
    // VI: Cập nhật giao diện ô quay với biểu tượng phần thưởng, nhãn số lượng và trạng thái bật/vô hiệu hóa ban đầu.
    public void Setup(int index, CardSpinSlotInfo slotInfo, bool isDisabled = false)
    {
        SlotIndex = index;
        if (_slotBgImage == null)
        {
            _slotBgImage = GetComponent<Image>();
        }

        SetHighlight(false);

        if (slotInfo != null)
        {
            if (slotInfo.customIcon != null && rewardIconImage != null)
            {
                rewardIconImage.sprite = slotInfo.customIcon;
            }
            else if (slotInfo.slotType == CardSpinSlotType.Resource &&
                     slotInfo.resourceReward != null &&
                     ProfileManager.instance != null &&
                     ProfileManager.instance.RewardIconMapping != null &&
                     rewardIconImage != null)
            {
                rewardIconImage.sprite = slotInfo.resourceReward.GetRewardSprite();
            }

            if (slotInfo.slotType == CardSpinSlotType.Resource && slotInfo.resourceReward != null)
            {
                if (amountText != null)
                {
                    amountText.gameObject.SetActive(true);
                    amountText.text = slotInfo.resourceReward.GetNormalValue();
                }
            }
            else
            {
                if (amountText != null)
                {
                    amountText.gameObject.SetActive(false);
                }
            }
        }

        SetDisabled(isDisabled);
    }

    // EN: Toggles the selection highlight frame on this spin slot (only when the slot is not disabled).
    // VI: Bật hoặc tắt khung sáng chọn trên ô quay này (chỉ bật khi ô chưa bị vô hiệu hóa).
    public void SetHighlight(bool active)
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(active && !IsDisabled);
        }
    }

    // EN: Enables or disables this spin slot visually after its reward has been claimed.
    // VI: Bật hoặc vô hiệu hóa hiển thị của ô quay này sau khi phần thưởng của ô đã được nhận.
    public void SetDisabled(bool disabled)
    {
        IsDisabled = disabled;
        if (disabled)
        {
            SetHighlight(false);
        }

        if (disabledObject != null)
        {
            disabledObject.SetActive(disabled);
        }

        Color targetColor = disabled ? disabledTintColor : Color.white;
        if (_slotBgImage == null)
        {
            _slotBgImage = GetComponent<Image>();
        }
        if (_slotBgImage != null)
        {
            _slotBgImage.color = targetColor;
        }
        if (rewardIconImage != null)
        {
            rewardIconImage.color = targetColor;
        }
        if (amountText != null)
        {
            amountText.alpha = disabled ? disabledTintColor.a : 1f;
        }
    }

    // EN: Plays a punch scale animation when the spin wheel lands on this winning slot.
    // VI: Phát hiệu ứng nảy phóng to khi vòng quay dừng lại trúng ô thưởng này.
    public void PlayLandedPulse()
    {
        SetHighlight(true);
        transform.DOKill();
        transform.localScale = Vector3.one;
        transform.DOPunchScale(Vector3.one * 0.15f, 0.35f, 6, 0.8f);
    }
}
