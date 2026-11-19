using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public sealed class PulseScale : MonoBehaviour
{
    [SerializeField, Tooltip("Hệ số scale nhỏ nhất so với scale gốc.")]
    private float minScale = 1f;

    [SerializeField, Tooltip("Hệ số scale lớn nhất so với scale gốc.")]
    private float maxScale = 1.2f;

    [SerializeField, Min(0.01f), Tooltip("Thời gian một chu kỳ to rồi nhỏ (giây).")]
    private float period = 1f;

    private RectTransform rectTransform;
    private Vector3 baseScale;
    private float elapsed;

    // EN: Cache the RectTransform and its authored scale used as the pulse baseline.
    // VI: Cache RectTransform và scale gốc làm mốc cho hiệu ứng nhịp.
    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        baseScale = rectTransform.localScale;
    }

    // EN: Restart the pulse from the minimum scale whenever the object is enabled.
    // VI: Bắt đầu lại nhịp từ scale nhỏ nhất mỗi khi đối tượng được bật.
    private void OnEnable()
    {
        elapsed = 0f;
    }

    // EN: Smoothly oscillate the scale between min and max with a cosine wave using unscaled time.
    // VI: Dao động scale mượt giữa min và max theo sóng cosin, dùng thời gian không bị timeScale ảnh hưởng.
    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float wave = 0.5f - 0.5f * Mathf.Cos(elapsed * 2f * Mathf.PI / period);
        rectTransform.localScale = baseScale * Mathf.Lerp(minScale, maxScale, wave);
    }

    // EN: Restore the authored scale so the object is not left mid-pulse when disabled.
    // VI: Trả về scale gốc để đối tượng không bị kẹt ở giữa nhịp khi tắt.
    private void OnDisable()
    {
        rectTransform.localScale = baseScale;
    }
}
