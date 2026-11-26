using Spine.Unity;
using UnityEngine;

namespace FoodPop
{
    // EN: Spine body of a customer, authored in the scene as a child of a portrait SpriteRenderer: replaces that sprite on screen while keeping its position, scale and sorting.
    // VI: Thân spine của khách, đặt sẵn trong scene làm con của SpriteRenderer ảnh khách: thay ảnh đó trên màn hình nhưng giữ vị trí, scale và thứ tự vẽ của nó.
    [DisallowMultipleComponent]
    public sealed class CustomerSpine : MonoBehaviour
    {
        [Tooltip("Ảnh khách mà spine này thay thế (thường là cha trực tiếp).")]
        [SerializeField] private SpriteRenderer host;
        [Tooltip("SkeletonAnimation của khách.")]
        [SerializeField] private SkeletonAnimation skeleton;
        [Tooltip("Renderer của skeleton, dùng để chép thứ tự vẽ của ảnh khách.")]
        [SerializeField] private MeshRenderer meshRenderer;

        private string currentAnimation;
        private bool physicsOff;

#if UNITY_EDITOR
        // EN: Fills missing references from the parent sprite and this object's SkeletonAnimation and MeshRenderer.
        // VI: Tự gán tham chiếu còn thiếu từ sprite cha, SkeletonAnimation và MeshRenderer trên object này.
        private void Reset()
        {
            host = transform.parent != null ? transform.parent.GetComponent<SpriteRenderer>() : null;
            skeleton = GetComponent<SkeletonAnimation>();
            meshRenderer = GetComponent<MeshRenderer>();
        }
#endif

        // EN: Prepares a waiting-queue body: builds the skeleton now (so pooled clones do not initialize mid-game) and turns off every physics constraint, which these small background customers do not need (also after later character swaps).
        // VI: Chuẩn bị thân spine cho hàng chờ: dựng skeleton ngay (để bản nhân bản trong pool không khởi tạo giữa lúc chơi) và tắt mọi ràng buộc physics, vốn không cần cho khách nhỏ phía sau (kể cả sau khi đổi nhân vật).
        public void PrepareForQueue()
        {
            skeleton.Initialize(false);
            physicsOff = true;
            DisablePhysics();
        }

        // EN: Shows the spine instead of the host sprite (or the sprite again), copying the host's sorting so layering stays the same.
        // VI: Hiện spine thay cho sprite gốc (hoặc hiện lại sprite), chép thứ tự vẽ của sprite gốc để lớp hiển thị không đổi.
        public void Show(bool visible)
        {
            gameObject.SetActive(visible);
            host.enabled = !visible;
            if (!visible)
                return;
            meshRenderer.sortingLayerID = host.sortingLayerID;
            meshRenderer.sortingOrder = host.sortingOrder;
        }

        // EN: Switches this body to another customer character (rebuilding the skeleton only when the character changes); the next Play starts fresh and queue bodies keep physics off.
        // VI: Đổi thân spine này sang nhân vật khách khác (chỉ dựng lại skeleton khi nhân vật đổi); lần Play kế tiếp phát lại từ đầu và thân ở hàng chờ vẫn tắt physics.
        public void SetSkeletonData(SkeletonDataAsset data)
        {
            if (skeleton.skeletonDataAsset == data)
                return;
            skeleton.skeletonDataAsset = data;
            skeleton.Initialize(true);
            currentAnimation = null;
            if (physicsOff)
                DisablePhysics();
        }

        // EN: Returns the played entry, optionally starting a new loop at a random phase; existing matching loops keep their timing.
        // VI: Trả track đang phát, có thể bắt đầu vòng lặp mới ở thời điểm ngẫu nhiên; vòng lặp trùng tên đang chạy giữ nguyên thời gian.
        public Spine.TrackEntry Play(string animationName, bool loop, bool randomStart = false)
        {
            if (loop && currentAnimation == animationName)
                return skeleton.AnimationState.GetCurrent(0);
            currentAnimation = animationName;
            Spine.TrackEntry entry = skeleton.AnimationState.SetAnimation(0, animationName, loop);
            if (loop && randomStart)
                entry.TrackTime = Random.Range(0f, entry.Animation.Duration);
            return entry;
        }

        // EN: Tints the whole skeleton (e.g. the darker look of waiting customers).
        // VI: Phủ màu toàn bộ skeleton (vd. màu tối của khách đang chờ).
        public void SetTint(Color color)
        {
            skeleton.Skeleton.SetColor(color);
        }

        // EN: Turns off every physics constraint of the current skeleton.
        // VI: Tắt mọi ràng buộc physics của skeleton hiện tại.
        private void DisablePhysics()
        {
            foreach (Spine.PhysicsConstraint constraint in skeleton.Skeleton.PhysicsConstraints)
                constraint.Mix = 0f;
        }
    }
}
