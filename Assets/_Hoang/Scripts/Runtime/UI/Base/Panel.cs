using Longvm.RewardAnimator;
using UnityEngine;

/// <summary>
/// Base class for scene-specific UI panels (e.g. HomePanel, GamePanel).
/// Each scene should have one Panel subclass managing its primary HUD/UI layer.
/// </summary>
public abstract class Panel : MonoBehaviour
{
    [SerializeField] protected Canvas canvas;

    public RectTransform RectTransform { get; private set; }
    [SerializeField] protected CoinDisplay _coinDisplay;
    [SerializeField] protected LifeDisplay _lifeDisplay;
    public RewardAnimator RewardAnimator;

    protected virtual void OnValidate()
    {
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }
    }

    protected virtual void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
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

    public void SetActiveLife(bool active)
    {
        _lifeDisplay.gameObject.SetActive(active);
    }
}
