using Longvm.UIAnimator;
using MonetToolkit;
using UnityEngine;

public class PopupRemoveAdsPack : Popup
{
    [SerializeField] private UIAnimator _openAnim;
    [SerializeField] private UIAnimator _closeAnim;
    [SerializeField] private Pack _pack;
    [SerializeField] private GameObject _closeButton;

    public override void Open(object args = null)
    {
        base.Open(args);
        _openAnim.InitAndPlay();
        _pack.Init();
        _closeButton.SetActive(false);
        Invoke(nameof(ShowCloseButton), 1f);
    }

    public void ShowCloseButton()
    {
        _closeButton.SetActive(true);
    }
    public override void Close()
    {
        base.Close();
    }

    public void CloseButton()
    {
        //AudioManager.Ins.PlaySound(SoundType.PopupHide);
        _closeAnim.Play(Close);
    }
}
