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
    public ScrollSnap PanelScroller => panelScroller;

    [SerializeField] float _inActivePosY = -80;
    [SerializeField] float _activePosY = -30;

    [SerializeField] float _inactiveIconScale = 0.85f;
    [SerializeField] float _activeIconScale = 1f;

    // EN: Snaps the panel scroller to this button's tab page when clicked.
    // VI: Đưa bộ cuộn panel tới trang tab của nút này khi được nhấn.
    public void OnClick()
    {
        panelScroller.SnapToPage(tabId);
    }

    // EN: Blends icon scale, icon height, and label alpha based on how close the fractional page index is to this tab.
    // VI: Pha trộn tỉ lệ icon, độ cao icon và độ trong suốt nhãn theo khoảng cách từ chỉ số trang hiện tại tới tab này.
    public void OnDrag(float pageIndex)
    {
        float index = pageIndex;
        float delta = -Mathf.Abs(tabId - index);
        delta = Mathf.Clamp(delta, -1, 0) + 1;  // 0 to 1

        _iconRectTf.localScale = Vector3.one * (delta * (_activeIconScale - _inactiveIconScale) + _inactiveIconScale);
        _iconRectTf.anchoredPosition = Vector3.up * (delta * (_activePosY - _inActivePosY) + _inActivePosY);
        _buttonName.alpha = delta;
    }

    // EN: Animates the button icon scale, vertical position, and label opacity to the active or inactive state.
    // VI: Diễn hoạt tỉ lệ icon, vị trí dọc và độ mờ của nhãn sang trạng thái đang chọn hoặc không chọn.
    public void SetActiveUI(bool active)
    {
        _iconRectTf.DOScale(active ? _activeIconScale : _inactiveIconScale, 0.2f);
        _iconRectTf.DOAnchorPosY(active ? _activePosY : _inActivePosY, 0.2f);
        _buttonName.DOFade(active ? 1 : 0, 0.2f);
    }
}
