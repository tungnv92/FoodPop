using System.Collections;
using System.Collections.Generic;
using GamePackages;
using Lofelt.NiceVibrations;
using UnityEngine;

namespace FoodPop
{
    // EN: One slot of a customer's order card: the ordered final dish picture (popping as its ingredients arrive) and a checkmark once the dish is done.
    // VI: Một ô trên thẻ order của khách: ảnh món hoàn chỉnh được order (nảy nhẹ khi nguyên liệu tới) và dấu tích khi món xong.
    [DisallowMultipleComponent]
    public class OrderItemView : MonoBehaviour
    {
        // EN: World position where served food lands (the icon), falling back to this line's position.
        // VI: Vị trí world mà món được phục vụ đáp xuống (icon), nếu không có thì lấy vị trí dòng này.
        public Vector3 TargetPosition => ingredientRenderer != null ? ingredientRenderer.transform.position : transform.position;

        // EN: World scale of the icon, used as the size a flying food shrinks to on arrival.
        // VI: Kích thước world của icon, dùng làm kích thước món bay thu nhỏ về khi tới nơi.
        public Vector3 TargetScale => ingredientRenderer != null ? ingredientRenderer.transform.lossyScale : transform.lossyScale;

        // EN: Sorting of the icon so flying food can be drawn above the order card.
        // VI: Thứ tự vẽ của icon để món bay được vẽ trên thẻ order.
        public SpriteRenderer IngredientRenderer => ingredientRenderer;

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

        [Tooltip("Renderer hiện icon món được order; món bay tới sẽ đáp vào đây.")]
        [SerializeField] private SpriteRenderer ingredientRenderer;
        [Tooltip("Renderer dấu tích hiện khi món đã nhận đủ.")]
        [SerializeField] private SpriteRenderer checkmark;

        [Tooltip("Tỉ lệ ảnh món so với ô icon gốc (1 = to bằng icon nguyên liệu).")]
        [Min(0.1f)] [SerializeField] private float dishSizeRatio = 1.15f;

        private Vector3 iconBaseScale = Vector3.one;
        private Vector3 iconDefaultScale = Vector3.one;
        private float iconSlotSize = 1f;
        private bool scalesCaptured;
        private Vector3 checkBaseScale = Vector3.one;
        private Coroutine iconPopRoutine;
        private Coroutine checkPopRoutine;
        private Coroutine dishFadeRoutine;
        private bool donePopPending;
        private readonly Dictionary<FinalFoodView, FinalFoodView> dishInstances = new Dictionary<FinalFoodView, FinalFoodView>();
        private FinalFoodView dishInstance;

        // EN: Captures the resting scales as soon as the slot wakes up.
        // VI: Ghi nhớ scale gốc ngay khi ô được khởi tạo.
        private void Awake()
        {
            CaptureBaseScales();
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

        // EN: Shows an ordered final dish: an instance of its picture prefab (reused per prefab) fitted to the slot and drawn at the icon's sorting, without count or checkmark; no picture when the group has no prefab.
        // VI: Hiện một món hoàn chỉnh được order: instance prefab hình món (tái dùng theo prefab) co vừa ô và vẽ theo thứ tự vẽ của icon, không có số lượng hay dấu tích; không có hình khi nhóm chưa có prefab.
        public void ShowDish(FinalFoodView dish)
        {
            CaptureBaseScales();
            StopPop();
            donePopPending = false;
            gameObject.SetActive(true);
            if (ingredientRenderer != null)
            {
                ingredientRenderer.enabled = true;
                ingredientRenderer.sprite = null;
                float size = dish != null ? dish.GetLocalSize() : 0f;
                float scale = size > 0f ? iconSlotSize * dishSizeRatio / size : iconDefaultScale.x;
                iconBaseScale = new Vector3(scale, scale, scale);
                ingredientRenderer.transform.localScale = iconBaseScale;
                ingredientRenderer.color = Color.white;
                SetDishInstance(dish);
            }
            if (checkmark != null)
            {
                checkmark.enabled = true;
                checkmark.gameObject.SetActive(false);
            }
        }

        // EN: Shows or hides the dish picture (with its done checkmark) while the card shows this dish's ingredients instead; the checkmark keeps its done state and reappears with the picture.
        // VI: Hiện hoặc ẩn ảnh món (kèm dấu tích hoàn thành) khi thẻ đang hiện nguyên liệu của món thay cho ảnh; dấu tích giữ trạng thái hoàn thành và hiện lại cùng ảnh món.
        public void SetDishVisible(bool visible, float duration = 0f)
        {
            CaptureBaseScales();
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
                if (donePopPending && visible)
                    PlayDonePop();
                return;
            }
            StopPop();
            dishFadeRoutine = StartCoroutine(FadeDish(visible, duration));
        }

        // EN: Plays a light emphasis vibration when the player's vibration setting is on (no-op before the profile is loaded); called when the player taps this dish.
        // VI: Phát rung nhấn nhẹ khi người chơi bật rung (không làm gì nếu hồ sơ người chơi chưa được nạp); gọi khi người chơi chạm vào món này.
        public void PlayHaptic()
        {
            if (ProfileManager.instance != null && ProfileManager.instance.PlayerDataManager != null &&
                ProfileManager.instance.PlayerDataManager.isVibrateOn)
                HapticPatterns.PlayEmphasis(0.8f, 0.5f);
        }

