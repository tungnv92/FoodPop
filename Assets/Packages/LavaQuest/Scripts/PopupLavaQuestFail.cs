using Longvm.UIAnimator;
//using Spine.Unity;
using UnityEngine;

public class PopupLavaQuestFail : Popup
{
    [SerializeField] UIAnimator openAnim;
    [SerializeField] UIAnimator closeAnim;
    //[SerializeField] SkeletonGraphic animSpine;

    // EN: Opens the LavaQuest fail popup and plays the entry animation.
    // VI: Mở popup thất bại LavaQuest và phát hiệu ứng xuất hiện.
    public override void Open(object args = null)
    {
        base.Open(args);
        openAnim.InitAndPlay();

        //if (animSpine == null) return;
        //animSpine.Initialize(true);
        //if (animSpine.AnimationState != null)
        //{
        //    animSpine.AnimationState.SetAnimation(0, "Start", false);
        //    animSpine.AnimationState.AddAnimation(0, "Idle", true, 0f);
        //}
    }

    // EN: Plays the close animation and closes both the fail popup and the main LavaQuest popup.
    // VI: Phát hiệu ứng đóng và đóng cả popup thất bại lẫn popup LavaQuest chính.
    public void ContinueButton()
    {
        //AudioManager.Ins.PlaySound(SoundType.PopupHide);
        closeAnim.Play(Close);
        UIManager.Ins.ClosePopup(PopupId.LavaQuest);
    }
}
