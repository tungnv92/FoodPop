using System.Collections;
using TMPro;
using UnityEngine;

namespace FoodPop
{
    // EN: One slot of a customer's order card: the ordered final dish picture (popping as its ingredients arrive) and a checkmark once the dish is done. Can still show a plain ingredient icon with a count.
    // VI: Một ô trên thẻ order của khách: ảnh món hoàn chỉnh được order (nảy nhẹ khi nguyên liệu tới) và dấu tích khi món xong. Vẫn có thể hiện icon nguyên liệu kèm số lượng.
    [DisallowMultipleComponent]
    public class OrderItemView : MonoBehaviour
    {
        [Tooltip("Cấu hình thời gian và hiệu ứng dùng chung trong màn chơi; để trống dùng asset mặc định.")]
        [SerializeField] private IngameConfig ingameConfig;

        // EN: Resolves shared gameplay tuning while supporting an explicitly assigned configuration asset.
        // VI: Lấy thông số màn chơi dùng chung, hỗ trợ asset cấu hình được gán riêng.
        private IngameConfig Config => IngameConfig.Resolve(ingameConfig);

        [Tooltip("Renderer hiện icon món được order; món bay tới sẽ đáp vào đây.")]
        [SerializeField] private SpriteRenderer ingredientRenderer;
        [Tooltip("Object chứa nhãn số lượng (ẩn khi món đã đủ).")]
        [SerializeField] private GameObject countRoot;
        [Tooltip("Text hiển thị số lượng còn thiếu.")]
        [SerializeField] private TMP_Text countText;
        [Tooltip("Renderer dấu tích hiện khi món đã nhận đủ.")]
        [SerializeField] private SpriteRenderer checkmark;

        [Tooltip("Tỉ lệ ảnh món so với ô icon gốc (1 = to bằng icon nguyên liệu).")]
        [Min(0.1f)] [SerializeField] private float dishSizeRatio = 1.15f;
        [Header("Bóng món")]
        [Tooltip("Renderer bóng của ảnh món (child của icon, dùng cùng sprite với món).")]
        [SerializeField] private SpriteRenderer dishShadow;
        [Tooltip("Độ đậm của bóng món.")]
        [Range(0f, 1f)] [SerializeField] private float shadowOpacity = 0.35f;
        [Tooltip("Độ lệch của bóng so với món, tính theo đơn vị của ô order (không phụ thuộc cỡ ảnh món).")]
        [SerializeField] private Vector2 shadowOffset = new Vector2(0.05f, -0.06f);

        private Vector3 iconBaseScale = Vector3.one;
        private Vector3 iconDefaultScale = Vector3.one;
        private float iconSlotSize = 1f;
        private bool scalesCaptured;
        private Vector3 checkBaseScale = Vector3.one;
        private Coroutine iconPopRoutine;
        private Coroutine checkPopRoutine;
        private Coroutine dishFadeRoutine;

        // EN: World position where served food lands (the icon), falling back to this line's position.
        // VI: Vị trí world mà món được phục vụ đáp xuống (icon), nếu không có thì lấy vị trí dòng này.
        public Vector3 TargetPosition => ingredientRenderer != null ? ingredientRenderer.transform.position : transform.position;

        // EN: World scale of the icon, used as the size a flying food shrinks to on arrival.
        // VI: Kích thước world của icon, dùng làm kích thước món bay thu nhỏ về khi tới nơi.
        public Vector3 TargetScale => ingredientRenderer != null ? ingredientRenderer.transform.lossyScale : transform.lossyScale;

        // EN: Sorting of the icon so flying food can be drawn above the order card.
        // VI: Thứ tự vẽ của icon để món bay được vẽ trên thẻ order.
        public SpriteRenderer IngredientRenderer => ingredientRenderer;

        // EN: Captures the resting scales as soon as the slot wakes up.
        // VI: Ghi nhớ scale gốc ngay khi ô được khởi tạo.
        private void Awake()
        {
            CaptureBaseScales();
        }

        // EN: Remembers (once) the resting scales of the icon and checkmark for the pop animations, and the icon's visible size used to fit dish pictures.
        // VI: Ghi nhớ (một lần) scale gốc của icon và dấu tích cho hiệu ứng nảy, và kích thước hiển thị của icon để co ảnh món cho vừa.
        private void CaptureBaseScales()
        {
            if (scalesCaptured)
                return;
            scalesCaptured = true;
            if (ingredientRenderer != null)
            {
                iconBaseScale = ingredientRenderer.transform.localScale;
                iconDefaultScale = iconBaseScale;
                Sprite sprite = ingredientRenderer.sprite;
                float spriteSize = sprite != null ? Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y) : 1f;
                iconSlotSize = spriteSize * iconDefaultScale.x;
            }
            if (checkmark != null)
                checkBaseScale = checkmark.transform.localScale;
        }

