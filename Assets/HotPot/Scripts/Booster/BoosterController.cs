using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FoodPop
{
    // EN: Owns every booster button (in BoosterSquare): reads taps on their icons, checks whether a booster can be used and runs the booster's gameplay logic and effects. Locks board input while a booster animates.
    // VI: Quản lý mọi nút booster (trong BoosterSquare): đọc thao tác chạm lên icon, kiểm tra có dùng được không và chạy logic chơi cùng hiệu ứng của booster. Khóa thao tác trên board khi booster đang chạy hiệu ứng.
    public class BoosterController : MonoBehaviour
    {
        // EN: Pickup rejection when every column has at most one row; only the icon shakes, no short message is shown.
        // VI: Lý do từ chối Pickup khi mọi cột chỉ còn tối đa 1 hàng; chỉ lắc icon, không hiện thông báo ngắn.
        private const string PickupNeedsTwoRows = "cần ít nhất một cột còn từ 2 hàng trở lên";

        // EN: Clean rejection when the belt has no food; TryActivate shows the BeltEmpty short message for it.
        // VI: Lý do từ chối Clean khi băng chuyền không có food; TryActivate hiện thông báo ngắn BeltEmpty cho lý do này.
        private const string CleanBeltEmpty = "băng chuyền đang trống";

        // EN: Rejection when no board is assigned (setup error); only logged, never shown to the player.
        // VI: Lý do từ chối khi chưa gán board (lỗi thiết lập); chỉ ghi log, không hiện cho người chơi.
        private const string BoardNotAssigned = "chưa gán board";

        // EN: Scene instance, if any.
        // VI: Bản trong scene, nếu có.
        public static BoosterController Instance => instance;

        // EN: True while a booster is running its effect.
        // VI: True khi một booster đang chạy hiệu ứng.
        public bool IsRunning => running;

        private Camera inputCamera => Camera.main;

        [Tooltip("Board nguyên liệu mà các booster tác động.")]
        [SerializeField] private FoodBoardManager board;
        [Tooltip("Các nút booster do controller quản lý.")]
        [SerializeField] private List<Booster> boosters = new List<Booster>();
        [Tooltip("Object chứa các nút booster, bị ẩn khi đang chọn đĩa bằng PickupHand; để trống thì ẩn từng nút booster.")]
        [SerializeField] private GameObject boosterRoot;
        [Tooltip("Kho giữ món nhận food sau khi dùng booster dọn băng chuyền.")]
        [SerializeField] private FoodHoldingStorage holdingStorage;
        [Tooltip("Điều khiển level, dùng số level hiện tại để khóa hoặc mở booster; để trống thì tự tìm trong scene.")]
        [SerializeField] private LevelController levelController;

        private static BoosterController instance;
        private bool running;
        private PickupHandBooster pickupSelecting;
        private readonly HashSet<Food> pickupTargets = new HashSet<Food>();
        private readonly HashSet<Food> pickupScratch = new HashSet<Food>();
        private int pickupVisibleRows = int.MaxValue;
        private bool pickupCameraMoved;
        private float pickupCameraRestY;
        private Coroutine cameraMove;
        private bool pickupInputWasLocked;
        private bool pickupOwnsInput;
        private readonly List<PickupPose> pickupPoses = new List<PickupPose>();
        private sealed class PickupPose
        {
            public Food dish;
            public Vector3 position, scale;
            public int[] orders;
            public int boost;
        }
        private ConveyorChefMovement collectionBelt;
        private ChefManager collectionChef;
        private CustomerManager collectionCustomers;
        private bool previousBeltPause, previousChefPause, previousServingPause;
        private bool collectionPaused;
        private bool cleanPending;
        private bool collectionInputWasLocked;
        private readonly List<SpriteRenderer> collectionFlyers = new List<SpriteRenderer>();
        private readonly List<ParticleEffect> collectionTrails = new List<ParticleEffect>();
        private Booster trialBooster;
        private System.Action trialTapped;

        // EN: Clears the static instance when entering Play Mode with domain reload disabled.
        // VI: Xóa instance tĩnh khi vào Play Mode lúc tắt tải lại miền mã.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            instance = null;
        }

        // EN: Registers the scene instance and fills any reference left empty in the Inspector.
        // VI: Đăng ký bản trong scene và tự gán các tham chiếu còn trống trong Inspector.
        private void Awake()
        {
            instance = this;
            ResolveReferences();
        }

        // EN: Restores singleton registration after script reload so storage can observe booster ownership, follows level loads and applies the current level's booster locks.
        // VI: Khôi phục đăng ký singleton sau tải lại script để kho nhận biết booster đang giữ quyền điều khiển, theo dõi lúc nạp level và áp khóa booster theo level hiện tại.
        private void OnEnable()
        {
            instance = this;
            if (levelController == null)
                return;
            levelController.LevelLoaded += OnLevelLoaded;
            if (levelController.CurrentLevel != null)
                RefreshBoosterLocks(levelController.CurrentLevel.levelNumber);
        }

        // EN: During a guided trial only the trial booster reacts (even while the game is paused); otherwise handles booster selection and taps only while gameplay time is running.
        // VI: Trong lượt dùng thử có hướng dẫn chỉ booster được thử phản hồi (kể cả khi game đang tạm dừng); ngoài ra chỉ xử lý chọn và chạm booster khi thời gian gameplay đang chạy.
        private void Update()
        {
            if (trialBooster != null)
            {
                UpdateTrial();
                return;
            }
            if (Time.timeScale <= 0f) return;
            if (pickupSelecting != null)
            {
                UpdatePickupSelection();
                return;
            }
            if (!TryGetPointerDown(out Vector2 worldPoint))
                return;
            foreach (Booster booster in boosters)
            {
                if (booster != null && booster.Contains(worldPoint))
                {
                    TryActivate(booster);
                    return;
                }
            }
        }

        // EN: Stops following level loads and releases Clean and Pickup-owned animation and input state when disabled.
        // VI: Ngừng theo dõi lúc nạp level và giải phóng hoạt cảnh cùng trạng thái input của Clean và Pickup khi bị tắt.
        private void OnDisable()
        {
            if (levelController != null)
                levelController.LevelLoaded -= OnLevelLoaded;
            CancelCleanCollection();
            CancelPickupForReplay();
        }

        // EN: Unregisters the scene instance.
        // VI: Hủy đăng ký bản trong scene.
        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        // EN: Only shakes a locked booster (no purchase, no message), requests purchases when empty and starts eligible boosters only after payment succeeds; Pickup commits at the validated pick boundary. Player-facing rejections (Clean on an empty belt, Pickup reasons) show their BoosterConfig short message.
        // VI: Booster còn khóa chỉ lắc (không mua, không thông báo); yêu cầu mua khi hết lượt và chỉ chạy booster hợp lệ sau khi thanh toán thành công; Pickup chốt tại bước lấy đã kiểm tra. Các lý do từ chối hiện cho người chơi (Clean khi băng trống, lý do của Pickup) dùng thông báo ngắn trong BoosterConfig.
        public bool TryActivate(Booster booster)
        {
            if (Time.timeScale <= 0f) return false;
            if (booster != null && booster.IsLocked)
            {
                booster.PlayDenied();
                Debug.Log($"[BoosterController] {booster.Type} còn khóa, mở ở level {IngameConfig.Instance.Boosters.GetUnlockLevel(booster.Type)}.", booster);
                return false;
            }
            if (booster != null && !running && !booster.HasAmount)
            {
                booster.RequestBuy();
                return false;
            }
            string reason = booster == null ? "không có booster"
                : running ? ShortDes(BoosterConfig.ShortDesType.BoosterRunning)
                : CanUse(booster);
            if (reason != null)
            {
                if (booster != null)
                {
                    booster.PlayDenied();
                    if (booster is CleanBrushBooster && reason == CleanBeltEmpty)
                        UIManager.Instance?.ShowShortMessage(ShortDes(BoosterConfig.ShortDesType.BeltEmpty));
                    else if (booster is PickupHandBooster && reason != PickupNeedsTwoRows && reason != BoardNotAssigned)
                        UIManager.Instance?.ShowShortMessage(reason);
                    Debug.Log($"[BoosterController] Không dùng được {booster.Type}: {reason}.", booster);
                }
                return false;
            }
            switch (booster)
            {
                case ShuffleBooster shuffle:
                    if (!shuffle.Consume()) return false;
                    StartCoroutine(RunLocked(Shuffle(shuffle)));
                    return true;
                case PickupHandBooster pickup:
                    BeginPickupSelection(pickup);
                    return true;
                case AutoCompleteBooster autoComplete:
                    if (!autoComplete.Consume()) return false;
                    StartCoroutine(RunLocked(AutoComplete(autoComplete)));
                    return true;
                case CleanBrushBooster cleanBrush:
                    if (!cleanBrush.Consume()) return false;
                    cleanPending = true;
                    StartCoroutine(RunLocked(CleanBrush(cleanBrush)));
                    return true;
                default:
                    Debug.LogWarning($"[BoosterController] Booster {booster.Type} chưa có logic.", booster);
                    return false;
            }
        }

        // EN: Restricts input to one booster for the unlock intro's guided trial; tapped runs once when the player taps it (other taps are ignored, even while paused).
        // VI: Giới hạn thao tác vào một booster cho lượt dùng thử có hướng dẫn của màn giới thiệu mở khóa; tapped chạy một lần khi người chơi chạm booster đó (các chạm khác bị bỏ qua, kể cả khi đang tạm dừng).
        public void BeginTrial(Booster booster, System.Action tapped)
        {
            trialBooster = booster;
            trialTapped = tapped;
        }

        // EN: Leaves trial mode without calling the tap callback.
        // VI: Thoát chế độ dùng thử mà không gọi callback chạm.
        public void EndTrial()
        {
            trialBooster = null;
            trialTapped = null;
        }

        // EN: Checks whether revive can reuse Clean's configured effects and storage without starting a booster.
        // VI: Kiểm tra hồi sinh có thể dùng lại hiệu ứng và kho của Clean mà chưa bắt đầu booster.
        public bool CanCleanForRevive()
        {
            return isActiveAndEnabled && !running && !cleanPending && board != null &&
                holdingStorage != null && holdingStorage.IsReady && !holdingStorage.IsReturning &&
                FindCleanSettings() != null && ChefManager.Instance != null &&
                ChefManager.Instance.HasFoodOnBelt() && FindFirstObjectByType<ConveyorChefMovement>() != null;
        }

        // EN: Starts the normal locked Clean collection for revive without consuming or tapping a booster button.
        // VI: Bắt đầu lượt thu Clean khóa thao tác để hồi sinh mà không tiêu hao hoặc bấm nút booster.
        public bool TryCleanForRevive(System.Action<bool> completed)
        {
            if (Time.timeScale <= 0f || !CanCleanForRevive()) return false;
            CleanBrushBooster settings = FindCleanSettings();
            cleanPending = true;
            StartCoroutine(RunReviveClean(settings, completed));
            return true;
        }

        // EN: Cancels pending collection flights on replay or disable and restores stored portions and pause ownership.
        // VI: Hủy chuyến thu đang chờ khi chơi lại hoặc tắt, khôi phục food đã giữ và quyền tạm dừng.
        public void CancelCleanCollection()
        {
            if (!collectionPaused && !cleanPending)
                return;
            cleanPending = false;
            StopAllCoroutines();
            foreach (SpriteRenderer flyer in collectionFlyers.ToArray())
                if (flyer != null && flyer.gameObject.activeSelf) ReleaseFx(flyer);
            collectionFlyers.Clear();
            foreach (ParticleEffect trail in collectionTrails)
                if (trail != null) trail.Stop();
            if (collectionChef != null)
                foreach (ConveyorSlot slot in collectionChef.Slots)
                    if (slot != null) slot.ResetCollectionPlate();
            if (holdingStorage != null) holdingStorage.CancelCollection();
            RestoreCollectionPause();
            if (board != null) board.InputLocked = collectionInputWasLocked;
            SetBusy(false);
            SetBoosterRootVisible(true);
        }

        // EN: Fills empty references straight from the scene: the board singleton, the LevelController, every Booster under this object (inactive ones included) and the child named "Root" that holds the booster buttons.
        // VI: Tự gán tham chiếu còn trống từ scene: singleton board, LevelController, mọi Booster nằm dưới object này (kể cả đang tắt) và object con tên "Root" chứa các nút booster.
        private void ResolveReferences()
        {
            if (board == null)
                board = FoodBoardManager.Instance;
            if (levelController == null)
                levelController = FindFirstObjectByType<LevelController>();
            if (boosters == null || boosters.Count == 0 || boosters.Contains(null))
                boosters = new List<Booster>(GetComponentsInChildren<Booster>(true));
            if (boosterRoot == null)
            {
                Transform root = transform.Find("Root");
                if (root != null)
                    boosterRoot = root.gameObject;
            }
        }

        // EN: Level-load callback: updates every booster's lock for the new level number.
        // VI: Callback khi nạp level: cập nhật khóa của mọi booster theo số level mới.
        private void OnLevelLoaded(LevelConfig config)
        {
            RefreshBoosterLocks(config.levelNumber);
        }

        // EN: Locks or unlocks every booster button for the given level number.
        // VI: Khóa hoặc mở khóa mọi nút booster theo số level đã cho.
        private void RefreshBoosterLocks(int level)
        {
            foreach (Booster booster in boosters)
                if (booster != null)
                    booster.RefreshLock(level);
        }

        // EN: Player-facing short message text from BoosterConfig.
        // VI: Nội dung thông báo ngắn cho người chơi lấy từ BoosterConfig.
        private static string ShortDes(BoosterConfig.ShortDesType type)
        {
            return IngameConfig.Instance.Boosters.GetShortDes(type);
        }

        // EN: Uses the existing Clean component even when its button is hidden, preserving its effects and tuning.
        // VI: Dùng component Clean hiện có kể cả khi nút bị ẩn, giữ nguyên hiệu ứng và thông số.
        private CleanBrushBooster FindCleanSettings()
        {
            foreach (Booster booster in boosters)
                if (booster is CleanBrushBooster clean) return clean;
            return GetComponentInChildren<CleanBrushBooster>(true);
        }

        // EN: Reports completion only after Clean has released its pause and input locks and freed conveyor space.
        // VI: Báo hoàn tất sau khi Clean nhả tạm dừng, khóa thao tác và giải phóng chỗ trên băng chuyền.
        private IEnumerator RunReviveClean(CleanBrushBooster settings, System.Action<bool> completed)
        {
            yield return RunLocked(CleanBrush(settings));
            ChefManager chef = ChefManager.Instance;
            completed?.Invoke(chef != null && chef.CountEmptySlots() > 0);
        }

        // EN: Returns null when the booster can do something right now, otherwise the reason it cannot (always logged; the busy-board and Pickup reasons are BoosterConfig short messages because Pickup shows them to the player, a missing board is log-only).
        // VI: Trả về null khi booster dùng được ngay lúc này, ngược lại trả về lý do không dùng được (luôn ghi log; lý do board đang bận và lý do Pickup là thông báo ngắn trong BoosterConfig vì Pickup hiện chúng cho người chơi, thiếu board chỉ ghi log).
        private string CanUse(Booster booster)
        {
            if (board == null)
                return BoardNotAssigned;
            if (board.IsAnimating)
                return ShortDes(BoosterConfig.ShortDesType.BoardBusy);
            switch (booster)
            {
                case ShuffleBooster _:
                    if (!CollectShuffleSpots(out _, out List<Food> dishes))
                        return "board không có đĩa để xáo";
                    return HasDifferentContent(dishes) ? null : "mọi đĩa đều giống nhau";
                case PickupHandBooster pickup:
                    return CanPickup(pickup.maxVisibleRows);
                case CleanBrushBooster _:
                    if (holdingStorage == null || !holdingStorage.IsReady)
                        return "chưa có túi giữ món sẵn sàng";
                    if (holdingStorage.IsReturning)
                        return "túi đang trả món lên băng chuyền";
                    return ChefManager.Instance != null && ChefManager.Instance.HasFoodOnBelt() ? null : CleanBeltEmpty;
                case AutoCompleteBooster _:
                    return CustomerManager.Instance != null && CustomerManager.Instance.HasAutoCompleteTarget() ? null : "không có món nào cần hoàn thành";
                default:
                    return "booster chưa có logic";
            }
        }

        // EN: Locks input during execution, optionally restoring the lock from before Pickup selection rather than its temporary lock.
        // VI: Khóa input khi chạy, có thể khôi phục khóa trước chế độ chọn Pickup thay vì khóa tạm của chế độ chọn.
        private IEnumerator RunLocked(IEnumerator routine, bool? restoreInputLock = null)
        {
            SetBusy(true);
            bool wasLocked = restoreInputLock ?? board.InputLocked;
            collectionInputWasLocked = wasLocked;
            board.InputLocked = true;
            try
            {
                yield return StartCoroutine(routine);
            }
            finally
            {
                cleanPending = false;
                if (board != null) board.InputLocked = wasLocked;
                if (restoreInputLock.HasValue) pickupOwnsInput = false;
                SetBusy(false);
                SetBoosterRootVisible(true);
            }
        }

        // EN: Marks a booster action as running (or finished): while busy no other booster can start, and every booster icon is shown dimmed so the player sees they are unavailable.
        // VI: Đánh dấu một hành động booster đang chạy (hoặc đã xong): trong lúc bận không booster nào khác bắt đầu được, và mọi icon booster được làm mờ để người chơi thấy chúng đang không dùng được.
        private void SetBusy(bool busy)
        {
            running = busy;
            foreach (Booster booster in boosters)
                if (booster != null)
                    booster.SetAvailable(!busy);
        }

        // EN: Shows or hides the booster buttons: the assigned root object, or each booster button when no root is assigned (the root must never be this controller's own object, or its Update would stop).
        // VI: Hiện hoặc ẩn các nút booster: object root đã gán, hoặc từng nút booster khi chưa gán root (root không được là chính object của controller, nếu không Update sẽ dừng).
        private void SetBoosterRootVisible(bool visible)
        {
            if (boosterRoot != null && boosterRoot != gameObject)
            {
                boosterRoot.SetActive(visible);
                return;
            }
            foreach (Booster booster in boosters)
                if (booster != null)
                    booster.gameObject.SetActive(visible);
        }

        // ---------------------------------------------------------------- PickupHand

        // EN: Requires at least one column with two or more rows (when every column has a single row the player can already pick any dish normally), then checks visible groups and reports ice or capacity using the same rules as selection; the fallback "nothing to pick" text comes from BoosterConfig.
        // VI: Yêu cầu có ít nhất một cột còn từ 2 hàng trở lên (khi mọi cột chỉ còn 1 hàng thì người chơi đã lấy được mọi đĩa như bình thường), sau đó kiểm tra nhóm trong vùng chọn và báo băng hoặc sức chứa theo cùng quy tắc lựa chọn; câu "không có gì để lấy" dự phòng lấy từ BoosterConfig.
        private string CanPickup(int maxRows)
        {
            if (GetBoardMaxRows() < 2)
                return PickupNeedsTwoRows;
            string blocked = null, capacity = null;
            int freeSlots = CountFreeSlots();
            for (int c = 0; c < board.ColumnCount; c++)
            {
                IReadOnlyList<Food> column = board.GetColumnDishes(c);
                for (int r = 0; r < column.Count && r < maxRows; r++)
                {
                    Food dish = column[r];
                    if (dish == null)
                        continue;
                    string reason = board.GetPickupGroupRejection(dish, freeSlots);
                    if (reason == null) return null;
                    if (board.GetPickupGroupRejection(dish, int.MaxValue) == null) capacity = reason;
                    else blocked = reason;
                }
            }
            return capacity ?? blocked ?? ShortDes(BoosterConfig.ShortDesType.NoPlateToPick);
        }

        // EN: Number of empty conveyor slots now (0 when there is no chef manager).
        // VI: Số slot trống trên băng chuyền lúc này (0 khi không có chef manager).
        private static int CountFreeSlots()
        {
            ChefManager chef = ChefManager.Instance;
            return chef != null ? chef.CountEmptySlots() : 0;
        }

        // EN: Checks all linked members' ice and combined portion count for PickupHand.
        // VI: Kiểm tra băng mọi thành viên nối và tổng số phần cho PickupHand.
        private bool FitsConveyor(Food dish, int freeSlots)
        {
            return board.CanPickupGroup(dish, freeSlots);
        }

        // EN: Saves the previous input lock before entering selection and highlighting available groups.
        // VI: Lưu khóa input trước khi vào chế độ chọn và làm sáng các nhóm lấy được.
        private void BeginPickupSelection(PickupHandBooster settings)
        {
            SetBusy(true);
            pickupInputWasLocked = board.InputLocked;
            pickupOwnsInput = true;
            board.InputLocked = true;
            pickupSelecting = settings;
            SetBoosterRootVisible(false);
            settings.ShowPrompt(true);
            MovePickupCamera(settings);
            pickupTargets.Clear();
            RefreshPickupTargets(true);
        }

        // EN: Waits for a tap on the trial booster and hands it to the trial callback once.
        // VI: Chờ chạm vào booster đang được thử và chuyển cho callback dùng thử một lần.
        private void UpdateTrial()
        {
            if (!TryGetPointerDown(out Vector2 worldPoint) || !trialBooster.Contains(worldPoint))
                return;
            System.Action tapped = trialTapped;
            EndTrial();
            tapped?.Invoke();
        }

        // EN: Updates available highlights and routes pointer input to cancellation or whole-group selection.
        // VI: Cập nhật nhóm lấy được và chuyển thao tác chạm sang hủy hoặc chọn cả nhóm.
        private void UpdatePickupSelection()
        {
            RefreshPickupTargets(false);
            if (!TryGetPointerDown(out Vector2 worldPoint))
                return;
            if (pickupSelecting.CancelContains(worldPoint))
            {
                CancelPickupSelection();
                return;
            }
            if (!board.TryGetDishAt(worldPoint, out int column, out int row))
                return;
            TryPickupDishAt(column, row);
        }

        // EN: Validates every linked member (rejections use BoosterConfig short messages; a dish outside the selectable rows only flashes, without a message) and starts Pickup using the input lock saved before selection.
        // VI: Kiểm tra mọi thành viên nối (lý do từ chối dùng thông báo ngắn trong BoosterConfig; đĩa ngoài vùng hàng được chọn chỉ nháy, không hiện thông báo) và chạy Pickup với khóa input đã lưu trước chế độ chọn.
        public bool TryPickupDishAt(int column, int row)
        {
            if (pickupSelecting == null || column < 0 || column >= board.ColumnCount) return false;
            if (row < 0 || row >= board.GetColumnDishes(column).Count) return false;
            Food dish = board.GetColumnDishes(column)[row];
            if (row >= pickupVisibleRows)
            {
                ShowPickupDenied(dish, "đĩa nằm ngoài vùng chọn", false);
                return false;
            }
            string reason = board.GetPickupGroupRejection(dish, CountFreeSlots());
            if (reason != null)
            {
                ShowPickupDenied(dish, reason);
                return false;
            }
            PickupHandBooster settings = pickupSelecting;
            pickupSelecting = null;
            settings.ShowPrompt(false);
            StartCoroutine(RunLocked(Pickup(settings, column, row), pickupInputWasLocked));
            return true;
        }

        // EN: Flashes all members of the rejected group, logs the reason and shows it as a short message unless showMessage is false.
        // VI: Nháy mọi thành viên trong nhóm bị từ chối, ghi log lý do và hiện lý do thành thông báo ngắn trừ khi showMessage là false.
        private void ShowPickupDenied(Food dish, string reason, bool showMessage = true)
        {
            foreach (Food member in board.GetPickMembers(dish))
                if (member != null) member.Selection?.PlayRejected();
            if (showMessage)
                UIManager.Instance?.ShowShortMessage(reason);
            Debug.Log($"[BoosterController] Pickup: {reason}.", this);
        }

        // EN: Recomputes which dishes can be taken (only rows brought into view by the camera); when the set changed (or force is true) lights those up with the white outline and turns every other dish back to plain (no outline, dimmed).
        // VI: Tính lại các đĩa lấy được (chỉ các hàng camera đã kéo vào màn hình); khi tập đĩa thay đổi (hoặc force là true) thì làm sáng kèm viền trắng các đĩa đó và đưa mọi đĩa khác về trạng thái thường (không viền, tối).
        private void RefreshPickupTargets(bool force)
        {
            pickupScratch.Clear();
            int freeSlots = CountFreeSlots();
            for (int c = 0; c < board.ColumnCount; c++)
            {
                IReadOnlyList<Food> column = board.GetColumnDishes(c);
                for (int r = 0; r < column.Count && r < pickupVisibleRows; r++)
                {
                    Food dish = column[r];
                    if (dish != null && !dish.IsFrozen && FitsConveyor(dish, freeSlots))
                        pickupScratch.Add(dish);
                }
            }
            if (!force && pickupScratch.SetEquals(pickupTargets))
                return;
            pickupTargets.Clear();
            pickupTargets.UnionWith(pickupScratch);
            for (int c = 0; c < board.ColumnCount; c++)
                foreach (Food dish in board.GetColumnDishes(c))
                    if (dish != null && dish.Selection != null)
                        dish.Selection.SetSelectable(pickupTargets.Contains(dish));
        }

        // EN: Cancels selection and restores the input state saved before Pickup started.
        // VI: Hủy lựa chọn và khôi phục trạng thái input đã lưu trước khi bắt đầu Pickup.
        private void CancelPickupSelection()
        {
            pickupSelecting.ShowPrompt(false);
            pickupSelecting = null;
            pickupTargets.Clear();
            RestorePickupCamera();
            board.RefreshSelection();
            board.InputLocked = pickupInputWasLocked;
            pickupOwnsInput = false;
            SetBusy(false);
            SetBoosterRootVisible(true);
        }

        // EN: Lifts the linked group, commits its booster payment after board validation and restores poses if picking or payment fails (showing the board reason as a short message; a failure without a gameplay reason only flashes the dishes and logs).
        // VI: Nhấc nhóm nối, chốt lượt booster sau kiểm tra board và khôi phục tư thế nếu lấy hoặc thanh toán thất bại (hiện lý do của board thành thông báo ngắn; thất bại không có lý do gameplay thì chỉ nháy đĩa và ghi log).
        private IEnumerator Pickup(PickupHandBooster settings, int column, int row)
        {
            Food dish = board.GetColumnDishes(column)[row];
            HighlightOnly(dish);
            foreach (Food member in board.GetPickMembers(dish))
                if (member != null)
                    pickupPoses.Add(new PickupPose { dish = member, position = member.transform.position,
                        scale = member.transform.localScale, orders = BoostSorting(member, settings.sortingBoost),
                        boost = settings.sortingBoost });

            Coroutine reveal = StartCoroutine(board.RevealPickGroup(dish, settings.revealDuration));
            try
            {
                yield return LiftPickupGroup(settings);
                if (reveal != null) yield return reveal;
                if (board.TryPickDishAt(column, row, settings.Consume))
                {
                    pickupPoses.Clear();
                }
                else
                {
                    RestorePickupPoses();
                    string reason = board.GetPickupGroupRejection(dish, CountFreeSlots());
                    ShowPickupDenied(dish, reason ?? "không thể lấy nguyên liệu lúc này", reason != null);
                }
                while (board.IsAnimating) yield return null;
                Coroutine cameraBack = RestorePickupCamera();
                if (cameraBack != null) yield return cameraBack;
            }
            finally
            {
                if (reveal != null) StopCoroutine(reveal);
                RestorePickupPoses();
                if (board != null) board.RefreshSelection();
            }
        }

        // EN: Restores the transforms and sorting saved for dishes that have not been picked.
        // VI: Trả transform và thứ tự vẽ đã lưu cho các đĩa chưa được lấy.
        private void RestorePickupPoses()
        {
            foreach (PickupPose pose in pickupPoses)
                if (pose.dish != null)
                {
                    pose.dish.transform.position = pose.position;
                    pose.dish.transform.localScale = pose.scale;
                    RestoreSorting(pose.dish, pose.orders, pose.boost);
                }
            pickupPoses.Clear();
        }

        // EN: Cancels Pickup before replay or disable and restores poses, camera and the original input lock.
        // VI: Hủy Pickup trước khi chơi lại hoặc vô hiệu hóa, trả tư thế, camera và khóa input ban đầu.
        public void CancelPickupForReplay()
        {
            if (!pickupOwnsInput && pickupSelecting == null) return;
            StopAllCoroutines();
            if (pickupSelecting != null) pickupSelecting.ShowPrompt(false);
            pickupSelecting = null;
            RestorePickupPoses();
            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (pickupCameraMoved && cam != null)
            {
                Vector3 position = cam.transform.position;
                position.y = pickupCameraRestY;
                cam.transform.position = position;
            }
            cameraMove = null;
            pickupCameraMoved = false;
            pickupVisibleRows = int.MaxValue;
            pickupTargets.Clear();
            if (board != null)
            {
                board.MaxVisibleRows = int.MaxValue;
                board.InputLocked = pickupInputWasLocked;
                board.RefreshSelection();
            }
            pickupOwnsInput = false;
            SetBusy(false);
            SetBoosterRootVisible(true);
        }

        // EN: Hides every row past maxVisibleRows, then slides the camera down just enough to show min(maxVisibleRows, rows the board has) rows above the prompt; never moves it up. Remembers the old Y so it can slide back. Dishes below the screen turn on by themselves (the board hides/shows them by the camera's bottom edge).
        // VI: Ẩn mọi hàng sau maxVisibleRows, rồi trượt camera xuống vừa đủ để thấy min(maxVisibleRows, số hàng board đang có) hàng phía trên thông báo; không bao giờ đẩy camera lên. Ghi nhớ Y cũ để trượt về. Đĩa dưới màn hình tự bật lại (board bật/tắt đĩa theo mép dưới camera).
        private void MovePickupCamera(PickupHandBooster settings)
        {
            int boardRows = GetBoardMaxRows();
            pickupVisibleRows = Mathf.Min(settings.maxVisibleRows, boardRows);
            board.MaxVisibleRows = settings.maxVisibleRows;
            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (cam == null || pickupVisibleRows <= 0)
                return;
            float cameraBottom = GetCameraBottomY(cam);
            float lowestRowBottom = GetRowBottomY(pickupVisibleRows - 1);
            float coveredHeight = GetPromptCoveredHeight(settings, cameraBottom);
            float shift = cameraBottom + coveredHeight + settings.boardBottomPadding - lowestRowBottom;
            if (shift <= 0f)
                return;
            pickupCameraRestY = cam.transform.position.y;
            pickupCameraMoved = true;
            StartCameraMove(cam, pickupCameraRestY - shift, settings.cameraMoveDuration);
        }

        // EN: Lifts the board's row limit, slides the camera back to where it was before PickupHand (if it was moved) and clears the visible-row limit; returns the running slide, or null.
        // VI: Bỏ giới hạn số hàng của board, trượt camera về vị trí trước khi dùng PickupHand (nếu đã dịch) và bỏ giới hạn số hàng chọn; trả về coroutine đang trượt, hoặc null.
        private Coroutine RestorePickupCamera()
        {
            pickupVisibleRows = int.MaxValue;
            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (!pickupCameraMoved || cam == null)
            {
                board.MaxVisibleRows = int.MaxValue;
                return null;
            }
            pickupCameraMoved = false;
            PickupHandBooster settings = null;
            foreach (Booster booster in boosters)
                if (booster is PickupHandBooster pickup)
                    settings = pickup;
            return StartCoroutine(SlideBackThenShowRows(StartCameraMove(cam, pickupCameraRestY, settings != null ? settings.cameraMoveDuration : 0.35f)));
        }

        // EN: Waits for the camera to finish sliding back, then lifts the board's row limit, so the extra rows never flash in while the camera is still low.
        // VI: Chờ camera trượt về xong rồi mới bỏ giới hạn số hàng của board, để các hàng thừa không chớp hiện lên khi camera vẫn còn ở thấp.
        private IEnumerator SlideBackThenShowRows(Coroutine slide)
        {
            yield return slide;
            board.MaxVisibleRows = int.MaxValue;
        }

        // EN: Largest number of dishes in any column.
        // VI: Số đĩa nhiều nhất trong một cột.
        private int GetBoardMaxRows()
        {
            int rows = 0;
            for (int c = 0; c < board.ColumnCount; c++)
                rows = Mathf.Max(rows, board.GetColumnDishes(c).Count);
            return rows;
        }

        // EN: World Y of the bottom of a dish plate sitting at the given row (half a plate below the row centre, measured on a column head).
        // VI: Toạ độ Y world của đáy đĩa nằm ở hàng đã cho (thấp hơn tâm hàng nửa chiều cao đĩa, đo trên một đĩa đầu cột).
        private float GetRowBottomY(int row)
        {
            float halfPlate = 0.6f;
            int column = 0;
            for (int c = 0; c < board.ColumnCount; c++)
            {
                IReadOnlyList<Food> dishes = board.GetColumnDishes(c);
                if (dishes.Count == 0 || dishes[0] == null)
                    continue;
                column = c;
                SpriteRenderer plate = dishes[0].PlateRenderer != null ? dishes[0].PlateRenderer : dishes[0].IconRenderer;
                if (plate != null)
                    halfPlate = plate.bounds.extents.y;
                break;
            }
            return board.GetColumnTransform(column).TransformPoint(board.GetRowLocalPosition(row)).y - halfPlate;
        }

        // EN: Height (world units) of the screen bottom covered by the prompt: from the camera bottom to the top of the prompt's renderers.
        // VI: Chiều cao (đơn vị world) phần đáy màn hình bị thông báo che: từ mép dưới camera tới đỉnh các renderer của thông báo.
        private static float GetPromptCoveredHeight(PickupHandBooster settings, float cameraBottom)
        {
            if (settings.PromptRoot == null)
                return 0f;
            float top = cameraBottom;
            foreach (Renderer renderer in settings.PromptRoot.GetComponentsInChildren<Renderer>())
                if (renderer.enabled)
                    top = Mathf.Max(top, renderer.bounds.max.y);
            return top - cameraBottom;
        }

        // EN: World Y of the bottom edge of the camera view.
        // VI: Toạ độ Y world của mép dưới khung camera.
        private static float GetCameraBottomY(Camera cam)
        {
            return cam.ViewportToWorldPoint(new Vector3(0.5f, 0f, Mathf.Abs(cam.transform.position.z))).y;
        }

        // EN: Stops any running camera slide and starts a new one to targetY.
        // VI: Dừng lần trượt camera đang chạy và bắt đầu trượt tới targetY.
        private Coroutine StartCameraMove(Camera cam, float targetY, float duration)
        {
            if (cameraMove != null)
                StopCoroutine(cameraMove);
            cameraMove = StartCoroutine(MoveCameraY(cam, targetY, duration));
            return cameraMove;
        }

        // EN: Smoothly slides the camera's Y to targetY over duration (X and Z unchanged).
        // VI: Trượt mượt Y của camera tới targetY trong thời gian duration (giữ nguyên X và Z).
        private IEnumerator MoveCameraY(Camera cam, float targetY, float duration)
        {
            Transform camTransform = cam.transform;
            float startY = camTransform.position.y;
            for (float time = 0f; time < duration && camTransform != null; time += Time.deltaTime)
            {
                Vector3 position = camTransform.position;
                position.y = Mathf.Lerp(startY, targetY, Mathf.SmoothStep(0f, 1f, time / duration));
                camTransform.position = position;
                yield return null;
            }
            if (camTransform != null)
            {
                Vector3 final = camTransform.position;
                final.y = targetY;
                camTransform.position = final;
            }
            cameraMove = null;
        }

        // EN: Highlights the chosen dish and every linked member during PickupHand, dimming unrelated dishes.
        // VI: Làm sáng đĩa chọn và mọi thành viên nối khi dùng PickupHand, làm tối đĩa không liên quan.
        private void HighlightOnly(Food chosen)
        {
            for (int c = 0; c < board.ColumnCount; c++)
                foreach (Food dish in board.GetColumnDishes(c))
                    if (dish != null && dish.Selection != null)
                        dish.Selection.SetSelectable(board.SharesPickGroup(dish, chosen));
            pickupTargets.Clear();
        }

        // EN: Raises every renderer of the dish by boost so the lifted dish draws over its neighbours; returns the old orders. A dish that is picked goes to the pool, which restores its orders when it is reused.
        // VI: Tăng thứ tự vẽ mọi renderer của đĩa thêm boost để đĩa đang nhấc nằm trên đĩa bên cạnh; trả về thứ tự cũ. Đĩa đã lấy sẽ về pool và được trả lại thứ tự vẽ khi dùng lại.
        private static int[] BoostSorting(Food dish, int boost)
        {
            SpriteRenderer[] renderers = dish.VisualRenderers ?? new SpriteRenderer[0];
            int[] orders = new int[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;
                orders[i] = renderers[i].sortingOrder;
                renderers[i].sortingOrder += boost;
            }
            if (dish.CountCanvas != null)
                dish.CountCanvas.sortingOrder += boost;
            return orders;
        }

        // EN: Puts the dish's renderer orders back (when the dish is set back down instead of being picked).
        // VI: Trả lại thứ tự vẽ của đĩa (khi đĩa được đặt lại thay vì bị lấy).
        private static void RestoreSorting(Food dish, int[] orders, int boost)
        {
            SpriteRenderer[] renderers = dish.VisualRenderers ?? new SpriteRenderer[0];
            for (int i = 0; i < renderers.Length && i < orders.Length; i++)
                if (renderers[i] != null)
                    renderers[i].sortingOrder = orders[i];
            if (dish.CountCanvas != null)
                dish.CountCanvas.sortingOrder -= boost;
        }

        // EN: Applies shared lift progress to all members while preserving their relative positions.
        // VI: Dùng chung tiến độ nhấc cho mọi thành viên và giữ nguyên vị trí tương đối.
        private IEnumerator LiftPickupGroup(PickupHandBooster settings)
        {
            float time = 0f;
            while (true)
            {
                float progress = settings.liftDuration <= 0f ? 1f : Mathf.Clamp01(time / settings.liftDuration);
                float ease = 1f - Mathf.Pow(1f - progress, 2f);
                foreach (PickupPose pose in pickupPoses)
                    if (pose.dish != null)
                    {
                        pose.dish.transform.position = pose.position + Vector3.up * settings.liftHeight * ease;
                        pose.dish.transform.localScale = pose.scale * Mathf.Lerp(1f, settings.liftScale, ease);
                    }
                if (progress >= 1f) yield break;
                yield return null;
                time += Time.deltaTime;
            }
        }

        // ---------------------------------------------------------------- AutoComplete

        // EN: Live state of the AutoComplete star: its particle instance, where it is, its size and the absorb pulse.
        // VI: Trạng thái hiện tại của ngôi sao AutoComplete: bản particle, vị trí, kích thước và nhịp phồng khi hút.
        private sealed class StarState
        {
            public ParticleEffect effect;
            public Vector3 position;
            public float size;
            public float pulse;
            public bool alive = true;
        }

        private readonly List<SpriteRenderer> fxPool = new List<SpriteRenderer>();
        private Material defaultFxMaterial;
        private Transform fxRoot;
        private ParticleEffect starInstance;

        // EN: Cancels unfinished holding returns before reserving a dish, then gathers each real ingredient once from belt, storage or board and serves it through the star.
        // VI: Hủy chuyến trả từ khay chưa xong trước khi giữ chỗ món, rồi lấy từng nguyên liệu thật một lần từ băng, kho hoặc board và phục vụ qua sao.
        private IEnumerator AutoComplete(AutoCompleteBooster settings)
        {
            if (holdingStorage != null) holdingStorage.CancelPendingReturn();
            CustomerManager customers = CustomerManager.Instance;
            if (customers == null || !customers.TryReserveAutoComplete(out CustomerManager.AutoCompleteJob job))
            {
                Debug.Log("[BoosterController] AutoComplete: không còn món nào để hoàn thành.", settings);
                yield break;
            }
            int layer = job.serving.EffectSortingLayer;
            Vector3 slot = job.serving.GetDishSlotCenter(job.dish);
            ChefManager chef = ChefManager.Instance;
            Vector3 home = (chef != null ? chef.ChefTransform.position : slot) + (Vector3)settings.starOffset;

            StarState star = StartStar(settings, layer, home);
            StartCoroutine(StarDriver(settings, star));
            yield return GrowStar(settings, star, layer);

            int absorbed = 0;
            for (int i = 0; i < job.ingredients.Count; i++)
            {
                StartCoroutine(GatherIngredient(settings, star, job.ingredients[i], layer, () => absorbed++));
                yield return new WaitForSeconds(settings.gatherInterval);
            }
            while (absorbed < job.ingredients.Count)
                yield return null;
            yield return new WaitForSeconds(settings.shineHold);

            Vector3 burstPoint = slot + Vector3.up * settings.burstHeight;
            yield return FlyStar(settings, star, home, burstPoint);
            yield return VanishStar(settings, star);
            OneShotEffects.PlayParticle(settings.burstEffect, burstPoint, layer, settings.sortingOrder);

            List<SpriteRenderer> pieces = PopIngredients(settings, job, burstPoint, layer);
            yield return SpreadPieces(settings, pieces, burstPoint);
            for (int i = 0; i < pieces.Count; i++)
            {
                SpriteRenderer piece = pieces[i];
                if (!job.serving.LaunchBoosterServe(job.dish, job.ingredients[i], piece.sprite, piece.transform.position, piece.transform.localScale))
                    Debug.LogWarning($"[BoosterController] AutoComplete: không thả được {job.ingredients[i]} vào khay.", settings);
                ReleaseFx(piece);
                if (i < pieces.Count - 1)
                    yield return new WaitForSeconds(settings.dropInterval);
            }
            while (job.serving.IsReceiving(job.dish))
                yield return null;
        }

        // EN: Places the (reused) star particle at its start point, puts it on the effect sorting and starts it at zero size.
        // VI: Đặt particle ngôi sao (dùng lại) tại điểm bắt đầu, gán thứ tự vẽ của hiệu ứng và chạy nó từ kích thước 0.
        private StarState StartStar(AutoCompleteBooster settings, int layer, Vector3 home)
        {
            if (starInstance == null && settings.starEffect != null)
                starInstance = Instantiate(settings.starEffect, FxRoot);
            StarState star = new StarState { effect = starInstance, position = home, size = 0f };
            if (star.effect != null)
            {
                star.effect.transform.position = home;
                star.effect.transform.localScale = Vector3.zero;
                star.effect.SetSorting(layer, settings.sortingOrder);
                star.effect.gameObject.SetActive(true);
                star.effect.Play();
            }
            return star;
        }

        // EN: Every frame while the star lives: moves the particle star to its position and scales it by its size plus the absorb pulse (the particles scale with the transform). When it ends, stops emitting so live particles fade out on their own.
        // VI: Mỗi frame khi sao còn sống: đưa particle ngôi sao tới vị trí và scale theo kích thước cộng nhịp phồng khi hút (particle scale theo transform). Khi kết thúc thì ngừng phát để các hạt đang sống tự mờ đi.
        private IEnumerator StarDriver(AutoCompleteBooster settings, StarState star)
        {
            while (star.alive)
            {
                star.pulse = Mathf.Max(0f, star.pulse - Time.deltaTime * 5f);
                if (star.effect != null)
                {
                    star.effect.transform.position = star.position;
                    star.effect.transform.localScale = Vector3.one * star.size * (1f + settings.absorbPulse * star.pulse);
                }
                yield return null;
            }
            if (star.effect != null)
                star.effect.Stop();
        }

        // EN: Grows the star and its magic circle in to its large chef size (starScale * chefScale) with a small overshoot, then plays the sparkle puff.
        // VI: Cho ngôi sao và vòng phép lớn dần tới cỡ lớn khi ở đầu bếp (starScale * chefScale) kèm vượt cỡ nhẹ, rồi chạy chùm lấp lánh.
        private IEnumerator GrowStar(AutoCompleteBooster settings, StarState star, int layer)
        {
            for (float time = 0f; time < settings.appearDuration; time += Time.deltaTime)
            {
                float t = time / settings.appearDuration;
                star.size = settings.starScale * settings.chefScale * (t < 0.7f ? 1.15f * Mathf.SmoothStep(0f, 1f, t / 0.7f) : Mathf.Lerp(1.15f, 1f, (t - 0.7f) / 0.3f));
                yield return null;
            }
            star.size = settings.starScale * settings.chefScale;
            OneShotEffects.PlayParticle(settings.sparkleEffect, star.position, layer, settings.sortingOrder + 5);
        }

        // EN: Takes an ingredient from belt, holding tray or board, then flies it into the AutoComplete star.
        // VI: Lấy nguyên liệu từ băng, khay giữ món hoặc board rồi cho bay vào sao AutoComplete.
        // EN: Uses the shared ingredient configuration instead of per-food catalogue references.
        // VI: Dùng cấu hình nguyên liệu chung thay cho tham chiếu bảng riêng trên từng food.
        private IEnumerator GatherIngredient(AutoCompleteBooster settings, StarState star, FoodType type, int layer, System.Action absorbed)
        {
            ChefManager chef = ChefManager.Instance;
            Vector3 start;
            Sprite sprite;
            if ((chef == null || !chef.TryTakeFood(type, out start, out sprite)) &&
                (holdingStorage == null || !holdingStorage.TryTake(type, out start, out sprite)) &&
                !board.TryTakePortion(type, out start, out sprite))
            {
                Debug.LogWarning($"[BoosterController] AutoComplete: không tìm thấy {type} trên băng chuyền hay board; kiểm tra lại level.", this);
                start = star.position;
                sprite = IngameConfig.Instance.FoodIcons != null ? IngameConfig.Instance.FoodIcons.GetIcon(type) : null;
            }
            SpriteRenderer flyer = RentFx(sprite, layer, settings.sortingOrder + 3);
            Vector3 lift = Vector3.up * settings.gatherArcHeight;
            for (float time = 0f; time < settings.gatherDuration; time += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, time / settings.gatherDuration);
                Vector3 control = (start + star.position) * 0.5f + lift;
                Vector3 position = (1f - t) * (1f - t) * start + 2f * (1f - t) * t * control + t * t * star.position;
                float size = settings.ingredientSize * (t < 0.7f ? Mathf.Lerp(0.6f, 1f, t / 0.7f) : 1f - (t - 0.7f) / 0.3f);
                PlaceFx(flyer, position, size, t * 180f);
                yield return null;
            }
            ReleaseFx(flyer);
            star.pulse = 1f;
            OneShotEffects.PlayParticle(settings.sparkleEffect, star.position, layer, settings.sortingOrder + 5);
            absorbed();
        }

        // EN: Flies the star from the chef to above the dish slot along a sideways arc (its flight-trail layer leaves sparkles behind) while it shrinks smoothly from its chef size to its slot size.
        // VI: Cho sao bay từ đầu bếp tới phía trên ô món theo vòng cung lệch ngang (lớp vệt bay để lại hạt lấp lánh phía sau) trong lúc thu nhỏ mượt từ cỡ ở đầu bếp về cỡ ở ô món.
        private static IEnumerator FlyStar(AutoCompleteBooster settings, StarState star, Vector3 from, Vector3 to)
        {
            Vector3 control = (from + to) * 0.5f + new Vector3(settings.flyArc, settings.flyArc * 0.5f, 0f);
            for (float time = 0f; time < settings.flyDuration; time += Time.deltaTime)
            {
                float k = time / settings.flyDuration;
                float t = 1f - (1f - k) * (1f - k);
                star.position = (1f - t) * (1f - t) * from + 2f * (1f - t) * t * control + t * t * to;
                star.size = settings.starScale * Mathf.Lerp(settings.chefScale, settings.slotScale, Mathf.SmoothStep(0f, 1f, k));
                yield return null;
            }
            star.position = to;
        }

        // EN: Shrinks the star away quickly so the burst replaces it, then ends it (remaining particles fade on their own).
        // VI: Thu nhỏ ngôi sao thật nhanh để vụ nổ thay thế nó, rồi kết thúc (các hạt còn lại tự mờ đi).
        private static IEnumerator VanishStar(AutoCompleteBooster settings, StarState star)
        {
            float from = star.size;
            for (float time = 0f; time < settings.vanishDuration; time += Time.deltaTime)
            {
                star.size = Mathf.Lerp(from, 0f, time / settings.vanishDuration);
                yield return null;
            }
            star.size = 0f;
            star.alive = false;
        }

        // EN: Creates one prepared-looking sprite per reserved ingredient at the burst point.
        // VI: Tạo một sprite (dạng đã sơ chế) cho mỗi nguyên liệu đã giữ chỗ tại tâm vụ nổ.
        // EN: Uses the shared ingredient configuration instead of per-food catalogue references.
        // VI: Dùng cấu hình nguyên liệu chung thay cho tham chiếu bảng riêng trên từng food.
        private List<SpriteRenderer> PopIngredients(AutoCompleteBooster settings, CustomerManager.AutoCompleteJob job, Vector3 point, int layer)
        {
            List<SpriteRenderer> pieces = new List<SpriteRenderer>(job.ingredients.Count);
            foreach (FoodType type in job.ingredients)
            {
                Sprite sprite = IngameConfig.Instance.FoodIcons != null ? IngameConfig.Instance.FoodIcons.GetIcon(type, FoodState.Prepared) : null;
                SpriteRenderer piece = RentFx(sprite, layer, settings.sortingOrder + 3);
                PlaceFx(piece, point, 0.01f, 0f);
                pieces.Add(piece);
            }
            return pieces;
        }

        // EN: Bursts the pieces outward over the upper half circle while they grow to full size.
        // VI: Cho các nguyên liệu bung ra theo nửa vòng tròn phía trên trong lúc lớn dần tới cỡ thật.
        private static IEnumerator SpreadPieces(AutoCompleteBooster settings, List<SpriteRenderer> pieces, Vector3 point)
        {
            for (float time = 0f; time < settings.popDuration; time += Time.deltaTime)
            {
                float k = time / settings.popDuration;
                float t = 1f - (1f - k) * (1f - k);
                for (int i = 0; i < pieces.Count; i++)
                {
                    float angle = Mathf.PI * (pieces.Count == 1 ? 0.5f : 0.15f + 0.7f * i / (pieces.Count - 1));
                    Vector3 direction = new Vector3(Mathf.Cos(Mathf.PI - angle), Mathf.Sin(angle), 0f);
                    PlaceFx(pieces[i], point + direction * settings.popDistance * t, settings.ingredientSize * Mathf.Lerp(0.3f, 1f, t), (1f - t) * (i - 1) * 30f);
                }
                yield return null;
            }
        }

        // EN: Unscaled scene-root object that holds the booster's runtime effects, created on first use.
        // VI: Object gốc không bị scale chứa hiệu ứng lúc chạy của booster, tạo ở lần dùng đầu.
        private Transform FxRoot
        {
            get
            {
                if (fxRoot == null)
                    fxRoot = new GameObject("BoosterFx").transform;
                return fxRoot;
            }
        }

        // EN: Rents a flight renderer with clean material and property overrides so interrupted highlights never leak into later effects.
        // VI: Lấy renderer bay có vật liệu và thuộc tính sạch để hiệu ứng sáng bị ngắt không ảnh hưởng lượt sau.
        private SpriteRenderer RentFx(Sprite sprite, int layer, int order)
        {
            SpriteRenderer fx;
            if (fxPool.Count > 0)
            {
                fx = fxPool[fxPool.Count - 1];
                fxPool.RemoveAt(fxPool.Count - 1);
            }
            else
            {
                fx = new GameObject("Fx").AddComponent<SpriteRenderer>();
                fx.transform.SetParent(FxRoot, false);
            }
            if (defaultFxMaterial == null) defaultFxMaterial = fx.sharedMaterial;
            fx.sharedMaterial = defaultFxMaterial;
            fx.SetPropertyBlock(null);
            fx.sprite = sprite;
            fx.sortingLayerID = layer;
            fx.sortingOrder = order;
            fx.color = Color.white;
            fx.transform.localScale = Vector3.zero;
            fx.gameObject.SetActive(true);
            return fx;
        }

        // EN: Hides a sprite renderer and keeps it for reuse.
        // VI: Ẩn sprite renderer và giữ lại để dùng lại.
        private void ReleaseFx(SpriteRenderer fx)
        {
            if (fx == null || !fx.gameObject.activeSelf)
                return;
            fx.gameObject.SetActive(false);
            fxPool.Add(fx);
        }

        // EN: Places a sprite at a world point with the given world height (keeping the sprite's aspect) and Z rotation.
        // VI: Đặt sprite tại điểm world với chiều cao world đã cho (giữ tỉ lệ sprite) và góc xoay Z.
        private static void PlaceFx(SpriteRenderer fx, Vector3 position, float worldHeight, float angle)
        {
            Transform tr = fx.transform;
            tr.SetPositionAndRotation(new Vector3(position.x, position.y, tr.position.z), Quaternion.Euler(0f, 0f, angle));
            float height = fx.sprite != null ? Mathf.Max(0.0001f, fx.sprite.bounds.size.y) : 1f;
            tr.localScale = Vector3.one * (worldHeight / height);
        }

        // ---------------------------------------------------------------- CleanBrush

        // EN: Pauses belt activity, pops the holding bag in and collects food into it in staggered flights, waiting for the final landing; the bag hides again if nothing was collected.
        // VI: Tạm dừng hoạt động băng, cho túi giữ món bật hiện và thu food vào bằng các chuyến bay nối tiếp, chờ tới lần đáp cuối; túi ẩn lại nếu không thu được gì.
        private IEnumerator CleanBrush(CleanBrushBooster settings)
        {
            ChefManager chef = ChefManager.Instance;
            CustomerManager customers = CustomerManager.Instance;
            ConveyorChefMovement belt = FindFirstObjectByType<ConveyorChefMovement>();
            if (chef == null || belt == null || holdingStorage == null || !holdingStorage.IsReady)
                yield break;
            PauseForCollection(belt, chef, customers);
            holdingStorage.ShowBag();
            try
            {
                int launched = 0, landed = 0;
                foreach (ConveyorSlot slot in chef.Slots)
                {
                    if (slot == null || !slot.isActiveAndEnabled || slot.isEmpty || slot.food == null)
                        continue;
                    Vector3 start = slot.LandingPosition;
                    Vector3 sourceScale = slot.LandingScale;
                    FoodState state = slot.food.state;
                    if (!chef.TryTakeSlot(slot, out FoodType type, out Sprite sprite))
                        continue;
                    FoodHoldingStorage.Portion portion = holdingStorage.Store(type, state, sprite, sourceScale);
                    int layer = slot.IngredientRenderer != null ? slot.IngredientRenderer.sortingLayerID : 0;
                    launched++;
                    StartCoroutine(CollectIntoTray(settings, slot, portion, sprite, start, layer, () => landed++));
                    yield return new WaitForSeconds(settings.collectInterval);
                }
                while (landed < launched)
                    yield return null;
                foreach (ConveyorSlot slot in chef.Slots)
                    while (slot != null && slot.IsPlateReturning)
                        yield return null;
                holdingStorage.HideIfEmpty();
            }
            finally
            {
                RestoreCollectionPause();
            }
        }

        // EN: Suspends belt movement, chef work and serving without discarding the chef's detached food.
        // VI: Dừng băng, công việc đầu bếp và phục vụ mà không làm mất phần món riêng tại đầu bếp.
        private void PauseForCollection(ConveyorChefMovement belt, ChefManager chef, CustomerManager customers)
        {
            collectionBelt = belt;
            collectionChef = chef;
            collectionCustomers = customers;
            previousBeltPause = belt.BoosterPaused;
            previousChefPause = chef.BoosterPaused;
            previousServingPause = customers != null && customers.BoosterPaused;
            collectionPaused = true;
            belt.BoosterPaused = true;
            chef.BoosterPaused = true;
            if (customers != null) customers.BoosterPaused = true;
        }

        // EN: Restores only the pause flags owned by collection, safely tolerating destroyed scene objects.
        // VI: Khôi phục riêng cờ tạm dừng của lượt thu, xử lý an toàn khi object scene đã bị hủy.
        private void RestoreCollectionPause()
        {
            if (!collectionPaused)
                return;
            if (collectionBelt != null) collectionBelt.BoosterPaused = previousBeltPause;
            if (collectionChef != null) collectionChef.BoosterPaused = previousChefPause;
            if (collectionCustomers != null) collectionCustomers.BoosterPaused = previousServingPause;
            collectionPaused = false;
        }

        // EN: Lifts plate and highlighted food together, then drops the plate while food flies at original size to the bag mouth and drops in behind the bag's front layer.
        // VI: Nhấc đĩa và food sáng cùng nhau, rồi thả đĩa xuống khi food bay ở kích thước gốc tới miệng túi và rơi vào sau lớp trước của túi.
        private IEnumerator CollectIntoTray(CleanBrushBooster settings, ConveyorSlot slot, FoodHoldingStorage.Portion portion, Sprite sprite, Vector3 start, int layer, System.Action landed)
        {
            SpriteRenderer flyer = RentFx(sprite, layer, settings.sortingOrder);
            Material normalMaterial = flyer.sharedMaterial;
            if (settings.collectionHighlight != null) flyer.sharedMaterial = settings.collectionHighlight;
            var highlight = new MaterialPropertyBlock();
            ParticleEffect trail = null;
            bool arrived = false;
            collectionFlyers.Add(flyer);
            try
            {
                for (float elapsed = 0f; elapsed < settings.liftDuration; elapsed += Time.deltaTime)
                {
                    float t = Mathf.Clamp01(elapsed / settings.liftDuration);
                    if (slot != null) slot.SetCollectionLift(settings.liftHeight * t);
                    flyer.transform.position = start + Vector3.up * settings.liftHeight * t;
                    float pulse = Mathf.Sin(t * Mathf.PI);
                    flyer.transform.localScale = portion.worldScale * (1f + settings.collectPulse * pulse);
                    highlight.SetFloat("_FlashStrength", pulse);
                    flyer.SetPropertyBlock(highlight);
                    yield return null;
                }
                flyer.sharedMaterial = normalMaterial;
                flyer.SetPropertyBlock(null);
                if (slot != null)
                {
                    slot.SetCollectionLift(settings.liftHeight);
                    slot.DropCollectionPlate(settings.plateDropDuration);
                }
                Vector3 from = start + Vector3.up * settings.liftHeight;
                trail = StartCollectionTrail(settings.collectionTrail, from, layer, settings.sortingOrder - 1);
                for (float elapsed = 0f; elapsed < settings.flyDuration; elapsed += Time.deltaTime)
                {
                    if (holdingStorage == null)
                        yield break;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / settings.flyDuration);
                    Vector3 target = holdingStorage.MouthPosition;
                    Vector3 control = (from + target) * 0.5f + Vector3.up * settings.flyArc;
                    Vector3 position = (1f-t)*(1f-t)*from + 2f*(1f-t)*t*control + t*t*target;
                    flyer.transform.position = position;
                    if (trail != null) trail.transform.position = position;
                    flyer.transform.localScale = portion.worldScale;
                    flyer.transform.rotation = Quaternion.Euler(0f, 0f, -8f * Mathf.Sin(t * Mathf.PI));
                    yield return null;
                }
                arrived = true;
            }
            finally
            {
                if (trail != null) trail.Stop();
                collectionFlyers.Remove(flyer);
                if (flyer != null)
                {
                    flyer.sharedMaterial = normalMaterial;
                    flyer.SetPropertyBlock(null);
                }
                if (flyer != null && flyer.gameObject.activeSelf) ReleaseFx(flyer);
                if (holdingStorage != null)
                {
                    if (arrived) holdingStorage.DropIn(portion);
                    else holdingStorage.Reveal(portion);
                }
                landed();
            }
        }

        // EN: Reuses finished world-space sparkle trails, keeping lingering particles alive after emission stops.
        // VI: Dùng lại vệt hạt sáng world đã kết thúc, giữ hạt còn sống tự mờ sau khi dừng phát.
        private ParticleEffect StartCollectionTrail(ParticleEffect prefab, Vector3 position, int layer, int order)
        {
            if (prefab == null)
                return null;
            collectionTrails.RemoveAll(effect => effect == null);
            ParticleEffect trail = collectionTrails.Find(effect => !effect.IsAlive);
            if (trail == null)
            {
                trail = Instantiate(prefab, FxRoot);
                collectionTrails.Add(trail);
            }
            trail.transform.position = position;
            trail.SetSorting(layer, order);
            trail.Play();
            return trail;
        }
        // ---------------------------------------------------------------- Shuffle

        // EN: Collects unlinked plates once, including double contents with fixed footprints; linked groups stay unchanged.
        // VI: Gom đĩa không nối một lần, gồm nguyên liệu đĩa đôi giữ vùng chiếm; nhóm nối giữ nguyên.
        private bool CollectShuffleSpots(out List<Vector2Int> spots, out List<Food> dishes)
        {
            spots = new List<Vector2Int>();
            dishes = new List<Food>();
            HashSet<Food> seen = new HashSet<Food>();
            for (int c = 0; c < board.ColumnCount; c++)
            {
                IReadOnlyList<Food> column = board.GetColumnDishes(c);
                for (int r = 0; r < column.Count; r++)
                {
                    Food dish = column[r];
                    if (dish == null || board.IsLinked(dish) || !seen.Add(dish))
                        continue;
                    spots.Add(new Vector2Int(c, r));
                    dishes.Add(dish);
                }
            }
            return dishes.Count > 0;
        }

        // EN: Checks both halves for different nonempty stacks so even one double plate can be shuffled.
        // VI: Kiểm tra cả hai nửa có nhóm nguyên liệu khác nhau và còn phần, cho phép xáo cả khi chỉ có một đĩa đôi.
        private static bool HasDifferentContent(List<Food> dishes)
        {
            List<(FoodType type, int amount)> contents = CollectShuffleContents(dishes);
            for (int i = 1; i < contents.Count; i++)
                if (contents[i] != contents[0])
                    return true;
            return false;
        }

        // EN: Snapshots nonempty ingredient stacks in plate and half order, retaining every type and quantity together.
        // VI: Chụp các nhóm nguyên liệu còn phần theo thứ tự đĩa và nửa đĩa, giữ loại đi cùng số lượng.
        private static List<(FoodType type, int amount)> CollectShuffleContents(List<Food> dishes)
        {
            List<(FoodType type, int amount)> contents = new List<(FoodType type, int amount)>();
            foreach (Food dish in dishes)
            {
                if (dish.amount > 0)
                    contents.Add((dish.foodType, dish.amount));
                if (dish.IsDouble && dish.SecondAmount > 0)
                    contents.Add((dish.SecondFoodType, dish.SecondAmount));
            }
            return contents;
        }

        // EN: Shuffles stacks across occupied halves when doubles exist, leaving empty halves and plate geometry unchanged.
        // VI: Xáo nhóm nguyên liệu giữa các nửa còn phần khi có đĩa đôi, giữ nguyên nửa trống và hình dạng đĩa.
        private static void ShuffleContents(List<Food> dishes)
        {
            if (!dishes.Exists(dish => dish.IsDouble))
                return;
            List<(FoodType type, int amount)> contents = CollectShuffleContents(dishes);
            for (int i = contents.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (contents[i], contents[j]) = (contents[j], contents[i]);
            }
            int next = 0;
            foreach (Food dish in dishes)
            {
                if (dish.amount > 0)
                {
                    var content = contents[next++];
                    dish.SetShuffleContent(false, content.type, content.amount);
                }
                if (dish.IsDouble && dish.SecondAmount > 0)
                {
                    var content = contents[next++];
                    dish.SetShuffleContent(true, content.type, content.amount);
                }
            }
        }

        // EN: Shuffles contents including double halves, moves only single plates, and retains concealment and ice per position.
        // VI: Xáo nguyên liệu gồm cả hai nửa đĩa đôi, chỉ di chuyển đĩa đơn và giữ nắp cùng băng theo vị trí.
        private IEnumerator Shuffle(ShuffleBooster settings)
        {
            if (!CollectShuffleSpots(out List<Vector2Int> spots, out List<Food> dishes))
                yield break;
            board.OffscreenHidingSuspended = true;
            yield return Glow(dishes, settings);

            int spotCount = spots.Count;
            bool[] spotHidden = new bool[spotCount];
            bool[] spotFrozen = new bool[spotCount];
            int[] spotBlock = new int[spotCount];
            for (int k = 0; k < spotCount; k++)
            {
                spotHidden[k] = dishes[k].isHidden;
                spotFrozen[k] = dishes[k].IsFrozen;
                spotBlock[k] = dishes[k].blockValue;
            }

            List<Coroutine> closing = new List<Coroutine>();
            foreach (Food dish in dishes)
                if (!dish.isHidden)
                    closing.Add(StartCoroutine(dish.CloseLid(settings.lidCloseDuration, settings.lidDropHeight)));
            foreach (Coroutine routine in closing)
                yield return routine;

            foreach (Food dish in dishes)
                dish.SetLidMaterial(settings.rainbowMaterial);
            yield return ShakeLids(dishes, settings, settings.shakeDuration);

            ShuffleContents(dishes);
            foreach (Food dish in dishes)
            {
                if (dish.IsFrozen)
                    dish.SetFrozen(false, 0);
                dish.HideCountBadge();
            }
            List<int> order = MakeShuffledOrder(spots, dishes, spotHidden, spotFrozen);
            yield return SwapDishes(spots, dishes, order, settings);

            foreach (Food dish in dishes)
            {
                dish.SetLidTilt(0f);
                dish.SetLidMaterial(null);
            }
            List<Coroutine> opening = new List<Coroutine>();
            for (int k = 0; k < spotCount; k++)
            {
                Food dish = dishes[order[k]];
                dish.SetFrozen(spotFrozen[k], spotBlock[k]);
                if (spotHidden[k])
                    dish.SetHidden(true);
                else
                    opening.Add(StartCoroutine(dish.FadeOutLid(settings.lidOpenDuration, settings.lidOpenLift, settings.lidOpenScale)));
            }
            foreach (Coroutine routine in opening)
                yield return routine;

            ApplyNewLayout(spots, dishes, order);
            board.OffscreenHidingSuspended = false;
        }

        // EN: Lights every dish up (full brightness) and hides every outline (column heads included) until the board refreshes after the shuffle, then pulses the dish scale once.
        // VI: Cho mọi đĩa sáng lên (độ sáng tối đa) và ẩn mọi viền (kể cả đĩa đầu cột) tới khi board cập nhật lại sau khi xáo, rồi phồng nhẹ một nhịp.
        private static IEnumerator Glow(List<Food> dishes, ShuffleBooster settings)
        {
            List<Vector3> scales = new List<Vector3>(dishes.Count);
            foreach (Food dish in dishes)
            {
                scales.Add(dish.transform.localScale);
                if (dish.Selection != null)
                {
                    dish.Selection.ApplyBrightness(true);
                    dish.Selection.HideOutline();
                }
            }
            for (float time = 0f; time < settings.glowDuration; time += Time.deltaTime)
            {
                float pulse = 1f + (settings.glowScale - 1f) * Mathf.Sin(Mathf.Clamp01(time / settings.glowDuration) * Mathf.PI);
                for (int i = 0; i < dishes.Count; i++)
                    dishes[i].transform.localScale = scales[i] * pulse;
                yield return null;
            }
            for (int i = 0; i < dishes.Count; i++)
                dishes[i].transform.localScale = scales[i];
        }

        // EN: Wobbles every lid for the given time.
        // VI: Lắc nắp của mọi đĩa trong thời gian đã cho.
        private static IEnumerator ShakeLids(List<Food> dishes, ShuffleBooster settings, float duration)
        {
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                for (int i = 0; i < dishes.Count; i++)
                    dishes[i].SetLidTilt(Mathf.Sin((time * settings.shakeFrequency + i * 0.37f) * Mathf.PI * 2f) * settings.shakeAngle);
                yield return null;
            }
        }

        // EN: Keeps double plates at their original indices and shuffles singles, prioritizing needed ingredients in unfrozen first and second rows.
        // VI: Giữ đĩa đôi ở chỉ số gốc và xáo đĩa đơn, ưu tiên nguyên liệu đang cần vào hàng một và hai không đóng băng.
        private static List<int> MakeShuffledOrder(List<Vector2Int> spots, List<Food> dishes, bool[] spotHidden, bool[] spotFrozen)
        {
            int count = spots.Count;
            int[] order = new int[count];
            for (int k = 0; k < count; k++)
                order[k] = -1;
            List<int> freeDishes = new List<int>();
            for (int i = 0; i < count; i++)
            {
                if (dishes[i].IsDouble)
                    order[i] = i;
                else
                    freeDishes.Add(i);
            }

            List<FoodType> needs = new List<FoodType>();
            CustomerManager customers = CustomerManager.Instance;
            if (customers != null)
                customers.GetNeededIngredients(needs);

            int needIndex = PlaceNeeded(needs, 0, 0, spots, dishes, spotHidden, spotFrozen, order, freeDishes);
            if (needIndex >= 0)
                PlaceNeeded(needs, needIndex + 1, 1, spots, dishes, spotHidden, spotFrozen, order, freeDishes);

            for (int i = freeDishes.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (freeDishes[i], freeDishes[j]) = (freeDishes[j], freeDishes[i]);
            }
            int next = 0;
            for (int k = 0; k < count; k++)
                if (order[k] < 0)
                    order[k] = freeDishes[next++];
            return new List<int>(order);
        }

        // EN: Walks the needs list from startNeed and puts the first need that a free dish can satisfy into a random free, unfrozen spot of the given row (hidden spots only as a fallback). Returns the index of the need used, or -1.
        // VI: Duyệt danh sách nhu cầu từ startNeed và đặt nhu cầu đầu tiên có đĩa trống đáp ứng vào một ô ngẫu nhiên còn trống, không đóng băng ở hàng đã cho (ô ẩn chỉ dùng khi không còn ô khác). Trả về chỉ số nhu cầu đã dùng, hoặc -1.
        private static int PlaceNeeded(List<FoodType> needs, int startNeed, int row, List<Vector2Int> spots, List<Food> dishes, bool[] spotHidden, bool[] spotFrozen, int[] order, List<int> freeDishes)
        {
            List<int> rowSpots = new List<int>();
            for (int pass = 0; pass < 2 && rowSpots.Count == 0; pass++)
                for (int k = 0; k < spots.Count; k++)
                    if (spots[k].y == row && order[k] < 0 && !spotFrozen[k] && (pass == 1 || !spotHidden[k]))
                        rowSpots.Add(k);
            if (rowSpots.Count == 0)
                return -1;
            for (int n = startNeed; n < needs.Count; n++)
            {
                List<int> matches = new List<int>();
                foreach (int d in freeDishes)
                    if (dishes[d].foodType == needs[n])
                        matches.Add(d);
                if (matches.Count == 0)
                    continue;
                int dish = matches[Random.Range(0, matches.Count)];
                int spot = rowSpots[Random.Range(0, rowSpots.Count)];
                order[spot] = dish;
                freeDishes.Remove(dish);
                return n;
            }
            return -1;
        }

        // EN: Moves single plates along arcs to their new columns while shaking all lids; double transforms remain untouched.
        // VI: Đưa đĩa đơn theo vòng cung sang cột mới trong lúc rung mọi nắp; giữ nguyên transform đĩa đôi.
        private IEnumerator SwapDishes(List<Vector2Int> spots, List<Food> dishes, List<int> order, ShuffleBooster settings)
        {
            int count = spots.Count;
            Vector3[] from = new Vector3[count];
            Vector3[] to = new Vector3[count];
            Transform[] moving = new Transform[count];
            for (int k = 0; k < count; k++)
            {
                Food dish = dishes[order[k]];
                moving[k] = dish.transform;
                from[k] = moving[k].position;
                if (dish.IsDouble)
                {
                    to[k] = from[k];
                    continue;
                }
                Transform column = board.GetColumnTransform(spots[k].x);
                moving[k].SetParent(column, true);
                to[k] = column.TransformPoint(board.GetRowLocalPosition(spots[k].y));
            }
            float elapsed = 0f;
            while (elapsed < settings.swapDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / settings.swapDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                for (int k = 0; k < count; k++)
                {
                    if (dishes[order[k]].IsDouble)
                        continue;
                    Vector3 position = Vector3.LerpUnclamped(from[k], to[k], eased);
                    if ((from[k] - to[k]).sqrMagnitude > 0.0001f)
                        position.y += settings.swapArcHeight * 4f * t * (1f - t);
                    moving[k].position = position;
                }
                for (int i = 0; i < dishes.Count; i++)
                    dishes[i].SetLidTilt(Mathf.Sin(((elapsed + settings.shakeDuration) * settings.shakeFrequency + i * 0.37f) * Mathf.PI * 2f) * settings.shakeAngle * (1f - t));
                yield return null;
            }
            for (int k = 0; k < count; k++)
                if (!dishes[order[k]].IsDouble)
                    moving[k].localPosition = board.GetRowLocalPosition(spots[k].y);
        }

        // EN: Writes the new column lists (moved dishes in their new spots, frozen/hidden ones unchanged) back to the board.
        // VI: Ghi danh sách cột mới (đĩa đã đổi chỗ ở vị trí mới, đĩa đóng băng/ẩn giữ nguyên) về board.
        private void ApplyNewLayout(List<Vector2Int> spots, List<Food> dishes, List<int> order)
        {
            List<List<Food>> columns = new List<List<Food>>();
            for (int c = 0; c < board.ColumnCount; c++)
                columns.Add(new List<Food>(board.GetColumnDishes(c)));
            for (int k = 0; k < spots.Count; k++)
                columns[spots[k].x][spots[k].y] = dishes[order[k]];
            board.ApplyColumnDishes(columns);
        }

        // EN: Returns true on the frame the mouse or touch is pressed, with its world position (Input System).
        // VI: Trả về true ở frame chuột hoặc ngón tay vừa nhấn, kèm vị trí world (Input System).
        private bool TryGetPointerDown(out Vector2 worldPoint)
        {
            worldPoint = default;
            Pointer pointer = Pointer.current;
            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (pointer == null || cam == null || !pointer.press.wasPressedThisFrame)
                return false;
            Vector2 screen = pointer.position.ReadValue();
            worldPoint = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(cam.transform.position.z)));
            return true;
        }

    }
}
