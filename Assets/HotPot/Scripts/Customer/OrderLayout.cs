using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Base of one order-card layout (one component per dish count). Subclasses decide the initial display (Arrange); tapping a dish loads the designed ingredient layout prefab (IngredientPreviewView, chosen per ingredient count from the form config), whose own component draws and animates the ingredients.
    // VI: Lớp gốc của một bố cục thẻ order (mỗi số món một component). Lớp con tự quyết hiển thị ban đầu (Arrange); chạm món sẽ nạp prefab layout nguyên liệu thiết kế sẵn (IngredientPreviewView, chọn theo số nguyên liệu trong bộ mẫu), component của prefab tự vẽ và chạy hiệu ứng nguyên liệu.
    public abstract class OrderLayout : MonoBehaviour
    {
        [Header("Layout nguyên liệu (khi chạm vào món)")]
        [Tooltip("Bộ mẫu theo số nguyên liệu (ScriptableObject, dùng chung cho mọi thẻ order): prefab layout nguyên liệu + tuỳ chọn collapse.")]
        [SerializeField] protected IngredientGridFormConfig ingredientFormConfig;
        [Tooltip("Mẫu cũ lưu trên component (chỉ dùng khi chưa gán ingredientFormConfig; giữ để chuyển dữ liệu sang asset).")]
        [HideInInspector] [SerializeField] protected IngredientGridForm[] ingredientForms = IngredientGridForm.Defaults();
        [Tooltip("Lệch của layout nguyên liệu so với vị trí đặt (local của thẻ).")]
        [SerializeField] protected Vector2 ingredientGridOffset = Vector2.zero;

        private OrderView cardView;
        private readonly Dictionary<OrderItemView, IngredientPreviewView> activePreviews = new Dictionary<OrderItemView, IngredientPreviewView>();
        private readonly Dictionary<OrderItemView, Dictionary<IngredientPreviewView, IngredientPreviewView>> previewPool = new Dictionary<OrderItemView, Dictionary<IngredientPreviewView, IngredientPreviewView>>();

        // EN: The OrderView on the same card, holding the shared column settings.
        // VI: OrderView trên cùng thẻ, giữ thông số cột dùng chung.
        protected OrderView CardView => cardView != null ? cardView : (cardView = GetComponent<OrderView>());

        // EN: Number of dishes this layout handles; OrderView picks the layout whose count matches the order.
        // VI: Số món mà bố cục này xử lý; OrderView chọn bố cục có số món khớp với order.
        public abstract int DishCount { get; }

        // EN: True when the form for this ingredient count uses collapseLayout (the other dishes hide while it shows its ingredients).
        // VI: True khi mẫu cho số nguyên liệu này bật collapseLayout (các món khác ẩn đi khi món này hiện nguyên liệu).
        public virtual bool IsCollapseLayout(int ingredientCount) => ResolveIngredientForm(ingredientCount).collapseLayout;

        // EN: Seconds after opening a dish with this many ingredients before the card collapses onto the 1-dish layout (form's collapseLayout / collapseTime); negative = never.
        // VI: Số giây sau khi mở món có chừng này nguyên liệu trước khi thẻ thu về bố cục 1 món (theo collapseLayout / collapseTime của mẫu); số âm = không thu.
        public virtual float GetAutoCollapseDelay(int ingredientCount)
        {
            IngredientGridForm form = ResolveIngredientForm(ingredientCount);
            return form.collapseLayout ? form.collapseTime : -1f;
        }

        // EN: Places the active slots and fits the card for the initial (collapsed) display.
        // VI: Đặt các ô đang dùng và co thẻ cho hiển thị ban đầu (chưa mở rộng).
        public abstract void Arrange(OrderView view, IReadOnlyList<OrderItemView> items);

        // EN: Expanded view of one dish: loads the designed layout of its form (by ingredient count). A form without a layout prefab keeps showing the dish picture and logs a warning.
        // VI: Giao diện mở rộng của một món: nạp layout thiết kế sẵn của mẫu (theo số nguyên liệu). Mẫu chưa có prefab layout thì giữ ảnh món và báo cảnh báo.
        public virtual void ShowIngredients(OrderItemView item, IReadOnlyList<Sprite> icons, IReadOnlyList<bool> done)
        {
            IngredientGridForm form = ResolveIngredientForm(icons.Count);
            if (form.template == null)
            {
                Debug.LogWarning($"[OrderLayout] {name}/{GetType().Name}: mẫu {form.ingredientCount} nguyên liệu chưa gán prefab layout (template); chạy Tools → HotPot → Setup Ingredient Previews.", this);
                return;
            }
            ShowPreview(item, form, icons, done);
        }

        // EN: Loads the designed layout (form.template) for a dish: one instance per dish slot and prefab, placed under the card at scale 1: on the dish slot, or for the first phase of a collapse form opened to the right of the dish column (to the left for the last dish), while the collapsed view (this 1-dish layout drawing a dish of a bigger card) is always centred on the dish, then filled and opened with its own transition.
        // VI: Nạp layout thiết kế sẵn (form.template) cho một món: mỗi ô món và mỗi prefab một instance, đặt dưới thẻ với scale 1: tại ô món, hoặc ở giai đoạn đầu của mẫu collapse thì mở sang phải từ cột của món (món cuối mở sang trái); còn trạng thái đã thu (bố cục 1 món vẽ một món của thẻ nhiều món) luôn nằm giữa món, rồi gán nguyên liệu và mở bằng hiệu ứng riêng của nó.
        protected void ShowPreview(OrderItemView item, IngredientGridForm form, IReadOnlyList<Sprite> icons, IReadOnlyList<bool> done)
        {
            OrderView view = CardView;
            bool animate = view == null || view.AnimateTransitions;
            IngredientPreviewView preview = GetPreview(item, form.template);
            if (activePreviews.TryGetValue(item, out IngredientPreviewView previous) && previous != null && previous != preview)
                previous.Hide(true);
            bool opening = !preview.gameObject.activeSelf || preview.IsClosing || previous != preview;
            Vector3 slot = item.transform.localPosition;
            bool collapsedView = view != null && !view.IsCardLayout(this);
            Vector2 anchor = form.collapseLayout && !collapsedView
                ? CollapseAnchor(item, preview) + form.collapseLayoutPosition
                : new Vector2(slot.x, slot.y);
            Transform t = preview.transform;
            t.localPosition = new Vector3(anchor.x + ingredientGridOffset.x, anchor.y + ingredientGridOffset.y, slot.z - 0.01f);
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            if (opening)
                item.SetDishVisible(false, animate ? preview.OpenDuration * 0.6f : 0f);
            preview.Show(icons, done, item.IngredientRenderer, opening, animate);
            activePreviews[item] = preview;
        }

        // EN: First collapse phase: the layout grows to the right from the left edge of the dish's column, except for the last dish of a multi-dish card, which grows to the left from its column's right edge; a layout not wider than the column stays centred on the dish.
        // VI: Giai đoạn đầu của collapse: layout mọc sang phải từ mép trái cột của món, riêng món cuối của thẻ nhiều món thì mọc sang trái từ mép phải cột; layout không rộng hơn cột thì nằm giữa món.
        protected Vector2 CollapseAnchor(OrderItemView item, IngredientPreviewView preview)
        {
            Vector3 slot = item.transform.localPosition;
            OrderView view = CardView;
            Rect rect = preview.LocalRect;
            float column = view != null ? view.ColumnSize.x / Mathf.Max(0.0001f, Mathf.Abs(view.RestLossyScale.x)) : rect.width;
            if (rect.width <= column + 0.0001f)
                return new Vector2(slot.x - rect.center.x, slot.y);
            int index = view != null ? view.IndexOfDish(item) : -1;
            int count = view != null ? view.ActiveDishCount : 0;
            bool last = count > 1 && index == count - 1;
            float x = last
                ? slot.x + column * 0.5f - rect.xMax
                : slot.x - column * 0.5f - rect.xMin;
            return new Vector2(x, slot.y);
        }

        // EN: Card-local rectangle covered by the dish's open ingredient layout (false when none is open).
        // VI: Hình chữ nhật (local của thẻ) mà layout nguyên liệu đang mở của món chiếm (false khi không có).
        public bool TryGetOpenArea(OrderItemView item, out Rect area)
        {
            area = default;
            if (!activePreviews.TryGetValue(item, out IngredientPreviewView preview) || preview == null)
                return false;
            Rect local = preview.LocalRect;
            Vector2 offset = preview.transform.localPosition;
            area = new Rect(local.position + offset, local.size);
            return true;
        }

        // EN: Returns the pooled instance of a designed layout for a dish slot, creating it (switched off) under the card the first time.
        // VI: Trả về instance dùng lại của layout thiết kế sẵn cho một ô món, tạo mới (đang tắt) dưới thẻ ở lần đầu.
        private IngredientPreviewView GetPreview(OrderItemView item, IngredientPreviewView template)
        {
            if (!previewPool.TryGetValue(item, out Dictionary<IngredientPreviewView, IngredientPreviewView> byTemplate))
            {
                byTemplate = new Dictionary<IngredientPreviewView, IngredientPreviewView>();
                previewPool[item] = byTemplate;
            }
            if (byTemplate.TryGetValue(template, out IngredientPreviewView instance) && instance != null)
                return instance;
            instance = Instantiate(template, item.transform.parent, false);
            instance.name = template.name + " · " + item.name;
            instance.gameObject.SetActive(false);
            foreach (Collider2D extra in instance.GetComponentsInChildren<Collider2D>(true))
                extra.enabled = false;
            byTemplate[template] = instance;
            return instance;
        }

        // EN: Closes a dish's ingredient layout (instantly or with its close transition) and, unless told otherwise, pops the dish picture back.
        // VI: Đóng layout nguyên liệu của món (tức thì hoặc có hiệu ứng đóng) và, trừ khi được yêu cầu khác, cho ảnh món nảy lại.
        public virtual void HideIngredients(OrderItemView item, bool instant = false, bool restoreDish = true)
        {
            OrderView view = CardView;
            bool skip = instant || view == null || !view.AnimateTransitions;
            if (!activePreviews.TryGetValue(item, out IngredientPreviewView preview) || preview == null)
            {
                if (restoreDish)
                    item.SetDishVisible(true);
                return;
            }
            activePreviews.Remove(item);
            if (restoreDish)
                item.SetDishVisible(true, skip ? 0f : preview.CloseDuration);
            preview.Hide(skip);
        }

        // EN: Entry for an ingredient count: the exact one, else the nearest count.
        // VI: Mẫu cho một số nguyên liệu: đúng số đó, không có thì lấy mẫu có số gần nhất.
        protected virtual IngredientGridForm ResolveIngredientForm(int count)
        {
            IngredientGridForm[] forms = Forms;
            if (forms == null || forms.Length == 0)
                return new IngredientGridForm(count);
            IngredientGridForm best = forms[0];
            foreach (IngredientGridForm form in forms)
                if (Mathf.Abs(form.ingredientCount - count) < Mathf.Abs(best.ingredientCount - count))
                    best = form;
            return best;
        }

        // EN: Forms in use: the shared config asset when assigned, otherwise the legacy per-component array.
        // VI: Bộ mẫu đang dùng: asset cấu hình dùng chung nếu đã gán, ngược lại là mảng cũ trên component.
        protected IngredientGridForm[] Forms =>
            ingredientFormConfig != null && ingredientFormConfig.forms != null && ingredientFormConfig.forms.Length > 0
                ? ingredientFormConfig.forms
                : ingredientForms;

        // EN: Shared form config asset (null when not assigned yet).
        // VI: Asset bộ mẫu dùng chung (null khi chưa gán).
        public IngredientGridFormConfig IngredientFormConfig => ingredientFormConfig;

        // EN: Legacy per-component forms, read by the editor migration tool.
        // VI: Bộ mẫu cũ trên component, công cụ editor đọc để chuyển sang asset.
        public IngredientGridForm[] LegacyIngredientForms => ingredientForms;

        // EN: Places slot i at a local position with the given scale.
        // VI: Đặt ô i tại vị trí local với scale cho trước.
        protected static void PlaceSlot(OrderItemView item, Vector2 position, float scale)
        {
            if (item == null)
                return;
            Transform slot = item.transform;
            slot.localPosition = new Vector3(position.x, position.y, slot.localPosition.z);
            slot.localScale = new Vector3(scale, scale, scale);
        }

        // EN: Lays the dishes out as one row of world-sized columns centred on the card (columnSpacing between columns). In each column the dish area = column x dishFill: the slot is scaled so its dish picture (OrderItemView.DishSize) fits that area, and its BoxCollider2D tap area is resized to exactly that area. The background width = sum of the columns + spacing + padding.
        // VI: Xếp các món thành một hàng cột (kích thước world) căn giữa thẻ (columnSpacing giữa các cột). Trong mỗi cột, vùng món = cột x dishFill: ô món được scale để ảnh món (OrderItemView.DishSize) vừa vùng đó, và BoxCollider2D (vùng chạm) khít đúng vùng đó. Chiều rộng nền = tổng các cột + khoảng cách + lề.
        protected void ArrangeColumns(OrderView view, IReadOnlyList<OrderItemView> items, int count)
        {
            count = Mathf.Min(count, items.Count);
            Vector3 lossy = view.RestLossyScale;
            Vector2 card = new Vector2(Mathf.Max(0.0001f, Mathf.Abs(lossy.x)), Mathf.Max(0.0001f, Mathf.Abs(lossy.y)));
            Vector2 columnSize = view.ColumnSize;
            float columnSpacing = view.ColumnSpacing;
            float dishFill = view.DishFill;
            Vector2 columnsOffset = view.ColumnsOffset;
            float totalWorld = count * columnSize.x;
            if (count >= 2)
                totalWorld += (count - 1) * columnSpacing;
            float xWorld = columnsOffset.x - totalWorld * 0.5f;
            for (int i = 0; i < count; i++)
            {
                Vector2 columnWorld = columnSize;
                Vector2 areaWorld = columnWorld * dishFill;
                Vector2 areaLocal = new Vector2(areaWorld.x / card.x, areaWorld.y / card.y);
                float dishSize = items[i] != null ? Mathf.Max(0.0001f, items[i].DishSize) : 1f;
                float scale = Mathf.Min(areaLocal.x, areaLocal.y) / dishSize;
                Vector2 centre = new Vector2((xWorld + columnWorld.x * 0.5f) / card.x, columnsOffset.y / card.y);
                PlaceSlot(items[i], centre, scale);
                OrderItemView item = items[i];
                if (item != null)
                {
                    Vector3 itemLossy = Vector3.Scale(view.RestLossyScale, item.transform.localScale);
                    Vector2 toItem = new Vector2(1f / Mathf.Max(0.0001f, Mathf.Abs(itemLossy.x)), 1f / Mathf.Max(0.0001f, Mathf.Abs(itemLossy.y)));
                    Vector2 areaItem = Vector2.Scale(areaWorld, toItem);
                    BoxCollider2D tap = item.GetComponent<BoxCollider2D>();
                    if (tap != null)
                    {
                        tap.size = areaItem;
                        tap.offset = Vector2.zero;
                    }
                }
                xWorld += columnWorld.x + columnSpacing;
            }
            FitBackgroundWidth(view, totalWorld / card.x + view.Padding.x);
        }

        // EN: Lays out a dish count that has no layout component yet as one row of columns with this layout's settings.
        // VI: Xếp số món chưa có component bố cục thành một hàng cột theo thông số của bố cục này.
        public void ArrangeFallback(OrderView view, IReadOnlyList<OrderItemView> items)
        {
            ArrangeColumns(view, items, items.Count);
        }

        // EN: Resizes only the background width (height kept) to the given card-local width and centres it on the card.
        // VI: Chỉ đổi chiều rộng nền (giữ chiều cao) theo chiều rộng local của thẻ và đặt nền vào tâm thẻ.
        protected static void FitBackgroundWidth(OrderView view, float width)
        {
            SpriteRenderer background = view.Background;
            if (background == null)
                return;
            background.enabled = true;
            Transform panel = background.transform;
            background.drawMode = SpriteDrawMode.Sliced;
            background.size = new Vector2(width / Mathf.Max(0.0001f, Mathf.Abs(panel.localScale.x)), background.size.y);
            panel.localPosition = new Vector3(0f, 0f, panel.localPosition.z);
        }

#if UNITY_EDITOR
        // EN: Refreshes the card's Edit Mode preview when this layout's settings change.
        // VI: Cập nhật bản xem trước của thẻ trong Edit Mode khi thông số bố cục thay đổi.
        protected virtual void OnValidate()
        {
            OrderView view = GetComponent<OrderView>();
            if (view == null)
                return;
            if (Application.isPlaying)
                view.RefreshExpanded();
            else
                view.RefreshPreview();
        }
#endif
    }
}
