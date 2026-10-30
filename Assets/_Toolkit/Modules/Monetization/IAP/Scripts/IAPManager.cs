//using Firebase.Analytics;
using GamePackages;
using HomeFeatures.Profile;
using Longvm.RewardAnimator;
//using SDK;
using SharedModules.ED;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.Core.Environments;

using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using static UnityEngine.Rendering.DebugUI;

namespace MonetToolkit
{
    public class IAPManager : SingletonMonoBehaviour<IAPManager>
    {
        public bool isLogEvent = true;
        const string Key = "IAP";

        [SerializeField] bool purchaseButtonClicked;
        [SerializeField] bool isTestWithoutStoreSetup;
        [SerializeField] GameObject loading;
        [SerializeField, ExposedScriptableObject] ProductConfigContainer container;
        [SerializeField] IAPData data;
        //[SerializeField] AdsManager _adsManager;
        public IAPData Data => data;
        [SerializeField] IAPConfig config;
        public IAPConfig Config => config;
        bool isSuggestStarterPack;

        public bool GameSceneEntered;
        bool isFirstBackHome;
        //Data gameData => DataManager.Ins.Data;

#if UNITY_EDITOR
        const string environment = "test";
#else
        const string environment = "production";
#endif

        bool unityServicesInitialized;
        bool initialized;
        bool dataLoaded;
        LocationType locationType;
        StoreController storeController;
        //IExtensionProvider extensions;
        RewardAnimator rewardAnimator;
        bool isHome => LoadSceneManager.Ins.CurrentSceneId == SceneId.Home;
        bool isGame => LoadSceneManager.Ins.CurrentSceneId == SceneId.Game;

        public bool IsPurchasing { get; private set; }
        private string purchasingProductId;
        PlayerDataManager PlayerDataManager => ProfileManager.instance.PlayerDataManager;
        async void Start()
        {
            await InitializeUnityServices();
        }

        public void Init()
        {
            LoadData();
            Observer.OnNewday += OnNewDay;
            StartCoroutine(InitializeCoroutine());
        }

        async Task InitializeUnityServices()
        {
            try
            {
                if (isTestWithoutStoreSetup) return;
                var options = new InitializationOptions().SetEnvironmentName(environment);
                await UnityServices.InitializeAsync(options);
                unityServicesInitialized = true;
            } catch (Exception exception)
            {
                Debug.LogError($"Unity services failed to initialize. Exception: {exception}");
            }
        }

        #region Initialization

        IEnumerator InitializeCoroutine()
        {
            yield return new WaitUntil(() => unityServicesInitialized);
            Debug.Log("IAP Initializing...");
            storeController = UnityIAPServices.StoreController();
            RegisterCallbacks();
            var connect = storeController.Connect();
            yield return new WaitUntil(() => connect.IsCompleted);
            if (connect.IsFaulted)
            {
                Debug.LogError("IAP Connect Failed");
                yield break;
            }
            initialized = true;
            FetchProducts();
        }

        void RegisterCallbacks()
        {
            storeController.OnStoreDisconnected += OnStoreDisconnected;
            storeController.OnProductsFetched += OnProductsFetched;
            storeController.OnProductsFetchFailed += OnProductsFetchFailed;
            storeController.OnPurchasesFetched += OnPurchasesFetched;
            storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            storeController.OnPurchasePending += OnPurchasePending;
            storeController.OnPurchaseFailed += OnPurchaseFailed;
        }
        void FetchProducts()
        {
            var products = new List<ProductDefinition>();
            AddProducts(products);
            storeController.FetchProducts(products);
            Debug.Log("FetchProducts: " + products.Count);
        }

        void AddProducts(List<ProductDefinition> products)
        {
            foreach (var product in container.Products)
            {
                products.Add(new ProductDefinition(product.Id, product.ProductType));
            }
        }

        #endregion

        #region LOAD & SAVE DATA
        public void LoadData()
        {
            try
            {
                if (dataLoaded) return;

                if (PlayerPrefs.HasKey(Key))
                {
                    string json = PlayerPrefs.GetString(Key);
                    data = JsonUtility.FromJson<IAPData>(json);
                } else
                {
                    data = new()
                    {
                        StarterPackFirstOffer = new SerializableDateTime(GameManager.Ins.Now.Date + TimeSpan.FromDays(100))
                    };
                    SaveData();
                }

                dataLoaded = true;
                isSuggestStarterPack = true;
                isFirstBackHome = true;
                GameSceneEntered = false;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                data = new()
                {
                    StarterPackFirstOffer = new SerializableDateTime(GameManager.Ins.Now.Date + TimeSpan.FromDays(100))
                };
                SaveData();
            }
        }

