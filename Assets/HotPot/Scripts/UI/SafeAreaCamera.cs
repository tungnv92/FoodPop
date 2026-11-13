using UnityEngine;

namespace FoodPop
{
    [ExecuteAlways]
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class SafeAreaCamera : MonoBehaviour
    {
        [Tooltip("Vùng nội dung ưu tiên theo tọa độ thế giới, được kiểm tra trước ở kích thước camera chuẩn.")]
        [SerializeField] private Rect cameraBounds = new Rect(-5.3986816f, -11.7f, 10.797363f, 23.4f);
        [Tooltip("Vùng nội dung tối thiểu được kiểm tra sau vùng ưu tiên, phải hẹp hơn vùng ưu tiên theo chiều ngang mới có tác dụng. Nếu cả hai không vừa, camera chỉnh kích thước để chứa vùng này.")]
        [SerializeField] private Rect secondaryBounds = new Rect(-5.2f, -11.7f, 10.4f, 23.4f);
        [Tooltip("Kích thước camera chuẩn để kiểm tra lần lượt hai vùng nội dung.")]
        [SerializeField, Min(0.01f)] private float defaultOrthographicSize = 11.7f;
        [Tooltip("Mức phóng to tối đa (phần trăm so với kích thước camera chuẩn) khi màn hình rộng hơn vùng ưu tiên, để giảm viền hở hai bên.")]
        [SerializeField, Range(0f, 50f)] private float maxZoomInPercent = 5f;

        private Camera targetCamera;
        private Rect lastPixelRect;
        private Rect lastBounds;
        private Rect lastSecondaryBounds;
        private float lastDefaultSize;
        private float lastMaxZoomInPercent;
        private Vector3 lastCameraPosition;
        private float framingY;
        private bool hasFramingY;

        // EN: Cache the attached camera, remember its starting Y as the framing origin and immediately evaluate the two content bounds at the default size.
        // VI: Lưu camera gắn cùng đối tượng, ghi nhớ Y ban đầu làm gốc tính khung hình và kiểm tra ngay hai vùng nội dung ở kích thước chuẩn.
        private void OnEnable()
        {
            targetCamera = GetComponent<Camera>();
            RefreshFramingOrigin();
        }

        // EN: Takes the camera's current Y as the framing origin and re-evaluates framing. In Play Mode, vertical camera pans (such as PickupHand sliding down to show more rows) keep this origin, so they move the view instead of zooming it out; call this after deliberately repositioning the camera for a new layout.
        // VI: Lấy Y hiện tại của camera làm gốc tính khung hình rồi tính lại. Trong Play Mode, các lần camera trượt dọc (như PickupHand trượt xuống để thấy thêm hàng) giữ nguyên gốc này, nên khung nhìn dịch chuyển chứ không bị thu nhỏ; gọi hàm này sau khi chủ động đặt lại vị trí camera cho bố cục mới.
        public void RefreshFramingOrigin()
        {
            framingY = transform.position.y;
            hasFramingY = true;
            ApplyFraming();
        }

        // EN: Re-evaluate framing when the viewport, either bound, default size, zoom-in limit or camera position changes.
        // VI: Kiểm tra lại khung hình khi khung nhìn, một trong hai vùng, kích thước chuẩn, giới hạn phóng to hoặc vị trí camera thay đổi.
        private void LateUpdate()
        {
            if (targetCamera.pixelRect != lastPixelRect || cameraBounds != lastBounds ||
                secondaryBounds != lastSecondaryBounds || defaultOrthographicSize != lastDefaultSize ||
                maxZoomInPercent != lastMaxZoomInPercent || transform.position != lastCameraPosition)
                ApplyFraming();
        }

        // EN: Keep both bounds and the default size positive, then refresh the Inspector preview.
        // VI: Giữ kích thước hai vùng và kích thước chuẩn dương, rồi cập nhật xem trước trong Inspector.
        private void OnValidate()
        {
            cameraBounds.width = Mathf.Max(0.01f, cameraBounds.width);
            cameraBounds.height = Mathf.Max(0.01f, cameraBounds.height);
            secondaryBounds.width = Mathf.Max(0.01f, secondaryBounds.width);
            secondaryBounds.height = Mathf.Max(0.01f, secondaryBounds.height);
            defaultOrthographicSize = Mathf.Max(0.01f, defaultOrthographicSize);
            targetCamera = GetComponent<Camera>();
            ApplyFraming();
        }

        // EN: When the primary bound fits, zoom in up to the limit to fill its width; otherwise keep the default size if the second bound fits, or fit the second bound. The camera never moves. In Play Mode the bounds are measured from the framing origin Y, not the live Y, so vertical pans never change the size; in Edit Mode the live position is used.
        // VI: Khi vùng ưu tiên vừa, phóng to tới giới hạn để lấp chiều ngang vùng này; nếu không thì giữ kích thước chuẩn khi vùng hai vừa, hoặc khớp vùng hai. Camera không dịch chuyển. Trong Play Mode hai vùng được đo từ Y gốc chứ không phải Y hiện tại, nên camera trượt dọc không làm đổi kích thước; trong Edit Mode dùng vị trí hiện tại.
        private void ApplyFraming()
        {
            if (targetCamera == null)
                targetCamera = GetComponent<Camera>();
            lastPixelRect = targetCamera.pixelRect;
            lastBounds = cameraBounds;
            lastSecondaryBounds = secondaryBounds;
            lastDefaultSize = defaultOrthographicSize;
            lastMaxZoomInPercent = maxZoomInPercent;
            lastCameraPosition = transform.position;
            if (!Application.isPlaying || !hasFramingY)
            {
                framingY = lastCameraPosition.y;
                hasFramingY = true;
            }
            Vector3 framingPosition = new Vector3(lastCameraPosition.x, framingY, lastCameraPosition.z);
            if (!targetCamera.orthographic || lastPixelRect.width <= 0f || lastPixelRect.height <= 0f)
                return;

            float aspect = lastPixelRect.width / lastPixelRect.height;
            targetCamera.orthographicSize = defaultOrthographicSize;
            if (GetRequiredSize(cameraBounds, framingPosition, aspect) <= defaultOrthographicSize + 0.0001f)
            {
                targetCamera.orthographicSize = GetZoomedInSize(aspect);
                return;
            }

            float secondarySize = GetRequiredSize(secondaryBounds, framingPosition, aspect);
            if (secondarySize > defaultOrthographicSize + 0.0001f)
                targetCamera.orthographicSize = secondarySize;
        }

        // EN: Shrink the size until the primary bound's width fills the screen, but never more than maxZoomInPercent below the default size. Top and bottom may be cropped; edge-anchored HUD follows the camera.
        // VI: Giảm kích thước tới khi chiều ngang vùng ưu tiên lấp kín màn hình, nhưng không nhỏ hơn kích thước chuẩn quá maxZoomInPercent. Có thể cắt bớt trên/dưới; HUD bám mép đi theo camera.
        private float GetZoomedInSize(float aspect)
        {
            float halfWidth = Mathf.Max(Mathf.Abs(cameraBounds.xMin - lastCameraPosition.x),
                Mathf.Abs(cameraBounds.xMax - lastCameraPosition.x));
            float minSize = defaultOrthographicSize * (1f - maxZoomInPercent / 100f);
            return Mathf.Clamp(halfWidth / aspect, minSize, defaultOrthographicSize);
        }

        // EN: Calculate the minimum orthographic size that contains a world-space bound at a fixed camera position.
        // VI: Tính kích thước trực giao nhỏ nhất chứa một vùng theo tọa độ thế giới tại vị trí camera cố định.
        private static float GetRequiredSize(Rect bounds, Vector3 cameraPosition, float aspect)
        {
            float halfWidth = Mathf.Max(Mathf.Abs(bounds.xMin - cameraPosition.x),
                Mathf.Abs(bounds.xMax - cameraPosition.x));
            float halfHeight = Mathf.Max(Mathf.Abs(bounds.yMin - cameraPosition.y),
                Mathf.Abs(bounds.yMax - cameraPosition.y));
            return Mathf.Max(halfHeight, halfWidth / aspect);
        }

        // EN: Show the primary bound in cyan and the secondary bound in yellow when selecting the camera.
        // VI: Hiển thị vùng ưu tiên màu xanh và vùng thứ hai màu vàng khi chọn camera.
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(new Vector3(cameraBounds.center.x, cameraBounds.center.y, 0f),
                new Vector3(cameraBounds.width, cameraBounds.height, 0f));
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(secondaryBounds.center.x, secondaryBounds.center.y, 0f),
                new Vector3(secondaryBounds.width, secondaryBounds.height, 0f));
        }
    }
}
