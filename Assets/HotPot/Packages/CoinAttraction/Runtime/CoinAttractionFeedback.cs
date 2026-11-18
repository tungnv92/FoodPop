using AssetKits.ParticleImage;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    [DisallowMultipleComponent]
    public sealed class CoinAttractionFeedback : MonoBehaviour
    {
        [Tooltip("Bộ phát xu báo khi từng xu đến ví.")]
        [SerializeField] private ParticleImage particles;
        [Tooltip("Biểu tượng xu trên ví nảy nhẹ khi nhận xu.")]
        [SerializeField] private RectTransform walletIcon;
        [Tooltip("Ảnh ánh sáng phía sau biểu tượng ví.")]
        [SerializeField] private Image impactGlow;
        [Tooltip("Thời gian phản hồi mỗi lần xu đến ví, tính bằng giây không phụ thuộc tốc độ game.")]
        [SerializeField, Min(0.01f)] private float impactDuration = 0.15f;
        [Tooltip("Mức phóng thêm của biểu tượng ví khi xu chạm vào.")]
        [SerializeField, Range(0f, 0.3f)] private float impactScale = 0.15f;

        private Vector3 restScale;
        private Coroutine impact;

        // EN: Captures the authored wallet scale when the prefab already has a target assigned.
        // VI: Ghi lại scale thiết kế của ví khi prefab đã được gán điểm đến.
        private void Awake()
        {
            if (walletIcon != null) restScale = walletIcon.localScale;
        }

        // EN: Listens to actual particle arrivals and clears the previous flight's visual feedback.
        // VI: Theo dõi lúc từng hạt xu đến ví và xóa phản hồi của chuyến bay trước.
        private void OnEnable()
        {
            particles.onStart.AddListener(ResetFeedback);
            particles.onParticleFinish.AddListener(PlayImpact);
            ResetFeedback();
        }

        // EN: Removes arrival listeners, clears flying coins and restores the wallet when the effect is hidden or destroyed.
        // VI: Gỡ theo dõi xu đến, xóa các xu đang bay và khôi phục ví khi hiệu ứng bị ẩn hoặc hủy.
        private void OnDisable()
        {
            particles.onStart.RemoveListener(ResetFeedback);
            particles.onParticleFinish.RemoveListener(PlayImpact);
            Stop();
        }

        // EN: Binds a wallet target and starts a fresh flight without depending on any game resource manager.
        // VI: Gán ví đích và chạy chuyến bay mới mà không phụ thuộc manager tài nguyên của game.
        public void Play(RectTransform target)
        {
            Stop();
            walletIcon = target;
            restScale = target.localScale;
            particles.attractorTarget = target;
            particles.Play();
        }

        // EN: Clears flying coins and impact feedback before cancellation or reuse.
        // VI: Xóa các xu đang bay và phản hồi chạm ví trước khi hủy hoặc tái sử dụng.
        public void Stop()
        {
            particles.Stop(true);
            ResetFeedback();
        }

        // EN: Stops pending feedback and restores an assigned wallet without changing any resource balance.
        // VI: Dừng phản hồi và khôi phục ví đã gán mà không thay đổi số dư tài nguyên.
        private void ResetFeedback()
        {
            if (impact != null) StopCoroutine(impact);
            impact = null;
            if (walletIcon != null) walletIcon.localScale = restScale;
            Color color = impactGlow.color;
            color.a = 0f;
            impactGlow.color = color;
        }

        // EN: Places the prefab-owned glow at the wallet and starts an unscaled impact when a target is assigned.
        // VI: Đặt ánh sáng thuộc prefab tại ví và chạy phản hồi không phụ thuộc time scale khi đã có điểm đến.
        private void PlayImpact()
        {
            ResetFeedback();
            if (walletIcon == null) return;
            impactGlow.rectTransform.position = walletIcon.position;
            impact = StartCoroutine(AnimateImpact());
        }

        // EN: Animates a smooth wallet pulse and fading glow using unscaled time, with no tween-library dependency.
        // VI: Diễn hoạt ví nảy mềm và ánh sáng mờ dần theo thời gian unscaled, không phụ thuộc thư viện tween.
        private IEnumerator AnimateImpact()
        {
            float elapsed = 0f;
            while (elapsed < impactDuration)
            {
                float progress = elapsed / impactDuration;
                walletIcon.localScale = restScale * (1f + impactScale * Mathf.Sin(progress * Mathf.PI));
                Color color = impactGlow.color;
                color.a = 0.75f * (1f - progress);
                impactGlow.color = color;
                yield return null;
                elapsed += Time.unscaledDeltaTime;
            }
            impact = null;
            walletIcon.localScale = restScale;
            Color finalColor = impactGlow.color;
            finalColor.a = 0f;
            impactGlow.color = finalColor;
        }
    }
}