        // EN: Shows the line with a food icon, without a checkmark; the count label only appears when more than one is needed (final-dish ingredients need one each).
        // VI: Hiện dòng với icon món, chưa có dấu tích; nhãn số lượng chỉ hiện khi cần nhiều hơn một (nguyên liệu món hoàn chỉnh chỉ cần một mỗi loại).
        public void Show(Sprite icon, int count)
        {
            CaptureBaseScales();
            StopPop();
            gameObject.SetActive(true);
            iconBaseScale = iconDefaultScale;
            if (ingredientRenderer != null)
            {
                ingredientRenderer.enabled = true;
                ingredientRenderer.sprite = icon;
                ingredientRenderer.color = Color.white;
                ingredientRenderer.transform.localScale = iconBaseScale;
            }
            if (dishShadow != null)
                dishShadow.enabled = false;
            if (checkmark != null)
            {
                checkmark.enabled = true;
                checkmark.gameObject.SetActive(false);
            }
            SetCountVisible(count > 1);
            SetCountText(count);
        }

        // EN: Shows an ordered final dish: its picture in full colour fitted to the slot, without count or checkmark.
        // VI: Hiện một món hoàn chỉnh được order: ảnh món màu gốc co vừa ô, không có số lượng hay dấu tích.
        public void ShowDish(Sprite dish)
        {
            CaptureBaseScales();
            StopPop();
            gameObject.SetActive(true);
            if (ingredientRenderer != null)
            {
                ingredientRenderer.enabled = true;
                ingredientRenderer.sprite = dish;
                float size = dish != null ? Mathf.Max(dish.bounds.size.x, dish.bounds.size.y) : 0f;
                float scale = size > 0f ? iconSlotSize * dishSizeRatio / size : iconDefaultScale.x;
                iconBaseScale = new Vector3(scale, scale, scale);
                ingredientRenderer.transform.localScale = iconBaseScale;
                ingredientRenderer.color = Color.white;
            }
            UpdateDishShadow(dish, iconBaseScale.x);
            if (checkmark != null)
            {
                checkmark.enabled = true;
                checkmark.gameObject.SetActive(false);
            }
            SetCountVisible(false);
        }

        // EN: Draws the dish's drop shadow: the same sprite in translucent black just behind the dish, offset by shadowOffset in slot units (compensating the dish scale so every picture size gets the same offset). Hidden when there is no dish.
        // VI: Vẽ bóng của món: cùng sprite màu đen bán trong suốt ngay sau món, lệch theo shadowOffset tính bằng đơn vị ô (bù scale ảnh món để mọi cỡ ảnh có cùng độ lệch). Ẩn khi không có món.
        private void UpdateDishShadow(Sprite dish, float dishScale)
        {
            if (dishShadow == null)
                return;
            dishShadow.enabled = dish != null && ingredientRenderer != null;
            if (!dishShadow.enabled)
                return;
            dishShadow.sprite = dish;
            dishShadow.color = new Color(0f, 0f, 0f, shadowOpacity);
            dishShadow.sortingLayerID = ingredientRenderer.sortingLayerID;
            dishShadow.sortingOrder = ingredientRenderer.sortingOrder - 1;
            float scale = Mathf.Max(0.0001f, dishScale);
            Transform shadow = dishShadow.transform;
            shadow.localPosition = new Vector3(shadowOffset.x / scale, shadowOffset.y / scale, 0.001f);
            shadow.localRotation = Quaternion.identity;
            shadow.localScale = Vector3.one;
        }

        // EN: Side of the square the dish picture is fitted into, in this slot's local units (before the slot's own scale); layouts scale the slot so this size fits the dish area.
        // VI: Cạnh ô vuông mà ảnh món được co vào, theo đơn vị local của ô (trước scale của ô); bố cục scale ô để kích thước này vừa vùng món.
        public float DishSize
        {
            get
            {
                CaptureBaseScales();
                return Mathf.Max(0.0001f, iconSlotSize * dishSizeRatio);
            }
        }

        // EN: Shows or hides the dish picture (with its shadow and done checkmark) while the card shows this dish's ingredients instead; the checkmark keeps its done state and reappears with the picture.
        // VI: Hiện hoặc ẩn ảnh món (kèm bóng và dấu tích hoàn thành) khi thẻ đang hiện nguyên liệu của món thay cho ảnh; dấu tích giữ trạng thái hoàn thành và hiện lại cùng ảnh món.
        public void SetDishVisible(bool visible, float duration = 0f)
        {
            if (dishFadeRoutine != null)
            {
                StopCoroutine(dishFadeRoutine);
                dishFadeRoutine = null;
                if (ingredientRenderer != null)
                    ingredientRenderer.transform.localScale = iconBaseScale;
            }
            if (duration <= 0f || !isActiveAndEnabled || ingredientRenderer == null)
            {
                ApplyDishVisible(visible);
                return;
            }
            StopPop();
            dishFadeRoutine = StartCoroutine(FadeDish(visible, duration));
        }

