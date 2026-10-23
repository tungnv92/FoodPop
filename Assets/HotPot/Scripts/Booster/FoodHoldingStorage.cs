using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FoodPop
{
    // EN: Holds individual conveyor portions and returns only ingredients still missing from current orders.
    // VI: Giữ từng phần food của băng chuyền và chỉ trả nguyên liệu order hiện tại còn thiếu.
    public sealed class FoodHoldingStorage : MonoBehaviour
    {
        public sealed class Portion
        {
            public FoodType type;
            public FoodState state;
            public SpriteRenderer visual;
            public Vector3 worldScale;
        }

        [Tooltip("Khay có chuyển động kéo ra và thu vào.")]
        [SerializeField] private FoodHoldingTray tray;
        [Tooltip("Object chứa hình food ở giữa lớp sau và lớp trước của khay.")]
        [SerializeField] private Transform foodRoot;
        [Tooltip("Băng chuyền nhận food trả từ khay.")]
        [SerializeField] private ConveyorChefMovement conveyor;
        [Tooltip("Bảng hình nguyên liệu dùng để trả food vào slot.")]
        [SerializeField] private FoodIconConfig iconConfig;
        [Tooltip("Thời gian food bay trở lại đĩa trống, tính bằng giây.")]
        [SerializeField, Min(0.05f)] private float returnDuration = 0.4f;

        private readonly List<Portion> portions = new List<Portion>();
        private readonly List<FoodType> needs = new List<FoodType>();
        private Coroutine returning;
        private ConveyorSlot returnSlot;
        private Portion returnPortion;
        private int returnArrivalId;
        private SpriteRenderer returnFlyer;
        public bool IsReady => isActiveAndEnabled && tray != null && tray.isActiveAndEnabled && foodRoot != null;
        public bool IsReturning => returning != null;
        public int Count => portions.Count;

        // EN: Reserves demand returns with a unique slot arrival identity while no booster owns the tray.
        // VI: Giữ chỗ chuyến trả theo nhu cầu bằng mã đáp riêng khi không booster nào đang dùng khay.
        private void Update()
        {
            if (!IsReady || IsReturning || portions.Count == 0 || conveyor == null || conveyor.BoosterPaused ||
                (BoosterController.Instance != null && BoosterController.Instance.IsRunning))
                return;
            CustomerManager customers = CustomerManager.Instance;
            if (customers == null)
                return;
            customers.GetNeededIngredients(needs);
            foreach (FoodType need in needs)
            {
                Portion portion = portions.Find(item => item.type == need);
                if (portion == null)
                    continue;
                ConveyorSlot slot = FindEmptySlot();
                if (slot == null || !slot.TryFillFood(portion.type, iconConfig))
                    return;
                slot.food.SetState(portion.state);
                slot.BeginFoodArrival();
                returnSlot = slot;
                returnPortion = portion;
                returnArrivalId = slot.ArrivalId;
                returning = StartCoroutine(RunReturn(portion, slot, returnArrivalId));
                return;
            }
        }

        // EN: Finds an active empty slot; reservations made before flight prevent other picks from taking it.
        // VI: Tìm slot rỗng đang hoạt động; giữ chỗ trước khi bay tránh lượt pick khác chiếm slot.
        private ConveyorSlot FindEmptySlot()
        {
            foreach (ConveyorSlot slot in conveyor.Slots)
                if (slot != null && slot.isActiveAndEnabled && slot.isEmpty)
                    return slot;
            return null;
        }

        // EN: Registers a portion with its original conveyor world scale so storage never shrinks it.
        // VI: Ghi nhận phần food cùng scale world gốc trên băng để kho không thu nhỏ food.
        public Portion Store(FoodType type, FoodState state, Sprite sprite, Vector3 worldScale)
        {
            var visual = new GameObject("Stored food · " + type).AddComponent<SpriteRenderer>();
            visual.transform.SetParent(foodRoot, false);
            visual.sprite = sprite != null ? sprite : iconConfig != null ? iconConfig.GetIcon(type, state) : null;
            visual.enabled = false;
            var portion = new Portion { type = type, state = state, visual = visual, worldScale = worldScale };
            portions.Add(portion);
            LayoutPortions();
            return portion;
        }

        // EN: Returns a stored portion's current target, following drawer motion and layout changes.
        // VI: Trả về đích hiện tại của food đã giữ, theo chuyển động khay và thay đổi bố cục.
        public Vector3 GetPosition(Portion portion)
        {
            return portion.visual != null ? portion.visual.transform.position : foodRoot.position;
        }

        // EN: Reveals a collected portion between the tray's back and glass layers.
        // VI: Hiện phần food đã thu giữa đáy khay và lớp kính.
        public void Reveal(Portion portion)
        {
            if (portion.visual != null)
                portion.visual.enabled = true;
        }

        // EN: Reveals collected portions after cancellation and keeps the drawer extended while it holds food.
        // VI: Hiện food đã thu khi hủy lượt và giữ khay kéo ra khi còn food.
        public void CancelCollection()
        {
            foreach (Portion portion in portions) Reveal(portion);
            if (tray != null) tray.SetImmediate(portions.Count > 0);
        }

        // EN: Cancels return reservations and clears the old level's stored portions before loading a new board.
        // VI: Hủy chỗ giữ chuyến trả và xóa food kho của level cũ trước khi nạp board mới.
        public void ResetForLevel()
        {
            CancelPendingReturn();
            StopAllCoroutines();
            returning = null;
            returnSlot = null;
            returnPortion = null;
            foreach (Portion portion in portions)
                if (portion.visual != null) Destroy(portion.visual.gameObject);
            portions.Clear();
            if (tray != null) tray.SetImmediate(false);
        }

        // EN: Supplies a portion to AutoComplete and closes the drawer only after its last portion is removed.
        // VI: Cấp phần food cho AutoComplete và chỉ đóng khay khi phần cuối đã được lấy hết.
        public bool TryTake(FoodType type, out Vector3 position, out Sprite sprite)
        {
            position = default;
            sprite = null;
            Portion portion = portions.Find(item => item.type == type && item != returnPortion);
            if (portion == null)
                return false;
            position = GetPosition(portion);
            sprite = portion.visual.sprite;
            portions.Remove(portion);
            Destroy(portion.visual.gameObject);
            LayoutPortions();
            if (portions.Count == 0 && !IsReturning && IsReady) tray.Retract();
            return true;
        }

        // EN: Cleans up only this return's arrival identity so reused slots retain their replacement food.
        // VI: Chỉ dọn mã đáp của chuyến trả này để slot dùng lại giữ nguyên food thay thế.
        private IEnumerator RunReturn(Portion portion, ConveyorSlot slot, int arrivalId)
        {
            try
            {
                yield return ReturnToBelt(portion, slot, arrivalId);
            }
            finally
            {
                if (slot != null) slot.CancelFoodArrival(arrivalId);
                if (returnSlot == slot && returnArrivalId == arrivalId)
                {
                    if (portions.Contains(portion)) Reveal(portion);
                    returnSlot = null;
                    returnPortion = null;
                    returning = null;
                    if (IsReady)
                    {
                        if (portions.Count > 0) tray.Extend(); else tray.Retract();
                    }
                }
            }
        }

        // EN: Waits for the requested endpoint, retaining the extended endpoint whenever portions remain.
        // VI: Chờ vị trí yêu cầu, giữ vị trí kéo ra mỗi khi vẫn còn food.
        public IEnumerator MoveDrawer(bool open)
        {
            if (!IsReady)
                yield break;
            open = open || portions.Count > 0;
            if (open) tray.Extend(); else tray.Retract();
            var target = open ? FoodHoldingTray.DrawerState.Extended : FoodHoldingTray.DrawerState.Retracted;
            while (IsReady && tray.State != target)
                yield return null;
        }

        // EN: Transfers storage data only when this exact arrival lands; stale flights never finish or clear newer food.
        // VI: Chỉ chuyển dữ liệu kho khi đúng mã đáp này hoàn tất; chuyến cũ không đáp hoặc xóa food mới.
        private IEnumerator ReturnToBelt(Portion portion, ConveyorSlot slot, int arrivalId)
        {
            yield return MoveDrawer(true);
            if (!IsReady)
                yield break;
            SpriteRenderer flyer = CreateFlyer(portion.visual.sprite);
            returnFlyer = flyer;
            portion.visual.enabled = false;
            Vector3 start = GetPosition(portion);
            try
            {
                for (float elapsed = 0f; elapsed < returnDuration; elapsed += Time.deltaTime)
                {
                    if (slot == null || !slot.OwnsFoodArrival(arrivalId))
                        yield break;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / returnDuration);
                    flyer.transform.position = Vector3.Lerp(start, slot.LandingPosition, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.5f;
                    flyer.transform.localScale = Vector3.Lerp(portion.visual.transform.lossyScale, slot.LandingScale, t);
                    yield return null;
                }
                if (slot != null && slot.CompleteFoodArrival(arrivalId))
                {
                    portions.Remove(portion);
                    Destroy(portion.visual.gameObject);
                    LayoutPortions();
                }
            }
            finally
            {
                if (flyer != null) Destroy(flyer.gameObject);
                if (returnFlyer == flyer) returnFlyer = null;
                if (portions.Contains(portion))
                {
                    if (slot != null) slot.CancelFoodArrival(arrivalId);
                    Reveal(portion);
                }
            }
            yield return MoveDrawer(false);
        }

        // EN: Creates an independent flight sprite above the conveyor, outside the tray's clipping group.
        // VI: Tạo sprite bay độc lập phía trên băng chuyền, ngoài nhóm cắt của khay.
        private SpriteRenderer CreateFlyer(Sprite sprite)
        {
            var flyer = new GameObject("Holding return flight").AddComponent<SpriteRenderer>();
            flyer.sprite = sprite;
            var group = tray.GetComponent<SortingGroup>();
            flyer.sortingLayerID = group != null ? group.sortingLayerID : 0;
            flyer.sortingOrder = 600;
            return flyer;
        }

        // EN: Positions stored visuals while compensating parent scale to retain each portion's original world size.
        // VI: Xếp hình food và bù scale cha để giữ kích thước world gốc của từng phần.
        private void LayoutPortions()
        {
            for (int i = 0; i < portions.Count; i++)
            {
                SpriteRenderer visual = portions[i].visual;
                visual.transform.localPosition = new Vector3((i % 3 - 1) * 0.45f, -0.05f + (i / 3 % 2) * 0.22f, 0f);
                Vector3 parentScale = foodRoot.lossyScale;
                Vector3 scale = portions[i].worldScale;
                visual.transform.localScale = new Vector3(
                    scale.x / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
                    scale.y / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)),
                    scale.z / Mathf.Max(0.001f, Mathf.Abs(parentScale.z)));
                visual.sortingOrder = i;
            }
        }

        // EN: Cancels a pending transfer before AutoComplete reads inventory, returning its single portion to storage.
        // VI: Hủy chuyến chuyển đang chờ trước khi AutoComplete đọc kho, trả đúng một phần về khay giữ.
        public void CancelPendingReturn()
        {
            ConveyorSlot slot = returnSlot;
            Portion portion = returnPortion;
            int arrivalId = returnArrivalId;
            if (returning != null) StopCoroutine(returning);
            returning = null;
            if (slot != null) slot.CancelFoodArrival(arrivalId);
            if (portion != null && portions.Contains(portion)) Reveal(portion);
            if (returnFlyer != null) Destroy(returnFlyer.gameObject);
            returnFlyer = null;
            returnSlot = null;
            returnPortion = null;
        }

        // EN: Releases only the pending return's owned reservation and keeps its ingredient available in storage.
        // VI: Chỉ giải phóng phần giữ chỗ của chuyến trả đang chờ, giữ nguyên liệu còn dùng được trong kho.
        private void OnDisable()
        {
            CancelPendingReturn();
        }

#if UNITY_EDITOR
        // EN: Assigns scene references without changing tray placement.
        // VI: Gán tham chiếu scene mà không thay đổi vị trí khay.
        public void Configure(FoodHoldingTray drawerTray, Transform root, ConveyorChefMovement belt, FoodIconConfig icons)
        {
            tray = drawerTray;
            foodRoot = root;
            conveyor = belt;
            iconConfig = icons;
        }
#endif
    }
}
