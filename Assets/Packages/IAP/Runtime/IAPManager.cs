using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;

namespace GamePackages
{
    // EN: Where a purchase was started; used as the reward placement for tracking.
    // VI: Nơi bắt đầu mua; dùng làm vị trí nhận thưởng để thống kê.
    public enum LocationType
    {
        popup_shop,
        popup_remove_ads,
        popup_revive,
        popup_activate_pass,
        popup_beginner_pack,
        popup_more_booster,
        popup_journey_offer,
        popup_worldcup,
    }

    // EN: Persistent Unity IAP v5 manager: connects the store, fetches every IAPProductConfig product, buys a pack, grants its rewards and restores owned Remove Ads.
    // VI: Manager Unity IAP v5 xuyên scene: kết nối store, tải mọi sản phẩm từ IAPProductConfig, mua gói, cộng thưởng và khôi phục Xóa quảng cáo đã mua.
    public sealed class IAPManager : MonoBehaviour
    {
        public static IAPManager Instance { get; private set; }

        [Tooltip("Config của mọi gói IAP: id, loại sản phẩm và phần thưởng.")]
        [SerializeField] private List<IAPProductConfig> packConfigs = new List<IAPProductConfig>();
        [Tooltip("Lớp phủ chặn thao tác, hiện từ lúc bấm mua tới khi store trả kết quả.")]
        [SerializeField] private GameObject blocker;
        [Tooltip("Vòng loading xoay giữa lớp phủ.")]
        [SerializeField] private RectTransform spinner;
        [Tooltip("Số giây cho một vòng xoay của vòng loading.")]
        [SerializeField, Min(0.1f)] private float spinDuration = 1f;

        private StoreController storeController;
        private CrossPlatformValidator validator;
        private bool isReady;
        private bool isConnecting;
        private bool isPurchasing;
        private string purchasingProductId;
        private LocationType location;
        private UnityAction onSuccess;
        private UnityAction onFailed;

        #region Unity

        // EN: Keeps one persistent instance across scenes, hides the blocker and prepares its spinner.
        // VI: Giữ một instance duy nhất xuyên scene, ẩn lớp phủ và chuẩn bị vòng loading.
        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            blocker.SetActive(false);
            StartSpinner();
        }

        // EN: Creates the receipt validator and the store controller, registers its callbacks once and starts connecting; skipped in the Editor, where purchases are simulated.
        // VI: Tạo bộ kiểm tra receipt và store controller, đăng ký callback một lần và bắt đầu kết nối; bỏ qua trong Editor, nơi mua được giả lập.
        private void Start()
        {
            if (Application.isEditor) return;
            CreateValidator();
            storeController = UnityIAPServices.StoreController();
            RegisterCallbacks();
            StartCoroutine(ConnectCoroutine());
        }

        #endregion

        #region Public API

        // EN: Returns the store's localized price, or the config USD price while the store is not ready.
        // VI: Trả giá theo store, hoặc giá USD trong config khi store chưa sẵn sàng.
        public string GetPrice(IAPProductConfig config)
        {
            Product product = isReady ? storeController.GetProductById(config.productId) : null;
            return product != null ? product.metadata.localizedPriceString : $"${config.price:0.00}";
        }

        // EN: Buys a pack (granted at once in the Editor without the store); success runs after rewards are granted, failed runs when the store is not ready (a reconnect is started) or the purchase fails.
        // VI: Mua một gói (trong Editor trao ngay, không qua store); success chạy sau khi cộng thưởng, failed chạy khi store chưa sẵn sàng (sẽ bắt đầu kết nối lại) hoặc mua lỗi.
        public void Buy(IAPProductConfig config, LocationType location, UnityAction success = null, UnityAction failed = null)
        {
            if (isPurchasing) return;
            this.location = location;
            onSuccess = success;
            onFailed = failed;
            if (Application.isEditor)
            {
                GrantPack(config, "iap_" + location);
                success?.Invoke();
                return;
            }
            if (!isReady)
            {
                Debug.LogError("IAP not ready");
                if (!isConnecting) StartCoroutine(ConnectCoroutine());
                failed?.Invoke();
                return;
            }
            purchasingProductId = config.productId;
            SetPurchasing(true);
            storeController.PurchaseProduct(config.productId);
        }

