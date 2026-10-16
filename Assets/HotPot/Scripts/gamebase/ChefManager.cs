using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Coordinates chef preparation jobs and conveyor slot availability through one scene singleton.
    // VI: Điều phối công việc sơ chế và slot băng chuyền qua một singleton trong scene.
    [DisallowMultipleComponent]
    public class ChefManager : MonoBehaviour
    {
        private static ChefManager instance;
        private static readonly ConveyorSlot[] EmptySlots = new ConveyorSlot[0];
        private static readonly int IsChoppingId = Animator.StringToHash("IsChopping");

        [Tooltip("Animator đầu bếp với tham số điều khiển trạng thái cắt.")]
        [SerializeField] private Animator chefAnimator;
        [Tooltip("Component điều khiển đường đi và chuyển động các đĩa trên băng chuyền.")]
        [SerializeField] private ConveyorChefMovement conveyorMovement;
        [Tooltip("Thời gian sơ chế một nguyên liệu, tính bằng giây.")]
        [Min(0.01f)] [SerializeField] private float preparationDuration = 1.5f;

        private Coroutine preparationRoutine;
        private Food preparingFood;
        private ConveyorSlot preparingSlot;

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

        // EN: Indicates whether the chef currently has an unfinished preparation job.
        // VI: Cho biết đầu bếp đang có công việc sơ chế chưa hoàn thành.
        public bool IsBusy => preparationRoutine != null;

        // EN: Returns the food currently being prepared, or null while idle.
        // VI: Trả về nguyên liệu đang sơ chế, hoặc null khi rảnh.
        public Food CurrentFood => preparingFood;

        // EN: Clears stale static state when entering Play Mode, including with domain reload disabled.
        // VI: Xóa trạng thái tĩnh cũ khi vào Play Mode, kể cả khi tắt tải lại miền mã.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            instance = null;
        }

        // EN: Registers this manager and removes only duplicate components, preserving scene objects.
        // VI: Đăng ký manager và chỉ xóa component trùng, giữ nguyên các đối tượng scene.
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
        }

        // EN: Cancels incomplete work when disabled so it cannot prepare food later unexpectedly.
        // VI: Hủy việc chưa hoàn thành khi tắt để tránh sơ chế nguyên liệu muộn ngoài ý muốn.
        private void OnDisable()
        {
            if (instance == this)
                CancelPreparation();
        }

        // EN: Releases this manager's singleton registration when its scene is destroyed.
        // VI: Giải phóng đăng ký singleton của manager khi scene bị hủy.
        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        // EN: Counts usable empty slots from the conveyor's current authoritative list.
        // VI: Đếm slot rỗng có thể dùng từ danh sách hiện tại của băng chuyền.
        public int GetEmptySlotCount()
        {
            int count = 0;
            foreach (ConveyorSlot slot in Slots)
            {
                if (slot != null && slot.isActiveAndEnabled && slot.isEmpty)
                    count++;
            }
            return count;
        }

        // EN: Finds the closest active empty slot, returning false when no slot is available.
        // VI: Tìm slot rỗng đang hoạt động gần nhất, trả về false khi không còn slot khả dụng.
        public bool TryGetNearestEmptySlot(Vector3 worldPosition, out ConveyorSlot nearest)
        {
            nearest = null;
            float bestDistance = float.MaxValue;
            foreach (ConveyorSlot slot in Slots)
            {
                if (slot == null || !slot.isActiveAndEnabled || !slot.isEmpty)
                    continue;
                float distance = (slot.transform.position - worldPosition).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    nearest = slot;
                }
            }
            return nearest != null;
        }

        // EN: Collects the given number of active empty slots closest to a world position (nearest first); returns false and leaves the list empty when there are not enough.
        // VI: Lấy đủ số slot rỗng đang hoạt động gần một vị trí world nhất (gần trước); trả về false và để danh sách rỗng khi không đủ slot.
        public bool TryGetNearestEmptySlots(Vector3 worldPosition, int count, List<ConveyorSlot> result)
        {
            result.Clear();
            if (count <= 0)
                return false;
            foreach (ConveyorSlot slot in Slots)
            {
                if (slot != null && slot.isActiveAndEnabled && slot.isEmpty)
                    result.Add(slot);
            }
            if (result.Count < count)
            {
                result.Clear();
                return false;
            }
            result.Sort((a, b) => (a.transform.position - worldPosition).sqrMagnitude
                .CompareTo((b.transform.position - worldPosition).sqrMagnitude));
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

        // EN: Starts one raw-food job on an owned active slot, rejecting concurrent or invalid work and food that is still flying in from the board.
        // VI: Bắt đầu sơ chế một nguyên liệu thô trên slot đang hoạt động thuộc manager, từ chối việc trùng, không hợp lệ hoặc món vẫn đang bay từ board lên.
        public bool TryPrepareSlot(ConveyorSlot slot)
        {
            if (!isActiveAndEnabled || IsBusy || chefAnimator == null || !ContainsSlot(slot) ||
                !slot.isActiveAndEnabled || slot.isEmpty || slot.IsFoodArriving || slot.food == null || !slot.food.IsRaw)
                return false;
            preparingSlot = slot;
            preparingFood = slot.food;
            preparationRoutine = StartCoroutine(PrepareFood());
            return true;
        }

        // EN: Runs the chop animation and prepares food only if the original slot still holds it at completion.
        // VI: Chạy hoạt ảnh cắt và chỉ sơ chế khi slot gốc vẫn giữ nguyên liệu đó lúc hoàn thành.
        private IEnumerator PrepareFood()
        {
            SetChopping(true);
            yield return new WaitForSeconds(Mathf.Max(0.01f, preparationDuration));
            if (preparingSlot != null && preparingSlot.isActiveAndEnabled && !preparingSlot.isEmpty &&
                preparingFood != null && preparingSlot.food == preparingFood)
                preparingFood.Prepare();
            preparingSlot = null;
            preparingFood = null;
            preparationRoutine = null;
            SetChopping(false);
        }

        // EN: Stops the current job without changing the food's processing state and returns the chef to idle.
        // VI: Dừng công việc hiện tại, giữ trạng thái nguyên liệu và đưa đầu bếp về nghỉ.
        public void CancelPreparation()
        {
            if (preparationRoutine != null)
                StopCoroutine(preparationRoutine);
            preparationRoutine = null;
            preparingSlot = null;
            preparingFood = null;
            SetChopping(false);
        }

        // EN: Starts or pauses conveyor motion without changing slot occupancy.
        // VI: Bật hoặc dừng băng chuyền mà không đổi nguyên liệu trên các slot.
        public void SetConveyorMoving(bool moving)
        {
            if (conveyorMovement != null)
                conveyorMovement.SetMoving(moving);
        }

        // EN: Updates the chef animator's existing Idle/Chop boolean parameter.
        // VI: Cập nhật tham số bật/tắt trạng thái nghỉ và cắt có sẵn của animator đầu bếp.
        private void SetChopping(bool chopping)
        {
            if (chefAnimator != null && chefAnimator.runtimeAnimatorController != null)
                chefAnimator.SetBool(IsChoppingId, chopping);
        }
    }
}
