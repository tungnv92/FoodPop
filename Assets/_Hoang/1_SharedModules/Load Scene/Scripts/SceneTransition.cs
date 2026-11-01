using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] CanvasScaler scaler;
    [SerializeField] Transform holeTf;
    [SerializeField] CanvasGroup progressCanvasGroup;
    [SerializeField] CanvasGroup bgCanvasGroup;
    [SerializeField] Image fillImage;
    [SerializeField] RectTransform spineRectTf;
    private void Start()
    {
        float baseRatio = 1080f / 1920;
        float screenRatio = Screen.width * 1f / Screen.height;
        scaler.matchWidthOrHeight = 1;
    }

    public void Close(SceneId targetScene, Action onClosed = null)
    {
        gameObject.SetActive(true);
        progressCanvasGroup.DOFade(1, 0.25f);
        bgCanvasGroup.DOFade(1, 0.25f).OnComplete(() =>
        {
            onClosed.Invoke();
            //AudioManager.Ins.PlaySound(SoundType.Loading);
        });

        //holeTf.DOScale(0, 0.5f).OnComplete(() =>
        //{
        //    onClosed.Invoke();
        //    progressCanvasGroup.DOFade(1, 0.1f);
        //    AudioManager.Ins.PlaySound(SoundType.Loading);

        //});
    }

    public void Open()
    {
        progressCanvasGroup.DOFade(0, 0.25f);
        bgCanvasGroup.DOFade(0, 0.25f).OnComplete(() =>
        {
            gameObject.SetActive(false);
            SetProgress(0);
        });
        //holeTf.DOScale(3, 0.5f).OnComplete(() => gameObject.SetActive(false));
    }

    public void SetProgress(float progress)
    {
        fillImage.fillAmount = progress;
        spineRectTf.ChangeAnchorPosX((progress - 0.5f) * 356 * 2);
    }
}
