using DG.Tweening;
using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HomeFeatures.DailyLogin
{
    // EN: One daily login slot: shows its day label, the reward icon and amount loaded from DailyGiftRewardStage, and its claimed/current state.
    // VI: Một ô quà đăng nhập: hiện nhãn ngày, icon và số lượng thưởng nạp từ DailyGiftRewardStage, cùng trạng thái đã nhận/hôm nay.
    public class Day : MonoBehaviour
    {
        [Tooltip("RectTransform của ô, dùng cho hiệu ứng nhịp khi là ngày hôm nay.")]
        [SerializeField] RectTransform myRt;
        [Tooltip("Chữ tên ngày, ví dụ Day 1.")]
        [SerializeField] TMP_Text dayText;
        [Tooltip("Lớp phủ và dấu tick khi ngày đã nhận.")]
        [SerializeField] GameObject disableGo;
        [Tooltip("Nền nổi bật khi là ngày hôm nay.")]
        [SerializeField] GameObject highlight_go;
        [Tooltip("Ảnh icon phần thưởng, tra theo loại thưởng trong resourceIcons của DailyGiftRewardConfig.")]
        [SerializeField] protected Image rewardIcon;
        [Tooltip("Chữ số lượng phần thưởng, lấy từ phần thưởng đầu tiên trong config của ngày.")]
        [SerializeField] protected TMP_Text rewardAmount;

        public int day { get; private set; }
        Tween highlightTween;

        // EN: Fills the RectTransform reference from this object in the editor.
        // VI: Tự gán RectTransform của chính object này trong editor.
        private void OnValidate()
        {
            myRt = transform as RectTransform;
        }

        // EN: Stops the highlight pulse and restores the slot scale when hidden.
        // VI: Dừng nhịp nổi bật và trả lại scale của ô khi bị ẩn.
        private void OnDisable()
        {
            if (highlightTween != null)
            {
                highlightTween.Kill();
                myRt.localScale = Vector3.one;
                highlightTween = null;
            }
        }

        // EN: Sets the day label and shows this day's reward from its configured stage.
        // VI: Đặt nhãn ngày và hiện phần thưởng của ngày theo mốc đã cấu hình.
        public void Init(int day, DailyGiftRewardStage rewardStage, PopupDailyLogin popup)
        {
            this.day = day;
            ShowLabel(day);
            ShowReward(rewardStage);
        }

        // EN: Shows the slot as available (true) or claimed (false) and clears any highlight.
        // VI: Hiện ô ở trạng thái còn nhận (true) hoặc đã nhận (false) và tắt nổi bật.
        public virtual void Enable(bool enable)
        {
            disableGo.SetActive(!enable);
            SetHighlightVisible(false);
            highlightTween?.Kill();
        }

        // EN: Marks the slot as today's gift with the highlight visual and a looping scale pulse.
        // VI: Đánh dấu ô là quà hôm nay với hình nổi bật và nhịp phóng to lặp lại.
        public virtual void Highlight()
        {
            highlightTween = myRt.DOScale(1.05f, 0.75f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.Linear);
            SetHighlightVisible(true);
        }

        // EN: Writes the "Day N" label; slots whose label is part of the art override this.
        // VI: Ghi nhãn "Day N"; ô có nhãn nằm sẵn trong hình thì ghi đè hàm này.
        protected virtual void ShowLabel(int day)
        {
            dayText.text = "Day " + day;
        }

        // EN: Displays the first reward of the stage: its icon looked up by resource type in the config and its formatted amount (x1 for boosters).
        // VI: Hiện phần thưởng đầu tiên của mốc: icon tra theo loại tài nguyên trong config và số lượng đã định dạng (x1 với booster).
        protected virtual void ShowReward(DailyGiftRewardStage rewardStage)
        {
            ResourceGame gift = rewardStage.gifts[0];
            rewardIcon.sprite = DailyGiftFeature.Instance.Config.GetResourceIcon(gift.type);
            rewardAmount.text = gift.GetNormalValue();
        }

        // EN: Shows or hides the today highlight background; slots without one override this.
        // VI: Bật hoặc tắt nền nổi bật ngày hôm nay; ô không có nền này thì ghi đè hàm này.
        protected virtual void SetHighlightVisible(bool visible)
        {
            highlight_go.SetActive(visible);
        }
    }
}
