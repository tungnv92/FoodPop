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
        Sesame = 11,
        Octopus = 12,
        FriedTofu = 13,
        Scallop = 14
    }
}
