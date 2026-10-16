namespace FoodPop
{
    // EN: Processing state of a food; keep numeric values stable because they are serialized.
    // VI: Trạng thái chế biến của nguyên liệu; giữ nguyên giá trị số vì đã được lưu (serialize).
    public enum FoodState
    {
        Raw = 0,       // EN: initial raw ingredient / VI: nguyên liệu thô ban đầu
        Prepared = 1   // EN: ingredient after preparation / VI: nguyên liệu đã sơ chế
    }
}
