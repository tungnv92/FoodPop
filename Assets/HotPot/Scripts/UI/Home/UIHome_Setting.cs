using GamePackages;
using Lofelt.NiceVibrations;
using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: Settings button on the Home screen: opens the settings popup when tapped.
    // VI: Nút cài đặt trên màn Home: mở popup cài đặt khi bấm.
    [DisallowMultipleComponent]
    public sealed class UIHome_Setting : MonoBehaviour
    {
        [Tooltip("Nút cài đặt.")]
        [SerializeField] private Button settingButton;

        private PlayerDataManager playerData;

        // EN: Caches player data and binds the button.
        // VI: Cache dữ liệu người chơi và nối nút.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            playerData = ProfileManager.instance.PlayerDataManager;
            settingButton.onClick.AddListener(OnClickSetting);
        }

        // EN: Removes the button listener.
        // VI: Gỡ listener của nút.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            settingButton.onClick.RemoveListener(OnClickSetting);
        }

        // EN: Plays haptics and opens the settings popup.
        // VI: Phát rung và mở popup cài đặt.
        private void OnClickSetting()
        {
            if (playerData.isVibrateOn) HapticPatterns.PlayEmphasis(0.8f, 0.5f);
            UIManager.Instance.OpenUI<PopupSetting>();
        }
    }
}
