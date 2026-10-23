using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{

    [DisallowMultipleComponent]
    public class ServingTrayView : MonoBehaviour
    {
        // EN: A tray never holds more than this many dishes, so the slot pool is capped here.
        // VI: Khay không bao giờ chứa quá số món này, nên pool ô được giới hạn tại đây.
        public const int MaxSlots = 4;

        [Tooltip("Bộ hiển thị hình khay ở đối tượng con, dùng làm hệ tọa độ cho các ô món.")]
        [SerializeField]
        private SpriteRenderer trayRenderer;

        [Tooltip("Độ lệch tâm các ô món so với tâm ảnh khay.")]
        [SerializeField]
        private Vector2 slotCenter = new Vector2(0f, 0.08f);

        [Tooltip("Khoảng cách ngang giữa tâm các ô món cạnh nhau (tọa độ local của khay).")]
        [SerializeField]
        private float spacingX = 0.5f;

        [Tooltip("Component xếp chồng nguyên liệu trên các ô của khay (cùng object).")]
        [SerializeField]
        private TrayDishStack dishStack;

        [Tooltip("Prefab ô món trống (gốc có DishSlotView, viền nét đứt và hiệu ứng lấp lánh). Tạo sẵn tối đa 4 ô và tái sử dụng, chỉ bật/tắt theo số món trong order.")]
        [SerializeField]
        private DishSlotView dishSlotViewPrefab;

        // Pool of every slot ever created for this tray (at most MaxSlots); only the first activeCount entries are in use.
        [SerializeField]
        [HideInInspector]
        private List<Transform> slots = new List<Transform>();

        [SerializeField]
        [HideInInspector]
        private List<DishSlotView> slotViews = new List<DishSlotView>();

        private readonly List<Transform> activeSlots = new List<Transform>(MaxSlots);

        private int activeCount;

        // EN: Returns the active dish anchors in order.
        // VI: Trả về các vị trí đặt món đang dùng theo thứ tự.
        public IReadOnlyList<Transform> Slots => activeSlots;

        // EN: Returns the current number of active dish slots.
        // VI: Trả về số ô món đang dùng.
        public int SlotCount => activeCount;

        // EN: Returns the DishSlotView of an active slot, or null for an invalid index.
        // VI: Trả về DishSlotView của một ô đang dùng, hoặc null khi chỉ số không hợp lệ.
        public DishSlotView GetSlotView(int index)
        {
            return index >= 0 && index < activeCount && index < slotViews.Count ? slotViews[index] : null;
        }

        // EN: Returns the stack component linked to this tray.
        // VI: Trả về component xếp chồng được nối với khay.
        public TrayDishStack DishStack => dishStack;

        // EN: Returns the tray sorting layer for served food.
        // VI: Trả về lớp vẽ của khay dùng cho món được phục vụ.
        public int SortingLayerId => (trayRenderer != null) ? trayRenderer.sortingLayerID : 0;

        // EN: Returns the first sorting order above tray slot outlines.
        // VI: Trả về thứ tự vẽ đầu tiên phía trên viền ô khay.
        public int StackSortingOrder => ((trayRenderer != null) ? trayRenderer.sortingOrder : 0) + 2;

        // EN: Shows the requested number of slots (clamped to MaxSlots) by enabling pooled prefab instances; slots are created only the first time they are needed and are never destroyed afterwards.
        // VI: Hiển thị đúng số ô yêu cầu (giới hạn MaxSlots) bằng cách bật các instance prefab trong pool; ô chỉ được tạo ở lần đầu cần dùng và không bị hủy sau đó.
        public void SetSlotCount(int count)
        {
            if (trayRenderer == null)
            {
                return;
            }
            count = ClampSlotCount(count);
            PrunePool();
            if (!EnsurePoolSize(count))
            {
                return;
            }
            activeCount = count;
            activeSlots.Clear();
            for (int i = 0; i < slots.Count; i++)
            {
                bool active = i < count;
                if (!active)
                {
                    slotViews[i].StopTwinkle();
                } else
                {
                    activeSlots.Add(slots[i]);
                }
                if (slots[i].gameObject.activeSelf != active)
                {
                    slots[i].gameObject.SetActive(active);
                }
            }
            LayoutSlots();
        }

        // EN: Clamps the requested slot count to [0, MaxSlots] and warns when an order asks for more dishes than the tray supports.
        // VI: Giới hạn số ô yêu cầu trong [0, MaxSlots] và cảnh báo khi order đòi nhiều món hơn khay hỗ trợ.
        private int ClampSlotCount(int count)
        {
            if (count > MaxSlots)
            {
                Debug.LogWarning($"[ServingTrayView] Order có {count} món, vượt quá tối đa {MaxSlots} ô của khay.", this);
            }
            return Mathf.Clamp(count, 0, MaxSlots);
        }

        // EN: Drops pool entries whose objects were deleted externally (e.g. by hand in the editor) and extra entries beyond MaxSlots, keeping slots and slotViews aligned.
        // VI: Bỏ các phần tử pool có object đã bị xóa từ bên ngoài (vd. xóa tay trong editor) và phần tử thừa quá MaxSlots, giữ slots và slotViews khớp nhau.
        private void PrunePool()
        {
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                DishSlotView view = i < slotViews.Count ? slotViews[i] : null;
                if (slots[i] == null || view == null || i >= MaxSlots)
                {
                    if (slots[i] != null && i >= MaxSlots)
                    {
                        DestroySlotObject(slots[i].gameObject);
                    }
                    slots.RemoveAt(i);
                    if (i < slotViews.Count)
                    {
                        slotViews.RemoveAt(i);
                    }
                }
            }
            if (slotViews.Count > slots.Count)
            {
                slotViews.RemoveRange(slots.Count, slotViews.Count - slots.Count);
            }
        }

        // EN: Instantiates prefab slots until the pool holds at least the requested count; returns false when the prefab is missing.
        // VI: Tạo thêm ô từ prefab cho tới khi pool có ít nhất số ô yêu cầu; trả về false khi chưa gán prefab.
        private bool EnsurePoolSize(int count)
        {
            if (slots.Count >= count)
            {
                return true;
            }
            if (dishSlotViewPrefab == null)
            {
                Debug.LogWarning("[ServingTrayView] Chưa gán prefab ô món.", this);
                return false;
            }
            while (slots.Count < count)
            {
                DishSlotView view = Object.Instantiate(dishSlotViewPrefab, trayRenderer.transform, worldPositionStays: false);
                Transform slot = view.transform;
                slot.name = $"Dish Slot {slots.Count + 1}";
                slot.gameObject.layer = base.gameObject.layer;
                slotViews.Add(view);
                slots.Add(slot);
            }
            return true;
        }

        // EN: Destroys a slot object with the call that is valid for the current mode (play or edit).
        // VI: Hủy object ô bằng hàm phù hợp với chế độ hiện tại (play hoặc edit).
        private static void DestroySlotObject(GameObject slot)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(slot);
            } else
            {
                Object.DestroyImmediate(slot);
            }
        }

        // EN: Lays the active slots out in one horizontal row centred on slotCenter (odd counts put the middle slot at slotCenter.x, e.g. slot 2 of 3), neighbouring centres spacingX apart in tray-local space, and updates sorting without changing their appearance.
        // VI: Xếp các ô đang dùng thành một hàng ngang căn giữa quanh slotCenter (số ô lẻ thì ô giữa nằm đúng slotCenter.x, vd. ô 2 khi có 3 ô), tâm các ô cạnh nhau cách nhau spacingX theo tọa độ local của khay, và cập nhật thứ tự vẽ mà không đổi hình thức ô.
        private void LayoutSlots()
        {
            float half = (activeCount - 1) * 0.5f;
            for (int i = 0; i < activeCount; i++)
            {
                DishSlotView view = slotViews[i];
                float offset = (i - half) * spacingX;
                view.transform.localPosition = new Vector3(slotCenter.x + offset, slotCenter.y, 0f);
                UpdateSlotSorting(view);
            }
        }


        // EN: Draws the referenced slot outline above the tray without overriding size, sprite or colour.
        // VI: Vẽ viền ô (qua reference) phía trên khay mà không ghi đè kích thước, ảnh hoặc màu.
        private void UpdateSlotSorting(DishSlotView view)
        {
            SpriteRenderer outline = view.Outline;
            if (outline == null)
            {
                return;
            }
            outline.gameObject.layer = base.gameObject.layer;
            outline.sortingLayerID = trayRenderer.sortingLayerID;
            outline.sortingOrder = trayRenderer.sortingOrder + 1;
        }

        // EN: Resolves the companion tray component on the same object and re-lays out the active slots when spacing is edited in the Inspector.
        // VI: Gán component khay đi cùng trên cùng đối tượng và xếp lại các ô đang dùng khi chỉnh khoảng cách trong Inspector.
        private void OnValidate()
        {
            if (dishStack == null)
            {
                dishStack = GetComponent<TrayDishStack>();
            }
            spacingX = Mathf.Max(0f, spacingX);
            if (trayRenderer != null && activeCount > 0)
            {
                LayoutSlots();
            }
        }
    }

}
