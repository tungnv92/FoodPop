using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Coordinates chef preparation jobs and conveyor slot availability through one scene singleton.
    // VI: Điều phối công việc sơ chế và slot băng chuyền qua một singleton trong scene.
    [DisallowMultipleComponent]
    public partial class ChefManager : MonoBehaviour
    {
        private const float PreparedPoofDuration = 0.22f;

        private static readonly ConveyorSlot[] EmptySlots = new ConveyorSlot[0];

        // EN: Returns the scene singleton, resolving it once if lifecycle registration has not run yet.
        // VI: Trả về singleton scene, tìm một lần nếu vòng đời chưa đăng ký instance.
        public static ChefManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<ChefManager>();
                return instance;
            }
        }

        // EN: Exposes the movement component's authoritative slot list without duplicating slot storage.
        // VI: Cung cấp danh sách slot gốc của component chuyển động, không lưu danh sách trùng lặp.
        public IReadOnlyList<ConveyorSlot> Slots => conveyorMovement != null ? conveyorMovement.Slots : EmptySlots;

        // EN: Indicates an unfinished belt preparation or a reserved direct-to-order job.
        // VI: Cho biết đầu bếp đang sơ chế món trên băng hoặc giữ lượt đi thẳng tới order.
        public bool IsBusy => preparationRoutine != null || directServe != null;

        public bool BoosterPaused { get; set; }

        // EN: The chef figure's transform (the AutoComplete star appears above it).
        // VI: Transform của nhân vật đầu bếp (ngôi sao AutoComplete hiện phía trên).
        public Transform ChefTransform => ChefAnims != null ? ChefAnims.transform : transform;


        [Tooltip("Anims đầu bếp.")]
        [SerializeField] private ChefAnims ChefAnims;
        [Tooltip("Component điều khiển đường đi và chuyển động các đĩa trên băng chuyền.")]
        [SerializeField] private ConveyorChefMovement conveyorMovement;

        [Tooltip("Bật để đầu bếp tự sơ chế món thô khi món đi vào trạm sơ chế trên băng chuyền.")]
        [SerializeField] private bool autoPrepareInStation = true;

        [Header("Bàn cắt trước mặt đầu bếp")]
        [Tooltip("Điểm món bay tới để đầu bếp cắt (thường là mặt thớt); để trống sẽ dùng vị trí đầu bếp cộng Cutting Offset.")]
        [SerializeField] private Transform cuttingPoint;
        [Tooltip("Thớt trước mặt đầu bếp (tùy chọn); rung nhẹ theo mỗi nhát chặt.")]
        [SerializeField] private Transform cuttingBoard;

        [Tooltip("Độ lệch (world) từ vị trí đầu bếp tới điểm cắt khi chưa gán Cutting Point.")]
        [SerializeField] private Vector3 cuttingOffset = new Vector3(0f, -0.6f, 0f);

        [Header("Hiệu ứng khi sơ chế xong")]
        [Tooltip("Prefab particle (tùy chọn) chạy một lần tại món trước mặt đầu bếp ngay khi sơ chế xong (lúc hiện ảnh đã sơ chế).")]
        [SerializeField] private ParticleEffect preparedEffectPrefab;

        private static ChefManager instance;
        private Coroutine preparationRoutine;
        private Food preparingFood;
        private ConveyorSlot preparingSlot;
        private int preparingArrivalId;
        private float preparationElapsed;
        private float preparationTotal = 1f;
        private readonly Dictionary<ConveyorSlot, float> slotPriorities = new Dictionary<ConveyorSlot, float>();
        private readonly List<FoodType> neededIngredients = new List<FoodType>();
        private SpriteRenderer cuttingFood;
        private SpriteRenderer cuttingShadow;
        private Vector3 boardBaseScale = Vector3.one;
        private readonly Dictionary<ParticleEffect, ParticleEffect> debrisInstances = new Dictionary<ParticleEffect, ParticleEffect>();

        // EN: Registers this manager (removing only duplicate components) and remembers the cutting board's rest scale for the chop punch.
        // VI: Đăng ký manager (chỉ xóa component trùng) và ghi nhớ scale gốc của thớt cho hiệu ứng nảy khi chặt.
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[ChefManager] Scene đã có manager; bỏ component trùng.", this);
                enabled = false;
                Destroy(this);
                return;
            }
            instance = this;
            if (cuttingBoard != null)
                boardBaseScale = cuttingBoard.localScale;
        }

        // EN: Starts a station job only while idle, keeping its source plate reserved.
        // VI: Bắt đầu sơ chế khi rảnh, giữ chỗ trên đĩa nguồn.
        private void Update()
        {
            if (BoosterPaused || !autoPrepareInStation || IsBusy || conveyorMovement == null)
                return;
            ConveyorSlot slot = FindSlotToPrepare();
            if (slot != null)
                TryPrepareSlot(slot);
        }

        // EN: Shows the cutting point in the Scene view when the manager is selected.
        // VI: Hiển thị điểm cắt trong Scene view khi chọn manager.
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.55f, 0.1f, 1f);
            Gizmos.DrawWireSphere(GetCuttingPosition(), 0.3f);
        }

        // EN: Cancels incomplete work when disabled so it cannot prepare food later unexpectedly.
        // VI: Hủy việc chưa hoàn thành khi tắt để tránh sơ chế nguyên liệu muộn ngoài ý muốn.
        private void OnDisable()
        {
            if (instance == this)
                CancelPreparation();
        }

        // EN: Releases this manager's singleton registration and its runtime objects when its scene is destroyed.
        // VI: Giải phóng đăng ký singleton và các object tạo lúc chạy của manager khi scene bị hủy.
        private void OnDestroy()
        {
            OnDestroyRuntimeObjects();
            if (instance == this)
                instance = null;
        }

        // EN: Takes matching belt food or the chef portion for a booster, cancelling its reservation before clearing the original plate.
        // VI: Lấy món khớp trên băng hoặc tại đầu bếp cho booster, hủy sơ chế rồi xóa món khỏi đĩa gốc.
        public bool TryTakeFood(FoodType type, out Vector3 position, out Sprite sprite)
        {
            position = default;
            sprite = null;
            ConveyorSlot slot = FindFoodSlot(type, s => !s.IsFoodArriving);
            if (slot == null && preparingFood != null && preparingFood.foodType == type)
            {
                position = cuttingFood.transform.position;
                sprite = cuttingFood.sprite;
                ConveyorSlot source = preparingSlot;
                CancelPreparation();
                if (source != null) source.ClearFood();
                return true;
            }
            slot = slot ?? FindFoodSlot(type, s => true);
            if (slot == null)
                return false;
            position = slot.LandingPosition;
            sprite = slot.IngredientRenderer != null ? slot.IngredientRenderer.sprite : null;
            if (slot == preparingSlot)
                CancelPreparation();
            slot.ClearFood();
            return true;
        }

        // EN: Captures a belt portion before clearing it, cancelling chef return when it owns this destination.
        // VI: Ghi nhận phần trên băng trước khi xóa, hủy lượt trả của đầu bếp nếu đích thuộc lượt này.
        public bool TryTakeSlot(ConveyorSlot slot, out FoodType type, out Sprite sprite)
        {
            type = FoodType.None;
            sprite = null;
            if (slot == null || slot.isEmpty || slot.food == null)
                return false;
            type = slot.food.foodType;
            sprite = slot.IngredientRenderer != null ? slot.IngredientRenderer.sprite : null;
            if (slot == preparingSlot)
                CancelPreparation();
            slot.ClearFood();
            return type != FoodType.None;
        }

        // EN: Includes food currently at the chef when checking booster availability.
        // VI: Tính cả món hiện tại đầu bếp khi kiểm tra booster có thể lấy món hay không.
        public bool HasFoodOnBelt()
        {
            if (preparingFood != null)
                return true;
            IReadOnlyList<ConveyorSlot> slots = Slots;
            for (int i = 0; i < slots.Count; i++)
                if (slots[i] != null && slots[i].isActiveAndEnabled && !slots[i].isEmpty && slots[i].food != null)
                    return true;
            return false;
        }

        // EN: Counts empty plates; the plate being prepared remains occupied.
        // VI: Đếm đĩa trống; đĩa đang sơ chế vẫn chiếm chỗ.
        public int CountEmptySlots()
        {
            int count = 0;
            IReadOnlyList<ConveyorSlot> slots = Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                ConveyorSlot slot = slots[i];
                if (slot != null && slot.isActiveAndEnabled && slot.isEmpty)
                    count++;
            }
            return count;
        }

        // EN: Selects empty board destinations nearest the station entrance, excluding occupied preparation plates.
        // VI: Chọn đích trống từ board gần tới cửa trạm nhất, bỏ qua đĩa đang giữ món sơ chế.
        public bool TryGetNearestEmptySlots(Vector3 worldPosition, int count, List<ConveyorSlot> result)
        {
            result.Clear();
            slotPriorities.Clear();
            if (count <= 0)
                return false;
            IReadOnlyList<ConveyorSlot> slots = Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                ConveyorSlot slot = slots[i];
                if (slot != null && slot.isActiveAndEnabled && slot.isEmpty)
                {
                    result.Add(slot);
                    slotPriorities[slot] = GetSlotPriority(i, slot, worldPosition);
                }
            }
            if (result.Count < count)
            {
                result.Clear();
                return false;
            }
            result.Sort((a, b) => slotPriorities[a].CompareTo(slotPriorities[b]));
            result.RemoveRange(count, result.Count - count);
            return true;
        }

        // EN: Checks membership against the conveyor's slot references rather than hierarchy position.
        // VI: Kiểm tra slot thuộc băng chuyền bằng reference thay vì vị trí trong hierarchy.
        public bool ContainsSlot(ConveyorSlot slot)
        {
            if (slot == null)
                return false;
            foreach (ConveyorSlot candidate in Slots)
            {
                if (candidate == slot)
                    return true;
            }
            return false;
        }

        // EN: Reserves a landed raw portion on its original plate while the chef processes it.
        // VI: Giữ phần thô đã đáp trên đĩa gốc trong lúc đầu bếp sơ chế.
        public bool TryPrepareSlot(ConveyorSlot slot)
        {
            if (!isActiveAndEnabled || IsBusy || !ContainsSlot(slot) || !CanPrepare(slot))
                return false;
            preparingSlot = slot;
            preparingFood = slot.food;
            SetupCuttingFood(slot.IngredientRenderer, GetRawSprite(slot), slot.LandingPosition, slot.LandingRotation, slot.LandingScale);
            slot.BeginFoodArrival();
            preparingArrivalId = slot.ArrivalId;
            slot.SetPreparationProgress(0f);
            preparationRoutine = StartCoroutine(PrepareFood(slot.LandingScale));
            return true;
        }

        // EN: Sends the best raw portion of a committed pick straight to an idle chef; landed belt food wins equal priorities.
        // VI: Đưa phần thô ưu tiên cao nhất của lượt lấy đã chốt thẳng tới đầu bếp rảnh; món đã đáp trên băng thắng khi ngang ưu tiên.
        public void TryPreparePicked(IReadOnlyList<DishPickBatch.Entry> entries)
        {
            if (!isActiveAndEnabled || BoosterPaused || !autoPrepareInStation || IsBusy || Time.timeScale <= 0f)
                return;
            LoadNeededIngredients();
            HashSet<ConveyorSlot> pickedSlots = new HashSet<ConveyorSlot>();
            ConveyorSlot best = null;
            SpriteRenderer source = null;
            Food dish = null;
            int bestRank = int.MaxValue;
            foreach (DishPickBatch.Entry entry in entries)
            {
                foreach (ConveyorSlot slot in entry.slots)
                {
                    pickedSlots.Add(slot);
                    if (!CanPrepare(slot))
                        continue;
                    int rank = GetNeedRank(slot.food.foodType);
                    if (best != null && rank >= bestRank)
                        continue;
                    best = slot;
                    bestRank = rank;
                    dish = entry.dish;
                    source = entry.first.Contains(slot) ? dish.IconRenderer : dish.SecondIconRenderer;
                }
            }
            if (best == null)
                return;
            foreach (ConveyorSlot slot in Slots)
                if (!pickedSlots.Contains(slot) && CanPrepare(slot) && GetNeedRank(slot.food.foodType) <= bestRank)
                    return;
            preparingSlot = best;
            preparingFood = best.food;
            SetupCuttingFood(source, GetRawSprite(best), best.LandingPosition, best.LandingRotation, best.LandingScale);
            Transform flyer = cuttingFood.transform;
            flyer.position = source != null ? source.transform.position : dish.transform.position;
            flyer.rotation = source != null ? source.transform.rotation : dish.transform.rotation;
            flyer.localScale = source != null ? source.transform.lossyScale : dish.transform.lossyScale;
            best.BeginFoodArrival();
            preparingArrivalId = best.ArrivalId;
            preparationRoutine = StartCoroutine(PrepareFood(best.LandingScale));
        }

        // EN: Stops chef work, hides progress and restores the reserved plate without clearing its food.
        // VI: Dừng sơ chế, ẩn tiến độ và khôi phục đĩa giữ chỗ mà không xóa món.
        public void CancelPreparation()
        {
            if (preparationRoutine != null)
                StopCoroutine(preparationRoutine);
            FinishPreparation();
        }

        // EN: Shared gameplay tuning from the IngameConfig singleton (Resources asset); no per-component reference.
        // VI: Thông số màn chơi dùng chung lấy từ singleton IngameConfig (asset trong Resources); không cần tham chiếu riêng trên component.
        private IngameConfig Config => IngameConfig.Instance;


        // EN: Clears stale static state when entering Play Mode, including with domain reload disabled.
        // VI: Xóa trạng thái tĩnh cũ khi vào Play Mode, kể cả khi tắt tải lại miền mã.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            instance = null;
        }

        // EN: Picks the next raw, already landed portion anywhere on the belt (not only inside the station). First by customer need: the ingredient that comes earliest in CustomerManager's serving priority (earliest customer, card order, base first); then by position: a slot inside the station that is about to leave it, otherwise the slot that reaches the station soonest. Returns null when there is nothing raw.
        // VI: Chọn phần nguyên liệu thô đã đáp xuống tiếp theo ở bất kỳ đâu trên băng (không chỉ trong trạm). Ưu tiên trước theo nhu cầu khách: nguyên liệu đứng sớm nhất trong thứ tự phục vụ của CustomerManager (khách đến sớm nhất, thứ tự trên thẻ, món đế trước); sau đó theo vị trí: slot đang trong trạm và sắp ra khỏi trạm, nếu không thì slot sắp tới trạm sớm nhất. Trả về null khi không có món thô nào.
        private ConveyorSlot FindSlotToPrepare()
        {
            ConveyorSlot best = null;
            int bestNeed = int.MaxValue;
            float bestDistance = float.MaxValue;
            bool needsLoaded = false;
            IReadOnlyList<ConveyorSlot> slots = Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                ConveyorSlot slot = slots[i];
                if (!CanPrepare(slot))
                    continue;
                if (!needsLoaded)
                {
                    LoadNeededIngredients();
                    needsLoaded = true;
                }
                int need = GetNeedRank(slot.food.foodType);
                float distance = GetDistanceToChef(i);
                if (need < bestNeed || (need == bestNeed && distance < bestDistance))
                {
                    best = slot;
                    bestNeed = need;
                    bestDistance = distance;
                }
            }
            return best;
        }

        // EN: Returns the world position in front of the chef where food is cut.
        // VI: Trả về vị trí world trước mặt đầu bếp nơi món được cắt.
        private Vector3 GetCuttingPosition()
        {
            if (cuttingPoint != null)
                return cuttingPoint.position;
            Transform chef = ChefAnims != null ? ChefAnims.transform : transform;
            return chef.position + cuttingOffset;
        }

        // EN: Destroys the runtime cutting sprite, its shadow and debris emitters together with this manager.
        // VI: Hủy sprite món đang cắt, bóng đổ và bộ phát vụn tạo lúc chạy cùng với manager này.
        private void OnDestroyRuntimeObjects()
        {
            if (cuttingFood != null)
                Destroy(cuttingFood.gameObject);
            if (cuttingShadow != null)
                Destroy(cuttingShadow.gameObject);
            foreach (ParticleEffect debris in debrisInstances.Values)
            {
                if (debris != null)
                    Destroy(debris.gameObject);
            }
            debrisInstances.Clear();
        }

        // EN: First active slot holding the given food that also passes the filter, or null.
        // VI: Slot đang hoạt động đầu tiên chứa nguyên liệu đã cho và thỏa bộ lọc, hoặc null.
        private ConveyorSlot FindFoodSlot(FoodType type, System.Func<ConveyorSlot, bool> filter)
        {
            IReadOnlyList<ConveyorSlot> slots = Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                ConveyorSlot slot = slots[i];
                if (slot != null && slot.isActiveAndEnabled && !slot.isEmpty && slot.food != null && slot.food.foodType == type && filter(slot))
                    return slot;
            }
            return null;
        }

        // EN: Ranks empty slots by remaining travel to the station entrance, falling back to spatial distance when movement is unavailable.
        // VI: Xếp slot trống theo quãng đường còn lại tới cửa vào trạm, dùng khoảng cách không gian khi không có component di chuyển.
        private float GetSlotPriority(int index, ConveyorSlot slot, Vector3 worldPosition)
        {
            if (conveyorMovement == null)
                return (slot.transform.position - worldPosition).sqrMagnitude;
            return conveyorMovement.GetDistanceToStation(index);
        }

        // EN: Prepare the reusable cutting sprite at an explicit source pose for belt or slot-free preparation.
        // VI: Chuẩn bị sprite cắt dùng lại tại tư thế nguồn tường minh cho lượt trên băng hoặc đi thẳng.
        private SpriteRenderer SetupCuttingFood(SpriteRenderer source, Sprite sprite, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (cuttingFood == null)
            {
                cuttingFood = new GameObject("Cutting food").AddComponent<SpriteRenderer>();
                cuttingFood.transform.SetParent(null);
            }
            cuttingFood.sprite = sprite;
            cuttingFood.color = Color.white;
            cuttingFood.enabled = true;
            cuttingFood.sortingLayerID = source != null ? source.sortingLayerID : 0;
            cuttingFood.sortingOrder = (source != null ? source.sortingOrder : 0) + Config.preparation.cuttingSortingBoost;
            cuttingFood.gameObject.layer = source != null ? source.gameObject.layer : 0;
            Transform target = cuttingFood.transform;
            target.position = position;
            target.rotation = rotation;
            target.localScale = scale;
            cuttingFood.gameObject.SetActive(true);
            return cuttingFood;
        }

        // EN: Raw sprite of the slot's food for the flight to the chef: what the plate shows, else the raw icon from the shared FoodIconConfig.
        // VI: Sprite thô của món trên slot dùng khi bay vào đầu bếp: hình đang hiện trên đĩa, nếu không có thì icon thô trong FoodIconConfig dùng chung.
        private static Sprite GetRawSprite(ConveyorSlot slot)
        {
            Sprite sprite = slot.FoodSprite;
            if (sprite != null || slot.food == null)
                return sprite;
            FoodIconConfig icons = IngameConfig.Instance.FoodIcons;
            return icons != null ? icons.GetIcon(slot.food.foodType, FoodState.Raw) : null;
        }

        // EN: Flies the reserved raw portion from its current sprite pose to the chef, prepares it, then returns it to the same moving plate. The progress ring covers both flights, chopping and the prepared poof; pause and arrival identity protect the job.
        // VI: Bay phần thô đã giữ từ vị trí và scale sprite hiện tại tới đầu bếp, sơ chế rồi trả về đúng đĩa đang chạy. Vòng tiến độ tính hai lượt bay, chặt và poof; trạng thái dừng và mã lượt đáp bảo vệ công việc.
        private IEnumerator PrepareFood(Vector3 plateScale)
        {
            SpriteRenderer flyer = cuttingFood;
            Vector3 start = flyer.transform.position;
            Vector3 startScale = flyer.transform.localScale;
            Vector3 cutScale = plateScale * Config.preparation.cuttingScale;
            float chopDuration = Mathf.Max(0.01f, ChefAnims != null ? ChefAnims.GetPreparationDuration(preparingFood.foodType) : 0f);
            float distance = Vector3.Distance(start, GetCuttingPosition());
            float liftDuration = GetFlightDuration(Config.preparation.liftDuration, distance);
            float dropDuration = GetFlightDuration(Config.preparation.dropDuration, Vector3.Distance(preparingSlot.LandingPosition, GetCuttingPosition()));
            BeginPreparationProgress(liftDuration + chopDuration + dropDuration);
            yield return Hop(flyer.transform, () => start, GetCuttingPosition, startScale, cutScale, liftDuration);
            if (!OwnsPreparationPlate()) { FinishPreparation(); yield break; }
            flyer.enabled = false;
            SetChopping(true);
            yield return PlayChops(chopDuration, preparingFood.foodType, flyer, cutScale);
            SetChopping(false);
            if (!OwnsPreparationPlate() || !preparingFood.Prepare()) { FinishPreparation(); yield break; }
            Sprite preparedSprite = IngameConfig.Instance.FoodIcons != null ? IngameConfig.Instance.FoodIcons.GetIcon(preparingFood.foodType, FoodState.Prepared) : flyer.sprite;
            yield return PlayPreparedPoof(flyer, preparedSprite, cutScale);
            if (!OwnsPreparationPlate()) { FinishPreparation(); yield break; }
            ConveyorSlot slot = preparingSlot;
            yield return Hop(flyer.transform, GetCuttingPosition, () => slot != null ? slot.LandingPosition : flyer.transform.position, cutScale, slot.LandingScale, dropDuration);
            FinishPreparation();
        }

        // EN: Refreshes the customers' ingredient needs in serving priority, including food already on the belt.
        // VI: Cập nhật nhu cầu nguyên liệu của khách theo thứ tự phục vụ, tính cả món đang nằm trên băng.
        private void LoadNeededIngredients()
        {
            CustomerManager customers = CustomerManager.Instance;
            if (customers != null)
                customers.GetNeededIngredients(neededIngredients, false);
            else
                neededIngredients.Clear();
        }

        // EN: Checks that a slot is active and holds raw food that has finished flying in.
        // VI: Kiểm tra slot đang hoạt động và chứa món thô đã bay tới xong.
        private static bool CanPrepare(ConveyorSlot slot)
        {
            return slot != null && slot.isActiveAndEnabled && !slot.isEmpty && !slot.IsFoodArriving &&
                   slot.food != null && slot.food.IsRaw;
        }

        // EN: Position of the food type in the customers' serving priority (0 = most urgent); int.MaxValue when no customer needs it.
        // VI: Vị trí của loại nguyên liệu trong thứ tự phục vụ khách (0 = cần gấp nhất); int.MaxValue khi không khách nào cần.
        private int GetNeedRank(FoodType type)
        {
            int rank = neededIngredients.IndexOf(type);
            return rank >= 0 ? rank : int.MaxValue;
        }

        // EN: Cancel any undelivered direct reservation, hide chef visuals and release only the owned belt arrival.
        // VI: Hủy giữ order chưa giao, ẩn hình đầu bếp và chỉ nhả lượt đáp trên băng đang sở hữu.
        private void FinishPreparation()
        {
            directServe?.Cancel();
            directServe = null;
            if (cuttingFood != null)
                cuttingFood.gameObject.SetActive(false);
            if (cuttingBoard != null)
                cuttingBoard.localScale = boardBaseScale;
            SetChopping(false);
            if (preparingSlot != null && preparingSlot.OwnsFoodArrival(preparingArrivalId))
            {
                preparingSlot.HidePreparationProgress();
                preparingSlot.CompleteFoodArrival(preparingArrivalId);
            }
            preparingSlot = null;
            preparingFood = null;
            preparationRoutine = null;
        }

        // EN: Ranks how soon a slot is at the chef: inside the station values are below 0 (the closer to the station exit, the lower), outside it is the remaining belt distance to the station start.
        // VI: Xếp hạng slot sớm tới chỗ đầu bếp cỡ nào: trong trạm giá trị nhỏ hơn 0 (càng gần cửa ra trạm càng nhỏ), ngoài trạm là quãng đường còn lại trên băng tới đầu trạm.
        private float GetDistanceToChef(int index)
        {
            float stationProgress = conveyorMovement.GetStationProgress(index);
            return stationProgress >= 0f ? -1f - stationProgress : conveyorMovement.GetDistanceToStation(index);
        }

        // EN: Flight time for a plate at the given world distance from the cutting point: the base time up to nearFlightDistance, then flightTimePerUnit more per extra world unit, capped at base × maxFlightMultiplier.
        // VI: Thời gian bay cho đĩa cách điểm cắt một khoảng world đã cho: giữ thời gian gốc trong phạm vi nearFlightDistance, xa hơn thì cộng flightTimePerUnit cho mỗi đơn vị world, tối đa bằng thời gian gốc × maxFlightMultiplier.
        private float GetFlightDuration(float baseDuration, float distance)
        {
            IngameConfig.PreparationSettings prep = Config.preparation;
            float extra = Mathf.Max(0f, distance - prep.nearFlightDistance) * prep.flightTimePerUnit;
            return Mathf.Min(baseDuration + extra, baseDuration * Mathf.Max(1f, prep.maxFlightMultiplier));
        }

        // EN: Starts the plate's progress ring for one job: total time = both flights and chopping (given) + the prepared poof.
        // VI: Bắt đầu vòng tiến độ trên đĩa cho một lượt: tổng thời gian = hai lượt bay và thời gian chặt (truyền vào) + poof sơ chế xong.
        private void BeginPreparationProgress(float flightsAndChop)
        {
            preparationElapsed = 0f;
            preparationTotal = Mathf.Max(0.01f, flightsAndChop + PreparedPoofDuration);
            if (preparingSlot != null)
                preparingSlot.SetPreparationProgress(0f);
        }

        // EN: Moves food along a scaled-time arc between moving endpoints, suspending flight during booster pause; the flight time counts toward the plate's progress ring.
        // VI: Di chuyển món theo cung dùng thời gian game giữa hai đích động, tạm ngừng bay khi booster dừng; thời gian bay được tính vào vòng tiến độ trên đĩa.
        private IEnumerator Hop(Transform target, System.Func<Vector3> from, System.Func<Vector3> to, Vector3 fromScale, Vector3 toScale, float duration)
        {
            float time = 0f;
            while (time < duration)
            {
                if (BoosterPaused)
                {
                    yield return null;
                    continue;
                }
                time += Time.deltaTime;
                AdvancePreparationProgress(Time.deltaTime);
                float t = Mathf.Clamp01(time / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                Vector3 position = Vector3.LerpUnclamped(from(), to(), eased);
                position.y += Config.preparation.hopHeight * 4f * t * (1f - t);
                target.position = position;
                target.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
                yield return null;
            }
            target.position = to();
            target.localScale = toScale;
        }

        // EN: Matches the original food and arrival identity so cleared or reused plates cannot receive an old chef job.
        // VI: Khớp món gốc và mã lượt đáp để đĩa đã xóa hoặc dùng lại không nhận lượt sơ chế cũ.
        private bool OwnsPreparationPlate()
        {
            return preparingSlot != null && preparingSlot.isActiveAndEnabled && !preparingSlot.isEmpty &&
                   preparingFood != null && preparingSlot.food == preparingFood && preparingSlot.OwnsFoodArrival(preparingArrivalId);
        }

        // EN: Select Spine idle/preparation for the active belt or direct ingredient.
        // VI: Chọn Spine nghỉ/sơ chế cho nguyên liệu đang xử lý trên băng hoặc đi thẳng.
        private void SetChopping(bool chopping)
        {
            if (ChefAnims != null)
                ChefAnims.SetPreparing(chopping, directServe != null ? directServe.Ingredient : preparingFood != null ? preparingFood.foodType : FoodType.None);
        }

        // EN: Share chopping motion, sound and debris between valid belt and direct jobs, respecting booster pause.
        // VI: Dùng chung nhịp chặt, âm thanh và vụn cho lượt băng/đi thẳng hợp lệ, tuân theo booster pause.
        private IEnumerator PlayChops(float duration, FoodType type, SpriteRenderer foodSprite, Vector3 restScale)
        {
            ParticleEffect debris = GetDebris(type);
            float elapsed = 0f;
            float nextChop = 0f;
            float lastChop = 0f;
            while (elapsed < duration)
            {
                if (BoosterPaused)
                {
                    yield return null;
                    continue;
                }
                if (!HasPreparationTarget())
                    yield break;
                if (elapsed >= nextChop)
                {
                    IngameSoundManager.Instance?.PlayChop(type);
                    EmitDebris(debris, foodSprite);
                    lastChop = nextChop;
                    nextChop += Config.preparation.chopInterval;
                }
                ApplyChopPunch(foodSprite.transform, restScale, elapsed - lastChop);
                yield return null;
                elapsed += Time.deltaTime;
                AdvancePreparationProgress(Time.deltaTime);
            }
            ApplyChopPunch(foodSprite.transform, restScale, float.MaxValue);
        }

        // EN: Shows the prepared sprite with a chime, the prepared particle effect and a pop, suspending animation during booster pause; the pop time counts toward the plate's progress ring.
        // VI: Hiện ảnh đã sơ chế kèm tiếng ting, hiệu ứng particle sơ chế xong và nhịp nảy, tạm ngừng hiệu ứng khi booster dừng; thời gian nảy được tính vào vòng tiến độ trên đĩa.
        private IEnumerator PlayPreparedPoof(SpriteRenderer flyer, Sprite preparedSprite, Vector3 restScale)
        {
            flyer.sprite = preparedSprite;
            flyer.enabled = true;
            IngameSoundManager.Instance?.PlayPrepared();
            PlayPreparedEffect(flyer);
            float time = 0f;
            while (time < PreparedPoofDuration)
            {
                if (BoosterPaused)
                {
                    yield return null;
                    continue;
                }
                time += Time.deltaTime;
                AdvancePreparationProgress(Time.deltaTime);
                float t = Mathf.Clamp01(time / PreparedPoofDuration);
                float pop = t < 0.6f ? Mathf.Lerp(0.4f, 1.15f, t / 0.6f) : Mathf.Lerp(1.15f, 1f, (t - 0.6f) / 0.4f);
                flyer.transform.localScale = restScale * pop;
                yield return null;
            }
            flyer.transform.localScale = restScale;
        }

        // EN: Adds unpaused game time to the current job and redraws the plate's progress ring.
        // VI: Cộng thời gian game (không tính lúc tạm dừng) vào lượt hiện tại và vẽ lại vòng tiến độ trên đĩa.
        private void AdvancePreparationProgress(float deltaTime)
        {
            preparationElapsed += deltaTime;
            if (OwnsPreparationPlate())
                preparingSlot.SetPreparationProgress(preparationElapsed / preparationTotal);
        }

        // EN: Resolve the ingredient-specific debris emitter, caching one instance for each configured prefab.
        // VI: Lấy bộ phát vụn riêng của nguyên liệu, cache một instance cho mỗi prefab đã cấu hình.
        private ParticleEffect GetDebris(FoodType type)
        {
            FoodIconConfig config = IngameConfig.Instance.FoodIcons;
            ParticleEffect prefab = config != null ? config.GetChopDebris(type) : null;
            if (prefab == null)
                return null;
            if (!debrisInstances.TryGetValue(prefab, out ParticleEffect instance) || instance == null)
            {
                instance = Instantiate(prefab);
                debrisInstances[prefab] = instance;
            }
            return instance;
        }

        // EN: Moves the debris emitter to the food, draws it just above the food and emits Config.preparation.debrisPerChop pieces from the main layer and about half as many from each extra layer (e.g. sauce drops, shell bits).
        // VI: Đưa bộ phát vụn tới vị trí món, vẽ ngay phía trên món, bắn Config.preparation.debrisPerChop mảnh ở lớp chính và khoảng một nửa số đó ở mỗi lớp phụ (ví dụ giọt sốt, mảnh vỏ).
        private void EmitDebris(ParticleEffect debris, SpriteRenderer food)
        {
            if (debris == null || food == null || Config.preparation.debrisPerChop <= 0)
                return;
            debris.transform.position = food.transform.position;
            debris.SetSorting(food.sortingLayerID, food.sortingOrder + 1);
            debris.Emit(Config.preparation.debrisPerChop, Mathf.Max(1, Config.preparation.debrisPerChop / 2));
        }

        // EN: Squashes the food and slightly presses the cutting board right after a chop, easing back within ~0.1 s, so each knife hit has weight.
        // VI: Làm món bẹp và thớt lún nhẹ ngay sau mỗi nhát chặt, trở lại bình thường trong khoảng 0.1 giây, để mỗi nhát dao có lực.
        private void ApplyChopPunch(Transform food, Vector3 restScale, float timeSinceChop)
        {
            const float PunchTime = 0.1f;
            float strength = timeSinceChop < PunchTime ? Config.preparation.chopPunch * (1f - timeSinceChop / PunchTime) : 0f;
            food.localScale = Vector3.Scale(restScale, new Vector3(1f + strength, 1f - strength, 1f));
            if (cuttingBoard != null)
                cuttingBoard.localScale = Vector3.Scale(boardBaseScale, new Vector3(1f + strength * 0.25f, 1f - strength * 0.4f, 1f));
        }

        // EN: Plays the optional prepared particle once at the food, drawn just above it, using the shared pooled one-shot helper.
        // VI: Chạy particle sơ chế xong (tùy chọn) một lần tại món, vẽ ngay phía trên món, dùng helper particle một lần có pool dùng chung.
        private void PlayPreparedEffect(SpriteRenderer food)
        {
            if (preparedEffectPrefab == null || food == null)
                return;
            OneShotEffects.PlayParticle(preparedEffectPrefab, food.transform.position, food.sortingLayerID, food.sortingOrder + 1);
        }
    }
}
