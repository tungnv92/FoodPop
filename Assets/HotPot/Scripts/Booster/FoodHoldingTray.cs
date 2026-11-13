using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace FoodPop
{
    // EN: The holding bag: back layer, stored food and front layer. Exposes the bag mouth for flights, pops in when used and out when empty, and plays a soft squash bounce when food goes in or out.
    // VI: Túi giữ món: lớp sau, food đang giữ và lớp trước. Cung cấp vị trí miệng túi cho các chuyến bay, bật hiện khi dùng và thu ẩn khi trống, nảy nhún mềm khi food vào hoặc ra.
    public sealed class FoodHoldingTray : MonoBehaviour
    {
        [Tooltip("Thân túi (chứa lớp sau, food và lớp trước); đây là phần nảy nhún và phần hiện/ẩn.")]
        [FormerlySerializedAs("drawer")]
        [SerializeField] private Transform body;
        [Tooltip("Điểm miệng túi; để trống thì dùng thân túi cộng mouthOffset.")]
        [SerializeField] private Transform mouth;
        [Tooltip("Vị trí miệng túi so với thân túi (tọa độ cục bộ), dùng khi chưa gán mouth.")]
        [SerializeField] private Vector3 mouthOffset = new Vector3(0f, 0.62f, 0f);
        [Tooltip("Độ cao đáy túi trong tọa độ cục bộ của thân túi; túi nhún và mọc lên quanh đáy này để không bị nhấc khỏi bàn.")]
        [SerializeField] private float bottomY = -0.9f;
        [Tooltip("Thời gian một nhịp nảy nhún, tính bằng giây.")]
        [SerializeField, Min(0.05f)] private float bounceDuration = 0.28f;
        [Tooltip("Độ nhún tối đa của túi (0.1 = bẹp 10%).")]
        [SerializeField, Range(0f, 0.4f)] private float bounceStrength = 0.1f;

        [Header("Hiện / ẩn")]
        [Tooltip("Ẩn túi khi vào màn chơi; túi chỉ hiện khi booster Clean dùng tới.")]
        [SerializeField] private bool startHidden = true;
        [Tooltip("Thời gian túi bật hiện (mọc lên từ đáy, nảy nhẹ), tính bằng giây.")]
        [SerializeField, Min(0.01f)] private float showDuration = 0.28f;
        [Tooltip("Thời gian túi thu nhỏ rồi ẩn đi, tính bằng giây.")]
        [SerializeField, Min(0.01f)] private float hideDuration = 0.2f;

        private Vector3 restScale;
        private Vector3 restPosition;
        private bool hasRest;
        private Coroutine bounce;
        private Coroutine fade;
        private float bounceStartTime;
        private float squash;
        private float appear = 1f;
        private bool shown = true;

        // EN: The transform that bounces and pops; falls back to this object when no body is assigned.
        // VI: Transform được nảy nhún và hiện/ẩn; dùng chính object này nếu chưa gán thân túi.
        public Transform Body => body != null ? body : transform;

        // EN: World position of the bag mouth, where food enters and leaves.
        // VI: Vị trí world của miệng túi, nơi food đi vào và đi ra.
        public Vector3 MouthPosition => mouth != null ? mouth.position : Body.TransformPoint(mouthOffset);

        // EN: True while the bag is shown or popping in.
        // VI: True khi túi đang hiện hoặc đang bật hiện.
        public bool IsShown => shown;

        // EN: Finds the "Bag" child when no body is assigned, remembers the authored pose and starts hidden when requested.
        // VI: Tự tìm object con "Bag" khi chưa gán thân túi, ghi nhớ tư thế thiết kế và ẩn túi lúc đầu nếu được yêu cầu.
        private void Awake()
        {
            if (body == null)
                body = transform.Find("Bag");
            CaptureRest();
            if (startHidden)
                HideImmediate();
        }

        // EN: Stops running animations and snaps to the target visibility without squash.
        // VI: Dừng các animation đang chạy và đặt ngay về trạng thái hiện/ẩn đích, không còn nhún.
        private void OnDisable()
        {
            bounce = null;
            fade = null;
            squash = 0f;
            appear = shown ? 1f : 0f;
            ApplyPose();
        }

        // EN: Pops the bag in from its bottom with a small overshoot; does nothing when it is already shown.
        // VI: Cho túi mọc lên từ đáy với nhịp vượt nhẹ; không làm gì nếu túi đã hiện.
        [ContextMenu("Test/Hiện túi")]
        public void Show()
        {
            if (shown)
                return;
            shown = true;
            StartFade(true);
        }

        // EN: Shrinks the bag down to its bottom and hides it; does nothing when it is already hidden.
        // VI: Thu túi xuống về phía đáy rồi ẩn; không làm gì nếu túi đã ẩn.
        [ContextMenu("Test/Ẩn túi")]
        public void Hide()
        {
            if (!shown)
                return;
            shown = false;
            StartFade(false);
        }

        // EN: Hides the bag at once (used when a level is reset).
        // VI: Ẩn túi ngay lập tức (dùng khi đặt lại màn chơi).
        public void HideImmediate()
        {
            CaptureRest();
            if (fade != null)
                StopCoroutine(fade);
            fade = null;
            shown = false;
            appear = 0f;
            ApplyPose();
        }

        // EN: Starts the show or hide animation from the current visibility (works outside Play Mode by snapping).
        // VI: Bắt đầu animation hiện hoặc ẩn từ mức hiện tại (ngoài Play Mode thì đặt ngay).
        private void StartFade(bool show)
        {
            CaptureRest();
            if (fade != null)
                StopCoroutine(fade);
            fade = null;
            if (!isActiveAndEnabled || !Application.isPlaying)
            {
                appear = show ? 1f : 0f;
                ApplyPose();
                return;
            }
            fade = StartCoroutine(show ? PlayShow() : PlayHide());
        }

        // EN: Grows from the current size to full size with an ease-out-back curve.
        // VI: Lớn dần từ kích thước hiện tại tới đầy đủ theo đường cong vượt nhẹ rồi về.
        private IEnumerator PlayShow()
        {
            float from = appear;
            for (float elapsed = 0f; elapsed < showDuration; elapsed += Time.deltaTime)
            {
                appear = Mathf.LerpUnclamped(from, 1f, EaseOutBack(elapsed / showDuration));
                ApplyPose();
                yield return null;
            }
            appear = 1f;
            ApplyPose();
            fade = null;
        }

        // EN: Shrinks from the current size to zero with an ease-in curve.
        // VI: Nhỏ dần từ kích thước hiện tại về không theo đường cong tăng tốc.
        private IEnumerator PlayHide()
        {
            float from = appear;
            for (float elapsed = 0f; elapsed < hideDuration; elapsed += Time.deltaTime)
            {
                float t = elapsed / hideDuration;
                appear = Mathf.Lerp(from, 0f, t * t);
                ApplyPose();
                yield return null;
            }
            appear = 0f;
            ApplyPose();
            fade = null;
        }

        // EN: Ease-out-back curve (overshoots slightly past 1 before settling).
        // VI: Đường cong ease-out-back (vượt nhẹ quá 1 rồi mới dừng).
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = Mathf.Clamp01(t) - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        // EN: Plays a soft squash-and-stretch bounce; rapid calls do not restart a bounce that has just begun.
        // VI: Nảy nhún mềm kiểu bẹp rồi giãn; gọi liên tục sẽ không khởi động lại nhịp nảy vừa bắt đầu.
        [ContextMenu("Test/Nảy túi")]
        public void Bounce()
        {
            if (!isActiveAndEnabled)
                return;
            CaptureRest();
            if (bounce != null && Time.time - bounceStartTime < bounceDuration * 0.4f)
                return;
            if (bounce != null)
                StopCoroutine(bounce);
            bounceStartTime = Time.time;
            bounce = StartCoroutine(PlayBounce());
        }

        // EN: Damped wave: squashes first, then stretches slightly, then settles.
        // VI: Sóng tắt dần: bẹp xuống trước, giãn nhẹ lên, rồi đứng yên.
        private IEnumerator PlayBounce()
        {
            for (float elapsed = 0f; elapsed < bounceDuration; elapsed += Time.deltaTime)
            {
                float t = elapsed / bounceDuration;
                squash = bounceStrength * Mathf.Sin(t * Mathf.PI * 2f) * (1f - t);
                ApplyPose();
                yield return null;
            }
            squash = 0f;
            ApplyPose();
            bounce = null;
        }

        // EN: Applies visibility and squash to the body, keeping its bottom edge fixed (positive squash = flatter and wider).
        // VI: Áp mức hiện/ẩn và độ nhún lên thân túi, giữ cố định mép đáy (nhún dương = thấp và rộng hơn).
        private void ApplyPose()
        {
            if (!hasRest)
                return;
            Transform target = Body;
            float scaleY = appear * (1f - squash);
            float scaleX = appear * (1f + squash * 0.6f);
            target.localScale = new Vector3(restScale.x * scaleX, restScale.y * scaleY, restScale.z);
            target.localPosition = restPosition + Vector3.up * (bottomY * restScale.y * (1f - scaleY));
        }

        // EN: Stores the rest pose once.
        // VI: Lưu tư thế nghỉ một lần.
        private void CaptureRest()
        {
            if (hasRest)
                return;
            restScale = Body.localScale;
            restPosition = Body.localPosition;
            hasRest = true;
        }
    }
}
