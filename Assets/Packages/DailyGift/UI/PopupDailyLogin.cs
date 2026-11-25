using GamePackages;
using System.Collections.Generic;
using UnityEngine;

namespace HomeFeatures.DailyLogin
{
    // EN: Daily login popup opened by FoodPop.UIManager; shows the seven gift days and grants today's gift on Claim.
    // VI: Popup đăng nhập hằng ngày do FoodPop.UIManager mở; hiện bảy ngày quà và trao quà hôm nay khi bấm Claim.
    public class PopupDailyLogin : FoodPop.UICanvasPrime
    {
        [Tooltip("Chữ hẹn quay lại ngày mai, hiện khi hôm nay đã nhận quà.")]
        [SerializeField] GameObject next_day_text;
        [Tooltip("Nút nhận quà hôm nay.")]
        [SerializeField] GameObject collectButton;
        [Tooltip("Bảy ô ngày theo thứ tự, ô cuối là Day 7.")]
        [SerializeField] Day[] days;

        DailyGiftFeature DailyGift => DailyGiftFeature.Instance;
        DailyGiftRewardConfig DailyGiftRewardConfig => DailyGift.Config;
        int currentDayID = 1;
        bool initialized;

        // EN: Caches the player profile used for haptics and rewards while the popup is visible.
        // VI: Cache hồ sơ người chơi dùng cho rung và phần thưởng khi popup đang hiện.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            CacheFeedbackManagers();
        }

        // EN: Activates the popup, fills every day slot once, refreshes claim state and plays the shared open animation.
        // VI: Bật popup, điền các ô ngày một lần, làm mới trạng thái nhận quà và chạy hiệu ứng mở dùng chung.
        public override void Open()
        {
            base.Open();
            InitData();
            UpdateButtonStatus();
            LoadData();
            AnimateOpen();
        }

        // EN: Handles Claim: greys out today's slot, grants the rewards and closes with the shared animation.
        // VI: Xử lý Claim: làm tối ô hôm nay, trao thưởng rồi đóng bằng hiệu ứng dùng chung.
        public void ClaimButton()
        {
            if (closing || !DailyGift.IsGoodToClaim()) return;
            PlayHaptic();
            days[currentDayID - 1].Enable(false);
            OnClaimReward();
            CloseWithAnimation();
        }

        // EN: Closes the popup with the shared exit animation.
        // VI: Đóng popup bằng hiệu ứng thoát dùng chung.
        public void OnClickClose()
        {
            if (closing) return;
            PlayHaptic();
            CloseWithAnimation();
        }

        // EN: Binds each day slot to its reward stage on first open.
        // VI: Gán mỗi ô ngày với mốc thưởng của nó ở lần mở đầu tiên.
        void InitData()
        {
            if (initialized) return;
            for (int i = 0; i < days.Length; i++)
                days[i].Init(i + 1, DailyGiftRewardConfig.GetRewardStage(i), this);
            initialized = true;
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
            currentDayID = DailyGift.GetCurrentDayId();
            for (int i = 0; i < days.Length; i++)
            {
                if (i < currentDayID - 1)
                {
                    days[i].Enable(false);
                }
                else if (i == currentDayID - 1)
                {
                    bool isClaimed = DailyGift.IsClaimed(currentDayID);
                    days[i].Enable(!isClaimed);
                    if (!isClaimed) days[i].Highlight();
                }
                else
                {
                    days[i].Enable(true);
                }
            }
        }

        // EN: Marks today's gift as claimed and grants every reward immediately.
        // VI: Đánh dấu đã nhận quà hôm nay và cộng ngay mọi phần thưởng.
        void OnClaimReward()
        {
            DailyGift.OnClaimComplete();
            List<ResourceGame> gifts = DailyGiftRewardConfig.GetRewardsByDay(currentDayID);
            foreach (ResourceGame gift in gifts)
                GrantReward(gift.type, gift.value);
            UpdateButtonStatus();
        }

        // EN: Adds one daily gift reward to the player's profile under the daily_gift placement.
        // VI: Cộng một phần thưởng quà hằng ngày vào hồ sơ người chơi với placement daily_gift.
        void GrantReward(ResourceType type, int value)
        {
            const string placement = "daily_gift";
            switch (type)
            {
                case ResourceType.Coin:
                    playerData.OnAddCoin(value, placement);
                    break;
                case ResourceType.BoosterClean:
                case ResourceType.BoosterShuffle:
                case ResourceType.BoosterPickup:
                case ResourceType.BoosterAutoComplete:
                    playerData.OnAddBooster(type, value, placement);
                    break;
                case ResourceType.UnlimitedLives:
                    playerData.AddUnlimitedLiveTime(value, placement);
                    break;
            }
        }
    }
}
