using System;
using Crystal;
using DG.Tweening;
using UnityEngine;

public class HandTutAnim : MonoBehaviour
{
    [SerializeField] RectTransform handTF;
    [SerializeField] GameObject touch_go;
    [SerializeField] Animator handAnim;
    [SerializeField] GameObject altButton;

    Action onClickDelegate;
    public void ShowDragAnim(Vector2 srcAnchor, Vector2 targetAnchor)
    {
        handAnim.enabled = false;
        touch_go.SetActive(false);
        handTF.anchoredPosition = srcAnchor;
        handTF.gameObject.SetActive(true);

        handTF.DOAnchorPos(targetAnchor, 1f).OnComplete(() => ShowDragAnim(srcAnchor, targetAnchor));
    }

    public void ShowTouchAnim(Vector3 targetPos, Vector2 offset, Action onClickAction = null)
    {
        handTF.position = targetPos;
        handTF.anchoredPosition += offset;
        handTF.gameObject.SetActive(true);
        handAnim.enabled = true;
        handAnim.SetTrigger("touch");

        if (onClickAction != null)
        {
            onClickDelegate = onClickAction;
            altButton.SetActive(true);
        }
        else
        {
            altButton.SetActive(false);
        }
    }

    public void ShowAtAnchor(Vector2 targetAnchor, Action onClickAction = null)
    {
        Debug.Log("Target Anchor: " +  targetAnchor);
        handTF.anchoredPosition = targetAnchor;
        handTF.gameObject.SetActive(true);
        handAnim.enabled = true;
        handAnim.SetTrigger("touch");

        if (onClickAction != null)
        {
            onClickDelegate = onClickAction;
            altButton.SetActive(true);
        }
        else
        {
            altButton.SetActive(false);
        }
    }

    public void HideHandAnim()
    {
        handTF.DOKill();
        handTF.gameObject.SetActive(false);
        handAnim.enabled = false;
    }

    #region UI Events
    public void OnClickAlter()
    {
        onClickDelegate?.Invoke();
        onClickDelegate = null;
    }
    #endregion
}
