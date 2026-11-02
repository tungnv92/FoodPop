using DG.Tweening;
using TMPro;
using UnityEngine;

public class ButtonNavigator : MonoBehaviour
{
    [SerializeField] int tabId;
    [SerializeField] ScrollSnap panelScroller;
    [SerializeField] RectTransform _rectTf;
    [SerializeField] RectTransform _iconRectTf;
    [SerializeField] TMP_Text _buttonName;
    [SerializeField] NavigationGroup _parentGroup;
    public RectTransform RectTf => _rectTf;

    [SerializeField] float _inActivePosY = -80;
    [SerializeField] float _activePosY = -30;

    [SerializeField] float _inactiveIconScale = 0.85f;
    [SerializeField] float _activeIconScale = 1f;
    public void OnClick()
    {
        panelScroller.SnapToPage(tabId);
    }

    public void OnDrag(Vector2 value)
    {
        float index = value.x * 2;
        float delta = -Mathf.Abs(tabId - index);
        delta = Mathf.Clamp(delta, -1, 0) + 1;  // 0 to 1

        _iconRectTf.localScale = Vector3.one * (delta * (_activeIconScale - _inactiveIconScale) + _inactiveIconScale);
        _iconRectTf.anchoredPosition = Vector3.up * (delta * (_activePosY - _inActivePosY) + _inActivePosY);
        _buttonName.alpha = delta;
    }

    public void SetActiveUI(bool active)
    {
        _iconRectTf.DOScale(active ? _activeIconScale : _inactiveIconScale, 0.2f);
        _iconRectTf.DOAnchorPosY(active ? _activePosY : _inActivePosY, 0.2f);
        _buttonName.DOFade(active ? 1 : 0, 0.2f);
    }
}
