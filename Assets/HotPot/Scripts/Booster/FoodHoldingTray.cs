using System.Collections;
using UnityEngine;

namespace FoodPop
{
    // EN: Animates the holding drawer independently of food collection and order matching.
    // VI: Điều khiển ngăn kéo giữ món độc lập với việc thu food và tìm order.
    [ExecuteAlways]
    public sealed class FoodHoldingTray : MonoBehaviour
    {
        public enum DrawerState { Retracted, Extending, Extended, Retracting }

        [Tooltip("Phần khay di chuyển, chứa lớp sau, food và lớp trước.")]
        [SerializeField] private Transform drawer;
        [Tooltip("Vị trí cục bộ khi khay kéo ra hoàn toàn.")]
        [SerializeField] private Vector3 extendedPosition;
        [Tooltip("Độ dịch vào vách so với vị trí kéo ra.")]
        [SerializeField] private Vector3 retractOffset = new Vector3(0f, 0.65f, 0f);
        [Tooltip("Thời gian kéo ra hoặc thu vào hoàn toàn, tính bằng giây.")]
        [SerializeField, Min(0.01f)] private float slideDuration = 0.3f;
        [Tooltip("Bắt đầu với khay thu gọn khi vào màn chơi.")]
        [SerializeField] private bool startRetracted = true;
        [Tooltip("Vật liệu cắt phần khay còn nằm trong vách.")]
        [SerializeField] private Material clippedMaterial;
        [Tooltip("Độ cao mép vách trong tọa độ cục bộ của khay.")]
        [SerializeField] private float wallEdgeY = 0.46f;
        [Tooltip("Thời gian giữ khay ở vị trí kéo ra khi chạy thử, tính bằng giây.")]
        [SerializeField, Min(0f)] private float testHoldDuration = 1f;
        private Coroutine slide;
        private Coroutine motionTest;
        private float extension;
        private MaterialPropertyBlock clipProperties;
        public DrawerState State { get; private set; }

        // EN: Initializes runtime position and restores nonserialized clipping after editor reload or leaving Play Mode.
        // VI: Khởi tạo vị trí lúc chơi và khôi phục dữ liệu cắt không được lưu sau tải lại editor hoặc thoát Play Mode.
        private void OnEnable()
        {
            if (Application.IsPlaying(gameObject))
                SetImmediate(!startRetracted);
            else if (drawer != null)
                RefreshClipping();
        }

        // EN: Refreshes the stationary clip plane after parent movement and for food added while the drawer is idle.
        // VI: Cập nhật mặt cắt cố định sau khi di chuyển cha và cho food thêm vào khi khay đứng yên.
        private void LateUpdate()
        {
            if (drawer != null)
                RefreshClipping();
        }

        // EN: Cancels runtime movement and tests on disable without changing authored prefab or editor positions.
        // VI: Hủy chuyển động và lượt thử lúc tắt trong game, không thay đổi vị trí thiết kế của prefab hoặc editor.
        private void OnDisable()
        {
            if (motionTest != null)
                StopCoroutine(motionTest);
            motionTest = null;
            if (Application.IsPlaying(gameObject))
                SetImmediate(!startRetracted);
        }

        // EN: Tests a full cycle only on an empty tray; repeated calls restart the test.
        // VI: Chạy thử chu kỳ đầy đủ chỉ khi khay trống; gọi lại sẽ bắt đầu lượt thử mới.
        [ContextMenu("Test/Kéo ra rồi thu vào")]
        public void TestMotion()
        {
            if (!Application.isPlaying || !isActiveAndEnabled || drawer == null)
            {
                Debug.LogWarning("[FoodHoldingTray] Cần vào Play Mode và bật khay đã gán Drawer để chạy thử.", this);
                return;
            }
            if (HasStoredFood())
            {
                Debug.LogWarning("[FoodHoldingTray] Khay còn food nên không chạy thử thu vào.", this);
                return;
            }
            if (motionTest != null)
                StopCoroutine(motionTest);
            SetImmediate(false);
            motionTest = StartCoroutine(RunMotionTest());
        }

        // EN: Waits for test movement with scaled time, leaving the tray open if food arrives during the test.
        // VI: Chờ chuyển động thử theo thời gian game, giữ khay mở nếu food được thêm trong lượt thử.
        private IEnumerator RunMotionTest()
        {
            RequestSlide(true);
            while (State != DrawerState.Extended)
                yield return null;
            yield return new WaitForSeconds(Mathf.Max(0f, testHoldDuration));
            RequestSlide(false);
            while (State != DrawerState.Retracted && !HasStoredFood())
                yield return null;
            motionTest = null;
        }

