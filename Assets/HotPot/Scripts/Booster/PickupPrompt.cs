using DG.Tweening;
using GamePackages;
using Lofelt.NiceVibrations;
using TMPro;
using UnityEngine;

namespace FoodPop
{
    // EN: The "pick a dish" prompt shown while PickupHand is selecting: owns its text (the BoosterConfig PickupPrompt short message) and the cancel button with its tap area and click feedback (the same squash pulse, haptic and click sound as popup close buttons). Sits on the prompt's root object.
    // VI: Thông báo "chọn đĩa" hiện khi PickupHand đang chọn: tự quản lý chữ (thông báo ngắn PickupPrompt trong BoosterConfig) và nút hủy cùng vùng chạm và phản hồi khi bấm (nhún, rung và tiếng click giống nút đóng của popup). Gắn trên object gốc của thông báo.
    public class PickupPrompt : MonoBehaviour
    {
        [Tooltip("Text hiển thị thông báo.")]
        [SerializeField] private TMP_Text promptText;
        [Tooltip("Sprite nút hủy, cũng là vùng chạm để hủy chọn.")]
        [SerializeField] private SpriteRenderer cancelButton;
        [Tooltip("Vùng chạm của nút hủy; để trống sẽ tự lấy BoxCollider2D trên nút hủy, không có thì tự thêm.")]
        [SerializeField] private BoxCollider2D cancelCollider;
        [Tooltip("Tỉ lệ co nút hủy khi bấm (giống nút đóng của popup).")]
        [SerializeField, Range(0.8f, 1f)] private float cancelPressedScale = 0.94f;

        private Vector3 cancelRestScale;
        private bool hasCancelRestScale;
        private Tween cancelTween;

        // EN: Stops a running cancel pulse and puts the button back to its authored scale.
        // VI: Dừng nhịp nhún nút hủy đang chạy và trả nút về tỉ lệ thiết kế.
        private void OnDisable()
        {
            StopCancelPulse();
        }

        // EN: Shows the prompt with its current BoosterConfig text, or hides it at once (stopping a cancel pulse).
        // VI: Hiện thông báo với nội dung hiện tại trong BoosterConfig, hoặc ẩn ngay (dừng nhịp nhún nút hủy).
        public void Show(bool visible)
        {
            StopCancelPulse();
            if (promptText != null && visible)
                promptText.text = IngameConfig.Instance.Boosters.GetShortDes(BoosterConfig.ShortDesType.PickupPrompt);
            gameObject.SetActive(visible);
        }

        // EN: Plays the popup close-button feedback on the cancel button (haptic, click sound, squash and spring back on unscaled time), then hides the prompt once the pulse ends.
        // VI: Phát phản hồi giống nút đóng popup trên nút hủy (rung, tiếng click, co rồi bật về theo thời gian độc lập), sau đó ẩn thông báo khi nhịp nhún kết thúc.
        public void PlayCancelAndHide()
        {
            PlayClickFeedback();
            StopCancelPulse();
            if (cancelButton == null)
            {
                gameObject.SetActive(false);
                return;
            }
            Transform button = cancelButton.transform;
            cancelRestScale = button.localScale;
            hasCancelRestScale = true;
            cancelTween = DOTween.Sequence().SetUpdate(true)
                .Append(button.DOScale(cancelRestScale * cancelPressedScale, 0.05f))
                .Append(button.DOScale(cancelRestScale, 0.16f).SetEase(Ease.OutBack))
                .OnComplete(() => gameObject.SetActive(false));
        }

        // EN: True when the world point is inside the cancel button's BoxCollider2D while the button is shown.
        // VI: True khi điểm world nằm trong BoxCollider2D của nút hủy lúc nút đang hiện.
        public bool CancelContains(Vector2 worldPoint)
        {
            BoxCollider2D area = ResolveCancelArea();
            return area != null && area.enabled && area.gameObject.activeInHierarchy && area.OverlapPoint(worldPoint);
        }

        // EN: The cancel button's tap area: the assigned collider, otherwise the BoxCollider2D on the cancel button, added as a trigger (sized to the sprite) when missing.
        // VI: Vùng chạm của nút hủy: collider đã gán, nếu không thì BoxCollider2D trên nút hủy, tự thêm dạng trigger (khớp cỡ sprite) khi chưa có.
        private BoxCollider2D ResolveCancelArea()
        {
            if (cancelCollider == null && cancelButton != null)
            {
                cancelCollider = cancelButton.GetComponent<BoxCollider2D>();
                if (cancelCollider == null)
                {
                    cancelCollider = cancelButton.gameObject.AddComponent<BoxCollider2D>();
                    cancelCollider.isTrigger = true;
                }
            }
            return cancelCollider;
        }

        // EN: Plays click haptics and sound when the player's settings allow them, like popup buttons.
        // VI: Phát rung và tiếng click khi cài đặt người chơi cho phép, giống các nút trong popup.
        private static void PlayClickFeedback()
        {
            PlayerDataManager playerData = ProfileManager.instance.PlayerDataManager;
            if (playerData.isVibrateOn) HapticPatterns.PlayEmphasis(0.8f, 0.5f);
            if (playerData.IsSoundOn) IngameSoundManager.Instance.PlayClick();
        }

        // EN: Kills a running cancel pulse and restores the button's authored scale.
        // VI: Hủy nhịp nhún nút hủy đang chạy và trả nút về tỉ lệ thiết kế.
        private void StopCancelPulse()
        {
            cancelTween?.Kill();
            cancelTween = null;
            if (hasCancelRestScale && cancelButton != null)
                cancelButton.transform.localScale = cancelRestScale;
        }
    }
}
