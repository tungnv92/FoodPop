using UnityEngine;

/// <summary>
/// Base class for scene-specific UI panels (e.g. HomePanel, GamePanel).
/// Each scene should have one Panel subclass managing its primary HUD/UI layer.
/// </summary>
public abstract class Panel : MonoBehaviour
{
    public RectTransform RectTransform { get; private set; }

    [SerializeField] protected Canvas canvas;
    [SerializeField] protected CoinDisplay _coinDisplay;
    [Tooltip("Object hiển thị mạng (Life Display). Trỏ vào RectTransform chứ không trỏ vào script, để UI vẫn chạy khi không có package HeartSystem.")]
    [SerializeField] protected RectTransform _lifeDisplay;

    protected virtual void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
    }

    protected virtual void OnValidate()
    {
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }
    }

    /// <summary>
    /// Enables or disables the panel's canvas rendering.
    /// Use this instead of SetActive to preserve MonoBehaviour state.
    /// </summary>
    public void EnableCanvas(bool enable)
    {
        canvas.enabled = enable;
    }

    public void SetActiveCoin(bool active)
    {
        _coinDisplay.gameObject.SetActive(active);
    }

    // EN: Shows or hides the lives widget object.
    // VI: Hiện hoặc ẩn object hiển thị mạng.
    public void SetActiveLife(bool active)
    {
        _lifeDisplay.gameObject.SetActive(active);
    }
}