        [Button]
        public void SaveData()
        {
            try
            {
                if (!dataLoaded) return;
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
                PlayerPrefs.Save();
            } catch { }
        }
        #endregion

        #region Callbacks
        void OnProductsFetched(List<Product> products)
        {
            Debug.Log("Products Fetched");
            storeController.FetchPurchases();
        }

        void OnPurchasesFetched(Orders orders)
        {
            Debug.Log("OnPurchasesFetched");
            foreach (var order in orders.ConfirmedOrders)
            {
                foreach (var product in order.Info.PurchasedProductInfo)
                {
                    string productId = product.productId;
                    Debug.Log("Restored: " + productId);
                    // 🔥 tránh restore lại nhiều lần
                    if (IsAlreadyOwnedByLocal(productId))
                        continue;
                    if (!IsValidateReceipt(order.Info.Receipt))
                        return;
                    if (productId == ProductId.RemoveAds ||
                        //productId == ProductId.RemoveAds_5 ||
                        //productId == ProductId.RemoveAds_3 ||
                        productId == ProductId.RemoveAdsPack)
                    {
                        Debug.Log("[IAP] Restore Pack: " + productId);
                        //AdsManager.Ins.RemoveAds();
                        data.RemoveAdsPurchased = true;
                        SaveData();
                    }
                    SaveOwnerNoncomsume(productId);
                }
            }
        }
        bool IsAlreadyOwnedByLocal(string productId)
        {
            return PlayerPrefs.GetInt("IsOwned_" + productId, 0) == 1;
        }

        void SaveOwnerNoncomsume(string productId)
        {
            PlayerPrefs.SetInt("IsOwned_" + productId, 1);
            PlayerPrefs.Save();
        }
        public void OnStoreDisconnected(StoreConnectionFailureDescription des)
        {
            string errorMessage = $"IAP failed to initialize. Reason: {des.message}";
            Debug.LogError(errorMessage);
        }

        void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogError("Products Fetch Failed: " + failure.FailureReason);
        }

