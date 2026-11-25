using UnityEngine;
using UnityEngine.UI;

public class ImageFullScene : MonoBehaviour
{
    void Start()
    {
        var image = GetComponent<Image>();
        var rt = GetComponent<RectTransform>();

        var canvas = GetComponentInParent<Canvas>();
        var canvasRect = canvas.GetComponent<RectTransform>();

        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        float screenRatio = canvasWidth / canvasHeight;
        float imageRatio = (float)image.sprite.bounds.size.x / image.sprite.bounds.size.y;
        if (screenRatio >= imageRatio)
        {
            // Fit theo chiều ngang
            rt.sizeDelta = new Vector2(canvasWidth, canvasWidth / imageRatio);
        } else
        {
            // Fit theo chiều dọc
            rt.sizeDelta = new Vector2(canvasHeight * imageRatio, canvasHeight);
        }
    }
}
