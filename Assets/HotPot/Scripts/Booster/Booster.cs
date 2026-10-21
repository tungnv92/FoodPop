using System.Collections;
using UnityEngine;

namespace FoodPop
{
    // EN: Base of every booster button: owns its icon and icon feedback (the button-press click from ButtonPressFx on every use, plus a sideways shake when the booster cannot be used); the gameplay logic is run by BoosterController (use counts will come from separate data).
    // VI: Lớp gốc của mọi nút booster: giữ icon và phản hồi trên icon (hiệu ứng bấm nút của ButtonPressFx mỗi lần dùng, cộng thêm lắc ngang khi không dùng được); logic chơi do BoosterController chạy (số lượt sẽ lấy từ data riêng sau).
    public abstract class Booster : MonoBehaviour
    {
        [Tooltip("Icon của booster, cũng là vùng chạm để kích hoạt.")]
        [SerializeField] private SpriteRenderer icon;
        [Tooltip("Hiệu ứng bấm nút khi chạm vào booster; để trống sẽ tự thêm lúc chạy.")]
        [SerializeField] private ButtonPressFx pressFx;

        private Coroutine shakeRoutine;

        // EN: Which booster this is.
        // VI: Loại booster.
        public abstract BoosterType Type { get; }

        // EN: The icon renderer (subclasses may reuse its sprite).
        // VI: Renderer icon (lớp con có thể dùng lại sprite của nó).
        protected SpriteRenderer Icon => icon;

        // EN: Length (seconds) of the click animation, so callers can wait for it before hiding the button.
        // VI: Thời gian (giây) của hiệu ứng bấm, để nơi gọi có thể chờ xong rồi mới ẩn nút.
        public float PressDuration => PressFx != null ? PressFx.Duration : 0f;

        // EN: The click effect: the assigned one, otherwise one found or added on the icon (configured to scale and tint the icon).
        // VI: Hiệu ứng bấm: cái đã gán, nếu không thì tìm hoặc thêm trên icon (cấu hình co giãn và đổi màu icon).
        private ButtonPressFx PressFx
        {
            get
            {
                if (pressFx == null && icon != null)
                {
                    pressFx = icon.GetComponent<ButtonPressFx>();
                    if (pressFx == null)
                    {
                        pressFx = icon.gameObject.AddComponent<ButtonPressFx>();
                        pressFx.Configure(icon.transform, icon);
                    }
                }
                return pressFx;
            }
        }

        // EN: Puts the icon rotation back when the button is hidden mid-shake (the click effect restores its own scale and colour).
        // VI: Trả lại góc xoay của icon khi nút bị ẩn giữa lúc lắc (hiệu ứng bấm tự trả lại cỡ và màu của nó).
        private void OnDisable()
        {
            shakeRoutine = null;
            if (icon != null)
                icon.transform.localRotation = Quaternion.identity;
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

        // EN: Booster accepted: plays the button click.
        // VI: Booster được dùng: chạy hiệu ứng bấm nút.
        public void PlayActivated()
        {
            if (PressFx != null)
                PressFx.Play();
        }

        // EN: Booster cannot be used now: plays the button click and shakes the icon sideways.
        // VI: Lúc này không dùng được booster: chạy hiệu ứng bấm nút và lắc icon sang ngang.
        public void PlayDenied()
        {
            if (PressFx != null)
                PressFx.Play();
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
        // EN: Editor-only: assigns the icon renderer and its click effect (used by the setup tool).
        // VI: Chỉ dùng trong editor: gán renderer icon và hiệu ứng bấm của nó (công cụ setup gọi).
        public void SetIcon(SpriteRenderer renderer, ButtonPressFx press = null)
        {
            icon = renderer;
            if (press != null)
                pressFx = press;
        }
#endif
    }
}