        // EN: Extends the drawer from its current position; repeated requests do not restart it.
        // VI: Kéo khay ra từ vị trí hiện tại; yêu cầu lặp không khởi động lại chuyển động.
        [ContextMenu("Kéo khay ra")]
        public void Extend()
        {
            RequestSlide(true);
        }

        // EN: Retracts the drawer from its current position without changing its contents.
        // VI: Thu khay vào từ vị trí hiện tại mà không thay đổi food bên trong.
        [ContextMenu("Thu gọn khay")]
        public void Retract()
        {
            RequestSlide(false);
        }

        // EN: Handles previews and runtime reversal, forcing the drawer open while it contains food.
        // VI: Xử lý xem trước và đảo chiều lúc chơi, buộc khay kéo ra khi còn food bên trong.
        private void RequestSlide(bool open)
        {
            open = open || HasStoredFood();
            if (!Application.isPlaying)
            {
                SetImmediate(open);
                return;
            }
            if (!isActiveAndEnabled || drawer == null)
                return;
            if (open ? State == DrawerState.Extended || State == DrawerState.Extending
                     : State == DrawerState.Retracted || State == DrawerState.Retracting)
                return;
            if (slide != null)
                StopCoroutine(slide);
            slide = StartCoroutine(SlideTo(open));
        }

        // EN: Slides the drawer to its endpoint using scaled time and preserves smooth direction changes.
        // VI: Trượt khay tới vị trí cuối bằng thời gian game và giữ đổi chiều liên tục.
        private IEnumerator SlideTo(bool open)
        {
            State = open ? DrawerState.Extending : DrawerState.Retracting;
            float from = extension;
            float target = open ? 1f : 0f;
            float duration = Mathf.Max(0.01f, slideDuration * Mathf.Abs(target - from));
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                ApplyExtension(Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, elapsed / duration)));
                yield return null;
            }
            ApplyExtension(target);
            State = open ? DrawerState.Extended : DrawerState.Retracted;
            slide = null;
        }

        // EN: Sets the drawer endpoint immediately while keeping nonempty storage extended.
        // VI: Đặt ngay vị trí cuối của khay, giữ kho còn food ở trạng thái kéo ra.
        public void SetImmediate(bool open)
        {
            open = open || HasStoredFood();
            if (slide != null)
                StopCoroutine(slide);
            slide = null;
            ApplyExtension(open ? 1f : 0f);
            State = open ? DrawerState.Extended : DrawerState.Retracted;
        }

        // EN: Checks live storage contents before allowing any retraction, including manual test calls.
        // VI: Kiểm tra food trong kho trước khi cho thu khay, gồm cả lời gọi thử thủ công.
        private bool HasStoredFood()
        {
            FoodHoldingStorage storage = GetComponent<FoodHoldingStorage>();
            return storage != null && storage.Count > 0;
        }

        // EN: Moves the drawer without scaling and clips the part inside the stationary wall.
        // VI: Di chuyển khay không đổi kích thước và che phần nằm trong vách cố định.
        private void ApplyExtension(float value)
        {
            extension = value;
            if (drawer != null)
            {
                drawer.localPosition = extendedPosition + retractOffset * (1f - value);
                RefreshClipping();
            }
        }

        // EN: Restores stationary wall clipping while preserving sorting and unrelated renderer overrides.
        // VI: Khôi phục cắt theo vách cố định và giữ thứ tự vẽ cùng thuộc tính ghi đè khác của renderer.
        private void RefreshClipping()
        {
            if (clippedMaterial == null)
                return;
            if (clipProperties == null)
                clipProperties = new MaterialPropertyBlock();
            float top = transform.TransformPoint(new Vector3(0f, wallEdgeY, 0f)).y;
            foreach (SpriteRenderer renderer in drawer.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.sharedMaterial != clippedMaterial)
                    renderer.sharedMaterial = clippedMaterial;
                if (renderer.maskInteraction != SpriteMaskInteraction.None)
                    renderer.maskInteraction = SpriteMaskInteraction.None;
                renderer.GetPropertyBlock(clipProperties);
                clipProperties.SetFloat("_ClipTop", top);
                renderer.SetPropertyBlock(clipProperties);
            }
        }

#if UNITY_EDITOR
        // EN: Wires the movable drawer and clipping material while retaining its authored extended position.
        // VI: Gán phần khay chuyển động và vật liệu cắt, giữ vị trí kéo ra đã thiết kế.
        public void Configure(Transform movingDrawer, Material material = null)
        {
            drawer = movingDrawer;
            extendedPosition = movingDrawer.localPosition;
            clippedMaterial = material;
            SetImmediate(false);
        }
#endif
    }
}
