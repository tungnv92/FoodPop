using System.Collections;
using Longvm.RewardAnimator;
using SharedModules.ED;
using UnityEngine;

namespace MonetToolkit
{
    public class Shop : MonoBehaviour
    {
        [SerializeField] GameObject removeAdsBanner_Go;
        [SerializeField] GameObject starterPackGo;
        //[SerializeField] GameObject masterPassPackGo;
        [SerializeField] GameObject removeAdsGo;
        //[SerializeField] LayoutGroup layoutGroup;
        //[SerializeField] ContentSizeFitter contentSizeFitter;
        [SerializeField] RewardAnimator rewardAnimator;
        [SerializeField] Pack[] packs;

        IAPData iAPData;

        private void OnDisable()
        {
            EventDispatcher.UnregisterListener(EventId.PurchaseRemoveAds, OnRemoveAdsPurchased);
            EventDispatcher.UnregisterListener(EventId.PurchaseStarterPack, OnStarterPackPurchased);
            EventDispatcher.UnregisterListener(EventId.ActivateMasterPass, OnMasterPassPurchased);
        }

        public void Init()
        {
            EventDispatcher.RegisterListener(EventId.PurchaseRemoveAds, OnRemoveAdsPurchased);
            EventDispatcher.RegisterListener(EventId.PurchaseStarterPack, OnStarterPackPurchased);
            EventDispatcher.RegisterListener(EventId.ActivateMasterPass, OnMasterPassPurchased);

            iAPData = IAPManager.Ins.Data;

            if (rewardAnimator != null)
            {
                IAPManager.Ins.SetRewardAnimator(rewardAnimator);
            }
            else
            {
                IAPManager.Ins.SetRewardAnimator(UIManager.Ins.GetPanel().RewardAnimator);
            }

            for (int i = 0; i < packs.Length; i++)
            {
                packs[i].Init();
            }

            DisplayObject();
        }

        public void DisplayObject(bool showFull = false)
        {
            removeAdsGo.SetActive(!IAPManager.Ins.RemoveAdsPurchased());
            starterPackGo.SetActive(!iAPData.StarterPackPurchased/* && IAPManager.Ins.StarterPackEnabled()*/);
            //masterPassPackGo.SetActive(!MasterPassManager.Ins.Data.Activated && MasterPassManager.Ins.IsEnabled());
            removeAdsBanner_Go.SetActive((removeAdsGo.activeInHierarchy));
        }

        void OnRemoveAdsPurchased(object args)
        {
            DisableAndReorder(removeAdsGo);
        }

        void OnStarterPackPurchased(object args)
        {
            DisableAndReorder(starterPackGo);
        }

        void OnMasterPassPurchased(object args)
        {
            //DisableAndReorder(masterPassPackGo);
        }

        void DisableAndReorder(params GameObject[] go)
        {
            StartCoroutine(DisableAndReorderCoroutine(go));
        }

        IEnumerator DisableAndReorderCoroutine(params GameObject[] gameObjects)
        {
            //layoutGroup.enabled = true;
            //contentSizeFitter.enabled = true;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                gameObjects[i].SetActive(false);
            }

            removeAdsBanner_Go.SetActive(removeAdsGo.activeInHierarchy);
            yield return null;
            //contentSizeFitter.enabled = false;
            //layoutGroup.enabled = false;
        }

        #region UI Events
        public void OnClickRemoveAds()
        {
            UIManager.Ins.OpenPopup(PopupId.RemoveAds);
        }
        #endregion
    }
}
