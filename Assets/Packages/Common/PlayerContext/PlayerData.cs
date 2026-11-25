using System.Collections.Generic;
namespace GamePackages
{
    [System.Serializable]
    public class PlayerData
    {
        public bool isFirstAppOpen;
        public int currentLevel;
        public bool isPlaying;
        public ShopData shopData = new();
        public UserSettingData userSettingData = new();
        public List<ResourceGame> l_ResourceGame = new();
        // Saved state of module features, one entry per FeatureModule id.
        // Trạng thái lưu của các tính năng module, mỗi id FeatureModule một entry.
        public List<FeatureSaveEntry> features = new();
        // EN: Initializes player progress, resources and enabled user settings; shop state and feature models use their own defaults.
        // VI: Khởi tạo tiến trình, tài nguyên và cài đặt người dùng mặc định bật; trạng thái shop và model tính năng dùng mặc định riêng.
        public void OnInit()
        {
            isFirstAppOpen = true;
            currentLevel = 1;
            l_ResourceGame.Add(new ResourceGame() { type = ResourceType.Coin, value = 1000 });
            l_ResourceGame.Add(new ResourceGame() { type = ResourceType.BoosterClean, value = 0 });
            l_ResourceGame.Add(new ResourceGame() { type = ResourceType.BoosterShuffle, value = 0 });
            l_ResourceGame.Add(new ResourceGame() { type = ResourceType.BoosterPickup, value = 0 });
            l_ResourceGame.Add(new ResourceGame() { type = ResourceType.BoosterAutoComplete, value = 0 });
            userSettingData = new();
        }

    }
    [System.Serializable]
    public class ResourceGame
    {
        public ResourceType type;
        public int value;
        public string GetNormalValue()
        {
            string sValue = "";
            switch (type)
            {
                case ResourceType.BoosterClean:
                case ResourceType.BoosterShuffle:
                case ResourceType.BoosterPickup:
                case ResourceType.BoosterAutoComplete:
                    sValue = "x" + value.ToString();
                    break;
                case ResourceType.UnlimitedLives:
                    if (value >= 60)
                    {
                        int mi = value % 60;
                        sValue = (int)(value / 60) + "h" + (mi > 0 ? $"{mi}m" : "");
                    } else
                        sValue = value.ToString() + "m";
                    break;
                default:
                    sValue = value.ToString();
                    break;
            }
            return sValue;
        }
    }
    public enum ResourceType
    {
        Coin,
        UnlimitedLives,
        BoosterPickup,
        BoosterClean,
        BoosterShuffle,
        BoosterAutoComplete,
    }

}
