using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardCollectionRewardUI : MonoBehaviour
{
    [Tooltip("Hình ảnh biểu tượng của phần thưởng.")]
    [SerializeField] Image rewardIcon;

    [Tooltip("Nhãn hiển thị số lượng hoặc thời lượng của phần thưởng.")]
    [SerializeField] TMP_Text amountText;

    // EN: Populates the reward icon and amount text from the given ResourceGame configuration.
    // VI: Hiển thị biểu tượng và số lượng phần thưởng từ cấu hình ResourceGame được truyền vào.
    public void SetReward(ResourceGame rw_config)
    {
        rewardIcon.sprite = rw_config.GetRewardSprite();
        amountText.text = rw_config.GetNormalValue();
    }
}
