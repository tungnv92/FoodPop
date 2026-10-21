using UnityEngine;

namespace FoodPop
{
    // EN: Root component of the DishSlot prefab: holds direct references to the slot's own visuals, including the twinkling-star particle effect nested in the prefab, so gameplay code never searches children.
    // VI: Component gốc của prefab DishSlot: giữ tham chiếu trực tiếp tới hình ảnh của ô, gồm cả hiệu ứng particle sao lấp lánh lồng sẵn trong prefab, để code gameplay không phải tìm child.
    [DisallowMultipleComponent]
    public class DishSlotView : MonoBehaviour
    {
        [Tooltip("Hiệu ứng particle sao lấp lánh đặt sẵn trong ô (con của prefab DishSlot).")]
        [SerializeField] private ParticleEffect twinkle;

        [Tooltip("SpriteRenderer viền nét đứt của ô (child \"Slot Outline\" trong prefab DishSlot), dùng để đặt thứ tự vẽ và đo bề rộng ô.")]
        [SerializeField] private SpriteRenderer outline;

        private bool twinkling;

        // EN: Returns the slot's dashed outline renderer (may be null if not wired in the prefab).
        // VI: Trả về SpriteRenderer viền nét đứt của ô (có thể null nếu chưa gán trong prefab).
        public SpriteRenderer Outline => outline;

        // EN: True while the twinkle is emitting.
        // VI: True khi hiệu ứng lấp lánh đang phát.
        public bool IsTwinkling => twinkling;

        // EN: Starts the slot's twinkle on the given sorting (keeps the effect at world scale 1 so particle sizes do not depend on the slot/tray scale).
        // VI: Bật hiệu ứng lấp lánh của ô với thứ tự vẽ đã cho (giữ hiệu ứng ở scale thế giới 1 để cỡ particle không phụ thuộc scale của ô/khay).
        public void PlayTwinkle(int sortingLayer, int sortingOrder)
        {
            if (twinkle == null || twinkling)
                return;
            Transform effect = twinkle.transform;
            Vector3 parentScale = effect.parent != null ? effect.parent.lossyScale : Vector3.one;
            effect.localScale = new Vector3(1f / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)), 1f / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);
            twinkle.SetSorting(sortingLayer, sortingOrder);
            twinkle.Play();
            twinkling = true;
        }

        // EN: Stops emitting so the last stars fade out in place.
        // VI: Ngừng phát để các sao cuối mờ dần tại chỗ.
        public void StopTwinkle()
        {
            if (twinkle == null || !twinkling)
                return;
            twinkle.Stop();
            twinkling = false;
        }

#if UNITY_EDITOR
        // EN: Editor-only: assigns the nested twinkle effect (used by the builder tool).
        // VI: Chỉ dùng trong editor: gán hiệu ứng lấp lánh lồng sẵn (công cụ dựng gọi hàm này).
        public void SetTwinkle(ParticleEffect effect)
        {
            twinkle = effect;
        }

        // EN: Editor-only: assigns the slot outline renderer (used by the builder tool).
        // VI: Chỉ dùng trong editor: gán SpriteRenderer viền ô (công cụ dựng gọi hàm này).
        public void SetOutline(SpriteRenderer renderer)
        {
            outline = renderer;
        }
#endif
    }
}
