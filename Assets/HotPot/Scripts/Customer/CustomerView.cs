using System.Collections;
using UnityEngine;

namespace FoodPop
{
    // EN: The customer standing at a counter lane, together with their order card: swaps the portrait and slides both in and out horizontally.
    // VI: Khách đang đứng ở một làn quầy cùng thẻ order: đổi ảnh khách và trượt cả hai vào/ra theo chiều ngang.
    [DisallowMultipleComponent]
    public class CustomerView : MonoBehaviour
    {
        [Tooltip("Renderer ảnh khách.")]
        [SerializeField] private SpriteRenderer portrait;
        [Tooltip("Thẻ order đi cùng khách (trượt vào/ra cùng khách).")]
        [SerializeField] private Transform orderCard;
        [Tooltip("Vị trí world của tâm ảnh khách, dùng để bắn hiệu ứng tim.")]
        [SerializeField] private Transform heartAnchor;

        private Vector3 customerHome;
        private Vector3 cardHome;
        private bool homeCaptured;

        // EN: World position for the reward hearts (the anchor, or the portrait's centre).
        // VI: Vị trí world để bắn tim khen thưởng (anchor, hoặc tâm ảnh khách).
        public Vector3 HeartPosition => heartAnchor != null ? heartAnchor.position :
            portrait != null ? portrait.bounds.center : transform.position;

        // EN: Sorting of the portrait, so effects can be drawn just above it.
        // VI: Thứ tự vẽ của ảnh khách, để hiệu ứng vẽ ngay phía trên.
        public SpriteRenderer Portrait => portrait;

        // EN: Remembers the scene layout as the "standing at the counter" position.
        // VI: Ghi nhớ bố cục trong scene làm vị trí "đứng tại quầy".
        private void Awake()
        {
            CaptureHome();
        }

        // EN: Stores the current positions of the customer and order card once.
        // VI: Lưu vị trí hiện tại của khách và thẻ order một lần.
        private void CaptureHome()
        {
            if (homeCaptured)
                return;
            customerHome = transform.position;
            cardHome = orderCard != null ? orderCard.position : Vector3.zero;
            homeCaptured = true;
        }

        // EN: Changes the portrait sprite (keeps the current one when null).
        // VI: Đổi ảnh khách (giữ ảnh hiện tại nếu truyền null).
        public void SetPortrait(Sprite sprite)
        {
            if (portrait != null && sprite != null)
                portrait.sprite = sprite;
        }

        // EN: Shows or hides the customer and their order card.
        // VI: Hiện hoặc ẩn khách cùng thẻ order.
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
            if (orderCard != null)
                orderCard.gameObject.SetActive(visible);
        }

        // EN: Puts the customer and order card back at the counter immediately.
        // VI: Đưa khách và thẻ order về ngay vị trí tại quầy.
        public void SnapHome()
        {
            CaptureHome();
            ApplyOffset(0f);
        }

        // EN: Slides from the counter to an off-screen offset (positive x = right) over the duration.
        // VI: Trượt từ quầy ra vị trí lệch ngoài màn hình (x dương = sang phải) trong khoảng thời gian đã cho.
        public IEnumerator SlideOut(float offsetX, float duration)
        {
            CaptureHome();
            yield return Slide(0f, offsetX, duration, EaseIn);
        }

        // EN: Moves customer and card together from one horizontal offset to another using an easing curve.
        // VI: Di chuyển khách và thẻ cùng lúc từ độ lệch ngang này sang độ lệch khác theo đường cong easing.
        private IEnumerator Slide(float fromX, float toX, float duration, System.Func<float, float> ease)
        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                ApplyOffset(Mathf.LerpUnclamped(fromX, toX, ease(Mathf.Clamp01(time / duration))));
                yield return null;
            }
            ApplyOffset(toX);
        }

        // EN: Places the customer and card at their home positions shifted by offsetX.
        // VI: Đặt khách và thẻ tại vị trí gốc dịch thêm offsetX.
        private void ApplyOffset(float offsetX)
        {
            Vector3 shift = new Vector3(offsetX, 0f, 0f);
            transform.position = customerHome + shift;
            if (orderCard != null)
                orderCard.position = cardHome + shift;
        }

        // EN: Quadratic ease-in (starts slow, leaves fast).
        // VI: Easing bậc hai tăng dần (bắt đầu chậm, rời đi nhanh).
        private static float EaseIn(float t) => t * t;
    }
}
