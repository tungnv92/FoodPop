using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FoodPop
{
    // EN: The always-visible holding cabinet: holds individual conveyor portions in fixed spots that stay hidden until they receive food (one portion per spot; the whole spot, tray and food together, hops when food lands and squashes when food leaves) and returns only ingredients still missing from current orders.
    // VI: Tủ giữ món luôn hiện: giữ từng phần food của băng chuyền trong các ô cố định, ô ẩn cho tới khi nhận food (mỗi ô một phần; cả ô gồm khay và food nảy lên khi food đáp xuống và bẹp nhẹ khi food rời đi) và chỉ trả nguyên liệu order hiện tại còn thiếu.
    public sealed class FoodHoldingStorage : MonoBehaviour
    {
        public sealed class Portion
        {
            public FoodType type;
            public FoodState state;
            public Transform spot;
            public SpriteRenderer visual;
            public Vector3 worldScale;
            public bool rising;
        }

        // EN: Sorting order of stored food inside FoodRoot's sorting group, above each spot's tray sprite.
        // VI: Thứ tự vẽ của food trong nhóm sắp xếp của FoodRoot, nằm trên sprite khay của từng ô.
        private const int FoodSortingOrder = 5;

        // EN: Number of hops a spot makes after food lands on it.
        // VI: Số lần ô nảy lên sau khi food đáp xuống.
        private const int SpotHops = 2;

        public bool IsReady => isActiveAndEnabled && foodRoot != null && spots.Length > 0;

        public bool IsReturning => returning != null;

        public int Count => portions.Count;

        // EN: Number of spots that can still receive food.
        // VI: Số ô còn nhận thêm được food.
        public int FreeSpots => spots.Length - portions.Count;

        [Tooltip("Object chứa các ô food ở giữa lớp sau và lớp trước của tủ.")]
        [SerializeField] private Transform foodRoot;
        [Tooltip("Các ô đặt food theo thứ tự lấp; số ô là số food tối đa Clean thu được. Để trống thì dùng các object con trực tiếp của FoodRoot. Ô tự bật khi có food và tắt khi trống.")]
        [SerializeField] private Transform[] spots = new Transform[0];
        [Tooltip("Băng chuyền nhận food trả từ tủ.")]
        [SerializeField] private ConveyorChefMovement conveyor;
        [Tooltip("Thời gian food bay trở lại đĩa trống, tính bằng giây.")]
        [SerializeField, Min(0.05f)] private float returnDuration = 0.4f;

        [Header("Food trong ô")]
        [Tooltip("Tỉ lệ kích thước food khi nằm trong ô so với kích thước trên băng chuyền.")]
        [SerializeField, Range(0.3f, 1.2f)] private float storedScale = 0.6f;
        [Tooltip("Vị trí food so với tâm ô (tọa độ cục bộ của ô).")]
        [SerializeField] private Vector3 foodOffset = Vector3.zero;
        [Tooltip("Tốc độ food thu về kích thước trong ô (càng lớn càng nhanh).")]
        [SerializeField, Min(1f)] private float settleSpeed = 14f;
        [Tooltip("Thời gian food phóng to tại ô trước khi bay ra, tính bằng giây.")]
        [SerializeField, Min(0f)] private float riseDuration = 0.15f;

        [Header("Nảy ô (khay + food)")]
        [Tooltip("Thời gian nảy của ô khi food đáp vào hoặc rời đi, tính bằng giây.")]
        [SerializeField, Min(0.05f)] private float spotBounceDuration = 0.45f;
        [Tooltip("Độ nhún tối đa của ô (0.1 = bẹp 10%); 0 để tắt bẹp-giãn.")]
        [SerializeField, Range(0f, 0.4f)] private float spotBounceStrength = 0.1f;
        [Tooltip("Độ cao lần nảy đầu của cả ô (khay + food) khi food đáp xuống, tọa độ cục bộ của FoodRoot; các lần sau thấp dần. 0 để chỉ bẹp-giãn tại chỗ.")]
        [SerializeField, Min(0f)] private float spotHopHeight = 0.3f;

        private readonly List<Portion> portions = new List<Portion>();

        private readonly List<FoodType> needs = new List<FoodType>();

        private Coroutine returning;
        private ConveyorSlot returnSlot;
        private Portion returnPortion;
        private int returnArrivalId;
        private SpriteRenderer returnFlyer;
        private Vector3[] spotRestScales = new Vector3[0];
        private Vector3[] spotRestPositions = new Vector3[0];
        private Coroutine[] spotBounces = new Coroutine[0];

        // EN: Fills missing references from this object's own hierarchy, remembers each spot's authored pose and hides every spot until it receives food.
        // VI: Tự gán các tham chiếu còn thiếu từ chính cây object của tủ, ghi nhớ tư thế thiết kế của từng ô và ẩn mọi ô cho tới khi ô nhận food.
        private void Awake()
        {
            ResolveReferences();
            CaptureSpotRest();
            HideAllSpots();
        }

        // EN: Reserves demand returns with a unique slot arrival identity while no booster owns the cabinet; only ingredients a current customer can accept right now leave it (raw ones are re-prepared by the chef on the belt).
        // VI: Giữ chỗ chuyến trả theo nhu cầu bằng mã đáp riêng khi không booster nào đang dùng tủ; chỉ trả nguyên liệu mà khách hiện tại nhận được ngay (món thô sẽ được đầu bếp sơ chế lại trên băng).
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

        // EN: Smoothly moves visible portions onto their tray and scales them to their stored size (or to full size while rising out of their spot).
        // VI: Đưa mượt food đang hiện về vị trí trên khay và thu về kích thước trong ô (hoặc phóng tới kích thước đầy đủ khi đang chuẩn bị bay ra).
        private void LateUpdate()
        {
            if (portions.Count == 0)
                return;
            float blend = 1f - Mathf.Exp(-settleSpeed * Time.deltaTime);
            foreach (Portion portion in portions)
            {
                if (portion.visual == null || !portion.visual.enabled)
                    continue;
                Transform visual = portion.visual.transform;
                visual.localPosition = Vector3.Lerp(visual.localPosition, foodOffset, blend);
                visual.localScale = Vector3.Lerp(visual.localScale, LocalScaleFor(portion, portion.rising ? 1f : storedScale), blend);
            }
        }

        // EN: Releases only the pending return's owned reservation (keeping its ingredient in the cabinet) and puts every spot with captured bounce state back to its rest pose because coroutines stop here (the loop follows the runtime arrays, which a script reload empties while the serialized spots stay filled).
        // VI: Chỉ giải phóng phần giữ chỗ của chuyến trả đang chờ (giữ nguyên liệu trong tủ) và đưa mọi ô đã có trạng thái nảy về tư thế nghỉ vì coroutine dừng tại đây (vòng lặp theo mảng runtime, vốn bị làm rỗng khi tải lại script trong khi mảng spots serialize vẫn còn).
        private void OnDisable()
        {
            CancelPendingReturn();
            for (int i = 0; i < spotBounces.Length; i++)
                StopSpotBounce(i);
        }

        // EN: Registers a hidden portion in the first free spot with its original conveyor world scale; the spot (still hidden) and the food appear when DropIn or Reveal is called. Returns null when every spot is taken. Uses the shared ingredient configuration for the fallback icon.
        // VI: Ghi nhận một phần food (đang ẩn) vào ô trống đầu tiên cùng scale world gốc trên băng; ô (vẫn đang ẩn) và food hiện ra khi gọi DropIn hoặc Reveal. Trả về null khi mọi ô đã có food. Dùng cấu hình nguyên liệu chung cho icon dự phòng.
        public Portion Store(FoodType type, FoodState state, Sprite sprite, Vector3 worldScale)
        {
            Transform spot = FindFreeSpot();
            if (spot == null)
                return null;
            var visual = new GameObject("Stored food · " + type).AddComponent<SpriteRenderer>();
            visual.transform.SetParent(spot, false);
            visual.sprite = sprite != null ? sprite : IngameConfig.Instance.FoodIcons != null ? IngameConfig.Instance.FoodIcons.GetIcon(type, state) : null;
            visual.sortingOrder = FoodSortingOrder;
            visual.enabled = false;
            var portion = new Portion { type = type, state = state, spot = spot, visual = visual, worldScale = worldScale };
            portions.Add(portion);
            SnapToSpot(portion);
            return portion;
        }

        // EN: Returns a stored portion's current world position in its spot.
        // VI: Trả về vị trí world hiện tại của food trong ô của nó.
        public Vector3 GetPosition(Portion portion)
        {
            return portion.visual != null ? portion.visual.transform.position : portion.spot.position;
        }

        // EN: Turns the spot on as the food lands, shows the food at full size on its tray (it shrinks to its stored size) and makes the whole spot hop.
        // VI: Bật ô lên khi food đáp xuống, hiện food ở kích thước đầy đủ trên khay (rồi thu về kích thước trong ô) và cho cả ô nảy lên.
        public void DropIn(Portion portion)
        {
            if (portion == null || portion.visual == null)
                return;
            portion.spot.gameObject.SetActive(true);
            Transform visual = portion.visual.transform;
            visual.localPosition = foodOffset;
            visual.localScale = LocalScaleFor(portion, 1f);
            portion.rising = false;
            portion.visual.enabled = true;
            BounceSpot(portion.spot, true);
        }

        // EN: Turns the spot on and shows a portion directly on its tray without bouncing (used when a collection or return is cancelled).
        // VI: Bật ô và hiện food ngay trên khay, không nảy (dùng khi lượt thu hoặc chuyến trả bị hủy).
        public void Reveal(Portion portion)
        {
            if (portion == null || portion.visual == null)
                return;
            portion.spot.gameObject.SetActive(true);
            portion.rising = false;
            if (!portion.visual.enabled)
                SnapToSpot(portion);
            portion.visual.enabled = true;
        }

        // EN: Reveals every collected portion after a cancelled collection.
        // VI: Hiện mọi food đã thu sau khi lượt thu bị hủy.
        public void CancelCollection()
        {
            foreach (Portion portion in portions) Reveal(portion);
        }

        // EN: Cancels return reservations and clears the old level's stored portions and spots before loading a new board.
        // VI: Hủy chỗ giữ chuyến trả, xóa food và tắt các ô của level cũ trước khi nạp board mới.
        public void ResetForLevel()
        {
            CancelPendingReturn();
            StopAllCoroutines();
            returning = null;
            returnSlot = null;
            returnPortion = null;
            foreach (Portion portion in portions.ToArray())
                RemovePortion(portion);
        }

        // EN: Supplies a portion to AutoComplete; the flight starts from the portion's spot, which turns off.
        // VI: Cấp phần food cho AutoComplete; chuyến bay bắt đầu từ ô của food (ô đó tắt đi).
        public bool TryTake(FoodType type, out Vector3 position, out Sprite sprite)
        {
            position = default;
            sprite = null;
            Portion portion = portions.Find(item => item.type == type && item != returnPortion);
            if (portion == null)
                return false;
            position = GetPosition(portion);
            sprite = portion.visual.sprite;
            RemovePortion(portion);
            return true;
        }

        // EN: Cancels a pending transfer before AutoComplete reads inventory, returning its single portion to its spot.
        // VI: Hủy chuyến chuyển đang chờ trước khi AutoComplete đọc kho, trả đúng một phần về ô của nó.
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

#if UNITY_EDITOR
        // EN: Assigns scene references without changing cabinet placement.
        // VI: Gán tham chiếu scene mà không thay đổi vị trí tủ.
        public void Configure(Transform root, ConveyorChefMovement belt)
        {
            foodRoot = root;
            conveyor = belt;
        }
#endif

        // EN: Uses the child named "FoodRoot" and FoodRoot's direct children as spots when they are not assigned; warns once when FoodRoot is still missing.
        // VI: Dùng object con tên "FoodRoot" và các object con trực tiếp của FoodRoot làm ô khi chưa được gán; cảnh báo nếu vẫn thiếu FoodRoot.
        private void ResolveReferences()
        {
            if (foodRoot == null)
                foodRoot = FindChild(transform, "FoodRoot");
            if (foodRoot == null)
            {
                Debug.LogWarning("[FoodHoldingStorage] Không tìm thấy FoodRoot trong tủ giữ món nên Clean không dùng được. Hãy gán foodRoot hoặc thêm object con tên FoodRoot.", this);
                return;
            }
            if (spots.Length == 0)
            {
                spots = new Transform[foodRoot.childCount];
                for (int i = 0; i < spots.Length; i++)
                    spots[i] = foodRoot.GetChild(i);
            }
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

        // EN: Remembers each spot's authored scale and position so bounces always settle back to them.
        // VI: Ghi nhớ scale và vị trí thiết kế của từng ô để nhịp nảy luôn trở về đúng tư thế đó.
        private void CaptureSpotRest()
        {
            spotRestScales = new Vector3[spots.Length];
            spotRestPositions = new Vector3[spots.Length];
            spotBounces = new Coroutine[spots.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                spotRestScales[i] = spots[i] != null ? spots[i].localScale : Vector3.one;
                spotRestPositions[i] = spots[i] != null ? spots[i].localPosition : Vector3.zero;
            }
        }

        // EN: Turns every spot off so no empty tray shows before food arrives, even when the scene left spots on for layout work.
        // VI: Tắt mọi ô để không có khay trống nào hiện trước khi food tới, kể cả khi scene đang để ô bật để chỉnh bố cục.
        private void HideAllSpots()
        {
            foreach (Transform spot in spots)
                if (spot != null) spot.gameObject.SetActive(false);
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

        // EN: Grows the portion to full size in its spot, bounces the spot as the food leaves, then flies it to the reserved slot; storage data moves (and the spot turns off) only when this exact arrival lands, so stale flights never finish or clear newer food.
        // VI: Phóng food tới kích thước đầy đủ tại ô, nảy ô khi food rời đi, rồi bay tới slot đã giữ chỗ; dữ liệu kho chỉ chuyển (và ô tắt đi) khi đúng mã đáp này hoàn tất, nên chuyến cũ không đáp hoặc xóa food mới.
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
                Vector3 start = portion.visual.transform.position;
                portion.visual.enabled = false;
                BounceSpot(portion.spot, false);
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
                    RemovePortion(portion);
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

        // EN: Creates an independent flight sprite above the conveyor, outside the cabinet's sorting group.
        // VI: Tạo sprite bay độc lập phía trên băng chuyền, ngoài nhóm sắp xếp của tủ.
        private SpriteRenderer CreateFlyer(Sprite sprite)
        {
            var flyer = new GameObject("Holding return flight").AddComponent<SpriteRenderer>();
            flyer.sprite = sprite;
            var group = GetComponent<SortingGroup>();
            flyer.sortingLayerID = group != null ? group.sortingLayerID : 0;
            flyer.sortingOrder = 600;
            return flyer;
        }

        // EN: Restarts a bounce on one whole spot (tray and food together); hop makes it jump when food lands, otherwise it only squashes in place. The cabinet itself stays still.
        // VI: Khởi động lại nhịp nảy trên cả một ô (khay và food cùng lúc); hop cho ô bật lên khi food đáp, ngược lại chỉ bẹp-giãn tại chỗ. Bản thân tủ đứng yên.
        private void BounceSpot(Transform spot, bool hop)
        {
            int index = System.Array.IndexOf(spots, spot);
            if (index < 0 || !isActiveAndEnabled || (spotBounceStrength <= 0f && (!hop || spotHopHeight <= 0f)))
                return;
            StopSpotBounce(index);
            spotBounces[index] = StartCoroutine(PlaySpotBounce(index, hop));
        }

        // EN: Stops a spot's bounce and puts the spot back to its authored scale and position.
        // VI: Dừng nhịp nảy của một ô và đưa ô về scale và vị trí thiết kế.
        private void StopSpotBounce(int index)
        {
            if (spotBounces[index] != null)
                StopCoroutine(spotBounces[index]);
            spotBounces[index] = null;
            if (spots[index] == null)
                return;
            spots[index].localScale = spotRestScales[index];
            spots[index].localPosition = spotRestPositions[index];
        }

        // EN: Damped bounce on one spot. With hop: SpotHops jumps that get lower, squashing (lower and wider) each time it touches down. Without hop: squashes, stretches slightly, then settles. Always ends at the rest pose.
        // VI: Nhịp nảy tắt dần trên một ô. Có hop: SpotHops lần bật lên thấp dần, mỗi lần chạm xuống thì bẹp (thấp và rộng hơn). Không hop: bẹp, giãn nhẹ rồi đứng yên. Luôn kết thúc ở tư thế nghỉ.
        private IEnumerator PlaySpotBounce(int index, bool hop)
        {
            Transform spot = spots[index];
            Vector3 restScale = spotRestScales[index];
            Vector3 restPosition = spotRestPositions[index];
            for (float elapsed = 0f; elapsed < spotBounceDuration; elapsed += Time.deltaTime)
            {
                float t = elapsed / spotBounceDuration;
                float decay = 1f - t;
                float height = 0f;
                float squash;
                if (hop)
                {
                    float arc = Mathf.Abs(Mathf.Sin(t * Mathf.PI * SpotHops));
                    height = spotHopHeight * arc * decay;
                    squash = spotBounceStrength * decay * Mathf.Pow(1f - arc, 4f);
                }
                else
                    squash = spotBounceStrength * Mathf.Sin(t * Mathf.PI * 2f) * decay;
                spot.localPosition = restPosition + Vector3.up * height;
                spot.localScale = new Vector3(restScale.x * (1f + squash * 0.6f), restScale.y * (1f - squash), restScale.z);
                yield return null;
            }
            spot.localScale = restScale;
            spot.localPosition = restPosition;
            spotBounces[index] = null;
        }

        // EN: Removes a portion from storage, destroys its sprite, stops its spot's bounce and turns the spot off.
        // VI: Xóa food khỏi kho, hủy sprite của nó, dừng nảy và tắt ô chứa nó.
        private void RemovePortion(Portion portion)
        {
            portions.Remove(portion);
            if (portion.visual != null) Destroy(portion.visual.gameObject);
            int index = System.Array.IndexOf(spots, portion.spot);
            if (index >= 0) StopSpotBounce(index);
            if (portion.spot != null) portion.spot.gameObject.SetActive(false);
        }

        // EN: Local scale that gives the portion its original world size times the factor, compensating its spot's authored scale (so spot bounces still show on the food).
        // VI: Scale cục bộ để food có kích thước world gốc nhân hệ số, bù theo scale thiết kế của ô (để nhịp nảy của ô vẫn thấy trên food).
        private Vector3 LocalScaleFor(Portion portion, float factor)
        {
            Transform spot = portion.spot;
            int index = System.Array.IndexOf(spots, spot);
            Vector3 spotScale = index >= 0 ? spotRestScales[index] : spot.localScale;
            Vector3 parentScale = Vector3.Scale(spot.parent != null ? spot.parent.lossyScale : Vector3.one, spotScale);
            Vector3 scale = portion.worldScale * factor;
            return new Vector3(
                scale.x / Mathf.Max(0.001f, Mathf.Abs(parentScale.x)),
                scale.y / Mathf.Max(0.001f, Mathf.Abs(parentScale.y)),
                scale.z / Mathf.Max(0.001f, Mathf.Abs(parentScale.z)));
        }

        // EN: First spot (in order) not used by any stored portion.
        // VI: Ô đầu tiên (theo thứ tự) chưa có food nào chiếm.
        private Transform FindFreeSpot()
        {
            foreach (Transform spot in spots)
                if (spot != null && !portions.Exists(portion => portion.spot == spot))
                    return spot;
            return null;
        }

        // EN: Places a portion exactly on its spot at its stored size.
        // VI: Đặt food đúng vào ô của nó với kích thước trong ô.
        private void SnapToSpot(Portion portion)
        {
            Transform visual = portion.visual.transform;
            visual.localPosition = foodOffset;
            visual.localRotation = Quaternion.identity;
            visual.localScale = LocalScaleFor(portion, storedScale);
        }

    }
}
