using GamePackages;
using TMPro;
using UnityEngine;

namespace HomeFeatures.DailyLogin
{
    public class ButtonDailyLogin : MonoBehaviour
    {
        DailyGiftFeature DailyGift => DailyGiftFeature.Instance;
        [SerializeField] GameObject lockedObject;
        [SerializeField] TMP_Text unlockLevelTMP;
        [SerializeField] GameObject unlockedObject;
        [SerializeField] GameObject notiGo;
        [SerializeField] TMP_Text countdown_text;

        // EN: Hides a disabled daily gift button and removes the obsolete level-lock display.
        // VI: Ẩn nút quà hằng ngày khi bị tắt và bỏ hiển thị khóa theo level cũ.
        private void Start()
        {
            bool enabled = DailyGift.IsEnabledFeature();
            gameObject.SetActive(enabled);
            lockedObject.SetActive(false);
            unlockLevelTMP.gameObject.SetActive(false);
            unlockedObject.SetActive(enabled);
            countdown_text.gameObject.SetActive(false);
        }

        // EN: Shows the notification dot while today's gift can be claimed.
        // VI: Hiện chấm thông báo khi quà hôm nay còn nhận được.
        private void Update()
        {
            bool canCollect = DailyGift.IsGoodToClaim();
            notiGo.SetActive(canCollect);
            //countdown_text.text = TimeUtil.TimeToNewDay(ProfileManager.instance.TimeManager.GetNow());
            //countdown_text.gameObject.SetActive(!canCollect);
        }

        // EN: Opens the daily login popup through the scene's FoodPop.UIManager.
        // VI: Mở popup đăng nhập hằng ngày qua FoodPop.UIManager của scene.
        public void OpenPopup()
        {
            FoodPop.UIManager.Instance.OpenUI<PopupDailyLogin>();
        }

        #region UI Events
        public void OnClick()
        {
            OpenPopup();
        }
        #endregion
    }
}
