namespace FoodPop
{
    // EN: Kinds of in-game boosters.
    // VI: Các loại booster trong màn chơi.
    public enum BoosterType
    {
        PickupHand = 0,   // EN: pick any dish on the board / VI: lấy đĩa bất kỳ trên board
        CleanBrush = 1,   // EN: clear unneeded food off the conveyor / VI: dọn món thừa trên băng chuyền
        Shuffle = 2,      // EN: shuffle the dishes on the board / VI: xáo trộn đĩa trên board
        AutoComplete = 3, // EN: finish one ordered dish / VI: hoàn thành một món của khách
    }
}
