using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Draws a straight cord and rim eyelets for each consecutive pair in an A–B–C chain, cloned from the prefab's templates.
    // VI: Vẽ dây thẳng và móc mép đĩa cho từng cặp liên tiếp trong chuỗi A–B–C, nhân bản từ mẫu có sẵn trong prefab.
    public sealed class DishLinkView : MonoBehaviour
    {
        [Tooltip("Dây mẫu (LineRenderer) trong prefab; chỉnh vật liệu, texture và kiểu đầu dây tại đây. Mẫu được tắt và nhân bản cho từng đoạn A–B, B–C.")]
        [SerializeField] private LineRenderer cordTemplate;
        [Tooltip("Móc mẫu (SpriteRenderer) ở mép đĩa; đổi sprite tại đây. Mẫu được tắt và nhân bản cho hai đầu mỗi đoạn dây.")]
        [SerializeField] private SpriteRenderer eyeletTemplate;
        [Tooltip("Màu nhân lên dây và móc khi chưa có đĩa nào của nhóm ở hàng đầu.")]
        [SerializeField] private Color waitingTint = new Color(0.58f, 0.58f, 0.58f, 1f);
        [Tooltip("Màu nhân lên dây và móc khi có đĩa của nhóm ở hàng đầu.")]
        [SerializeField] private Color highlightTint = Color.white;
        [Tooltip("Độ dày dây so với cạnh nhỏ nhất của đĩa.")]
        [Min(0f)][SerializeField] private float cordWidthRatio = 0.095f;
        [Tooltip("Đường kính móc so với cạnh nhỏ nhất của đĩa.")]
        [Min(0f)][SerializeField] private float eyeletSizeRatio = 0.19f;
        [Tooltip("Thứ tự vẽ của dây và móc so với mặt đĩa (Plate) cao nhất trong nhóm. 4 = trên nắp (đĩa + 3) và nguyên liệu thường, dưới nguyên liệu được nâng, số đếm (đĩa + 7) và băng (đĩa + 8).")]
        [SerializeField] private int cordPlateOffset = 4;
        [Tooltip("Thứ tự vẽ nguyên liệu bình thường so với mặt đĩa; nguyên liệu trở về mức này khi không còn ở hàng sau của đoạn dây.")]
        [SerializeField] private int ingredientPlateOffset = 2;
        [Tooltip("Thứ tự vẽ nguyên liệu của đĩa ở hàng sau (hàng sâu hơn) mỗi đoạn dây so với mặt đĩa; nắp của đĩa đó nằm ngay trên (+1), nên dây chui dưới cả món lẫn nắp đang đóng. Hai đĩa cùng hàng đều được nâng.")]
        [SerializeField] private int raisedIngredientPlateOffset = 6;
        [Tooltip("Độ lệch Z của móc về phía camera để móc luôn vẽ đè lên đầu dây khi cùng thứ tự vẽ.")]
        [SerializeField] private float eyeletDepthOffset = -0.01f;

        private readonly List<LineRenderer> cords = new List<LineRenderer>();
        private readonly List<SpriteRenderer> eyelets = new List<SpriteRenderer>();

        // EN: Hides the templates; segments are cloned lazily by Initialize so a pooled view can be reused for any chain length.
        // VI: Ẩn mẫu; đoạn dây được nhân bản dần trong Initialize để view trong pool dùng lại được cho mọi độ dài chuỗi.
        private void Awake()
        {
            cordTemplate.gameObject.SetActive(false);
            eyeletTemplate.gameObject.SetActive(false);
        }

        // EN: Prepares the view for a chain of count dishes, cloning missing cord/eyelet segments and keeping extra pooled ones for later chains.
        // VI: Chuẩn bị view cho chuỗi count đĩa, nhân bản thêm dây/móc còn thiếu và giữ đoạn dư trong view cho chuỗi sau.
        public void Initialize(int count)
        {
            for (int i = cords.Count; i < count - 1; i++)
            {
                cords.Add(CloneTemplate(cordTemplate, $"Cord {i + 1}"));
                eyelets.Add(CloneTemplate(eyeletTemplate, "Rim eyelet"));
                eyelets.Add(CloneTemplate(eyeletTemplate, "Rim eyelet"));
            }
        }

        // EN: Follows fixed rim anchors, tints every segment when any member is at the front and layers the cord above lids but under the food of each segment's back-row dish; rows are board depths per member.
        // VI: Bám các điểm gắn cố định trên viền khay, đổi màu mọi đoạn khi có thành viên ở đầu hàng và xếp dây trên nắp nhưng dưới món của đĩa hàng sau mỗi đoạn; rows là độ sâu hàng trên board của từng thành viên.
        public void Refresh(IReadOnlyList<Food> members, IReadOnlyList<int> rows, bool highlighted)
        {
            UpdateIngredientLayers(members, rows);
            UpdateCords(members, highlighted);
        }

        // EN: Returns every member's food to its normal layer when the chain dissolves or is cleared.
        // VI: Trả món của mọi thành viên về lớp vẽ thường khi chuỗi giải thể hoặc bị xóa.
        public void ResetIngredientLayers(IReadOnlyList<Food> members)
        {
            foreach (Food dish in members)
                dish.SetIngredientLayer(ingredientPlateOffset);
        }

        // EN: Instantiates an active copy of a hidden template under this view.
        // VI: Tạo bản sao đang bật của một mẫu đã ẩn, đặt dưới hình nối này.
        private T CloneTemplate<T>(T template, string name) where T : Renderer
        {
            T copy = Instantiate(template, transform);
            copy.name = name;
            copy.gameObject.SetActive(true);
            return copy;
        }

        // EN: Raises the food and lid of the deeper dish in each segment (both when they share a row) and keeps the others at the normal layer.
        // VI: Nâng món và nắp của đĩa sâu hơn trong mỗi đoạn (cả hai khi cùng hàng) và giữ các đĩa còn lại ở lớp thường.
        private void UpdateIngredientLayers(IReadOnlyList<Food> members, IReadOnlyList<int> rows)
        {
            for (int i = 0; i < members.Count; i++)
            {
                bool raised = (i > 0 && rows[i] >= rows[i - 1]) || (i + 1 < members.Count && rows[i] >= rows[i + 1]);
                members[i].SetIngredientLayer(raised ? raisedIngredientPlateOffset : ingredientPlateOffset);
            }
        }

        // EN: Places each cord segment between fixed tray anchors and tints it with its two eyelets; segments without a member pair stay hidden.
        // VI: Đặt từng đoạn dây giữa các điểm gắn cố định của khay, tô màu cùng hai móc; đoạn không có cặp đĩa thì ẩn.
        private void UpdateCords(IReadOnlyList<Food> members, bool highlighted)
        {
            Color tint = highlighted ? highlightTint : waitingTint;
            for (int i = 0; i < cords.Count; i++)
            {
                bool valid = i + 1 < members.Count && members[i] != null && members[i + 1] != null;
                Food a = valid ? members[i] : null;
                Food b = valid ? members[i + 1] : null;
                bool visible = valid && (a.gameObject.activeInHierarchy || b.gameObject.activeInHierarchy);
                cords[i].enabled = visible;
                eyelets[i * 2].enabled = eyelets[i * 2 + 1].enabled = visible;
                if (!valid)
                    continue;
                Bounds first = GetPlateBounds(a);
                Bounds second = GetPlateBounds(b);
                Vector3 start = GetAnchorPoint(a, first.center, second.center);
                Vector3 end = GetAnchorPoint(b, second.center, first.center);
                int sorting = GetSortingOrder(members);
                int layer = a.PlateRenderer != null ? a.PlateRenderer.sortingLayerID : 0;
                cords[i].SetPosition(0, start);
                cords[i].SetPosition(1, end);
                cords[i].sortingLayerID = layer;
                cords[i].sortingOrder = sorting;
                cords[i].startColor = cords[i].endColor = tint;
                float size = Mathf.Min(first.size.x, first.size.y, second.size.x, second.size.y);
                cords[i].widthMultiplier = size * cordWidthRatio;
                cords[i].textureScale = new Vector2(1f / Mathf.Max(0.001f, cords[i].widthMultiplier * 2f), 1f);
                SetEyelet(eyelets[i * 2], start, size, tint, layer, sorting);
                SetEyelet(eyelets[i * 2 + 1], end, size, tint, layer, sorting);
            }
        }

        // EN: Gets reliable plate bounds even when offscreen pooling visibility disables a dish.
        // VI: Lấy giới hạn đĩa chính xác kể cả khi đĩa bị tắt do nằm ngoài màn hình.
        private static Bounds GetPlateBounds(Food dish)
        {
            SpriteRenderer plate = dish.PlateRenderer;
            if (plate == null || plate.sprite == null)
                return new Bounds(dish.transform.position, Vector3.one);
            Vector3 scale = plate.transform.lossyScale;
            scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            return new Bounds(plate.transform.TransformPoint(plate.sprite.bounds.center), Vector3.Scale(plate.sprite.bounds.size, scale));
        }

        // EN: Picks the dish's prefab anchor whose direction from the tray centre best matches the direction to the other dish; falls back to the tray centre when the dish has no anchors.
        // VI: Chọn điểm gắn trong prefab đĩa có hướng từ tâm khay gần nhất với hướng tới đĩa kia; dùng tâm khay khi đĩa chưa có điểm gắn.
        private static Vector3 GetAnchorPoint(Food dish, Vector3 center, Vector3 target)
        {
            Vector2 direction = ((Vector2)(target - center)).normalized;
            Vector3 best = center;
            float bestDot = float.MinValue;
            foreach (Transform anchor in dish.LinkAnchors)
            {
                float dot = Vector2.Dot(((Vector2)(anchor.position - center)).normalized, direction);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = anchor.position;
                }
            }
            return best;
        }

        // EN: Draws the link at the highest member plate + cordPlateOffset: above lids and normal food, below raised back-row food, count badges and ice; follows members boosted by PickupHand.
        // VI: Vẽ dây ở mặt đĩa cao nhất của nhóm + cordPlateOffset: trên nắp và món thường, dưới món hàng sau được nâng, số đếm và băng; bám theo thành viên được PickupHand nâng thứ tự vẽ.
        private int GetSortingOrder(IReadOnlyList<Food> members)
        {
            int order = 0;
            foreach (Food dish in members)
                if (dish != null && dish.PlateRenderer != null)
                    order = Mathf.Max(order, dish.PlateRenderer.sortingOrder);
            return order + cordPlateOffset;
        }

        // EN: Positions an eyelet at a constant world size relative to the plate, even under scaled board parents, nudged toward the camera to cover the cord end.
        // VI: Đặt móc theo kích thước thế giới tỷ lệ với đĩa, kể cả khi board cha có scale, đẩy nhẹ về phía camera để che đầu dây.
        private void SetEyelet(SpriteRenderer ring, Vector3 position, float size, Color tint, int layer, int order)
        {
            ring.transform.position = position + new Vector3(0f, 0f, eyeletDepthOffset);
            Vector3 parentScale = ring.transform.parent.lossyScale;
            Vector2 spriteSize = ring.sprite != null ? (Vector2)ring.sprite.bounds.size : Vector2.one;
            float diameter = size * eyeletSizeRatio;
            ring.transform.localScale = new Vector3(diameter / Mathf.Max(0.001f, Mathf.Abs(parentScale.x) * spriteSize.x),
                diameter / Mathf.Max(0.001f, Mathf.Abs(parentScale.y) * spriteSize.y), 1f);
            ring.color = tint;
            ring.sortingLayerID = layer;
            ring.sortingOrder = order;
        }
    }
}
