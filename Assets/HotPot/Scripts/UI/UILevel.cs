using TMPro;
using UnityEngine;

namespace FoodPop
{
    [DisallowMultipleComponent]
    public sealed class UILevel : MonoBehaviour
    {
        [Tooltip("Chữ hiển thị số màn chơi đang nạp.")]
        [SerializeField] private TMP_Text levelLabel;
        [Tooltip("Controller nạp màn chơi trong scene.")]
        [SerializeField] private LevelController levelController;

        // EN: Reads the current configuration and listens for load, replay and next-level changes.
        // VI: Đọc cấu hình hiện tại và theo dõi nạp màn, chơi lại hoặc sang màn tiếp theo.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            levelController.LevelLoaded += ShowLevel;
            ShowLevel(levelController.CurrentLevel);
        }

        // EN: Removes the level-load listener when hidden.
        // VI: Gỡ listener nạp màn khi phần hiển thị bị ẩn.
        private void OnDisable()
        {
            if (Application.isPlaying) levelController.LevelLoaded -= ShowLevel;
        }

        // EN: Displays the authored level number without changing saved player progression.
        // VI: Hiện số màn đã cấu hình mà không thay đổi tiến trình người chơi đã lưu.
        private void ShowLevel(LevelConfig config)
        {
            if (config != null) levelLabel.text = $"Level {config.levelNumber}";
        }
    }
}
