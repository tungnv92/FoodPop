using System;
using Longvm.UIAnimator;
using UnityEngine;

public class PopupNewVersion : Popup
{
    //[SerializeField] UIAnimator openAnimator;
    //[SerializeField] UIAnimator closeAnimator;

    //Action onClickLater; 
    //public override void Open(object args = null)
    //{
    //    base.Open();
    //    openAnimator.InitAndPlay();
    //    AudioManager.Ins.PlaySound(SoundType.PopupShow);
    //}

    //public override void Close()
    //{
    //    AudioManager.Ins.PlaySound(SoundType.PopupHide);
    //    closeAnimator.Play(() =>
    //    {
    //        base.Close();
    //    });
    //}

    //public void SetOnClickLater(Action action) => onClickLater = action;


    //#region UI Events
    //public void OnClickUpdate()
    //{
    //    //Close();
    //    string storeUrl = $"https://play.google.com/store/apps/details?id={Application.identifier}";

    //    Application.OpenURL(storeUrl);
    //}

    //public void OnClickLater()
    //{
    //    Close();
    //    onClickLater?.Invoke();
    //    onClickLater = null;
    //}
    //#endregion
}
