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
        [Tooltip("Board nguyên liệu mà các booster tác động.")]
        [SerializeField] private FoodBoardManager board;
        [Tooltip("Camera dùng đổi vị trí chạm sang world; để trống dùng Camera.main.")]
        [SerializeField] private Camera inputCamera;
        [Tooltip("Các nút booster do controller quản lý.")]
        [SerializeField] private List<Booster> boosters = new List<Booster>();
        [Tooltip("Object chứa các nút booster, bị ẩn khi đang chọn đĩa bằng PickupHand; để trống thì ẩn từng nút booster.")]
        [SerializeField] private GameObject boosterRoot;
        [Tooltip("Kho giữ món nhận food sau khi dùng booster dọn băng chuyền.")]
        [SerializeField] private FoodHoldingStorage holdingStorage;

        private static BoosterController instance;
        private bool running;
        private PickupHandBooster pickupSelecting;
        private readonly HashSet<Food> pickupTargets = new HashSet<Food>();
        private readonly HashSet<Food> pickupScratch = new HashSet<Food>();
        private int pickupVisibleRows = int.MaxValue;
        private bool pickupCameraMoved;
        private float pickupCameraRestY;
        private Coroutine cameraMove;
        private ConveyorChefMovement collectionBelt;
        private ChefManager collectionChef;
        private CustomerManager collectionCustomers;
        private bool previousBeltPause, previousChefPause, previousServingPause;
        private bool collectionPaused;
        private bool cleanPending;
        private bool collectionInputWasLocked;
        private readonly List<SpriteRenderer> collectionFlyers = new List<SpriteRenderer>();
        private readonly List<ParticleEffect> collectionTrails = new List<ParticleEffect>();

        // EN: Scene instance, if any.
        // VI: Bản trong scene, nếu có.
        public static BoosterController Instance => instance;

        // EN: True while a booster is running its effect.
        // VI: True khi một booster đang chạy hiệu ứng.
        public bool IsRunning => running;

        // EN: Clears the static instance when entering Play Mode with domain reload disabled.
        // VI: Xóa instance tĩnh khi vào Play Mode lúc tắt tải lại miền mã.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            instance = null;
        }

        // EN: Registers the scene instance.
        // VI: Đăng ký bản trong scene.
        private void Awake()
        {
            instance = this;
        }

        // EN: Restores singleton registration after script reload so storage can observe booster ownership.
        // VI: Khôi phục đăng ký singleton sau tải lại script để kho nhận biết booster đang giữ quyền điều khiển.
        private void OnEnable()
        {
            instance = this;
        }

        // EN: Unregisters the scene instance.
        // VI: Hủy đăng ký bản trong scene.
        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        // EN: Releases collection-owned pause and input locks when the controller is disabled.
        // VI: Giải phóng cờ tạm dừng và khóa input do lượt thu giữ khi controller bị tắt.
        private void OnDisable()
        {
            CancelCleanCollection();
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

        // EN: While PickupHand is selecting, drives the selection; otherwise activates the booster whose icon was tapped this frame.
        // VI: Khi PickupHand đang chọn đĩa thì điều khiển việc chọn; ngược lại kích hoạt booster có icon vừa được chạm trong frame này.
        private void Update()
        {
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

        // EN: Runs a booster when its conditions hold; otherwise shakes its icon and logs why. Returns true when it started.
        // VI: Chạy booster khi đủ điều kiện; nếu không thì lắc icon và ghi log lý do. Trả về true khi đã bắt đầu chạy.
        public bool TryActivate(Booster booster)
        {
            string reason = booster == null ? "không có booster"
                : running ? "đang có booster khác chạy"
                : CanUse(booster);
            if (reason != null)
            {
                if (booster != null)
                {
                    booster.PlayDenied();
                    Debug.Log($"[BoosterController] Không dùng được {booster.Type}: {reason}.", booster);
                }
                return false;
            }
            switch (booster)
            {
                case ShuffleBooster shuffle:
                    StartCoroutine(RunLocked(Shuffle(shuffle)));
                    return true;
                case PickupHandBooster pickup:
                    BeginPickupSelection(pickup);
                    return true;
                case AutoCompleteBooster autoComplete:
                    StartCoroutine(RunLocked(AutoComplete(autoComplete)));
                    return true;
                case CleanBrushBooster cleanBrush:
                    cleanPending = true;
                    StartCoroutine(RunLocked(CleanBrush(cleanBrush)));
                    return true;
                default:
                    Debug.LogWarning($"[BoosterController] Booster {booster.Type} chưa có logic.", booster);
                    return false;
            }
        }

        // EN: Returns null when the booster can do something right now, otherwise the reason it cannot (shown in the log).
        // VI: Trả về null khi booster dùng được ngay lúc này, ngược lại trả về lý do không dùng được (ghi ra log).
        private string CanUse(Booster booster)
        {
            if (board == null)
                return "chưa gán board";
            if (board.IsAnimating)
                return "board đang trượt cột hoặc mở nắp";
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
                        return "chưa có khay giữ món sẵn sàng";
                    if (holdingStorage.IsReturning)
                        return "khay đang trả món lên băng chuyền";
                    return ChefManager.Instance != null && ChefManager.Instance.HasFoodOnBelt() ? null : "băng chuyền đang trống";
                case AutoCompleteBooster _:
                    return CustomerManager.Instance != null && CustomerManager.Instance.HasAutoCompleteTarget() ? null : "không có món nào cần hoàn thành";
                default:
                    return "booster chưa có logic";
            }
        }

        // EN: Locks board input during a booster and restores the previous lock and buttons when the routine ends or is cancelled.
        // VI: Khóa thao tác board trong lượt booster và khôi phục khóa trước đó cùng các nút khi routine xong hoặc bị hủy.
        private IEnumerator RunLocked(IEnumerator routine)
        {
            SetBusy(true);
            bool wasLocked = board.InputLocked;
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

        // EN: Returns null when at least one dish within the first maxRows rows can be taken right now (not frozen and the conveyor has enough free slots for all of its portions), otherwise the reason.
        // VI: Trả về null khi có ít nhất một đĩa trong maxRows hàng đầu lấy được ngay lúc này (không đóng băng và băng chuyền đủ slot trống cho mọi phần của đĩa), ngược lại trả về lý do.
        private string CanPickup(int maxRows)
        {
            bool anyUnfrozen = false;
            int freeSlots = CountFreeSlots();
            for (int c = 0; c < board.ColumnCount; c++)
            {
                IReadOnlyList<Food> column = board.GetColumnDishes(c);
                for (int r = 0; r < column.Count && r < maxRows; r++)
                {
                    Food dish = column[r];
                    if (dish == null || dish.IsFrozen)
                        continue;
                    anyUnfrozen = true;
                    if (FitsConveyor(dish, freeSlots))
                        return null;
                }
            }
            return anyUnfrozen ? "băng chuyền không đủ slot trống cho đĩa nào" : "board trống hoặc mọi đĩa đều đóng băng";
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

        // EN: Enters selection mode: locks the board, hides the booster buttons, shows the prompt with its cancel button and highlights every dish that can be taken.
        // VI: Vào chế độ chọn: khóa board, ẩn các nút booster, hiện thông báo kèm nút hủy và làm sáng mọi đĩa lấy được.
        private void BeginPickupSelection(PickupHandBooster settings)
        {
            SetBusy(true);
            board.InputLocked = true;
            pickupSelecting = settings;
            SetBoosterRootVisible(false);
            settings.ShowPrompt(true);
            MovePickupCamera(settings);
            pickupTargets.Clear();
            RefreshPickupTargets(true);
        }

        // EN: Each selection frame: keeps the highlights in sync with the free conveyor slots, then handles a tap on the cancel button or on a dish (a dish that cannot be taken flashes red).
        // VI: Mỗi frame khi đang chọn: cập nhật đĩa sáng theo số slot trống trên băng, rồi xử lý chạm vào nút hủy hoặc vào đĩa (đĩa không lấy được thì nháy đỏ).
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
            Food dish = board.GetColumnDishes(column)[row];
            if (!pickupTargets.Contains(dish))
            {
                if (dish.Selection != null)
                    dish.Selection.PlayRejected();
                Debug.Log($"[BoosterController] Không lấy được đĩa này: {(dish.IsFrozen ? "đĩa đang đóng băng" : "băng chuyền không đủ slot trống")}.", dish);
                return;
            }
            PickupHandBooster settings = pickupSelecting;
            pickupSelecting = null;
            settings.ShowPrompt(false);
            StartCoroutine(RunLocked(Pickup(settings, column, row)));
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

        // EN: Cancel button: hides the prompt, slides the camera back, restores the normal board look, unlocks the board and shows the booster buttons again. No use is spent.
        // VI: Nút hủy: ẩn thông báo, trượt camera về chỗ cũ, trả board về giao diện bình thường, mở khóa board và hiện lại các nút booster. Không tính lượt.
        private void CancelPickupSelection()
        {
            pickupSelecting.ShowPrompt(false);
            pickupSelecting = null;
            pickupTargets.Clear();
            RestorePickupCamera();
            board.RefreshSelection();
            board.InputLocked = false;
            SetBusy(false);
            SetBoosterRootVisible(true);
        }

        // EN: Lifts the chosen dish, reveals every linked lid and picks the entire group; restores the chosen dish if capacity changes.
        // VI: Nhấc đĩa chọn, mở mọi nắp nối và lấy cả nhóm; đặt lại đĩa chọn nếu sức chứa thay đổi.
        private IEnumerator Pickup(PickupHandBooster settings, int column, int row)
        {
            Food dish = board.GetColumnDishes(column)[row];
            HighlightOnly(dish);
            int[] sortingOrders = BoostSorting(dish, settings.sortingBoost);

            Coroutine reveal = StartCoroutine(board.RevealPickGroup(dish, settings.revealDuration));
            Vector3 dishRest = dish.transform.position;
            Vector3 dishScale = dish.transform.localScale;
            yield return LiftDish(dish, settings, dishRest, dishScale);
            if (reveal != null)
                yield return reveal;

            if (!board.TryPickDishAt(column, row))
            {
                dish.transform.position = dishRest;
                dish.transform.localScale = dishScale;
                RestoreSorting(dish, sortingOrders, settings.sortingBoost);
                Debug.Log("[BoosterController] PickupHand: băng chuyền đã hết chỗ trong lúc nhấc đĩa, đặt đĩa lại.", dish);
            }

            while (board.IsAnimating)
                yield return null;
            Coroutine cameraBack = RestorePickupCamera();
            if (cameraBack != null)
                yield return cameraBack;
            board.RefreshSelection();
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

        // EN: Lifts the dish by liftHeight while it grows slightly.
        // VI: Nhấc đĩa lên liftHeight trong lúc đĩa phồng nhẹ.
        private static IEnumerator LiftDish(Food dish, PickupHandBooster settings, Vector3 dishRest, Vector3 dishScale)
        {
            for (float time = 0f; time < settings.liftDuration; time += Time.deltaTime)
            {
                float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(time / settings.liftDuration), 2f);
                dish.transform.position = dishRest + Vector3.up * settings.liftHeight * ease;
                dish.transform.localScale = dishScale * Mathf.Lerp(1f, settings.liftScale, ease);
                yield return null;
            }
            dish.transform.position = dishRest + Vector3.up * settings.liftHeight;
            dish.transform.localScale = dishScale * settings.liftScale;
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
                sprite = board.IconConfig != null ? board.IconConfig.GetIcon(type) : null;
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
        private List<SpriteRenderer> PopIngredients(AutoCompleteBooster settings, CustomerManager.AutoCompleteJob job, Vector3 point, int layer)
        {
            List<SpriteRenderer> pieces = new List<SpriteRenderer>(job.ingredients.Count);
            foreach (FoodType type in job.ingredients)
            {
                Sprite sprite = board.IconConfig != null ? board.IconConfig.GetIcon(type, FoodState.Prepared) : null;
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

        // EN: Pauses belt activity, extends the tray, collects food in staggered flights and retracts after the final landing.
        // VI: Tạm dừng hoạt động băng, kéo khay ra, thu food bằng chuyến bay nối tiếp rồi thu khay sau lần đáp cuối.
        private IEnumerator CleanBrush(CleanBrushBooster settings)
        {
            ChefManager chef = ChefManager.Instance;
            CustomerManager customers = CustomerManager.Instance;
            ConveyorChefMovement belt = FindFirstObjectByType<ConveyorChefMovement>();
            if (chef == null || belt == null || holdingStorage == null || !holdingStorage.IsReady)
                yield break;
            PauseForCollection(belt, chef, customers);
            try
            {
                yield return holdingStorage.MoveDrawer(true);
                if (!holdingStorage.IsReady)
                    yield break;
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
                yield return holdingStorage.MoveDrawer(false);
            }
            finally
            {
                RestoreCollectionPause();
            }
        }

        // EN: Remembers existing pause flags before suspending belt movement, preparation and new serving flights.
        // VI: Ghi nhớ cờ tạm dừng cũ trước khi dừng băng, sơ chế và chuyến phục vụ mới.
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
            chef.CancelPreparation();
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

        // EN: Lifts plate and highlighted food together, then drops the plate while food flies into storage at original size.
        // VI: Nhấc đĩa và food sáng cùng nhau, rồi thả đĩa xuống khi food bay vào kho ở kích thước gốc.
        private IEnumerator CollectIntoTray(CleanBrushBooster settings, ConveyorSlot slot, FoodHoldingStorage.Portion portion, Sprite sprite, Vector3 start, int layer, System.Action landed)
        {
            SpriteRenderer flyer = RentFx(sprite, layer, settings.sortingOrder);
            Material normalMaterial = flyer.sharedMaterial;
            if (settings.collectionHighlight != null) flyer.sharedMaterial = settings.collectionHighlight;
            var highlight = new MaterialPropertyBlock();
            ParticleEffect trail = null;
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
                    Vector3 target = holdingStorage.GetPosition(portion);
                    Vector3 control = (from + target) * 0.5f + Vector3.up * settings.flyArc;
                    Vector3 position = (1f-t)*(1f-t)*from + 2f*(1f-t)*t*control + t*t*target;
                    flyer.transform.position = position;
                    if (trail != null) trail.transform.position = position;
                    flyer.transform.localScale = portion.worldScale;
                    flyer.transform.rotation = Quaternion.Euler(0f, 0f, -8f * Mathf.Sin(t * Mathf.PI));
                    yield return null;
                }
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
                if (holdingStorage != null) holdingStorage.Reveal(portion);
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

#if UNITY_EDITOR
        // EN: Editor-only: assigns the board, camera, booster list and the root object that holds the booster buttons (used by the setup tool).
        // VI: Chỉ dùng trong editor: gán board, camera, danh sách booster và object root chứa các nút booster (công cụ setup gọi).
        public void Configure(FoodBoardManager boardManager, Camera camera, List<Booster> buttons, GameObject root)
        {
            board = boardManager;
            inputCamera = camera;
            boosters = buttons;
            if (root != null)
                boosterRoot = root;
        }
#endif
    }
}
