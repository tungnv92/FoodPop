using GamePackages;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: Home settings popup opened by FoodPop.UIManager: toggles sound, music and vibration and saves each change through PlayerDataManager.
    // VI: Popup cài đặt ở Home do FoodPop.UIManager mở: bật/tắt âm thanh, nhạc và rung, lưu từng thay đổi qua PlayerDataManager.
    [DisallowMultipleComponent]
    public sealed class PopupSetting : UICanvasPrime
    {
        // EN: One settings row: its button and the on/off visuals.
        // VI: Một dòng cài đặt: nút và hình trạng thái bật/tắt.
        [Serializable]
        private sealed class SettingControl
        {
            public Button button;
            public GameObject onState;
            public GameObject offState;
        }

        [Tooltip("Nút đóng popup.")]
        [SerializeField] private Button closeButton;
        [Tooltip("Dòng bật/tắt hiệu ứng âm thanh.")]
        [SerializeField] private SettingControl sound;
        [Tooltip("Dòng bật/tắt nhạc nền.")]
        [SerializeField] private SettingControl music;
        [Tooltip("Dòng bật/tắt rung.")]
        [SerializeField] private SettingControl vibration;
        [Tooltip("Nút khôi phục giao dịch, chỉ hiện trên iOS (Apple bắt buộc khi có gói mua một lần).")]
        [SerializeField] private Button restoreButton;

        // EN: Caches feedback managers, binds the close, toggle and restore buttons, follows saved-state changes and shows the current settings.
        // VI: Cache manager phản hồi, nối nút đóng, các nút bật/tắt và nút khôi phục, theo dõi thay đổi trạng thái đã lưu và hiện cài đặt hiện tại.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            CacheFeedbackManagers();
            closeButton.onClick.AddListener(OnClickClose);
            sound.button.onClick.AddListener(ToggleSound);
            music.button.onClick.AddListener(ToggleMusic);
            vibration.button.onClick.AddListener(ToggleVibration);
            restoreButton.onClick.AddListener(OnClickRestore);
            playerData.StateChanged += RefreshSettings;
            RefreshSettings();
        }

        // EN: Removes the listeners added in OnEnable.
        // VI: Gỡ các listener đã thêm trong OnEnable.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            closeButton.onClick.RemoveListener(OnClickClose);
            sound.button.onClick.RemoveListener(ToggleSound);
            music.button.onClick.RemoveListener(ToggleMusic);
            vibration.button.onClick.RemoveListener(ToggleVibration);
            restoreButton.onClick.RemoveListener(OnClickRestore);
            playerData.StateChanged -= RefreshSettings;
        }

        // EN: Activates the popup, shows the restore button only on iOS builds, and plays the shared open animation.
        // VI: Bật popup, chỉ hiện nút khôi phục khi build iOS, và chạy hiệu ứng mở dùng chung.
        public override void Open()
        {
            base.Open();
#if UNITY_IOS
            restoreButton.gameObject.SetActive(true);
#else
            restoreButton.gameObject.SetActive(false);
#endif
            AnimateOpen();
        }

        // EN: Saves the flipped sound setting, then plays haptics.
        // VI: Lưu trạng thái âm thanh đã đảo, rồi phát rung.
        private void ToggleSound()
        {
            if (closing) return;
            playerData.SetSoundEnabled(!playerData.IsSoundOn);
            PlayHaptic();
        }

        // EN: Saves the flipped music setting, then plays haptics.
        // VI: Lưu trạng thái nhạc đã đảo, rồi phát rung.
        private void ToggleMusic()
        {
            if (closing) return;
            playerData.SetMusicEnabled(!playerData.IsMusicOn);
            PlayHaptic();
        }

        // EN: Saves the flipped vibration setting, then plays haptics with the new preference.
        // VI: Lưu trạng thái rung đã đảo, rồi phát rung theo cài đặt mới.
        private void ToggleVibration()
        {
            if (closing) return;
            playerData.SetVibrationEnabled(!playerData.isVibrateOn);
            PlayHaptic();
        }

        // EN: Restores App Store purchases and reports the result with a short message.
        // VI: Khôi phục giao dịch App Store và báo kết quả bằng thông báo ngắn.
        private void OnClickRestore()
        {
            if (closing) return;
            PlayHaptic();
            IAPManager.Instance.RestorePurchases(success =>
                Owner.ShowShortMessage(success ? "Purchases restored." : "Restore failed. Please try again."));
        }

        // EN: Synchronizes every switch visual with the saved preferences.
        // VI: Đồng bộ hình mọi công tắc với cài đặt đã lưu.
        private void RefreshSettings()
        {
            ShowSetting(sound, playerData.IsSoundOn);
            ShowSetting(music, playerData.IsMusicOn);
            ShowSetting(vibration, playerData.isVibrateOn);
        }

        // EN: Shows exactly one state of a switch.
        // VI: Hiện đúng một trạng thái của công tắc.
        private static void ShowSetting(SettingControl control, bool enabled)
        {
            control.onState.SetActive(enabled);
            control.offState.SetActive(!enabled);
        }

        // EN: Closes the popup with the shared exit animation.
        // VI: Đóng popup bằng hiệu ứng thoát dùng chung.
        private void OnClickClose()
        {
            if (closing) return;
            PlayHaptic();
            CloseWithAnimation();
        }
    }
}
