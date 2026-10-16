using UnityEngine;

namespace FoodPop
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class IngredientShadow : MonoBehaviour
    {
        [Tooltip("Renderer nguyên liệu cần tạo bóng.")]
        [SerializeField] private SpriteRenderer ingredient;
        [Tooltip("Renderer bóng dùng cùng sprite với nguyên liệu.")]
        [SerializeField] private SpriteRenderer shadow;
        [Tooltip("Độ đậm của bóng trên mặt đĩa.")]
        [Range(0f, 1f)] [SerializeField] private float opacity = 0.4f;
        [Tooltip("Độ lệch bóng so với nguyên liệu trong tọa độ cục bộ.")]
        [SerializeField] private Vector2 offset = new Vector2(0.08f, -0.08f);

        // EN: Resolves the ingredient and its prebuilt shadow when the object becomes active.
        // VI: Tìm nguyên liệu và bóng có sẵn khi đối tượng được bật.
        private void OnEnable()
        {
            ingredient = GetComponent<SpriteRenderer>();
            Transform child = transform.Find("IngredientShadow");
            if (shadow == null && child != null)
                shadow = child.GetComponent<SpriteRenderer>();
            RefreshShadow();
        }

        // EN: Follows sprite, visibility and sorting changes, including Raw/Prepared transitions.
        // VI: Theo thay đổi sprite, hiển thị và thứ tự vẽ, gồm chuyển trạng thái thô/sơ chế.
        private void LateUpdate()
        {
            RefreshShadow();
        }

        // EN: Copies the food sprite into an offset translucent silhouette above the plate and below the food.
        // VI: Dùng cùng sprite đồ ăn tạo bóng lệch bán trong suốt trên đĩa và dưới nguyên liệu.
        public void RefreshShadow()
        {
            if (ingredient == null || shadow == null)
                return;
            shadow.sprite = ingredient.sprite;
            shadow.enabled = ingredient.enabled && ingredient.sprite != null;
            if (!shadow.enabled || shadow.sprite == null)
                return;

            SpriteRenderer plate = transform.parent != null
                ? transform.parent.Find("Plate")?.GetComponent<SpriteRenderer>() : null;
            if (plate != null && plate.sortingLayerID == ingredient.sortingLayerID)
                ingredient.sortingOrder = Mathf.Max(ingredient.sortingOrder, plate.sortingOrder + 2);
            shadow.sortingLayerID = ingredient.sortingLayerID;
            shadow.sortingOrder = ingredient.sortingOrder - 1;
            shadow.gameObject.layer = gameObject.layer;
            shadow.color = new Color(0f, 0f, 0f, opacity * ingredient.color.a);
            shadow.flipX = ingredient.flipX;
            shadow.flipY = ingredient.flipY;
            shadow.drawMode = ingredient.drawMode;
            shadow.size = ingredient.size;
            shadow.transform.localPosition = new Vector3(offset.x, offset.y, 0.001f);
            shadow.transform.localRotation = Quaternion.identity;
            shadow.transform.localScale = Vector3.one;
        }
    }
}
