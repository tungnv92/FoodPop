using System.Collections;
using UnityEngine;

namespace FoodPop
{
    // EN: Button click feel for world-space sprites: on Play() the target squashes down and darkens slightly (press), then springs back up past its size with a small overshoot and settles (release). Restarting mid-animation starts again from the resting pose, and disabling the object snaps it back, so the button never gets stuck pressed.
    // VI: Cảm giác bấm nút cho sprite trong world: khi gọi Play(), đối tượng co nhỏ lại và tối nhẹ (nhấn xuống), rồi bật lên vượt cỡ một chút và về cỡ thường (thả ra). Gọi lại giữa chừng thì chạy lại từ tư thế nghỉ, và khi object bị tắt sẽ trả về ngay tư thế nghỉ, nên nút không bao giờ bị kẹt ở trạng thái nhấn.
    [DisallowMultipleComponent]
    public class ButtonPressFx : MonoBehaviour
    {
        [Tooltip("Transform được co giãn; để trống dùng chính object này.")]
        [SerializeField] private Transform target;
        [Tooltip("Sprite được làm tối khi nhấn; để trống thì không đổi màu.")]
        [SerializeField] private SpriteRenderer tintTarget;

        [Header("Nhấn xuống")]
        [Tooltip("Tỉ lệ cỡ khi nhấn xuống (0.88 = nhỏ đi 12%).")]
        [Range(0.5f, 1f)] [SerializeField] private float pressScale = 0.88f;
        [Tooltip("Thời gian (giây) co xuống.")]
        [Min(0.01f)] [SerializeField] private float pressDuration = 0.07f;
        [Tooltip("Độ sáng khi nhấn (1 = không đổi, 0.85 = tối đi 15%).")]
        [Range(0.3f, 1f)] [SerializeField] private float pressBrightness = 0.85f;

        [Header("Thả ra")]
        [Tooltip("Tỉ lệ cỡ cao nhất khi bật lên (1.06 = to hơn 6%).")]
        [Min(1f)] [SerializeField] private float releaseOvershoot = 1.06f;
        [Tooltip("Thời gian (giây) bật lên rồi về cỡ thường.")]
        [Min(0.01f)] [SerializeField] private float releaseDuration = 0.16f;

        private Vector3 restScale;
        private Color restColor;
        private float baseBrightness = 1f;
        private bool restCaptured;
        private Coroutine routine;

        // EN: Total length of the press + release animation (seconds).
        // VI: Tổng thời gian hiệu ứng nhấn + thả (giây).
        public float Duration => pressDuration + releaseDuration;

        // EN: Transform being animated (target, or this object).
        // VI: Transform đang được làm hiệu ứng (target, hoặc chính object này).
        private Transform Target => target != null ? target : transform;

        // EN: Plays the press-and-release animation from the resting pose.
        // VI: Chạy hiệu ứng nhấn và thả từ tư thế nghỉ.
        public void Play()
        {
            if (!isActiveAndEnabled)
                return;
            CaptureRest();
            if (routine != null)
                StopCoroutine(routine);
            ApplyPose(1f, 1f);
            routine = StartCoroutine(PressAndRelease());
        }

        // EN: Snaps back to the resting pose when the object is turned off mid-animation.
        // VI: Trả về tư thế nghỉ khi object bị tắt giữa lúc chạy hiệu ứng.
        private void OnDisable()
        {
            routine = null;
            if (restCaptured)
                ApplyPose(1f, 1f);
        }

        // EN: Remembers the resting scale and colour the first time the button is pressed.
        // VI: Ghi nhớ cỡ và màu lúc nghỉ ở lần nhấn đầu tiên.
        private void CaptureRest()
        {
            if (restCaptured)
                return;
            restScale = Target.localScale;
            if (tintTarget != null)
                restColor = tintTarget.color;
            restCaptured = true;
        }

        // EN: Press: eases down to pressScale and darkens; release: springs up past 1 to releaseOvershoot and settles back while the colour recovers.
        // VI: Nhấn: co dần tới pressScale và tối đi; thả: bật lên vượt 1 tới releaseOvershoot rồi về lại trong lúc màu sáng trở lại.
        private IEnumerator PressAndRelease()
        {
            for (float time = 0f; time < pressDuration; time += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, time / pressDuration);
                ApplyPose(Mathf.Lerp(1f, pressScale, t), Mathf.Lerp(1f, pressBrightness, t));
                yield return null;
            }
            for (float time = 0f; time < releaseDuration; time += Time.deltaTime)
            {
                float t = Mathf.Clamp01(time / releaseDuration);
                float scale = t < 0.5f
                    ? Mathf.Lerp(pressScale, releaseOvershoot, Mathf.SmoothStep(0f, 1f, t * 2f))
                    : Mathf.Lerp(releaseOvershoot, 1f, Mathf.SmoothStep(0f, 1f, (t - 0.5f) * 2f));
                ApplyPose(scale, Mathf.Lerp(pressBrightness, 1f, t));
                yield return null;
            }
            ApplyPose(1f, 1f);
            routine = null;
        }

        // EN: Sets the target to restScale * scale and its tint to the resting colour times brightness times the base brightness (alpha kept).
        // VI: Đặt cỡ target bằng restScale * scale và màu bằng màu lúc nghỉ nhân độ sáng nhân độ sáng nền (giữ nguyên alpha).
        private void ApplyPose(float scale, float brightness)
        {
            Target.localScale = restScale * scale;
            if (tintTarget != null)
            {
                float value = brightness * baseBrightness;
                tintTarget.color = new Color(restColor.r * value, restColor.g * value, restColor.b * value, restColor.a);
            }
        }

        // EN: Sets a lasting brightness multiplier for the tinted sprite (e.g. 0.55 while the button is unavailable); the press animation darkens on top of it, and it applies at once when no press is playing.
        // VI: Đặt hệ số độ sáng lâu dài cho sprite đổi màu (ví dụ 0.55 khi nút đang không dùng được); hiệu ứng nhấn làm tối thêm trên nền đó, và áp dụng ngay khi không có hiệu ứng nhấn đang chạy.
        public void SetBaseBrightness(float value)
        {
            baseBrightness = value;
            if (tintTarget == null)
                return;
            CaptureRest();
            if (routine == null)
                ApplyPose(1f, 1f);
        }

        // EN: Assigns the animated transform and the tinted sprite (used by the setup tool and when a booster adds this component at runtime). Must be called before the first Play().
        // VI: Gán transform được làm hiệu ứng và sprite đổi màu (công cụ setup gọi, hoặc booster gọi khi tự thêm component lúc chạy). Phải gọi trước lần Play() đầu tiên.
        public void Configure(Transform scaled, SpriteRenderer tinted)
        {
            target = scaled;
            tintTarget = tinted;
        }
    }
}
