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
        [Tooltip("Renderer nguyên liệu trên đĩa.")]
        [SerializeField] private SpriteRenderer ingredient;
        [Tooltip("Độ sáng của đĩa và nguyên liệu chưa đến lượt chọn.")]
        [Range(0f, 1f)] [SerializeField] private float inactiveBrightness = 0.75f;
        [SerializeField] private Color plateColor = Color.white;
        [SerializeField] private Color ingredientColor = Color.white;
        [SerializeField] private bool selectable;

        // EN: Indicates whether this dish is currently the selectable head of its column.
        // VI: Cho biết đĩa này có đang là phần tử đầu cột được chọn hay không.
        public bool IsSelectable => selectable;

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
            SetSelectable(false);
        }

        // EN: Clears board selection when a dish moves to the pool or conveyor.
        // VI: Xóa trạng thái chọn trên board khi đĩa chuyển vào pool hoặc lên băng chuyền.
        private void OnTransformParentChanged()
        {
            SetSelectable(false);
        }

        // EN: Highlights column heads (outline drawn just below the plate) and dims waiting plates, food and lids; conveyor food keeps normal brightness.
        // VI: Hiện viền đầu cột (viền vẽ ngay dưới lớp đĩa) và làm tối đĩa, đồ ăn cùng nắp đang chờ; đồ ăn trên băng chuyền giữ độ sáng bình thường.
        public void SetSelectable(bool value)
        {
            selectable = value;
            bool onConveyor = transform.parent != null && transform.parent.GetComponent<ConveyorSlot>() != null;
            float brightness = value || onConveyor ? 1f : inactiveBrightness;
            if (plate != null)
            {
                plate.color = new Color(plateColor.r * brightness, plateColor.g * brightness, plateColor.b * brightness, plateColor.a);
                SpriteRenderer lid = plate.transform.Find("Lid")?.GetComponent<SpriteRenderer>();
                if (lid != null)
                    lid.color = plate.color;
            }
            if (ingredient != null)
                ingredient.color = new Color(ingredientColor.r * brightness, ingredientColor.g * brightness, ingredientColor.b * brightness, ingredientColor.a);
            if (outline == null || plate == null)
                return;
            outline.enabled = value && isActiveAndEnabled && plate.enabled;
            outline.color = Color.white;
            outline.sortingLayerID = plate.sortingLayerID;
            outline.sortingOrder = plate.sortingOrder - 1;
            outline.gameObject.layer = plate.gameObject.layer;
        }
    }
}
