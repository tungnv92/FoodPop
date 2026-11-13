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
        levelTmp.text = $"Level {ProfileManager.instance.PlayerDataManager.GetCurrentLevel()}";
        bool isHard = GameManager.Ins.IsHardLevel();
        bool isSuperHard = GameManager.Ins.IsSuperHardLevel();

        hardButton.SetActive(isHard);
        superHardButton.SetActive(isSuperHard);
        levelTmp.color = levelTextColors[isHard ? 1 : (isSuperHard ? 2 : 0)];
    }
    public void OnClick()
    {
        //if (!NetworkManager.Ins.IsInternetAvailable())
        //{
        //    UIManager.Ins.OpenPopup(PopupId.NoInternet);
        //    return;
        //}


        if (ProfileManager.instance.GameFeatureContext.HeartManager.IsEnoughHeartToPlay())
        {
            LoadSceneManager.Ins.LoadScene(SceneId.Game, () => { });
        }
        else UIManager.Ins.OpenPopup(PopupId.MoreLife);
    }
}
