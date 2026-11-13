using System.Collections;
using UnityEngine;

namespace FoodPop
{
    [DisallowMultipleComponent]
    public sealed class SaltedEdamameAnimation : AnimatorFinalFoodAnimation
    {
        [Tooltip("Hình đậu thường trong lúc rắc muối.")]
        [SerializeField] private SpriteRenderer plainEdamame;
        [Tooltip("Hình đậu muối sau khi rắc xong.")]
        [SerializeField] private SpriteRenderer saltedEdamame;

        // EN: Start the completed picture transparent so the Animator can fade it over plain beans while salt falls.
        // VI: Bắt đầu hình hoàn chỉnh trong suốt để Animator hiện dần trên đậu thường khi muối rơi.
        public override IEnumerator Play()
        {
            plainEdamame.enabled = true;
            Color color = saltedEdamame.color;
            color.a = 0f;
            saltedEdamame.color = color;
            saltedEdamame.enabled = true;
            yield return base.Play();
        }

        // EN: Restore full opacity on interruption or pooling without overriding tray layer visibility.
        // VI: Khôi phục độ rõ khi ngắt hoặc trả pool, không ghi đè trạng thái ẩn/hiện lớp của khay.
        public override void ResetAnimation()
        {
            base.ResetAnimation();
            Color color = saltedEdamame.color;
            color.a = 1f;
            saltedEdamame.color = color;
        }

        // EN: Animation Event: hide the plain layer after the salted picture has fully faded in.
        // VI: Animation Event: ẩn lớp đậu thường sau khi hình đậu muối đã hiện đầy đủ.
        public void ShowSaltedDish()
        {
            plainEdamame.enabled = false;
            saltedEdamame.enabled = true;
        }
    }
}
