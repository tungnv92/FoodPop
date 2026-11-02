using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Noti : MonoBehaviour
{
    public Tween scaleTween;
    public Sequence rotateSequence;
    public float rotateAngle = 10f;
    public float rotateDuration = 0.15f;
    public float rotateDelay = 2f;


    private void OnEnable()
    {
        Loop();
    }

    public void Loop()
    {
        StartCoroutine(IELoop());
        IEnumerator IELoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(rotateDelay);
                DoAnim();
                yield return new WaitForSeconds(rotateDelay);
            }
        }
    }

    [Button]
    public void DoAnim()
    {
        Resett();
        Anim();
    }

    public void Resett()
    {
        scaleTween?.Kill();
        rotateSequence?.Kill();
        this.transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
    }

    public void Anim()
    {
        //scale
        //scaleTween = this.transform.DOScale(1.1f, rotateDuration * 5f).SetEase(Ease.OutSine).SetLoops(2, LoopType.Yoyo);
        //rotate
        rotateSequence = DOTween.Sequence();
        rotateSequence.Append(transform.DORotate(new Vector3(0, 0, -rotateAngle), rotateDuration)
            .SetEase(Ease.OutSine))
            .Append(transform.DORotate(new Vector3(0, 0, rotateAngle), rotateDuration * 2f)
            .SetEase(Ease.InOutSine)
            .SetLoops(4, LoopType.Yoyo))
            .Append(transform.DORotate(Vector3.zero, rotateDuration)
            .SetEase(Ease.OutSine));

    }
}
