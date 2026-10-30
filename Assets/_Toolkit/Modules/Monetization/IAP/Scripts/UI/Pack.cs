using TMPro;
using UnityEngine;

namespace MonetToolkit
{
    public class Pack : MonoBehaviour
    {
        //[SerializeField] RectTransform _transform;
        //public new RectTransform transform => _transform;
        [SerializeField] protected ButtonPurchase buttonPurchase;
        [SerializeField] ProductConfigContainer productConfigContainer;
        [SerializeField] TMP_Text[] rewardAmountTexts;

        public virtual void Init()
        {
            buttonPurchase.Init();
            if (rewardAmountTexts.IsNullOrEmpty()) return;
            ProductConfig productConfig = productConfigContainer.GetProductConfig(buttonPurchase.Id);
            if (productConfig == null || productConfig.RewardConfigs.IsNullOrEmpty()) return;
            for (int i = 0; i < rewardAmountTexts.Length; i++)
            {
                rewardAmountTexts[i].text = productConfig.RewardConfigs[i].GetAmountString();
            }
        }
    }
}
