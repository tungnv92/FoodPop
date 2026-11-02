using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace MonetToolkit
{
    public class ButtonPurchase : MonoBehaviour
    {
        static string[] ids = ProductId.Ids;
        [SerializeField, ValueDropdown("ids")] string id;
        public string Id => id;
        [SerializeField] LocationType locationType;
        [SerializeField] TMP_Text priceText;

        public virtual void Init()
        {
            priceText.text = IAPManager.Ins.GetLocalizedPriceString(id);
        }

        #region UI Events
        public void OnClick()
        {
            Debug.Log("on click");
            IAPManager.Ins.OnPurchaseClick(id, locationType);
        }
        #endregion
    }
}
