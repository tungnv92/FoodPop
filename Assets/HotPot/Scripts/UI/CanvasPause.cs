using System;
using GamePackages;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace FoodPop
{
    [DisallowMultipleComponent]
    public sealed class CanvasPause : UICanvasPrime
    {
        [Serializable]
        private sealed class SettingControl
        {
            public Button button;
            public GameObject onState;
            public GameObject offState;
        }

        [FormerlySerializedAs("primaryButton")]
        [Tooltip("Nút về màn hình Home.")]
        [SerializeField] private Button homeButton;
        [FormerlySerializedAs("secondaryButton")]
        [Tooltip("Nút chơi lại màn hiện tại.")]
        [SerializeField] private Button replayButton;
        [Tooltip("Nút đóng cài đặt để tiếp tục màn chơi.")]
        [SerializeField] private Button closeButton;
        [Tooltip("Nút và hình bật/tắt hiệu ứng âm thanh.")]
        [SerializeField] private SettingControl sound;
        [Tooltip("Nút và hình bật/tắt nhạc nền.")]
        [SerializeField] private SettingControl music;
        [Tooltip("Nút và hình bật/tắt rung.")]
        [SerializeField] private SettingControl vibration;
        private LevelController levelController;
        private CustomerManager customerManager;

        // EN: Caches shared feedback managers and binds the six settings buttons while visible.
        // VI: Cache manager phản hồi dùng chung và nối sáu nút cài đặt khi canvas đang hiện.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            CacheFeedbackManagers();
            levelController = FindFirstObjectByType<LevelController>();
            customerManager = CustomerManager.Instance;
            homeButton.onClick.AddListener(Home);
            replayButton.onClick.AddListener(Replay);
            closeButton.onClick.AddListener(Resume);
            sound.button.onClick.AddListener(ToggleSound);
            music.button.onClick.AddListener(ToggleMusic);
            vibration.button.onClick.AddListener(ToggleVibration);
            playerData.StateChanged += RefreshSettings;
            RefreshSettings();
        }

        // EN: Cancels animations, removes listeners and restores only the pause owned by this canvas.
        // VI: Hủy hoạt cảnh, gỡ listener và chỉ nhả tạm dừng do canvas này giữ.
        private void OnDisable()
        {
            ResetAnimation();
            if (Application.isPlaying)
            {
                homeButton.onClick.RemoveListener(Home);
                replayButton.onClick.RemoveListener(Replay);
                closeButton.onClick.RemoveListener(Resume);
                sound.button.onClick.RemoveListener(ToggleSound);
                music.button.onClick.RemoveListener(ToggleMusic);
                vibration.button.onClick.RemoveListener(ToggleVibration);
                playerData.StateChanged -= RefreshSettings;
            }
            ReleaseGameplayPause();
        }

        // EN: Pauses gameplay and animates the settings canvas in with unscaled time.
        // VI: Tạm dừng gameplay và chạy hiệu ứng mở canvas bằng thời gian độc lập.
        public override void Open()
        {
            PauseGameplay();
            base.Open();
            AnimateOpen();
        }

        // EN: Requests Home using the shared completed-order warning rule.
        // VI: Yêu cầu về Home theo quy tắc cảnh báo dùng chung dựa trên order hoàn tất.
        private void Home()
        {
            RequestQuit(ReturnHome);
        }

        // EN: Warns after a complete order without click audio; direct navigation retains click audio.
        // VI: Cảnh báo sau order hoàn tất không phát tiếng nhấn; điều hướng trực tiếp vẫn phát tiếng nhấn.
        private void RequestQuit(UnityEngine.Events.UnityAction action)
        {
            if (closing) return;
            PlayHaptic();
            if (!customerManager.HasCompletedOrder) PlaySoundClick();
            if (customerManager.HasCompletedOrder)
                CloseWithAnimation(() => Owner.ShowWarningQuit(action, Owner.ShowPause));
            else
                CloseWithAnimation(action);
        }

        // EN: Charges a life only after order completion, then records quit and loads Home.
        // VI: Chỉ trừ mạng khi đã hoàn tất order, sau đó ghi nhận thoát và nạp Home.
        private void ReturnHome()
        {
            if (customerManager.HasCompletedOrder)
                ProfileManager.instance.GameFeatureContext.HeartManager.UseHeart();
            levelController.BackHome();
            playerData.ResetStatePlaying();
            SceneManager.LoadScene("Home");
        }
        // EN: Requests Replay using the shared completed-order warning rule.
        // VI: Yêu cầu chơi lại theo quy tắc cảnh báo dùng chung dựa trên order hoàn tất.
        private void Replay()
        {
            RequestQuit(ConfirmReplay);
        }
        // EN: Charges and saves a life only after order completion, then reloads the current level.
        // VI: Chỉ trừ và lưu mạng khi đã hoàn tất order, sau đó nạp lại màn hiện tại.
        private void ConfirmReplay()
        {
            if (customerManager.HasCompletedOrder)
            {
                ProfileManager.instance.GameFeatureContext.HeartManager.UseHeart();
                playerData.Save();
            }
            levelController.ReplayLevel();
        }

        // EN: Plays feedback and animates closing before continuing the existing level.
        // VI: Phát phản hồi và chạy hiệu ứng đóng trước khi tiếp tục màn hiện tại.
        private void Resume()
        {
            if (closing) return;
            PlayClickFeedback();
            CloseWithAnimation(null);
        }

        // EN: Saves the sound toggle and plays feedback using the resulting preferences.
        // VI: Lưu trạng thái âm thanh và phát phản hồi theo cài đặt sau thay đổi.
        private void ToggleSound()
        {
            if (closing) return;
            playerData.SetSoundEnabled(!playerData.IsSoundOn);
            PlayClickFeedback();
        }

        // EN: Saves the music toggle and plays button feedback.
        // VI: Lưu trạng thái nhạc và phát phản hồi nhấn nút.
        private void ToggleMusic()
        {
            if (closing) return;
            playerData.SetMusicEnabled(!playerData.IsMusicOn);
            PlayClickFeedback();
        }

        // EN: Saves the vibration toggle and plays feedback using the resulting preferences.
        // VI: Lưu trạng thái rung và phát phản hồi theo cài đặt sau thay đổi.
        private void ToggleVibration()
        {
            if (closing) return;
            playerData.SetVibrationEnabled(!playerData.isVibrateOn);
            PlayClickFeedback();
        }

        // EN: Synchronizes all switch visuals with saved preferences without triggering button actions.
        // VI: Đồng bộ hình công tắc với cài đặt đã lưu mà không kích hoạt hành động nút.
        private void RefreshSettings()
        {
            ShowSetting(sound, playerData.IsSoundOn);
            ShowSetting(music, playerData.IsMusicOn);
            ShowSetting(vibration, playerData.isVibrateOn);
        }

        // EN: Shows exactly one state of an authored switch.
        // VI: Hiện đúng một trạng thái của công tắc đã thiết kế.
        private static void ShowSetting(SettingControl control, bool enabled)
        {
            control.onState.SetActive(enabled);
            control.offState.SetActive(!enabled);
        }

    }
}
