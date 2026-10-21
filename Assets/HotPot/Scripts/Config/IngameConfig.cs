using System;
using UnityEngine;

namespace FoodPop
{
    // EN: Shared gameplay timing and animation tuning; scene layout and prefab appearance remain on their own components.
    // VI: Cấu hình thời gian và hoạt cảnh màn chơi dùng chung; bố cục scene và hình thức prefab giữ tại component tương ứng.
    [CreateAssetMenu(fileName = "IngameConfig", menuName = "Hot Pot/Ingame Config")]
    public class IngameConfig : ScriptableObject
    {
        [Serializable]
        public class IngredientFlightSettings
        {
            [Tooltip("Thời gian nguyên liệu bay từ băng chuyền tới ô khay, tính bằng giây.")]
            [Min(0.05f)] public float serveFlyDuration = 0.55f;
            [Tooltip("Độ cao đỉnh vòng cung của nguyên liệu, tính bằng đơn vị thế giới.")]
            [Min(0f)] public float serveArcHeight = 2f;
            [Tooltip("Hệ số phóng to của nguyên liệu ở đỉnh vòng cung.")]
            [Min(1f)] public float servePeakScale = 1.3f;
            [Tooltip("Tỉ lệ nguyên liệu lúc tới thẻ order khi không có khay, so với lúc rời băng chuyền.")]
            [Range(0.1f, 1f)] public float serveEndScale = 0.55f;
            [Tooltip("Thứ tự vẽ cộng thêm để nguyên liệu đang bay nằm trên băng chuyền và khay.")]
            public int serveSortingBoost = 40;
            [Tooltip("Thời gian nảy của nguyên liệu khi đáp xuống khay, tính bằng giây.")]
            [Min(0.01f)] public float landPopDuration = 0.18f;
            [Tooltip("Độ phóng to khi nguyên liệu đáp xuống khay, 0.2 tương ứng tăng thêm 20%.")]
            [Range(0f, 1f)] public float landPopAmount = 0.2f;
        }

        [Tooltip("Nguyên liệu bay tới khay")]
        public IngredientFlightSettings ingredientFlight = new IngredientFlightSettings();

        [Serializable]
        public class BoardMotionSettings
        {
            [Tooltip("Thời gian (giây) các đĩa còn lại trong cột trượt lên một hàng sau khi đĩa đầu được chọn.")]
            [Min(0f)] public float shiftUpDuration = 0.18f;
        }

        [Tooltip("Chuyển động trên board")]
        public BoardMotionSettings boardMotion = new BoardMotionSettings();

        [Serializable]
        public class BoardFlightSettings
        {
            [Tooltip("Thời gian (giây) một phần nguyên liệu bay từ đĩa lên slot băng chuyền.")]
            [Min(0.01f)] public float flyDuration = 0.4f;
            [Tooltip("Độ cao đỉnh vòng cung (đơn vị world) so với đường thẳng nối điểm đầu và điểm cuối.")]
            [Min(0f)] public float arcHeight = 1.5f;
            [Tooltip("Độ trễ (giây) giữa các phần khi một đĩa có nhiều phần.")]
            [Min(0f)] public float portionDelay = 0.06f;
            [Tooltip("Đường cong tiến độ bay theo thời gian (trục ngang 0→1 là thời gian, trục dọc 0→1 là quãng đường).")]
            public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            [Tooltip("Hệ số phóng to ở đỉnh vòng cung so với kích thước bình thường.")]
            [Min(1f)] public float peakScale = 1.25f;
            [Tooltip("Số vòng xoay trong lúc bay; 0 để không xoay.")]
            public float spinTurns = 1f;
            [Tooltip("Cộng thêm vào thứ tự vẽ để món đang bay luôn nằm trên board và băng chuyền.")]
            public int sortingOrderBoost = 100;
        }

        [Tooltip("Nguyên liệu bay lên băng chuyền")]
        public BoardFlightSettings boardFlight = new BoardFlightSettings();