        // EN: Restores App Store purchases behind the blocker, then refetches owned orders so OnPurchasesFetched re-applies Remove Ads; done reports success exactly once. Fails at once while another purchase runs, and starts a reconnect when the store is not ready; a disconnect during the restore also reports failure.
        // VI: Khôi phục giao dịch App Store sau lớp phủ, rồi tải lại đơn đã sở hữu để OnPurchasesFetched áp dụng lại Xóa quảng cáo; done báo kết quả đúng một lần. Báo thất bại ngay khi đang có giao dịch khác, và bắt đầu kết nối lại khi store chưa sẵn sàng; mất kết nối trong lúc khôi phục cũng báo thất bại.
        public void RestorePurchases(UnityAction<bool> done)
        {
            if (isPurchasing)
            {
                done(false);
                return;
            }
            if (!isReady)
            {
                if (!isConnecting && !Application.isEditor) StartCoroutine(ConnectCoroutine());
                done(false);
                return;
            }
            purchasingProductId = null;
            onSuccess = null;
            onFailed = () => done(false);
            SetPurchasing(true);
            storeController.RestoreTransactions((success, error) =>
            {
                if (!isPurchasing) return;
                SetPurchasing(false);
                if (success) storeController.FetchPurchases();
                else Debug.LogError("IAP restore failed: " + error);
                done(success);
            });
        }

        #endregion

        #region Initialization

        // EN: Builds the local receipt validator from the obfuscated Tangle data. Google Play is validated only once GooglePlayTangle holds the real license key; until then Android receipts are accepted with a warning. Apple only needs AppleTangle on StoreKit 1, since StoreKit 2 is verified by Apple.
        // VI: Tạo bộ kiểm tra receipt cục bộ từ dữ liệu Tangle đã mã hóa. Google Play chỉ được kiểm tra khi GooglePlayTangle đã có License key thật; trước đó receipt Android được chấp nhận kèm cảnh báo. Apple chỉ cần AppleTangle khi dùng StoreKit 1, vì StoreKit 2 đã được Apple xác thực.
        private void CreateValidator()
        {
            if (!GooglePlayTangle.IsPopulated && Application.platform == RuntimePlatform.Android)
                Debug.LogWarning("IAP: GooglePlayTangle has no license key, Google Play receipts are not validated.");
            byte[] googleKey = GooglePlayTangle.IsPopulated ? GooglePlayTangle.Data() : null;
            validator = new CrossPlatformValidator(googleKey, AppleTangle.Data(), Application.identifier, Application.identifier);
        }

        // EN: Loops the spinner rotation in real time so it keeps turning while the game is paused; it plays only while the blocker is shown.
        // VI: Xoay vòng loading liên tục theo thời gian thực để vẫn quay khi game pause; chỉ chạy khi lớp phủ đang hiện.
        private void StartSpinner()
        {
            spinner.DOLocalRotate(new Vector3(0f, 0f, 360f), spinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1)
                .SetUpdate(true)
                .SetLink(spinner.gameObject, LinkBehaviour.PauseOnDisablePlayOnEnable);
        }

        // EN: Subscribes to every store event this manager handles.
        // VI: Đăng ký mọi sự kiện store mà manager xử lý.
        private void RegisterCallbacks()
        {
            storeController.OnStoreDisconnected += OnStoreDisconnected;
            storeController.OnProductsFetched += OnProductsFetched;
            storeController.OnProductsFetchFailed += OnProductsFetchFailed;
            storeController.OnPurchasesFetched += OnPurchasesFetched;
            storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            storeController.OnPurchasePending += OnPurchasePending;
            storeController.OnPurchaseDeferred += OnPurchaseDeferred;
            storeController.OnPurchaseFailed += OnPurchaseFailed;
        }

