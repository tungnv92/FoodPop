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

    [Tooltip("Danh sách các biểu tượng ngôi sao hiển thị độ hiếm nếu ô thưởng là Thẻ (1 đến 4 sao).")]
    [SerializeField] private GameObject[] starObjects;

    public int SlotIndex { get; private set; }

    // EN: Populates the spin slot UI with the configured reward icon, amount text, or card star count.
    // VI: Cập nhật giao diện ô quay với biểu tượng phần thưởng, nhãn số lượng hoặc số sao của thẻ.
    public void Setup(int index, CardSpinSlotInfo slotInfo)
    {
        SlotIndex = index;
        SetHighlight(false);

        if (slotInfo == null) return;

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
            UpdateStarIcons(0);
        }
        else
        {
            if (amountText != null)
            {
                amountText.gameObject.SetActive(false);
            }
            UpdateStarIcons((int)slotInfo.cardRarity);
        }
    }

    // EN: Activates the matching number of star icons for a card slot (0 hides all stars).
    // VI: Bật số lượng biểu tượng ngôi sao tương ứng cho ô thưởng thẻ (0 sẽ ẩn toàn bộ sao).
    private void UpdateStarIcons(int starCount)
    {
        if (starObjects == null) return;
        for (int i = 0; i < starObjects.Length; i++)
        {
            if (starObjects[i] != null)
            {
                starObjects[i].SetActive(i < starCount);
            }
        }
    }

    // EN: Toggles the selection highlight frame on this spin slot.
    // VI: Bật hoặc tắt khung sáng chọn trên ô quay này.
    public void SetHighlight(bool active)
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(active);
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

