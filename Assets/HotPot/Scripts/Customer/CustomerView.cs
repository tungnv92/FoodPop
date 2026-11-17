using UnityEngine;
using UnityEngine.Serialization;

namespace FoodPop
{
    // EN: The customer standing at a counter lane, together with their order card: swaps the portrait and places both at the counter seat (walking in/out is done by CustomerManager's walker/leaver figures).
    // VI: Khách đang đứng ở một làn quầy cùng thẻ order: đổi ảnh khách và đặt cả hai tại chỗ đứng ở quầy (việc đi vào/ra do hình walker/leaver của CustomerManager đảm nhận).
    [DisallowMultipleComponent]
    public class CustomerView : MonoBehaviour
    {
        [Tooltip("Renderer ảnh khách.")]
        [SerializeField] private SpriteRenderer portrait;
        [Tooltip("Thẻ order đi cùng khách (trượt vào/ra cùng khách).")]
        [SerializeField] private Transform orderCard;
        [Tooltip("Điểm hiện chữ khen khi khách nhận đủ món; để trống dùng tâm ảnh khách.")]
        [FormerlySerializedAs("heartAnchor")]
        [SerializeField] private Transform praiseAnchor;

        [Tooltip("Loại khách đang được phục vụ, nhận từ cấu hình level khi vào quầy.")]
        [SerializeField] private CustomerType currentType;

        private Vector3 customerHome;
        private Vector3 cardHome;
        private bool homeCaptured;
        private float seatOffsetX;
        private float cardSeatOffsetX;

        // EN: Remembers the scene layout as the "standing at the counter" position.
        // VI: Ghi nhớ bố cục trong scene làm vị trí "đứng tại quầy".
        private void Awake()
        {
            CaptureHome();
        }

        // EN: Exposes the active customer's classification without changing serving behavior.
        // VI: Cho biết phân loại khách hiện tại, không thay đổi hành vi phục vụ.
        public CustomerType Type => currentType;

        // EN: World position for the praise text (the anchor, or the portrait's centre).
        // VI: Vị trí world để hiện chữ khen (anchor, hoặc tâm ảnh khách).
        public Vector3 PraisePosition => praiseAnchor != null ? praiseAnchor.position :
            portrait != null ? portrait.bounds.center : transform.position;

        // EN: Sorting of the portrait, so effects can be drawn just above it.
        // VI: Thứ tự vẽ của ảnh khách, để hiệu ứng vẽ ngay phía trên.
        public SpriteRenderer Portrait => portrait;

        // EN: The lane's own counter position (without any seat offset).
        // VI: Vị trí quầy gốc của làn (chưa cộng độ lệch chỗ đứng).
        public Vector3 HomePosition { get { CaptureHome(); return customerHome; } }

        // EN: Shifts where this customer stands at the counter (e.g. a Shipper standing in the middle); 0 = the lane's own spot.
        // VI: Dịch chỗ khách đứng tại quầy (vd. Shipper đứng giữa); thẻ order vẫn ở chỗ gốc của làn để không che khách; 0 = chỗ gốc của làn.
        public void SetSeatOffset(float offsetX, float cardOffsetX = 0f)
        {
            CaptureHome();
            seatOffsetX = offsetX;
            cardSeatOffsetX = cardOffsetX;
        }

        // EN: The order card this customer slides with (null when the card is not linked).
        // VI: Thẻ order trượt cùng khách này (null nếu chưa liên kết).
        public Transform OrderCard => orderCard;

        // EN: Horizontal distance from the transform to the portrait's visual centre.
        // VI: Khoảng cách ngang từ transform tới tâm hình của ảnh khách.
        public float PortraitCenterOffsetX => portrait != null && portrait.sprite != null ? portrait.bounds.center.x - transform.position.x : 0f;

        // EN: Applies the configured type, defaulting invalid old values to Normal.
        // VI: Gán loại khách, dùng Normal nếu dữ liệu cũ không hợp lệ.
        public void SetCustomerType(CustomerType type)
        {
            currentType = System.Enum.IsDefined(typeof(CustomerType), type) ? type : CustomerType.Normal;
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

        // EN: Places the customer and card at their home positions shifted by offsetX.
        // VI: Đặt khách và thẻ tại vị trí gốc dịch thêm offsetX.
        private void ApplyOffset(float offsetX)
        {
            Vector3 shift = new Vector3(offsetX, 0f, 0f);
            transform.position = customerHome + shift + new Vector3(seatOffsetX, 0f, 0f);
            if (orderCard != null)
                orderCard.position = cardHome + shift + new Vector3(cardSeatOffsetX, 0f, 0f);
        }
    }
}
