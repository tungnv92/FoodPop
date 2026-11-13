using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FoodPop
{
    // EN: Holds individual conveyor portions inside the holding bag and returns only ingredients still missing from current orders.
    // VI: Giữ từng phần food của băng chuyền trong túi giữ món và chỉ trả nguyên liệu order hiện tại còn thiếu.
    public sealed class FoodHoldingStorage : MonoBehaviour
    {
        public sealed class Portion
        {
            public FoodType type;
            public FoodState state;
            public SpriteRenderer visual;
            public Vector3 worldScale;
            public Vector3 targetPosition;
            public float targetAngle;
            public bool rising;
        }

        [Tooltip("Túi giữ món (lớp sau, food, lớp trước) và hiệu ứng nảy nhún.")]
        [SerializeField] private FoodHoldingTray tray;
        [Tooltip("Object chứa hình food ở giữa lớp sau và lớp trước của túi.")]
        [SerializeField] private Transform foodRoot;
        [Tooltip("Băng chuyền nhận food trả từ túi.")]
        [SerializeField] private ConveyorChefMovement conveyor;
        [Tooltip("Thời gian food bay trở lại đĩa trống, tính bằng giây.")]
        [SerializeField, Min(0.05f)] private float returnDuration = 0.4f;

        [Header("Xếp food trong túi")]
        [Tooltip("Tỉ lệ kích thước food khi nằm trong túi so với kích thước trên băng chuyền.")]
        [SerializeField, Range(0.3f, 1.2f)] private float storedScale = 0.6f;
        [Tooltip("Số food trên một hàng trong túi.")]
        [SerializeField, Min(1)] private int perRow = 3;
        [Tooltip("Khoảng cách ngang giữa hai food cùng hàng (tọa độ cục bộ của FoodRoot).")]
        [SerializeField] private float columnSpacing = 0.36f;
        [Tooltip("Độ cao hàng đầu tiên (tọa độ cục bộ của FoodRoot); các hàng sau xếp chồng cao dần.")]
        [SerializeField] private float firstRowY = 0.2f;
        [Tooltip("Khoảng cách dọc giữa hai hàng food.")]
        [SerializeField] private float rowSpacing = 0.12f;
        [Tooltip("Số hàng tối đa; food nhiều hơn sẽ chồng lên hàng trên cùng.")]
        [SerializeField, Min(1)] private int maxRows = 3;
        [Tooltip("Góc nghiêng tối đa của food trong túi (độ).")]
        [SerializeField, Range(0f, 30f)] private float tiltAngle = 10f;
        [Tooltip("Tốc độ food trượt vào chỗ trong túi (càng lớn càng nhanh).")]
        [SerializeField, Min(1f)] private float settleSpeed = 14f;
        [Tooltip("Thời gian food nhô lên miệng túi trước khi bay ra, tính bằng giây.")]
        [SerializeField, Min(0f)] private float riseDuration = 0.15f;

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

        // EN: World position of the bag mouth, where collected food enters and returned food leaves.
        // VI: Vị trí world của miệng túi, nơi food thu vào đi vào và food trả về đi ra.
        public Vector3 MouthPosition => tray != null ? tray.MouthPosition : foodRoot != null ? foodRoot.position : transform.position;

        // EN: Fills missing references from this object's own hierarchy.
        // VI: Tự gán các tham chiếu còn thiếu từ chính cây object của túi.
        private void Awake()
        {
            ResolveReferences();
        }

        // EN: Uses the FoodHoldingTray on this object and the child named "FoodRoot" when they are not assigned; warns once when FoodRoot is still missing.
        // VI: Dùng FoodHoldingTray trên object này và object con tên "FoodRoot" khi chưa được gán; cảnh báo nếu vẫn thiếu FoodRoot.
        private void ResolveReferences()
        {
            if (tray == null)
                tray = GetComponent<FoodHoldingTray>();
            if (foodRoot == null)
                foodRoot = FindChild(transform, "FoodRoot");
            if (foodRoot == null)
                Debug.LogWarning("[FoodHoldingStorage] Không tìm thấy FoodRoot trong túi giữ món nên Clean không dùng được. Hãy gán foodRoot hoặc thêm object con tên FoodRoot.", this);
        }

        // EN: Depth-first search for a descendant with the given name.
        // VI: Tìm theo chiều sâu object con cháu có tên đã cho.
        private static Transform FindChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;
                Transform found = FindChild(child, childName);
                if (found != null)
                    return found;
            }
            return null;
        }

        // EN: Reserves demand returns with a unique slot arrival identity while no booster owns the bag; only ingredients a current customer can accept right now leave the bag (raw ones are re-prepared by the chef on the belt).
        // VI: Giữ chỗ chuyến trả theo nhu cầu bằng mã đáp riêng khi không booster nào đang dùng túi; chỉ trả nguyên liệu mà khách hiện tại nhận được ngay (món thô sẽ được đầu bếp sơ chế lại trên băng).
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
                if (portion == null || !customers.TryFindCustomer(need))
                    continue;
                ConveyorSlot slot = FindEmptySlot();
                if (slot == null || !slot.TryFillFood(portion.type))
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

        // EN: Registers a hidden portion with its original conveyor world scale; it appears when DropIn or Reveal is called. Uses the shared ingredient configuration for the fallback icon.
        // VI: Ghi nhận một phần food (đang ẩn) cùng scale world gốc trên băng; food hiện ra khi gọi DropIn hoặc Reveal. Dùng cấu hình nguyên liệu chung cho icon dự phòng.
        public Portion Store(FoodType type, FoodState state, Sprite sprite, Vector3 worldScale)
        {
            var visual = new GameObject("Stored food · " + type).AddComponent<SpriteRenderer>();
            visual.transform.SetParent(foodRoot, false);
            visual.sprite = sprite != null ? sprite : IngameConfig.Instance.FoodIcons != null ? IngameConfig.Instance.FoodIcons.GetIcon(type, state) : null;
            visual.enabled = false;
            var portion = new Portion { type = type, state = state, visual = visual, worldScale = worldScale };
            portions.Add(portion);
            LayoutPortions();
            SnapToTarget(portion);
            return portion;
        }

        // EN: Returns a stored portion's current world position inside the bag.
        // VI: Trả về vị trí world hiện tại của food đang nằm trong túi.
        public Vector3 GetPosition(Portion portion)
        {
            return portion.visual != null ? portion.visual.transform.position : foodRoot.position;
        }

        // EN: Shows a portion at full size at the bag mouth, lets it sink into its place behind the front layer, and bounces the bag.
        // VI: Hiện food ở kích thước đầy đủ tại miệng túi, cho food chìm xuống chỗ của nó phía sau lớp trước, rồi nảy túi.
        public void DropIn(Portion portion)
        {
            if (portion == null || portion.visual == null || foodRoot == null)
                return;
            Transform visual = portion.visual.transform;
            Vector3 mouth = MouthPosition;
            visual.position = new Vector3(mouth.x, mouth.y, foodRoot.position.z);
            visual.localRotation = Quaternion.identity;
            visual.localScale = LocalScaleFor(portion, 1f);
            portion.rising = false;
            portion.visual.enabled = true;
            if (tray != null) tray.Bounce();
        }

        // EN: Shows a portion directly in its place (used when a collection or return is cancelled).
        // VI: Hiện food ngay tại chỗ của nó trong túi (dùng khi lượt thu hoặc chuyến trả bị hủy).
        public void Reveal(Portion portion)
        {
            if (portion == null || portion.visual == null)
                return;
            portion.rising = false;
            if (!portion.visual.enabled)
                SnapToTarget(portion);
            portion.visual.enabled = true;
        }

        // EN: Reveals every collected portion after a cancelled collection and hides the bag when nothing was collected.
        // VI: Hiện mọi food đã thu sau khi lượt thu bị hủy và ẩn túi nếu chưa thu được gì.
        public void CancelCollection()
        {
            foreach (Portion portion in portions) Reveal(portion);
            HideIfEmpty();
        }

        // EN: Pops the bag in before food is collected into it.
        // VI: Cho túi bật hiện trước khi thu food vào.
        public void ShowBag()
        {
            if (tray != null) tray.Show();
        }

        // EN: Hides the bag once it holds no food.
        // VI: Ẩn túi khi trong túi không còn food.
        public void HideIfEmpty()
        {
            if (portions.Count == 0 && tray != null) tray.Hide();
        }

        // EN: Cancels return reservations and clears the old level's stored portions before loading a new board.
        // VI: Hủy chỗ giữ chuyến trả và xóa food trong túi của level cũ trước khi nạp board mới.
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
            if (tray != null) tray.HideImmediate();
        }

        // EN: Supplies a portion to AutoComplete; the flight starts from the bag mouth, the bag bounces and hides when it becomes empty.
        // VI: Cấp phần food cho AutoComplete; chuyến bay bắt đầu từ miệng túi, túi nảy lên và ẩn đi khi đã trống.
        public bool TryTake(FoodType type, out Vector3 position, out Sprite sprite)
        {
            position = default;
            sprite = null;
            Portion portion = portions.Find(item => item.type == type && item != returnPortion);
            if (portion == null)
                return false;
            position = MouthPosition;
            sprite = portion.visual.sprite;
            portions.Remove(portion);
            Destroy(portion.visual.gameObject);
            LayoutPortions();
            if (tray != null) tray.Bounce();
            HideIfEmpty();
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
                }
            }
        }

        // EN: Raises the portion to the bag mouth, then flies it to the reserved slot; storage data moves only when this exact arrival lands, so stale flights never finish or clear newer food. The bag hides after its last portion leaves.
        // VI: Nâng food lên miệng túi rồi bay tới slot đã giữ chỗ; dữ liệu kho chỉ chuyển khi đúng mã đáp này hoàn tất, nên chuyến cũ không đáp hoặc xóa food mới. Túi ẩn đi sau khi phần cuối cùng rời túi.
        private IEnumerator ReturnToBelt(Portion portion, ConveyorSlot slot, int arrivalId)
        {
            if (!IsReady || portion.visual == null)
                yield break;
            SpriteRenderer flyer = null;
            try
            {
                Reveal(portion);
                portion.rising = true;
                for (float elapsed = 0f; elapsed < riseDuration; elapsed += Time.deltaTime)
                {
                    if (slot == null || !slot.OwnsFoodArrival(arrivalId))
                        yield break;
                    yield return null;
                }
                portion.rising = false;
                flyer = CreateFlyer(portion.visual.sprite);
                returnFlyer = flyer;
                Vector3 startScale = portion.visual.transform.lossyScale;
                portion.visual.enabled = false;
                if (tray != null) tray.Bounce();
                Vector3 start = MouthPosition;
                for (float elapsed = 0f; elapsed < returnDuration; elapsed += Time.deltaTime)
                {
                    if (slot == null || !slot.OwnsFoodArrival(arrivalId))
                        yield break;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / returnDuration);
                    flyer.transform.position = Vector3.Lerp(start, slot.LandingPosition, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 0.5f;
                    flyer.transform.localScale = Vector3.Lerp(startScale, slot.LandingScale, t);
                    yield return null;
                }
                if (slot != null && slot.CompleteFoodArrival(arrivalId))
                {
                    portions.Remove(portion);
                    Destroy(portion.visual.gameObject);
                    LayoutPortions();
                    HideIfEmpty();
                }
            }
            finally
            {
                portion.rising = false;
                if (flyer != null) Destroy(flyer.gameObject);
                if (returnFlyer == flyer) returnFlyer = null;
                if (portions.Contains(portion))
                {
                    if (slot != null) slot.CancelFoodArrival(arrivalId);
                    Reveal(portion);
                }
            }
        }

        // EN: Creates an independent flight sprite above the conveyor, outside the bag's sorting group.
        // VI: Tạo sprite bay độc lập phía trên băng chuyền, ngoài nhóm sắp xếp của túi.
        private SpriteRenderer CreateFlyer(Sprite sprite)
        {
            var flyer = new GameObject("Holding return flight").AddComponent<SpriteRenderer>();
            flyer.sprite = sprite;
            var group = tray.GetComponent<SortingGroup>();
            flyer.sortingLayerID = group != null ? group.sortingLayerID : 0;
            flyer.sortingOrder = 600;
            return flyer;
        }

        // EN: Computes each portion's place in the bag: rows fill from the bottom up (older food lower, newer food peeking out of the mouth), odd rows offset by half a column, each portion with a small fixed tilt.
        // VI: Tính chỗ của từng food trong túi: các hàng xếp từ dưới lên (food cũ nằm dưới, food mới nhô lên miệng túi), hàng lẻ lệch nửa cột, mỗi food nghiêng nhẹ một góc cố định.
        private void LayoutPortions()
        {
            int columns = Mathf.Max(1, perRow);
            float halfWidth = columnSpacing * (columns - 1) * 0.5f;
            for (int i = 0; i < portions.Count; i++)
            {
                Portion portion = portions[i];
                int row = i / columns;
                float x = (i % columns) * columnSpacing - halfWidth;
                if (row % 2 == 1 && columns > 1)
                {
                    x += columnSpacing * 0.5f;
                    if (x > halfWidth + 0.001f) x = -halfWidth + columnSpacing * 0.25f;
                }
                float y = firstRowY + Mathf.Min(row, Mathf.Max(1, maxRows) - 1) * rowSpacing;
                portion.targetPosition = new Vector3(x, y, 0f);
                portion.targetAngle = ((i * 37) % 21 - 10) / 10f * tiltAngle;
                if (portion.visual != null) portion.visual.sortingOrder = i;
            }
        }

        // EN: Smoothly moves visible portions toward their place (or toward the mouth while rising out).
        // VI: Di chuyển mượt food đang hiện tới chỗ của nó (hoặc tới miệng túi khi đang nhô lên để ra ngoài).
        private void LateUpdate()
        {
            if (foodRoot == null || portions.Count == 0)
                return;
            float blend = 1f - Mathf.Exp(-settleSpeed * Time.deltaTime);
            Vector3 mouth = foodRoot.InverseTransformPoint(MouthPosition);
            foreach (Portion portion in portions)
            {
                if (portion.visual == null || !portion.visual.enabled)
                    continue;
                Transform visual = portion.visual.transform;
                Vector3 target = portion.rising ? new Vector3(mouth.x, mouth.y, 0f) : portion.targetPosition;
                visual.localPosition = Vector3.Lerp(visual.localPosition, target, blend);
                visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.Euler(0f, 0f, portion.rising ? 0f : portion.targetAngle), blend);
                visual.localScale = Vector3.Lerp(visual.localScale, LocalScaleFor(portion, portion.rising ? 1f : storedScale), blend);
            }
        }

        // EN: Places a portion exactly at its layout target.
        // VI: Đặt food đúng vào vị trí xếp của nó.
        private void SnapToTarget(Portion portion)
        {
            Transform visual = portion.visual.transform;
            visual.localPosition = portion.targetPosition;
            visual.localRotation = Quaternion.Euler(0f, 0f, portion.targetAngle);
            visual.localScale = LocalScaleFor(portion, storedScale);
        }

        // EN: Local scale that gives the portion its original world size times the factor, compensating FoodRoot's scale.
        // VI: Scale cục bộ để food có kích thước world gốc nhân hệ số, đã bù scale của FoodRoot.
        private Vector3 LocalScaleFor(Portion portion, float factor)
        {
            Vector3 parentScale = foodRoot.lossyScale;
            Vector3 scale = portion.worldScale * factor;
            return new Vector3(
                scale.x / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
                scale.y / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)),
                scale.z / Mathf.Max(0.001f, Mathf.Abs(parentScale.z)));
        }

        // EN: Cancels a pending transfer before AutoComplete reads inventory, returning its single portion to the bag.
        // VI: Hủy chuyến chuyển đang chờ trước khi AutoComplete đọc kho, trả đúng một phần về túi giữ món.
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

        // EN: Releases only the pending return's owned reservation and keeps its ingredient available in the bag.
        // VI: Chỉ giải phóng phần giữ chỗ của chuyến trả đang chờ, giữ nguyên liệu còn dùng được trong túi.
        private void OnDisable()
        {
            CancelPendingReturn();
        }

#if UNITY_EDITOR
        // EN: Assigns scene references without changing bag placement.
        // VI: Gán tham chiếu scene mà không thay đổi vị trí túi.
        public void Configure(FoodHoldingTray bag, Transform root, ConveyorChefMovement belt)
        {
            tray = bag;
            foodRoot = root;
            conveyor = belt;
        }
#endif
    }
}
