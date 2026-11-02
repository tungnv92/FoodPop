using GamePackages;
using Longvm.UIAnimator;
using UnityEngine;

public class PopupLose : Popup
{
    [SerializeField] UIAnimator openAnim;
    [SerializeField] UIAnimator closeAnim;
    [SerializeField] GameObject backHomeButton;
    [SerializeField] RectTransform _safeAreaRectTf;
    LoseType _loseType;
    public override void Open(object args = null)
    {
        base.Open(args);
        if (args == null) args = (int)LoseType.OutOfSlot;
        _loseType = (LoseType)(int.Parse(args.ToString()));

        //UIManager.Ins.GetPanel<GamePanel>().LifeDisplay.gameObject.SetActive(true);
        //UIManager.Ins.GetPanel<GamePanel>().LifeDisplay.transform.SetParent(_safeAreaRectTf);
        openAnim.InitAndPlay();
        ProfileManager.instance.GameFeatureContext.HeartManager.UseHeart();
        //AudioManager.Ins.PlaySound(SoundType.Lose);
        backHomeButton.SetActive(GameManager.Ins.Config.CanBackHome());
    }

    public override void Close()
    {
        base.Close();
    }

    public void OnClickRetry()
    {
        //if (!NetworkManager.Ins.IsInternetAvailable())
        //{
        //    UIManager.Ins.OpenPopup(PopupId.NoInternet);
        //    return;
        //}

        if (!ProfileManager.instance.GameFeatureContext.HeartManager.IsEnoughHeartToPlay())
        {
            UIManager.Ins.OpenPopup(PopupId.MoreLife);
            return;
        }

        //SDKBridge.ShowInter(GamePlacement.EndGame, InterPlacement.Retry, GameplayManager.Ins.currentLevel, successCallback: () =>
        //{
        //    GameplayManager.Ins.OnReplayGame(_loseType == LoseType.Replay);
        //});
    }

    public void OnClickHome()
    {
        //if (!NetworkManager.Ins.IsInternetAvailable())
        //{
        //    UIManager.Ins.OpenPopup(PopupId.NoInternet);
        //    return;
        //}

        //SDKBridge.ShowInter(GamePlacement.EndGame, InterPlacement.Lose, GameplayManager.Ins.currentLevel, successCallback: () =>
        //{
        //    GameplayManager.Ins.OnQuitLevelBackHome(_loseType == LoseType.Replay);
        //});
    }
}
