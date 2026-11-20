using GamePackages;
using Longvm.RewardAnimator;
using Longvm.UIAnimator;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HomeFeatures.DailyLogin
{
    public class PopupDailyLogin : Popup
    {
        DailyGiftFeature DailyGift => DailyGiftFeature.Instance;
        DailyGiftRewardConfig DailyGiftRewardConfig => DailyGift.Config;
        PlayerDataManager PlayerDataManager => ProfileManager.instance.PlayerDataManager;
        [SerializeField] UIAnimator openAnimator;
        public UIAnimator closeAnimator;
        [SerializeField] GameObject next_day_text;
        [SerializeField] GameObject collectButton;
        [SerializeField] Day[] days;
        [SerializeField] private int currentDayID=1;
        //[SerializeField] GameObject tapToClaimButton;
        bool initialized;
        RewardAnimator rewardAnimator;
        bool isClaimed = false;
        bool IsLoadData;
        public override void Open(object args = null)
        {
            base.Open(args);
            IsLoadData = false;
            InitData();
            UpdateButtonStatus();
            openAnimator.Init();
            openAnimator.Play(() =>
            {
                LoadData();
            });
        }

        // EN: Handles the claim button click by disabling the current day slot, closing the popup, and granting rewards.
        // VI: Xử lý khi nhấn nút nhận thưởng bằng cách tắt ô ngày hiện tại, đóng popup và trao thưởng.
        public void ClaimButton()
        {
            if (!IsLoadData) return;
            //AudioManager.Ins.PlaySound(SoundType.PopupHide);
            days[currentDayID - 1].Enable(false);
            closeAnimator.Play(Close);
            OnClaimReward();
        }
        // EN: Marks today's gift as claimed, then plays the reward animation that adds each reward to the player.
        // VI: Đánh dấu đã nhận quà hôm nay, rồi phát hiệu ứng thưởng và cộng từng phần thưởng cho người chơi.
        public void OnClaimReward()
        {
            DailyGift.OnClaimComplete();
            if (rewardAnimator == null) return;
            rewardAnimator.Init();
            //string placement = GamePlacement.DailyGift.ToString();
            string placement = "daily_gift";
            int level = PlayerDataManager.GetCurrentLevel();
            List<ResourceGame> gifts = DailyGiftRewardConfig.GetRewardsByDay(currentDayID);
            gifts.ForEach(x =>
            {

                ResourceType type = x.type;
                int value = x.value;
                rewardAnimator.Add((int)type, value, onReceiveReward: () =>
                {
                    switch (type)
                    {
                        case ResourceType.Coin:
                            PlayerDataManager.OnAddCoin(value, placement);
                            break;
                        case ResourceType.BoosterClean:
                        case ResourceType.BoosterShuffle:
                        case ResourceType.BoosterPickup:
                        case ResourceType.BoosterAutoComplete:
                            PlayerDataManager.OnAddBooster(type, value, placement);
                            break;
                        case ResourceType.UnlimitedLives:
                            PlayerDataManager.AddUnlimitedLiveTime(value, placement);
                            break;
                    }
                });
            });
            rewardAnimator.Play();
            rewardAnimator.OnComplete(null);
            next_day_text.SetActive(true);
            collectButton.SetActive(false);
            UpdateButtonStatus();
        }

        void InitData()
        {
            if (!initialized)
            {
                rewardAnimator = UIManager.Ins.GetPanel().RewardAnimator;
                for (int i = 0; i < days.Length; i++)
                {
                    days[i].Init(i + 1, DailyGiftRewardConfig.GetRewardStage(i), this);
                }
                initialized = true;
            }
        }
        // EN: Shows the collect button while today's gift can be claimed, otherwise the next-day text.
        // VI: Hiện nút nhận khi quà hôm nay còn nhận được, nếu không thì hiện chữ chờ ngày mai.
        void UpdateButtonStatus()
        {
            bool isGoodToClaim = DailyGift.IsGoodToClaim();
            next_day_text.SetActive(!isGoodToClaim);
            collectButton.SetActive(isGoodToClaim);
        }
        // EN: Enables, disables or highlights each day slot from the current gift day and its claimed state.
        // VI: Bật, tắt hoặc làm nổi bật từng ô ngày theo ngày quà hiện tại và trạng thái đã nhận.
        void LoadData()
        {
            currentDayID = DailyGift.GetCurrentDayId ();
            for (int i = 0; i < days.Length; i++)
            {
                if (i < currentDayID - 1)
                {
                    days[i].Enable(false);
                } else if (i == currentDayID - 1)
                {
                    bool isClaimed = DailyGift.IsClaimed(currentDayID);
                    if (isClaimed)
                    {
                        days[i].Enable(false);
                    } else
                    {
                        days[i].Enable(true);
                        days[i].Highlight();
                    }
                } else
                {
                    days[i].Enable(true);
                }
            }
            IsLoadData = true;
        }

        #region UI Events
        // EN: Plays the close animation and closes the daily login popup.
        // VI: Phát hiệu ứng đóng và đóng popup đăng nhập hàng ngày.
        public void OnClickClose()
        {
            //AudioManager.Ins.PlaySound(SoundType.PopupHide);
            closeAnimator.Play(() =>
            {
                Close();
            });
        }


        #endregion
    }
}
