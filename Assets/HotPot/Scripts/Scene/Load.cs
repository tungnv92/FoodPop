using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using GamePackages;

public class Load : MonoBehaviour
{
    [Tooltip("Thanh tiến độ tải.")]
    [SerializeField] Image fillImage;
    [Tooltip("Từ level lớn hơn số này mới vào Home sau khi tải; nhỏ hơn hoặc bằng thì vào thẳng gameplay.")]
    [SerializeField] int backHomeAfterLevel = 0;

    // EN: Applies the app frame-rate settings before any scene logic runs.
    // VI: Áp dụng cài đặt tốc độ khung hình của ứng dụng trước khi logic scene chạy.
    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Physics.simulationMode = SimulationMode.Update;
    }

    // EN: Shows unscaled loading progress and waits for profile initialization before entering Home or gameplay.
    // VI: Hiện tiến độ tải bằng thời gian thực và chờ khởi tạo hồ sơ trước khi vào Home hoặc gameplay.
    IEnumerator Start()
    {
        Tween tween = DOVirtual.Float(0, 0.8f, 2, SetProgress).SetUpdate(true);
        yield return new WaitUntil(() => !tween.active);
        yield return new WaitUntil(() => ProfileManager.instance.IsReady);
        bool canBackHome = ProfileManager.instance.PlayerDataManager.GetCurrentLevel() > backHomeAfterLevel;
        SceneId scene = canBackHome ? SceneId.Home : SceneId.Game;
        DOVirtual.Float(0.8f, 1f, 0.5f, SetProgress).OnComplete(() => LoadSceneManager.Ins.LoadScene(scene));
    }

    // EN: Fills the loading bar to the given progress.
    // VI: Tô thanh tải theo tiến độ được truyền vào.
    void SetProgress(float progress)
    {
        fillImage.fillAmount = progress;
    }
}
