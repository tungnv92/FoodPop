using GamePackages;
using TMPro;
using UnityEngine;

public class PlayButton : MonoBehaviour
{
    [SerializeField] GameObject hardButton;
    [SerializeField] GameObject superHardButton;
    [SerializeField] TMP_Text levelTmp;
    [SerializeField] Color[] levelTextColors;
    // EN: Coordinates Start using encapsulated player and feature APIs instead of accessing live save models.
    // VI: Điều phối Start qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
    private void Start()
    {
        levelTmp.text = $"Level {ProfileManager.instance.GetCurrentLevel()}";
        //bool isHard = GameManager.Ins.IsHardLevel();
        //bool isSuperHard = GameManager.Ins.IsSuperHardLevel();

        //hardButton.SetActive(isHard);
        //superHardButton.SetActive(isSuperHard);
        //levelTmp.color = levelTextColors[isHard ? 1 : (isSuperHard ? 2 : 0)];
    }
    // EN: Starts a level only after the Internet gate and profile are ready and enough hearts are available.
    // VI: Chỉ vào màn khi Internet và hồ sơ đã sẵn sàng, đồng thời có đủ mạng.
    public void OnClick()
    {
        if (!ProfileManager.instance.IsReady) return;
        if (ProfileManager.instance.Features.Get<IHeart>()?.IsEnoughHeartToPlay() ?? true)
        {
            LoadSceneManager.Ins.LoadScene(SceneId.Game, () => { });
        }
        else UIManager.Ins.OpenPopup(PopupId.MoreLife);
    }
}