        [Serializable]
        public class DishAssemblySettings
        {
            [Tooltip("Thời gian (giây) chờ sau nguyên liệu cuối rồi mới ghép thành món.")]
            [Min(0f)] public float assembleDelay = 0.12f;
            [Tooltip("Thời gian (giây) các lớp chụm vào giữa ô trước khi hiện món hoàn chỉnh.")]
            [Min(0.01f)] public float mergeDuration = 0.18f;
            [Tooltip("Thời gian (giây) nảy của món hoàn chỉnh khi xuất hiện.")]
            [Min(0.01f)] public float dishPopDuration = 0.3f;
            [Tooltip("Độ phóng to tối đa khi món hoàn chỉnh xuất hiện (0.35 = to thêm 35%).")]
            [Range(0f, 1f)] public float dishPopAmount = 0.35f;
        }

        [Tooltip("Ghép món trên khay")]
        public DishAssemblySettings dishAssembly = new DishAssemblySettings();

        [Serializable]
        public class DishOrderFlightSettings
        {
            [Tooltip("Thời gian (giây) món hoàn chỉnh nằm trên khay sau khi nảy rồi mới bay lên thẻ.")]
            [Min(0f)] public float dishFlyDelay = 0.15f;
            [Tooltip("Thời gian (giây) món hoàn chỉnh bay từ ô khay lên thẻ order.")]
            [Min(0.05f)] public float dishFlyDuration = 0.5f;
            [Tooltip("Độ cao đỉnh vòng cung khi món hoàn chỉnh bay lên thẻ (đơn vị world).")]
            [Min(0f)] public float dishFlyArcHeight = 1.2f;
            [Tooltip("Hệ số phóng to của món hoàn chỉnh ở giữa đường bay vào order, sau đó thu về đúng kích thước ô món.")]
            [Min(1f)] public float dishFlyPeakScale = 1.3f;
        }

        [Tooltip("Món hoàn chỉnh bay vào order")]
        public DishOrderFlightSettings dishOrderFlight = new DishOrderFlightSettings();

        [Serializable]
        public class CustomersSettings
        {
            [Tooltip("Thời gian (giây) chờ sau khi bắn tim rồi khách mới rời đi.")]
            [Min(0f)] public float leaveDelay = 0.6f;
            [Tooltip("Thời gian (giây) khách trượt ra hoặc trượt vào.")]
            [Min(0.05f)] public float slideDuration = 0.45f;
            [Tooltip("Quãng đường (đơn vị world) trượt sang ngang để khách ra khỏi màn hình.")]
            [Min(0f)] public float slideDistance = 9f;
            [Tooltip("Màu phủ cho khách chưa tới lượt order (tối hơn khách đang order).")]
            public Color waitingTint = new Color(0.55f, 0.55f, 0.6f, 1f);
            [Tooltip("Kích thước khách chưa tới lượt so với khách đang order (0.8 = nhỏ hơn 20%).")]
            [Range(0.3f, 1f)] public float waitingScale = 0.8f;
            [Tooltip("Thời gian (giây) khách đầu hàng bước lên quầy và cả hàng dồn lên.")]
            [Min(0.05f)] public float stepForwardDuration = 0.4f;
            [Tooltip("Khoảng cách (world) giữa hai người trong hàng khi scene không đặt sẵn khách chờ: x dịch ra phía ngoài làn, y dịch lên phía sau.")]
            public Vector2 queueSpacing = new Vector2(0.8f, 0.45f);
        }

        [Tooltip("Khách và hàng chờ")]
        public CustomersSettings customers = new CustomersSettings();

