namespace FoodPop
{
    // EN: Stable ingredient IDs; keep numeric values unchanged when extending saved content.
    // VI: ID nguyên liệu cố định; giữ nguyên giá trị số khi bổ sung nội dung đã lưu.
    public enum FoodType
    {
        None = 0,
        SushiRice = 1,
        Salmon = 2,
        Tuna = 3,
        Shrimp = 4,
        CrabStick = 5,
        Tamagoyaki = 6,
        Cucumber = 7,
        // ID 8 is retired; do not reuse it for another ingredient.
        SalmonRoe = 9,
        Unagi = 10,
        // EN: ID 11 is retired; never reuse it for another ingredient.
        // VI: ID 11 đã ngừng dùng; không tái sử dụng cho nguyên liệu khác.
        Octopus = 12,
        Seaweed = 13,
        Scallop = 14,
        SoySauce = 15,
        PickledGinger = 16,
        Wasabi = 17
    }
}
