using Sirenix.OdinInspector;
using UnityEngine.Purchasing;

namespace MonetToolkit
{
    [System.Serializable]
    public class ProductConfig
    {
        static string[] ids = ProductId.Ids;

        [ValueDropdown("ids")]
        public string Id;
        public ProductType ProductType;
        [TableList]
        public RewardConfig[] RewardConfigs;
    }
}