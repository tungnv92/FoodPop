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
    // EN: Opens the lose-life popup; the lose-heart hint shows only when a heart would actually be spent (IHeart present and lives not unlimited).
    // VI: Mở popup mất mạng; gợi ý mất mạng chỉ hiện khi thật sự bị trừ mạng (có IHeart và không đang vô hạn).
    public override void Open(object args = null)
    {
        base.Open(args);
        _openAnim.InitAndPlay();
        _reason = (LoseType)(int.Parse(args.ToString()));
        _quitButton.SetActive(_reason == LoseType.QuitGame);
        _retryButton.SetActive(_reason == LoseType.Replay);
        UIManager.Ins.GetPanel().SetActiveLife(true);

        IHeart heart = ProfileManager.instance.Features.Get<IHeart>();
        bool canLoseHeart = heart != null && !heart.IsUnlimitedLives();

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

    // EN: Confirms quitting by closing the popup and spending a heart without a feature-warning popup.
    // VI: Xác nhận thoát bằng cách đóng popup và trừ mạng, không mở popup cảnh báo tính năng.
    public void QuitButton()
    {
        _closeAnim.Play(() =>
        {
            Close();
            ProfileManager.instance.Features.Get<IHeart>()?.UseHeart();
            //GameplayManager.Ins.OnQuitLevelBackHome();
        });
    }

    // EN: Closes Pause and opens Lose directly, then closes this popup without checking feature warnings.
    // VI: Đóng Pause và mở thẳng Lose rồi đóng popup hiện tại, không kiểm tra cảnh báo tính năng.
    public void RetryButton()
    {
        UIManager.Ins.ClosePopup(PopupId.Pause);
        UIManager.Ins.OpenPopup(PopupId.Lose, (int)_reason);
        CloseButton();
    }
    #endregion
}
