using UnityEngine;

namespace FoodPop
{
    // EN: Displays serving time with a bottom-up sprite fill and a separate fill for the final five seconds.
    // VI: Hiển thị thời gian phục vụ bằng sprite đầy từ dưới lên và phần màu riêng trong năm giây cuối.
    [DisallowMultipleComponent]
    public class CustomerTimerBar : MonoBehaviour
    {
        private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
        private static readonly int FillRangeId = Shader.PropertyToID("_FillRange");
        private const float FinalSeconds = 5f;

        // EN: Exposes the clamped visible fraction, like a UI Image fill amount.
        // VI: Cung cấp tỉ lệ hiển thị đã giới hạn, tương tự lượng đầy của ảnh giao diện.
        public float FillAmount { get; private set; }

        // EN: Returns the authored frame width in the parent's local units for card placement.
        // VI: Trả chiều rộng khung thiết kế theo đơn vị cục bộ của cha để đặt cạnh thẻ.
        public float LocalWidth
        {
            get
            {
                SpriteRenderer frame = background != null ? background : fill1;
                if (frame == null)
                    return 0f;
                float width = frame.drawMode != SpriteDrawMode.Simple ? frame.size.x : frame.sprite != null ? frame.sprite.bounds.size.x : 0f;
                return width * Mathf.Abs(frame.transform.localScale.x) * Mathf.Abs(transform.localScale.x);
            }
        }

        [Header("Tham chiếu")]
        [Tooltip("Nền thanh thời gian, giữ nguyên kích thước thiết kế.")]
        [SerializeField] private SpriteRenderer background;
        [Tooltip("Phần màu bình thường, hiện khi còn trên năm giây.")]
        [SerializeField] private SpriteRenderer fill1;
        [Tooltip("Phần màu cảnh báo, hiện khi còn tối đa năm giây.")]
        [SerializeField] private SpriteRenderer fill2;

        private MaterialPropertyBlock fillProperties;

        // EN: Shows the bar with the current remaining and total serving seconds.
        // VI: Hiện thanh theo số giây phục vụ còn lại và tổng thời gian hiện tại.
        public void Show(float remainingSeconds, float totalSeconds)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            SetProgress(remainingSeconds, totalSeconds);
        }

        // EN: Clips both sprites to the same fraction without scaling them and selects the final-seconds fill by actual time.
        // VI: Cắt hai sprite theo cùng tỉ lệ mà không co giãn, chọn phần màu cuối theo số giây thực còn lại.
        public void SetProgress(float remainingSeconds, float totalSeconds)
        {
            FillAmount = totalSeconds > 0f ? Mathf.Clamp01(remainingSeconds / totalSeconds) : 0f;
            bool final = remainingSeconds <= FinalSeconds;
            UpdateFill(fill1, !final);
            UpdateFill(fill2, final);
        }

        // EN: Hides the complete timer bar for untimed or finished customers.
        // VI: Ẩn toàn bộ thanh thời gian cho khách không tính giờ hoặc đã hoàn tất.
        public void Hide()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        // EN: Sets the sprite's clipping fraction and local height range, preserving its artwork, size and transform.
        // VI: Đặt tỉ lệ cắt và khoảng chiều cao cục bộ, giữ nguyên hình ảnh, kích thước và transform của sprite.
        private void UpdateFill(SpriteRenderer renderer, bool selected)
        {
            if (renderer == null)
                return;
            renderer.enabled = selected && FillAmount > 0f;
            if (fillProperties == null)
                fillProperties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(fillProperties);
            Bounds bounds = renderer.localBounds;
            fillProperties.SetFloat(FillAmountId, FillAmount);
            fillProperties.SetVector(FillRangeId, new Vector4(bounds.min.y, bounds.size.y, 0f, 0f));
            renderer.SetPropertyBlock(fillProperties);
            if (background != null && renderer.sortingLayerID == background.sortingLayerID && renderer.sortingOrder <= background.sortingOrder)
                renderer.sortingOrder = background.sortingOrder + 1;
        }
    }
}
