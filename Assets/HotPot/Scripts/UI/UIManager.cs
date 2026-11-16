using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace FoodPop
{
    // EN: Creates and reuses typed canvas instances from the UIRef prefab catalog.
    // VI: Tạo và dùng lại instance canvas theo class từ danh sách prefab UIRef.
    [DisallowMultipleComponent]
    public sealed class UIManager : MonoBehaviour
    {

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
        public UICanvasPrime ActivePopup => activePopup;

        // EN: Exposes the visible popup's authored canvas without rebuilding or searching its hierarchy.
        // VI: Cung cấp canvas đã thiết kế của popup đang mở mà không dựng lại hoặc tìm trong cấu trúc.
        public Canvas ActiveCanvas => activePopup != null ? activePopup.Canvas : null;

        // EN: Exposes cached instances without allowing registry changes.
        // VI: Cung cấp instance đã lưu, không cho sửa danh sách quản lý.
        public IReadOnlyDictionary<Type, UICanvasPrime> UICanvasList => uiCanvasList;
        [Tooltip("Thanh thông báo ngắn có hiệu ứng hiện và ẩn, không chặn thao tác.")]
        [SerializeField] private ShortMessageView shortMessage;

        [Tooltip("Asset lưu danh sách prefab canvas của giao diện.")]
        [SerializeField] private UIRef uiRefSO;
        [Tooltip("Object cha của popup tạo mới; để trống dùng object manager.")]
        [SerializeField] private Transform uiCanvasParentTrans;
        private readonly Dictionary<Type, UICanvasPrime> uiCanvasReferenceDict = new Dictionary<Type, UICanvasPrime>();
        private readonly Dictionary<Type, UICanvasPrime> uiCanvasList = new Dictionary<Type, UICanvasPrime>();
        private bool initialized;
        private static UIManager instance;
        private IngameSoundManager soundManager;
        private UICanvasPrime activePopup;
        private bool ownsBuyPause;
        private float previousBuyTimeScale;

        // EN: Registers the scene manager, caches effects audio, prepares input and starts with popups hidden.
        // VI: Đăng ký manager scene, cache âm thanh hiệu ứng, chuẩn bị nhập liệu và bắt đầu với popup ẩn.
        private void Awake()
        {
            if (instance != null && instance != this)
                Debug.LogWarning("[UIManager] Scene đang có nhiều object quản lý UI.", this);
            else instance = this;
            if (Application.isPlaying) soundManager = IngameSoundManager.Instance;
            EnsureEventSystem();
            HideAll();
        }

        // EN: Closes owned popups when their scene manager is disabled.
        // VI: Đóng các popup được quản lý khi manager scene bị tắt.
        private void OnDisable()
        {
            if (initialized) HideAll();
        }

        // EN: Releases only this manager's cached singleton when its scene object is destroyed.
        // VI: Chỉ giải phóng singleton của manager này khi object scene bị hủy.
        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        // EN: Reuses an instance or creates its UIRef prefab on first request.
        // VI: Dùng lại instance hoặc tạo prefab từ UIRef khi được yêu cầu lần đầu.
        public T GetUICanvas<T>() where T : UICanvasPrime
        {
            InitCanvasData();
            Type key = typeof(T);
            if (uiCanvasList.TryGetValue(key, out UICanvasPrime canvas) && canvas != null) return (T)canvas;
            if (!uiCanvasReferenceDict.TryGetValue(key, out UICanvasPrime prefab))
            {
                Debug.LogError($"[UIManager] Chưa đăng ký canvas {key.Name}.", this);
                return null;
            }
            canvas = Instantiate(prefab, uiCanvasParentTrans != null ? uiCanvasParentTrans : transform);
            canvas.gameObject.SetActive(false);
            canvas.Owner = this;
            uiCanvasList[key] = canvas;
            return (T)canvas;
        }

        // EN: Opens one canvas exclusively; use Show methods to supply gameplay callbacks.
        // VI: Mở riêng một canvas; dùng hàm Show để truyền callback gameplay.
        public T OpenUI<T>() where T : UICanvasPrime
        {
            T canvas = GetUICanvas<T>();
            if (canvas is CanvasBuyBooster shop) return ShowBuyBooster(shop.SelectedType) as T;
            if (canvas is CanvasPause)
            {
                ShowPause();
                return canvas;
            }
            if (PreparePopup(canvas)) canvas.Open();
            return canvas;
        }

        // EN: Closes an existing canvas without creating it and releases its shop pause.
        // VI: Đóng canvas có sẵn không tạo mới, nhả tạm dừng cửa hàng của nó.
        public void CloseUI<T>() where T : UICanvasPrime
        {
            InitCanvasData();
            if (!uiCanvasList.TryGetValue(typeof(T), out UICanvasPrime canvas) || canvas == null) return;
            canvas.Close();
            if (canvas is CanvasBuyBooster) ReleaseBuyPause();
            if (activePopup == canvas) activePopup = null;
        }

        // EN: Checks visibility without creating an instance.
        // VI: Kiểm tra trạng thái mở mà không tạo instance.
        public bool IsUICanvasOpened<T>() where T : UICanvasPrime
        {
            InitCanvasData();
            return uiCanvasList.TryGetValue(typeof(T), out UICanvasPrime canvas) && canvas != null && canvas.IsOpening();
        }

        // EN: Closes all popups through the common cleanup path.
        // VI: Đóng mọi popup qua luồng dọn chung.
        public void CloseAllUI()
        {
            HideAll();
        }

        // EN: Closes all other canvas types while retaining the specified canvas and its pause.
        // VI: Đóng các kiểu canvas khác, giữ canvas chỉ định cùng tạm dừng của nó.
        public void CloseAllUI<T>() where T : UICanvasPrime
        {
            InitCanvasData();
            foreach (UICanvasPrime canvas in new List<UICanvasPrime>(uiCanvasList.Values))
            {
                if (canvas == null || canvas is T || !canvas.IsOpening()) continue;
                canvas.Close();
                if (activePopup == canvas) activePopup = null;
            }
            if (typeof(T) != typeof(CanvasBuyBooster)) ReleaseBuyPause();
        }

        // EN: Opens victory, binds the next-level action and plays the win cue.
        // VI: Mở chiến thắng, nối hành động sang màn và phát âm thắng.
        public void ShowWin(UnityAction next, bool hasNextLevel = true, bool animate = true)
        {
            CanvasWin canvas = GetUICanvas<CanvasWin>();
            if (PreparePopup(canvas))
            {
                canvas.Show(next, hasNextLevel, animate);
                soundManager?.PlayEvent(IngameSoundManager.Event.GameWin);
            }
        }

        // EN: Opens confirmed defeat, binds replay and plays the failure cue.
        // VI: Mở thất bại xác nhận, nối chơi lại và phát âm thua.
        public void ShowLose(UnityAction replay, bool animate = true)
        {
            CanvasLose canvas = GetUICanvas<CanvasLose>();
            if (PreparePopup(canvas))
            {
                canvas.Show(replay, animate);
                soundManager?.PlayEvent(IngameSoundManager.Event.GameFailed);
            }
        }

        // EN: Opens the matching revive case with accept/decline actions and the existing revive-offer cue.
        // VI: Mở đúng trường hợp hồi sinh với hành động đồng ý/từ chối và âm mời hồi sinh hiện có.
        public void ShowRevive(UnityAction revive, UnityAction decline, bool available = true, bool animate = true, CanvasRevive.ReviveCase reason = CanvasRevive.ReviveCase.OutOfSlots)
        {
            CanvasRevive canvas = GetUICanvas<CanvasRevive>();
            if (PreparePopup(canvas))
            {
                canvas.Show(revive, decline, available, animate, reason);
                soundManager?.PlayEvent(IngameSoundManager.Event.OutOfSpace);
            }
        }

        // EN: Returns the direct-base booster shop at its configured price and pauses gameplay until it closes.
        // VI: Trả cửa hàng booster kế thừa trực tiếp với giá cấu hình và tạm dừng game tới khi đóng.
        public CanvasBuyBooster ShowBuyBooster(BoosterType type)
        {
            if (!CanvasBuyBooster.TryGetResource(type, out _)) return null;
            CanvasBuyBooster canvas = GetUICanvas<CanvasBuyBooster>();
            if (!PreparePopup(canvas)) return null;
            if (Application.isPlaying && Time.timeScale > 0f)
            {
                previousBuyTimeScale = Time.timeScale;
                ownsBuyPause = true;
                Time.timeScale = 0f;
            }
            canvas.Show(type, CloseUI<CanvasBuyBooster>);
            return canvas;
        }

        // EN: Plays the unlock cue and opens the "new booster" popup for a booster that just unlocked; the caller pauses the level, reads the icon position and closes it with CloseUI<CanvasBoosterIntro>.
        // VI: Phát âm mở khóa và mở popup "booster mới" cho booster vừa mở khóa; nơi gọi tự tạm dừng màn chơi, đọc vị trí icon và đóng bằng CloseUI<CanvasBoosterIntro>.
        public CanvasBoosterIntro ShowBoosterIntro(Sprite icon, string title, string info, UnityAction gotIt)
        {
            CanvasBoosterIntro canvas = GetUICanvas<CanvasBoosterIntro>();
            if (!PreparePopup(canvas)) return null;
            canvas.Show(icon, title, info, gotIt);
            soundManager?.PlayEvent(IngameSoundManager.Event.NewItemUnlocked);
            return canvas;
        }

        // EN: Opens settings pause only during running gameplay without replacing another popup.
        // VI: Mở tạm dừng từ cài đặt chỉ khi gameplay đang chạy, không thay thế popup khác.
        public void ShowPause()
        {
            if (Application.isPlaying && (activePopup != null || Time.timeScale <= 0f)) return;
            CanvasPause canvas = GetUICanvas<CanvasPause>();
            if (PreparePopup(canvas)) canvas.Open();
        }

        // EN: Opens quit confirmation through the shared registry, preserving exclusive popup ownership.
        // VI: Mở xác nhận thoát qua registry dùng chung, giữ quyền quản lý popup độc quyền.
        public void ShowWarningQuit(UnityAction confirm, UnityAction cancel)
        {
            CanvasWarningQuit canvas = GetUICanvas<CanvasWarningQuit>();
            if (PreparePopup(canvas)) canvas.Show(confirm, cancel);
        }

        // EN: Displays a short nonblocking message independently of result popups.
        // VI: Hiện thông báo ngắn không chặn thao tác, độc lập với popup kết quả.
        public void ShowShortMessage(string message)
        {
            if (shortMessage != null) shortMessage.Show(message);
        }

        // EN: Closes all scene popups, releases only the shop-owned time pause and dismisses transient feedback.
        // VI: Đóng mọi popup scene, nhả riêng tạm dừng do cửa hàng giữ và bỏ phản hồi ngắn tạm.
        public void HideAll()
        {
            InitCanvasData();
            foreach (UICanvasPrime canvas in new List<UICanvasPrime>(uiCanvasList.Values))
                if (canvas != null && canvas.IsOpening()) canvas.Close();
            ReleaseBuyPause();
            if (shortMessage != null) shortMessage.HideImmediate();
            activePopup = null;
        }

        // EN: Releases active state and removes canvases configured to destroy on close.
        // VI: Giải phóng trạng thái mở và bỏ canvas được cấu hình hủy khi đóng.
        internal void OnCanvasClosed(UICanvasPrime canvas, bool remove)
        {
            if (canvas is CanvasBuyBooster) ReleaseBuyPause();
            if (activePopup == canvas) activePopup = null;
            if (remove) uiCanvasList.Remove(canvas.GetType());
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

        // EN: Registers UIRef prefab references once without creating scene instances.
        // VI: Đăng ký tham chiếu prefab UIRef một lần, không tạo instance trong scene.
        private void InitCanvasData()
        {
            if (initialized) return;
            initialized = true;
            foreach (UICanvasPrime prefab in uiRefSO.uiCanvasRefList)
                uiCanvasReferenceDict.Add(prefab.GetType(), prefab);
        }

        // EN: Validates a scene reference and closes other popups before activating the requested one.
        // VI: Kiểm tra tham chiếu scene và đóng các popup khác trước khi kích hoạt popup được yêu cầu.
        private bool PreparePopup(UICanvasPrime popup)
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

        // EN: Restores time only when the manager owns the shop pause.
        // VI: Khôi phục thời gian chỉ khi manager sở hữu tạm dừng cửa hàng.
        private void ReleaseBuyPause()
        {
            if (ownsBuyPause)
            {
                Time.timeScale = previousBuyTimeScale;
                ownsBuyPause = false;
            }
        }
        // EN: Clears the cached scene manager when entering a new Play Mode session.
        // VI: Xóa manager scene đã lưu khi bắt đầu phiên Play Mode mới.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            instance = null;
        }

    }
}
