using DG.Tweening;
using GamePackages;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionControlInfo : Popup
{
    public List<GameObject> objs = new();
    public GameObject continueTmp;
    public Action OnClose = () => { };

    bool isShowingAnim;
    Coroutine showingRoutine;
    // EN: Opens the view using manager queries and commands instead of mutable save models.
    // VI: Mở giao diện bằng truy vấn và lệnh manager thay vì model lưu có thể sửa.
    public override void Open(object args = null)
    {
        base.Open(args);
        AnimShow();
        if (continueTmp != null) continueTmp.SetActive(false);

        MissionControlManager.Instance.MarkInfoShown();
    }

    public void AnimShow()
    {
        if (objs != null)
        {
            foreach (var obj in objs)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        showingRoutine = StartCoroutine(IEAnimShow());
        IEnumerator IEAnimShow()
        {
            isShowingAnim = true;
            if (objs != null)
            {
                foreach (var obj in objs)
                {
                    if (obj != null)
                    {
                        obj.SetActive(true);
                        obj.transform.localScale = Vector3.one * 0.3f;
                        obj.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
                        yield return new WaitForSeconds(0.25f);
                    }
                }
            }

            if (continueTmp != null)
            {
                continueTmp.SetActive(true);
                continueTmp.transform.localScale = Vector3.one * 0.3f;
                continueTmp.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }

            isShowingAnim = false;
            yield return null;
        }
    }

    public void ButtonContinue()
    {
        if (isShowingAnim)
        {
            isShowingAnim = false;
            StopCoroutine(showingRoutine);
            foreach (var obj in objs)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    obj.transform.localScale = Vector3.one;
                }
            }

            continueTmp.SetActive(true);
            continueTmp.transform.localScale = Vector3.one;
            return;
        }

        OnClose?.Invoke();
        OnClose = null;
        Close();
    }
}

public class PopupMissionControlInfo : MissionControlInfo
{
}
