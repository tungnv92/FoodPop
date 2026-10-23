using System.Collections;
using UnityEngine;

namespace FoodPop
{
    // EN: Owns booster hit areas, availability tint and denied-use shake; BoosterController starts gameplay immediately.
    // VI: Quản lý vùng chạm, màu khả dụng và nhịp lắc khi bị từ chối; BoosterController bắt đầu gameplay ngay.
    public abstract class Booster : MonoBehaviour
    {
        [Tooltip("Icon của booster, cũng là vùng chạm để kích hoạt.")]
        [SerializeField] private SpriteRenderer icon;
        private Coroutine shakeRoutine;

        // EN: Which booster this is.
        // VI: Loại booster.
        public abstract BoosterType Type { get; }

        // EN: The icon renderer (subclasses may reuse its sprite).
        // VI: Renderer icon (lớp con có thể dùng lại sprite của nó).
        protected SpriteRenderer Icon => icon;

        // EN: Restores icon rotation when hidden during a denied-use shake.
        // VI: Khôi phục góc xoay icon khi bị ẩn giữa nhịp lắc từ chối.
        private void OnDisable()
        {
            shakeRoutine = null;
            if (icon != null)
                icon.transform.localRotation = Quaternion.identity;
        }

        [Tooltip("Độ sáng của icon khi booster đang không dùng được (có booster khác đang chạy).")]
        [Range(0f, 1f)] [SerializeField] private float unavailableBrightness = 0.55f;

        // EN: Dims unavailable booster icons directly and restores brightness when available.
        // VI: Làm mờ trực tiếp icon booster không khả dụng và khôi phục độ sáng khi dùng được.
        public void SetAvailable(bool available)
        {
            float value = available ? 1f : unavailableBrightness;
            if (icon != null)
                icon.color = new Color(value, value, value, icon.color.a);
        }

        // EN: True when the world point is on this booster's icon.
        // VI: True khi điểm thế giới nằm trên icon của booster.
        public bool Contains(Vector2 worldPoint)
        {
            if (icon == null || !icon.enabled || !icon.gameObject.activeInHierarchy)
                return false;
            Bounds bounds = icon.bounds;
            return worldPoint.x >= bounds.min.x && worldPoint.x <= bounds.max.x && worldPoint.y >= bounds.min.y && worldPoint.y <= bounds.max.y;
        }

        // EN: Shakes the icon sideways when the booster cannot be used, without a press animation.
        // VI: Lắc icon sang ngang khi booster không dùng được, không chạy hiệu ứng nhấn.
        public void PlayDenied()
        {
            if (icon == null || !isActiveAndEnabled)
                return;
            if (shakeRoutine != null)
                StopCoroutine(shakeRoutine);
            icon.transform.localRotation = Quaternion.identity;
            shakeRoutine = StartCoroutine(Shake());
        }

        // EN: Quick left-right tilt shake.
        // VI: Lắc nghiêng trái phải nhanh.
        private IEnumerator Shake()
        {
            const float Duration = 0.3f;
            for (float time = 0f; time < Duration; time += Time.deltaTime)
            {
                float t = time / Duration;
                icon.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 6f) * 12f * (1f - t));
                yield return null;
            }
            icon.transform.localRotation = Quaternion.identity;
            shakeRoutine = null;
        }

#if UNITY_EDITOR
        // EN: Editor-only: assigns the icon renderer used for hit detection and availability tint.
        // VI: Chỉ dùng trong editor: gán renderer icon dùng cho vùng chạm và màu khả dụng.
        public void SetIcon(SpriteRenderer renderer)
        {
            icon = renderer;
        }
#endif
    }
}
