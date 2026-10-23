using System;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: How the ingredient layout appears when a dish is tapped: Pop = icons pop in one by one; Rows = list style, rows slide up and fade in one after another.
    // VI: Cách layout nguyên liệu xuất hiện khi chạm món: Pop = nguyên liệu nảy ra lần lượt; Rows = kiểu danh sách, từng hàng trượt lên và hiện dần.
    public enum IngredientOpenStyle
    {
        Pop = 0,
        Rows = 1
    }

    // EN: Designed ingredient layout (an "OrderPreview_x_y" prefab) shown when a dish is tapped: the designer places the dark panel ("Card") and one "Ingredient" renderer per ingredient by hand; at runtime the layout loads this prefab, fills the slots with the raw ingredient icons (arrived ones darkened) and plays its own open/close transition.
    // VI: Layout nguyên liệu thiết kế sẵn (prefab "OrderPreview_x_y") hiện ra khi chạm món: người thiết kế tự đặt nền tối ("Card") và mỗi nguyên liệu một renderer "Ingredient"; khi chạy, bố cục nạp prefab này, gán icon nguyên liệu raw vào các ô (nguyên liệu đã tới bị tối) và chạy hiệu ứng mở/đóng riêng của nó.
    [DisallowMultipleComponent]
    public class IngredientPreviewView : MonoBehaviour
    {
        [Header("Thành phần")]
        [Tooltip("Nền của layout nguyên liệu (object \"Card\").")]
        [SerializeField] private SpriteRenderer background;
        [Tooltip("Các ô nguyên liệu. Thứ tự gán nguyên liệu (đế trước) theo VỊ TRÍ: trên xuống dưới, rồi trái sang phải; thứ tự trong danh sách không quan trọng.")]
        [SerializeField] private List<SpriteRenderer> slots = new List<SpriteRenderer>();
        [Tooltip("Độ sáng của nguyên liệu đã tới (0 = đen, 1 = gốc).")]
        [Range(0f, 1f)] [SerializeField] private float doneBrightness = 0.35f;

        [Header("Hiệu ứng chuyển")]
        [Tooltip("Kiểu mở: Rows = từng hàng trượt lên và hiện dần (kiểu danh sách); Pop = nguyên liệu nảy ra lần lượt.")]
        [SerializeField] private IngredientOpenStyle openStyle = IngredientOpenStyle.Rows;
        [Tooltip("Thời gian mở (giây).")]
        [Min(0f)] [SerializeField] private float openDuration = 0.22f;
        [Tooltip("Thời gian đóng (giây).")]
        [Min(0f)] [SerializeField] private float closeDuration = 0.16f;
        [Tooltip("Rows: độ trễ giữa hai hàng (giây). Pop: độ trễ giữa hai nguyên liệu.")]
        [Min(0f)] [SerializeField] private float stepDelay = 0.08f;
        [Tooltip("Rows: quãng trượt của mỗi hàng (local của layout).")]
        [Min(0f)] [SerializeField] private float rowSlideDistance = 0.15f;
        [Tooltip("Thời gian nháy của nguyên liệu vừa tới khi layout đang mở (giây).")]
        [Min(0f)] [SerializeField] private float arrivalFlashDuration = 0.3f;
        [Tooltip("Khoảng y coi như cùng một hàng khi chia hàng cho hiệu ứng.")]
        [Min(0f)] [SerializeField] private float rowTolerance = 0.05f;

        private bool prepared;
        private SpriteRenderer[] ordered = new SpriteRenderer[0];
        private Vector3[] basePositions = new Vector3[0];
        private Vector3[] baseScales = new Vector3[0];
        private int[] rowOf = new int[0];
        private int rowCount;
        private Color backgroundColor = Color.white;
        private bool[] doneStates = new bool[0];
        private int shownCount;
        private bool closing;
        private OrderTweens tweens;
        private OrderTweens Tweens => tweens ?? (tweens = new OrderTweens(this));

        // EN: Seconds of the open / close transitions (0 when disabled by the card).
        // VI: Thời gian hiệu ứng mở / đóng.
        public float OpenDuration => openDuration;
        public float CloseDuration => closeDuration;

        // EN: Number of ingredient slots of this layout.
        // VI: Số ô nguyên liệu của layout.
        public int SlotCount
        {
            get
            {
                Prepare();
                return ordered.Length;
            }
        }

        // EN: True while the close transition is running (a new tap reopens from there).
        // VI: True khi hiệu ứng đóng đang chạy (chạm mới sẽ mở lại từ đó).
        public bool IsClosing => closing;

        // EN: Fills the slots with the icons (recipe order, base first) and their arrival state, draws above the dish slot (panel +1, icons +2) and, when opening, plays the open transition; when already open, newly arrived ingredients flash.
        // VI: Gán icon vào các ô (thứ tự công thức, đế trước) cùng trạng thái đã tới, vẽ trên ô món (nền +1, icon +2) và khi mở thì chạy hiệu ứng mở; khi đang mở sẵn thì nguyên liệu vừa tới sẽ nháy.
        public void Show(IReadOnlyList<Sprite> icons, IReadOnlyList<bool> done, SpriteRenderer sortingReference, bool opening, bool animate)
        {
            Prepare();
            closing = false;
            gameObject.SetActive(true);
            ApplySorting(sortingReference);
            if (icons.Count > ordered.Length)
                Debug.LogWarning($"[IngredientPreviewView] {name} chỉ có {ordered.Length} ô cho {icons.Count} nguyên liệu; các nguyên liệu cuối bị bỏ.", this);
            shownCount = Mathf.Min(icons.Count, ordered.Length);
            if (doneStates.Length != ordered.Length)
                doneStates = new bool[ordered.Length];
            for (int i = 0; i < ordered.Length; i++)
            {
                SpriteRenderer slot = ordered[i];
                bool used = i < shownCount;
                slot.gameObject.SetActive(used);
                if (!used)
                    continue;
                slot.sprite = icons[i];
                bool isDone = done != null && i < done.Count && done[i];
                bool flash = !opening && isDone && !doneStates[i];
                doneStates[i] = isDone;
                if (opening)
                {
                    Tweens.Stop(slot);
                    slot.transform.localPosition = basePositions[i];
                    slot.transform.localScale = baseScales[i];
                    slot.color = Tint(isDone, 1f);
                }
                else if (flash)
                    FlashArrived(i);
                else if (!Tweens.IsRunning(slot))
                    slot.color = Tint(isDone, slot.color.a);
            }
            if (opening)
            {
                if (background != null)
                    background.color = backgroundColor;
                PlayOpen(animate ? openDuration : 0f);
            }
        }

        // EN: Closes the layout (instantly or with the close transition) and switches it off at the end.
        // VI: Đóng layout (tức thì hoặc có hiệu ứng) và tắt object khi xong.
        public void Hide(bool instant)
        {
            Prepare();
            if (!gameObject.activeSelf)
                return;
            float duration = instant ? 0f : closeDuration;
            closing = true;
            if (openStyle == IngredientOpenStyle.Rows)
                PlayCloseRows(duration);
            else
                PlayClosePop(duration);
        }

        // EN: Area of the layout in its own local space: the panel ("Card") rectangle, or the box around the slots when there is no panel.
        // VI: Vùng của layout trong không gian local của nó: hình chữ nhật của nền ("Card"), hoặc khung bao các ô khi không có nền.
        public Rect LocalRect
        {
            get
            {
                Prepare();
                if (background != null)
                {
                    Transform t = background.transform;
                    Vector2 size = background.drawMode != SpriteDrawMode.Simple
                        ? background.size
                        : background.sprite != null ? (Vector2)background.sprite.bounds.size : Vector2.one;
                    size = Vector2.Scale(size, new Vector2(Mathf.Abs(t.localScale.x), Mathf.Abs(t.localScale.y)));
                    Vector2 centre = t.localPosition;
                    return new Rect(centre - size * 0.5f, size);
                }
                if (ordered.Length == 0)
                    return new Rect(Vector2.zero, Vector2.zero);
                Vector2 min = basePositions[0], max = basePositions[0];
                foreach (Vector3 p in basePositions)
                {
                    min = Vector2.Min(min, p);
                    max = Vector2.Max(max, p);
                }
                return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }
        }

        // EN: Collects the slots ("Ingredient…" renderers) and the panel ("Card") when not assigned; used by the editor setup and on Reset.
        // VI: Gom các ô ("Ingredient…") và nền ("Card") khi chưa gán; dùng cho setup trong editor và khi Reset.
        public void Collect()
        {
            if (background == null)
            {
                Transform card = transform.Find("Card");
                if (card != null)
                    background = card.GetComponent<SpriteRenderer>();
            }
            slots.RemoveAll(s => s == null);
            if (slots.Count == 0)
                foreach (Transform child in transform)
                    if (child.name.StartsWith("Ingredient") && child.TryGetComponent(out SpriteRenderer renderer))
                        slots.Add(renderer);
            prepared = false;
        }

        private void Reset()
        {
            Collect();
        }

        // EN: Orders the slots by position (top to bottom, then left to right), remembers their designed position/scale and groups them into rows.
        // VI: Sắp các ô theo vị trí (trên xuống, rồi trái sang phải), ghi nhớ vị trí/scale thiết kế và chia hàng.
        private void Prepare()
        {
            if (prepared)
                return;
            prepared = true;
            if (slots.Count == 0 || background == null)
                Collect();
            List<SpriteRenderer> list = slots.FindAll(s => s != null);
            float tolerance = rowTolerance;
            list.Sort((a, b) =>
            {
                Vector3 pa = a.transform.localPosition, pb = b.transform.localPosition;
                if (Mathf.Abs(pa.y - pb.y) > tolerance)
                    return pb.y.CompareTo(pa.y);
                return pa.x.CompareTo(pb.x);
            });
            ordered = list.ToArray();
            basePositions = new Vector3[ordered.Length];
            baseScales = new Vector3[ordered.Length];
            rowOf = new int[ordered.Length];
            rowCount = 0;
            float lastY = float.NaN;
            for (int i = 0; i < ordered.Length; i++)
            {
                basePositions[i] = ordered[i].transform.localPosition;
                baseScales[i] = ordered[i].transform.localScale;
                if (i == 0 || Mathf.Abs(basePositions[i].y - lastY) > tolerance)
                {
                    rowCount++;
                    lastY = basePositions[i].y;
                }
                rowOf[i] = rowCount - 1;
            }
            if (background != null)
                backgroundColor = background.color;
            doneStates = new bool[ordered.Length];
        }

        private void ApplySorting(SpriteRenderer reference)
        {
            if (reference == null)
                return;
            if (background != null)
            {
                background.sortingLayerID = reference.sortingLayerID;
                background.sortingOrder = reference.sortingOrder + 1;
            }
            foreach (SpriteRenderer slot in ordered)
            {
                slot.sortingLayerID = reference.sortingLayerID;
                slot.sortingOrder = reference.sortingOrder + 2;
            }
        }

        private Color Tint(bool isDone, float alpha)
        {
            float b = isDone ? doneBrightness : 1f;
            return new Color(b, b, b, alpha);
        }

        private void PlayOpen(float duration)
        {
            Transform root = transform;
            int rows = Mathf.Max(1, rowCount);
            if (openStyle == IngredientOpenStyle.Rows)
            {
                float lead = duration * 0.4f;
                float fade = Mathf.Max(0.0001f, duration * 0.6f);
                float total = duration <= 0f ? 0f : lead + duration + stepDelay * (rows - 1);
                Action<float> step = t =>
                {
                    float time = t * total;
                    for (int i = 0; i < shownCount; i++)
                    {
                        float k = duration > 0f ? Mathf.Clamp01((time - lead - rowOf[i] * stepDelay) / duration) : 1f;
                        float e = OrderTweens.EaseOut(k);
                        ordered[i].transform.localPosition = basePositions[i] - new Vector3(0f, rowSlideDistance * (1f - e), 0f);
                        ordered[i].color = Tint(doneStates[i], e);
                    }
                    if (background != null)
                        background.color = WithAlpha(backgroundColor, backgroundColor.a * OrderTweens.EaseOut(duration > 0f ? Mathf.Clamp01(time / fade) : 1f));
                };
                if (total > 0f && isActiveAndEnabled)
                    step(0f);
                Tweens.Run(root, total, step);
            }
            else
            {
                float total = duration <= 0f ? 0f : duration + stepDelay * Mathf.Max(0, shownCount - 1);
                Action<float> step = t =>
                {
                    float time = t * total;
                    for (int i = 0; i < shownCount; i++)
                    {
                        float k = duration > 0f ? Mathf.Clamp01((time - i * stepDelay) / duration) : 1f;
                        ordered[i].transform.localScale = baseScales[i] * OrderTweens.EaseOutBack(k);
                    }
                    if (background != null)
                        background.color = WithAlpha(backgroundColor, backgroundColor.a * OrderTweens.EaseOut(duration > 0f ? Mathf.Clamp01(time / duration) : 1f));
                };
                if (total > 0f && isActiveAndEnabled)
                    step(0f);
                Tweens.Run(root, total, step);
            }
        }

        private void PlayCloseRows(float duration)
        {
            int rows = Mathf.Max(1, rowCount);
            float delay = stepDelay * 0.5f;
            Vector3[] starts = new Vector3[shownCount];
            float[] alphas = new float[shownCount];
            for (int i = 0; i < shownCount; i++)
            {
                Tweens.Stop(ordered[i]);
                starts[i] = ordered[i].transform.localPosition;
                alphas[i] = ordered[i].color.a;
            }
            float startAlpha = background != null ? background.color.a : 0f;
            float rowsTime = duration + delay * (rows - 1);
            float total = duration <= 0f ? 0f : rowsTime + duration * 0.5f;
            Tweens.Run(transform, total, t =>
            {
                float time = t * total;
                for (int i = 0; i < shownCount; i++)
                {
                    float k = duration > 0f ? Mathf.Clamp01((time - (rows - 1 - rowOf[i]) * delay) / duration) : 1f;
                    float e = OrderTweens.EaseOut(k);
                    ordered[i].transform.localPosition = starts[i] - new Vector3(0f, rowSlideDistance * e, 0f);
                    ordered[i].color = Tint(doneStates[i], alphas[i] * (1f - e));
                }
                if (background != null)
                {
                    float k = total > 0f ? Mathf.Clamp01((time - rowsTime * 0.5f) / Mathf.Max(0.0001f, total - rowsTime * 0.5f)) : 1f;
                    background.color = WithAlpha(backgroundColor, startAlpha * (1f - OrderTweens.EaseOut(k)));
                }
            }, FinishClose);
        }

        private void PlayClosePop(float duration)
        {
            Vector3[] starts = new Vector3[shownCount];
            for (int i = 0; i < shownCount; i++)
            {
                Tweens.Stop(ordered[i]);
                starts[i] = ordered[i].transform.localScale;
            }
            float startAlpha = background != null ? background.color.a : 0f;
            Tweens.Run(transform, duration, t =>
            {
                float k = 1f - OrderTweens.EaseOut(t);
                for (int i = 0; i < shownCount; i++)
                    ordered[i].transform.localScale = starts[i] * k;
                if (background != null)
                    background.color = WithAlpha(backgroundColor, startAlpha * k);
            }, FinishClose);
        }

        // EN: Switches the layout off and restores the designed slot transforms and panel colour for the next opening.
        // VI: Tắt layout và trả transform thiết kế của các ô cùng màu nền cho lần mở sau.
        private void FinishClose()
        {
            closing = false;
            for (int i = 0; i < ordered.Length; i++)
            {
                ordered[i].transform.localPosition = basePositions[i];
                ordered[i].transform.localScale = baseScales[i];
            }
            if (background != null)
                background.color = backgroundColor;
            gameObject.SetActive(false);
        }

        private void FlashArrived(int i)
        {
            SpriteRenderer slot = ordered[i];
            Vector3 scale = baseScales[i];
            float target = doneBrightness;
            Tweens.Run(slot, arrivalFlashDuration, t =>
            {
                float b = Mathf.Lerp(1f, target, OrderTweens.EaseOut(t));
                slot.color = new Color(b, b, b, 1f);
                slot.transform.localScale = scale * (1f + 0.25f * Mathf.Sin(t * Mathf.PI));
            }, () => slot.transform.localScale = scale);
        }

        private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);
    }
}
