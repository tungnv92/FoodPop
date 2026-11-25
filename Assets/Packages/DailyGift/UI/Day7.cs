using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HomeFeatures.DailyLogin
{
    // EN: Day 7 slot: its rewards sit inside a box whose lid stays closed until day seven arrives (today or already claimed).
    // VI: Ô Day 7: phần thưởng nằm trong hộp có nắp, nắp đóng tới khi đến ngày thứ bảy (hôm nay hoặc đã nhận).
    public class Day7 : Day
    {
        [Tooltip("Nắp hộp quà che phần thưởng; ẩn khi đến ngày 7.")]
        [SerializeField] GameObject lid;
        [Tooltip("Các ảnh icon phần thưởng trong hộp, theo thứ tự phần thưởng của config Day 7.")]
        [SerializeField] Image[] rewardIcons;
        [Tooltip("Các chữ số lượng tương ứng với từng icon phần thưởng.")]
        [SerializeField] TMP_Text[] rewardAmounts;

        // EN: Shows the slot as available or claimed; the lid stays closed only while day seven has not been reached.
        // VI: Hiện ô ở trạng thái còn nhận hoặc đã nhận; nắp chỉ đóng khi chưa tới ngày thứ bảy.
        public override void Enable(bool enable)
        {
            base.Enable(enable);
            lid.SetActive(enable);
        }

        // EN: Marks Day 7 as today's gift and opens the lid to reveal its rewards.
        // VI: Đánh dấu Day 7 là quà hôm nay và mở nắp để lộ phần thưởng.
        public override void Highlight()
        {
            base.Highlight();
            lid.SetActive(false);
        }

        // EN: Day 7's label is the Tag image in the prefab, so no text is written.
        // VI: Nhãn Day 7 là ảnh Tag trong prefab nên không ghi chữ.
        protected override void ShowLabel(int day)
        {
        }

        // EN: Fills one icon and amount per configured reward, icons looked up by resource type; unused slots are hidden.
        // VI: Điền một icon và số lượng cho mỗi phần thưởng trong config, icon tra theo loại tài nguyên; ô thừa bị ẩn.
        protected override void ShowReward(DailyGiftRewardStage rewardStage)
        {
            DailyGiftRewardConfig config = DailyGiftFeature.Instance.Config;
            for (int i = 0; i < rewardIcons.Length; i++)
            {
                bool used = i < rewardStage.gifts.Count;
                rewardIcons[i].transform.parent.gameObject.SetActive(used);
                if (!used) continue;
                ResourceGame gift = rewardStage.gifts[i];
                rewardIcons[i].sprite = config.GetResourceIcon(gift.type);
                rewardAmounts[i].text = gift.GetNormalValue();
            }
        }

        // EN: Day 7 has no separate highlight background; opening the lid and the pulse mark it as today.
        // VI: Day 7 không có nền nổi bật riêng; việc mở nắp và nhịp phóng to đánh dấu ngày hôm nay.
        protected override void SetHighlightVisible(bool visible)
        {
        }
    }
}