        // EN: Connects the store, then fetches the configured products; used at startup and to reconnect after a disconnect.
        // VI: Kết nối store rồi tải các sản phẩm trong config; dùng lúc khởi động và để kết nối lại sau khi mất kết nối.
        private IEnumerator ConnectCoroutine()
        {
            isConnecting = true;
            var connect = storeController.Connect();
            yield return new WaitUntil(() => connect.IsCompleted);
            isConnecting = false;
            if (connect.IsFaulted)
            {
                Debug.LogError("IAP connect failed");
                yield break;
            }
            FetchProducts();
        }

        // EN: Requests one store product per pack config.
        // VI: Yêu cầu một sản phẩm store cho mỗi config gói.
        private void FetchProducts()
        {
            var products = new List<ProductDefinition>();
            foreach (IAPProductConfig config in packConfigs)
            {
                products.Add(new ProductDefinition(config.productId, config.productType));
            }
            storeController.FetchProducts(products);
        }

        #endregion

        #region Store Callbacks

        // EN: Marks the store not ready and fails the purchase or restore in progress through its own failure callback; the next Buy starts a reconnect.
        // VI: Đánh dấu store chưa sẵn sàng và báo lỗi giao dịch hoặc lần khôi phục đang chạy qua đúng callback lỗi của nó; lần Buy sau sẽ bắt đầu kết nối lại.
        private void OnStoreDisconnected(StoreConnectionFailureDescription failure)
        {
            Debug.LogError("IAP store disconnected: " + failure.message);
            isReady = false;
            if (!isPurchasing) return;
            SetPurchasing(false);
            onFailed?.Invoke();
        }

        // EN: Marks the store ready and fetches owned purchases for restore.
        // VI: Đánh dấu store sẵn sàng và tải giao dịch đã sở hữu để khôi phục.
        private void OnProductsFetched(List<Product> products)
        {
            isReady = true;
            storeController.FetchPurchases();
        }

