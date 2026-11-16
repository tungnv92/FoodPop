using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Owns customer queues, order progress and dispatch priority, delegating serving visuals to each lane's independent ServingFood.
    // VI: Quản lý hàng khách, tiến độ order và ưu tiên phục vụ, giao hiển thị phục vụ cho ServingFood độc lập của từng làn.
    [DisallowMultipleComponent]
    public class CustomerManager : MonoBehaviour
    {
        // EN: A dish the AutoComplete booster will finish: its tray, its slot index and the ingredients it still needed (now reserved, in recipe order, base first).
        // VI: Món mà booster AutoComplete sẽ hoàn thành: khay, chỉ số ô và các nguyên liệu nó còn thiếu (đã được giữ chỗ, theo thứ tự công thức, đế trước).
        public sealed class AutoCompleteJob
        {
            public ServingFood serving;
            public int dish;
            public readonly List<FoodType> ingredients = new List<FoodType>();
        }

        private const string ShipperPortraitName = "char_13";

        // EN: Returns the scene singleton, resolving it once when Awake has not run yet.
        // VI: Trả về singleton trong scene, tìm một lần nếu Awake chưa chạy.
        public static CustomerManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<CustomerManager>();
                return instance;
            }
        }

        // EN: Counts all loaded customers not yet fully departed from the counter.
        // VI: Đếm mọi khách đã nạp chưa hoàn tất rời quầy.
        public int RemainingCustomers => customerQueue.Count - completedCustomers;

        // EN: True after one customer's complete order has been delivered in the current level.
        // VI: Đúng sau khi đã giao đủ toàn bộ order của một khách trong màn hiện tại.
        public bool HasCompletedOrder { get; private set; }

        // EN: Announces a valid customer ready to order, using its zero-based queue position.
        // VI: Báo khách hợp lệ sẵn sàng gọi món, dùng vị trí hàng chờ bắt đầu từ không.
        public event System.Action<int, OrderView> CustomerArrived;

        public bool BoosterPaused { get; set; }

        // EN: True once every customer of the level has been served.
        // VI: True khi mọi khách của level đã được phục vụ.
        public bool AllServed => allServedLogged;

        // EN: True while a timed customer (Boss, Shipper) is still ordering with no serving time left.
        // VI: Đúng khi có khách tính giờ (Boss, Shipper) vẫn đang chờ món mà đã hết thời gian phục vụ.
        public bool ServeTimeExpired
        {
            get
            {
                foreach (Lane lane in lanes)
                    if (IsServeTimeExpired(lane))
                        return true;
                return false;
            }
        }

        public event Action<int> RemainingCustomersChanged;

        [Header("Tham chiếu")]
        [Tooltip("Băng chuyền chứa các slot món.")]
        [SerializeField] private ConveyorChefMovement conveyor;
        [Tooltip("Làn trái (phần tử 0) và làn phải (phần tử 1).")]
        [SerializeField] private Lane[] lanes = new Lane[2];
        [Tooltip("Bật: khách Shipper được phục vụ một mình, đứng giữa màn hình với một khay phục vụ. Tắt: Shipper phục vụ bình thường theo lượt như khách khác.")]
        [SerializeField] private bool OnlyServeOneShipper = true;

        [Header("Phục vụ")]
        [Tooltip("Thời gian (giây) món phải nằm trên đĩa sau khi đáp xuống rồi mới được bay vào order.")]
        [SerializeField, Min(0f)] private float serveDelayAfterLanding = 0.2f;

        private static CustomerManager instance;

        private PraiseText praiseText;

        private long arrivalCounter;
        private bool allServedLogged;
        private List<CustomerOrder> customerQueue = new List<CustomerOrder>();
        private int nextCustomer;
        private int completedCustomers;
        private Lane waitingLane;
        // EN: Portrait picked for each customer in the shared queue (index = queue position), so the same customer keeps one look.
        // VI: Ảnh đã chọn cho từng khách trong hàng chung (chỉ số = vị trí hàng), để một khách luôn giữ một ảnh.
        private readonly Dictionary<int, Sprite> portraitByCustomer = new Dictionary<int, Sprite>();
        private readonly Dictionary<Transform, Vector3> cardHomes = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<Transform, Vector3> servingHomes = new Dictionary<Transform, Vector3>();
        private bool seatHomesCaptured;
        private float shipperCardGap;
        private readonly List<Sprite> portraitBag = new List<Sprite>();
        private Sprite lastRandomPortrait;

        // EN: Registers the singleton and removes a duplicate component.
        // VI: Đăng ký singleton và bỏ component trùng.
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[CustomerManager] Scene đã có CustomerManager; bỏ component trùng.", this);
                Destroy(this);
                return;
            }
            instance = this;
        }

