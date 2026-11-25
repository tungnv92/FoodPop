using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: Animates tab width, icon position/scale and name visibility; the taskbar handles clicks and the shared sliding background.
    // VI: Chuyển động độ rộng tab, vị trí/scale icon và độ hiện tên; taskbar xử lý bấm và nền chọn dùng chung trượt ngang.
    [DisallowMultipleComponent]
    public sealed class UIHome_Tab : MonoBehaviour
    {
        public Button Button => button;

        [Tooltip("Nút phủ cả ô tab.")]
        [SerializeField] private Button button;
        [Tooltip("Icon của tab, được nâng lên khi tab đang được chọn.")]
        [SerializeField] private RectTransform icon;
        [Tooltip("Chữ tên tab, chỉ hiện khi tab đang được chọn.")]
        [SerializeField] private GameObject nameLabel;
        [Tooltip("Vị trí Y của icon khi tab không được chọn.")]
        [SerializeField] private float iconNormalY = 120f;
        [Tooltip("Vị trí Y của icon khi tab đang được chọn.")]
        [SerializeField] private float iconSelectedY = 165f;
        [Tooltip("Phần tử layout của ô tab, dùng để đặt độ rộng tương đối trong thanh tab.")]
        [SerializeField] private LayoutElement layoutElement;
        [Tooltip("Độ rộng tương đối của tab đang chọn so với tab thường (1.3 = rộng hơn 30%).")]
        [SerializeField] private float selectedWidthRatio = 1.3f;

        private Graphic nameGraphic;
        private Vector3 iconRestScale;
        private float nameAlpha;
        private float selectionProgress;
        private bool initialized;
        private bool isSelected;
        private Tween selectionTween;

        // EN: Cancels an interrupted transition and keeps the current selection at its finished appearance while hidden.
        // VI: Hủy chuyển động đang dở và giữ giao diện hoàn tất của trạng thái chọn hiện tại khi tab bị ẩn.
        private void OnDisable()
        {
            selectionTween?.Kill();
            selectionTween = null;
            if (initialized) ApplySelection(isSelected ? 1f : 0f);
        }

        // EN: Sets the initial state immediately, then animates selection in unscaled time over the taskbar-provided duration.
        // VI: Đặt trạng thái đầu ngay, sau đó chuyển trạng thái chọn theo thời gian độc lập trong khoảng thời gian taskbar truyền vào.
        public void SetSelected(bool selected, float duration = 0.22f)
        {
            if (!initialized)
            {
                CacheVisuals();
                isSelected = selected;
                ApplySelection(selected ? 1f : 0f);
                return;
            }
            if (isSelected == selected) return;
            isSelected = selected;
            selectionTween?.Kill();
            selectionTween = null;
            float target = selected ? 1f : 0f;
            if (!Application.isPlaying || duration <= 0f)
            {
                ApplySelection(target);
                return;
            }
            selectionTween = DOVirtual.Float(selectionProgress, target, duration, ApplySelection)
                .SetEase(Ease.OutCubic).SetUpdate(true)
                .OnComplete(() => selectionTween = null);
        }

        // EN: Applies selection progress to width, icon height, 90–100 percent icon scale and name alpha.
        // VI: Áp dụng tiến độ chọn cho độ rộng, độ cao icon, scale icon từ 90 đến 100 phần trăm và alpha tên.
        private void ApplySelection(float progress)
        {
            selectionProgress = progress;
            layoutElement.flexibleWidth = Mathf.Lerp(1f, selectedWidthRatio, progress);
            icon.anchoredPosition = new Vector2(icon.anchoredPosition.x, Mathf.Lerp(iconNormalY, iconSelectedY, progress));
            icon.localScale = iconRestScale * Mathf.Lerp(0.9f, 1f, progress);
            nameLabel.SetActive(progress > 0f);
            Color color = nameGraphic.color;
            color.a = nameAlpha * progress;
            nameGraphic.color = color;
        }

        // EN: Caches authored icon scale and name alpha once, even if selection is initialized before child Awake.
        // VI: Cache scale icon thiết kế và alpha tên một lần, kể cả khi trạng thái chọn được đặt trước Awake của tab con.
        private void CacheVisuals()
        {
            nameGraphic = nameLabel.GetComponent<Graphic>();
            iconRestScale = icon.localScale;
            nameAlpha = nameGraphic.color.a;
            initialized = true;
        }
    }
}
