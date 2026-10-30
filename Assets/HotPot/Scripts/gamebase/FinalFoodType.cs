namespace FoodPop
{
    // EN: Stable IDs of final dishes a customer can order; value = dish line x 10 + ingredient count. Keep numeric values unchanged once levels use them.
    // VI: ID cố định của các món hoàn chỉnh (final food) mà khách order; giá trị = dòng món x 10 + số nguyên liệu. Giữ nguyên giá trị số khi level đã dùng.
    public enum FinalFoodType
    {
        None = 0,
        SakeNigiri2 = 12,
        SakeNigiri3 = 13,
        SakeNigiri4 = 14,
        SakeNigiri5 = 15,
        SakeNigiri6 = 16,
        MaguroNigiri2 = 22,
        MaguroNigiri3 = 23,
        MaguroNigiri4 = 24,
        MaguroNigiri5 = 25,
        MaguroNigiri6 = 26,
        EbiNigiri2 = 32,
        EbiNigiri3 = 33,
        EbiNigiri4 = 34,
        EbiNigiri5 = 35,
        EbiNigiri6 = 36,
        // EN: IDs 42 and 43 are retired; never reuse them.
        // VI: ID 42 và 43 đã ngừng dùng; không tái sử dụng.
        TamagoNigiri4 = 44,
        TamagoNigiri5 = 45,
        TamagoNigiri6 = 46,
        UnagiNigiri2 = 52,
        UnagiNigiri3 = 53,
        UnagiNigiri4 = 54,
        UnagiNigiri5 = 55,
        UnagiNigiri6 = 56,
        TakoNigiri2 = 62,
        TakoNigiri3 = 63,
        TakoNigiri4 = 64,
        TakoNigiri5 = 65,
        TakoNigiri6 = 66,
        HotateNigiri2 = 72,
        HotateNigiri3 = 73,
        HotateNigiri4 = 74,
        HotateNigiri5 = 75,
        HotateNigiri6 = 76,
        // EN: IDs 82 and 83 are retired; never reuse them.
        // VI: ID 82 và 83 đã ngừng dùng; không tái sử dụng.
        KanikamaNigiri4 = 84,
        KanikamaNigiri5 = 85,
        KanikamaNigiri6 = 86,
        GreenTea = 91,
        IcedGreenTea = 92,
        BoiledEdamame = 101,
        SaltedEdamame = 102,
        TeriyakiChickenSkewer = 112
    }
}
