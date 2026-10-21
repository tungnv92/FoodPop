using System.Collections;
using UnityEngine;

namespace FoodPop
{
    [DisallowMultipleComponent]
    public class DishSelectionOutline : MonoBehaviour
    {
        [Tooltip("Renderer đĩa cần hiện viền chọn.")]
        [SerializeField] private SpriteRenderer plate;
        [Tooltip("Renderer viền trắng theo hình dáng đĩa.")]
        [SerializeField] private SpriteRenderer outline;
        [Tooltip("Renderer nắp che nguyên liệu (con của đĩa), tối theo đĩa.")]
        [SerializeField] private SpriteRenderer lid;
        [Tooltip("Renderer nguyên liệu trên đĩa.")]
        [SerializeField] private SpriteRenderer ingredient;
        [Tooltip("Độ sáng của đĩa và nguyên liệu chưa đến lượt chọn.")]
        [Range(0f, 1f)] [SerializeField] private float inactiveBrightness = 0.75f;
        [SerializeField] private Color plateColor = Color.white;
        [SerializeField] private Color ingredientColor = Color.white;
        [SerializeField] private bool selectable;

        [Header("Viền đỏ khi băng chuyền hết chỗ")]
        [Tooltip("Màu viền khi nháy báo băng chuyền không còn slot trống.")]
        [SerializeField] private Color rejectColor = new Color(1f, 0.15f, 0.12f, 1f);
        [Tooltip("Số lần viền nháy đỏ.")]
        [Min(1)] [SerializeField] private int rejectFlashCount = 2;
        [Tooltip("Thời gian (giây) của mỗi lần nháy.")]
        [Min(0.02f)] [SerializeField] private float rejectFlashDuration = 0.16f;
        [Tooltip("Độ phồng của viền ở đỉnh mỗi lần nháy (1.1 = to hơn 10%).")]
        [Min(1f)] [SerializeField] private float rejectPulseScale = 1.1f;

        private Coroutine rejectRoutine;
        private Vector3 outlineRestScale;
        private bool outlineRestCaptured;

        // EN: Restores the current outline when a pooled dish becomes active.
        // VI: Khôi phục viền hiện tại khi đĩa từ pool được bật.
        private void OnEnable()
        {
            SetSelectable(selectable);
        }

        // EN: Clears selection immediately when the dish becomes inactive.
        // VI: Xóa trạng thái chọn ngay khi đĩa bị tắt.
        private void OnDisable()
        {
            StopReject();
            SetSelectable(false);
        }

        // EN: Clears board selection when a dish moves to the pool or conveyor.
        // VI: Xóa trạng thái chọn trên board khi đĩa chuyển vào pool hoặc lên băng chuyền.
        private void OnTransformParentChanged()
        {
            SetSelectable(false);
        }

        // EN: Flashes the outline red with a little pulse a few times (the conveyor has no free slot for this dish), then restores the normal outline.
        // VI: Cho viền nháy đỏ kèm nhịp phồng nhẹ vài lần (băng chuyền không còn slot trống cho đĩa này), rồi trả lại viền bình thường.
        public void PlayRejected()
        {
            if (outline == null || !isActiveAndEnabled)
                return;
            StopReject();
            rejectRoutine = StartCoroutine(RejectFlash());
        }

        // EN: Red flash coroutine: each flash pulses the outline scale up and back while its colour goes red and back to white.
        // VI: Coroutine nháy đỏ: mỗi lần nháy, viền phồng lên rồi thu lại trong khi màu chuyển sang đỏ rồi về trắng.
        private IEnumerator RejectFlash()
        {
            if (!outlineRestCaptured)
            {
                outlineRestScale = outline.transform.localScale;
                outlineRestCaptured = true;
            }
            outline.enabled = true;
            int flashes = Mathf.Max(1, rejectFlashCount);
            float each = Mathf.Max(0.02f, rejectFlashDuration);
            for (int f = 0; f < flashes; f++)
            {
                for (float time = 0f; time < each; time += Time.deltaTime)
                {
                    float wave = Mathf.Sin(Mathf.Clamp01(time / each) * Mathf.PI);
                    outline.color = Color.Lerp(Color.white, rejectColor, wave);
                    outline.transform.localScale = outlineRestScale * (1f + (rejectPulseScale - 1f) * wave);
                    yield return null;
                }
            }
            rejectRoutine = null;
            RestoreOutline();
        }

        // EN: Cancels a running red flash and restores the outline.
        // VI: Hủy hiệu ứng nháy đỏ đang chạy và trả lại viền.
        private void StopReject()
        {
            if (rejectRoutine != null)
            {
                StopCoroutine(rejectRoutine);
                rejectRoutine = null;
            }
            RestoreOutline();
        }

        // EN: Puts the outline back to its resting scale and the current selectable state.
        // VI: Đưa viền về scale nghỉ và trạng thái chọn hiện tại.
        private void RestoreOutline()
        {
            if (outline == null)
                return;
            if (outlineRestCaptured)
                outline.transform.localScale = outlineRestScale;
            outline.color = Color.white;
            outline.enabled = selectable && isActiveAndEnabled && plate != null && plate.enabled;
        }

        // EN: Highlights column heads (outline drawn just below the plate) and dims waiting plates, food and lids.
        // VI: Hiện viền đầu cột (viền vẽ ngay dưới lớp đĩa) và làm tối đĩa, đồ ăn cùng nắp đang chờ.
        public void SetSelectable(bool value)
        {
            selectable = value;
            ApplyBrightness(value);
            if (outline == null || plate == null)
                return;
            outline.enabled = value && isActiveAndEnabled && plate.enabled;
            outline.color = Color.white;
            outline.sortingLayerID = plate.sortingLayerID;
            outline.sortingOrder = plate.sortingOrder - 1;
            outline.gameObject.layer = plate.gameObject.layer;
        }

        // EN: Hides the selection outline until the next SetSelectable call (used while a booster animates the board).
        // VI: Ẩn viền chọn tới lần gọi SetSelectable kế tiếp (dùng khi booster đang chạy hiệu ứng trên board).
        public void HideOutline()
        {
            if (outline != null)
                outline.enabled = false;
        }

        // EN: Shows the plate, lid and food at full brightness (true) or dimmed (false) without touching the outline.
        // VI: Hiện đĩa, nắp và đồ ăn ở độ sáng tối đa (true) hoặc làm tối (false) mà không đụng tới viền.
        public void ApplyBrightness(bool full)
        {
            float brightness = full ? 1f : inactiveBrightness;
            if (plate != null)
            {
                plate.color = new Color(plateColor.r * brightness, plateColor.g * brightness, plateColor.b * brightness, plateColor.a);
                if (lid != null)
                    lid.color = plate.color;
            }
            if (ingredient != null)
                ingredient.color = new Color(ingredientColor.r * brightness, ingredientColor.g * brightness, ingredientColor.b * brightness, ingredientColor.a);
        }
    }
}
