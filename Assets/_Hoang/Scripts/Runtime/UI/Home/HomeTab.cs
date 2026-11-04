using UnityEngine;

public class HomeTab : MonoBehaviour
{
    [SerializeField] protected RectTransform _rectTf;

    protected float BASE_RATIO = 1920f / 1080;

    // EN: Resizes the tab background to preserve its 9:16 aspect ratio while covering the screen,
    //     and resizes the HomePanel inner container if attached.
    // VI: Điều chỉnh kích thước nền tab để giữ tỉ lệ 9:16 đồng thời phủ kín màn hình,
    //     và điều chỉnh khung chứa bên trong của HomePanel nếu có gắn kèm.
    public virtual void Start()
    {
        float ratio = Screen.height * 1f / Screen.width;
        if (ratio < BASE_RATIO)
        {
            _rectTf.ChangeSizeDeltaX(_rectTf.sizeDelta.x * BASE_RATIO / ratio);
            _rectTf.ChangeSizeDeltaY(_rectTf.sizeDelta.y * BASE_RATIO / ratio);
        }
        else
        {
            _rectTf.ChangeSizeDeltaX(_rectTf.sizeDelta.x * ratio / BASE_RATIO);
            _rectTf.ChangeSizeDeltaY(_rectTf.sizeDelta.y * ratio / BASE_RATIO);
        }

        if (TryGetComponent<HomePanel>(out var homePanel))
        {
            homePanel.ResizeInnerContainer();
        }
    }

    public virtual void OpenTab()
    {

    }
}