        // EN: Logs a product fetch failure.
        // VI: Ghi log khi tải sản phẩm lỗi.
        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogError("IAP products fetch failed: " + failure.FailureReason);
        }

        // EN: Restores Remove Ads from confirmed store orders with a valid receipt, without granting pack rewards again.
        // VI: Khôi phục Xóa quảng cáo từ đơn đã xác nhận có receipt hợp lệ, không cộng lại phần thưởng gói.
        private void OnPurchasesFetched(Orders orders)
        {
            PlayerDataManager playerData = ProfileManager.instance.PlayerDataManager;
            foreach (var order in orders.ConfirmedOrders)
            {
                if (!IsValidReceipt(order.Info.Receipt)) continue;
                foreach (var info in order.Info.PurchasedProductInfo)
                {
                    IAPProductConfig config = GetPackConfig(info.productId);
                    if (config != null && config.RemovesAds && !playerData.HasRemoveAds)
                        playerData.OnBoughtRemoveAds();
                }
            }
        }

        // EN: Logs a restore failure.
        // VI: Ghi log khi khôi phục giao dịch lỗi.
        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            Debug.LogError("IAP restore failed: " + failure.Message);
        }

        // EN: Rejects an order with an invalid receipt (not granted, not confirmed, the current purchase fails). Otherwise grants and saves the order before confirming it with the store, so a crash in between lets the store deliver it again. Only the order of the product being bought ends the current purchase and runs its success callback; an older order replayed by the store is granted with the "iap_pending" placement.
        // VI: Từ chối đơn có receipt không hợp lệ (không trao, không xác nhận, giao dịch hiện tại báo lỗi). Nếu hợp lệ thì trao và lưu trước khi xác nhận với store, để nếu crash giữa chừng store sẽ giao lại. Chỉ đơn của sản phẩm đang mua mới kết thúc giao dịch hiện tại và gọi callback thành công; đơn cũ store giao lại được trao với placement "iap_pending".
        private void OnPurchasePending(PendingOrder order)
        {
            string productId = order.Info.PurchasedProductInfo.Count > 0 ? order.Info.PurchasedProductInfo[0].productId : null;
            IAPProductConfig config = productId != null ? GetPackConfig(productId) : null;
            bool isCurrent = isPurchasing && productId == purchasingProductId;
            if (!IsValidReceipt(order.Info.Receipt))
            {
                if (!isCurrent) return;
                SetPurchasing(false);
                onFailed?.Invoke();
                return;
            }
            if (config != null)
            {
                ProfileManager.instance.Features.Get<IRecordTracking>()?.RecordIAP();
                GrantPack(config, isCurrent ? "iap_" + location : "iap_pending");
            }
            else
            {
                Debug.LogError("IAP order has no product config: " + productId);
            }
            storeController.ConfirmPurchase(order);
            if (!isCurrent) return;
            SetPurchasing(false);
            onSuccess?.Invoke();
        }

        // EN: Unlocks buying while a payment waits for approval (Ask to Buy, pending payment); the store delivers it later through OnPurchasePending.
        // VI: Mở khóa mua khi thanh toán đang chờ duyệt (Ask to Buy, thanh toán chờ); store sẽ giao đơn sau qua OnPurchasePending.
        private void OnPurchaseDeferred(DeferredOrder order)
        {
            Debug.Log("IAP purchase deferred");
            SetPurchasing(false);
        }

        // EN: Clears the purchasing state and reports the failure to the caller.
        // VI: Xóa trạng thái đang mua và báo lỗi cho nơi gọi.
        private void OnPurchaseFailed(FailedOrder failure)
        {
            Debug.LogError("IAP purchase failed: " + failure.FailureReason);
            SetPurchasing(false);
            onFailed?.Invoke();
        }

        #endregion

        #region Purchase Helpers

        // EN: Marks a store purchase as running or finished and shows or hides the blocker with it.
        // VI: Đánh dấu giao dịch store đang chạy hoặc đã xong và bật/tắt lớp phủ cùng lúc.
        private void SetPurchasing(bool purchasing)
        {
            isPurchasing = purchasing;
            blocker.SetActive(purchasing);
        }

        // EN: Validates a store receipt locally; Google Play receipts are accepted without checking until GooglePlayTangle holds the real license key.
        // VI: Kiểm tra receipt store cục bộ; receipt Google Play được chấp nhận không kiểm tra cho tới khi GooglePlayTangle có License key thật.
        private bool IsValidReceipt(string receipt)
        {
            if (Application.platform == RuntimePlatform.Android && !GooglePlayTangle.IsPopulated) return true;
            try
            {
                validator.Validate(receipt);
                return true;
            }
            catch (IAPSecurityException e)
            {
                Debug.LogError("IAP invalid receipt: " + e);
                return false;
            }
        }

        // EN: Finds the pack config with the given product id, or null.
        // VI: Tìm config gói theo id sản phẩm, trả null nếu không có.
        private IAPProductConfig GetPackConfig(string productId)
        {
            return packConfigs.Find(config => config.productId == productId);
        }

        // EN: Applies the ownership of the pack type (Remove Ads, Starter Pack) and grants its rewards; each grant is saved by PlayerDataManager.
        // VI: Áp dụng quyền sở hữu theo loại gói (Xóa quảng cáo, Starter Pack) và cộng phần thưởng; mỗi lần cộng đều được PlayerDataManager lưu.
        private void GrantPack(IAPProductConfig config, string placement)
        {
            PlayerDataManager playerData = ProfileManager.instance.PlayerDataManager;
            if (config.RemovesAds) playerData.OnBoughtRemoveAds();
            if (config.packType == ProductPackType.StarterPack) playerData.OnBoughtStarterPack();
            GrantRewards(playerData, config.rewards, placement);
        }

        // EN: Grants each reward with the given tracking placement: Coin, unlimited lives, or a booster.
        // VI: Cộng từng phần thưởng kèm placement thống kê được truyền vào: Coin, mạng vô hạn hoặc booster.
        private void GrantRewards(PlayerDataManager playerData, List<ResourceGame> rewards, string placement)
        {
            foreach (ResourceGame reward in rewards)
            {
                switch (reward.type)
                {
                    case ResourceType.Coin:
                        playerData.OnAddCoin(reward.value, placement);
                        break;
                    case ResourceType.UnlimitedLives:
                        playerData.AddUnlimitedLiveTime(reward.value, placement);
                        break;
                    default:
                        playerData.OnAddBooster(reward.type, reward.value, placement);
                        break;
                }
            }
        }

        #endregion
    }
}
