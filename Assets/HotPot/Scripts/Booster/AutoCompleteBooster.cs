using UnityEngine;

namespace FoodPop
{
    // EN: AutoComplete booster: finishes the ordered dish missing the most ingredients. A particle star with its magic circle appears above the chef, the missing ingredients (taken from the conveyor first, then from the board) fly into it and vanish, the star flies to the dish's slot on the serving tray leaving a sparkle trail and bursts, and the ingredients pop out and drop into the slot in recipe order; the usual dish-complete flow follows. Holds the particle prefabs and timing; BoosterController runs the logic.
    // VI: Booster AutoComplete: hoàn thành món đang được order còn thiếu nhiều nguyên liệu nhất. Ngôi sao particle cùng vòng phép hiện trên đầu bếp, các nguyên liệu còn thiếu (lấy từ băng chuyền trước, rồi tới board) bay vào sao và biến mất, sao bay tới ô của món trên khay phục vụ để lại vệt lấp lánh rồi nổ, nguyên liệu bung ra và rơi vào ô theo thứ tự công thức; sau đó là luồng hoàn thành món như thường. Giữ prefab particle và thời gian; BoosterController chạy logic.
    public class AutoCompleteBooster : Booster
    {
        public override BoosterType Type => BoosterType.AutoComplete;

        [Header("Particle")]
        [Tooltip("Prefab particle ngôi sao lặp lại (lõi sao, quầng sáng, vòng phép, sao chổi, bokeh, sao nhỏ, vệt bay).")]
        public ParticleEffect starEffect;
        [Tooltip("Prefab particle vụ nổ khi sao tới khay.")]
        public ParticleEffect burstEffect;
        [Tooltip("Prefab particle chùm lấp lánh nhỏ khi sao xuất hiện và mỗi lần hút nguyên liệu.")]
        public ParticleEffect sparkleEffect;
        [Tooltip("Thứ tự vẽ của hiệu ứng (cao hơn khay và băng chuyền).")]
        public int sortingOrder = 500;

        [Header("Ngôi sao")]
        [Tooltip("Tỉ lệ kích thước hiệu ứng ngôi sao (1 = sao cao khoảng 1 đơn vị world).")]
        [Min(0.1f)] public float starScale = 1f;
        [Tooltip("Vị trí sao so với đầu bếp (đơn vị world).")]
        public Vector2 starOffset = new Vector2(0f, 1.6f);
        [Tooltip("Tỉ lệ phóng to thêm của sao khi ở đầu bếp (1.6 = to hơn 60% so với starScale).")]
        [Min(0.1f)] public float chefScale = 1.6f;
        [Tooltip("Tỉ lệ của sao khi tới ô món trên khay, sao thu nhỏ dần về cỡ này trong lúc bay (0.6 = bằng 60% starScale).")]
        [Min(0.05f)] public float slotScale = 0.6f;
        [Tooltip("Thời gian (giây) sao và vòng phép lớn dần lúc xuất hiện.")]
        [Min(0.01f)] public float appearDuration = 0.35f;
        [Tooltip("Độ phồng thêm của sao mỗi lần hút một nguyên liệu (0.2 = to hơn 20%).")]
        [Min(0f)] public float absorbPulse = 0.2f;

        [Header("Nguyên liệu bay vào sao")]
        [Tooltip("Khoảng cách (giây) giữa hai nguyên liệu bay vào sao.")]
        [Min(0f)] public float gatherInterval = 0.13f;
        [Tooltip("Thời gian (giây) một nguyên liệu bay vào sao.")]
        [Min(0.05f)] public float gatherDuration = 0.4f;
        [Tooltip("Độ cao vòng cung (đơn vị world) khi nguyên liệu bay vào sao.")]
        [Min(0f)] public float gatherArcHeight = 1.2f;
        [Tooltip("Kích thước nguyên liệu khi bay (đơn vị world).")]
        [Min(0.05f)] public float ingredientSize = 0.7f;
        [Tooltip("Thời gian (giây) sao còn toả sáng sau khi hút hết nguyên liệu.")]
        [Min(0f)] public float shineHold = 0.2f;

        [Header("Bay tới khay + nổ")]
        [Tooltip("Thời gian (giây) sao bay tới ô món trên khay.")]
        [Min(0.05f)] public float flyDuration = 0.7f;
        [Tooltip("Độ lệch vòng cung (đơn vị world) khi sao bay tới khay.")]
        public float flyArc = 1.5f;
        [Tooltip("Độ cao (đơn vị world) phía trên ô món nơi sao nổ.")]
        [Min(0f)] public float burstHeight = 0.6f;
        [Tooltip("Thời gian (giây) sao thu nhỏ biến mất ngay trước vụ nổ.")]
        [Min(0.01f)] public float vanishDuration = 0.1f;
        [Tooltip("Khoảng cách (đơn vị world) nguyên liệu bung ra từ tâm vụ nổ.")]
        [Min(0f)] public float popDistance = 0.9f;
        [Tooltip("Thời gian (giây) nguyên liệu bung ra trước khi rơi vào ô.")]
        [Min(0.01f)] public float popDuration = 0.22f;
        [Tooltip("Khoảng cách (giây) giữa hai nguyên liệu rơi vào ô.")]
        [Min(0f)] public float dropInterval = 0.12f;
    }
}
