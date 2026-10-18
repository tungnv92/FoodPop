using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Scene singleton that runs the customer queues: loads orders from the LevelConfig, sends prepared food from the belt to the customer who needs it (first come, first served), ticks off completed lines and swaps in the next customer of each lane.
    // VI: Singleton của scene điều khiển hàng khách: nạp order từ LevelConfig, đưa món đã sơ chế trên băng tới khách cần (ai đến trước phục vụ trước), đánh dấu dòng đã đủ và đổi sang khách tiếp theo của từng làn.
    [DisallowMultipleComponent]
    public class CustomerManager : MonoBehaviour
    {
        // EN: One counter lane: the customer spot, its order card lines, the waiting figures behind it and the portraits used for its customers.
        // VI: Một làn quầy: vị trí khách, các dòng của thẻ order, các khách đứng chờ phía sau và danh sách ảnh dùng cho khách của làn.
        [Serializable]
        private class Lane
        {
            [Tooltip("Khách đang đứng ở làn này (kèm thẻ order).")]
            public CustomerView customer;
            [Tooltip("Các dòng của thẻ order, từ trên xuống.")]
            public OrderItemView[] orderItems = new OrderItemView[0];
            [Tooltip("Ảnh các khách đứng chờ đặt sẵn trong scene (người gần quầy nhất trước); dùng làm vị trí hàng chờ và làm mẫu khi cần thêm người.")]
            public SpriteRenderer[] waitingFigures = new SpriteRenderer[0];
            [Tooltip("Ảnh khách dùng lần lượt cho các khách của làn (lặp lại khi hết).")]
            public Sprite[] portraits = new Sprite[0];
            [Tooltip("Hướng trượt ra/vào: -1 = mép trái màn hình, 1 = mép phải.")]
            public int slideDirection = -1;

            [NonSerialized] public List<CustomerOrder> queue;
            [NonSerialized] public int queueIndex;
            [NonSerialized] public bool ordering;
            [NonSerialized] public long arrivalStamp;
            [NonSerialized] public readonly List<FoodType> lineTypes = new List<FoodType>();
            [NonSerialized] public readonly List<int> remaining = new List<int>();
            [NonSerialized] public readonly List<int> incoming = new List<int>();
            [NonSerialized] public readonly List<SpriteRenderer> figures = new List<SpriteRenderer>();
            [NonSerialized] public readonly List<Vector3> spots = new List<Vector3>();
            [NonSerialized] public Vector3 spotStep;
            [NonSerialized] public Vector3 queueOrigin;
            [NonSerialized] public bool queueCaptured;
        }

        // EN: One food flying from a belt slot to an order line.
        // VI: Một món đang bay từ slot băng chuyền tới một dòng order.
        private sealed class ServeFlight
        {
            public Lane lane;
            public int line;
            public SpriteRenderer flyer;
            public Vector3 start;
            public Vector3 startScale;
        }

        private static CustomerManager instance;

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

        [Header("Tham chiếu")]
        [Tooltip("Băng chuyền chứa các slot món.")]
        [SerializeField] private ConveyorChefMovement conveyor;
        [Tooltip("Bảng icon nguyên liệu (dùng ảnh đã sơ chế cho thẻ order).")]
        [SerializeField] private FoodIconConfig foodIconConfig;
        [Tooltip("Làn trái (phần tử 0) và làn phải (phần tử 1).")]
        [SerializeField] private Lane[] lanes = new Lane[2];

        [Header("Món bay tới khách")]
        [Tooltip("Thời gian (giây) món bay từ băng chuyền tới thẻ order.")]
        [Min(0.05f)] [SerializeField] private float serveFlyDuration = 0.55f;
        [Tooltip("Độ cao đỉnh vòng cung khi bay (đơn vị world).")]
        [Min(0f)] [SerializeField] private float serveArcHeight = 2f;
        [Tooltip("Hệ số phóng to ở đỉnh vòng cung.")]
        [Min(1f)] [SerializeField] private float servePeakScale = 1.3f;
        [Tooltip("Cộng thêm vào thứ tự vẽ để món bay nằm trên thẻ order.")]
        [SerializeField] private int serveSortingBoost = 40;
        [Tooltip("Âm thanh khi món chạm thẻ order.")]
        [SerializeField] private AudioClip serveSound;
        [Range(0f, 1f)] [SerializeField] private float serveVolume = 0.6f;
        [Tooltip("Khoảng cách tối thiểu (giây) giữa hai lần phát âm chạm; các món chạm sát nhau chỉ phát một tiếng.")]
        [Min(0f)] [SerializeField] private float serveSoundInterval = 0.12f;
        [Tooltip("Các món chạm liên tiếp trong khoảng này (giây) được tính là một chuỗi: tiếng sau cao dần và nhỏ dần.")]
        [Min(0f)] [SerializeField] private float serveComboWindow = 0.5f;
        [Tooltip("Mức tăng cao độ cho mỗi tiếng trong chuỗi (0.06 = cao thêm 6%).")]
        [Range(0f, 0.2f)] [SerializeField] private float serveComboPitchStep = 0.06f;

        [Header("Hoàn thành")]
        [Tooltip("Hiệu ứng tim bắn ra tại khách khi đủ cả order.")]
        [SerializeField] private ParticleEffect heartEffectPrefab;
        [Tooltip("Âm thanh khách vui khi nhận đủ order.")]
        [SerializeField] private AudioClip customerHappySound;
        [Range(0f, 1f)] [SerializeField] private float customerHappyVolume = 0.9f;

        [Header("Khách ra/vào")]
        [Tooltip("Thời gian (giây) chờ sau khi bắn tim rồi khách mới rời đi.")]
        [Min(0f)] [SerializeField] private float leaveDelay = 0.6f;
        [Tooltip("Thời gian (giây) khách trượt ra hoặc trượt vào.")]
        [Min(0.05f)] [SerializeField] private float slideDuration = 0.45f;
        [Tooltip("Quãng đường (đơn vị world) trượt sang ngang để khách ra khỏi màn hình.")]
        [Min(0f)] [SerializeField] private float slideDistance = 9f;

        [Header("Hàng chờ")]
        [Tooltip("Màu phủ cho khách chưa tới lượt order (tối hơn khách đang order).")]
        [SerializeField] private Color waitingTint = new Color(0.55f, 0.55f, 0.6f, 1f);
        [Tooltip("Kích thước khách chưa tới lượt so với khách đang order (0.8 = nhỏ hơn 20%).")]
        [Range(0.3f, 1f)] [SerializeField] private float waitingScale = 0.8f;
        [Tooltip("Thời gian (giây) khách đầu hàng bước lên quầy và cả hàng dồn lên.")]
        [Min(0.05f)] [SerializeField] private float stepForwardDuration = 0.4f;
        [Tooltip("Khoảng cách (world) giữa hai người trong hàng khi scene không đặt sẵn khách chờ: x dịch ra phía ngoài làn, y dịch lên phía sau.")]
        [SerializeField] private Vector2 queueSpacing = new Vector2(0.8f, 0.45f);

        private readonly List<ServeFlight> flights = new List<ServeFlight>();
        private readonly Stack<SpriteRenderer> flyerPool = new Stack<SpriteRenderer>();
        private Transform flyerRoot;
        private long arrivalCounter;
        private bool allServedLogged;
        private float lastServeSoundTime = -10f;
        private int serveCombo;

        // EN: Clears the static singleton when entering Play Mode, including when domain reload is disabled.
        // VI: Xóa singleton tĩnh khi vào Play Mode, kể cả khi tắt tải lại miền mã.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            instance = null;
        }

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

        // EN: Releases the singleton and the flyer container.
        // VI: Giải phóng singleton và object chứa món bay.
        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
            if (flyerRoot != null)
                Destroy(flyerRoot.gameObject);
        }

        // EN: Each frame, sends every prepared food on the belt to the earliest customer still needing it.
        // VI: Mỗi frame, gửi mọi món đã sơ chế trên băng tới khách đến sớm nhất còn cần món đó.
        private void Update()
        {
            if (conveyor == null)
                return;
            IReadOnlyList<ConveyorSlot> slots = conveyor.Slots;
            for (int i = 0; i < slots.Count; i++)
                TryServeFromSlot(slots[i]);
        }

        // EN: Loads both queues from the level and brings in the first customer of each lane.
        // VI: Nạp hai hàng khách từ level và đưa khách đầu tiên của mỗi làn vào.
        public void LoadCustomers(LevelConfig config)
        {
            if (config == null)
                return;
            StopAllCoroutines();
            CancelFlights();
            allServedLogged = false;
            for (int side = 0; side < lanes.Length; side++)
            {
                Lane lane = lanes[side];
                if (lane == null)
                    continue;
                lane.queue = config.GetQueue(side);
                lane.queueIndex = 0;
                lane.ordering = false;
                CaptureQueueSpots(lane);
                LayoutWaitingQueue(lane, 0f);
                if (lane.queue != null && lane.queue.Count > 0)
                    StartCoroutine(BringNextCustomer(lane));
                else if (lane.customer != null)
                    lane.customer.SetVisible(false);
            }
            CheckAllServed();
        }

        // EN: Sends the slot's prepared food to a customer when one still needs that type.
        // VI: Gửi món đã sơ chế trên slot tới khách nếu còn khách cần loại đó.
        private void TryServeFromSlot(ConveyorSlot slot)
        {
            if (slot == null || slot.isEmpty || slot.IsFoodArriving || slot.food == null || !slot.food.IsPrepared)
                return;
            if (!TryFindCustomer(slot.food.foodType, out Lane lane, out int line))
                return;
            LaunchServe(slot, lane, line);
        }

        // EN: True when some ordering customer still needs this food type (counting food already flying to them).
        // VI: True khi còn khách đang order cần loại món này (đã tính món đang bay tới).
        public bool TryFindCustomer(FoodType type) => TryFindCustomer(type, out _, out _);

        // EN: True once every customer of the level has been served.
        // VI: True khi mọi khách của level đã được phục vụ.
        public bool AllServed => allServedLogged;

        // EN: Finds the ordering lane whose customer arrived first and still needs the food type (counting food already flying to them).
        // VI: Tìm làn đang order có khách đến sớm nhất và vẫn cần loại món (đã tính món đang bay tới).
        private bool TryFindCustomer(FoodType type, out Lane best, out int bestLine)
        {
            best = null;
            bestLine = -1;
            foreach (Lane lane in lanes)
            {
                if (lane == null || !lane.ordering || (best != null && lane.arrivalStamp >= best.arrivalStamp))
                    continue;
                int line = lane.lineTypes.IndexOf(type);
                if (line < 0 || lane.remaining[line] - lane.incoming[line] <= 0)
                    continue;
                best = lane;
                bestLine = line;
            }
            return best != null;
        }

        // EN: Takes the food off the belt and flies its sprite along an arc to the order line.
        // VI: Lấy món khỏi băng chuyền và cho sprite của món bay theo vòng cung tới dòng order.
        private void LaunchServe(ConveyorSlot slot, Lane lane, int line)
        {
            SpriteRenderer source = slot.IngredientRenderer;
            OrderItemView target = lane.orderItems[line];
            int sortingOrder = Mathf.Max(source != null ? source.sortingOrder : 0,
                target.IngredientRenderer != null ? target.IngredientRenderer.sortingOrder : 0) + serveSortingBoost;
            SpriteRenderer flyer = RentFlyer(source != null ? source.sprite : null,
                source != null ? source.sortingLayerID : 0, sortingOrder);
            ServeFlight flight = new ServeFlight
            {
                lane = lane,
                line = line,
                flyer = flyer,
                start = slot.LandingPosition,
                startScale = slot.LandingScale
            };
            lane.incoming[line]++;
            slot.ClearFood();
            flights.Add(flight);
            StartCoroutine(FlyToOrder(flight, target));
        }

        // EN: Moves the flyer along a parabolic arc toward the order line, then delivers it.
        // VI: Di chuyển món bay theo vòng cung parabol tới dòng order rồi giao món.
        private IEnumerator FlyToOrder(ServeFlight flight, OrderItemView target)
        {
            Transform flyer = flight.flyer.transform;
            flyer.SetPositionAndRotation(flight.start, Quaternion.identity);
            flyer.localScale = flight.startScale;
            flight.flyer.gameObject.SetActive(true);
            float time = 0f;
            while (time < serveFlyDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / serveFlyDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                Vector3 position = Vector3.LerpUnclamped(flight.start, target.TargetPosition, eased);
                position.y += serveArcHeight * 4f * t * (1f - t);
                flyer.position = position;
                float pop = 1f + (servePeakScale - 1f) * Mathf.Sin(t * Mathf.PI);
                flyer.localScale = Vector3.LerpUnclamped(flight.startScale, target.TargetScale, eased) * pop;
                yield return null;
            }
            flights.Remove(flight);
            ReturnFlyer(flight.flyer);
            DeliverFood(flight.lane, flight.line);
        }

        // EN: Counts one delivered food: updates the line, ticks it when complete and finishes the customer when every line is complete.
        // VI: Tính một món đã giao: cập nhật dòng, đánh dấu tích khi đủ và kết thúc khách khi mọi dòng đã đủ.
        private void DeliverFood(Lane lane, int line)
        {
            lane.incoming[line] = Mathf.Max(0, lane.incoming[line] - 1);
            lane.remaining[line] = Mathf.Max(0, lane.remaining[line] - 1);
            OrderItemView item = lane.orderItems[line];
            PlayServeSound();
            if (lane.remaining[line] > 0)
            {
                item.SetRemaining(lane.remaining[line]);
                return;
            }
            item.MarkDone();
            if (IsOrderComplete(lane))
                StartCoroutine(FinishCustomer(lane));
        }

        // EN: Plays the landing pop at most once per serveSoundInterval; pops close together form a combo that rises in pitch and softens, instead of stacking into one harsh sound.
        // VI: Phát tiếng chạm tối đa một lần mỗi serveSoundInterval; các tiếng sát nhau thành chuỗi cao dần và nhỏ dần, thay vì chồng lên nhau gây chói.
        private void PlayServeSound()
        {
            float now = Time.time;
            float sinceLast = now - lastServeSoundTime;
            if (sinceLast < serveSoundInterval)
                return;
            serveCombo = sinceLast <= serveComboWindow ? Mathf.Min(serveCombo + 1, 6) : 0;
            lastServeSoundTime = now;
            float pitch = 1f + serveComboPitchStep * serveCombo;
            float volume = serveVolume / (1f + 0.15f * serveCombo);
            OneShotEffects.PlaySoundAtPitch(serveSound, volume, pitch);
        }

        // EN: True when every line of the lane's current order has been fully delivered.
        // VI: True khi mọi dòng trong order hiện tại của làn đã được giao đủ.
        private static bool IsOrderComplete(Lane lane)
        {
            foreach (int count in lane.remaining)
            {
                if (count > 0)
                    return false;
            }
            return true;
        }

        // EN: Rewards the customer with hearts, slides them out sideways and brings in the next customer of the lane.
        // VI: Thưởng khách bằng hiệu ứng tim, cho khách trượt ngang ra ngoài và đưa khách tiếp theo của làn vào.
        private IEnumerator FinishCustomer(Lane lane)
        {
            lane.ordering = false;
            PlayHearts(lane.customer);
            OneShotEffects.PlaySound(customerHappySound, customerHappyVolume, 0f);
            if (leaveDelay > 0f)
                yield return new WaitForSeconds(leaveDelay);
            if (lane.customer != null)
                yield return lane.customer.SlideOut(lane.slideDirection * slideDistance, slideDuration);
            lane.queueIndex++;
            if (lane.queueIndex < lane.queue.Count)
            {
                yield return StepQueueForward(lane);
                yield return BringNextCustomer(lane);
            }
            else
            {
                if (lane.customer != null)
                    lane.customer.SetVisible(false);
                CheckAllServed();
            }
        }

        // EN: Shows the lane's next customer with their order, sliding in from the side when animate is true.
        // VI: Hiện khách tiếp theo của làn cùng order, trượt vào từ cạnh màn hình nếu animate là true.
        private IEnumerator BringNextCustomer(Lane lane)
        {
            CustomerOrder order = lane.queue[lane.queueIndex];
            SetupOrder(lane, order);
            if (lane.customer != null)
            {
                lane.customer.SetPortrait(GetPortrait(lane, lane.queueIndex));
                lane.customer.SnapHome();
                lane.customer.SetVisible(true);
            }
            LayoutWaitingQueue(lane, 0f);
            yield return null;
            lane.arrivalStamp = ++arrivalCounter;
            lane.ordering = true;
        }

        // EN: Fills the order card lines from the order (merging repeated food types) and hides unused lines.
        // VI: Điền các dòng thẻ order từ order (gộp các dòng trùng loại món) và ẩn các dòng không dùng.
        private void SetupOrder(Lane lane, CustomerOrder order)
        {
            lane.lineTypes.Clear();
            lane.remaining.Clear();
            lane.incoming.Clear();
            if (order?.foods != null)
            {
                foreach (OrderFood food in order.foods)
                    AddOrderLine(lane, food);
            }
            if (lane.lineTypes.Count > lane.orderItems.Length)
                Debug.LogWarning($"[CustomerManager] Order có {lane.lineTypes.Count} món nhưng thẻ chỉ có {lane.orderItems.Length} dòng; bỏ bớt các món cuối.", this);
            while (lane.lineTypes.Count > lane.orderItems.Length)
            {
                int last = lane.lineTypes.Count - 1;
                lane.lineTypes.RemoveAt(last);
                lane.remaining.RemoveAt(last);
                lane.incoming.RemoveAt(last);
            }
            for (int i = 0; i < lane.orderItems.Length; i++)
            {
                OrderItemView item = lane.orderItems[i];
                if (item == null)
                    continue;
                if (i < lane.lineTypes.Count)
                    item.Show(GetOrderIcon(lane.lineTypes[i]), lane.remaining[i]);
                else
                    item.Hide();
            }
        }

        // EN: Adds one order entry as a line, or adds its count to an existing line of the same food.
        // VI: Thêm một mục order thành một dòng, hoặc cộng số lượng vào dòng cùng loại món đã có.
        private static void AddOrderLine(Lane lane, OrderFood food)
        {
            if (food == null || food.foodType == FoodType.None || food.requiredCount <= 0)
                return;
            int existing = lane.lineTypes.IndexOf(food.foodType);
            if (existing >= 0)
            {
                lane.remaining[existing] += food.requiredCount;
                return;
            }
            lane.lineTypes.Add(food.foodType);
            lane.remaining.Add(food.requiredCount);
            lane.incoming.Add(0);
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
                lane.spotStep = new Vector3(lane.slideDirection * queueSpacing.x, queueSpacing.y, 0f);
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

        // EN: Places every waiting customer of the lane on its spot: smaller, darker and drawn behind the ordering customer, with each person further back drawn behind the one in front. stepT (0..1) blends from the previous spot for the walk-forward animation.
        // VI: Đặt mọi khách đang chờ của làn vào chỗ của mình: nhỏ hơn, tối hơn và vẽ sau khách đang order, người đứng sau vẽ sau người phía trước. stepT (0..1) nội suy từ chỗ cũ để tạo hoạt cảnh dồn lên.
        private void LayoutWaitingQueue(Lane lane, float stepT)
        {
            int waiting = lane.queue != null ? Mathf.Max(0, lane.queue.Count - lane.queueIndex - 1) : 0;
            EnsureFigures(lane, waiting);
            SpriteRenderer active = lane.customer != null ? lane.customer.Portrait : null;
            for (int k = 0; k < lane.figures.Count; k++)
            {
                SpriteRenderer figure = lane.figures[k];
                bool used = k < waiting;
                figure.gameObject.SetActive(used);
                if (!used)
                    continue;
                Sprite portrait = GetPortrait(lane, lane.queueIndex + 1 + k);
                if (portrait != null)
                    figure.sprite = portrait;
                figure.color = waitingTint;
                Vector3 target = GetQueueSpot(lane, k);
                figure.transform.position = stepT > 0f ? Vector3.LerpUnclamped(GetQueueSpot(lane, k + 1), target, stepT) : target;
                if (active != null)
                {
                    SetWorldScale(figure.transform, active.transform.lossyScale * waitingScale);
                    figure.sortingLayerID = active.sortingLayerID;
                    figure.sortingOrder = active.sortingOrder - 1 - k;
                }
            }
        }

        // EN: Walks the queue forward one place: the first waiting customer moves to the counter while growing and brightening, the rest move up one spot.
        // VI: Cho hàng chờ dồn lên một chỗ: khách đầu hàng đi tới quầy, to dần và sáng dần, những người còn lại tiến lên một chỗ.
        private IEnumerator StepQueueForward(Lane lane)
        {
            SpriteRenderer active = lane.customer != null ? lane.customer.Portrait : null;
            if (lane.figures.Count == 0 || active == null)
                yield break;
            SpriteRenderer walker = lane.figures[0];
            lane.figures.RemoveAt(0);
            lane.figures.Add(walker);
            lane.customer.SnapHome();
            lane.customer.SetVisible(false);
            Vector3 from = walker.transform.position;
            Vector3 fromScale = walker.transform.lossyScale;
            Vector3 to = active.transform.position;
            Vector3 toScale = active.transform.lossyScale;
            walker.sortingOrder = active.sortingOrder;
            float time = 0f;
            while (time < stepForwardDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(time / stepForwardDuration));
                walker.transform.position = Vector3.LerpUnclamped(from, to, t) + Vector3.up * (0.15f * Mathf.Sin(t * Mathf.PI));
                SetWorldScale(walker.transform, Vector3.LerpUnclamped(fromScale, toScale, t));
                walker.color = Color.Lerp(waitingTint, Color.white, t);
                LayoutQueueBehind(lane, t);
                yield return null;
            }
            walker.gameObject.SetActive(false);
        }

        // EN: Moves the remaining waiting customers (all but the one walking to the counter) one spot forward at progress t.
        // VI: Đưa các khách chờ còn lại (trừ người đang đi tới quầy) tiến lên một chỗ theo tiến độ t.
        private void LayoutQueueBehind(Lane lane, float t)
        {
            int waiting = lane.queue != null ? Mathf.Max(0, lane.queue.Count - lane.queueIndex - 1) : 0;
            for (int k = 0; k < waiting && k < lane.figures.Count - 1; k++)
            {
                SpriteRenderer figure = lane.figures[k];
                figure.transform.position = Vector3.LerpUnclamped(GetQueueSpot(lane, k + 1), GetQueueSpot(lane, k), t);
            }
        }

        // EN: Returns the portrait for the customer at a queue index (cycling through the lane's portraits), or null when none are set.
        // VI: Trả về ảnh của khách tại vị trí trong hàng (lặp lại danh sách ảnh của làn), hoặc null nếu chưa có ảnh.
        private static Sprite GetPortrait(Lane lane, int index)
        {
            return lane.portraits.Length > 0 ? lane.portraits[index % lane.portraits.Length] : null;
        }

        // EN: Sets a transform's local scale so its world scale matches the given value.
        // VI: Đặt scale cục bộ để scale world của transform bằng giá trị cho trước.
        private static void SetWorldScale(Transform target, Vector3 worldScale)
        {
            Vector3 parent = target.parent != null ? target.parent.lossyScale : Vector3.one;
            target.localScale = new Vector3(worldScale.x / Mathf.Max(0.0001f, parent.x), worldScale.y / Mathf.Max(0.0001f, parent.y), 1f);
        }

        // EN: Plays the heart burst just above the customer's portrait.
        // VI: Bắn hiệu ứng tim ngay phía trên ảnh khách.
        private void PlayHearts(CustomerView customer)
        {
            if (customer == null)
                return;
            SpriteRenderer portrait = customer.Portrait;
            int sortingLayer = portrait != null ? portrait.sortingLayerID : 0;
            int sortingOrder = (portrait != null ? portrait.sortingOrder : 0) + serveSortingBoost;
            OneShotEffects.PlayParticle(heartEffectPrefab, customer.HeartPosition, sortingLayer, sortingOrder);
        }

        // EN: Logs once when no lane has a customer left to serve.
        // VI: Ghi log một lần khi không còn làn nào có khách cần phục vụ.
        private void CheckAllServed()
        {
            if (allServedLogged)
                return;
            foreach (Lane lane in lanes)
            {
                if (lane != null && lane.queue != null && lane.queueIndex < lane.queue.Count)
                    return;
            }
            allServedLogged = true;
            Debug.Log("[CustomerManager] Đã phục vụ hết khách của level.", this);
        }

        // EN: Returns the prepared sprite of a food for the order card (raw sprite when no prepared one exists).
        // VI: Trả về ảnh đã sơ chế của món cho thẻ order (dùng ảnh thô nếu chưa có ảnh sơ chế).
        private Sprite GetOrderIcon(FoodType type)
        {
            return foodIconConfig != null ? foodIconConfig.GetIcon(type, FoodState.Prepared) : null;
        }

        // EN: Takes a flyer sprite from the pool (creating one only when the pool is empty) and applies the sprite and sorting.
        // VI: Lấy sprite bay từ pool (chỉ tạo mới khi pool rỗng) và gán sprite cùng thứ tự vẽ.
        private SpriteRenderer RentFlyer(Sprite sprite, int sortingLayer, int sortingOrder)
        {
            SpriteRenderer flyer = flyerPool.Count > 0 ? flyerPool.Pop() : null;
            if (flyer == null)
            {
                if (flyerRoot == null)
                    flyerRoot = new GameObject("ServeFlyers").transform;
                GameObject go = new GameObject("Served food");
                go.transform.SetParent(flyerRoot, false);
                flyer = go.AddComponent<SpriteRenderer>();
            }
            flyer.sprite = sprite;
            flyer.sortingLayerID = sortingLayer;
            flyer.sortingOrder = sortingOrder;
            flyer.gameObject.SetActive(false);
            return flyer;
        }

        // EN: Hides a flyer and keeps it for reuse.
        // VI: Ẩn sprite bay và giữ lại để tái sử dụng.
        private void ReturnFlyer(SpriteRenderer flyer)
        {
            if (flyer == null)
                return;
            flyer.gameObject.SetActive(false);
            flyerPool.Push(flyer);
        }

        // EN: Drops every flight in progress (used when a level is reloaded).
        // VI: Hủy mọi món đang bay (dùng khi nạp lại level).
        private void CancelFlights()
        {
            foreach (ServeFlight flight in flights)
                ReturnFlyer(flight.flyer);
            flights.Clear();
        }
    }
}
