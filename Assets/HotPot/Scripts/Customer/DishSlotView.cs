using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Root component of the DishSlot prefab: holds direct references to the slot's own visuals, including the twinkling-star particle effect nested in the prefab, so gameplay code never searches children.
    // VI: Component gốc của prefab DishSlot: giữ tham chiếu trực tiếp tới hình ảnh của ô, gồm cả hiệu ứng particle sao lấp lánh lồng sẵn trong prefab, để code gameplay không phải tìm child.
    [DisallowMultipleComponent]
    public class DishSlotView : MonoBehaviour
    {
        // EN: True when a dish picture is prepared on this slot (its layers appear as the ingredients land).
        // VI: True khi ô đã chuẩn bị hình món (các lớp hiện dần khi nguyên liệu đáp xuống).
        public bool HasDish => dish != null;

        // EN: Number of dish layers already shown.
        // VI: Số lớp món đã hiện.
        public int RevealedCount => revealedCount;

        // EN: Returns the slot's dashed outline renderer (may be null if not wired in the prefab).
        // VI: Trả về SpriteRenderer viền nét đứt của ô (có thể null nếu chưa gán trong prefab).
        public SpriteRenderer Outline => outline;

        // EN: True while the twinkle is emitting.
        // VI: True khi hiệu ứng lấp lánh đang phát.
        public bool IsTwinkling => twinkling;

        // EN: True while the final arriving ingredient is playing its preparation animation.
        // VI: Đúng khi nguyên liệu cuối vừa đáp đang chạy hoạt cảnh chế biến.
        public bool IsPreparing => preparing;

        [Tooltip("Hiệu ứng particle sao lấp lánh đặt sẵn trong ô (con của prefab DishSlot).")]
        [SerializeField] private ParticleEffect twinkle;

        [Tooltip("SpriteRenderer viền nét đứt của ô (child \"Slot Outline\" trong prefab DishSlot), dùng để đặt thứ tự vẽ và đo bề rộng ô.")]
        [SerializeField] private SpriteRenderer outline;

        [Header("Món trên ô")]
        [Tooltip("Đĩa đựng món (child \"Plate\" trong prefab DishSlot): ẩn khi ô trống, hiện ra khi nguyên liệu đầu tiên bay tới nơi.")]
        [SerializeField] private SpriteRenderer plate;
        [Tooltip("Đĩa riêng cho món có nguyên liệu trà (child \"Plate_Tea\" trong prefab DishSlot); để trống thì món trà dùng đĩa thường.")]
        [SerializeField] private SpriteRenderer teaPlate;
        [Tooltip("Thời gian đĩa nảy hiện ra (giây).")]
        [Min(0f)] [SerializeField] private float plateShowDuration = 0.2f;
        [Tooltip("Thời gian một lớp nguyên liệu của món nảy hiện ra khi nguyên liệu đáp xuống (giây).")]
        [Min(0f)] [SerializeField] private float layerRevealDuration = 0.18f;

        private Coroutine preparation;
        private bool preparing;
        private bool twinkling;
        private readonly Dictionary<FinalFoodView, FinalFoodView> dishInstances = new Dictionary<FinalFoodView, FinalFoodView>();
        private FinalFoodView dish;
        private readonly List<Vector3> layerScales = new List<Vector3>();
        private readonly List<bool> revealed = new List<bool>();
        private int revealedCount;
        private readonly Dictionary<SpriteRenderer, Vector3> plateRestScales = new Dictionary<SpriteRenderer, Vector3>();
        private SpriteRenderer activePlate;
        private OrderTweens tweens;
        private OrderTweens Tweens => tweens ?? (tweens = new OrderTweens(this));







#if UNITY_EDITOR
        // EN: Editor only: assigns the normal and tea plate renderers from their child objects when they are empty.
        // VI: Chỉ trong editor: gán renderer đĩa thường và đĩa trà từ object con khi còn trống.
        private void Reset()
        {
            if (plate == null)
                plate = FindChildRenderer("Plate");
            if (teaPlate == null)
                teaPlate = FindChildRenderer("Plate_Tea");
        }

        // EN: Editor only: fills the plate references from the "Plate" and "Plate_Tea" children when empty (no Find at runtime).
        // VI: Chỉ trong editor: tự gán đĩa từ child "Plate" và "Plate_Tea" khi còn trống (không Find lúc chạy).
        private void OnValidate()
        {
            Reset();
        }
#endif

        // EN: Cancel pending preparation when this slot leaves active gameplay.
        // VI: Hủy chế biến đang chờ khi ô rời gameplay hoạt động.
        private void OnDisable()
        {
            StopPreparation();
        }

        // EN: Prepares the slot for one ordered dish: an instance of its FinalFood prefab (reused per prefab) placed on the slot with every layer hidden; uses the dish's own plate when it has one (e.g. the double dish), otherwise the tea plate for dishes with tea leaves or the normal plate, and keeps that plate hidden until the first ingredient comes.
        // VI: Chuẩn bị ô cho một món được order: instance prefab FinalFood (tái dùng theo prefab) đặt trên ô với mọi lớp đang ẩn; dùng khay riêng của món nếu có (ví dụ khay đôi), nếu không thì đĩa trà cho món có lá trà hoặc đĩa thường, và giữ đĩa đó ẩn cho tới khi nguyên liệu đầu tiên bay tới.
        public void BeginDish(FinalFoodView prefab, float scale, int sortingLayer, int sortingOrder)
        {
            ClearDish();
            if (prefab == null)
                return;
            if (!dishInstances.TryGetValue(prefab, out dish) || dish == null)
            {
                dish = Instantiate(prefab, transform, false);
                dish.name = prefab.name + " (slot)";
                dishInstances[prefab] = dish;
            }
            Transform t = dish.transform;
            t.SetParent(transform, false);
            t.localPosition = new Vector3(0f, 0f, -0.05f);
            t.localRotation = Quaternion.identity;
            t.localScale = new Vector3(scale, scale, 1f);
            activePlate = dish.Plate != null ? dish.Plate : SelectPlate(prefab);
            int order = sortingOrder;
            if (activePlate != null && activePlate != dish.Plate)
            {
                activePlate.sortingLayerID = sortingLayer;
                order = Mathf.Max(order, activePlate.sortingOrder + 1);
            }
            dish.SetSorting(sortingLayer, order);
            layerScales.Clear();
            revealed.Clear();
            revealedCount = 0;
            foreach (FinalFoodView.IngredientLayer layer in dish.Layers)
            {
                layerScales.Add(layer.renderer != null ? layer.renderer.transform.localScale : Vector3.one);
                revealed.Add(false);
                if (layer.renderer != null)
                    layer.renderer.enabled = false;
            }
            if (dish.Plate != null)
                dish.Plate.enabled = false;
            dish.gameObject.SetActive(true);
        }

        // EN: Pops the plate chosen for the current dish in (once per dish); called by RevealIngredient when the first ingredient lands on this slot.
        // VI: Cho đĩa đã chọn cho món hiện tại nảy hiện ra (một lần mỗi món); RevealIngredient gọi khi nguyên liệu đầu tiên đáp xuống ô.
        public void ShowPlate()
        {
            if (activePlate == null || activePlate.enabled)
                return;
            Vector3 rest = GetRestScale(activePlate);
            activePlate.enabled = true;
            Transform t = activePlate.transform;
            t.localScale = Vector3.zero;
            Tweens.Run(t, plateShowDuration, k => t.localScale = Vector3.LerpUnclamped(Vector3.zero, rest, OrderTweens.EaseOutBack(k)),
                () => t.localScale = rest);
        }

        // EN: World position where an ingredient of this type lands: the centre of the first still-hidden dish layer showing it (the slot centre when the dish has no such layer).
        // VI: Vị trí world nơi nguyên liệu loại này đáp xuống: tâm lớp món đầu tiên còn ẩn thể hiện nó (tâm ô nếu món không có lớp đó).
        public Vector3 GetLandingPosition(FoodType type)
        {
            int index = FindLayer(type);
            if (index < 0)
                return transform.position;
            SpriteRenderer renderer = dish.Layers[index].renderer;
            return renderer.sprite != null ? renderer.transform.TransformPoint(renderer.sprite.bounds.center) : renderer.transform.position;
        }

        // EN: Reveal an arriving layer, or start preparation immediately on the final ingredient of an animated dish.
        // VI: Hiện lớp vừa đáp, hoặc chạy chế biến ngay khi nguyên liệu cuối của món có hoạt cảnh đến.
        public bool RevealIngredient(FoodType type)
        {
            ShowPlate();
            int index = FindLayer(type);
            if (index < 0)
                return false;
            revealed[index] = true;
            revealedCount++;
            if (dish.isAnimate && revealedCount == dish.Layers.Count)
            {
                preparing = true;
                preparation = StartCoroutine(PlayPreparation());
                return true;
            }
            SpriteRenderer renderer = dish.Layers[index].renderer;
            renderer.enabled = true;
            Transform t = renderer.transform;
            Vector3 rest = layerScales[index];
            t.localScale = Vector3.zero;
            Tweens.Run(t, layerRevealDuration, k => t.localScale = Vector3.LerpUnclamped(Vector3.zero, rest, OrderTweens.EaseOutBack(k)),
                () => t.localScale = rest);
            return true;
        }

        // EN: Hides the built dish picture (the finished dish takes its place) and optionally the plate.
        // VI: Ẩn hình món đang ghép (món hoàn chỉnh thay chỗ) và có thể ẩn luôn đĩa.
        public void HideDish(bool hidePlate)
        {
            if (dish != null)
                dish.gameObject.SetActive(false);
            if (hidePlate)
                HidePlate();
        }

        // EN: Hides both plates (normal and tea) at their resting scale.
        // VI: Ẩn cả hai đĩa (thường và trà) ở scale nghỉ.
        public void HidePlate()
        {
            HidePlateRenderer(plate);
            HidePlateRenderer(teaPlate);
        }





        // EN: Cancel preparation, hide the dish, restore its layers and its own plate to their resting state, and clear both slot plate visuals.
        // VI: Hủy chế biến, ẩn món, trả các lớp và khay riêng của món về trạng thái nghỉ, rồi dọn hình của cả hai đĩa của ô.
        public void ClearDish()
        {
            StopPreparation();
            if (dish != null)
            {
                for (int i = 0; i < dish.Layers.Count && i < layerScales.Count; i++)
                {
                    SpriteRenderer renderer = dish.Layers[i].renderer;
                    if (renderer == null)
                        continue;
                    Tweens.Stop(renderer.transform);
                    renderer.transform.localScale = layerScales[i];
                    renderer.enabled = true;
                }
                if (dish.Plate != null)
                {
                    HidePlateRenderer(dish.Plate);
                    dish.Plate.enabled = true;
                }
                dish.gameObject.SetActive(false);
            }
            dish = null;
            layerScales.Clear();
            revealed.Clear();
            revealedCount = 0;
            HidePlate();
            activePlate = null;
        }













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

        public void SetPlate(SpriteRenderer renderer)
        {
            plate = renderer;
        }





#endif

        // EN: Stops the plate's pop, restores its resting scale and hides it.
        // VI: Dừng cú nảy của đĩa, trả scale nghỉ và ẩn đĩa.
        private void HidePlateRenderer(SpriteRenderer renderer)
        {
            if (renderer == null)
                return;
            Vector3 rest = GetRestScale(renderer);
            Tweens.Stop(renderer.transform);
            renderer.transform.localScale = rest;
            renderer.enabled = false;
        }

        // EN: Returns the tea plate when the dish has a tea-leaves layer and a tea plate is assigned, otherwise the normal plate.
        // VI: Trả về đĩa trà khi món có lớp lá trà và đã gán đĩa trà, nếu không thì trả về đĩa thường.
        private SpriteRenderer SelectPlate(FinalFoodView prefab)
        {
            if (teaPlate != null)
            {
                foreach (FinalFoodView.IngredientLayer layer in prefab.Layers)
                    if (layer.ingredient == FoodType.TeaLeaves)
                        return teaPlate;
            }
            return plate;
        }

        // EN: Wait for the dish's Animator before allowing assembly and order delivery.
        // VI: Chờ Animator của món trước khi cho ghép và giao lên order.
        private IEnumerator PlayPreparation()
        {
            yield return dish.PlayAnimation();
            preparing = false;
            preparation = null;
        }

        // EN: Cancel an in-flight preparation coroutine on replay, reuse or disable.
        // VI: Hủy coroutine chế biến còn chạy khi chơi lại, tái dùng hoặc tắt.
        private void StopPreparation()
        {
            if (preparation != null) StopCoroutine(preparation);
            preparation = null;
            preparing = false;
        }

        // EN: Find the next unrevealed layer for an arriving ingredient.
        // VI: Tìm lớp chưa hiện tiếp theo cho nguyên liệu vừa đáp.
        private int FindLayer(FoodType type)
        {
            if (dish == null)
                return -1;
            for (int i = 0; i < dish.Layers.Count && i < revealed.Count; i++)
                if (!revealed[i] && dish.Layers[i].ingredient == type && dish.Layers[i].renderer != null)
                    return i;
            return -1;
        }

        // EN: Returns (capturing once, before any pop changes it) the resting local scale of a plate.
        // VI: Trả về (ghi nhớ một lần, trước khi cú nảy làm đổi) scale local nghỉ của một đĩa.
        private Vector3 GetRestScale(SpriteRenderer renderer)
        {
            if (!plateRestScales.TryGetValue(renderer, out Vector3 rest))
            {
                rest = renderer.transform.localScale;
                plateRestScales.Add(renderer, rest);
            }
            return rest;
        }

#if UNITY_EDITOR
        // EN: Editor only: returns the SpriteRenderer of the named direct child, or null.
        // VI: Chỉ trong editor: trả về SpriteRenderer của child trực tiếp có tên cho trước, hoặc null.
        private SpriteRenderer FindChildRenderer(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<SpriteRenderer>() : null;
        }
#endif
    }
}
