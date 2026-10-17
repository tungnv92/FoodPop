using System.Collections;
using TMPro;
using UnityEngine;

namespace FoodPop
{
    // EN: One line of a customer's order card: the food icon, how many are still needed and a checkmark once the line is complete.
    // VI: Một dòng trên thẻ order của khách: icon món, số lượng còn thiếu và dấu tích khi dòng đó đã đủ.
    [DisallowMultipleComponent]
    public class OrderItemView : MonoBehaviour
    {
        [Tooltip("Renderer hiện icon món được order; món bay tới sẽ đáp vào đây.")]
        [SerializeField] private SpriteRenderer ingredientRenderer;
        [Tooltip("Object chứa nhãn số lượng (ẩn khi món đã đủ).")]
        [SerializeField] private GameObject countRoot;
        [Tooltip("Text hiển thị số lượng còn thiếu.")]
        [SerializeField] private TMP_Text countText;
        [Tooltip("Renderer dấu tích hiện khi món đã nhận đủ.")]
        [SerializeField] private SpriteRenderer checkmark;
        [Tooltip("Thời gian (giây) hiệu ứng nảy khi số lượng thay đổi hoặc hiện dấu tích.")]
        [Min(0.01f)] [SerializeField] private float popDuration = 0.25f;
        [Tooltip("Độ phóng to tối đa của hiệu ứng nảy (0.3 = to thêm 30%).")]
        [Range(0f, 1f)] [SerializeField] private float popAmount = 0.3f;

        private Vector3 iconBaseScale = Vector3.one;
        private Vector3 checkBaseScale = Vector3.one;
        private Coroutine popRoutine;

        // EN: World position where served food lands (the icon), falling back to this line's position.
        // VI: Vị trí world mà món được phục vụ đáp xuống (icon), nếu không có thì lấy vị trí dòng này.
        public Vector3 TargetPosition => ingredientRenderer != null ? ingredientRenderer.transform.position : transform.position;

        // EN: World scale of the icon, used as the size a flying food shrinks to on arrival.
        // VI: Kích thước world của icon, dùng làm kích thước món bay thu nhỏ về khi tới nơi.
        public Vector3 TargetScale => ingredientRenderer != null ? ingredientRenderer.transform.lossyScale : transform.lossyScale;

        // EN: Sorting of the icon so flying food can be drawn above the order card.
        // VI: Thứ tự vẽ của icon để món bay được vẽ trên thẻ order.
        public SpriteRenderer IngredientRenderer => ingredientRenderer;

        // EN: Remembers the resting scales of the icon and checkmark for the pop animations.
        // VI: Ghi nhớ scale gốc của icon và dấu tích cho hiệu ứng nảy.
        private void Awake()
        {
            if (ingredientRenderer != null)
                iconBaseScale = ingredientRenderer.transform.localScale;
            if (checkmark != null)
                checkBaseScale = checkmark.transform.localScale;
        }

        // EN: Shows the line with a food icon and the required count, without a checkmark.
        // VI: Hiện dòng với icon món và số lượng yêu cầu, chưa có dấu tích.
        public void Show(Sprite icon, int count)
        {
            StopPop();
            gameObject.SetActive(true);
            if (ingredientRenderer != null)
                ingredientRenderer.sprite = icon;
            if (checkmark != null)
                checkmark.gameObject.SetActive(false);
            SetCountVisible(true);
            SetCountText(count);
        }

        // EN: Hides the whole line (used when the order has fewer foods than the card has lines).
        // VI: Ẩn cả dòng (dùng khi order có ít món hơn số dòng của thẻ).
        public void Hide()
        {
            StopPop();
            gameObject.SetActive(false);
        }

        // EN: Updates the remaining count and gives the icon a small pop.
        // VI: Cập nhật số lượng còn thiếu và làm icon nảy nhẹ.
        public void SetRemaining(int remaining)
        {
            SetCountText(remaining);
            PlayPop(ingredientRenderer != null ? ingredientRenderer.transform : null, iconBaseScale);
        }

        // EN: Marks the line as complete: hides the count and pops the checkmark in.
        // VI: Đánh dấu dòng đã đủ: ẩn số lượng và cho dấu tích bật ra.
        public void MarkDone()
        {
            SetCountVisible(false);
            if (checkmark == null)
                return;
            checkmark.gameObject.SetActive(true);
            PlayPop(checkmark.transform, checkBaseScale);
        }

        // EN: Writes the count into the text label.
        // VI: Ghi số lượng vào nhãn text.
        private void SetCountText(int count)
        {
            if (countText != null)
                countText.text = Mathf.Max(0, count).ToString();
        }

        // EN: Shows or hides the count label.
        // VI: Hiện hoặc ẩn nhãn số lượng.
        private void SetCountVisible(bool visible)
        {
            if (countRoot != null)
                countRoot.SetActive(visible);
        }

        // EN: Restarts the pop animation on the given transform.
        // VI: Chạy lại hiệu ứng nảy trên transform đã cho.
        private void PlayPop(Transform target, Vector3 baseScale)
        {
            if (target == null || !isActiveAndEnabled)
                return;
            StopPop();
            popRoutine = StartCoroutine(Pop(target, baseScale));
        }

        // EN: Grows the target quickly then eases it back to its resting scale.
        // VI: Phóng to nhanh rồi thu dần về scale gốc.
        private IEnumerator Pop(Transform target, Vector3 baseScale)
        {
            float time = 0f;
            while (time < popDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / popDuration);
                target.localScale = baseScale * (1f + popAmount * Mathf.Sin(t * Mathf.PI) * (1f - 0.5f * t));
                yield return null;
            }
            target.localScale = baseScale;
            popRoutine = null;
        }

        // EN: Stops a running pop and restores both resting scales.
        // VI: Dừng hiệu ứng nảy đang chạy và trả cả hai về scale gốc.
        private void StopPop()
        {
            if (popRoutine != null)
                StopCoroutine(popRoutine);
            popRoutine = null;
            if (ingredientRenderer != null)
                ingredientRenderer.transform.localScale = iconBaseScale;
            if (checkmark != null)
                checkmark.transform.localScale = checkBaseScale;
        }

        // EN: Restores the resting look when the line is disabled mid-animation.
        // VI: Khôi phục hình gốc khi dòng bị tắt giữa lúc đang chạy hiệu ứng.
        private void OnDisable()
        {
            StopPop();
        }
    }
}
