using Longvm.UIAnimator;
using UnityEngine;

public class PopupSetting : Popup
{
    [SerializeField] UIAnimator openAnim;
    [SerializeField] UIAnimator closeAnim;
    [SerializeField] ToggleButton[] buttons;

    public override void Open(object args = null)
    {
        base.Open(args);
        openAnim.InitAndPlay();
        foreach (var button in buttons) 
            button.InitState();
    }

    public override void Close()
    {
        base.Close();
    }

    public void ClickCloseButton()
    {
        //AudioManager.Ins.PlaySound(SoundType.PopupHide);
        closeAnim.Play(Close);
    }
}
