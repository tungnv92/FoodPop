using DG.Tweening;
using GamePackages;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PopupWinStreakInfo : Popup
{
    public List<GameObject> objs = new();
    public GameObject continueTmp;

    bool isShowingAnim = false;
    Coroutine showingRoutine;
    public override void Open(object args)
    {
        base.Open();
        AnimShow();
        if (continueTmp != null) continueTmp.SetActive(false);

        if (WinStreakManager.Instance != null && WinStreakManager.Instance.GetData() != null)
        {

        }
    }

    public void AnimShow()
    {
        foreach (var obj in objs)
        {
            if (obj != null) obj.SetActive(false);
        }
        showingRoutine = StartCoroutine(IEAnimShow());
        IEnumerator IEAnimShow()
        {
            isShowingAnim = true;
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
            if (continueTmp != null)
            {
                continueTmp.SetActive(true);
                continueTmp.transform.localScale = Vector3.one * 0.3f;
                continueTmp.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
            }

            isShowingAnim = false;
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

        Close();
    }
}

public class PopupTreasureInfo : PopupWinStreakInfo
{
}
