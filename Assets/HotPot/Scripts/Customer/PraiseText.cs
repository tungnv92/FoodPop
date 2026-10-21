using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Pooled praise-word sprites ("Delicious!", "Yummy!"...) that pop out above a happy customer, float up and fade away. Created and driven by CustomerManager; the sprites and all tuning live in IngameConfig.praiseText.
    // VI: Sprite chữ khen ("Delicious!", "Yummy!"...) dùng pool, bật ra phía trên khách vui, bay lên rồi mờ dần. Do CustomerManager tạo và điều khiển; sprite và mọi thông số nằm trong IngameConfig.praiseText.
    public class PraiseText
    {
        private readonly Transform root;
        private readonly Stack<SpriteRenderer> pool = new Stack<SpriteRenderer>();
        private readonly List<SpriteRenderer> active = new List<SpriteRenderer>();

        // EN: Creates the pool holder under the given parent.
        // VI: Tạo object chứa pool dưới object cha đã cho.
        public PraiseText(Transform parent)
        {
            GameObject holder = new GameObject("PraiseTexts");
            holder.transform.SetParent(parent, false);
            root = holder.transform;
        }

        // EN: Plays one random praise sprite at the given world position with the given sorting; run it as a coroutine.
        // VI: Bắn một sprite chữ khen ngẫu nhiên tại vị trí thế giới đã cho với thứ tự vẽ đã cho; chạy như coroutine.
        public IEnumerator Play(IngameConfig.PraiseTextSettings settings, Vector3 position, int sortingLayer, int sortingOrder)
        {
            if (settings == null || settings.entries == null || settings.entries.Length == 0)
                yield break;
            IngameConfig.PraiseEntry entry = settings.entries[Random.Range(0, settings.entries.Length)];
            if (entry == null || entry.sprite == null)
                yield break;
            Sprite sprite = entry.sprite;
            SpriteRenderer renderer = Rent();
            renderer.sprite = sprite;
            renderer.sortingLayerID = sortingLayer;
            renderer.sortingOrder = sortingOrder;
            renderer.color = Color.white;
            Vector2 size = sprite.bounds.size;
            float restScale = Mathf.Min(settings.height / Mathf.Max(0.0001f, size.y), settings.maxWidth / Mathf.Max(0.0001f, size.x));

            Transform t = renderer.transform;
            Vector3 start = position + (Vector3)settings.offset;
            Vector3 end = start + Vector3.up * settings.riseHeight;
            t.SetPositionAndRotation(start, Quaternion.Euler(0f, 0f, Random.Range(-settings.maxTilt, settings.maxTilt)));
            t.localScale = Vector3.zero;
            renderer.gameObject.SetActive(true);

            float duration = Mathf.Max(0.05f, settings.duration);
            for (float time = 0f; time < duration; )
            {
                time += Time.deltaTime;
                float p = Mathf.Clamp01(time / duration);
                float popPart = Mathf.Clamp01(p / 0.25f);
                float pop = popPart < 0.6f
                    ? Mathf.Lerp(0f, settings.popScale, popPart / 0.6f)
                    : Mathf.Lerp(settings.popScale, 1f, (popPart - 0.6f) / 0.4f);
                t.localScale = new Vector3(restScale * pop, restScale * pop, 1f);
                t.position = Vector3.LerpUnclamped(start, end, 1f - (1f - p) * (1f - p));
                renderer.color = new Color(1f, 1f, 1f, p < 0.65f ? 1f : 1f - (p - 0.65f) / 0.35f);
                yield return null;
            }
            Return(renderer);
        }

        // EN: Hides every praise sprite that is still showing (used when the level reloads).
        // VI: Ẩn mọi sprite chữ khen còn đang hiện (dùng khi nạp lại level).
        public void HideAll()
        {
            for (int i = active.Count - 1; i >= 0; i--)
                Return(active[i]);
        }

        // EN: Takes a renderer from the pool or creates a new one.
        // VI: Lấy một renderer từ pool hoặc tạo mới.
        private SpriteRenderer Rent()
        {
            SpriteRenderer renderer;
            if (pool.Count > 0)
                renderer = pool.Pop();
            else
            {
                GameObject go = new GameObject("Praise");
                go.transform.SetParent(root, false);
                renderer = go.AddComponent<SpriteRenderer>();
                go.SetActive(false);
            }
            active.Add(renderer);
            return renderer;
        }

        // EN: Hides a renderer and puts it back in the pool.
        // VI: Ẩn một renderer và trả lại vào pool.
        private void Return(SpriteRenderer renderer)
        {
            if (renderer == null || !active.Remove(renderer))
                return;
            renderer.gameObject.SetActive(false);
            pool.Push(renderer);
        }
    }
}
