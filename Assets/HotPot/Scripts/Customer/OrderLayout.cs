using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Base of one order-card layout (one component per dish count). Subclasses decide the initial display (Arrange) and the expanded ingredient view (ShowIngredients); shared helpers place slots, fit the background and draw the ingredient grid.
    // VI: Lớp gốc của một bố cục thẻ order (mỗi số món một component). Lớp con tự quyết hiển thị ban đầu (Arrange) và giao diện mở rộng nguyên liệu (ShowIngredients); helper dùng chung để đặt ô, co nền và vẽ lưới nguyên liệu.
    public abstract class OrderLayout : MonoBehaviour
    {
        [Header("Lưới nguyên liệu (khi chạm vào món)")]
        [Tooltip("Bộ mẫu lưới nguyên liệu (ScriptableObject, dùng chung cho mọi thẻ order): mẫu theo số nguyên liệu. Tạo bằng Tools → HotPot → Create Ingredient Form Configs hoặc Create → HotPot → Ingredient Grid Forms.")]
        [SerializeField] protected IngredientGridFormConfig ingredientFormConfig;
        [Tooltip("Mẫu cũ lưu trên component (chỉ dùng khi chưa gán ingredientFormConfig; giữ để chuyển dữ liệu sang asset).")]
        [HideInInspector] [SerializeField] protected IngredientGridForm[] ingredientForms = IngredientGridForm.Defaults();
        [Tooltip("Độ sáng của nguyên liệu đã tới (0 = đen, 1 = gốc).")]
        [Range(0f, 1f)] [SerializeField] protected float doneBrightness = 0.35f;
        [Tooltip("Lệch của object Ingredient Grid so với tâm ô món (local của thẻ).")]
        [SerializeField] protected Vector2 ingredientGridOffset = Vector2.zero;

        private OrderView cardView;
        private OrderTweens tweens;
        private readonly Dictionary<SpriteRenderer, bool> doneStates = new Dictionary<SpriteRenderer, bool>();

        // EN: Tween runner of this layout (open/close/flash animations).
        // VI: Bộ chạy tween của bố cục (hoạt cảnh mở/đóng/nháy).
        protected OrderTweens Tweens => tweens ?? (tweens = new OrderTweens(this));

        // EN: The OrderView on the same card, holding the shared column and ingredient background settings.
        // VI: OrderView trên cùng thẻ, giữ thông số cột và nền tối nguyên liệu dùng chung.
        protected OrderView CardView => cardView != null ? cardView : (cardView = GetComponent<OrderView>());

        private readonly Dictionary<OrderItemView, Vector2> columnAreas = new Dictionary<OrderItemView, Vector2>();
        private readonly Dictionary<OrderItemView, Transform> gridRoots = new Dictionary<OrderItemView, Transform>();
        private readonly Dictionary<OrderItemView, SpriteRenderer> backdrops = new Dictionary<OrderItemView, SpriteRenderer>();
        private Sprite cardSprite;
        private static Sprite whiteSprite;
        private readonly Dictionary<OrderItemView, List<SpriteRenderer>> grids = new Dictionary<OrderItemView, List<SpriteRenderer>>();

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

        // EN: Expanded view of one dish: hides the dish picture and shows its raw ingredients (arrived ones darkened). Override for a special design.
        // VI: Giao diện mở rộng của một món: ẩn ảnh món, hiện nguyên liệu raw (nguyên liệu đã tới bị tối). Override khi cần thiết kế riêng.
        public virtual void ShowIngredients(OrderItemView item, IReadOnlyList<Sprite> icons, IReadOnlyList<bool> done)
        {
            OrderView view = CardView;
            float duration = view != null ? view.OpenDuration : 0f;
            if (!gridRoots.TryGetValue(item, out Transform root) || root == null || !root.gameObject.activeSelf || Tweens.IsRunning(root))
                item.SetDishVisible(false, duration * 0.6f);
            int count = icons.Count;
            DrawIngredientGrid(item, icons, done, ResolveIngredientForm(count));
        }

        // EN: Collapses one dish back to its picture.
        // VI: Thu một món về lại ảnh món.
        public virtual void HideIngredients(OrderItemView item, bool instant = false, bool restoreDish = true)
        {
            OrderView view = CardView;
            float duration = instant || view == null ? 0f : view.CloseDuration;
            if (restoreDish)
                item.SetDishVisible(true, duration);
            if (!gridRoots.TryGetValue(item, out Transform root) || root == null)
                return;
            if (!root.gameObject.activeSelf)
            {
                Tweens.Stop(root);
                return;
            }
            List<SpriteRenderer> cells = grids.TryGetValue(item, out List<SpriteRenderer> list) ? list : new List<SpriteRenderer>();
            backdrops.TryGetValue(item, out SpriteRenderer backdrop);
            Vector3[] startScales = new Vector3[cells.Count];
            for (int i = 0; i < cells.Count; i++)
                if (cells[i] != null)
                    startScales[i] = cells[i].transform.localScale;
            Color backdropColor = backdrop != null ? backdrop.color : Color.clear;
            Tweens.Run(root, duration, t =>
            {
                float k = 1f - OrderTweens.EaseOut(t);
                for (int i = 0; i < cells.Count; i++)
                    if (cells[i] != null)
                        cells[i].transform.localScale = startScales[i] * k;
                if (backdrop != null)
                    backdrop.color = new Color(backdropColor.r, backdropColor.g, backdropColor.b, backdropColor.a * k);
            }, () => root.gameObject.SetActive(false));
        }

        // EN: Grid preset for an ingredient count: the exact one, else the nearest count.
        // VI: Mẫu lưới cho một số nguyên liệu: đúng số đó, không có thì lấy mẫu có số gần nhất.
        protected virtual IngredientGridForm ResolveIngredientForm(int count)
        {
            IngredientGridForm[] forms = Forms;
            if (forms == null || forms.Length == 0)
                return new IngredientGridForm(count, count <= 3 ? 1 : 2, 0.11f, new Vector2(0.34f, 0.34f));
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

        // EN: Draws the ingredient grid row by row (left to right, then down) centred in the slot, like Unity's GridLayout.
        // VI: Vẽ lưới nguyên liệu theo hàng (trái sang phải rồi xuống dưới), căn giữa trong ô, giống GridLayout của Unity.
        protected void DrawIngredientGrid(OrderItemView item, IReadOnlyList<Sprite> icons, IReadOnlyList<bool> done, IngredientGridForm form)
        {
            List<SpriteRenderer> cells = GetCells(item, icons.Count);
            int columns = Mathf.Max(1, form.columns);
            int rows = Mathf.CeilToInt(icons.Count / (float)columns);
            Vector2 step = form.spacing;
            Vector3 iconScale = new Vector3(form.iconScale, form.iconScale, 1f);
            Transform root = GetGridRoot(item);
            bool opening = !root.gameObject.activeSelf || Tweens.IsRunning(root);
            Tweens.Stop(root);
            Vector3 slot = item.transform.localPosition;
            Vector2 anchor = form.collapseLayout ? form.collapseLayoutPosition : new Vector2(slot.x, slot.y);
            root.localPosition = new Vector3(anchor.x + ingredientGridOffset.x, anchor.y + ingredientGridOffset.y, slot.z);
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;
            root.gameObject.SetActive(true);
            SpriteRenderer reference = item.IngredientRenderer;
            DrawIngredientBackground(item, reference, form);
            for (int i = 0; i < cells.Count; i++)
            {
                SpriteRenderer cell = cells[i];
                bool used = i < icons.Count;
                cell.gameObject.SetActive(used);
                if (!used)
                    continue;
                int row = i / columns;
                int col = i % columns;
                float x = (col - (columns - 1) * 0.5f) * step.x;
                float y = ((rows - 1) * 0.5f - row) * step.y;
                cell.transform.localPosition = new Vector3(x, y, -0.01f);
                cell.sprite = icons[i];
                cell.transform.localRotation = Quaternion.identity;
                cell.transform.localScale = iconScale;
                bool isDone = done != null && i < done.Count && done[i];
                bool wasDone = doneStates.TryGetValue(cell, out bool previous) && previous;
                doneStates[cell] = isDone;
                float b = isDone ? doneBrightness : 1f;
                if (!opening && isDone && !wasDone)
                    FlashArrived(cell, iconScale);
                else if (!Tweens.IsRunning(cell))
                    cell.color = new Color(b, b, b, 1f);
                if (reference != null)
                {
                    cell.sortingLayerID = reference.sortingLayerID;
                    cell.sortingOrder = reference.sortingOrder + 2;
                }
            }
            if (opening)
                PlayOpen(item, cells, icons.Count, iconScale);
        }

        // EN: Dark panel behind the ingredient grid covering the dish's column (sliced card sprite tinted dark by default), drawn just above the hidden dish and below the ingredients.
        // VI: Nền tối phía sau lưới nguyên liệu phủ cột của món (mặc định dùng sprite nền thẻ dạng 9-slice nhuộm tối), vẽ ngay trên ảnh món đã ẩn và dưới các nguyên liệu.
        protected virtual void DrawIngredientBackground(OrderItemView item, SpriteRenderer reference, IngredientGridForm form)
        {
            backdrops.TryGetValue(item, out SpriteRenderer backdrop);
            OrderView view = CardView;
            if (view == null || !view.ShowIngredientBackground)
            {
                if (backdrop != null)
                    backdrop.gameObject.SetActive(false);
                return;
            }
            if (backdrop == null)
            {
                GameObject go = new GameObject("Ingredient Background");
                go.transform.SetParent(GetGridRoot(item), false);
                backdrop = go.AddComponent<SpriteRenderer>();
                backdrops[item] = backdrop;
            }
            Sprite card = view.Background != null ? view.Background.sprite : cardSprite;
            Sprite sprite = view.IngredientBackgroundSprite != null ? view.IngredientBackgroundSprite : card != null ? card : WhiteSprite();
            backdrop.gameObject.SetActive(true);
            backdrop.sprite = sprite;
            backdrop.color = view.IngredientBackgroundColor;
            Vector2 size;
            if (form.backgroundSize.x > 0f && form.backgroundSize.y > 0f)
                size = form.backgroundSize;
            else
            {
                Vector2 area;
                if (!columnAreas.TryGetValue(item, out area))
                {
                    Vector3 lossy = view.transform.lossyScale;
                    area = new Vector2(view.ColumnSize.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)), view.ColumnSize.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)));
                }
                size = new Vector2(Mathf.Max(0.01f, area.x - view.IngredientBackgroundInset.x * 2f), Mathf.Max(0.01f, area.y - view.IngredientBackgroundInset.y * 2f));
            }
            Transform t = backdrop.transform;
            t.localPosition = new Vector3(0f, 0f, -0.005f);
            t.localRotation = Quaternion.identity;
            if (sprite.border != Vector4.zero)
            {
                backdrop.drawMode = SpriteDrawMode.Sliced;
                t.localScale = Vector3.one;
                backdrop.size = size;
            }
            else
            {
                backdrop.drawMode = SpriteDrawMode.Simple;
                Vector2 bounds = sprite.bounds.size;
                t.localScale = new Vector3(size.x / Mathf.Max(0.0001f, bounds.x), size.y / Mathf.Max(0.0001f, bounds.y), 1f);
            }
            if (reference != null)
            {
                backdrop.sortingLayerID = reference.sortingLayerID;
                backdrop.sortingOrder = reference.sortingOrder + 1;
            }
        }

        // EN: Shared 1x1 white sprite used when no background sprite is available.
        // VI: Sprite trắng 1x1 dùng chung khi không có sprite nền.
        private static Sprite WhiteSprite()
        {
            if (whiteSprite == null)
                whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 4f);
            return whiteSprite;
        }

        // EN: Lays the dishes out as one row of world-sized columns centred on the card (columnSpacing between columns). In each column the dish area = column x dishFill: the slot is scaled so its dish picture (OrderItemView.DishSize) fits that area, its BoxCollider2D tap area is resized to exactly that area; the whole column is the dish's ingredient area. The background width = sum of the columns + spacing + padding.
        // VI: Xếp các món thành một hàng cột (kích thước world) căn giữa thẻ (columnSpacing giữa các cột). Trong mỗi cột, vùng món = cột x dishFill: ô món được scale để ảnh món (OrderItemView.DishSize) vừa vùng đó, BoxCollider2D (vùng chạm) khít đúng vùng đó; cả cột là vùng nguyên liệu của món. Chiều rộng nền = tổng các cột + khoảng cách + lề.
        protected void ArrangeColumns(OrderView view, IReadOnlyList<OrderItemView> items, int count)
        {
            count = Mathf.Min(count, items.Count);
            cardSprite = view.Background != null ? view.Background.sprite : null;
            Vector3 lossy = view.transform.lossyScale;
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
                    Vector3 itemLossy = item.transform.lossyScale;
                    Vector2 toItem = new Vector2(1f / Mathf.Max(0.0001f, Mathf.Abs(itemLossy.x)), 1f / Mathf.Max(0.0001f, Mathf.Abs(itemLossy.y)));
                    Vector2 areaItem = Vector2.Scale(areaWorld, toItem);
                    columnAreas[item] = new Vector2(columnWorld.x / card.x, columnWorld.y / card.y);
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

        // EN: Returns (creating when needed) the pooled ingredient renderers under a slot.
        // VI: Trả về (tạo thêm khi cần) các renderer nguyên liệu dùng lại dưới một ô.
        // EN: Opening animation: the dark panel fades in while the ingredients pop in one after another (recipe order).
        // VI: Hoạt cảnh mở: nền tối hiện dần trong khi các nguyên liệu nảy ra lần lượt (theo thứ tự công thức).
        private void PlayOpen(OrderItemView item, List<SpriteRenderer> cells, int count, Vector3 iconScale)
        {
            OrderView view = CardView;
            float open = view != null ? view.OpenDuration : 0f;
            float stagger = view != null ? view.IngredientStagger : 0f;
            Transform root = GetGridRoot(item);
            backdrops.TryGetValue(item, out SpriteRenderer backdrop);
            Color backdropColor = backdrop != null ? backdrop.color : Color.clear;
            float total = open + stagger * Mathf.Max(0, count - 1);
            if (total > 0f && isActiveAndEnabled)
            {
                for (int i = 0; i < count && i < cells.Count; i++)
                    cells[i].transform.localScale = Vector3.zero;
                if (backdrop != null)
                    backdrop.color = new Color(backdropColor.r, backdropColor.g, backdropColor.b, 0f);
            }
            Tweens.Run(root, total, t =>
            {
                float time = t * total;
                for (int i = 0; i < count && i < cells.Count; i++)
                {
                    float k = open > 0f ? Mathf.Clamp01((time - i * stagger) / open) : 1f;
                    cells[i].transform.localScale = iconScale * OrderTweens.EaseOutBack(k);
                }
                if (backdrop != null)
                {
                    float a = open > 0f ? OrderTweens.EaseOut(Mathf.Clamp01(time / open)) : 1f;
                    backdrop.color = new Color(backdropColor.r, backdropColor.g, backdropColor.b, backdropColor.a * a);
                }
            });
        }

        // EN: An ingredient that just arrived while its grid is open pops and dims from full colour to the done brightness.
        // VI: Nguyên liệu vừa tới khi lưới đang mở sẽ nảy nhẹ và tối dần từ màu gốc về độ sáng "đã xong".
        private void FlashArrived(SpriteRenderer cell, Vector3 iconScale)
        {
            OrderView view = CardView;
            float duration = view != null ? view.ArrivalFlashDuration : 0f;
            float target = doneBrightness;
            Tweens.Run(cell, duration, t =>
            {
                float b = Mathf.Lerp(1f, target, OrderTweens.EaseOut(t));
                cell.color = new Color(b, b, b, 1f);
                cell.transform.localScale = iconScale * (1f + 0.25f * Mathf.Sin(t * Mathf.PI));
            });
        }

        // EN: Unscaled container of a dish's ingredient view, a sibling of the dish slot on the card placed by DrawIngredientGrid at the slot centre (or, for a collapseLayout form, at collapseLayoutPosition on the card) + ingredientGridOffset; cell transforms under it take the form values as-is.
        // VI: Object chứa giao diện nguyên liệu của món, không scale, là anh em của ô món trên thẻ được DrawIngredientGrid đặt tại tâm ô (hoặc, với mẫu bật collapseLayout, tại collapseLayoutPosition trên thẻ) + ingredientGridOffset; transform của ô nguyên liệu bên trong nhận nguyên giá trị của mẫu.
        protected Transform GetGridRoot(OrderItemView item)
        {
            if (!gridRoots.TryGetValue(item, out Transform root) || root == null)
            {
                root = new GameObject("Ingredient Grid · " + item.name).transform;
                root.SetParent(item.transform.parent, false);
                gridRoots[item] = root;
            }
            return root;
        }

        private List<SpriteRenderer> GetCells(OrderItemView item, int needed)
        {
            if (!grids.TryGetValue(item, out List<SpriteRenderer> cells))
            {
                cells = new List<SpriteRenderer>();
                grids[item] = cells;
            }
            cells.RemoveAll(c => c == null);
            while (cells.Count < needed)
            {
                GameObject go = new GameObject("Ingredient " + cells.Count);
                go.transform.SetParent(GetGridRoot(item), false);
                cells.Add(go.AddComponent<SpriteRenderer>());
            }
            return cells;
        }
    }
}
