using GamePackages;
using Longvm.UIAnimator;
using UnityEngine;

public class PopupPause : Popup
{
    [SerializeField] UIAnimator openAnim;
    [SerializeField] UIAnimator closeAnim;
    [SerializeField] ToggleButton[] buttons;
    [SerializeField] GameObject homeButton;
    public override void Open(object args = null)
    {
        base.Open(args);
        openAnim.InitAndPlay();
        foreach (var button in buttons)
            button.InitState();

        homeButton.SetActive(GameManager.Ins.Config.CanBackHome());
    }

    public override void Close()
    {
        base.Close();
    }

    #region UI Events
    public void ClickCloseButton()
    {
        //AudioManager.Ins.PlaySound(SoundType.PopupHide);
        closeAnim.Play(Close);
    }

    public void ClickHomeButton()
    {
        //if (!GameplayManager.Ins.IsMoved)
        //{
        //    AudioManager.Ins.PlaySound(SoundType.PopupHide);
        //    closeAnim.Play(() =>
        //    {
        //        Close();
        //        GameplayManager.Ins.OnQuitLevelBackHome(false);
        //    });
        //} else
        {
            GameFeatureContext context = ProfileManager.instance.GameFeatureContext;
            bool canLoseHeart = !context.HeartManager.IsUnlimitedLives();
            bool canLoseStreak = context.WinStreakManager.IsCanLoseStreak();
            bool canFailLavaQuest = context.LavaQuestManager.CanFailProgress;

            if (canLoseHeart)
            {
                UIManager.Ins.OpenPopup(PopupId.LoseLife, (int)LoseType.QuitGame);
            }
            else if (canLoseStreak || canFailLavaQuest)
            {
                UIManager.Ins.OpenPopup(PopupId.LoseEvent, (int)LoseType.QuitGame);
            }
            else
            {
                //SDKBridge.ShowInter(GamePlacement.PauseSetting, InterPlacement.BackHome, GameplayManager.Ins.currentLevel, successCallback: () =>
                //{
                //    AudioManager.Ins.PlaySound(SoundType.PopupHide);
                //    closeAnim.Play(() =>
                //    {
                //        Close();
                //        GameplayManager.Ins.OnQuitLevelBackHome(true);
                //    });
                //});

                closeAnim.Play(() =>
                {
                    Close();
                    //GameplayManager.Ins.OnQuitLevelBackHome(true);
                });
            }
        }

    }

    public void ClickRetryButton()
    {
        //if (!GameplayManager.Ins.IsMoved)
        //{
        //    AudioManager.Ins.PlaySound(SoundType.PopupHide);
        //    closeAnim.Play(() =>
        //    {
        //        Close();
        //        GameplayManager.Ins.OnReplayGame(false);
        //    });
        //} else
        {
            GameFeatureContext context = ProfileManager.instance.GameFeatureContext;
            bool canLoseHeart = !context.HeartManager.IsUnlimitedLives();
            bool canLoseStreak = context.WinStreakManager.IsCanLoseStreak();
            bool canFailLavaQuest = context.LavaQuestManager.CanFailProgress;
            bool shouldShowWarning = canLoseHeart || canLoseStreak || canFailLavaQuest;

            if (canLoseHeart)
            {
                UIManager.Ins.OpenPopup(PopupId.LoseLife, (int)LoseType.Replay);
            }
            else if (canLoseStreak || canFailLavaQuest)
            {
                UIManager.Ins.OpenPopup(PopupId.LoseEvent, (int)LoseType.Replay);
            }
            else
            {
                //SDKBridge.ShowInter(GamePlacement.PauseSetting, InterPlacement.Retry, GameplayManager.Ins.currentLevel, successCallback: () =>
                //{
                //    AudioManager.Ins.PlaySound(SoundType.PopupHide);
                //    closeAnim.Play(() =>
                //    {
                //        Close();
                //        GameplayManager.Ins.OnReplayGame(true);
                //    });
                //});

                closeAnim.Play(() =>
                {
                    Close();
                    //GameplayManager.Ins.OnReplayGame(true);
                });
            }
        }
    }
    #endregion
}
