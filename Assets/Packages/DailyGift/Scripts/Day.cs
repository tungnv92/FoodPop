using DG.Tweening;
using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HomeFeatures.DailyLogin
{
    public class Day : MonoBehaviour
    {
        [SerializeField] RectTransform myRt;
        [SerializeField] TMP_Text dayText;
        [SerializeField] GameObject disableGo;
        [SerializeField] RewardUI[] rewards;
        [SerializeField] GameObject highlight_go;

        PopupDailyLogin mainPopup;
        public int day { get; private set; }
        Tween highlightTween;

        private void OnValidate()
        {
            myRt = transform as RectTransform;
        }

        private void OnDisable()
        {
            if (highlightTween != null)
            {
                highlightTween.Kill();
                myRt.localScale = Vector3.one;
                highlightTween = null;
            }
        }

        public void Init(int day, DailyGiftRewardStage rewardSetConfig, PopupDailyLogin popup)
        {
            //myRt.localScale = Vector3.one;
            this.day = day;
            dayText.text = "Day " + day;
            for (int i = 0; i < rewards.Length && i < rewardSetConfig.gifts.Count; i++)
            {
                rewards[i].SetReward(rewardSetConfig.gifts[i]);
            }

            mainPopup = popup;
        }

        public void Enable(bool enable)
        {
            for (int i = 0; i < rewards.Length; i++)
                rewards[i].EnableEffect();

            disableGo.SetActive(!enable);
            highlight_go.SetActive(false);
            highlightTween?.Kill();
        }

        public virtual void Highlight()
        {
            //myRt.localScale = Vector3.one;
            highlightTween = myRt.DOScale(1.05f, 0.75f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
            highlight_go.SetActive(true);
        }

        #region UI Events
        //public void OnClickClaim()
        //{
        //    if (mainPopup != null)
        //    {
        //        Enable(false);
        //        mainPopup.closeAnimator.Play();
        //        mainPopup.Claim();
        //    }
        //}
        #endregion
    }
}
