using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Shared helper that plays pooled one-shot particle prefabs, so gameplay scripts do not each keep their own pools (sounds live in IngameSoundManager).
    // VI: Lớp dùng chung để chạy particle một lần (có pool), giúp các script gameplay không phải tự giữ pool riêng (âm thanh nằm ở IngameSoundManager).
    public static class OneShotEffects
    {
        private static readonly Dictionary<ParticleEffect, List<ParticleEffect>> Pools = new Dictionary<ParticleEffect, List<ParticleEffect>>();
        private static Transform root;

        // EN: Clears cached pools and objects when entering Play Mode, including when domain reload is disabled.
        // VI: Xóa pool và object đã lưu khi vào Play Mode, kể cả khi tắt tải lại miền mã.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            Pools.Clear();
            root = null;
        }

        // EN: Plays a particle prefab once at a world position, reusing a finished instance when possible, and puts all its renderers on the given sorting.
        // VI: Chạy prefab particle một lần tại vị trí world, tái sử dụng bản đã chạy xong nếu có, và đặt mọi renderer theo thứ tự vẽ đã cho.
        public static void PlayParticle(ParticleEffect prefab, Vector3 position, int sortingLayer, int sortingOrder)
        {
            if (prefab == null)
                return;
            ParticleEffect effect = RentParticle(prefab);
            effect.transform.position = position;
            effect.SetSorting(sortingLayer, sortingOrder);
            effect.Play();
        }

        // EN: Returns a finished instance of the prefab from its pool, or instantiates a new one under the shared root.
        // VI: Lấy một bản đã chạy xong của prefab từ pool, hoặc tạo bản mới dưới object gốc dùng chung.
        private static ParticleEffect RentParticle(ParticleEffect prefab)
        {
            if (!Pools.TryGetValue(prefab, out List<ParticleEffect> pool))
            {
                pool = new List<ParticleEffect>();
                Pools[prefab] = pool;
            }
            pool.RemoveAll(instance => instance == null);
            foreach (ParticleEffect candidate in pool)
            {
                if (!candidate.IsAlive)
                    return candidate;
            }
            ParticleEffect created = Object.Instantiate(prefab, GetRoot());
            pool.Add(created);
            return created;
        }

        // EN: Returns the unscaled scene-root object holding pooled effects, creating it when missing (e.g. after a scene change).
        // VI: Trả về object gốc không bị scale chứa hiệu ứng trong pool, tạo mới khi chưa có (ví dụ sau khi đổi scene).
        private static Transform GetRoot()
        {
            if (root == null)
                root = new GameObject("OneShotEffects").transform;
            return root;
        }
    }
}
