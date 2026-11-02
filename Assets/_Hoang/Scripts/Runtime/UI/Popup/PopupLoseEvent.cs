using GamePackages;
using Longvm.UIAnimator;
using UnityEngine;
using UnityEngine.UI;

public class PopupLoseEvent : Popup
{
    [SerializeField] private UIAnimator _openAnim;
    [SerializeField] private UIAnimator _closeAnim;

    [SerializeField] private GameObject _quitButton;
    [SerializeField] private GameObject _retryButton;
    [Space]
    [SerializeField] private GameObject _loseStreakObj;
    [SerializeField] private GameObject _loseLavaObj;
    [SerializeField] private GameObject _loseTowerObj;
    [SerializeField] private Image _loseStreakImage;
    [SerializeField] private Sprite[] _loseStreakSprites;
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
        bool canLoseStreak = context.WinStreakManager.IsCanLoseStreak();
        bool canFailLavaQuest = context.LavaQuestManager.CanFailProgress;
        bool canLoseTower = context.TowerRiseManager.ShouldWarningWhenLose();
        _loseLavaObj.SetActive(canFailLavaQuest);
        _loseStreakObj.SetActive(canLoseStreak);
        _loseTowerObj.SetActive(canLoseTower);
        if (canLoseStreak)
        {
            _loseStreakImage.sprite = _loseStreakSprites[context.WinStreakManager.WinStreakTier];
        }
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

        _closeAnim.Play(() =>
        {
            Close();
            ProfileManager.instance.GameFeatureContext.HeartManager.UseHeart();
        });
    }

    public void RetryButton()
    {
        _closeAnim.Play(Close);
        UIManager.Ins.ClosePopup(PopupId.Pause);
        //AudioManager.Ins.PlaySound(SoundType.PopupHide);
        UIManager.Ins.OpenPopup(PopupId.Lose, (int)_reason);
    }
    #endregion
}
