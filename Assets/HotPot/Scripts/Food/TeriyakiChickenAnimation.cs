using System.Collections;
using UnityEngine;

namespace FoodPop
{
    [DisallowMultipleComponent]
    public sealed class TeriyakiChickenAnimation : AnimatorFinalFoodAnimation
    {
        [Tooltip("Hình thịt gà chưa có sốt trong lúc rưới sốt teriyaki.")]
        [SerializeField] private SpriteRenderer plainChicken;
        [Tooltip("Hình thịt gà phủ sốt teriyaki sau khi rưới xong.")]
        [SerializeField] private SpriteRenderer saucedChicken;

        // EN: Start the sauced picture transparent so the Animator can fade it over plain chicken while the sauce pours.
        // VI: Bắt đầu hình gà phủ sốt trong suốt để Animator hiện dần trên gà chưa sốt khi sốt đang rưới.
        public override IEnumerator Play()
        {
            plainChicken.enabled = true;
            Color color = saucedChicken.color;
            color.a = 0f;
            saucedChicken.color = color;
            saucedChicken.enabled = true;
            yield return base.Play();
        }

        // EN: Restore full opacity on interruption or pooling without overriding tray layer visibility.
        // VI: Khôi phục độ rõ khi ngắt hoặc trả pool, không ghi đè trạng thái ẩn/hiện lớp của khay.
        public override void ResetAnimation()
        {
            base.ResetAnimation();
            Color color = saucedChicken.color;
            color.a = 1f;
            saucedChicken.color = color;
        }

        // EN: Animation Event: hide the plain layer after the sauced picture has fully faded in.
        // VI: Animation Event: ẩn lớp gà chưa sốt sau khi hình gà phủ sốt đã hiện đầy đủ.
        public void ShowSaucedDish()
        {
            plainChicken.enabled = false;
            saucedChicken.enabled = true;
        }
    }
}
