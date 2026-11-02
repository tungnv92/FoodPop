using AssetKits.ParticleImage;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class CurrencyDisplay : MonoBehaviour
{
    [System.Serializable]
    public class CurrencyAmount
    {
        [SerializeField, Range(0f, 2f)] float duration = 0.15f;
        [SerializeField] TMP_Text amountTmp;

        int previousAmount;

        public void InitAmount(int amount)
        {
            previousAmount = amount;
            amountTmp.text = amount.ToString();
        }

        void SetAmountTmp(int amount)
        {
            amountTmp.text = amount.ToString();
        }

        public void TweenAmount(int currentAmount)
        {
            DOVirtual.Int(previousAmount, currentAmount, duration, amount =>
            {
                SetAmountTmp(amount);
            }).SetEase(Ease.Linear);

            previousAmount = currentAmount;
        }
    }

    [System.Serializable]
    public class CurrencyIcon
    {
        [SerializeField] ParticleImage effect;
        [SerializeField, Range(0f, 1f)] float duration = 1f;
        [SerializeField, Range(1f, 2f)] float scale = 1.2f;
        [SerializeField] Transform transform;
        [SerializeField] AnimationCurve curve;

        public void TweenIcon()
        {
            effect.Play();
            transform.DOKill();
            transform.localScale = Vector3.one;
            transform.DOScale(scale, duration).SetEase(curve);
        }
    }

    [SerializeField] CurrencyAmount amount;
    public CurrencyAmount Amount => amount;
    [SerializeField] CurrencyIcon icon;
    public CurrencyIcon Icon => icon;

    // EN: Plays the currency icon scale and particle animation along with light haptic feedback.
    // VI: Phát hiệu ứng thu phóng và hạt cho icon tiền tệ cùng với phản hồi rung nhẹ.
    public void TweenIcon()
    {
        icon.TweenIcon();
        VibrationManager.Ins.Vibrate(HapticType.Light);
        //AudioManager.Ins.PlaySound(SoundType.CoinClaim);
    }
}
