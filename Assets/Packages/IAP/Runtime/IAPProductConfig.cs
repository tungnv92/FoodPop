using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

namespace GamePackages
{
    // EN: Kind of IAP pack; decides the ownership a purchase grants (Remove Ads, Starter Pack) and which prefab displays the pack.
    // VI: Loại gói IAP; quyết định quyền sở hữu khi mua (Xóa quảng cáo, Starter Pack) và prefab nào hiển thị gói.
    public enum ProductPackType
    {
        RemoveAds,
        RemoveAdsPack,
        SmallPack,
        MediumPack,
        LargePack,
        CoinPack,
        StarterPack,
        RevivePack,
        BoosterPack,
    }

    // EN: Config of one IAP product: pack type and name, store id, product type and price, and the rewards of that product only.
    // VI: Config của một sản phẩm IAP: loại và tên gói, id store, loại sản phẩm và giá, và phần thưởng riêng của sản phẩm đó.
    [CreateAssetMenu(fileName = "IAPProductConfig", menuName = "GamePackages/IAP/IAPProductConfig")]
    public sealed class IAPProductConfig : ScriptableObject
    {
        [Tooltip("Loại gói; RemoveAds/RemoveAdsPack xóa quảng cáo, StarterPack đánh dấu đã mua Starter Pack.")]
        public ProductPackType packType;
        [Tooltip("Tên gói hiển thị cho người chơi.")]
        public string packName;
        [Tooltip("Id sản phẩm IAP trên store, nhập tay đúng như khai báo trên store.")]
        public string productId;
        [Tooltip("Loại sản phẩm IAP: Consumable mua nhiều lần, NonConsumable mua một lần.")]
        public ProductType productType;
        [Tooltip("Giá gói (USD), hiện khi store chưa sẵn sàng.")]
        public float price;
        [Tooltip("Danh sách phần thưởng của riêng gói này, theo thứ tự hiển thị.")]
        public List<ResourceGame> rewards = new List<ResourceGame>();

        // EN: True for packs that remove ads.
        // VI: Đúng với các gói xóa quảng cáo.
        public bool RemovesAds => packType == ProductPackType.RemoveAds || packType == ProductPackType.RemoveAdsPack;
    }
}
