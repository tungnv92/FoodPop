using UnityEngine;
using UnityEngine.UI;

public class CanvasScaleControl : MonoBehaviour
{
    [SerializeField] Canvas _canvas;
    [SerializeField] CanvasScaler _scaler;


    private float BASE_RATIO = 1920 / 1080f;
    void OnEnable()
    {
        float ratio = Screen.height * 1f / Screen.width;

        _scaler.matchWidthOrHeight = (ratio > BASE_RATIO) ? 0 : 1;
    }
}
