using System;
using UnityEngine;

namespace FoodPop
{
    // EN: Per-booster shop, progression and intro settings (display name and description used by the unlock intro, the Coin price of one purchase and the level at which the booster unlocks), plus every short message shown to the player while using or buying a booster.
    // VI: Thiết lập cửa hàng, tiến trình và giới thiệu của từng booster (tên và mô tả hiển thị dùng cho màn giới thiệu khi mở khóa, giá Coin cho một lần mua và level bắt đầu mở khóa booster), cùng mọi thông báo ngắn hiện cho người chơi khi dùng hoặc mua booster.
    [CreateAssetMenu(fileName = "BoosterConfig", menuName = "Hot Pot/Booster Config")]
    public class BoosterConfig : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Loại booster.")]
            public BoosterType type;
            [Tooltip("Tên booster hiển thị cho người chơi, dùng ở màn giới thiệu khi mở khóa.")]
            public string boosterName;
            [Tooltip("Mô tả tác dụng của booster, dùng ở màn giới thiệu khi mở khóa.")]
            [TextArea(2, 4)] public string boosterInfo;
            [Tooltip("Giá mua một gói booster, tính bằng Coin.")]
            [Min(0)] public int price = 900;
            [Tooltip("Level bắt đầu mở khóa booster; ở các level nhỏ hơn booster bị khóa.")]
            [Min(1)] public int unlockLevel = 1;
            [Tooltip("Số lượt booster tặng khi mở khóa; cộng một lần duy nhất ở màn giới thiệu (booster mở từ level 1 không có màn giới thiệu nên không được tặng).")]
            [Min(0)] public int unlockGift = 2;
        }

        // EN: Kinds of short messages shown while using or buying a booster. Values are explicit so reordering never breaks saved data.
        // VI: Các loại thông báo ngắn hiện khi dùng hoặc mua booster. Giá trị được ghi rõ để sắp xếp lại không làm hỏng dữ liệu đã lưu.
        public enum ShortDesType
        {
            BoosterRunning = 0,
            BoardBusy = 1,
            BeltEmpty = 3,
            PickupPrompt = 4,
            NoPlateToPick = 7,
            PlateEmpty = 8,
            PlateFrozen = 9,
            LinkedPlateFrozen = 10,
            BeltFull = 11,
            LinkedPlatesNoSpace = 12,
            NotEnoughCoin = 13,
            PurchaseFailed = 14,
            TryPrompt = 15,
        }

        [Serializable]
        public class ShortDes
        {
            [Tooltip("Loại thông báo ngắn.")]
            public ShortDesType type;
            [Tooltip("Nội dung thông báo hiển thị cho người chơi. Riêng NotEnoughCoin dùng {0} để chèn giá.")]
            [TextArea(1, 3)] public string info;
        }

        [Tooltip("Tên, mô tả, giá mua và level mở khóa của từng loại booster.")]
        [SerializeField] private Entry[] boosters =
        {
            new Entry { type = BoosterType.PickupHand },
            new Entry { type = BoosterType.CleanBrush },
            new Entry { type = BoosterType.Shuffle },
            new Entry { type = BoosterType.AutoComplete },
        };
        [Tooltip("Các thông báo ngắn hiện khi dùng hoặc mua booster (loại và nội dung).")]
        [SerializeField] private ShortDes[] shortDes = Array.Empty<ShortDes>();

        // EN: Display name of the booster; falls back to the type name when left empty.
        // VI: Tên hiển thị của booster; để trống thì dùng tên loại booster.
        public string GetName(BoosterType type)
        {
            string displayName = Get(type).boosterName;
            return string.IsNullOrEmpty(displayName) ? type.ToString() : displayName;
        }

        // EN: Description of what the booster does, shown when it unlocks.
        // VI: Mô tả tác dụng của booster, hiển thị khi booster được mở khóa.
        public string GetInfo(BoosterType type)
        {
            return Get(type).boosterInfo;
        }

        // EN: Coin price of one purchase of the booster.
        // VI: Giá Coin cho một lần mua booster.
        public int GetPrice(BoosterType type)
        {
            return Get(type).price;
        }

        // EN: First level at which the booster can be used.
        // VI: Level đầu tiên dùng được booster.
        public int GetUnlockLevel(BoosterType type)
        {
            return Get(type).unlockLevel;
        }

        // EN: Number of uses given once when the booster is introduced on its unlock level.
        // VI: Số lượt tặng một lần khi booster được giới thiệu ở level mở khóa.
        public int GetUnlockGift(BoosterType type)
        {
            return Get(type).unlockGift;
        }

        // EN: True when the booster is still locked at the given level number.
        // VI: True khi booster vẫn còn khóa ở số level đã cho.
        public bool IsLocked(BoosterType type, int level)
        {
            return level < Get(type).unlockLevel;
        }

        // EN: Text of a short message; warns and returns the type name when the message is missing or empty.
        // VI: Nội dung của một thông báo ngắn; cảnh báo và trả tên loại khi thiếu hoặc để trống nội dung.
        public string GetShortDes(ShortDesType type)
        {
            ShortDes entry = Array.Find(shortDes, item => item != null && item.type == type);
            if (entry != null && !string.IsNullOrEmpty(entry.info))
                return entry.info;
            Debug.LogWarning($"[BoosterConfig] Thiếu nội dung thông báo ngắn {type}.", this);
            return type.ToString();
        }

        // EN: Finds the entry of a booster type; warns and returns default settings (price 900, unlocked from level 1) when the type is missing from the list.
        // VI: Tìm dòng thiết lập của một loại booster; cảnh báo và trả thiết lập mặc định (giá 900, mở từ level 1) khi danh sách thiếu loại đó.
        private Entry Get(BoosterType type)
        {
            Entry entry = Array.Find(boosters, item => item != null && item.type == type);
            if (entry != null)
                return entry;
            Debug.LogWarning($"[BoosterConfig] Thiếu thiết lập cho booster {type}; dùng giá và level mở khóa mặc định.", this);
            return new Entry { type = type };
        }
    }
}
