using GamePackages;
using TMPro;
using UnityEngine;

namespace HomeFeatures.DailyLogin
{
    public class RewardUI : MonoBehaviour
    {
        [SerializeField] TMP_Text amountText;
        [SerializeField] GameObject effect;
        [SerializeField] GameObject[] rewardIcon;

        public void SetReward(ResourceGame rw_config)
        {
            foreach (var item in rewardIcon)
                item.SetActive(false);

            rewardIcon[(int)rw_config.type].SetActive(true);
            amountText.text = rw_config.GetNormalValue();
        }

        public void EnableEffect()
        {
            effect.SetActive(true);
        }
    }
}
