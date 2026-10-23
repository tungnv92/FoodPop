using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FoodPop
{
    // EN: Order card of one customer. Holds the dish slots and each dish's recipe progress; the display of each dish count (initial layout and the tap-to-show-ingredients view) is delegated to an OrderLayout component on the same object (OrderLayout1Dish, OrderLayout2Dish, ...). Counts without a layout component are laid out as one row of columns by the layout handling the most dishes.
    // VI: Thẻ order của một khách. Giữ các ô món và tiến độ nguyên liệu từng món; hiển thị theo từng số món (bố cục ban đầu và giao diện chạm để xem nguyên liệu) giao cho component OrderLayout trên cùng object (OrderLayout1Dish, OrderLayout2Dish, ...). Số món chưa có component bố cục thì xếp thành một hàng cột bằng component xử lý nhiều món nhất.
    [DisallowMultipleComponent]
    public class OrderView : MonoBehaviour
    {
        [Tooltip("Các ô món trên thẻ (mỗi ô một món hoàn chỉnh), theo thứ tự hiển thị.")]
        [SerializeField] private OrderItemView[] items = new OrderItemView[0];
        [Tooltip("Bộ hiển thị nền thẻ order, chỉ đổi chiều rộng theo bố cục và giữ nguyên chiều cao.")]
        [SerializeField] private SpriteRenderer background;
        [Tooltip("Khoảng đệm nền; chỉ dùng thành phần ngang để chỉnh chiều rộng, giữ nguyên chiều cao nền.")]
        [SerializeField] private Vector2 backgroundPadding = new Vector2(0.32f, 0.3f);

        [Header("Cột món (dùng chung cho mọi bố cục)")]
        [Tooltip("Kích thước một cột món (áp dụng cho mọi cột), đơn vị WORLD: x = rộng, y = cao. Nền thẻ rộng bằng tổng các cột (+ khoảng cách + lề).")]
        [SerializeField] private Vector2 columnSize = new Vector2(0.8f, 1.8f);
        [Tooltip("Khoảng cách giữa hai cột món (đơn vị world), chỉ dùng khi có từ 2 cột.")]
        [Min(0f)] [SerializeField] private float columnSpacing = 0.15f;
        [Tooltip("Tỉ lệ vùng món so với cột (1 = khít cột). Vùng chạm (BoxCollider2D) và ảnh món đều khít vùng này.")]
        [Range(0.1f, 1f)] [SerializeField] private float dishFill = 1f;
        [Tooltip("Lệch của hàng món so với tâm thẻ (đơn vị world).")]
        [SerializeField] private Vector2 columnsOffset = Vector2.zero;

        [Header("Hiệu ứng chuyển")]
        [Tooltip("Bật hiệu ứng chuyển (mở/đóng layout nguyên liệu, ẩn/hiện món khi collapse). Tắt = đổi tức thì. Hiệu ứng mở/đóng nguyên liệu chỉnh trên từng prefab layout (IngredientPreviewView).")]
        [SerializeField] private bool animateTransitions = true;
        [Tooltip("Collapse: món khác bị ẩn khi layout nguyên liệu che quá tỉ lệ này chiều rộng cột của nó (0.25 = che hơn 1/4 cột).")]
        [Range(0f, 1f)] [SerializeField] private float coverThreshold = 0.25f;
        [Tooltip("Thời gian ẩn/hiện các món khác khi collapse (giây).")]
        [Min(0f)] [SerializeField] private float dishHideDuration = 0.16f;

        [Header("Ẩn/hiện thẻ theo khách")]
        [Tooltip("Thời gian thẻ thu nhỏ biến mất khi khách bắt đầu rời quầy (giây).")]
        [Min(0f)] [SerializeField] private float cardHideDuration = 0.2f;
        [Tooltip("Thời gian thẻ nảy hiện ra khi khách tới quầy (giây).")]
        [Min(0f)] [SerializeField] private float cardShowDuration = 0.28f;

        [Header("Chạm để xem nguyên liệu")]
        [Tooltip("Bật: chạm vào món để ẩn ảnh món và hiện nguyên liệu raw; chạm lại để thu về.")]
        [SerializeField] private bool tapToShowIngredients = true;
        [Tooltip("Icon nguyên liệu (dùng ảnh Raw).")]
        [SerializeField] private FoodIconConfig foodIconConfig;
        [Tooltip("Camera đổi vị trí chạm trên màn hình sang world; để trống dùng Camera.main. Vùng chạm là BoxCollider2D trên từng ô món.")]
        [SerializeField] private Camera inputCamera;

        [Header("Xem trước (Editor)")]
        [Tooltip("Số món dùng để xem trước bố cục trong Edit Mode (không ảnh hưởng khi chạy game).")]
        [Range(0, CustomerOrder.MaxDishes)] [SerializeField] private int previewDishCount = 1;

        private readonly List<List<FoodType>> recipes = new List<List<FoodType>>();
        private readonly List<List<bool>> arrived = new List<List<bool>>();
        private readonly List<Sprite> iconBuffer = new List<Sprite>();
        private bool[] expanded = new bool[0];
        private Coroutine[] collapseRoutines = new Coroutine[0];
        private int[] suspended = new int[0];
        private Vector3 cardRestScale = Vector3.one;
        private bool cardScaleCaptured;
        private bool cardHidden;
        private OrderLayout[] displayLayouts = new OrderLayout[0];
        private OrderLayout activeLayout;
        private OrderTweens tweens;
        private readonly Dictionary<Transform, Vector3> restScales = new Dictionary<Transform, Vector3>();

        // EN: Tween runner for the dish show/hide animations.
        // VI: Bộ chạy tween cho hoạt cảnh ẩn/hiện món.
        private OrderTweens Tweens => tweens ?? (tweens = new OrderTweens(this));
        private int activeCount;

        // EN: The dish slots of the card, in display order.
        // VI: Các ô món của thẻ, theo thứ tự hiển thị.
        public IReadOnlyList<OrderItemView> Items => items;

        // EN: Number of dish slots on the card.
        // VI: Số ô món trên thẻ.
        public int Capacity => items.Length;

        // EN: Shared values read by layout components.
        // VI: Các giá trị dùng chung cho component bố cục.
        public SpriteRenderer Background => background;
        public Vector2 Padding => backgroundPadding;

        // EN: Number of dishes on the current order and the position of a dish slot among them (-1 when not active).
        // VI: Số món của order hiện tại và vị trí của một ô món trong đó (-1 nếu không dùng).
        public int ActiveDishCount => activeCount;
        public int IndexOfDish(OrderItemView item)
        {
            for (int i = 0; i < activeCount && i < items.Length; i++)
                if (items[i] == item)
                    return i;
            return -1;
        }

        // EN: True when the layout is the one arranging the whole card (false for the 1-dish layout drawing a collapsed dish of a bigger card).
        // VI: True khi bố cục là bố cục đang xếp cả thẻ (false với bố cục 1 món đang vẽ món đã thu của thẻ nhiều món).
        public bool IsCardLayout(OrderLayout layout) => layout == activeLayout;

        // EN: World scale of the card at its normal size (ignores the hide/show animation of the root), used by layouts so slots and colliders are never computed from a shrunken card.
        // VI: Scale world của thẻ ở kích thước gốc (bỏ qua hoạt cảnh ẩn/hiện của root), bố cục dùng để không tính ô món và collider từ thẻ đang bị thu nhỏ.
        public Vector3 RestLossyScale
        {
            get
            {
                CaptureCardScale();
                Vector3 parent = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
                return Vector3.Scale(parent, cardRestScale);
            }
        }
        public Vector2 ColumnSize => columnSize;
        public float ColumnSpacing => columnSpacing;
        public float DishFill => dishFill;
        public Vector2 ColumnsOffset => columnsOffset;
        public bool AnimateTransitions => animateTransitions;
        public float DishHideDuration => animateTransitions ? dishHideDuration : 0f;

        // EN: Adds missing layout components and tap colliders at runtime.
        // VI: Thêm component bố cục và collider vùng chạm còn thiếu khi chạy game.
        private void Awake()
        {
            EnsureComponents();
            CaptureCardScale();
        }

        // EN: Shrinks the card root away (only the root's scale changes, children keep theirs) and returns the duration so the caller can wait before the customer leaves; taps are ignored until it shows again.
        // VI: Thu nhỏ root của thẻ cho biến mất (chỉ đổi scale của root, các con giữ nguyên) và trả về thời gian để bên gọi chờ xong rồi khách mới rời đi; bỏ qua chạm cho tới khi thẻ hiện lại.
        public float PlayHide()
        {
            CaptureCardScale();
            cardHidden = true;
            Transform card = transform;
            Vector3 from = card.localScale;
            Tweens.Run(card, cardHideDuration, t => card.localScale = Vector3.Lerp(from, Vector3.zero, t * t),
                () => card.localScale = Vector3.zero);
            return isActiveAndEnabled ? cardHideDuration : 0f;
        }

        // EN: Pops the card back from zero to its normal scale (customer arrives at the counter).
        // VI: Cho thẻ nảy từ 0 về scale gốc (khách tới quầy).
        public void PlayShow()
        {
            CaptureCardScale();
            cardHidden = false;
            Transform card = transform;
            card.localScale = Vector3.zero;
            Vector3 rest = cardRestScale;
            Tweens.Run(card, cardShowDuration, t => card.localScale = Vector3.LerpUnclamped(Vector3.zero, rest, OrderTweens.EaseOutBack(t)),
                () => card.localScale = rest);
        }

        // EN: Remembers the card's normal scale once (before any hide/show animation).
        // VI: Ghi nhớ scale gốc của thẻ một lần (trước mọi hoạt cảnh ẩn/hiện).
        private void CaptureCardScale()
        {
            if (cardScaleCaptured)
                return;
            cardRestScale = transform.localScale;
            cardScaleCaptured = true;
        }

        // EN: Stores each dish's recipe (in recipe order, base first) and clears arrival progress; call before Layout when an order starts.
        // VI: Lưu công thức từng món (theo thứ tự công thức, đế trước) và xoá tiến độ; gọi trước Layout khi bắt đầu order.
        public void SetRecipes(IReadOnlyList<IReadOnlyList<FoodType>> dishRecipes)
        {
            recipes.Clear();
            arrived.Clear();
            if (dishRecipes == null)
                return;
            foreach (IReadOnlyList<FoodType> recipe in dishRecipes)
            {
                List<FoodType> list = new List<FoodType>();
                if (recipe != null)
                    foreach (FoodType type in recipe)
                        if (type != FoodType.None)
                            list.Add(type);
                recipes.Add(list);
                arrived.Add(new List<bool>(new bool[list.Count]));
            }
        }

        // EN: Records one ingredient landing on a dish (first matching not-yet-arrived entry) and refreshes its ingredient view if open.
        // VI: Ghi nhận một nguyên liệu đáp xuống món (mục cùng loại đầu tiên chưa tới) và cập nhật lưới nguyên liệu nếu đang mở.
        public void MarkIngredientArrived(int dish, FoodType type)
        {
            if (dish < 0 || dish >= recipes.Count)
                return;
            List<FoodType> recipe = recipes[dish];
            for (int i = 0; i < recipe.Count; i++)
            {
                if (recipe[i] == type && !arrived[dish][i])
                {
                    arrived[dish][i] = true;
                    break;
                }
            }
            if (IsExpanded(dish))
                ShowIngredients(dish);
        }

        // EN: Shows the requested slots through the layout component for this dish count (collapsing any open ingredient view), or the legacy grid when none exists.
        // VI: Hiện các ô được yêu cầu qua component bố cục ứng với số món (thu mọi lưới nguyên liệu đang mở), hoặc lưới cũ khi chưa có.
        public void Layout(int count)
        {
            count = Mathf.Clamp(count, 0, items.Length);
            CollapseAll();
            StopDishAnimations();
            activeCount = count;
            expanded = new bool[items.Length];
            collapseRoutines = new Coroutine[items.Length];
            suspended = new int[items.Length];
            displayLayouts = new OrderLayout[items.Length];
            for (int i = 0; i < items.Length; i++)
                if (items[i] != null)
                    items[i].gameObject.SetActive(i < count);
            if (count == 0)
            {
                activeLayout = null;
                if (background != null)
                    background.enabled = false;
                return;
            }
            activeLayout = FindLayout(count);
            List<OrderItemView> active = new List<OrderItemView>(count);
            for (int i = 0; i < count; i++)
                active.Add(items[i]);
            if (activeLayout != null)
            {
                activeLayout.Arrange(this, active);
                return;
            }
            OrderLayout fallback = FindFallbackLayout();
            if (fallback != null)
            {
                activeLayout = fallback;
                fallback.ArrangeFallback(this, active);
            }
            else
                Debug.LogWarning($"[OrderView] {name} chưa có component bố cục (OrderLayout…); chạy Tools → HotPot → Setup Order Views.", this);
        }

        // EN: True when the dish currently shows its ingredients instead of its picture.
        // VI: True khi món đang hiện nguyên liệu thay cho ảnh món.
        public bool IsExpanded(int dish) => dish >= 0 && dish < expanded.Length && expanded[dish];

        // EN: Opens or closes the ingredient view of one dish (several dishes may be open at once).
        // VI: Mở hoặc đóng lưới nguyên liệu của một món (có thể mở nhiều món cùng lúc).
        public void SetExpanded(int dish, bool value)
        {
            if (activeLayout == null || dish < 0 || dish >= activeCount || items[dish] == null || dish >= recipes.Count)
                return;
            StopAutoCollapse(dish);
            bool focus = activeLayout.IsCollapseLayout(recipes[dish].Count);
            if (value)
            {
                expanded[dish] = true;
                ShowIngredients(dish);
                if (focus)
                    HideCoveredDishes(dish);
                float delay = activeLayout.GetAutoCollapseDelay(recipes[dish].Count);
                if (focus && delay >= 0f && isActiveAndEnabled)
                    collapseRoutines[dish] = StartCoroutine(CollapseToSingle(dish, delay));
            }
            else
            {
                DisplayLayout(dish).HideIngredients(items[dish]);
                expanded[dish] = false;
                displayLayouts[dish] = null;
                if (focus)
                    SetOtherDishesVisible(dish, true);
            }
        }

        // EN: Layout currently drawing a dish: the 1-dish layout once that dish has collapsed, otherwise the card's layout (several dishes may be collapsed at once).
        // VI: Bố cục đang vẽ một món: bố cục 1 món khi món đó đã thu về, ngược lại là bố cục của thẻ (có thể nhiều món cùng ở trạng thái đã thu).
        private OrderLayout DisplayLayout(int dish) =>
            dish >= 0 && dish < displayLayouts.Length && displayLayouts[dish] != null ? displayLayouts[dish] : activeLayout;

        // EN: After the form's collapseTime, shows the other dishes again and keeps showing the selected dish's ingredients in its own column, drawn with OrderLayout1Dish's ingredient forms; tapping it again closes it.
        // VI: Sau collapseTime của mẫu, hiện lại các món khác và tiếp tục hiện nguyên liệu của món đang chọn trong cột của nó, vẽ theo mẫu nguyên liệu của OrderLayout1Dish; chạm lại để đóng.
        private IEnumerator CollapseToSingle(int dish, float delay)
        {
            yield return new WaitForSeconds(delay);
            collapseRoutines[dish] = null;
            if (!IsExpanded(dish))
                yield break;
            OrderLayout single = FindLayout(1);
            if (single == null || single == activeLayout)
                yield break;
            activeLayout.HideIngredients(items[dish], false, false);
            displayLayouts[dish] = single;
            ShowIngredients(dish);
            SetOtherDishesVisible(dish, true);
        }

        // EN: Hides (closing their ingredient views) or shows again every active dish except one (optionally only those matching a filter); used while a collapseLayout dish shows its ingredients. A dish that was open when hidden is restored when shown again: an open non-collapse dish reopens in place, an already-collapsed dish returns to its collapsed ingredient view (1-dish forms) without hiding the others; a collapse dish still in its first phase is not restored.
        // VI: Ẩn (đóng luôn lưới nguyên liệu của chúng) hoặc hiện lại mọi món đang dùng trừ một món; dùng khi món có collapseLayout đang hiện nguyên liệu. Món đang mở lúc bị ẩn được khôi phục khi hiện lại: món không collapse mở lại tại chỗ, món đã thu về hiện lại lưới nguyên liệu theo mẫu 1 món mà không ẩn các món khác; món collapse còn ở giai đoạn đầu thì không khôi phục.
        // EN: Hides only the dishes whose column the open collapse layout of a dish covers by more than coverThreshold of its width (all others stay visible and tappable).
        // VI: Chỉ ẩn những món có cột bị layout collapse đang mở che quá coverThreshold chiều rộng (các món khác vẫn hiện và chạm được).
        private void HideCoveredDishes(int dish)
        {
            if (!activeLayout.TryGetOpenArea(items[dish], out Rect area))
                return;
            Vector3 lossy = RestLossyScale;
            Vector2 half = new Vector2(columnSize.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)), columnSize.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y))) * 0.5f;
            SetOtherDishesVisible(dish, false, i =>
            {
                Vector2 centre = items[i].transform.localPosition;
                Rect column = new Rect(centre - half, half * 2f);
                float overlapX = Mathf.Min(column.xMax, area.xMax) - Mathf.Max(column.xMin, area.xMin);
                float overlapY = Mathf.Min(column.yMax, area.yMax) - Mathf.Max(column.yMin, area.yMin);
                return overlapY > 0f && overlapX > column.width * coverThreshold;
            });
        }

        private void SetOtherDishesVisible(int keep, bool visible, System.Func<int, bool> filter = null)
        {
            for (int i = 0; i < activeCount; i++)
            {
                if (i == keep || items[i] == null)
                    continue;
                if (filter != null && !filter(i))
                    continue;
                if (!visible && IsExpanded(i) && i < suspended.Length)
                {
                    bool collapsed = displayLayouts[i] != null;
                    bool collapseForm = i < recipes.Count && activeLayout.IsCollapseLayout(recipes[i].Count);
                    suspended[i] = collapsed ? SuspendedCollapsed : collapseForm ? SuspendedNone : SuspendedOpen;
                    expanded[i] = false;
                    StopAutoCollapse(i);
                    DisplayLayout(i).HideIngredients(items[i], true);
                    displayLayouts[i] = null;
                }
                AnimateDishVisible(items[i], visible);
                if (visible && i < suspended.Length && suspended[i] != SuspendedNone)
                {
                    int mode = suspended[i];
                    suspended[i] = SuspendedNone;
                    if (mode == SuspendedCollapsed)
                    {
                        OrderLayout single = FindLayout(1);
                        if (single != null && single != activeLayout)
                        {
                            expanded[i] = true;
                            displayLayouts[i] = single;
                            ShowIngredients(i);
                            continue;
                        }
                    }
                    else
                        SetExpanded(i, true);
                }
            }
        }

        private const int SuspendedNone = 0;
        private const int SuspendedOpen = 1;
        private const int SuspendedCollapsed = 2;

        // EN: Shrinks a dish slot away and deactivates it, or reactivates it and pops it back to its laid-out scale; its tap collider is off while hidden.
        // VI: Thu nhỏ ô món rồi tắt, hoặc bật lại và nảy về scale theo bố cục; collider vùng chạm tắt khi đang ẩn.
        private void AnimateDishVisible(OrderItemView item, bool visible)
        {
            Transform slot = item.transform;
            BoxCollider2D tap = item.GetComponent<BoxCollider2D>();
            if (!restScales.TryGetValue(slot, out Vector3 rest) || !Tweens.IsRunning(slot))
                rest = slot.localScale;
            if (rest == Vector3.zero && restScales.TryGetValue(slot, out Vector3 stored))
                rest = stored;
            restScales[slot] = rest;
            if (visible)
            {
                if (!item.gameObject.activeSelf)
                {
                    slot.localScale = Vector3.zero;
                    item.gameObject.SetActive(true);
                }
                if (tap != null)
                    tap.enabled = true;
                Vector3 from = slot.localScale;
                Tweens.Run(slot, DishHideDuration, t => slot.localScale = Vector3.LerpUnclamped(from, rest, OrderTweens.EaseOutBack(t)),
                    () => slot.localScale = rest);
            }
            else
            {
                if (tap != null)
                    tap.enabled = false;
                if (!item.gameObject.activeSelf)
                    return;
                Vector3 from = slot.localScale;
                Tweens.Run(slot, DishHideDuration, t => slot.localScale = Vector3.Lerp(from, Vector3.zero, OrderTweens.EaseOut(t)),
                    () =>
                    {
                        item.gameObject.SetActive(false);
                        slot.localScale = rest;
                    });
            }
        }

        // EN: Cancels every dish show/hide animation, restoring the laid-out scales and tap colliders (used before a new layout).
        // VI: Huỷ mọi hoạt cảnh ẩn/hiện món, trả scale theo bố cục và collider vùng chạm (dùng trước khi xếp bố cục mới).
        private void StopDishAnimations()
        {
            foreach (OrderItemView item in items)
            {
                if (item == null)
                    continue;
                Transform slot = item.transform;
                if (Tweens.IsRunning(slot) && restScales.TryGetValue(slot, out Vector3 rest))
                    slot.localScale = rest;
                Tweens.Stop(slot);
                BoxCollider2D tap = item.GetComponent<BoxCollider2D>();
                if (tap != null)
                    tap.enabled = true;
            }
            restScales.Clear();
        }

        // EN: Re-applies the current layout (keeping hidden dishes hidden) and redraws open ingredient views; used when card settings change in Play Mode.
        // VI: Áp lại bố cục hiện tại (món đang ẩn vẫn ẩn) và vẽ lại lưới nguyên liệu đang mở; dùng khi đổi thông số thẻ lúc đang Play.
        public void RefreshArrangement()
        {
            if (activeLayout == null)
                return;
            List<OrderItemView> active = new List<OrderItemView>(activeCount);
            for (int i = 0; i < activeCount; i++)
                active.Add(items[i]);
            activeLayout.Arrange(this, active);
            RefreshExpanded();
        }

        // EN: Redraws every open ingredient view (used when layout settings change in Play Mode).
        // VI: Vẽ lại mọi lưới nguyên liệu đang mở (dùng khi đổi thông số bố cục lúc đang Play).
        public void RefreshExpanded()
        {
            if (activeLayout == null)
                return;
            for (int i = 0; i < activeCount && i < expanded.Length; i++)
                if (expanded[i] && i < recipes.Count && items[i] != null)
                    ShowIngredients(i);
        }

        // EN: Cancels a pending auto-collapse of one dish.
        // VI: Huỷ lần tự thu đang chờ của một món.
        private void StopAutoCollapse(int dish)
        {
            if (dish < 0 || dish >= collapseRoutines.Length || collapseRoutines[dish] == null)
                return;
            StopCoroutine(collapseRoutines[dish]);
            collapseRoutines[dish] = null;
        }

        // EN: Reads taps on the card and toggles the tapped dish.
        // VI: Đọc chạm lên thẻ và bật/tắt lưới nguyên liệu của món được chạm.
        private void Update()
        {
            if (!tapToShowIngredients || activeLayout == null || cardHidden)
                return;
            Pointer pointer = Pointer.current;
            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (pointer == null || cam == null || !pointer.press.wasPressedThisFrame)
                return;
            Vector2 screen = pointer.position.ReadValue();
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(cam.transform.position.z - transform.position.z)));
            if (TryGetDishAt(world, out int dish))
                SetExpanded(dish, !IsExpanded(dish));
        }

        // EN: Finds the active dish slot whose BoxCollider2D contains a world point.
        // VI: Tìm ô món đang dùng có BoxCollider2D chứa điểm world.
        public bool TryGetDishAt(Vector3 world, out int dish)
        {
            Physics2D.SyncTransforms();
            for (dish = 0; dish < activeCount; dish++)
            {
                OrderItemView item = items[dish];
                if (item == null || !item.gameObject.activeInHierarchy)
                    continue;
                BoxCollider2D area = item.GetComponent<BoxCollider2D>();
                if (area != null && area.enabled && area.OverlapPoint(world))
                    return true;
            }
            dish = -1;
            return false;
        }

        // EN: Adds what the card needs when missing: the 1-, 2- and 3-dish layout components and a trigger BoxCollider2D on every dish slot as its tap area (sized by the layout). Safe to call repeatedly.
        // VI: Thêm phần còn thiếu của thẻ: component bố cục 1, 2 và 3 món, và BoxCollider2D (trigger, size do bố cục đặt) trên mỗi ô món làm vùng chạm. Gọi nhiều lần không sao.
        public void EnsureComponents()
        {
            if (GetComponent<OrderLayout1Dish>() == null)
                gameObject.AddComponent<OrderLayout1Dish>();
            if (GetComponent<OrderLayout2Dish>() == null)
                gameObject.AddComponent<OrderLayout2Dish>();
            if (GetComponent<OrderLayout3Dish>() == null)
                gameObject.AddComponent<OrderLayout3Dish>();
            foreach (OrderItemView item in items)
            {
                if (item == null || item.GetComponent<BoxCollider2D>() != null)
                    continue;
                BoxCollider2D area = item.gameObject.AddComponent<BoxCollider2D>();
                area.isTrigger = true;
                area.size = Vector2.one;
                area.offset = Vector2.zero;
            }
        }

        // EN: Builds the raw icon list for a dish and hands it with its progress to the active layout.
        // VI: Lấy danh sách icon raw của món và giao cùng tiến độ cho bố cục đang dùng.
        private void ShowIngredients(int dish)
        {
            iconBuffer.Clear();
            foreach (FoodType type in recipes[dish])
                iconBuffer.Add(foodIconConfig != null ? foodIconConfig.GetIcon(type, FoodState.Raw) : null);
            DisplayLayout(dish).ShowIngredients(items[dish], iconBuffer, arrived[dish]);
        }

        // EN: Closes every open ingredient view.
        // VI: Đóng mọi lưới nguyên liệu đang mở.
        private void CollapseAll()
        {
            for (int i = 0; i < collapseRoutines.Length; i++)
                StopAutoCollapse(i);
            for (int i = 0; i < expanded.Length; i++)
                if (expanded[i] && activeLayout != null && i < items.Length && items[i] != null)
                    DisplayLayout(i).HideIngredients(items[i], true);
            for (int i = 0; i < displayLayouts.Length; i++)
                displayLayouts[i] = null;
        }

        // EN: Layout used for counts that have no component yet (3-4 dishes for now): the one handling the most dishes.
        // VI: Bố cục dùng cho số món chưa có component (hiện là 3-4 món): component xử lý nhiều món nhất.
        private OrderLayout FindFallbackLayout()
        {
            OrderLayout best = null;
            foreach (OrderLayout layout in GetComponents<OrderLayout>())
                if (layout != null && layout.enabled && (best == null || layout.DishCount > best.DishCount))
                    best = layout;
            return best;
        }

        // EN: Returns the layout component on this card that handles the given dish count.
        // VI: Trả về component bố cục trên thẻ ứng với số món cho trước.
        private OrderLayout FindLayout(int count)
        {
            foreach (OrderLayout layout in GetComponents<OrderLayout>())
                if (layout != null && layout.enabled && layout.DishCount == count)
                    return layout;
            return null;
        }

#if UNITY_EDITOR
        // EN: Resolves the card artwork and icon config, then previews the layout for previewDishCount (deferred, since components cannot be toggled inside OnValidate).
        // VI: Gán hình nền thẻ và cấu hình icon, rồi xem trước bố cục với previewDishCount (chạy trễ vì không được bật/tắt object trong OnValidate).
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                RefreshArrangement();
                return;
            }
            if (background == null)
            {
                Transform artwork = transform.Find("Artwork");
                if (artwork != null)
                    background = artwork.GetComponent<SpriteRenderer>();
            }
            if (foodIconConfig == null)
                foodIconConfig = UnityEditor.AssetDatabase.LoadAssetAtPath<FoodIconConfig>("Assets/HotPot/Config/FoodIconConfig.asset");
            RefreshPreview();
        }

        // EN: Re-applies the Edit Mode preview on the next editor tick; called by layout components when their settings change.
        // VI: Áp lại bản xem trước trong Edit Mode ở tick editor kế tiếp; component bố cục gọi khi thông số thay đổi.
        public void RefreshPreview()
        {
            if (Application.isPlaying)
                return;
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && !Application.isPlaying)
                    Layout(previewDishCount);
            };
        }
#endif
    }
}
