using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace FoodPop
{
    // EN: Owns exclusive popup visibility through the existing scene object and its three authored child canvases.
    // VI: Quản lý chỉ hiển thị một popup qua object có sẵn trong scene và ba canvas con đã thiết kế.
    [DisallowMultipleComponent]
    public sealed class UIManager : MonoBehaviour
    {
        [Tooltip("Canvas thắng đặt dưới object quản lý UI trong scene.")]
        [SerializeField] private CanvasWin canvasWin;
        [Tooltip("Canvas thua đặt dưới object quản lý UI trong scene.")]
        [SerializeField] private CanvasLose canvasLose;
        [Tooltip("Canvas mời hồi sinh đặt dưới object quản lý UI trong scene.")]
        [SerializeField] private CanvasRevive canvasRevive;
        [Tooltip("Thanh thông báo ngắn có hiệu ứng hiện và ẩn, không chặn thao tác.")]
        [SerializeField] private ShortMessageView shortMessage;

        private static UIManager instance;
        private LevelPopupView activePopup;

        // EN: Resolves the manager already present in the scene without creating another manager object.
        // VI: Lấy manager đã có trong scene mà không tạo thêm object quản lý.
        public static UIManager Instance
        {
            get
            {
                if (instance == null) instance = FindFirstObjectByType<UIManager>();
                return instance;
            }
        }

        // EN: Exposes the currently opened concrete popup for inspection and preview rendering.
        // VI: Cung cấp popup cụ thể đang mở để kiểm tra và dựng ảnh xem thử.
        public LevelPopupView ActivePopup => activePopup;

        // EN: Exposes the visible popup's authored canvas without rebuilding or searching its hierarchy.
        // VI: Cung cấp canvas đã thiết kế của popup đang mở mà không dựng lại hoặc tìm trong cấu trúc.
        public Canvas ActiveCanvas => activePopup != null ? activePopup.Canvas : null;

        // EN: Clears the cached scene manager when entering a new Play Mode session.
        // VI: Xóa manager scene đã lưu khi bắt đầu phiên Play Mode mới.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            instance = null;
        }

        // EN: Registers the scene manager, prepares input and starts with all result popups hidden.
        // VI: Đăng ký manager scene, chuẩn bị nhập liệu và bắt đầu với mọi popup kết quả được ẩn.
        private void Awake()
        {
            if (instance != null && instance != this)
                Debug.LogWarning("[UIManager] Scene đang có nhiều object quản lý UI.", this);
            else instance = this;
            EnsureEventSystem();
            HideAll();
        }

        // EN: Supplies pointer input only when the scene has no active event system.
        // VI: Bổ sung nhập liệu con trỏ chỉ khi scene chưa có hệ sự kiện đang hoạt động.
        private void EnsureEventSystem()
        {
            if (!Application.isPlaying || EventSystem.current != null) return;
            EventSystem events = GetComponentInChildren<EventSystem>(true);
            if (events != null)
            {
                events.gameObject.SetActive(true);
                return;
            }
            var root = new GameObject("UI Events", typeof(EventSystem), typeof(InputSystemUIInputModule));
            root.transform.SetParent(transform, false);
        }

        // EN: Opens the scene's victory canvas and binds advancement to the next level.
        // VI: Mở canvas chiến thắng trong scene và nối hành động sang màn kế tiếp.
        public void ShowWin(UnityAction next, bool hasNextLevel = true, bool animate = true)
        {
            if (PreparePopup(canvasWin)) canvasWin.Show(next, hasNextLevel, animate);
        }

        // EN: Opens the scene's confirmed defeat canvas and binds replay.
        // VI: Mở canvas thất bại đã xác nhận trong scene và nối chơi lại.
        public void ShowLose(UnityAction replay, bool animate = true)
        {
            if (PreparePopup(canvasLose)) canvasLose.Show(replay, animate);
        }

        // EN: Opens the scene's revive canvas with distinct acceptance and refusal actions.
        // VI: Mở canvas hồi sinh trong scene với hai hành động đồng ý và từ chối riêng.
        public void ShowRevive(UnityAction revive, UnityAction decline, bool available = true, bool animate = true)
        {
            if (PreparePopup(canvasRevive)) canvasRevive.Show(revive, decline, available, animate);
        }

        // EN: Validates a scene reference and closes other popups before activating the requested one.
        // VI: Kiểm tra tham chiếu scene và đóng các popup khác trước khi kích hoạt popup được yêu cầu.
        private bool PreparePopup(LevelPopupView popup)
        {
            if (popup == null)
            {
                Debug.LogError("[UIManager] Chưa gán canvas popup trong Inspector.", this);
                return false;
            }
            HideAll();
            EnsureEventSystem();
            activePopup = popup;
            return true;
        }

        // EN: Displays a short nonblocking message independently of result popups.
        // VI: Hiện thông báo ngắn không chặn thao tác, độc lập với popup kết quả.
        public void ShowShortMessage(string message)
        {
            if (shortMessage != null) shortMessage.Show(message);
        }

        // EN: Closes result canvases and dismisses transient short feedback during transitions or replay.
        // VI: Đóng canvas kết quả và bỏ thông báo ngắn tạm khi chuyển trạng thái hoặc chơi lại.
        public void HideAll()
        {
            if (canvasWin != null) canvasWin.Hide();
            if (canvasLose != null) canvasLose.Hide();
            if (canvasRevive != null) canvasRevive.Hide();
            if (shortMessage != null) shortMessage.HideImmediate();
            activePopup = null;
        }

        // EN: Closes owned popups when their scene manager is disabled.
        // VI: Đóng các popup được quản lý khi manager scene bị tắt.
        private void OnDisable()
        {
            HideAll();
        }

        // EN: Releases only this manager's cached singleton when its scene object is destroyed.
        // VI: Chỉ giải phóng singleton của manager này khi object scene bị hủy.
        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