        // EN: Gives the dish a small pop when one of its ingredients arrives.
        // VI: Cho món nảy nhẹ khi một nguyên liệu của món tới nơi.
        public void PlayIngredientArrived()
        {
            if (ingredientRenderer != null)
                PlayPop(ingredientRenderer.transform, iconBaseScale);
        }

        // EN: Marks the slot complete and pops both the dish image and checkmark independently when the completed dish arrives; while the dish picture is still popping back (its ingredient view just closed), both pops wait until that ends.
        // VI: Đánh dấu ô hoàn thành và cho ảnh món cùng dấu tích nảy độc lập khi món hoàn chỉnh bay tới; nếu ảnh món đang nảy hiện lại (vừa đóng layout nguyên liệu) thì cả hai cú nảy chờ tới khi xong.
        public void MarkDone()
        {
            if (checkmark != null)
                checkmark.gameObject.SetActive(true);
            if (dishFadeRoutine != null)
            {
                donePopPending = true;
                return;
            }
            PlayDonePop();
        }

        // EN: Hides the whole line (used when the order has fewer foods than the card has lines).
        // VI: Ẩn cả dòng (dùng khi order có ít món hơn số dòng của thẻ).
        public void Hide()
        {
            StopPop();
            gameObject.SetActive(false);
        }

        // EN: Pops the dish image and the checkmark for a completed dish.
        // VI: Cho ảnh món và dấu tích nảy khi món hoàn thành.
        private void PlayDonePop()
        {
            donePopPending = false;
            PlayIngredientArrived();
            if (checkmark != null)
                PlayPop(checkmark.transform, checkBaseScale);
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

        // EN: Stops both independent pop animations and restores icon and checkmark scales for reuse or disabling. Captures the resting scales first: a slot that never woke up (inactive since load, then hidden by an order with fewer dishes) would otherwise be reset to the default scale 1.
        // VI: Dừng cả hai hiệu ứng nảy độc lập và trả scale ảnh món cùng dấu tích về gốc khi tái sử dụng hoặc tắt ô. Ghi nhớ scale gốc trước: ô chưa từng Awake (tắt từ lúc nạp, rồi bị ẩn vì order ít món hơn) nếu không sẽ bị đặt về scale mặc định 1.
        private void StopPop()
        {
            CaptureBaseScales();
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

        // EN: Activates the card instance of the given dish prefab under the icon (creating it once per prefab) and hides the previous one; null hides any dish picture.
        // VI: Bật instance trên thẻ của prefab món cho trước dưới icon (mỗi prefab chỉ tạo một lần) và ẩn instance trước đó; null thì ẩn mọi hình món.
        private void SetDishInstance(FinalFoodView prefab)
        {
            if (dishInstance != null)
                dishInstance.gameObject.SetActive(false);
            dishInstance = null;
            if (prefab == null || ingredientRenderer == null)
                return;
            if (!dishInstances.TryGetValue(prefab, out dishInstance))
            {
                dishInstance = Instantiate(prefab, ingredientRenderer.transform, false);
                dishInstances.Add(prefab, dishInstance);
            }
            Transform dish = dishInstance.transform;
            dish.localPosition = Vector3.zero;
            dish.localRotation = Quaternion.identity;
            dish.localScale = Vector3.one;
            dishInstance.SetSorting(ingredientRenderer.sortingLayerID, ingredientRenderer.sortingOrder);
            dishInstance.gameObject.SetActive(true);
        }

        // EN: Applies the final visibility of the dish picture (sprite or prefab instance), its shadow and its checkmark.
        // VI: Áp trạng thái hiện/ẩn cuối cùng của ảnh món (sprite hoặc instance prefab), bóng và dấu tích.
        private void ApplyDishVisible(bool visible)
        {
            if (ingredientRenderer != null)
                ingredientRenderer.enabled = visible;
            if (dishInstance != null)
                dishInstance.gameObject.SetActive(visible);
            if (checkmark != null)
                checkmark.enabled = visible;
        }

        // EN: Shrinks the dish picture away (hide) or pops it back with a small overshoot (show); the shadow and the dish prefab instance follow as children of the icon, the checkmark switches at the hidden end.
        // VI: Thu nhỏ ảnh món cho biến mất (ẩn) hoặc nảy ảnh món trở lại có vượt nhẹ (hiện); bóng và instance prefab món đi theo vì là con của icon, dấu tích bật/tắt ở phía trạng thái ẩn.
        private IEnumerator FadeDish(bool visible, float duration)
        {
            Transform icon = ingredientRenderer.transform;
            if (visible)
            {
                ingredientRenderer.enabled = true;
                if (dishInstance != null)
                    dishInstance.gameObject.SetActive(true);
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
            if (donePopPending && visible)
                PlayDonePop();
        }

        // EN: Restarts only the requested icon or checkmark pop so both animations can play together.
        // VI: Chỉ chạy lại cú nảy của ảnh món hoặc dấu tích được yêu cầu để hai hiệu ứng có thể chạy cùng nhau.
        private void PlayPop(Transform target, Vector3 baseScale)
        {
            if (target == null || !isActiveAndEnabled)
                return;
            CaptureBaseScales();
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
            var pop = IngameConfig.Instance.orderPop;
            float time = 0f;
            while (time < pop.popDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / pop.popDuration);
                target.localScale = baseScale * (1f + pop.popAmount * Mathf.Sin(t * Mathf.PI) * (1f - 0.5f * t));
                yield return null;
            }
            target.localScale = baseScale;
            if (checkmark != null && target == checkmark.transform)
                checkPopRoutine = null;
            else
                iconPopRoutine = null;
        }
    }
}
