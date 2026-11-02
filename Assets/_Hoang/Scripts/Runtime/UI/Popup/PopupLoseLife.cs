using GamePackages;
using Longvm.UIAnimator;
using UnityEngine;
using UnityEngine.UI;

public enum LoseType
{
    QuitGame,
    OutOfSlot,
    Replay,
    OutOfMove,
    None
}
public class PopupLoseLife : Popup
{
    [SerializeField] private UIAnimator _openAnim;
    [SerializeField] private UIAnimator _closeAnim;

    [SerializeField] private GameObject _quitButton;
    [SerializeField] private GameObject _retryButton;
    [Space]
    [SerializeField] private GameObject _loseLifeObj;

    LoseType _reason;
    public override void Open(object args = null)
    {
        base.Open(args);
        _openAnim.InitAndPlay();
        _reason = (LoseType)(int.Parse(args.ToString()));
        _quitButton.SetActive(_reason == LoseType.QuitGame);
        _retryButton.SetActive(_reason == LoseType.Replay);
        UIManager.Ins.GetPanel().SetActiveLife(true);

        GameFeatureContext context = ProfileManager.instance.GameFeatureContext;
        bool canLoseHeart = !context.HeartManager.IsUnlimitedLives();

        _loseLifeObj.SetActive(canLoseHeart);
    }

    public override void Close()
    {
        base.Close();
    }

    #region UI Events
    public void CloseButton()
    {
        _closeAnim.Play(Close);
        //AudioManager.Ins.PlaySound(SoundType.PopupHide);
        UIManager.Ins.GetPanel().SetActiveLife(false);
    }

    public void QuitButton()
    {
        GameFeatureContext context = ProfileManager.instance.GameFeatureContext;
        bool canLoseStreak = context.WinStreakManager.IsCanLoseStreak();
        bool canFailLavaQuest = context.LavaQuestManager.CanFailProgress;
        bool canLoseTower = context.TowerRiseManager.ShouldWarningWhenLose();

        if (canLoseStreak || canFailLavaQuest || canLoseTower)
        {
            UIManager.Ins.OpenPopup(PopupId.LoseEvent, (int)_reason);
            CloseButton();
        }
        else
        {
            _closeAnim.Play(() =>
            {
                Close();
                ProfileManager.instance.GameFeatureContext.HeartManager.UseHeart();
                //GameplayManager.Ins.OnQuitLevelBackHome();
            });

            //SDKBridge.ShowInter(GamePlacement.PauseSetting, InterPlacement.BackHome, GameplayManager.Ins.currentLevel, successCallback: () =>
            //{
            //    AudioManager.Ins.PlaySound(SoundType.PopupHide);
            //    _closeAnim.Play(() =>
            //    {
            //        Close();
            //        ProfileManager.instance.GameFeatureContext.HeartManager.UseHeart();
            //        GameplayManager.Ins.OnQuitLevelBackHome();
            //    });
            //});          
        }
    }

    public void RetryButton()
    {
        GameFeatureContext context = ProfileManager.instance.GameFeatureContext;
        bool canLoseStreak = context.WinStreakManager.IsCanLoseStreak();
        bool canFailLavaQuest = context.LavaQuestManager.CanFailProgress;
        bool canLoseTower = context.TowerRiseManager.ShouldWarningWhenLose();

        if (canLoseStreak || canFailLavaQuest || canLoseTower)
        {
            UIManager.Ins.OpenPopup(PopupId.LoseEvent, (int)_reason);
        }
        else
        {
            UIManager.Ins.ClosePopup(PopupId.Pause);
            UIManager.Ins.OpenPopup(PopupId.Lose, (int)_reason);
        }
        CloseButton();
    }
    #endregion
}
