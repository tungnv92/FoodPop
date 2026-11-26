public class PopupLavaQuestFail : FoodPop.UICanvasPrime
{
    // EN: Opens the LavaQuest fail popup over the LavaQuest map with the shared open animation.
    // VI: Mở popup thất bại LavaQuest đè lên bản đồ LavaQuest với hiệu ứng mở dùng chung.
    public override void Open()
    {
        base.Open();
        AnimateOpen();
    }

    // EN: Plays the shared close animation, then closes the main LavaQuest popup as well.
    // VI: Chạy hiệu ứng đóng dùng chung rồi đóng luôn popup LavaQuest chính.
    public void ContinueButton()
    {
        CloseWithAnimation(() => FoodPop.UIManager.Instance.CloseUI<LavaQuest>());
    }
}