#if UNITY_EDITOR
        // EN: Links missing per-lane serving components left to right only when the scene contains exactly two trays.
        // VI: Gán component phục vụ còn thiếu theo thứ tự trái sang phải chỉ khi scene có đúng hai khay.
        private void OnValidate()
        {
            if (Application.isPlaying || lanes == null || lanes.Length < 2 ||
                (lanes[0]?.servingFood != null && lanes[1]?.servingFood != null))
                return;
            ServingFood[] services = FindObjectsByType<ServingFood>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (services.Length != 2)
                return;
            Array.Sort(services, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            for (int side = 0; side < 2; side++)
                if (lanes[side] != null && lanes[side].servingFood == null)
                    lanes[side].servingFood = services[side];
        }
#endif

        // EN: Sends prepared food from any belt slot straight to a matching order (no exit segment), unless collection has temporarily paused new serving flights.
        // VI: Gửi food đã sơ chế ở bất kỳ slot nào trên băng thẳng tới order phù hợp (không còn đoạn ra món), trừ khi lượt thu đang tạm dừng chuyến phục vụ mới.
        private void Update()
        {
            TickServeTimers();
            if (BoosterPaused || conveyor == null)
                return;
            foreach (ConveyorSlot slot in conveyor.Slots)
                TryServeFromSlot(slot);
        }

        // EN: Releases the registered manager and cancels its serving orders without letting duplicate components reset active trays.
        // VI: Giải phóng manager đã đăng ký và hủy order phục vụ, không để component trùng đặt lại các khay đang hoạt động.
        private void OnDestroy()
        {
            if (instance != this)
                return;
            foreach (Lane lane in lanes)
                ResetServing(lane);
            instance = null;
        }

        // EN: Resets customer progress and the completed-order flag, reserves the first customers and publishes the remaining count.
        // VI: Đặt lại tiến độ/cờ hoàn thành order, lấy khách đầu hàng chung và báo số khách còn lại.
        public void LoadCustomers(LevelConfig config)
        {
            if (config == null) return;
            StopAllCoroutines();
            praiseText?.HideAll();
            customerQueue = config.GetCustomerQueue();
            nextCustomer = 0;
            completedCustomers = 0;
            HasCompletedOrder = false;
            arrivalCounter = 0;
            waitingLane = null;
            allServedLogged = false;
            portraitByCustomer.Clear();
            portraitBag.Clear();
            lastRandomPortrait = null;
            foreach (Lane lane in lanes)
            {
                if (lane == null) continue;
                lane.queue = new List<CustomerOrder>();
                lane.queueIndex = 0;
                lane.ordering = false;
                ResetServing(lane);
                StopServeTimer(lane);
                lane.dishes.Clear();
                CaptureQueueSpots(lane);
                if (waitingLane == null) waitingLane = lane;
                foreach (SpriteRenderer figure in lane.figures) figure.gameObject.SetActive(false);
                if (lane.walker != null) lane.walker.gameObject.SetActive(false);
                if (lane.customer != null) lane.customer.SetVisible(false);
            }
            foreach (Lane lane in lanes)
                if (lane != null && AssignNextCustomer(lane)) StartCoroutine(BringNextCustomer(lane));
            if (waitingLane != null) LayoutWaitingQueue(waitingLane, 0f);
            CheckAllServed();
            RemainingCustomersChanged?.Invoke(RemainingCustomers);
        }

        // EN: True when some ordering customer has a dish that can take this ingredient right now (ingredients already flying are counted).
        // VI: True khi có khách đang order có món nhận được nguyên liệu này ngay lúc này (đã tính nguyên liệu đang bay tới).
        public bool TryFindCustomer(FoodType type) => TryFindCustomer(type, out _, out _);

        // EN: Reserve one currently acceptable portion before a capacity-limited pick leaves the board.
        // VI: Giữ một phần hiện có thể nhận trước khi lượt lấy thiếu sức chứa rời board.
        public bool TryReserveDirectServe(FoodType type, out DirectServeReservation reservation)
        {
            reservation = null;
            if (!TryFindCustomer(type, out Lane lane, out int dish)) return false;
            reservation = new DirectServeReservation(lane, dish, type);
            return true;
        }

        // EN: True when some ordering customer has a dish the AutoComplete booster can finish right now.
        // VI: True khi có khách đang order có món mà booster AutoComplete hoàn thành được ngay lúc này.
        public bool HasAutoCompleteTarget() => FindAutoCompleteDish(out _, out _);

        // EN: Picks the dish missing the most ingredients among every ordering customer (ties: earlier customer, then earlier dish on the card), skipping dishes with an ingredient still flying in or already assembling. Reserves all of its missing ingredients so the belt no longer serves them, and returns them in recipe order.
        // VI: Chọn món còn thiếu nhiều nguyên liệu nhất trong mọi khách đang order (bằng nhau thì khách đến sớm hơn, rồi món đứng trước trên thẻ), bỏ qua món đang có nguyên liệu bay tới hoặc đang ghép. Giữ chỗ mọi nguyên liệu còn thiếu để băng chuyền không gửi tới nữa, và trả về chúng theo thứ tự công thức.
        public bool TryReserveAutoComplete(out AutoCompleteJob job)
        {
            job = null;
            if (!FindAutoCompleteDish(out Lane lane, out int dish))
                return false;
            DishProgress progress = lane.dishes[dish];
            job = new AutoCompleteJob { serving = lane.servingFood, dish = dish };
            foreach (FoodType ingredient in progress.recipe)
                if (progress.missing.Contains(ingredient))
                    job.ingredients.Add(ingredient);
            foreach (FoodType ingredient in job.ingredients)
                progress.Reserve(ingredient);
            return job.ingredients.Count > 0;
        }

        // EN: Adds thirty seconds only to expired customers and passes the new remaining/total seconds to restore their normal fills.
        // VI: Chỉ cộng ba mươi giây cho khách hết giờ và truyền số giây còn lại/tổng mới để khôi phục phần màu bình thường.
        public void ResetExpiredServeTimers()
        {
            foreach (Lane lane in lanes)
            {
                if (!IsServeTimeExpired(lane))
                    continue;
                lane.serveTimeLeft += 30f;
                lane.serveTimeTotal = lane.serveTimeLeft;
                CustomerTimerBar bar = GetTimerBar(lane);
                if (bar != null)
                    bar.SetProgress(lane.serveTimeLeft, lane.serveTimeTotal);
            }
        }

        // EN: Defers deadlock checks while assigned customers move, receive ingredients or finish assembling dishes.
        // VI: Hoãn kiểm tra bế tắc khi khách đã phân chỗ đang di chuyển, nhận nguyên liệu hoặc ghép món.
        public bool HasPendingService()
        {
            foreach (Lane lane in lanes)
            {
                if (lane == null) continue;
                if (!lane.ordering && lane.queue != null && lane.queueIndex < lane.queue.Count)
                    return true;
                foreach (DishProgress dish in lane.dishes)
                    if (dish.incoming > 0 || (!dish.assembled && dish.missing.Count == 0))
                        return true;
            }
            return false;
        }

        // EN: Fills result with the ingredients still needed, in serving priority: customers by arrival (earliest first), their dishes in card order, each dish's base first and then its other missing ingredients in recipe order. When excludeConveyor is true (default), ingredients already on the conveyor (or landing on it) are taken out, earliest need first; the chef passes false to rank the food already on the belt.
        // VI: Điền vào result các nguyên liệu còn cần, theo thứ tự ưu tiên phục vụ: khách theo thứ tự đến (sớm nhất trước), món theo thứ tự trên thẻ, mỗi món đế trước rồi tới các nguyên liệu còn thiếu theo công thức. Khi excludeConveyor là true (mặc định), nguyên liệu đã có (hoặc đang đáp xuống) trên băng chuyền được trừ đi, trừ vào nhu cầu sớm nhất trước; đầu bếp truyền false để xếp hạng chính các món đang nằm trên băng.
        public void GetNeededIngredients(List<FoodType> result, bool excludeConveyor = true)
        {
            result.Clear();
            List<Lane> ordered = new List<Lane>();
            foreach (Lane lane in lanes)
                if (lane != null && lane.ordering)
                    ordered.Add(lane);
            ordered.Sort((a, b) => a.arrivalStamp.CompareTo(b.arrivalStamp));
            foreach (Lane lane in ordered)
            {
                foreach (DishProgress dish in lane.dishes)
                {
                    if (dish.assembled)
                        continue;
                    foreach (FoodType ingredient in dish.recipe)
                        if (dish.missing.Contains(ingredient))
                            result.Add(ingredient);
                }
            }
            if (!excludeConveyor || conveyor == null)
                return;
            IReadOnlyList<ConveyorSlot> slots = conveyor.Slots;
            for (int i = 0; i < slots.Count; i++)
            {
                ConveyorSlot slot = slots[i];
                if (slot == null || slot.food == null || (slot.isEmpty && !slot.IsFoodArriving))
                    continue;
                result.Remove(slot.food.foodType);
            }
        }

        // EN: Shared gameplay tuning from the IngameConfig singleton (Resources asset); no per-component reference.
        // VI: Thông số màn chơi dùng chung lấy từ singleton IngameConfig (asset trong Resources); không cần tham chiếu riêng trên component.
        private IngameConfig Config => IngameConfig.Instance;

        // EN: True when the lane's timed customer is still waiting with no time left.
        // VI: Đúng khi khách tính giờ của làn vẫn đang chờ món mà đã hết giờ.
        private static bool IsServeTimeExpired(Lane lane)
        {
            return lane != null && lane.ordering && lane.serveTimeTotal > 0f && lane.serveTimeLeft <= 0f;
        }

        // EN: Counts down in game time and passes remaining/total seconds so bars select their final-five-second fills accurately.
        // VI: Đếm ngược theo thời gian game và truyền số giây còn lại/tổng để thanh chọn đúng phần màu trong năm giây cuối.
        private void TickServeTimers()
        {
            foreach (Lane lane in lanes)
            {
                if (lane == null || !lane.ordering || lane.serveTimeTotal <= 0f)
                    continue;
                lane.serveTimeLeft = Mathf.Max(0f, lane.serveTimeLeft - Time.deltaTime);
                CustomerTimerBar bar = GetTimerBar(lane);
                if (bar != null)
                    bar.SetProgress(lane.serveTimeLeft, lane.serveTimeTotal);
            }
        }

        // EN: Selects the earliest eligible customer, reserves its recipe ingredient and delegates the flight to that lane's serving component; food that landed less than serveDelayAfterLanding seconds ago waits.
        // VI: Chọn khách phù hợp đến sớm nhất, giữ chỗ nguyên liệu trong công thức và giao chuyến bay cho component phục vụ của làn; món đáp chưa đủ serveDelayAfterLanding giây thì chờ.
        private void TryServeFromSlot(ConveyorSlot slot)
        {
            if (slot == null || slot.isEmpty || slot.IsFoodArriving || slot.food == null || !slot.food.IsPrepared)
                return;
            if (Time.time - slot.LandedTime < serveDelayAfterLanding)
                return;
            if (!TryFindCustomer(slot.food.foodType, out Lane lane, out int dish))
                return;
            FoodType type = slot.food.foodType;
            DishProgress progress = lane.dishes[dish];
            progress.Reserve(type);
            if (!lane.servingFood.LaunchServe(slot, dish))
                progress.CancelReservation(type);
        }

        // EN: Resets only the serving component assigned to this customer lane.
        // VI: Chỉ đặt lại component phục vụ được gán cho làn khách này.
        private void ResetServing(Lane lane)
        {
            if (lane != null && lane.servingFood != null)
                lane.servingFood.ResetOrder();
        }

        // EN: Ends the lane's countdown and hides its bar (customer served or level reset).
        // VI: Dừng đếm ngược của làn và ẩn thanh (khách đã đủ món hoặc nạp lại màn).
        private void StopServeTimer(Lane lane)
        {
            lane.serveTimeTotal = 0f;
            lane.serveTimeLeft = 0f;
            CustomerTimerBar bar = GetTimerBar(lane);
            if (bar != null)
                bar.Hide();
        }

        // EN: Remembers the queue spots from the waiting figures placed in the scene (nearest first) and the step between them, used to place any number of waiting customers.
        // VI: Ghi nhớ các vị trí hàng chờ từ khách đứng chờ đặt sẵn trong scene (gần quầy trước) và khoảng cách giữa chúng, dùng để xếp bao nhiêu khách chờ cũng được.
        private void CaptureQueueSpots(Lane lane)
        {
            if (lane.queueCaptured)
                return;
            lane.queueCaptured = true;
            foreach (SpriteRenderer figure in lane.waitingFigures)
            {
                if (figure == null)
                    continue;
                lane.figures.Add(figure);
                lane.spots.Add(figure.transform.position);
            }
            Vector3 counter = lane.customer != null && lane.customer.Portrait != null ? lane.customer.Portrait.transform.position : Vector3.zero;
            lane.queueOrigin = counter;
            if (lane.spots.Count >= 2)
                lane.spotStep = lane.spots[lane.spots.Count - 1] - lane.spots[lane.spots.Count - 2];
            else if (lane.spots.Count == 1)
                lane.spotStep = lane.spots[0] - counter;
            else
                lane.spotStep = new Vector3(lane.slideDirection * Config.customers.queueSpacing.x, Config.customers.queueSpacing.y, 0f);
        }

        // EN: Reserves one shared customer synchronously before arrival animations yield.
        // VI: Giữ một khách trong hàng chung trước khi hoạt cảnh nhường lượt thực thi.
        // EN: A Shipper is served alone: it only takes a seat when every other lane is empty, and no one else sits down while it is being served.
        // VI: Khách Shipper được phục vụ một mình: chỉ vào quầy khi các làn khác đều trống, và không ai khác vào quầy khi Shipper đang được phục vụ.
        private bool AssignNextCustomer(Lane lane)
        {
            if (nextCustomer >= customerQueue.Count || IsLaneBusy(lane)) return false;
            if (!OnlyServeOneShipper) { lane.customerNumber = nextCustomer; lane.queue.Add(customerQueue[nextCustomer++]); return true; }
            bool nextIsShipper = IsShipper(customerQueue[nextCustomer]);
            foreach (Lane other in lanes)
            {
                if (other == null || other == lane || !IsLaneBusy(other)) continue;
                if (nextIsShipper || IsShipper(other.queue[other.queueIndex])) return false;
            }
            lane.customerNumber = nextCustomer;
            lane.queue.Add(customerQueue[nextCustomer++]);
            return true;
        }

        // EN: Shows the reserved customer, announces its valid order after arrival and skips invalid orders.
        // VI: Hiện khách đã lấy, thông báo order hợp lệ khi đến và bỏ qua order không hợp lệ.
        private IEnumerator BringNextCustomer(Lane lane)
        {
            CustomerOrder customerOrder = lane.queue[lane.queueIndex];
            SetupOrder(lane, customerOrder);
            if (lane.customer != null) lane.customer.SetCustomerType(customerOrder?.customerType ?? CustomerType.Normal);
            if (lane.customer != null)
            {
                lane.customer.SetPortrait(GetPortrait(waitingLane, lane.customerNumber));
                ApplySeat(lane);
                lane.customer.SnapHome();
                lane.customer.SetVisible(true);
            }
            StartServeTimer(lane, customerOrder);
            if (lane.orderView != null)
                lane.orderView.PlayShow();
            LayoutWaitingQueue(waitingLane, 0f);
            yield return null;
            lane.arrivalStamp = ++arrivalCounter;
            if (lane.dishes.Count > 0)
            {
                lane.ordering = true;
                CustomerArrived?.Invoke(lane.customerNumber, lane.orderView);
                yield break;
            }
            Debug.LogWarning($"[CustomerManager] Khách {lane.customerNumber + 1} không có món hợp lệ nào; bỏ qua.", this);
            yield return FinishCustomer(lane);
        }

        // EN: Displays unassigned customers in the single shared waiting row.
        // VI: Hiển thị khách chưa phân chỗ trong một hàng chờ chung.
        private void LayoutWaitingQueue(Lane lane, float stepT)
        {
            int waiting = customerQueue.Count - nextCustomer;
            EnsureFigures(lane, waiting);
            SpriteRenderer active = lane.customer != null ? lane.customer.Portrait : null;
            for (int k = 0; k < lane.figures.Count; k++)
            {
                SpriteRenderer figure = lane.figures[k];
                bool used = k < waiting;
                figure.gameObject.SetActive(used);
                if (!used)
                    continue;
                Sprite portrait = GetPortrait(lane, nextCustomer + k);
                if (portrait != null)
                    figure.sprite = portrait;
                figure.color = Config.customers.waitingTint;
                Vector3 target = GetQueueSpot(lane, k);
                figure.transform.position = stepT > 0f ? Vector3.LerpUnclamped(GetQueueSpot(lane, k + 1), target, stepT) : target;
                if (active != null)
                {
                    SetWorldScale(figure.transform, active.transform.lossyScale * Config.customers.waitingScale);
                    figure.sortingLayerID = active.sortingLayerID;
                    figure.sortingOrder = active.sortingOrder - 1 - k;
                }
            }
        }

        // EN: Completes only after shared waiting and assigned or departing customers are exhausted.
        // VI: Chỉ hoàn tất khi hết khách chờ chung và khách đã phân chỗ hoặc đang rời đi.
        private void CheckAllServed()
        {
            if (allServedLogged || nextCustomer < customerQueue.Count)
                return;
            foreach (Lane lane in lanes)
            {
                if (lane != null && lane.queue != null && lane.queueIndex < lane.queue.Count)
                    return;
            }
            allServedLogged = true;
            Debug.Log("[CustomerManager] Đã phục vụ hết khách của level.", this);
        }

        // EN: Finds the ordering lane whose customer arrived first and has a dish that can take the ingredient; within a customer, the earliest dish on the card wins.
        // VI: Tìm làn đang order có khách đến sớm nhất và có món nhận được nguyên liệu; trong cùng một khách, món đứng trước trên thẻ được ưu tiên.
        private bool TryFindCustomer(FoodType type, out Lane best, out int bestDish)
        {
            best = null;
            bestDish = -1;
            foreach (Lane lane in lanes)
            {
                if (lane == null || !lane.ordering || (best != null && lane.arrivalStamp >= best.arrivalStamp))
                    continue;
                for (int d = 0; d < lane.dishes.Count; d++)
                {
                    if (!lane.dishes[d].CanAccept(type) || lane.servingFood == null || !lane.servingFood.CanServe(d))
                        continue;
                    best = lane;
                    bestDish = d;
                    break;
                }
            }
            return best != null;
        }

        // EN: Finds the AutoComplete target dish (see TryReserveAutoComplete) without reserving anything.
        // VI: Tìm món đích của AutoComplete (xem TryReserveAutoComplete) mà không giữ chỗ gì.
        private bool FindAutoCompleteDish(out Lane best, out int bestDish)
        {
            best = null;
            bestDish = -1;
            int bestMissing = 0;
            foreach (Lane lane in lanes)
            {
                if (lane == null || !lane.ordering || lane.servingFood == null)
                    continue;
                for (int d = 0; d < lane.dishes.Count; d++)
                {
                    DishProgress progress = lane.dishes[d];
                    int missing = progress.missing.Count;
                    if (progress.assembled || progress.incoming > 0 || missing == 0 || !lane.servingFood.CanServe(d))
                        continue;
                    bool better = missing > bestMissing ||
                                  (missing == bestMissing && best != null && lane != best && lane.arrivalStamp < best.arrivalStamp);
                    if (!better)
                        continue;
                    best = lane;
                    bestDish = d;
                    bestMissing = missing;
                }
            }
            return best != null;
        }
        // EN: The serving-time bar already created on the lane's order card, or null.
        // VI: Thanh thời gian phục vụ đã tạo trên thẻ order của làn, hoặc null.
        private static CustomerTimerBar GetTimerBar(Lane lane)
        {
            if (lane == null || lane.orderView == null)
                return null;
            CustomerTimerBar bar = lane.orderView.TimerBar;
            return bar != null ? bar : null;
        }

        private static bool IsLaneBusy(Lane lane) => lane.queue != null && lane.queueIndex < lane.queue.Count;

        private static bool IsShipper(CustomerOrder order) => order != null && order.customerType == CustomerType.Shipper;

        // EN: Builds the customer's ingredient groups, shows each group's dish prefab (when assigned) and its ingredients on the card.
        // VI: Dựng các nhóm nguyên liệu của khách, hiện prefab món của từng nhóm (nếu có gán) và nguyên liệu trên thẻ.
        private void SetupOrder(Lane lane, CustomerOrder order)
        {
            ResetServing(lane);
            lane.dishes.Clear();
            if (lane.orderView == null)
            {
                Debug.LogWarning("[CustomerManager] Làn chưa gán OrderView; chạy Tools → HotPot → Setup Customers And References.", this);
                return;
            }
            IReadOnlyList<OrderItemView> items = lane.orderView.Items;
            List<IngredientOrder> groups = order != null && order.ingredientOrders != null ? order.ingredientOrders : new List<IngredientOrder>();
            foreach (IngredientOrder group in groups)
            {
                if (!group.IsValid())
                {
                    Debug.LogWarning("[CustomerManager] Bỏ qua nhóm order có nguyên liệu trống hoặc không hợp lệ.", this);
                    continue;
                }
                if (lane.dishes.Count >= Mathf.Min(items.Count, CustomerOrder.MaxDishes))
                {
                    Debug.LogWarning("[CustomerManager] Order vượt số nhóm được hỗ trợ trên thẻ; bỏ các nhóm cuối.", this);
                    break;
                }
                lane.dishes.Add(new DishProgress(group.dish, group.ingredients));
            }
            BeginServing(lane);
            for (int i = 0; i < items.Count; i++)
            {
                OrderItemView item = items[i];
                if (item == null)
                    continue;
                if (i < lane.dishes.Count)
                    item.ShowDish(lane.dishes[i].dish);
                else
                    item.Hide();
            }
            List<IReadOnlyList<FoodType>> cardRecipes = new List<IReadOnlyList<FoodType>>(lane.dishes.Count);
            foreach (DishProgress dish in lane.dishes)
                cardRecipes.Add(dish.recipe);
            lane.orderView.SetRecipes(cardRecipes);
            lane.orderView.Layout(lane.dishes.Count);
        }

        // EN: Returns the portrait for the customer at a queue index (cycling through the lane's portraits), or null when none are set.
        // VI: Trả về ảnh của khách tại vị trí trong hàng (lặp lại danh sách ảnh của làn), hoặc null nếu chưa có ảnh.
        // EN: Shippers always use char_13; everyone else gets a random portrait from all lanes (shuffle bag: no repeat until all are used, never twice in a row).
        // VI: Khách Shipper luôn dùng char_13; khách khác lấy ảnh ngẫu nhiên từ tất cả các làn (túi xáo trộn: không lặp cho tới khi dùng hết, không trùng hai lần liên tiếp).
        private Sprite GetPortrait(Lane lane, int index)
        {
            if (portraitByCustomer.TryGetValue(index, out Sprite cached))
                return cached;
            bool shipper = index >= 0 && index < customerQueue.Count && customerQueue[index] != null && customerQueue[index].customerType == CustomerType.Shipper;
            Sprite picked = shipper ? FindPortrait(ShipperPortraitName) : null;
            if (picked == null)
                picked = DrawRandomPortrait();
            if (picked != null)
                portraitByCustomer[index] = picked;
            return picked;
        }

        // EN: A Shipper (served alone) stands in the middle between all lanes; other customers stand at their own lane.
        // VI: Shipper (phục vụ một mình) đứng ở giữa các làn; khách khác đứng ở làn của mình.
        // EN: A Shipper (served alone) stands at the centre of the screen with its order card to its right; other customers use their lane layout.
        // VI: Shipper (phục vụ một mình) đứng giữa màn hình, thẻ order nằm lệch bên phải; khách khác dùng bố cục của làn.
        private void ApplySeat(Lane lane)
        {
            if (lane.customer == null) return;
            Transform card = lane.orderView != null ? lane.orderView.transform : null;
            CaptureSeatHomes();
            Vector3 customerHome = lane.customer.HomePosition;
            float offset = 0f;
            float cardOffset = 0f;
            Camera cam = Camera.main;
            if (OnlyServeOneShipper && cam != null && IsLaneBusy(lane) && IsShipper(lane.queue[lane.queueIndex]))
            {
                float portraitCenter = customerHome.x + lane.customer.PortraitCenterOffsetX;
                offset = cam.transform.position.x - portraitCenter;
                if (card != null)
                    cardOffset = cam.transform.position.x + shipperCardGap - cardHomes[card].x;
                // EN: Only the Shipper's serving tray is used: move it to the centre and hide the others.
                // VI: Chỉ dùng khay phục vụ của Shipper: dời ra giữa và ẩn các khay khác.
                foreach (Lane l in lanes)
                {
                    if (l == null || l.servingFood == null) continue;
                    if (l == lane)
                    {
                        Vector3 home = servingHomes[l.servingFood.transform];
                        l.servingFood.transform.position = new Vector3(cam.transform.position.x, home.y, home.z);
                        l.servingFood.gameObject.SetActive(true);
                    }
                    else
                        l.servingFood.gameObject.SetActive(false);
                }
            }
            else
                RestoreServing(lane);
            bool linked = card != null && lane.customer.OrderCard == card;
            lane.customer.SetSeatOffset(offset, linked ? cardOffset : 0f);
            if (card != null && !linked)
                card.position = cardHomes[card] + new Vector3(cardOffset, 0f, 0f);
        }

        // EN: Starts Boss/Shipper countdowns and shows their bars with actual remaining/total seconds; hides bars for untimed customers.
        // VI: Bắt đầu đếm ngược Boss/Shipper và hiện thanh theo số giây còn lại/tổng thực tế; ẩn thanh của khách không tính giờ.
        private void StartServeTimer(Lane lane, CustomerOrder order)
        {
            bool timed = order != null && (order.customerType == CustomerType.Boss || order.customerType == CustomerType.Shipper) && order.timeServe > 0f;
            lane.serveTimeTotal = timed ? order.timeServe : 0f;
            lane.serveTimeLeft = lane.serveTimeTotal;
            CustomerTimerBar bar = timed && lane.orderView != null ? lane.orderView.GetOrCreateTimerBar() : GetTimerBar(lane);
            if (bar == null)
                return;
            if (timed)
                bar.Show(lane.serveTimeLeft, lane.serveTimeTotal);
            else
                bar.Hide();
        }

        // EN: Completes departure, publishes the remaining count and reserves the next customer.
        // VI: Hoàn tất khách rời đi, thông báo số khách còn lại rồi lấy khách kế tiếp.
        private IEnumerator FinishCustomer(Lane lane)
        {
            lane.ordering = false;
            StopServeTimer(lane);
            PlayPraise(lane.customer);
            IngameSoundManager.Instance?.PlayCustomerHappy();
            if (Config.customers.leaveDelay > 0f)
                yield return new WaitForSeconds(Config.customers.leaveDelay);
            if (lane.orderView != null)
            {
                float hide = lane.orderView.PlayHide();
                if (hide > 0f)
                    yield return new WaitForSeconds(hide);
            }
            if (lane.customer != null)
                yield return lane.customer.SlideOut(lane.slideDirection * Config.customers.slideDistance, Config.customers.slideDuration);
            ResetServing(lane);
            if (IsShipper(lane.queue[lane.queueIndex])) RestoreServing(null);
            lane.queueIndex++;
            completedCustomers++;
            RemainingCustomersChanged?.Invoke(RemainingCustomers);
            if (AssignNextCustomer(lane))
            {
                FillIdleLanes();
                yield return StepQueueForward(lane);
                yield return BringNextCustomer(lane);
            }
            else
            {
                if (lane.customer != null)
                    lane.customer.SetVisible(false);
                FillIdleLanes();
                CheckAllServed();
            }
        }

        // EN: Makes sure the lane has a figure per waiting customer, creating a plain sprite only when the pool (scene figures first) is too small.
        // VI: Bảo đảm làn có đủ hình cho mỗi khách đang chờ, chỉ tạo sprite mới khi pool (ưu tiên hình đặt sẵn trong scene) không đủ.
        private void EnsureFigures(Lane lane, int count)
        {
            while (lane.figures.Count < count)
            {
                SpriteRenderer figure = new GameObject($"Waiting customer {lane.figures.Count + 1}").AddComponent<SpriteRenderer>();
                figure.transform.SetParent(transform, true);
                lane.figures.Add(figure);
            }
        }

        // EN: World position of the k-th waiting spot (0 = right behind the counter), extending the scene spots by the last step.
        // VI: Vị trí world của chỗ chờ thứ k (0 = ngay sau quầy), nối dài các chỗ trong scene theo bước cuối cùng.
        private static Vector3 GetQueueSpot(Lane lane, int k)
        {
            if (k < lane.spots.Count)
                return lane.spots[k];
            Vector3 last = lane.spots.Count > 0 ? lane.spots[lane.spots.Count - 1] : lane.queueOrigin;
            return last + lane.spotStep * (k - lane.spots.Count + 1);
        }

        // EN: Sets a transform's local scale so its world scale matches the given value.
        // VI: Đặt scale cục bộ để scale world của transform bằng giá trị cho trước.
        private static void SetWorldScale(Transform target, Vector3 worldScale)
        {
            Vector3 parent = target.parent != null ? target.parent.lossyScale : Vector3.one;
            target.localScale = new Vector3(worldScale.x / Mathf.Max(0.0001f, parent.x), worldScale.y / Mathf.Max(0.0001f, parent.y), 1f);
        }

        // EN: Gives the lane's serving component its order view and recipes, and routes arrival callbacks to customer progress.
        // VI: Giao thẻ order cùng công thức cho component phục vụ của làn và chuyển callback đáp món về tiến độ khách.
        private void BeginServing(Lane lane)
        {
            if (lane.servingFood == null)
                return;
            List<FinalFoodView> dishes = new List<FinalFoodView>(lane.dishes.Count);
            List<IReadOnlyList<FoodType>> recipes = new List<IReadOnlyList<FoodType>>(lane.dishes.Count);
            foreach (DishProgress dish in lane.dishes)
            {
                dishes.Add(dish.dish);
                recipes.Add(dish.recipe);
            }
            lane.servingFood.BeginOrder(lane.orderView, dishes, recipes,
                dish => OnIngredientDelivered(lane, dish), dish => OnDishDelivered(lane, dish));
        }

        // EN: Finds a portrait by sprite name across all lanes.
        // VI: Tìm ảnh theo tên sprite trong tất cả các làn.
        private Sprite FindPortrait(string spriteName)
        {
            foreach (Lane l in lanes)
                if (l != null && l.portraits != null)
                    foreach (Sprite s in l.portraits)
                        if (s != null && s.name == spriteName) return s;
            return null;
        }

        // EN: Takes the next portrait from a shuffled bag of all non-shipper portraits, refilling it when empty.
        // VI: Lấy ảnh kế tiếp từ túi đã xáo trộn gồm mọi ảnh (trừ ảnh Shipper), nạp lại khi hết.
        private Sprite DrawRandomPortrait()
        {
            if (portraitBag.Count == 0)
            {
                foreach (Lane l in lanes)
                    if (l != null && l.portraits != null)
                        foreach (Sprite s in l.portraits)
                            if (s != null && s.name != ShipperPortraitName && !portraitBag.Contains(s)) portraitBag.Add(s);
                for (int i = portraitBag.Count - 1; i > 0; i--)
                {
                    int j = UnityEngine.Random.Range(0, i + 1);
                    (portraitBag[i], portraitBag[j]) = (portraitBag[j], portraitBag[i]);
                }
                if (portraitBag.Count > 1 && portraitBag[portraitBag.Count - 1] == lastRandomPortrait)
                    (portraitBag[0], portraitBag[portraitBag.Count - 1]) = (portraitBag[portraitBag.Count - 1], portraitBag[0]);
            }
            if (portraitBag.Count == 0)
                return null;
            Sprite picked = portraitBag[portraitBag.Count - 1];
            portraitBag.RemoveAt(portraitBag.Count - 1);
            lastRandomPortrait = picked;
            return picked;
        }

        // EN: Records lane layouts once (cards, serving trays) and the gap a Shipper's card keeps: the smallest normal customer-to-card distance.
        // VI: Ghi bố cục các làn một lần (thẻ, khay phục vụ) và khoảng cách thẻ của Shipper: khoảng cách khách–thẻ nhỏ nhất của bố cục thường.
        private void CaptureSeatHomes()
        {
            if (seatHomesCaptured) return;
            seatHomesCaptured = true;
            shipperCardGap = float.MaxValue;
            foreach (Lane l in lanes)
            {
                if (l == null) continue;
                if (l.servingFood != null) servingHomes[l.servingFood.transform] = l.servingFood.transform.position;
                if (l.orderView == null) continue;
                Transform card = l.orderView.transform;
                cardHomes[card] = card.position;
                if (l.customer != null)
                    shipperCardGap = Mathf.Min(shipperCardGap, Mathf.Abs(card.position.x - (l.customer.HomePosition.x + l.customer.PortraitCenterOffsetX)));
            }
            if (shipperCardGap == float.MaxValue) shipperCardGap = 0f;
        }

        // EN: Puts a lane's serving tray back at its own spot and shows every tray again.
        // VI: Trả khay phục vụ của làn về chỗ cũ và hiện lại mọi khay.
        private void RestoreServing(Lane lane)
        {
            foreach (Lane l in lanes)
            {
                if (l == null || l.servingFood == null) continue;
                if (servingHomes.TryGetValue(l.servingFood.transform, out Vector3 home) && (l == lane || lane == null))
                    l.servingFood.transform.position = home;
                if (!l.servingFood.gameObject.activeSelf) l.servingFood.gameObject.SetActive(true);
            }
        }

        // EN: Pops a random praise word ("Delicious!"...) above the customer, at the praise anchor.
        // VI: Bắn một chữ khen ngẫu nhiên ("Delicious!"...) phía trên khách, tại điểm neo chữ khen của khách.
        private void PlayPraise(CustomerView customer)
        {
            if (customer == null)
                return;
            if (praiseText == null)
                praiseText = new PraiseText(transform);
            SpriteRenderer portrait = customer.Portrait;
            int sortingLayer = portrait != null ? portrait.sortingLayerID : 0;
            int sortingOrder = (portrait != null ? portrait.sortingOrder : 0) + Config.ingredientFlight.serveSortingBoost + 5;
            StartCoroutine(praiseText.Play(Config.praiseText, customer.PraisePosition, sortingLayer, sortingOrder));
        }

        // EN: Seats waiting customers at lanes that were left empty (e.g. blocked while a Shipper was served alone).
        // VI: Đưa khách đang chờ vào các làn đang trống (vd. bị chặn trong lúc Shipper được phục vụ một mình).
        private void FillIdleLanes()
        {
            foreach (Lane other in lanes)
                if (other != null && AssignNextCustomer(other))
                    StartCoroutine(StepAndBring(other));
        }

        // EN: Animates a dedicated portrait from the shared queue to this seat without sharing walkers.
        // VI: Đưa hình khách riêng từ hàng chung tới chỗ này, không dùng chung hình đang di chuyển.
        private IEnumerator StepQueueForward(Lane lane)
        {
            SpriteRenderer active = lane.customer != null ? lane.customer.Portrait : null;
            if (waitingLane == null || active == null)
                yield break;
            if (lane.walker == null)
            {
                lane.walker = new GameObject("Arriving customer").AddComponent<SpriteRenderer>();
                lane.walker.transform.SetParent(transform, true);
            }
            SpriteRenderer walker = lane.walker;
            walker.sprite = GetPortrait(waitingLane, lane.customerNumber);
            walker.sortingLayerID = active.sortingLayerID;
            walker.transform.position = GetQueueSpot(waitingLane, 0);
            SetWorldScale(walker.transform, active.transform.lossyScale * Config.customers.waitingScale);
            walker.gameObject.SetActive(true);
            LayoutWaitingQueue(waitingLane, 0f);
            ApplySeat(lane);
            lane.customer.SnapHome();
            lane.customer.SetVisible(false);
            Vector3 from = walker.transform.position;
            Vector3 fromScale = walker.transform.lossyScale;
            Vector3 to = active.transform.position;
            Vector3 toScale = active.transform.lossyScale;
            walker.sortingOrder = active.sortingOrder;
            float time = 0f;
            while (time < Config.customers.stepForwardDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / Config.customers.stepForwardDuration));
                walker.transform.position = Vector3.LerpUnclamped(from, to, t) + Vector3.up * (0.15f * Mathf.Sin(t * Mathf.PI));
                SetWorldScale(walker.transform, Vector3.LerpUnclamped(fromScale, toScale, t));
                walker.color = Color.Lerp(Config.customers.waitingTint, Color.white, t);
                yield return null;
            }
            walker.gameObject.SetActive(false);
        }

        // EN: Records one ingredient arrival and requests assembly only when the recipe has fully landed.
        // VI: Ghi nhận một nguyên liệu đáp xuống và chỉ yêu cầu ghép khi toàn bộ công thức đã tới nơi.
        private void OnIngredientDelivered(Lane lane, int dish)
        {
            if (!lane.ordering || dish < 0 || dish >= lane.dishes.Count)
                return;
            DishProgress progress = lane.dishes[dish];
            progress.Arrive();
            if (progress.Done)
                lane.servingFood.CompleteDish(dish);
        }

        // EN: Marks dish delivery, records a completed customer order only after every dish arrives, then starts departure.
        // VI: Đánh dấu giao món, chỉ ghi nhận order khách hoàn tất khi đủ mọi món rồi bắt đầu rời quầy.
        private void OnDishDelivered(Lane lane, int dish)
        {
            if (!lane.ordering || dish < 0 || dish >= lane.dishes.Count || lane.dishes[dish].assembled)
                return;
            lane.dishes[dish].assembled = true;
            if (!IsOrderAssembled(lane))
            {
                IngameSoundManager.Instance?.PlayPrepared();
                return;
            }
            HasCompletedOrder = true;
            StartCoroutine(FinishCustomer(lane));
        }

        private IEnumerator StepAndBring(Lane lane)
        {
            yield return StepQueueForward(lane);
            yield return BringNextCustomer(lane);
        }

        // EN: True when every dish of the lane's current customer has been assembled on the tray.
        // VI: True khi mọi món của khách hiện tại trong làn đã được ghép xong trên khay.
        private static bool IsOrderAssembled(Lane lane)
        {
            foreach (DishProgress dish in lane.dishes)
            {
                if (!dish.assembled)
                    return false;
            }
            return true;
        }

        // EN: Clears the static singleton when entering Play Mode, including when domain reload is disabled.
        // VI: Xóa singleton tĩnh khi vào Play Mode, kể cả khi tắt tải lại miền mã.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            instance = null;
        }

    }
}
