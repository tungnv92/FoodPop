using GamePackages;
using Lofelt.NiceVibrations;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace FoodPop
{
    // EN: Provides pointer feedback and an optional Inspector action without owning booster eligibility.
    // VI: Phản hồi con trỏ và hành động tùy chọn trong Inspector, không quản lý điều kiện dùng booster.
    [DisallowMultipleComponent]
    public sealed class UIButtonClick : MonoBehaviour
    {
        [Tooltip("Collider nhận thao tác nhấn trên nút.")]
        [SerializeField] private BoxCollider2D hitArea;
        [Tooltip("Phần hình ảnh co giãn khi nhấn, nên tách khỏi collider.")]
        [SerializeField] private Transform visual;
        [Tooltip("Camera dùng đổi vị trí con trỏ sang tọa độ thế giới.")]
        [SerializeField] private Camera inputCamera;
        [Tooltip("Thời gian nhún nút, tính bằng giây và không phụ thuộc tốc độ game.")]
        [SerializeField, Min(0.01f)] private float duration = 0.22f;
        [Tooltip("Tỉ lệ co xuống ngay khi nhấn.")]
        [SerializeField, Range(0.5f, 1f)] private float pressedScale = 0.9f;
        [Tooltip("Tỉ lệ bật lên trước khi trở về kích thước gốc.")]
        [SerializeField, Range(1f, 1.3f)] private float reboundScale = 1.06f;

        [Tooltip("Hành động gọi khi nhấn trong collider; để trống nếu chỉ cần hiệu ứng.")]
        public UnityEvent onClick = new UnityEvent();

        private InputAction press;
        private Coroutine animation;
        private Vector3 restScale;
        private IngameSoundManager soundManager;
        private UIManager uiManager;

        // EN: Creates a dedicated press action and resolves the camera once.
        // VI: Tạo action nhấn riêng và xác định camera một lần.
        private void Awake()
        {
            press = new InputAction("UI button click feedback", InputActionType.Button, "<Pointer>/press");
            if (inputCamera == null) inputCamera = Camera.main;
        }

        // EN: Caches UI and audio managers, records the visual scale and subscribes while active.
        // VI: Cache manager UI và âm thanh, ghi kích thước hình ảnh và đăng ký khi đang hoạt động.
        private void OnEnable()
        {
            soundManager = IngameSoundManager.Instance;
            uiManager = UIManager.Instance;
            if (visual != null) restScale = visual.localScale;
            press.performed += OnPressed;
            press.Enable();
        }

        // EN: Plays visual and haptic feedback, invoking click audio only when the action does not open a popup.
        // VI: Phát hình ảnh và rung, chỉ phát tiếng nhấn khi hành động không mở popup.
        private void OnPressed(InputAction.CallbackContext context)
        {
            if (hitArea == null || !hitArea.enabled || !hitArea.gameObject.activeInHierarchy || inputCamera == null)
                return;
            Vector2 screen = Pointer.current.position.ReadValue();
            Vector2 world = inputCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y,
                Mathf.Abs(inputCamera.transform.position.z)));
            if (!hitArea.OverlapPoint(world)) return;
            Play();
            UICanvasPrime previousPopup = uiManager.ActivePopup;
            PlayHaptic();
            onClick.Invoke();
            if (uiManager.ActivePopup == previousPopup) soundManager?.PlayClick();
        }

        // EN: Unsubscribes input and resets an interrupted visual without affecting gameplay.
        // VI: Gỡ input và trả hình ảnh bị ngắt về ban đầu mà không tác động gameplay.
        private void OnDisable()
        {
            press.performed -= OnPressed;
            press.Disable();
            if (animation != null) StopCoroutine(animation);
            animation = null;
            if (visual != null) visual.localScale = restScale;
        }

        // EN: Releases the input action owned exclusively by this effect.
        // VI: Giải phóng action input chỉ thuộc hiệu ứng này.
        private void OnDestroy()
        {
            press.Dispose();
        }

        // EN: Plays button haptics only when vibration is enabled.
        // VI: Rung khi nhấn nút chỉ khi cài đặt rung được bật.
        public void PlayHaptic()
        {
            if (ProfileManager.instance.PlayerDataManager.isVibrateOn) HapticPatterns.PlayEmphasis(0.8f, 0.5f);
        }
        // EN: Restarts visual feedback immediately without invoking or waiting for booster logic.
        // VI: Bắt đầu lại phản hồi hình ảnh ngay, không gọi hoặc chờ logic booster.
        public void Play()
        {
            if (!isActiveAndEnabled || visual == null) return;
            if (animation != null) StopCoroutine(animation);
            animation = StartCoroutine(Animate());
        }

        // EN: Compresses and rebounds the visual using unscaled time, then restores its original scale.
        // VI: Co và bật hình ảnh bằng thời gian độc lập, sau đó trả kích thước ban đầu.
        private IEnumerator Animate()
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / duration;
                float scale = t < 0.35f
                    ? Mathf.Lerp(pressedScale, reboundScale, Mathf.SmoothStep(0f, 1f, t / 0.35f))
                    : Mathf.Lerp(reboundScale, 1f, Mathf.SmoothStep(0f, 1f, (t - 0.35f) / 0.65f));
                visual.localScale = restScale * scale;
                yield return null;
            }
            visual.localScale = restScale;
            animation = null;
        }
    }
}
