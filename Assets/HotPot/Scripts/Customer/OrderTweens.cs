using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Tiny keyed tween runner for the order card (no external library): starting a tween on a key cancels the previous one on that key, so a new tap can reverse an animation from where it is.
    // VI: Bộ chạy tween nhỏ theo khoá cho thẻ order (không dùng thư viện ngoài): chạy tween mới trên một khoá sẽ huỷ tween cũ của khoá đó, nên chạm mới có thể đảo chiều hoạt cảnh ngay tại chỗ.
    public sealed class OrderTweens
    {
        private readonly MonoBehaviour host;
        private readonly Dictionary<UnityEngine.Object, Coroutine> running = new Dictionary<UnityEngine.Object, Coroutine>();

        public OrderTweens(MonoBehaviour host)
        {
            this.host = host;
        }

        // EN: Runs step(t) with t from 0 to 1 over duration after delay, then done; runs instantly when duration <= 0 or the host is inactive.
        // VI: Chạy step(t) với t từ 0 tới 1 trong duration sau delay, rồi gọi done; chạy tức thì khi duration <= 0 hoặc host không hoạt động.
        public void Run(UnityEngine.Object key, float duration, Action<float> step, Action done = null, float delay = 0f)
        {
            Stop(key);
            if (duration <= 0f || host == null || !host.isActiveAndEnabled)
            {
                step?.Invoke(1f);
                done?.Invoke();
                return;
            }
            running[key] = host.StartCoroutine(Routine(key, duration, delay, step, done));
        }

        // EN: Cancels the tween on a key (its last applied value stays).
        // VI: Huỷ tween của một khoá (giữ giá trị đã áp dụng gần nhất).
        public void Stop(UnityEngine.Object key)
        {
            if (key == null || !running.TryGetValue(key, out Coroutine routine))
                return;
            if (routine != null && host != null)
                host.StopCoroutine(routine);
            running.Remove(key);
        }

        public bool IsRunning(UnityEngine.Object key) => key != null && running.ContainsKey(key);

        private IEnumerator Routine(UnityEngine.Object key, float duration, float delay, Action<float> step, Action done)
        {
            if (delay > 0f)
            {
                step?.Invoke(0f);
                yield return new WaitForSeconds(delay);
            }
            float time = 0f;
            while (time < duration)
            {
                time += Time.deltaTime;
                step?.Invoke(Mathf.Clamp01(time / duration));
                yield return null;
            }
            running.Remove(key);
            done?.Invoke();
        }

        // EN: Ease-out cubic.
        // VI: Easing ra mượt (cubic).
        public static float EaseOut(float t)
        {
            t = 1f - Mathf.Clamp01(t);
            return 1f - t * t * t;
        }

        // EN: Ease-out with a small overshoot, for pops.
        // VI: Easing ra có vượt nhẹ, dùng cho hiệu ứng nảy.
        public static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            t = Mathf.Clamp01(t) - 1f;
            return 1f + c3 * t * t * t + c1 * t * t;
        }
    }
}
