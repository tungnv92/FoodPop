using UnityEngine;
using UnityEngine.UI;

public class BackGround : MonoBehaviour
{
    public Image img;

    [Header("Content (Spine, decorations...)")]
    [Tooltip("Child container holding the SkeletonGraphic and other decorations. Anchors = center, Pivot = center.")]
    [SerializeField] private RectTransform _content;

    [Tooltip("Size of the background square that the content was laid out against in the editor.")]
    [SerializeField] private float _referenceSize = 1920f;

    private void Start()  // Chuyển sang Start()
    {
        img = GetComponent<Image>();

        // Lấy height thực tế trước khi thay đổi anchors
        float height = img.rectTransform.rect.height;  // Hoặc img.sprite.rect.height nếu muốn dựa vào sprite gốc

        // Set anchors để centered, không stretch
        img.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        // Set position về center nếu cần
        img.rectTransform.anchoredPosition = Vector2.zero;

        // Set sizeDelta để vuông
        img.rectTransform.sizeDelta = new Vector2(height, height);

        // Nếu muốn giữ aspect ratio, dùng Preserve Aspect trong Inspector hoặc code
        //img.preserveAspect = true;  // Optional, nếu sprite không vuông

        FitContent(height);
    }

    /// <summary>
    /// Keeps the content container locked to the background image by centering it
    /// and scaling it uniformly with the background's final size.
    /// </summary>
    /// <param name="backgroundSize">Final side length of the square background.</param>
    private void FitContent(float backgroundSize)
    {
        if (_content == null || _referenceSize <= 0f) return;

        _content.anchorMin = new Vector2(0.5f, 0.5f);
        _content.anchorMax = new Vector2(0.5f, 0.5f);
        _content.pivot = new Vector2(0.5f, 0.5f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = new Vector2(_referenceSize, _referenceSize);

        float scale = backgroundSize / _referenceSize;
        _content.localScale = new Vector3(scale, scale, 1f);
    }
}