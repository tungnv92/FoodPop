
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace MonetToolkit
{
    public class ButtonPurchaseSale : ButtonPurchase
    {
        [SerializeField, ValueDropdown("ids")] string default_id;
        [SerializeField] TMP_Text default_priceText;

        public override void Init()
        {
            base.Init();
            default_priceText.text = IAPManager.Ins.GetLocalizedPriceString(default_id);
        }
    }
}