        [Serializable]
        public class PreparationSettings
        {
            [Tooltip("Thời gian sơ chế một nguyên liệu, tính bằng giây.")]
            [Min(0.01f)] public float preparationDuration = 1f;
            [Tooltip("Độ nảy của món và thớt ở mỗi nhát chặt (0.08 = bẹp 8%).")]
            [Range(0f, 0.3f)] public float chopPunch = 0.08f;
            [Tooltip("Hệ số phóng to món khi nằm trước mặt đầu bếp so với kích thước trên đĩa.")]
            [Min(0.1f)] public float cuttingScale = 1.3f;
            [Tooltip("Thời gian (giây) món bay từ đĩa lên trước mặt đầu bếp.")]
            [Min(0.01f)] public float liftDuration = 0.25f;
            [Tooltip("Thời gian (giây) món đã sơ chế rơi lại vào đĩa.")]
            [Min(0.01f)] public float dropDuration = 0.3f;
            [Tooltip("Độ cao vòng cung (world) khi món bay lên và rơi xuống.")]
            [Min(0f)] public float hopHeight = 0.6f;
            [Tooltip("Cộng thêm vào thứ tự vẽ của món để món nằm trên đầu bếp và băng chuyền khi đang cắt.")]
            public int cuttingSortingBoost = 20;
            [Tooltip("Số mảnh vụn bắn ra ở mỗi nhát chặt.")]
            [Min(0)] public int debrisPerChop = 3;
            [Tooltip("Khoảng thời gian (giây) giữa hai nhát chặt; chỉnh cho khớp nhịp hoạt ảnh cắt.")]
            [Min(0.05f)] public float chopInterval = 0.167f;
        }

        [Tooltip("Sơ chế nguyên liệu")]
        public PreparationSettings preparation = new PreparationSettings();

        [Serializable]
        public class OrderPopSettings
        {
            [Tooltip("Thời gian (giây) hiệu ứng nảy khi số lượng thay đổi hoặc hiện dấu tích.")]
            [Min(0.01f)] public float popDuration = 0.25f;
            [Tooltip("Độ phóng to tối đa của hiệu ứng nảy (0.3 = to thêm 30%).")]
            [Range(0f, 1f)] public float popAmount = 0.3f;
        }

        [Tooltip("Hiệu ứng nảy trên order")]
        public OrderPopSettings orderPop = new OrderPopSettings();

        [Serializable]
        public class IceHitSettings
        {
            [Tooltip("Thời gian (giây) khối băng nảy mỗi khi bị trừ giá trị.")]
            [Min(0.01f)] public float iceHitDuration = 0.22f;
            [Tooltip("Độ nảy tối đa của khối băng khi bị trừ giá trị (0.12 = phồng 12%).")]
            [Range(0f, 0.5f)] public float iceHitAmount = 0.12f;
        }

        [Tooltip("Hiệu ứng băng trúng đòn")]
        public IceHitSettings iceHit = new IceHitSettings();

        [Serializable]
        public class TrayStackSettings
        {
            [Tooltip("Độ nhô lên của mỗi lớp nguyên liệu khi xếp chồng trong ô khay, theo tỉ lệ cỡ ô.")]
            [Min(0f)] public float stackStepRatio = 0.09f;
            [Tooltip("Scale cục bộ (theo ô) của mỗi lớp nguyên liệu và của món hoàn chỉnh khi nằm trong ô.")]
            [Min(0.05f)] public float foodScale = 0.45f;
            [Tooltip("Từ nguyên liệu thứ mấy trong ô thì bắt đầu chèn: nguyên liệu đáp vào vị trí lớp 2 rồi cả chồng nảy lên. Các nguyên liệu trước đó đáp thẳng vào chỗ của mình, không nảy.")]
            [Min(2)] public int insertFromIngredient = 3;
            [Tooltip("Thời gian (giây) các lớp nảy lên rồi rơi về vị trí mới khi chèn nguyên liệu. 0 = xếp ngay không hiệu ứng.")]
            [Min(0f)] public float bounceDuration = 0.45f;
            [Tooltip("Độ cao nảy (đơn vị thế giới) cộng dồn theo lớp: lớp 1 đứng im, lớp 2 nảy 1 lần giá trị này, lớp 3 nảy 2 lần...")]
            [Min(0f)] public float bounceStep = 0.3f;
            [Tooltip("Thời gian (giây) các lớp đứng yên ở đỉnh cú nảy khi đang tách xa nhất, trước khi rơi xuống.")]
            [Min(0f)] public float bounceHold = 0.1f;
        }

