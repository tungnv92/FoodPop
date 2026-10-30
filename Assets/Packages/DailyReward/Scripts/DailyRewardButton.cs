using GamePackages;
using UnityEngine;

public class DailyRewardButton : MonoBehaviour
{
    [SerializeField] GameObject notiGo;
    DailyRewardManager DailyRewardManager=>ProfileManager.instance.GameFeatureContext.DailyRewardManager;
    public void OnClick()
    {
        UIManager.Ins.OpenPopup(PopupId.DailyReward);
    }
    private void Update()
    {
        bool canCollect = DailyRewardManager.IsGoodToClaim();
        notiGo.SetActive(canCollect);
    }

    public bool CheckSuggest()
    {
        return DailyRewardManager.CheckSuggest();
    }
}
