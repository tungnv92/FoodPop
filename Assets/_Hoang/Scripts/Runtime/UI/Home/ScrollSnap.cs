using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(ScrollRect))]
public class ScrollSnap : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private float snapDuration = 0.25f;
    [SerializeField] private HomeTab[] tabs;
    private ScrollRect scrollRect;
    private RectTransform content;

    private int pageCount;
    private int currentPage;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        content = scrollRect.content;

        pageCount = content.childCount;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        UIManager.Ins.EnableBlocker(true);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        float page = scrollRect.horizontalNormalizedPosition * (pageCount - 1);

        currentPage = RoundToInt(page);

        SnapToPage(currentPage);
    }

    int RoundToInt(float value)
    {
        int rounded = 0;

        if (currentPage == 1)
        {
            if (value - currentPage < -0.2f) rounded = 0;
            else if (value - currentPage > 0.2f) rounded = 2;
            else rounded = 1;
        }else if (currentPage == 2)
        {
            if (value - currentPage < -0.2f) rounded = 1;
            else rounded = 2;
        }

        return rounded;
    }

    public void SnapToPage(int page)
    {
        page = Mathf.Clamp(page, 0, pageCount - 1);

        currentPage = page;
        Debug.Log("Current Page: " + currentPage);
        float target =
            pageCount == 1
            ? 0
            : (float)page / (pageCount - 1);

        //AudioManager.Ins.PlaySound(SoundType.HomePanelSwipe);
        tabs[currentPage].OpenTab();
        DOTween.To(
            () => scrollRect.horizontalNormalizedPosition,
            x => scrollRect.horizontalNormalizedPosition = x,
            target,
            snapDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                UIManager.Ins.EnableBlocker(false);
            });
    }

    public void NextPage()
    {
        SnapToPage(currentPage + 1);
    }

    public void PreviousPage()
    {
        SnapToPage(currentPage - 1);
    }
}