using GamePackages;
using TMPro;
using UnityEngine;

namespace HomeFeatures.DailyLogin
{
    public class ButtonDailyLogin : MonoBehaviour
    {
        [SerializeField] GameObject lockedObject;
        [SerializeField] TMP_Text unlockLevelTMP;
        [SerializeField] GameObject unlockedObject;
        [SerializeField] GameObject notiGo;
        [SerializeField] TMP_Text countdown_text;
        DailyGiftManager DailyGiftManager => ProfileManager.instance.GameFeatureContext.DailyGiftManager;
      
        private void Start()
        {
            lockedObject.SetActive(!DailyGiftManager.IsEnabledFeature());
            unlockLevelTMP.text = "Level " + DailyGiftManager.DailyGiftGlobalConfig.LevelActiveFeature;
            unlockedObject.SetActive(DailyGiftManager.IsEnabledFeature());
            countdown_text.gameObject.SetActive(false);
        }
   
        public void OpenPopup()
        {
            var popup = UIManager.Ins.OpenPopup<PopupDailyLogin>(PopupId.DailyLogin, this);
        }

        private void Update()
        {
            bool canCollect = DailyGiftManager.IsGoodToClaim();
            notiGo.SetActive(canCollect);
            //countdown_text.text = TimeUtil.TimeToNewDay(ProfileManager.instance.TimeManager.GetNow());
            //countdown_text.gameObject.SetActive(!canCollect);
        }

        public bool CheckSuggest()
        {
            return DailyGiftManager.CheckSuggest();
        }

        #region UI Events
        public void OnClick()
        {
            OpenPopup();
        }
        #endregion
    }
}