        // EN: Applies the final visibility of the dish picture, its shadow and its checkmark.
        // VI: Áp trạng thái hiện/ẩn cuối cùng của ảnh món, bóng và dấu tích.
        private void ApplyDishVisible(bool visible)
        {
            if (ingredientRenderer != null)
                ingredientRenderer.enabled = visible;
            if (dishShadow != null)
                dishShadow.enabled = visible && dishShadow.sprite != null;
            if (checkmark != null)
                checkmark.enabled = visible;
        }

        // EN: Shrinks the dish picture away (hide) or pops it back with a small overshoot (show); the shadow follows as a child of the picture, the checkmark switches at the hidden end.
        // VI: Thu nhỏ ảnh món cho biến mất (ẩn) hoặc nảy ảnh món trở lại có vượt nhẹ (hiện); bóng đi theo vì là con của ảnh, dấu tích bật/tắt ở phía trạng thái ẩn.
        private IEnumerator FadeDish(bool visible, float duration)
        {
            Transform icon = ingredientRenderer.transform;
            if (visible)
            {
                ingredientRenderer.enabled = true;
                if (dishShadow != null)
                    dishShadow.enabled = dishShadow.sprite != null;
                icon.localScale = Vector3.zero;
            }
            else
            {
                if (checkmark != null)
                    checkmark.enabled = false;
            }
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                float k = Mathf.Clamp01(time / duration);
                float s = visible ? OrderTweens.EaseOutBack(k) : 1f - OrderTweens.EaseOut(k);
                icon.localScale = iconBaseScale * s;
                yield return null;
            }
            icon.localScale = iconBaseScale;
            ApplyDishVisible(visible);
            dishFadeRoutine = null;
        }

        // EN: Gives the dish a small pop when one of its ingredients arrives.
        // VI: Cho món nảy nhẹ khi một nguyên liệu của món tới nơi.
        public void PlayIngredientArrived()
        {
            if (ingredientRenderer != null)
                PlayPop(ingredientRenderer.transform, iconBaseScale);
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

        // EN: Marks the slot complete and pops both the dish image and checkmark independently when the completed dish arrives.
        // VI: Đánh dấu ô hoàn thành và cho ảnh món cùng dấu tích nảy độc lập khi món hoàn chỉnh bay tới.
        public void MarkDone()
        {
            SetCountVisible(false);
            PlayIngredientArrived();
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

        // EN: Restarts only the requested icon or checkmark pop so both animations can play together.
        // VI: Chỉ chạy lại cú nảy của ảnh món hoặc dấu tích được yêu cầu để hai hiệu ứng có thể chạy cùng nhau.
        private void PlayPop(Transform target, Vector3 baseScale)
        {
            if (target == null || !isActiveAndEnabled)
                return;
            bool isCheckmark = checkmark != null && target == checkmark.transform;
            Coroutine running = isCheckmark ? checkPopRoutine : iconPopRoutine;
            if (running != null)
                StopCoroutine(running);
            target.localScale = baseScale;
            Coroutine routine = StartCoroutine(Pop(target, baseScale));
            if (isCheckmark)
                checkPopRoutine = routine;
            else
                iconPopRoutine = routine;
        }

        // EN: Grows one target then restores its resting scale and clears only its own animation handle.
        // VI: Phóng to một đối tượng rồi trả về scale gốc và chỉ xóa trạng thái hoạt cảnh của chính nó.
        private IEnumerator Pop(Transform target, Vector3 baseScale)
        {
            float time = 0f;
            while (time < Config.orderPop.popDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / Config.orderPop.popDuration);
                target.localScale = baseScale * (1f + Config.orderPop.popAmount * Mathf.Sin(t * Mathf.PI) * (1f - 0.5f * t));
                yield return null;
            }
            target.localScale = baseScale;
            if (checkmark != null && target == checkmark.transform)
                checkPopRoutine = null;
            else
                iconPopRoutine = null;
        }

        // EN: Stops both independent pop animations and restores icon and checkmark scales for reuse or disabling.
        // VI: Dừng cả hai hiệu ứng nảy độc lập và trả scale ảnh món cùng dấu tích về gốc khi tái sử dụng hoặc tắt ô.
        private void StopPop()
        {
            if (iconPopRoutine != null)
                StopCoroutine(iconPopRoutine);
            if (checkPopRoutine != null)
                StopCoroutine(checkPopRoutine);
            iconPopRoutine = null;
            checkPopRoutine = null;
            if (ingredientRenderer != null)
                ingredientRenderer.transform.localScale = iconBaseScale;
            if (checkmark != null)
                checkmark.transform.localScale = checkBaseScale;
        }

        // EN: Restores the resting look when the line is disabled mid-animation.
        // VI: Khôi phục hình gốc khi dòng bị tắt giữa lúc đang chạy hiệu ứng.
        private void OnDisable()
        {
            if (dishFadeRoutine != null)
            {
                StopCoroutine(dishFadeRoutine);
                dishFadeRoutine = null;
                if (ingredientRenderer != null)
                    ingredientRenderer.transform.localScale = iconBaseScale;
            }
            StopPop();
        }
    }
}
