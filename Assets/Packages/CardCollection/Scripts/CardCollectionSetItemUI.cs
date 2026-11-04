using System;
using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardCollectionSetItemUI : MonoBehaviour
{
    [Tooltip("Chỉ số bộ sưu tập mà ô này đại diện (0 đến 5).")]
    [SerializeField] private int collectionId;

    [Tooltip("Hình ảnh biểu tượng chủ đề của bộ sưu tập.")]
    [SerializeField] private Image iconImage;

    [Tooltip("Nhãn hiển thị tên chủ đề của bộ sưu tập (ví dụ: Treats, Pumpkins).")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("Nhãn hiển thị tiến độ thu thập thẻ của bộ (ví dụ: 0/9).")]
    [SerializeField] private TMP_Text progressText;

    [Tooltip("Thanh tiến độ thu thập thẻ của bộ sưu tập.")]
    [SerializeField] private Image progressFillImage;

    [Tooltip("Biểu tượng chấm đỏ thông báo khi bộ sưu tập hoàn thành và có thể nhận thưởng.")]
    [SerializeField] private GameObject claimableNotiObject;

    [Tooltip("Dấu tích hiển thị khi bộ sưu tập đã nhận thưởng hoàn tất.")]
    [SerializeField] private GameObject completedCheckObject;

    private Action<int> _onClickCallback;

    public int CollectionId => collectionId;

    // EN: Initializes the collection item button with its collection index, display data, and click callback.
    // VI: Khởi tạo nút bộ sưu tập với chỉ số bộ, dữ liệu hiển thị và hàm callback khi nhấn.
    public void Setup(int id, Action<int> onClickCallback)
    {
        collectionId = id;
        _onClickCallback = onClickCallback;
        RefreshUI();
    }

    // EN: Updates the collection name, icon, card count (X/9), progress bar, and notification badge from CardCollectionManager.
    // VI: Cập nhật tên bộ sưu tập, biểu tượng, số lượng thẻ (X/9), thanh tiến độ và chấm thông báo từ CardCollectionManager.
    public void RefreshUI()
    {
        CardCollectionManager manager = CardCollectionManager.Instance;
        if (manager == null || manager.config == null) return;

        CardCollectionSetInfo setInfo = manager.config.GetCollectionInfo(collectionId);
        if (setInfo != null)
        {
            if (titleText != null) titleText.text = setInfo.collectionName;
            if (iconImage != null && setInfo.collectionIcon != null)
            {
                iconImage.sprite = setInfo.collectionIcon;
            }
        }

        int owned = manager.GetOwnedCountInCollection(collectionId);
        int total = CardCollectionManager.CARDS_PER_COLLECTION;

        if (progressText != null)
        {
            progressText.text = $"{owned}/{total}";
        }

        if (progressFillImage != null)
        {
            progressFillImage.fillAmount = total > 0 ? (float)owned / total : 0f;
        }

        bool canClaim = manager.CanClaimCollectionReward(collectionId);
        bool isClaimed = manager.Data != null && manager.Data.IsCollectionClaimed(collectionId);

        if (claimableNotiObject != null) claimableNotiObject.SetActive(canClaim);
        if (completedCheckObject != null) completedCheckObject.SetActive(isClaimed);
    }

    // EN: Invokes the click callback to open the Collection Album popup for this collection.
    // VI: Gọi hàm callback khi nhấn để mở popup Album chi tiết cho bộ sưu tập này.
    public void OnClickSet()
    {
        if (_onClickCallback != null)
        {
            _onClickCallback.Invoke(collectionId);
        }
        else if (UIManager.Ins != null)
        {
            UIManager.Ins.OpenPopup(PopupId.CardCollectionAlbum, collectionId);
        }
    }
}

