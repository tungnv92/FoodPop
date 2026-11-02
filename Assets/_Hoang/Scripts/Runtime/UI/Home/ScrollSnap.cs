using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(ScrollRect))]
public class ScrollSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private float snapDuration = 0.25f;
    [Tooltip("Tỉ lệ một trang cần kéo để chuyển sang trang kế bên (0.2 = kéo 20% trang).")]
    [SerializeField, Range(0.05f, 0.5f)] private float swipeThreshold = 0.2f;
    [SerializeField] private HomeTab[] tabs;
    private ScrollRect scrollRect;
    private RectTransform content;

    private int pageCount;
    private int currentPage;
    private Tween snapTween;

    // EN: Caches the ScrollRect, content RectTransform, and total page count on initialization.
    // VI: Lưu tham chiếu ScrollRect, RectTransform của content và tổng số trang khi khởi tạo.
    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        content = scrollRect.content;

        pageCount = content.childCount;
    }

    // EN: Stops any running snap tween and blocks other UI input while the user drags between pages.
    // VI: Dừng tween snap đang chạy và chặn thao tác UI khác trong lúc người dùng kéo giữa các trang.
    public void OnBeginDrag(PointerEventData eventData)
    {
        snapTween?.Kill();
        UIManager.Ins.EnableBlocker(true);
    }

    // EN: Measures which page is closest to the viewport center on release and snaps to the resolved target page.
    // VI: Đo trang nào gần tâm viewport nhất khi thả tay và snap tới trang đích đã xác định.
    public void OnEndDrag(PointerEventData eventData)
    {
        SnapToPage(ResolveTargetPage(GetFractionalPageAtCenter()));
    }

    RectTransform Viewport => scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;

    // EN: Returns the horizontal distance from the viewport center to the page's center, in viewport local units
    //     (positive when the page lies to the right of center).
    // VI: Trả về khoảng cách ngang từ tâm viewport tới tâm trang theo đơn vị local của viewport
    //     (dương khi trang nằm bên phải tâm).
    float GetPageOffsetFromCenter(int page)
    {
        RectTransform pageRt = (RectTransform)content.GetChild(page);
        Vector3 pageCenterWorld = pageRt.TransformPoint(pageRt.rect.center);
        Vector3 pageCenterInViewport = Viewport.InverseTransformPoint(pageCenterWorld);
        return pageCenterInViewport.x - Viewport.rect.center.x;
    }

    // EN: Computes a fractional page index for the page currently at the viewport center by interpolating
    //     between the two pages whose centers surround it.
    // VI: Tính chỉ số trang dạng thực tại tâm viewport bằng cách nội suy giữa hai trang có tâm bao quanh tâm viewport.
    public float GetFractionalPageAtCenter()
    {
        if (pageCount <= 1) return 0f;

        float previousOffset = GetPageOffsetFromCenter(0);
        if (previousOffset >= 0f) return 0f;

        for (int i = 1; i < pageCount; i++)
        {
            float offset = GetPageOffsetFromCenter(i);
            if (offset >= 0f)
            {
                float span = offset - previousOffset;
                return span <= Mathf.Epsilon ? i : (i - 1) + (-previousOffset / span);
            }
            previousOffset = offset;
        }

        return pageCount - 1;
    }

    // EN: Calculates the content anchored X that places the given page's centerX exactly on the viewport's centerX.
    // VI: Tính anchoredPosition X của content để tâm X của trang chỉ định trùng đúng với tâm X của viewport.
    float GetCenteredContentX(int page)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Vector3 offsetWorld = Viewport.TransformVector(new Vector3(GetPageOffsetFromCenter(page), 0f, 0f));
        float offsetInContentParent = content.parent.InverseTransformVector(offsetWorld).x;
        return content.anchoredPosition.x - offsetInContentParent;
    }

    // EN: Picks the page to snap to for any page count. A drag past half a page snaps to the nearest page;
    //     a shorter drag past the swipe threshold moves one page in the drag direction; otherwise stays.
    // VI: Chọn trang để snap với số trang bất kỳ. Kéo quá nửa trang sẽ snap về trang gần nhất; kéo ngắn hơn
    //     nhưng vượt ngưỡng vuốt sẽ chuyển một trang theo hướng kéo; ngược lại giữ nguyên trang hiện tại.
    int ResolveTargetPage(float page)
    {
        float delta = page - currentPage;

        if (Mathf.Abs(delta) >= 0.5f) return Mathf.RoundToInt(page);
        if (delta > swipeThreshold) return currentPage + 1;
        if (delta < -swipeThreshold) return currentPage - 1;
        return currentPage;
    }

    // EN: Clamps the requested page, opens its tab, and tweens the content so the page's centerX aligns
    //     with the viewport's centerX.
    // VI: Giới hạn trang được yêu cầu, mở tab tương ứng và tween content để centerX của trang trùng
    //     với centerX của viewport.
    public void SnapToPage(int page)
    {
        page = Mathf.Clamp(page, 0, pageCount - 1);

        currentPage = page;

        //AudioManager.Ins.PlaySound(SoundType.HomePanelSwipe);
        if (currentPage < tabs.Length) tabs[currentPage].OpenTab();

        scrollRect.StopMovement();
        snapTween?.Kill();
        float targetX = GetCenteredContentX(page);
        snapTween = content.DOAnchorPosX(targetX, snapDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                UIManager.Ins.EnableBlocker(false);
            });
    }

    // EN: Snaps to the next page in sequence.
    // VI: Chuyển sang trang kế tiếp theo thứ tự.
    public void NextPage()
    {
        SnapToPage(currentPage + 1);
    }

    // EN: Snaps to the previous page in sequence.
    // VI: Chuyển về trang trước đó theo thứ tự.
    public void PreviousPage()
    {
        SnapToPage(currentPage - 1);
    }
}