        void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            Debug.LogError("Restore Failed: " + failure.Message);
        }

        #endregion
        void OnPurchasePending(PendingOrder order)
        {
            if (order.Info.PurchasedProductInfo.Count == 0)
            {
                Debug.LogError("No product info");
                return;
            }

            IsPurchasing = false;
            EnableBlocker(false);

            string productId = order.Info.PurchasedProductInfo[0].productId;
            Product product = storeController.GetProductById(productId);

            if (product == null)
            {
                Debug.LogError("Product not found: " + productId);
                return;
            }

            if (IsValidateReceipt(order.Info.Receipt))
            {
                storeController.ConfirmPurchase(order);
                OnPurchaseConfirmed(productId);
#if UNITY_IOS
            bool usingStoreKit2 =
                !string.IsNullOrEmpty(order.Info.Apple?.jwsRepresentation);

            if (usingStoreKit2 && SDKBridge.IsFirebaseReady())
            {
                TrackFirebasePurchase(product, order);
            }
#endif
            }

        }
        private void TrackFirebasePurchase(Product product, PendingOrder order)
        {
            //try
            //{
            //    FirebaseAnalytics.LogEvent(
            //        FirebaseAnalytics.EventPurchase,
            //        new Parameter(FirebaseAnalytics.ParameterTransactionID,
            //            order.Info.TransactionID),

            //        new Parameter(FirebaseAnalytics.ParameterCurrency,
            //            product.metadata.isoCurrencyCode),

            //        new Parameter(FirebaseAnalytics.ParameterValue,
            //            (double)product.metadata.localizedPrice),

            //        new Parameter(FirebaseAnalytics.ParameterItemID,
            //            product.definition.id),

            //        new Parameter(FirebaseAnalytics.ParameterItemName,
            //            product.metadata.localizedTitle),

            //        new Parameter(FirebaseAnalytics.ParameterQuantity,
            //            1)
            //    );

            //    Debug.Log($"Firebase Purchase Logged: {product.definition.id}");
            //} catch (Exception e)
            //{
            //    Debug.LogException(e);
            //}
        }
        void OnPurchaseConfirmed(string productID)
        {
            Debug.LogError("Product confirm: " + productID);
            Product product = storeController.GetProductById(productID);
            decimal cost = product.metadata.localizedPrice;
            string currencyCode = product.metadata.isoCurrencyCode;
            string id = product.definition.id;
            string name = ProductId.NameById[id];
            string placement = locationType.ToString();
            //int revenue = (int)product.metadata.localizedPrice;

            if (isGame) placement = "ingame_" + placement;
            else if (isHome) placement = "home_" + placement;

            StartCoroutine(HandlePurchaseComplete(productID));
            ProfileManager.instance.GameFeatureContext.RecordManager.RecordIAP();
            int count = ProfileManager.instance.GameFeatureContext.RecordManager.GetIAPCount();
            string packName = "";
            //SDKBridge.TrackEventIAPPurchased(ProfileManager.instance.PlayerData.currentLevel, placement, productID, packName, count == 1, count);
            //if (isLogEvent)
            //{
            //    SDKBridge.TrackAppflyerPurchase(productID, cost, currencyCode);
            //    SDKBridge.TrackAppflyerPurchaseComfirmed(productID);
            //}
        }

        public void OnPurchaseFailed(FailedOrder order)
        {
            Debug.LogError($"Purchase failed. Product: {purchasingProductId}. Reason: {order.FailureReason.ToString()}");
            IsPurchasing = false;
            purchaseButtonClicked = false;
            EnableBlocker(false);
            UIManager.Ins.OpenPopup(PopupId.PurchaseFail);
        }

        #region Receipt Validation

        bool IsValidateReceipt(string receipt)
        {
#if UNITY_EDITOR
            return true;
#elif UNITY_ANDROID
            var validator = new CrossPlatformValidator(GooglePlayTangle.Data(), null, Application.identifier);
            try
            {
                var receipts = validator.Validate(receipt);
                return true;
            } catch (IAPSecurityException e)
            {
                Debug.LogError($"Validation failed: {e}");
                return false;
            }
#elif UNITY_IOS
        var validator = new CrossPlatformValidator(null, AppleTangle.Data(), Application.identifier);
        try
        {
            var receipts = validator.Validate(receipt);
            return true;
        }
        catch (IAPSecurityException e)
        {
            Debug.LogError($"Validation failed: {e}");
            return false;
        }
#endif
        }

        #endregion

        IEnumerator HandlePurchaseComplete(string id)
        {
            Debug.Log("Handle purchase complete: " + id);
            UIManager.Ins.OpenPopup(PopupId.PurchaseSuccess);

            ProductConfig productConfig = container.GetProductConfig(id);
            if (productConfig == null) yield break;
            if (!purchaseButtonClicked) yield break;

            switch (productConfig.Id)
            {
                case ProductId.RemoveAds or /*ProductId.RemoveAds_5 or ProductId.RemoveAds_3 or*/ ProductId.RemoveAdsPack:
                    data.RemoveAdsPurchased = true;
                    SaveData();
                    ProfileManager.instance.PlayerData.isRemoveAds = true;
                    EventDispatcher.DispatchEvent(EventId.PurchaseRemoveAds);
                    break;
                case ProductId.BeginnerPack:
                    data.StarterPackPurchased = true;
                    SaveData();
                    EventDispatcher.DispatchEvent(EventId.PurchaseStarterPack);
                    break;
                case ProductId.RevivePack:
                    EventDispatcher.DispatchEvent(EventId.PurchaseRevivePack);
                    data.RevivePackTriggerCountdown = 3;
                    SaveData();
                    break;
                case ProductId.BoosterPack:
                    UIManager.Ins.ClosePopup(PopupId.MoreBooster);
                    break;
            }

            if (!productConfig.RewardConfigs.IsNullOrEmpty())
            {
                purchaseButtonClicked = false;
                CollectRewards(productConfig.RewardConfigs);
                yield return new WaitUntil(() => !UIManager.Ins.IsPopupOpen(PopupId.PurchaseSuccess));
                CollectAnimation(productConfig.RewardConfigs, productConfig.Id);
            }
            purchaseButtonClicked = false;
        }

        public void SetRewardAnimator(RewardAnimator rewardAnimator)
        {
            this.rewardAnimator = rewardAnimator;
        }
        void CollectRewards(RewardConfig[] rewardConfigs)
        {
            for (int i = 0; i < rewardConfigs.Length; i++)
            {
                CollectReward(rewardConfigs[i]);
            }
        }

        void CollectReward(RewardConfig rewardConfig)
        {
            string place = "";
            if (isHome)
            {
                place = "iap_home";
            } else if (isGame)
            {
                place = "iap_ingame";
            }
            place += "_" + locationType.ToString();
            int amount = rewardConfig.Amount;
            switch (rewardConfig.Type)
            {
                case RewardType.Coin:
                    PlayerDataManager.OnAddCoin(amount, place);
                    break;
                case RewardType.UnlimitedLife:
                    PlayerDataManager.AddUnlimitedLiveTime(amount, place);
                    break;
                case RewardType.Booster1:
                    PlayerDataManager.OnAddBooster(ResourceType.BoosterAddSlot, amount, place);
                    break;
                case RewardType.Booster2:
                    PlayerDataManager.OnAddBooster(ResourceType.BoosterShuffle, amount, place);
                    break;
                case RewardType.Booster3:
                    PlayerDataManager.OnAddBooster(ResourceType.BoosterPickup, amount, place);
                    break;
                case RewardType.Booster4:
                    PlayerDataManager.OnAddBooster(ResourceType.BoosterSuperBear, amount, place);
                    break;
            }
        }

        void CollectAnimation(RewardConfig[] rewardConfigs, string id)
        {
            if (rewardAnimator == null) rewardAnimator = UIManager.Ins.GetPanel().RewardAnimator;
            StartCoroutine(CollectAnimationCoroutine(rewardConfigs, id));
        }

        IEnumerator CollectAnimationCoroutine(RewardConfig[] rewardConfigs, string id)
        {
            rewardAnimator.Init();
            for (int i = 0; i < rewardConfigs.Length; i++)
            {
                RewardType type = rewardConfigs[i].Type;
                int amount = rewardConfigs[i].Amount;
                string amountString = rewardConfigs[i].GetAmountString();
                rewardAnimator.Add((int)type, amount);
            }
            yield return null;
            rewardAnimator.OnComplete(() =>
            {
                for (int i = 0; i < rewardConfigs.Length; i++)
                {
                    RewardType type = rewardConfigs[i].Type;

                    switch (type)
                    {
                        case RewardType.UnlimitedLife:
                            break;
                        case RewardType.Booster1:
                            break;
                        case RewardType.Booster2:
                            break;
                        case RewardType.Booster3:
                            break;
                        case RewardType.Booster4:
                            break;
                    }
                }
            });
            rewardAnimator.Play();
        }

        public string GetLocalizedPriceString(string productId)
        {
            if (!initialized || storeController == null) return "---";
            Product product = storeController.GetProductById(productId);
            return product != null ? product.metadata.localizedPriceString : "---";
        }

        public void OnPurchaseClick(string productId, LocationType locationType)
        {
            //if (!NetworkManager.Ins.IsInternetAvailable())
            //{
            //    UIManager.Ins.OpenPopup(PopupId.NoInternet);
            //    return;
            //}
            if (isTestWithoutStoreSetup)
            {
                purchaseButtonClicked = true;
                StartCoroutine(HandlePurchaseComplete(productId));
                return;
            }
            if (!initialized || storeController == null) return;
            if (purchaseButtonClicked) return;

            EnableBlocker(true);
            purchaseButtonClicked = true;
            storeController.PurchaseProduct(productId);
            IsPurchasing = true;
            this.locationType = locationType;
            //SDKBridge.TrackEventIAPClick(ProfileManager.instance.PlayerData.currentLevel, locationType.ToString(), productId);

        }

        public void OnNewDay()
        {
            //data.StarterPackSuggested = false;
            data.IsSuggestedRemoveAds = false;
            data.IsFirstInterShownToday = false;
        }
        public void EnableBlocker(bool enable) => loading.SetActive(enable);

        public bool RemoveAdsPurchased() => data.RemoveAdsPurchased;

        public bool StarterPackEnabled()
        {
            return ProfileManager.instance.PlayerData.currentLevel >= config.StarterPackOfferLevel;
        }

        public bool StarterPackPurchased()
        {
            return data.StarterPackPurchased;
        }
        public bool CheckSuggestStarterPack()
        {
            if (!StarterPackEnabled() || !isFirstBackHome || data.StarterPackPurchased || !isSuggestStarterPack)
            {
                return false;
            }

            if (!GameSceneEntered) return false;
            isFirstBackHome = false;
            isSuggestStarterPack = false;

            if (data.IsFirstOfferStarterPack)
            {
                data.IsFirstOfferStarterPack = false;
                data.StarterPackFirstOffer = new SerializableDateTime(DateTime.Now);
            }
            SaveData();
            UIManager.Ins.OpenPopup(PopupId.StarterPack);
            UIManager.Ins.EnableBlocker(false);
            return true;
        }

        public bool CheckSuggestRemoveAds()
        {
            if (RemoveAdsPurchased()) return false;
            if (data.IsSuggestedRemoveAds) return false;
            if (!data.IsFirstInterShownToday) return false;

            UIManager.Ins.OpenPopup(PopupId.RemoveAds);

            data.IsSuggestedRemoveAds = true;
            SaveData();
            return true;
        }

        public void OnLoseLevel()
        {
            if (data.RevivePackTriggerCountdown > 0)
            {
                data.RevivePackTriggerCountdown--;
                SaveData();
            }
        }
    }
}
