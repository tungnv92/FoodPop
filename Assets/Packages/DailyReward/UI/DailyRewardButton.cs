using GamePackages;
using UnityEngine;

public class DailyRewardButton : MonoBehaviour
{
    DailyRewardFeature DailyReward => DailyRewardFeature.Instance;
    [SerializeField] GameObject notiGo;
    // EN: Shows the notification dot while a daily reward can be claimed.
    // VI: Hiện chấm thông báo khi còn thưởng hằng ngày nhận được.
    private void Update()
    {
        bool canCollect = DailyReward.IsGoodToClaim();
        notiGo.SetActive(canCollect);
    }
    public void OnClick()
    {
        UIManager.Ins.OpenPopup(PopupId.DailyReward);
    }
}
