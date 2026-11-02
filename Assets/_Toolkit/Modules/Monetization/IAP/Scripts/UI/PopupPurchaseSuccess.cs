using System.Collections;
using Longvm.UIAnimator;
using UnityEngine;

public class PopupPurchaseSuccess : Popup
{
	[SerializeField] UIAnimator openAnimator;
	[SerializeField] UIAnimator closeAnimator;
    [SerializeField] float autoCloseDelay;

    IEnumerator autoCloseCoroutine;

    public override void Open(object args = null)
    {
        base.Open();
        openAnimator.InitAndPlay();
        autoCloseCoroutine = AutoCloseCoroutine();
        StartCoroutine(autoCloseCoroutine);
        //AudioManager.Ins.PlaySound(SoundType.PopupShow);
    }

    IEnumerator AutoCloseCoroutine()
    {
        //AudioManager.Ins.PlaySound(SoundType.PopupHide);
        yield return WaitFor.Seconds(autoCloseDelay);
        Close();
    }

    public override void Close()
    {
        closeAnimator.Play(() =>
        {
            base.Close();
        });
    }

    #region UI Events
    public void OnClickOk()
	{
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }
		Close();
	}
	#endregion
}
