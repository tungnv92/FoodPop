using DG.Tweening;
using System.Collections;
using UnityEngine;

public class NavigationGroup : MonoBehaviour
{
    [SerializeField] RectTransform _panelRectTf;
    [SerializeField] RectTransform _rectTf;
    [SerializeField] RectTransform _activeImageRectTf;
    [SerializeField] ButtonNavigator[] _buttons;
    [Tooltip("Chỉ số tab được chọn mặc định khi mở màn hình (bắt đầu từ 0).")]
    [SerializeField] int _defaultTabIndex = 2;

    public int TabCount => _buttons.Length;

    // EN: Waits for initial layout and tab sizing to complete, sizes the active highlight to one tab width,
    //     and snaps both the scroller and the navigation visuals to the default tab.
    // VI: Chờ layout và kích thước tab khởi tạo xong, đặt độ rộng ô highlight bằng một tab và đưa cả bộ cuộn
    //     lẫn hiển thị thanh điều hướng về tab mặc định.
    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        _activeImageRectTf.ChangeSizeDeltaX(_panelRectTf.rect.width / TabCount);

        int defaultIndex = Mathf.Clamp(_defaultTabIndex, 0, TabCount - 1);
        _buttons[defaultIndex].OnClick();
        MoveActiveImage(defaultIndex);
        foreach (var button in _buttons)
        {
            button.OnDrag(defaultIndex);
        }
    }

    // EN: Receives the scroll change callback, resolves the fractional page at the viewport center, moves the highlight
    //     between the two nearest buttons, and updates every button's active visuals.
    // VI: Nhận callback khi cuộn thay đổi, tính chỉ số trang thực tại tâm viewport, di chuyển highlight giữa hai nút
    //     gần nhất và cập nhật hiển thị active của mọi nút.
    public void OnDragTab(Vector2 value)
    {
        ScrollSnap scroller = _buttons.Length > 0 ? _buttons[0].PanelScroller : null;
        float pageIndex = scroller != null
            ? scroller.GetFractionalPageAtCenter()
            : Mathf.Clamp01(value.x) * (TabCount - 1);

        MoveActiveImage(pageIndex);

        foreach (var button in _buttons)
        {
            button.OnDrag(pageIndex);
        }
    }

    // EN: Places the active highlight by interpolating between the anchored X positions of the adjacent buttons.
    // VI: Đặt ô highlight bằng cách nội suy giữa vị trí X của hai nút liền kề.
    void MoveActiveImage(float pageIndex)
    {
        int leftIndex = Mathf.Clamp(Mathf.FloorToInt(pageIndex), 0, TabCount - 1);
        int rightIndex = Mathf.Min(leftIndex + 1, TabCount - 1);
        float t = pageIndex - leftIndex;

        float leftX = _buttons[leftIndex].RectTf.anchoredPosition.x;
        float rightX = _buttons[rightIndex].RectTf.anchoredPosition.x;
        float posX = Mathf.LerpUnclamped(leftX, rightX, t);
        _activeImageRectTf.anchoredPosition = new Vector2(posX, _activeImageRectTf.anchoredPosition.y);
    }
}
