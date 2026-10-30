using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ToggleButton : MonoBehaviour
{
    [SerializeField] private Image enabledImg;
    [SerializeField] private RectTransform handleRectTf;
    [SerializeField] private float handleDisablePosX = -34f;

    protected bool _isEnabled;

    public virtual void InitState()
    {
        SetUIState(_isEnabled);
        //SetLogicState(_isEnabled);
    }
    public virtual void SetLogicState(bool isEnable)
    {

    }

    public void SetUIState(bool isEnable)
    {
        handleRectTf.DOAnchorPosX((isEnable) ? - handleDisablePosX : handleDisablePosX, 0.15f);
        enabledImg.DOFade(isEnable ? 1 : 0, 0.15f);
    }

    public void OnClick()
    {
        _isEnabled = !_isEnabled;
        SetUIState(_isEnabled);
        SetLogicState(_isEnabled);
    }
}