        [Tooltip("Xếp chồng nguyên liệu trên khay")]
        public TrayStackSettings trayStack = new TrayStackSettings();

        [Serializable]
        public class LidRevealSettings
        {
            [Tooltip("Thời gian (giây) nắp của đĩa ẩn mờ dần rồi biến mất khi đĩa trượt tới ô đầu cột; chỉ sau đó đĩa mới mở và chọn được. 0 = mở ngay.")]
            [Min(0f)] public float fadeDuration = 0.35f;
            [Tooltip("Độ cao (đơn vị world) nắp được nhấc lên trong lúc mờ dần.")]
            [Min(0f)] public float liftHeight = 0.25f;
            [Tooltip("Tỉ lệ phóng to của nắp ở cuối lúc mờ dần (1 = giữ nguyên cỡ).")]
            [Min(1f)] public float scaleUp = 1.15f;
        }

        [Tooltip("Mở nắp đĩa ẩn")]
        public LidRevealSettings lidReveal = new LidRevealSettings();

        [Serializable]
        public class PraiseEntry
        {
            [Tooltip("Sprite chữ khen.")]
            public Sprite sprite;
        }

        [Serializable]
        public class PraiseTextSettings
        {
            [Tooltip("Các chữ khen; mỗi lần khách nhận đủ order sẽ bắn ra ngẫu nhiên một chữ.")]
            public PraiseEntry[] entries = new PraiseEntry[0];
            [Tooltip("Chiều cao (đơn vị world) của sprite chữ khen ở cỡ thường.")]
            [Min(0.05f)] public float height = 0.9f;
            [Tooltip("Chiều rộng tối đa (đơn vị world); chữ dài sẽ được thu nhỏ để không vượt quá.")]
            [Min(0.05f)] public float maxWidth = 2.8f;
            [Tooltip("Độ lệch (world) so với điểm bắn tim của khách.")]
            public Vector2 offset = new Vector2(0f, 0.6f);
            [Tooltip("Quãng đường (world) chữ bay lên trong suốt hiệu ứng.")]
            [Min(0f)] public float riseHeight = 1.2f;
            [Tooltip("Tổng thời gian (giây) chữ hiện, bay lên và mờ dần.")]
            [Min(0.05f)] public float duration = 1.1f;
            [Tooltip("Độ phóng to lúc chữ bật ra trước khi thu về cỡ thường (1.4 = to hơn 40%).")]
            [Min(1f)] public float popScale = 1.4f;
            [Tooltip("Góc nghiêng ngẫu nhiên tối đa (độ) của chữ.")]
            [Range(0f, 30f)] public float maxTilt = 8f;
        }

        [Tooltip("Chữ khen khi khách nhận đủ order")]
        public PraiseTextSettings praiseText = new PraiseTextSettings();

        [Serializable]
        public class DishTwinkleSettings
        {
            [Tooltip("Hiệu ứng bắt đầu (cố định tại ô khay) khi trong ô đã có từ số nguyên liệu này trở lên; dừng khi món hoàn chỉnh bay lên thẻ order.")]
            [Min(1)] public int minLayers = 2;
        }

        [Tooltip("Sao lấp lánh tại ô khay")]
        public DishTwinkleSettings dishTwinkle = new DishTwinkleSettings();

        private static IngameConfig defaultConfig;

        // EN: Uses an explicitly assigned config or the shared Resources asset for prefab instances without an override.
        // VI: Dùng cấu hình được gán hoặc asset Resources dùng chung cho prefab chưa có cấu hình riêng.
        public static IngameConfig Resolve(IngameConfig assigned)
        {
            if (assigned != null)
                return assigned;
            if (defaultConfig == null)
                defaultConfig = Resources.Load<IngameConfig>("IngameConfig");
            if (defaultConfig == null)
                throw new InvalidOperationException("Chưa có asset IngameConfig trong thư mục Resources.");
            return defaultConfig;
        }
    }
}