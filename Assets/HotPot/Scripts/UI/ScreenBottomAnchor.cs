using UnityEngine;

namespace FoodPop
{
    // EN: Keeps this object glued to the bottom edge of the camera view: every frame its Y is set to the bottom of the screen (optionally above the device safe area) plus bottomOffset, so it stays in place on any aspect ratio or when the camera size/position changes. X and Z are untouched. Runs in edit mode too, so the scene view shows the real position.
    // VI: Giữ object bám sát mép dưới khung camera: mỗi frame đặt Y bằng mép dưới màn hình (có thể tính phía trên vùng an toàn của thiết bị) cộng bottomOffset, nên đứng đúng chỗ với mọi tỉ lệ màn hình hoặc khi camera đổi size/vị trí. Không đổi X và Z. Chạy cả trong edit mode để scene hiện đúng vị trí thật.
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class ScreenBottomAnchor : MonoBehaviour
    {
        [Tooltip("Camera dùng để tính mép dưới màn hình; để trống dùng Camera.main.")]
        [SerializeField] private Camera targetCamera;
        [Tooltip("Khoảng cách (đơn vị world) từ mép dưới màn hình tới vị trí object; số âm là thấp hơn mép dưới.")]
        [SerializeField] private float bottomOffset = -0.1f;
        [Tooltip("Tính mép dưới theo vùng an toàn của thiết bị (tránh thanh điều hướng/tai thỏ) thay vì mép màn hình thật.")]
        [SerializeField] private bool useSafeArea = true;

        // EN: Re-anchors after cameras and layouts have updated this frame.
        // VI: Đặt lại vị trí sau khi camera và bố cục đã cập nhật trong frame.
        private void LateUpdate()
        {
            Anchor();
        }

        // EN: Moves the object so its Y sits bottomOffset above the bottom of the (safe) screen area.
        // VI: Di chuyển object để Y nằm cách mép dưới vùng màn hình (an toàn) một khoảng bottomOffset.
        private void Anchor()
        {
            Camera cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null)
                return;
            float bottomViewport = useSafeArea && Screen.height > 0 ? Screen.safeArea.yMin / Screen.height : 0f;
            float depth = cam.orthographic ? 0f : Mathf.Abs(transform.position.z - cam.transform.position.z);
            float bottomY = cam.ViewportToWorldPoint(new Vector3(0.5f, bottomViewport, depth)).y;
            Vector3 position = transform.position;
            float targetY = bottomY + bottomOffset;
            if (!Mathf.Approximately(position.y, targetY))
            {
                position.y = targetY;
                transform.position = position;
            }
        }
    }
}
