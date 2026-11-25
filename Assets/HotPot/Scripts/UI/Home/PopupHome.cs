using GamePackages;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FoodPop
{
    // EN: Full-screen Home screen: shows avatar and level (hearts and Coin are handled by UIHome_Life and UIHome_Coin), and routes the Home buttons to their popups or to gameplay. It is a base screen that stays visible under other popups.
    // VI: Màn Home toàn màn hình: hiện avatar và level (mạng và Coin do UIHome_Life và UIHome_Coin xử lý), và chuyển các nút Home tới popup tương ứng hoặc vào màn chơi. Là màn nền, luôn hiện bên dưới các popup khác.
    [DisallowMultipleComponent]
    public sealed class PopupHome : UICanvasPrime
    {
        public override bool IsScreen => true;

        [Header("Top bar")]
        [Tooltip("Ảnh avatar hiện tại của người chơi.")]
        [SerializeField] private Image avatarImage;

        [Header("Play")]
        [Tooltip("Nút Play vào màn chơi.")]
        [SerializeField] private Button playButton;
        [Tooltip("Chữ level hiện tại trên nút Play.")]
        [SerializeField] private TMP_Text levelText;

        // EN: Caches managers, binds the Play button and shows the avatar and level; the screen is placed in the Home scene, so this runs when the scene starts.
        // VI: Cache manager, nối nút Play, hiện avatar và level; màn được đặt sẵn trong scene Home nên chạy ngay khi scene bắt đầu.
        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            CacheFeedbackManagers();
            BindButtons(true);
            RefreshAvatar();
            levelText.text = $"Level {ProfileManager.instance.GetCurrentLevel()}";
        }

        // EN: Removes every listener added in OnEnable.
        // VI: Gỡ mọi listener đã thêm trong OnEnable.
        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            BindButtons(false);
        }

        // EN: Adds or removes the click handlers of every Home button.
        // VI: Thêm hoặc gỡ hàm xử lý bấm của mọi nút Home.
        private void BindButtons(bool bind)
        {
            Bind(playButton, OnClickPlay, bind);
        }

        // EN: Adds or removes one click handler.
        // VI: Thêm hoặc gỡ một hàm xử lý bấm.
        private static void Bind(Button button, UnityEngine.Events.UnityAction action, bool bind)
        {
            if (bind) button.onClick.AddListener(action);
            else button.onClick.RemoveListener(action);
        }

        // EN: Shows the player's selected avatar.
        // VI: Hiện avatar người chơi đã chọn.
        private void RefreshAvatar()
        {
            UserProfileFeature profile = UserProfileFeature.Instance;
            avatarImage.sprite = profile.GetAvatarSprite(profile.AvatarId);
        }

        // EN: Starts a level when the profile is ready and enough hearts are available; otherwise opens the buy-lives popup.
        // VI: Vào màn khi hồ sơ đã sẵn sàng và đủ mạng; nếu không thì mở popup mua mạng.
        private void OnClickPlay()
        {
            PlayHaptic();
            if (!ProfileManager.instance.IsReady) return;
            if (HeartFeature.Instance.IsEnoughHeartToPlay()) LoadSceneManager.Ins.LoadScene(SceneId.Game, () => { });
            else UIManager.Instance.OpenUI<PopupMoreLife>();
        }
    }
}
