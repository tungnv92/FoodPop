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
    [Tooltip("Ô cảnh báo mất chuỗi thắng (WinStreak đã gỡ, luôn ẩn).")]
    [SerializeField] private GameObject _loseStreakObj;
    [SerializeField] private GameObject _loseLavaObj;
    [SerializeField] private GameObject _loseTowerObj;
    LoseType _reason;
    // EN: Opens the legacy confirmation popup with every feature-warning slot hidden.
    // VI: Mở popup xác nhận cũ với toàn bộ ô cảnh báo tính năng được ẩn.
    public override void Open(object args = null)
    {
        base.Open(args);
        _openAnim.InitAndPlay();
        _reason = (LoseType)(int.Parse(args.ToString()));
        _quitButton.SetActive(_reason == LoseType.QuitGame);
        _retryButton.SetActive(_reason == LoseType.Replay);
        UIManager.Ins.GetPanel().SetActiveLife(true);

        _loseLavaObj.SetActive(false);
        _loseStreakObj.SetActive(false);
        _loseTowerObj.SetActive(false);
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

    // EN: Confirms quitting: closes the popup and spends a heart through IHeart (skipped without the HeartSystem package).
    // VI: Xác nhận thoát: đóng popup và trừ một mạng qua IHeart (bỏ qua khi không có package HeartSystem).
    public void QuitButton()
    {
        //SDKBridge.ShowInter(GamePlacement.PauseSetting, InterPlacement.BackHome, GameplayManager.Ins.currentLevel, successCallback: () =>
        //{
        //    AudioManager.Ins.PlaySound(SoundType.PopupHide);
        //    _closeAnim.Play(() =>
        //    {
        //        Close();
        //        GameplayManager.Ins.OnQuitLevelBackHome();
        //    });
        //});

        _closeAnim.Play(() =>
        {
            Close();
            ProfileManager.instance.Features.Get<IHeart>()?.UseHeart();
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
