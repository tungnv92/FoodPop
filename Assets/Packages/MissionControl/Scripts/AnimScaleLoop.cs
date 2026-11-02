using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimScaleLoop : MonoBehaviour
{
    public Tween scaleTween;

    private void OnEnable()
    {
        scaleTween?.Kill();
        this.transform.localScale = Vector3.one;
        scaleTween = this.transform.DOScale(1.1f, 1f).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo);
    }
}
