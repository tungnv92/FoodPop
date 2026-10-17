using UnityEngine;

namespace FoodPop
{
    // EN: Root of a particle effect prefab: keeps direct references to its particle layers and renderers so gameplay code never searches children at runtime.
    // VI: Gốc của prefab hiệu ứng particle: giữ tham chiếu trực tiếp tới các lớp particle và renderer để code gameplay không phải tìm child lúc chạy.
    [DisallowMultipleComponent]
    public class ParticleEffect : MonoBehaviour
    {
        [Tooltip("Lớp particle chính (thường nằm trên GameObject gốc).")]
        [SerializeField] private ParticleSystem mainLayer;
        [Tooltip("Mọi lớp particle của hiệu ứng, gồm cả lớp chính.")]
        [SerializeField] private ParticleSystem[] layers = new ParticleSystem[0];
        [Tooltip("Renderer của mọi lớp particle, dùng để đặt thứ tự vẽ.")]
        [SerializeField] private ParticleSystemRenderer[] renderers = new ParticleSystemRenderer[0];

        // EN: True while any layer still has live particles.
        // VI: True khi còn bất kỳ lớp nào có particle đang sống.
        public bool IsAlive => mainLayer != null && mainLayer.IsAlive(true);

        // EN: Puts every renderer of the effect on the given sorting layer and order.
        // VI: Đặt mọi renderer của hiệu ứng vào sorting layer và thứ tự vẽ đã cho.
        public void SetSorting(int sortingLayer, int sortingOrder)
        {
            foreach (ParticleSystemRenderer particleRenderer in renderers)
            {
                if (particleRenderer == null)
                    continue;
                particleRenderer.sortingLayerID = sortingLayer;
                particleRenderer.sortingOrder = sortingOrder;
            }
        }

        // EN: Plays every layer from the start.
        // VI: Chạy mọi lớp từ đầu.
        public void Play()
        {
            if (mainLayer != null)
                mainLayer.Play(true);
        }

        // EN: Stops emitting on every layer and lets live particles finish.
        // VI: Ngừng phát ở mọi lớp và để các particle đang sống tự kết thúc.
        public void Stop()
        {
            if (mainLayer != null)
                mainLayer.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // EN: Emits mainCount particles from the main layer and extraCount from each extra layer.
        // VI: Bắn mainCount particle ở lớp chính và extraCount ở mỗi lớp phụ.
        public void Emit(int mainCount, int extraCount)
        {
            foreach (ParticleSystem layer in layers)
            {
                if (layer != null)
                    layer.Emit(layer == mainLayer ? mainCount : extraCount);
            }
        }

#if UNITY_EDITOR
        // EN: Editor-only: fills the references from this prefab's own hierarchy (used by the builder tools).
        // VI: Chỉ dùng trong editor: tự gán tham chiếu từ hierarchy của chính prefab (công cụ dựng gọi hàm này).
        public void CollectReferences()
        {
            mainLayer = GetComponent<ParticleSystem>();
            layers = GetComponentsInChildren<ParticleSystem>(true);
            renderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
            if (mainLayer == null && layers.Length > 0)
                mainLayer = layers[0];
        }

        // EN: Fills missing references when the component is added in the editor.
        // VI: Tự gán tham chiếu còn thiếu khi component được thêm trong editor.
        private void Reset()
        {
            CollectReferences();
        }
#endif
    }
}
