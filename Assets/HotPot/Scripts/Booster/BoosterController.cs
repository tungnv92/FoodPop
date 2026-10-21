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

        private static BoosterController instance;
        private bool running;
        private PickupHandBooster pickupSelecting;
        private readonly HashSet<Food> pickupTargets = new HashSet<Food>();
        private readonly HashSet<Food> pickupScratch = new HashSet<Food>();
        private int pickupVisibleRows = int.MaxValue;
        private bool pickupCameraMoved;
        private float pickupCameraRestY;
        private Coroutine cameraMove;

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

        // EN: Unregisters the scene instance.
        // VI: Hủy đăng ký bản trong scene.
        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
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
            booster.PlayActivated();
            switch (booster)
            {
                case ShuffleBooster shuffle:
                    StartCoroutine(RunLocked(AfterPress(booster, Shuffle(shuffle))));
                    return true;
                case PickupHandBooster pickup:
                    StartCoroutine(BeginPickupAfterPress(pickup));
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
                        return "board còn ít hơn 2 đĩa";
                    return HasDifferentContent(dishes) ? null : "mọi đĩa đều giống nhau";
                case PickupHandBooster pickup:
                    return CanPickup(pickup.maxVisibleRows);
                default:
                    return "booster chưa có logic";
            }
        }

        // EN: Runs a booster routine with the board input locked. The routine runs as its own coroutine, so even if it throws, this wrapper still resumes, unlocks the board and the controller and shows the booster buttons again (they may have been hidden by PickupHand).
        // VI: Chạy routine của booster trong lúc khóa thao tác board. Routine chạy thành coroutine riêng nên kể cả khi nó lỗi, hàm bọc này vẫn chạy tiếp, mở khóa board cùng controller và hiện lại các nút booster (PickupHand có thể đã ẩn chúng).
        private IEnumerator RunLocked(IEnumerator routine)
        {
            running = true;
            board.InputLocked = true;
            yield return StartCoroutine(routine);
            board.InputLocked = false;
            running = false;
            SetBoosterRootVisible(true);
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

        // EN: True when every portion of the dish fits into the free conveyor slots (same count a normal pick reserves).
        // VI: True khi mọi phần của đĩa vừa với số slot trống trên băng (cùng số slot mà chọn thông thường giữ).
        private static bool FitsConveyor(Food dish, int freeSlots)
        {
            return Mathf.Max(1, dish.amount) <= freeSlots;
        }

        // EN: Waits for the booster's click effect to finish, then runs the booster routine, so the action starts in rhythm with the button (the controller and board are already locked by RunLocked during the wait).
        // VI: Chờ hiệu ứng bấm của booster chạy xong rồi mới chạy routine của booster, để hành động bắt đầu khớp nhịp với nút (controller và board đã được RunLocked khóa trong lúc chờ).
        private static IEnumerator AfterPress(Booster booster, IEnumerator routine)
        {
            if (booster.PressDuration > 0f)
                yield return new WaitForSeconds(booster.PressDuration);
            yield return routine;
        }

        // EN: Locks the controller and board right away (no second tap, no board pick), waits for the click effect to finish, then enters selection mode.
        // VI: Khóa controller và board ngay (không cho bấm lần hai, không cho chọn đĩa trên board), chờ hiệu ứng bấm chạy xong rồi mới vào chế độ chọn.
        private IEnumerator BeginPickupAfterPress(PickupHandBooster settings)
        {
            running = true;
            board.InputLocked = true;
            if (settings.PressDuration > 0f)
                yield return new WaitForSeconds(settings.PressDuration);
            BeginPickupSelection(settings);
        }

        // EN: Enters selection mode: locks the board, hides the booster buttons, shows the prompt with its cancel button and highlights every dish that can be taken.
        // VI: Vào chế độ chọn: khóa board, ẩn các nút booster, hiện thông báo kèm nút hủy và làm sáng mọi đĩa lấy được.
        private void BeginPickupSelection(PickupHandBooster settings)
        {
            running = true;
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
            running = false;
            SetBoosterRootVisible(true);
        }

        // EN: PickupHand effect: only the chosen dish stays lit, the dish lifts and grows slightly (opening its lid if hidden), then it is picked exactly like a normal pick (portions fly to the conveyor, dishes below shift up, ice takes damage). If the conveyor filled up meanwhile the dish is set back down with a red flash. Waits for the column to finish sliding and the camera to slide back before the booster buttons come back.
        // VI: Hiệu ứng PickupHand: chỉ đĩa được chọn còn sáng, đĩa được nhấc lên và phồng nhẹ (mở nắp nếu đang ẩn), rồi được lấy y như chọn thông thường (các phần bay lên băng, đĩa bên dưới dồn lên, băng bị trừ). Nếu trong lúc đó băng chuyền đã đầy thì đĩa được đặt lại kèm nháy đỏ. Chờ cột trượt xong và camera trượt về rồi mới hiện lại các nút booster.
        private IEnumerator Pickup(PickupHandBooster settings, int column, int row)
        {
            Food dish = board.GetColumnDishes(column)[row];
            HighlightOnly(dish);
            int[] sortingOrders = BoostSorting(dish, settings.sortingBoost);

            Coroutine reveal = dish.isHidden ? StartCoroutine(dish.FadeOutLid(settings.revealDuration)) : null;
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

        // EN: Slides the camera down just enough to show min(maxVisibleRows, rows the board has) rows above the prompt; never moves it up. Remembers the old Y so it can slide back. Dishes below the screen turn on by themselves (the board hides/shows them by the camera's bottom edge).
        // VI: Trượt camera xuống vừa đủ để thấy min(maxVisibleRows, số hàng board đang có) hàng phía trên thông báo; không bao giờ đẩy camera lên. Ghi nhớ Y cũ để trượt về. Đĩa dưới màn hình tự bật lại (board bật/tắt đĩa theo mép dưới camera).
        private void MovePickupCamera(PickupHandBooster settings)
        {
            int boardRows = GetBoardMaxRows();
            pickupVisibleRows = Mathf.Min(settings.maxVisibleRows, boardRows);
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

        // EN: Slides the camera back to where it was before PickupHand (if it was moved) and clears the visible-row limit; returns the running slide, or null.
        // VI: Trượt camera về vị trí trước khi dùng PickupHand (nếu đã dịch) và bỏ giới hạn số hàng; trả về coroutine đang trượt, hoặc null.
        private Coroutine RestorePickupCamera()
        {
            pickupVisibleRows = int.MaxValue;
            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (!pickupCameraMoved || cam == null)
                return null;
            pickupCameraMoved = false;
            PickupHandBooster settings = null;
            foreach (Booster booster in boosters)
                if (booster is PickupHandBooster pickup)
                    settings = pickup;
            return StartCameraMove(cam, pickupCameraRestY, settings != null ? settings.cameraMoveDuration : 0.35f);
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

        // EN: Keeps the white outline and full brightness only on the chosen dish; every other dish turns plain (used while the dish is lifted and picked).
        // VI: Chỉ giữ viền trắng và độ sáng tối đa ở đĩa được chọn; mọi đĩa khác về trạng thái thường (dùng trong lúc nhấc và lấy đĩa).
        private void HighlightOnly(Food chosen)
        {
            for (int c = 0; c < board.ColumnCount; c++)
                foreach (Food dish in board.GetColumnDishes(c))
                    if (dish != null && dish.Selection != null)
                        dish.Selection.SetSelectable(dish == chosen);
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

        // ---------------------------------------------------------------- Shuffle

        // EN: Collects every board position (column, row) and its dish. Returns false when there are fewer than two dishes.
        // VI: Lấy mọi vị trí trên board (cột, hàng) cùng đĩa ở đó. Trả về false khi có ít hơn hai đĩa.
        private bool CollectShuffleSpots(out List<Vector2Int> spots, out List<Food> dishes)
        {
            spots = new List<Vector2Int>();
            dishes = new List<Food>();
            for (int c = 0; c < board.ColumnCount; c++)
            {
                IReadOnlyList<Food> column = board.GetColumnDishes(c);
                for (int r = 0; r < column.Count; r++)
                {
                    Food dish = column[r];
                    if (dish == null)
                        continue;
                    spots.Add(new Vector2Int(c, r));
                    dishes.Add(dish);
                }
            }
            return dishes.Count >= 2;
        }

        // EN: True when not every dish holds the same food and amount; otherwise shuffling changes nothing.
        // VI: True khi các đĩa không cùng loại và cùng số lượng; nếu giống hết thì xáo không đổi gì.
        private static bool HasDifferentContent(List<Food> dishes)
        {
            for (int i = 1; i < dishes.Count; i++)
                if (!SameContent(dishes[i], dishes[0]))
                    return true;
            return false;
        }

        // EN: True when two dishes hold the same food and amount.
        // VI: True khi hai đĩa cùng loại và cùng số lượng.
        private static bool SameContent(Food a, Food b)
        {
            return a.foodType == b.foodType && a.amount == b.amount;
        }

        // EN: Shuffle: glow → close lids → shake with rainbow → swap places → open lids. Only the ingredient (food and amount) moves: the hidden and frozen states (lid, ice and its value) belong to the board position, so each position keeps its own after the swap. Dishes below the screen are kept active for the whole shuffle so the ones flying up into view are visible; the board turns them off again once the new layout is applied.
        // VI: Xáo trộn: sáng lên → đóng nắp → rung kèm cầu vồng → đổi chỗ → mở nắp. Chỉ nguyên liệu (loại và số lượng) đổi chỗ: trạng thái ẩn và đóng băng (nắp, băng và giá trị băng) thuộc về vị trí trên board nên mỗi vị trí giữ nguyên trạng thái của mình sau khi đổi. Đĩa dưới màn hình được giữ bật suốt quá trình xáo để đĩa bay lên vào màn hình vẫn nhìn thấy; board tắt lại chúng sau khi áp bố cục mới.
        private IEnumerator Shuffle(ShuffleBooster settings)
        {
            if (!CollectShuffleSpots(out List<Vector2Int> spots, out List<Food> dishes))
                yield break;
            board.OffscreenHidingSuspended = true;
            List<Food> allDishes = new List<Food>();
            for (int c = 0; c < board.ColumnCount; c++)
                allDishes.AddRange(board.GetColumnDishes(c));

            yield return Glow(allDishes, settings);

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

        // EN: Builds the new layout as order[k] = index of the dish that goes to spot k. The first ingredient still needed (CustomerManager priority) that exists on the board goes to a row-1 spot (column head) that is not frozen; the next needed ingredient goes to a row-2 spot that is not frozen (preferably not hidden); any column. Every other dish is shuffled randomly into the remaining spots.
        // VI: Dựng bố cục mới dưới dạng order[k] = chỉ số đĩa được đưa tới vị trí k. Nguyên liệu đang cần đầu tiên (theo ưu tiên của CustomerManager) có trên board được đưa lên một ô hàng 1 (đầu cột) không đóng băng; nguyên liệu cần tiếp theo được đưa vào một ô hàng 2 không đóng băng (ưu tiên ô không ẩn); cột nào cũng được. Các đĩa còn lại xáo ngẫu nhiên vào các ô còn trống.
        private static List<int> MakeShuffledOrder(List<Vector2Int> spots, List<Food> dishes, bool[] spotHidden, bool[] spotFrozen)
        {
            int count = spots.Count;
            int[] order = new int[count];
            for (int k = 0; k < count; k++)
                order[k] = -1;
            List<int> freeDishes = new List<int>();
            for (int i = 0; i < count; i++)
                freeDishes.Add(i);

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

        // EN: Moves each dish order[k] from where it is to spot k along an arc (lids keep shaking), reparenting it to the target column.
        // VI: Đưa đĩa order[k] từ chỗ hiện tại tới vị trí k theo vòng cung (nắp vẫn rung), đổi cha sang cột đích.
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
