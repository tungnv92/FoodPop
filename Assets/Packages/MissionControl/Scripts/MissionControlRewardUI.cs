using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionControlRewardUI : MonoBehaviour
{
    [SerializeField] Image rewardIcon;
    [SerializeField] TMP_Text rewardAmount;

    public void Init(Sprite rewardSprite, string amountString)
    {
        rewardIcon.sprite = rewardSprite;
        rewardAmount.text = amountString;
    }
}
