using UnityEngine;

namespace FoodPop
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ScreenTopAnchor : MonoBehaviour
    {
        [Tooltip("Camera dùng để tính mép trên màn hình; để trống dùng camera chính.")]
        [SerializeField] private Camera targetCamera;
        [Tooltip("Khoảng cách từ mép trên tới object theo đơn vị thế giới; số dương đưa object xuống dưới.")]
        [SerializeField] private float topOffset;
        [Tooltip("Tính mép trên theo vùng an toàn của thiết bị để tránh tai thỏ.")]
        [SerializeField] private bool useSafeArea = true;

        // EN: Re-anchor after camera and layout updates, including edit mode previews.
        // VI: Đặt lại vị trí sau cập nhật camera và bố cục, kể cả khi xem trước trong chế độ chỉnh sửa.
        private void LateUpdate()
        {
            Anchor();
        }

        // EN: Keep Y at the configured distance below the top screen edge while preserving X and Z.
        // VI: Giữ Y cách mép trên màn hình một khoảng đã cấu hình, giữ nguyên X và Z.
        private void Anchor()
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null)
                return;

            float topViewport = useSafeArea && Screen.height > 0 ? Screen.safeArea.yMax / Screen.height : 1f;
            float depth = cam.orthographic ? 0f : Mathf.Abs(transform.position.z - cam.transform.position.z);
            float topY = cam.ViewportToWorldPoint(new Vector3(0.5f, topViewport, depth)).y;
            Vector3 position = transform.position;
            float targetY = topY - topOffset;
            if (!Mathf.Approximately(position.y, targetY))
            {
                position.y = targetY;
                transform.position = position;
            }
        }
    }
}
