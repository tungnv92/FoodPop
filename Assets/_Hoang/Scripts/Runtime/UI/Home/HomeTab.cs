using UnityEngine;

public class HomeTab : MonoBehaviour
{
    [SerializeField] protected RectTransform _rectTf;

    protected float BASE_RATIO = 1920f / 1080;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
    }

    public virtual void OpenTab()
    {

    }
}
