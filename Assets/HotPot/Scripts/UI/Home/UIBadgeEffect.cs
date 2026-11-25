using DG.Tweening;
using UnityEngine;

namespace FoodPop
{
    // EN: Attention effect for an alert badge: pops in when it appears, then loops a short wobble-and-pulse with a pause while visible; owners only toggle the badge object.
    // VI: Hiệu ứng thu hút cho chấm báo: nảy lên khi xuất hiện, sau đó lặp nhịp lắc và phóng nhẹ có nghỉ khi đang hiện; nơi dùng chỉ cần bật/tắt object chấm báo.
    [DisallowMultipleComponent]
    public sealed class UIBadgeEffect : MonoBehaviour
    {
        [Tooltip("Tỉ lệ phóng to ở đỉnh nhịp.")]
        [SerializeField] private float pulseScale = 1.2f;
        [Tooltip("Góc lắc tối đa (độ).")]
        [SerializeField] private float wobbleAngle = 15f;
        [Tooltip("Thời gian nghỉ giữa hai nhịp (giây).")]
        [SerializeField, Min(0f)] private float interval = 1.2f;

        private Sequence loop;

        // EN: Pops the badge in from zero, then starts the looping attention pulse in unscaled time.
        // VI: Nảy chấm báo từ 0 lên, sau đó bắt đầu nhịp thu hút lặp lại theo thời gian độc lập.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            transform.localScale = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            loop = DOTween.Sequence().SetUpdate(true)
                .Append(transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack))
                .OnComplete(StartPulse);
        }

        // EN: Loops a pulse with a wobble followed by a pause (infinite loops cannot be nested, so this is a separate sequence).
        // VI: Lặp nhịp phóng to kèm lắc rồi nghỉ (không lồng được vòng lặp vô hạn nên dùng sequence riêng).
        private void StartPulse()
        {
            loop = DOTween.Sequence().SetUpdate(true)
                .Append(transform.DOScale(pulseScale, 0.15f).SetEase(Ease.OutQuad))
                .Join(transform.DOPunchRotation(new Vector3(0f, 0f, wobbleAngle), 0.45f, 8, 0.5f))
                .Append(transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack))
                .AppendInterval(interval)
                .SetLoops(-1);
        }

        // EN: Stops the effect and restores the authored scale and rotation.
        // VI: Dừng hiệu ứng và trả lại scale, góc xoay thiết kế.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            loop?.Kill();
            loop = null;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
        }
    }
}
