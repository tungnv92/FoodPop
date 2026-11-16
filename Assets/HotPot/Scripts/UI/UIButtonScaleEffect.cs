using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FoodPop
{
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class UIButtonScaleEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Tooltip("Tỉ lệ co nút khi nhấn.")]
        [SerializeField, Range(0.8f, 1f)] private float pressedScale = 0.94f;
        private Button button;
        private Vector3 restScale;
        private Tween scaleTween;

        // EN: Caches the button and its authored scale once.
        // VI: Cache nút và tỉ lệ đã thiết kế một lần.
        private void Awake()
        {
            button = GetComponent<Button>();
            restScale = transform.localScale;
        }

        // EN: Adds click feedback for pointer and keyboard activation.
        // VI: Nối hiệu ứng click cho thao tác con trỏ và bàn phím.
        private void OnEnable()
        {
            if (Application.isPlaying) button.onClick.AddListener(PlayClick);
        }

        // EN: Compresses an interactable button immediately on pointer press using unscaled time.
        // VI: Co nút đang tương tác ngay khi nhấn con trỏ bằng thời gian độc lập.
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !button.IsInteractable()) return;
            scaleTween?.Kill();
            scaleTween = transform.DOScale(restScale * pressedScale, 0.06f).SetUpdate(true);
        }

        // EN: Restores scale when the pointer releases, including when no click follows.
        // VI: Trả tỉ lệ khi thả con trỏ, kể cả khi không có click sau đó.
        public void OnPointerUp(PointerEventData eventData)
        {
            RestoreScale();
        }

        // EN: Restores a pressed button when the pointer leaves its hit area.
        // VI: Trả nút đang co khi con trỏ ra khỏi vùng nhấn.
        public void OnPointerExit(PointerEventData eventData)
        {
            RestoreScale();
        }

        // EN: Removes the owned listener, cancels animation and restores the authored scale.
        // VI: Gỡ listener sở hữu, hủy hoạt cảnh và trả tỉ lệ đã thiết kế.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            button.onClick.RemoveListener(PlayClick);
            scaleTween?.Kill();
            transform.localScale = restScale;
        }

        // EN: Pulses the button once on click, including keyboard submit, while game time is paused.
        // VI: Nhún nút một lần khi click, gồm cả submit bàn phím, kể cả khi game tạm dừng.
        private void PlayClick()
        {
            if (!isActiveAndEnabled) return;
            scaleTween?.Kill();
            scaleTween = DOTween.Sequence().SetUpdate(true)
                .Append(transform.DOScale(restScale * pressedScale, 0.05f))
                .Append(transform.DOScale(restScale, 0.16f).SetEase(Ease.OutBack));
        }

        // EN: Springs the visual back without changing button logic.
        // VI: Bật hình nút về ban đầu mà không đổi logic nút.
        private void RestoreScale()
        {
            scaleTween?.Kill();
            scaleTween = transform.DOScale(restScale, 0.16f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }
}
