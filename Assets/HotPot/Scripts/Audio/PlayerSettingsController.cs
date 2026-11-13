using GamePackages;
using Lofelt.NiceVibrations;
using UnityEngine;

namespace FoodPop
{
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class PlayerSettingsController : MonoBehaviour
    {
        [Tooltip("Nguồn phát hiệu ứng âm thanh của game.")]
        [SerializeField] private AudioSource soundSource;
        [Tooltip("Nguồn phát nhạc nền; gán clip nhạc mong muốn trên AudioSource.")]
        [SerializeField] private AudioSource musicSource;
        private PlayerDataManager playerData;

        // EN: Applies saved preferences after profile initialization and subscribes to later changes.
        // VI: Áp dụng cài đặt đã lưu sau khởi tạo hồ sơ và theo dõi thay đổi tiếp theo.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            playerData = ProfileManager.instance.PlayerDataManager;
            playerData.StateChanged += ApplySettings;
            ApplySettings();
        }

        // EN: Releases the saved-preference listener when leaving the scene.
        // VI: Gỡ listener cài đặt khi rời scene.
        private void OnDisable()
        {
            if (Application.isPlaying) playerData.StateChanged -= ApplySettings;
        }

        // EN: Independently mutes sound and music and enables or stops global haptics.
        // VI: Tắt tiếng âm thanh và nhạc độc lập, đồng thời bật hoặc dừng rung toàn cục.
        private void ApplySettings()
        {
            soundSource.mute = !playerData.IsSoundOn;
            musicSource.mute = !playerData.IsMusicOn;
            HapticController.hapticsEnabled = playerData.isVibrateOn;
        }
    }
}
