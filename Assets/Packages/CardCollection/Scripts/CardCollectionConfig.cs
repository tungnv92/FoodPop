using System;
using System.Collections.Generic;
using GamePackages;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GamePackages
{
    public enum CardRarity
    {
        OneStar = 1,
        TwoStar = 2,
        ThreeStar = 3,
        FourStar = 4,
    }

    public enum CardSpinSlotType
    {
        Resource = 0,
        RandomCardByRarity = 1,
    }

    [Serializable]
    public class CardRarityDropConfig
    {
        [Tooltip("Cấp độ hiếm của thẻ (1 đến 4 sao).")]
        public CardRarity rarity = CardRarity.OneStar;

        [Tooltip("Trọng số tỉ lệ rơi cơ bản cho độ hiếm này.")]
        public float dropWeight = 25f;

        [Tooltip("Số lượng vé nhận được khi thẻ trùng lặp ở độ hiếm này.")]
        public int duplicateTicketReward = 5;

        [Tooltip("Hình ảnh mặt sau (BackCard) hiển thị khi thẻ ở độ hiếm này chưa được thu thập.")]
        public Sprite backCardSprite;
    }

    [Serializable]
    public class CardCollectionCardInfo
    {
        [Tooltip("Mã định danh duy nhất của thẻ (0 đến 53).")]
        public int cardId;

        [Tooltip("Chỉ số bộ sưu tập chứa thẻ này (0 đến 5).")]
        public int collectionId;

        [Tooltip("Vị trí của thẻ trong bộ sưu tập (0 đến 8).")]
        public int cardIndexInCollection;

        [Tooltip("Tên hiển thị của thẻ.")]
        public string cardName;

        [Tooltip("Độ hiếm của thẻ (1 đến 4 sao).")]
        public CardRarity rarity = CardRarity.OneStar;

        [Tooltip("Hình ảnh hiển thị của thẻ khi đã thu thập.")]
        public Sprite cardSprite;
    }

    [Serializable]
    public class CardCollectionSetInfo
    {
        [Tooltip("Chỉ số bộ sưu tập (0 đến 5).")]
        public int collectionId;

        [Tooltip("Tên chủ đề của bộ sưu tập.")]
        public string collectionName;

        [Tooltip("Biểu tượng đại diện của bộ sưu tập.")]
        public Sprite collectionIcon;

        [Tooltip("Danh sách phần thưởng khi hoàn thành đủ 9 thẻ của bộ sưu tập.")]
        public List<ResourceGame> rewards = new();
    }

    [Serializable]
    public class CardSpinSlotInfo
    {
        [Tooltip("Chỉ số ô trên vòng quay (0 đến 11).")]
        public int slotIndex;

        [Tooltip("Loại phần thưởng của ô (Tài nguyên hoặc Thẻ ngẫu nhiên theo sao).")]
        public CardSpinSlotType slotType = CardSpinSlotType.RandomCardByRarity;

        [Tooltip("Phần thưởng tài nguyên nếu ô thuộc loại Resource.")]
        public ResourceGame resourceReward = new();

        [Tooltip("Độ hiếm của thẻ nhận được nếu ô thuộc loại RandomCardByRarity.")]
        public CardRarity cardRarity = CardRarity.OneStar;

        [Tooltip("Trọng số xác suất trúng ô này khi quay.")]
        public float weight = 10f;

        [Tooltip("Biểu tượng tùy chỉnh hiển thị trên ô quay.")]
        public Sprite customIcon;
    }

    [Serializable]
    public class CardPackShopInfo
    {
        [Tooltip("Mã sản phẩm IAP của gói thẻ.")]
        public string productId;

        [Tooltip("Tên hiển thị của gói thẻ (Basic Pack, Rare Pack, Epic Pack, Special Pack).")]
        public string packName;

        [Tooltip("Tổng số thẻ mở ra từ gói này.")]
        public int totalCardsCount = 3;

        [Tooltip("Độ hiếm tối thiểu được đảm bảo cho ít nhất 1 thẻ trong gói.")]
        public CardRarity guaranteedRarity = CardRarity.OneStar;

        [Tooltip("Số lượng thẻ đảm bảo đạt độ hiếm guaranteedRarity.")]
        public int guaranteedCount = 1;

        [Tooltip("Hình ảnh gói thẻ hiển thị trong cửa hàng.")]
        public Sprite packSprite;
    }

    [CreateAssetMenu(fileName = "CardCollectionConfig", menuName = "Scriptable Objects/CardCollectionConfig")]
    public class CardCollectionConfig : ScriptableObject
    {
        [Header("General Event Settings")]
        [Tooltip("Bật hoặc tắt sự kiện sưu tập thẻ.")]
        public bool isFeatureEnabled = true;

        [Tooltip("Cấp độ người chơi cần thắng để mở khóa sự kiện (mặc định: thắng level 40).")]
        public int unlockLevelIndex = 40;

        [Tooltip("Thời gian diễn ra sự kiện tính bằng ngày kể từ ngày mở khóa (mặc định: 14 ngày).")]
        public int eventDurationDays = 14;

        [Header("Drop Rate & Pity Algorithm")]
        [Tooltip("Cấu hình tỉ lệ rơi và số vé quy đổi khi trùng cho từng cấp sao (1-4 sao).")]
        public List<CardRarityDropConfig> rarityConfigs = new();

        [Tooltip("Số màn thắng ngay lần đầu liên tiếp không ra thẻ 3 sao+ để kích hoạt bảo hiểm 3 sao.")]
        public int pityWinsForThreeStar = 8;

        [Tooltip("Số màn thắng ngay lần đầu liên tiếp không ra thẻ 4 sao để kích hoạt bảo hiểm 4 sao.")]
        public int pityWinsForFourStar = 20;

        [Tooltip("Xác suất ưu tiên chọn thẻ chưa sở hữu trong cùng bậc sao đã quay trúng (0 đến 1).")]
        [Range(0f, 1f)]
        public float unownedCardPreferenceChance = 0.75f;

        [Header("Spin Feature Settings")]
        [Tooltip("Số vé cần thiết cho mỗi lượt quay (mặc định: 50 vé).")]
        public int spinCostTickets = 50;

        [Tooltip("Số lượt quay cần thiết để hoàn thành mốc thưởng vòng quay (mặc định: 12 lượt).")]
        public int spinSetTargetCount = 12;

        [Tooltip("Độ hiếm thẻ thưởng thêm khi đạt đủ 12/12 lượt quay.")]
        public CardRarity spinSetBonusCardRarity = CardRarity.FourStar;

        [Tooltip("Danh sách 12 ô phần thưởng trên bàn quay.")]
        public List<CardSpinSlotInfo> spinSlots = new();

        [Header("Grand Reward (54/54 Cards)")]
        [Tooltip("Danh sách phần thưởng tài nguyên khi thu thập đủ toàn bộ 54 thẻ.")]
        public List<ResourceGame> grandRewardResources = new();

        [Tooltip("ID khung avatar đặc biệt nhận được trong Grand Reward (-1 nếu không trao khung).")]
        public int grandRewardFrameId = -1;

        [Tooltip("Hình ảnh khung avatar đặc biệt hiển thị trên bảng Grand Reward.")]
        public Sprite grandRewardFrameSprite;

        [Header("Collections & Cards (6 Sets x 9 Cards = 54 Cards)")]
        [Tooltip("Danh sách 4 hình ảnh mặt sau (BackCard) tương ứng với độ hiếm từ 1 đến 4 sao.")]
        public List<Sprite> backCardSprites = new();

        [Tooltip("Danh sách 6 bộ sưu tập theo chủ đề.")]
        public List<CardCollectionSetInfo> collections = new();

        [Tooltip("Danh sách toàn bộ 54 thẻ trong sự kiện.")]
        public List<CardCollectionCardInfo> cards = new();

        [Header("IAP Card Packs Shop")]
        [Tooltip("Danh sách 4 gói thẻ bán trong cửa hàng sự kiện.")]
        public List<CardPackShopInfo> shopPacks = new();

        // EN: Ensures default event values, collections, 54 cards, spin slots, and shop packs are populated when edited in the Inspector.
        // VI: Đảm bảo các giá trị mặc định của sự kiện, 6 bộ sưu tập, 54 thẻ, 12 ô quay và 4 gói cửa hàng được khởi tạo khi chỉnh sửa trong Inspector.
        private void OnValidate()
        {
            if (rarityConfigs == null || rarityConfigs.Count == 0 ||
                collections == null || collections.Count == 0 ||
                cards == null || cards.Count == 0)
            {
                EnsureDefaultSetup();
            }
        }

        // EN: Populates default configurations for rarities, 6 collections, 54 cards, 12 spin slots, 4 IAP packs, and grand rewards.
        // VI: Khởi tạo cấu hình mặc định cho độ hiếm, 6 bộ sưu tập, 54 thẻ, 12 ô vòng quay, 4 gói IAP và phần thưởng lớn.
        [Button]
        public void EnsureDefaultSetup()
        {
            EnsureDefaultRarityConfigs();
            EnsureDefaultGrandRewards();
            EnsureDefaultCollectionsAndCards();
            EnsureDefaultSpinSlots();
            EnsureDefaultShopPacks();
        }

        // EN: Initializes the 4 rarity tiers with default drop weights and duplicate ticket conversion amounts.
        // VI: Khởi tạo 4 bậc độ hiếm với trọng số rơi mặc định và số lượng vé quy đổi khi thẻ bị trùng.
        private void EnsureDefaultRarityConfigs()
        {
            if (rarityConfigs != null && rarityConfigs.Count == 4) return;
            rarityConfigs = new List<CardRarityDropConfig>
            {
                new CardRarityDropConfig { rarity = CardRarity.OneStar, dropWeight = 65f, duplicateTicketReward = 5 },
                new CardRarityDropConfig { rarity = CardRarity.TwoStar, dropWeight = 25f, duplicateTicketReward = 10 },
                new CardRarityDropConfig { rarity = CardRarity.ThreeStar, dropWeight = 8f, duplicateTicketReward = 20 },
                new CardRarityDropConfig { rarity = CardRarity.FourStar, dropWeight = 2f, duplicateTicketReward = 50 },
            };
        }

        // EN: Initializes the default grand reward (6000 Coins and 6x SuperBear boosters) matching the event UI design.
        // VI: Khởi tạo phần thưởng lớn mặc định (6000 Xu và 6 Booster SuperBear) khớp với thiết kế giao diện sự kiện.
        private void EnsureDefaultGrandRewards()
        {
            if (grandRewardResources != null && grandRewardResources.Count > 0) return;
            grandRewardResources = new List<ResourceGame>
            {
                new ResourceGame { type = ResourceType.Coin, value = 6000 },
                new ResourceGame { type = ResourceType.BoosterSuperBear, value = 6 },
            };
        }

        // EN: Initializes the 6 themed collections and 54 cards (9 cards per collection with 1 to 4 star distribution).
        // VI: Khởi tạo 6 bộ sưu tập theo chủ đề và 54 thẻ (9 thẻ mỗi bộ sưu tập với phân bổ từ 1 đến 4 sao).
        private void EnsureDefaultCollectionsAndCards()
        {
            string[] defaultNames = { "Treats", "Pumpkins", "Home Decor", "Witchcraft", "Backyard", "Costumes" };

            if (collections == null || collections.Count != 6)
            {
                collections = new List<CardCollectionSetInfo>(6);
                for (int i = 0; i < 6; i++)
                {
                    collections.Add(new CardCollectionSetInfo
                    {
                        collectionId = i,
                        collectionName = defaultNames[i],
                        rewards = new List<ResourceGame>
                        {
                            new ResourceGame { type = ResourceType.BoosterAddSlot, value = 1 },
                            new ResourceGame { type = ResourceType.BoosterPickup, value = 1 },
                            new ResourceGame { type = ResourceType.BoosterShuffle, value = 1 },
                            new ResourceGame { type = ResourceType.BoosterSuperBear, value = 1 },
                        }
                    });
                }
            }

            if (cards == null || cards.Count != 54)
            {
                cards = new List<CardCollectionCardInfo>(54);
                CardRarity[] patternPerSet =
                {
                    CardRarity.OneStar, CardRarity.OneStar, CardRarity.OneStar,
                    CardRarity.OneStar, CardRarity.OneStar, CardRarity.OneStar,
                    CardRarity.TwoStar, CardRarity.ThreeStar, CardRarity.FourStar
                };

                int cardId = 0;
                for (int setIndex = 0; setIndex < 6; setIndex++)
                {
                    string setName = collections[setIndex].collectionName;
                    for (int idx = 0; idx < 9; idx++)
                    {
                        cards.Add(new CardCollectionCardInfo
                        {
                            cardId = cardId,
                            collectionId = setIndex,
                            cardIndexInCollection = idx,
                            cardName = $"{setName} #{idx + 1}",
                            rarity = patternPerSet[idx],
                        });
                        cardId++;
                    }
                }
            }
        }

        // EN: Initializes the 12 spin wheel slots matching the spin board layout (coins, boosters, and 1-4 star cards).
        // VI: Khởi tạo 12 ô trên bàn quay khớp với bố cục giao diện vòng quay (xu, booster và thẻ 1-4 sao).
        private void EnsureDefaultSpinSlots()
        {
            if (spinSlots != null && spinSlots.Count == 12) return;
            spinSlots = new List<CardSpinSlotInfo>
            {
                new CardSpinSlotInfo
                {
                    slotIndex = 0,
                    slotType = CardSpinSlotType.Resource,
                    resourceReward = new ResourceGame { type = ResourceType.Coin, value = 1000 },
                    weight = 8f
                },
                new CardSpinSlotInfo
                {
                    slotIndex = 1,
                    slotType = CardSpinSlotType.RandomCardByRarity,
                    cardRarity = CardRarity.OneStar,
                    weight = 14f
                },
                new CardSpinSlotInfo
                {
                    slotIndex = 2,
                    slotType = CardSpinSlotType.RandomCardByRarity,
                    cardRarity = CardRarity.ThreeStar,
                    weight = 5f
                },
                new CardSpinSlotInfo
                {
                    slotIndex = 3,
                    slotType = CardSpinSlotType.Resource,
                    resourceReward = new ResourceGame { type = ResourceType.BoosterAddSlot, value = 1 },
                    weight = 8f
                },
                new CardSpinSlotInfo
                {
                    slotIndex = 4,
                    slotType = CardSpinSlotType.RandomCardByRarity,
                    cardRarity = CardRarity.OneStar,
                    weight = 14f
                },
                new CardSpinSlotInfo
                {
                    slotIndex = 5,
                    slotType = CardSpinSlotType.RandomCardByRarity,
                    cardRarity = CardRarity.TwoStar,
                    weight = 10f
                },
                new CardSpinSlotInfo
                {
                    slotIndex = 6,
                    slotType = CardSpinSlotType.RandomCardByRarity,
                    cardRarity = CardRarity.FourStar,
                    weight = 2f
                },
                new CardSpinSlotInfo
                {
                    slotIndex = 7,
                    slotType = CardSpinSlotType.RandomCardByRarity,
                    cardRarity = CardRarity.TwoStar,
                    weight = 10f
                },
                new CardSpinSlotInfo
                {
                    slotIndex = 8,
                    slotType = CardSpinSlotType.RandomCardByRarity,
                    cardRarity = CardRarity.ThreeStar,
                    weight = 5f
                },
                new CardSpinSlotInfo
                {
                    slotIndex = 9,
                    slotType = CardSpinSlotType.Resource,
                    resourceReward = new ResourceGame { type = ResourceType.BoosterPickup, value = 1 },
                    weight = 8f
                },
                new CardSpinSlotInfo
                {
                    slotIndex = 10,
                    slotType = CardSpinSlotType.RandomCardByRarity,
                    cardRarity = CardRarity.OneStar,
                    weight = 14f
                },
                new CardSpinSlotInfo
                {
                    slotIndex = 11,
                    slotType = CardSpinSlotType.RandomCardByRarity,
                    cardRarity = CardRarity.TwoStar,
                    weight = 10f
                },
            };
        }

        // EN: Initializes the 4 IAP card packs (Basic, Rare, Epic, Special) for the Card Pack Shop popup.
        // VI: Khởi tạo 4 gói thẻ IAP (Basic, Rare, Epic, Special) cho popup cửa hàng gói thẻ.
        private void EnsureDefaultShopPacks()
        {
            if (shopPacks != null && shopPacks.Count == 4) return;
            shopPacks = new List<CardPackShopInfo>
            {
                new CardPackShopInfo
                {
                    productId = "card_pack_basic",
                    packName = "Basic Pack",
                    totalCardsCount = 3,
                    guaranteedRarity = CardRarity.OneStar,
                    guaranteedCount = 3
                },
                new CardPackShopInfo
                {
                    productId = "card_pack_rare",
                    packName = "Rare Pack",
                    totalCardsCount = 4,
                    guaranteedRarity = CardRarity.TwoStar,
                    guaranteedCount = 2
                },
                new CardPackShopInfo
                {
                    productId = "card_pack_epic",
                    packName = "Epic Pack",
                    totalCardsCount = 5,
                    guaranteedRarity = CardRarity.ThreeStar,
                    guaranteedCount = 2
                },
                new CardPackShopInfo
                {
                    productId = "card_pack_special",
                    packName = "Special Pack",
                    totalCardsCount = 6,
                    guaranteedRarity = CardRarity.FourStar,
                    guaranteedCount = 1
                },
            };
        }

        // EN: Retrieves the drop configuration for the specified card rarity.
        // VI: Lấy thông tin cấu hình tỉ lệ rơi và vé quy đổi cho độ hiếm thẻ chỉ định.
        public CardRarityDropConfig GetRarityConfig(CardRarity rarity)
        {
            EnsureDefaultRarityConfigs();
            for (int i = 0; i < rarityConfigs.Count; i++)
            {
                if (rarityConfigs[i].rarity == rarity)
                {
                    return rarityConfigs[i];
                }
            }
            return rarityConfigs[0];
        }

        // EN: Retrieves the BackCard sprite corresponding to the specified card rarity (1 to 4 stars).
        // VI: Lấy hình ảnh mặt sau (BackCard) tương ứng với độ hiếm của thẻ (từ 1 đến 4 sao).
        public Sprite GetBackCardSprite(CardRarity rarity)
        {
            int index = (int)rarity - 1;
            if (backCardSprites != null && index >= 0 && index < backCardSprites.Count && backCardSprites[index] != null)
            {
                return backCardSprites[index];
            }

            CardRarityDropConfig rarityConfig = GetRarityConfig(rarity);
            return rarityConfig != null ? rarityConfig.backCardSprite : null;
        }

        // EN: Retrieves the card definition by card ID (0 to 53).
        // VI: Lấy thông tin định nghĩa thẻ theo mã cardId (0 đến 53).
        public CardCollectionCardInfo GetCardInfo(int cardId)
        {
            EnsureDefaultCollectionsAndCards();
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i].cardId == cardId)
                {
                    return cards[i];
                }
            }
            return null;
        }

        // EN: Retrieves the collection set definition by collection ID (0 to 5).
        // VI: Lấy thông tin bộ sưu tập theo mã collectionId (0 đến 5).
        public CardCollectionSetInfo GetCollectionInfo(int collectionId)
        {
            EnsureDefaultCollectionsAndCards();
            for (int i = 0; i < collections.Count; i++)
            {
                if (collections[i].collectionId == collectionId)
                {
                    return collections[i];
                }
            }
            return null;
        }
    }
}

