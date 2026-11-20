using DG.Tweening;
//using Spine;
//using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

public class LavaQuestMileStone : MonoBehaviour
{
    public RectTransform rectTransform;
    public RectTransform targetJumpRectTF;
    public GameObject tireObj;
    //public SkeletonGraphic skeletonGraphic;

    public void Refresh(bool isShow, bool isAnim)
    {
        ShowTire(isShow, isAnim);
    }

    public void ShowTire(bool isShow, bool isAnim)
    {
        if (isShow)
        {
            if (tireObj != null)
            {
                tireObj.transform.localScale = Vector3.one;
            }
            if (isAnim)
            {
                Init();
            }
        }
        else
        {
            if (isAnim)
            {
                Done();
            }
            else
            {
                if (tireObj != null)
                {
                    tireObj.transform.localScale = Vector3.zero;
                }
            }
        }
    }

    // EN: Plays the milestone jump animation.
    // VI: Phát hiệu ứng nhảy tại mốc LavaQuest.
    public void Jump()
    {
        //if (skeletonGraphic == null) return;
        //skeletonGraphic.Initialize(true);
        //if (skeletonGraphic.AnimationState != null)
        //{
        //    skeletonGraphic.AnimationState.SetAnimation(0, "jump", false);
        //}
    }

    // EN: Initializes the milestone idle animation state.
    // VI: Khởi tạo trạng thái hiệu ứng nghỉ cho mốc LavaQuest.
    public void Init()
    {
        //if (skeletonGraphic == null) return;
        //skeletonGraphic.Initialize(true);
        //if (skeletonGraphic.AnimationState != null)
        //{
        //    skeletonGraphic.AnimationState.SetAnimation(0, "idle", true);
        //}
    }

    // EN: Plays the milestone completion/lose animation.
    // VI: Phát hiệu ứng kết thúc tại mốc LavaQuest.
    public void Done()
    {
        //if (skeletonGraphic != null)
        //{
        //    skeletonGraphic.Initialize(true);
        //    if (skeletonGraphic.AnimationState != null)
        //    {
        //        skeletonGraphic.AnimationState.SetAnimation(0, "lose", false);
        //    }
        //}
    }
}
