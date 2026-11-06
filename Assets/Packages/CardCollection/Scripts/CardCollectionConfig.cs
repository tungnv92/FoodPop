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
        SpecificCard = 2,
    }

    [Serializable]
    public class CardSetGachaParam
    {
        [Tooltip("Chỉ số bộ sưu tập (0: Treats, 1: Pumpkins, 2: Home Decor, 3: Witchcraft, 4: Backyard, 5: Costumes).")]
        public int collectionId;

        [Tooltip("Trọng số gốc của bộ sưu tập trong NormalGachaRule.")]
        public float baseWeight = 10f;

        [Tooltip("Hệ số tăng tỉ lệ giai đoạn đầu khi bộ sưu tập còn ít thẻ (Early boost).")]
        public float earlyStrength;

        [Tooltip("Hệ số tăng tỉ lệ giai đoạn cuối khi bộ sưu tập gần hoàn thành (Finish boost).")]
        public float finishStrength = 1f;

        [Tooltip("Số mũ đường cong của Finish boost (mặc định: 2.0).")]
        public float finishCurve = 2f;

        [Tooltip("Hệ số cộng thêm cho mỗi lượt liên tiếp không mở ra thẻ mới của bộ sưu tập này (Miss boost/lượt).")]
        public float missPerDraw = 0.1f;

        [Tooltip("Hệ số nhân trọng số ưu tiên cho thẻ chưa sở hữu trong bộ sưu tập (Thẻ mới x).")]
        public float newCardMultiplier = 2f;

        [Tooltip("Số lượt không ra thẻ mới tối đa trước khi ép bảo đảm (hard guarantee) ra thẻ mới của bộ này (0 = tắt).")]
        public int guaranteeDraws;
    }

    [Serializable]
    public class CardGachaSlotRule
    {
        [Tooltip("Tên quy tắc bốc thẻ (ví dụ: NormalGachaRule, BasicGachaRule_1).")]
        public string ruleName;

        [Tooltip("Trọng số tỉ lệ ra thẻ 1 sao (%).")]
        public float weight1Star = 60f;

        [Tooltip("Trọng số tỉ lệ ra thẻ 2 sao (%).")]
        public float weight2Star = 30f;

        [Tooltip("Trọng số tỉ lệ ra thẻ 3 sao (%).")]
        public float weight3Star = 10f;

        [Tooltip("Trọng số tỉ lệ ra thẻ 4 sao (%).")]
        public float weight4Star;

        [Tooltip("Sử dụng bảng thông số chi tiết từng set của NormalGachaRule (Early, Finish, Miss, Pity).")]
        public bool useNormalSetParams;

        [Tooltip("Có kích hoạt bảo hiểm ép ra thẻ mới theo số lượt (hard guarantee) hay không.")]
        public bool enableHardGuarantee;

        [Tooltip("Trọng số cơ bản của 6 bộ sưu tập (Treats..Costumes) khi không dùng NormalSetParams.")]
        public float[] setBaseWeights = { 100f, 100f, 100f, 100f, 100f, 100f };

        [Tooltip("Hệ số Finish boost áp dụng cho các bộ sưu tập trong quy tắc này.")]
        public float finishStrength;

        [Tooltip("Số mũ đường cong Finish boost (mặc định: 2.0).")]
        public float finishCurve = 2f;

        [Tooltip("Hệ số nhân trọng số cho thẻ mới chưa sở hữu trong quy tắc này (mặc định: 1.0 = không boost).")]
        public float newCardMultiplier = 1f;

        // EN: Returns the configured drop weight for the given card rarity in this rule.
        // VI: Trả về trọng số tỉ lệ rơi được cấu hình cho độ hiếm thẻ tương ứng trong quy tắc này.
        public float GetRarityWeight(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.OneStar => Mathf.Max(0f, weight1Star),
                CardRarity.TwoStar => Mathf.Max(0f, weight2Star),
                CardRarity.ThreeStar => Mathf.Max(0f, weight3Star),
                CardRarity.FourStar => Mathf.Max(0f, weight4Star),
                _ => 0f,
            };
        }
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

        [Tooltip("Nhóm ưu tiên quay (Sub-pool 1 đến 5); vòng quay luôn chọn từ sub-pool nhỏ nhất còn trống trên bàn.")]
        public int subPool = 1;

        [Tooltip("Loại phần thưởng của ô (Tài nguyên, Thẻ ngẫu nhiên theo sao hoặc Thẻ cố định theo ID).")]
        public CardSpinSlotType slotType = CardSpinSlotType.RandomCardByRarity;

        [Tooltip("Phần thưởng tài nguyên nếu ô thuộc loại Resource.")]
        public ResourceGame resourceReward = new();

        [Tooltip("Độ hiếm của thẻ nhận được nếu ô là thẻ.")]
        public CardRarity cardRarity = CardRarity.OneStar;

        [Tooltip("Mã thẻ cố định (0 đến 53, tương ứng #1 đến #54 trong bảng thiết kế; -1 nếu bốc ngẫu nhiên theo sao).")]
        public int specificCardId = -1;

        [Tooltip("Trọng số xác suất trúng ô này trong cùng một Sub-pool.")]
        public float weight = 10f;

        [Tooltip("Biểu tượng tùy chỉnh hiển thị trên ô quay.")]
        public Sprite customIcon;
    }

    [Serializable]
    public class CardSpinBoardInfo
    {
        [Tooltip("Chỉ số bảng vòng quay (0 đến 3, tương ứng Bảng 1 đến Bảng 4).")]
        public int boardIndex;

        [Tooltip("Tên hiển thị/ghi chú của bảng vòng quay.")]
        public string boardName;

        [Tooltip("Chỉ số bộ sưu tập mục tiêu của bảng (-1 nếu là bảng tài nguyên lặp lại).")]
        public int targetCollectionId = -1;

        [Tooltip("Nếu bật, bảng này sẽ tự động làm mới lại khi đã nhận hết cả 12 ô (áp dụng cho Bảng 4).")]
        public bool isRepeating;

        [Tooltip("Danh sách 12 ô phần thưởng của bảng vòng quay này.")]
        public List<CardSpinSlotInfo> slots = new();
    }

    [Serializable]
    public class CardPackShopInfo
    {
        [Tooltip("Mã sản phẩm IAP của gói thẻ.")]
        public string productId;

        [Tooltip("Tên hiển thị của gói thẻ (Basic Pack, Rare Pack, Epic Pack, Special Pack).")]
        public string packName;

        [Tooltip("Tổng số thẻ mở ra từ gói này (Basic=2, Rare=3, Epic=4, Special=5).")]
        public int totalCardsCount = 2;

        [Tooltip("Độ hiếm tối thiểu được đảm bảo cho thẻ đầu tiên trong gói.")]
        public CardRarity guaranteedRarity = CardRarity.OneStar;

        [Tooltip("Số lượng thẻ đảm bảo đạt độ hiếm guaranteedRarity.")]
        public int guaranteedCount = 1;

        [Tooltip("Hình ảnh gói thẻ hiển thị trong cửa hàng.")]
        public Sprite packSprite;

        [Tooltip("Danh sách quy tắc bốc thẻ cho từng ô thẻ trong gói (Slot 1..N).")]
        public List<CardGachaSlotRule> slotRules = new();
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

        [Header("Drop Rate & Pity Algorithm (NormalGachaRule)")]
        [Tooltip("Cấu hình tỉ lệ rơi và số vé quy đổi khi trùng cho từng cấp sao (1★=10, 2★=30, 3★=80, 4★=200).")]
        public List<CardRarityDropConfig> rarityConfigs = new();

        [Tooltip("Quy tắc gacha mặc định khi rơi thẻ từ màn chơi (NormalGachaRule: 1★ 60%, 2★ 30%, 3★ 10%, 4★ 0%).")]
        public CardGachaSlotRule normalGachaRule = new();

        [Tooltip("Thông số trọng số, Early/Finish/Miss boost và Pity cho từng bộ sưu tập trong NormalGachaRule.")]
        public List<CardSetGachaParam> normalGachaSetParams = new();

        [Tooltip("Số màn thắng ngay lần đầu liên tiếp không ra thẻ 3 sao+ để kích hoạt bảo hiểm 3 sao (legacy).")]
        public int pityWinsForThreeStar = 8;

        [Tooltip("Số màn thắng ngay lần đầu liên tiếp không ra thẻ 4 sao để kích hoạt bảo hiểm 4 sao (legacy).")]
        public int pityWinsForFourStar = 20;

        [Tooltip("Xác suất ưu tiên chọn thẻ chưa sở hữu trong cùng bậc sao đã quay trúng (0 đến 1).")]
        [Range(0f, 1f)]
        public float unownedCardPreferenceChance = 0.75f;

        [Header("Spin Feature Settings (Lucky Spin)")]
        [Tooltip("Số vé cơ bản cho lượt quay đầu tiên của bảng (mặc định: 50 vé).")]
        public int spinCostTickets = 50;

        [Tooltip("Giá vé tăng dần cho từng lượt quay từ lượt 1 đến lượt 12 trên mỗi bảng (50 -> 600, tổng 3900).")]
        public List<int> spinStepCosts = new();

        [Tooltip("Số lượt quay cần thiết để hoàn thành một bảng vòng quay (mặc định: 12 lượt).")]
        public int spinSetTargetCount = 12;

        [Tooltip("Độ hiếm thẻ thưởng ở mốc cuối của bảng vòng quay.")]
        public CardRarity spinSetBonusCardRarity = CardRarity.FourStar;

        [Tooltip("Danh sách 12 ô phần thưởng trên bàn quay mặc định (Bảng 1) và chứa icon mẫu cho thẻ 1-4 sao.")]
        public List<CardSpinSlotInfo> spinSlots = new();

        [Tooltip("Danh sách 4 bảng phần thưởng Lucky Spin (Bảng 1: Witchcraft, Bảng 2: Backyard, Bảng 3: Costumes, Bảng 4: Lặp tài nguyên).")]
        public List<CardSpinBoardInfo> spinBoards = new();

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

        // EN: Ensures default event values, gacha rules, collections, 54 cards, 4 spin boards, and shop packs are populated when edited in the Inspector.
        // VI: Đảm bảo các giá trị mặc định của sự kiện, quy tắc gacha, 6 bộ sưu tập, 54 thẻ, 4 bảng vòng quay và 4 gói cửa hàng được khởi tạo khi chỉnh sửa trong Inspector.
        private void OnValidate()
        {
            if (rarityConfigs == null || rarityConfigs.Count == 0 ||
                collections == null || collections.Count == 0 ||
                cards == null || cards.Count == 0 ||
                normalGachaSetParams == null || normalGachaSetParams.Count == 0 ||
                spinBoards == null || spinBoards.Count == 0)
            {
                EnsureDefaultSetup();
            }
        }

        // EN: Populates default configurations for rarities, NormalGachaRule, 6 collections, 54 cards, 4 Lucky Spin boards, 4 IAP packs, and grand rewards.
        // VI: Khởi tạo cấu hình mặc định cho độ hiếm, NormalGachaRule, 6 bộ sưu tập, 54 thẻ, 4 bảng Lucky Spin, 4 gói IAP và phần thưởng lớn.
        [Button]
        public void EnsureDefaultSetup()
        {
            EnsureDefaultRarityConfigs();
            EnsureDefaultNormalGachaRule();
            EnsureDefaultGrandRewards();
            EnsureDefaultCollectionsAndCards();
            EnsureDefaultSpinSlots();
            EnsureDefaultSpinBoards();
            EnsureDefaultShopPacks();
        }

        // EN: Initializes the 4 rarity tiers with NormalGachaRule drop weights (60/30/10/0) and duplicate token conversion amounts (10/30/80/200).
        // VI: Khởi tạo 4 bậc độ hiếm với trọng số rơi NormalGachaRule (60/30/10/0) và số lượng token quy đổi khi thẻ bị trùng (10/30/80/200).
        private void EnsureDefaultRarityConfigs()
        {
            if (rarityConfigs != null && rarityConfigs.Count == 4) return;
            rarityConfigs = new List<CardRarityDropConfig>
            {
                new CardRarityDropConfig { rarity = CardRarity.OneStar, dropWeight = 60f, duplicateTicketReward = 10 },
                new CardRarityDropConfig { rarity = CardRarity.TwoStar, dropWeight = 30f, duplicateTicketReward = 30 },
                new CardRarityDropConfig { rarity = CardRarity.ThreeStar, dropWeight = 10f, duplicateTicketReward = 80 },
                new CardRarityDropConfig { rarity = CardRarity.FourStar, dropWeight = 0f, duplicateTicketReward = 200 },
            };
        }

        // EN: Initializes the NormalGachaRule and per-set gacha parameters (base weight, early/finish/miss boost, new card multiplier, and hard guarantee N) per specification.
        // VI: Khởi tạo NormalGachaRule và thông số gacha cho từng bộ sưu tập (trọng số gốc, early/finish/miss boost, hệ số thẻ mới và bảo hiểm N lượt) theo tài liệu thiết kế.
        private void EnsureDefaultNormalGachaRule()
        {
            if (normalGachaRule == null || string.IsNullOrEmpty(normalGachaRule.ruleName))
            {
                normalGachaRule = new CardGachaSlotRule
                {
                    ruleName = "NormalGachaRule",
                    weight1Star = 60f,
                    weight2Star = 30f,
                    weight3Star = 10f,
                    weight4Star = 0f,
                    useNormalSetParams = true,
                    enableHardGuarantee = true,
                    setBaseWeights = new[] { 40f, 24f, 16f, 5f, 3f, 2f },
                    finishStrength = 1f,
                    finishCurve = 2f,
                    newCardMultiplier = 2f,
                };
            }

            if (normalGachaSetParams != null && normalGachaSetParams.Count == 6) return;
            normalGachaSetParams = new List<CardSetGachaParam>(6)
            {
                new CardSetGachaParam { collectionId = 0, baseWeight = 40f, earlyStrength = 2.0f, finishStrength = 1.0f, finishCurve = 2.0f, missPerDraw = 0.10f, newCardMultiplier = 2.0f, guaranteeDraws = 1 },
                new CardSetGachaParam { collectionId = 1, baseWeight = 24f, earlyStrength = 1.0f, finishStrength = 1.0f, finishCurve = 2.0f, missPerDraw = 0.10f, newCardMultiplier = 2.0f, guaranteeDraws = 2 },
                new CardSetGachaParam { collectionId = 2, baseWeight = 16f, earlyStrength = 0.0f, finishStrength = 1.0f, finishCurve = 2.0f, missPerDraw = 0.10f, newCardMultiplier = 2.0f, guaranteeDraws = 35 },
                new CardSetGachaParam { collectionId = 3, baseWeight = 5f,  earlyStrength = 0.0f, finishStrength = 0.5f, finishCurve = 2.0f, missPerDraw = 0.03f, newCardMultiplier = 1.5f, guaranteeDraws = 40 },
                new CardSetGachaParam { collectionId = 4, baseWeight = 3f,  earlyStrength = 0.0f, finishStrength = 1.0f, finishCurve = 2.0f, missPerDraw = 0.10f, newCardMultiplier = 2.0f, guaranteeDraws = 30 },
                new CardSetGachaParam { collectionId = 5, baseWeight = 2f,  earlyStrength = 0.0f, finishStrength = 0.5f, finishCurve = 2.0f, missPerDraw = 0.03f, newCardMultiplier = 1.5f, guaranteeDraws = 45 },
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

        // EN: Returns the rarity distribution for the 9 cards in the specified collection index (0 to 5) per Sheet 2 specification.
        // VI: Trả về cấu trúc phân bổ độ hiếm của 9 thẻ trong bộ sưu tập chỉ định (0 đến 5) theo đúng Sheet 2 của tài liệu thiết kế.
        public static CardRarity GetDefaultCardRarityInSet(int setIndex, int cardIdxInSet)
        {
            return setIndex switch
            {
                // Set 0 (Treats): 6x 1★, 3x 2★
                0 => cardIdxInSet < 6 ? CardRarity.OneStar : CardRarity.TwoStar,
                // Set 1 (Pumpkins): 3x 1★, 6x 2★
                1 => cardIdxInSet < 3 ? CardRarity.OneStar : CardRarity.TwoStar,
                // Set 2 (Home Decor): 6x 1★, 0x 2★, 3x 3★
                2 => cardIdxInSet < 6 ? CardRarity.OneStar : CardRarity.ThreeStar,
                // Sets 3, 4, 5 (Witchcraft, Backyard, Costumes): 3x 1★, 3x 2★, 2x 3★, 1x 4★
                _ => cardIdxInSet < 3
                    ? CardRarity.OneStar
                    : (cardIdxInSet < 6 ? CardRarity.TwoStar : (cardIdxInSet < 8 ? CardRarity.ThreeStar : CardRarity.FourStar)),
            };
        }

        // EN: Initializes the 6 themed collections and 54 cards (9 cards per collection with exact per-set 1-4 star distribution).
        // VI: Khởi tạo 6 bộ sưu tập theo chủ đề và 54 thẻ (9 thẻ mỗi bộ sưu tập với phân bổ 1-4 sao theo từng bộ).
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
                            rarity = GetDefaultCardRarityInSet(setIndex, idx),
                        });
                        cardId++;
                    }
                }
            }
        }

        // EN: Initializes the default 12 spin wheel slots (Board 1: Witchcraft) and escalating spin step costs (50 -> 600).
        // VI: Khởi tạo 12 ô vòng quay mặc định (Bảng 1: Witchcraft) và bảng giá vé tăng dần theo lượt (50 -> 600).
        private void EnsureDefaultSpinSlots()
        {
            if (spinStepCosts == null || spinStepCosts.Count != 12)
            {
                spinStepCosts = new List<int> { 50, 100, 150, 200, 250, 300, 350, 400, 450, 500, 550, 600 };
            }

            if (spinSlots != null && spinSlots.Count == 12) return;
            spinSlots = CreateDefaultSpinBoard1Slots();
        }

        // EN: Initializes the 4 Lucky Spin boards (Board 1: Witchcraft, Board 2: Backyard, Board 3: Costumes, Board 4: Repeating Resources) per Sheet 6.
        // VI: Khởi tạo 4 bảng phần thưởng Lucky Spin (Bảng 1: Witchcraft, Bảng 2: Backyard, Bảng 3: Costumes, Bảng 4: Tài nguyên lặp) theo Sheet 6.
        private void EnsureDefaultSpinBoards()
        {
            if (spinBoards != null && spinBoards.Count == 4) return;
            spinBoards = new List<CardSpinBoardInfo>(4)
            {
                new CardSpinBoardInfo
                {
                    boardIndex = 0,
                    boardName = "Board 1 - Witchcraft",
                    targetCollectionId = 3,
                    isRepeating = false,
                    slots = CreateDefaultSpinBoard1Slots(),
                },
                new CardSpinBoardInfo
                {
                    boardIndex = 1,
                    boardName = "Board 2 - Backyard",
                    targetCollectionId = 4,
                    isRepeating = false,
                    slots = CreateDefaultSpinBoard2Slots(),
                },
                new CardSpinBoardInfo
                {
                    boardIndex = 2,
                    boardName = "Board 3 - Costumes",
                    targetCollectionId = 5,
                    isRepeating = false,
                    slots = CreateDefaultSpinBoard3Slots(),
                },
                new CardSpinBoardInfo
                {
                    boardIndex = 3,
                    boardName = "Board 4 - Resource Pool (Repeat)",
                    targetCollectionId = -1,
                    isRepeating = true,
                    slots = CreateDefaultSpinBoard4Slots(),
                },
            };
        }

        // EN: Creates the 12 slots for Lucky Spin Board 1 (Set 4: Witchcraft cards #28..#36, 0-based cardId 27..35) with sub-pools 1..5.
        // VI: Tạo 12 ô phần thưởng cho Bảng 1 Lucky Spin (Bộ 4: Witchcraft thẻ #28..#36, cardId 27..35) với sub-pool 1..5.
        private List<CardSpinSlotInfo> CreateDefaultSpinBoard1Slots()
        {
            return new List<CardSpinSlotInfo>
            {
                new CardSpinSlotInfo { slotIndex = 0,  subPool = 3, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.Coin, value = 1000 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 1,  subPool = 1, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.OneStar,   specificCardId = 27, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 2,  subPool = 4, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.ThreeStar, specificCardId = 33, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 3,  subPool = 1, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.BoosterAddSlot, value = 1 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 4,  subPool = 1, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.OneStar,   specificCardId = 28, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 5,  subPool = 2, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.TwoStar,   specificCardId = 30, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 6,  subPool = 5, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.FourStar,  specificCardId = 35, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 7,  subPool = 2, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.TwoStar,   specificCardId = 31, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 8,  subPool = 4, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.ThreeStar, specificCardId = 34, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 9,  subPool = 1, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.BoosterPickup, value = 1 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 10, subPool = 1, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.OneStar,   specificCardId = 29, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 11, subPool = 2, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.TwoStar,   specificCardId = 32, weight = 10f },
            };
        }

        // EN: Creates the 12 slots for Lucky Spin Board 2 (Set 5: Backyard cards #37..#45, 0-based cardId 36..44) with sub-pools 1..5.
        // VI: Tạo 12 ô phần thưởng cho Bảng 2 Lucky Spin (Bộ 5: Backyard thẻ #37..#45, cardId 36..44) với sub-pool 1..5.
        private List<CardSpinSlotInfo> CreateDefaultSpinBoard2Slots()
        {
            return new List<CardSpinSlotInfo>
            {
                new CardSpinSlotInfo { slotIndex = 0,  subPool = 3, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.Coin, value = 1500 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 1,  subPool = 1, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.OneStar,   specificCardId = 36, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 2,  subPool = 2, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.TwoStar,   specificCardId = 41, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 3,  subPool = 1, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.BoosterShuffle, value = 1 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 4,  subPool = 1, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.OneStar,   specificCardId = 37, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 5,  subPool = 2, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.TwoStar,   specificCardId = 39, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 6,  subPool = 1, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.BoosterPickup, value = 2 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 7,  subPool = 4, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.ThreeStar, specificCardId = 42, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 8,  subPool = 2, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.TwoStar,   specificCardId = 40, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 9,  subPool = 5, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.FourStar,  specificCardId = 44, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 10, subPool = 1, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.OneStar,   specificCardId = 38, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 11, subPool = 4, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.ThreeStar, specificCardId = 43, weight = 10f },
            };
        }

        // EN: Creates the 12 slots for Lucky Spin Board 3 (Set 6: Costumes cards #46..#54, 0-based cardId 45..53) with sub-pools 1..5.
        // VI: Tạo 12 ô phần thưởng cho Bảng 3 Lucky Spin (Bộ 6: Costumes thẻ #46..#54, cardId 45..53) với sub-pool 1..5.
        private List<CardSpinSlotInfo> CreateDefaultSpinBoard3Slots()
        {
            return new List<CardSpinSlotInfo>
            {
                new CardSpinSlotInfo { slotIndex = 0,  subPool = 1, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.BoosterSuperBear, value = 2 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 1,  subPool = 1, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.OneStar,   specificCardId = 45, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 2,  subPool = 4, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.ThreeStar, specificCardId = 51, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 3,  subPool = 5, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.FourStar,  specificCardId = 53, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 4,  subPool = 1, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.OneStar,   specificCardId = 46, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 5,  subPool = 2, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.TwoStar,   specificCardId = 49, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 6,  subPool = 1, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.BoosterShuffle, value = 2 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 7,  subPool = 2, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.TwoStar,   specificCardId = 48, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 8,  subPool = 4, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.ThreeStar, specificCardId = 52, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 9,  subPool = 3, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.Coin, value = 2000 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 10, subPool = 1, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.OneStar,   specificCardId = 47, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 11, subPool = 2, slotType = CardSpinSlotType.SpecificCard, cardRarity = CardRarity.TwoStar,   specificCardId = 50, weight = 10f },
            };
        }

        // EN: Creates the 12 slots for Lucky Spin Board 4 (repeating resource pool) with sub-pools 1..2.
        // VI: Tạo 12 ô phần thưởng cho Bảng 4 Lucky Spin (bảng tài nguyên lặp lại) với sub-pool 1..2.
        private List<CardSpinSlotInfo> CreateDefaultSpinBoard4Slots()
        {
            return new List<CardSpinSlotInfo>
            {
                new CardSpinSlotInfo { slotIndex = 0,  subPool = 2, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.BoosterSuperBear, value = 3 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 1,  subPool = 1, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.BoosterAddSlot, value = 1 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 2,  subPool = 1, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.BoosterPickup, value = 1 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 3,  subPool = 2, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.Coin, value = 3000 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 4,  subPool = 1, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.BoosterShuffle, value = 2 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 5,  subPool = 1, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.Coin, value = 900 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 6,  subPool = 2, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.BoosterSuperBear, value = 3 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 7,  subPool = 1, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.Coin, value = 600 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 8,  subPool = 1, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.BoosterSuperBear, value = 1 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 9,  subPool = 2, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.Coin, value = 1500 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 10, subPool = 1, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.BoosterPickup, value = 2 }, weight = 10f },
                new CardSpinSlotInfo { slotIndex = 11, subPool = 1, slotType = CardSpinSlotType.Resource, resourceReward = new ResourceGame { type = ResourceType.Coin, value = 300 }, weight = 10f },
            };
        }

        // EN: Initializes the 4 IAP card packs (Basic: 2 cards, Rare: 3 cards, Epic: 4 cards, Special: 5 cards) and their per-slot gacha rules per Sheet 4.
        // VI: Khởi tạo 4 gói thẻ IAP (Basic: 2 thẻ, Rare: 3 thẻ, Epic: 4 thẻ, Special: 5 thẻ) cùng quy tắc gacha từng ô thẻ theo Sheet 4.
        private void EnsureDefaultShopPacks()
        {
            if (shopPacks != null && shopPacks.Count == 4 && shopPacks[0].slotRules != null && shopPacks[0].slotRules.Count > 0) return;

            Sprite basicSprite = (shopPacks != null && shopPacks.Count > 0) ? shopPacks[0].packSprite : null;
            Sprite rareSprite = (shopPacks != null && shopPacks.Count > 1) ? shopPacks[1].packSprite : null;
            Sprite epicSprite = (shopPacks != null && shopPacks.Count > 2) ? shopPacks[2].packSprite : null;
            Sprite specialSprite = (shopPacks != null && shopPacks.Count > 3) ? shopPacks[3].packSprite : null;

            shopPacks = new List<CardPackShopInfo>
            {
                new CardPackShopInfo
                {
                    productId = "card_pack_basic",
                    packName = "Basic Pack",
                    totalCardsCount = 2,
                    guaranteedRarity = CardRarity.OneStar,
                    guaranteedCount = 1,
                    packSprite = basicSprite,
                    slotRules = new List<CardGachaSlotRule>
                    {
                        new CardGachaSlotRule
                        {
                            ruleName = "BasicGachaRule_1",
                            weight1Star = 100f, weight2Star = 0f, weight3Star = 0f, weight4Star = 0f,
                            setBaseWeights = new[] { 250f, 210f, 180f, 140f, 120f, 100f },
                            finishStrength = 0f, finishCurve = 2f, newCardMultiplier = 1f,
                        },
                        new CardGachaSlotRule
                        {
                            ruleName = "BasicGachaRule_2",
                            weight1Star = 57f, weight2Star = 29f, weight3Star = 14f, weight4Star = 0f,
                            setBaseWeights = new[] { 230f, 230f, 180f, 120f, 120f, 120f },
                            finishStrength = 0.5f, finishCurve = 2f, newCardMultiplier = 1.5f,
                        },
                    }
                },
                new CardPackShopInfo
                {
                    productId = "card_pack_rare",
                    packName = "Rare Pack",
                    totalCardsCount = 3,
                    guaranteedRarity = CardRarity.TwoStar,
                    guaranteedCount = 1,
                    packSprite = rareSprite,
                    slotRules = new List<CardGachaSlotRule>
                    {
                        new CardGachaSlotRule
                        {
                            ruleName = "RareGachaRule_1",
                            weight1Star = 0f, weight2Star = 100f, weight3Star = 0f, weight4Star = 0f,
                            setBaseWeights = new[] { 140f, 160f, 200f, 180f, 170f, 150f },
                            finishStrength = 0f, finishCurve = 2f, newCardMultiplier = 1f,
                        },
                        new CardGachaSlotRule
                        {
                            ruleName = "RareGachaRule_2_3",
                            weight1Star = 45f, weight2Star = 29f, weight3Star = 20.9f, weight4Star = 5.0f,
                            setBaseWeights = new[] { 140f, 160f, 200f, 180f, 170f, 150f },
                            finishStrength = 1.0f, finishCurve = 2f, newCardMultiplier = 2.0f,
                        },
                        new CardGachaSlotRule
                        {
                            ruleName = "RareGachaRule_2_3",
                            weight1Star = 45f, weight2Star = 29f, weight3Star = 20.9f, weight4Star = 5.0f,
                            setBaseWeights = new[] { 140f, 160f, 200f, 180f, 170f, 150f },
                            finishStrength = 1.0f, finishCurve = 2f, newCardMultiplier = 2.0f,
                        },
                    }
                },
                new CardPackShopInfo
                {
                    productId = "card_pack_epic",
                    packName = "Epic Pack",
                    totalCardsCount = 4,
                    guaranteedRarity = CardRarity.ThreeStar,
                    guaranteedCount = 1,
                    packSprite = epicSprite,
                    slotRules = new List<CardGachaSlotRule>
                    {
                        new CardGachaSlotRule
                        {
                            ruleName = "EpicGachaRule_1",
                            weight1Star = 0f, weight2Star = 0f, weight3Star = 100f, weight4Star = 0f,
                            setBaseWeights = new[] { 80f, 100f, 160f, 230f, 220f, 210f },
                            finishStrength = 0f, finishCurve = 2f, newCardMultiplier = 1f,
                        },
                        new CardGachaSlotRule
                        {
                            ruleName = "EpicGachaRule_2",
                            weight1Star = 42f, weight2Star = 30f, weight3Star = 21.9f, weight4Star = 6.0f,
                            setBaseWeights = new[] { 80f, 100f, 160f, 230f, 220f, 210f },
                            finishStrength = 1.5f, finishCurve = 2f, newCardMultiplier = 3.0f,
                        },
                        new CardGachaSlotRule
                        {
                            ruleName = "EpicGachaRule_3",
                            weight1Star = 0f, weight2Star = 20f, weight3Star = 69.4f, weight4Star = 10.6f,
                            setBaseWeights = new[] { 80f, 100f, 160f, 230f, 220f, 210f },
                            finishStrength = 1.5f, finishCurve = 2f, newCardMultiplier = 3.0f,
                        },
                        new CardGachaSlotRule
                        {
                            ruleName = "EpicGachaRule_4",
                            weight1Star = 0f, weight2Star = 58.1f, weight3Star = 31.3f, weight4Star = 10.6f,
                            setBaseWeights = new[] { 80f, 100f, 160f, 230f, 220f, 210f },
                            finishStrength = 1.5f, finishCurve = 2f, newCardMultiplier = 3.0f,
                        },
                    }
                },
                new CardPackShopInfo
                {
                    productId = "card_pack_special",
                    packName = "Special Pack",
                    totalCardsCount = 5,
                    guaranteedRarity = CardRarity.FourStar,
                    guaranteedCount = 1,
                    packSprite = specialSprite,
                    slotRules = new List<CardGachaSlotRule>
                    {
                        new CardGachaSlotRule
                        {
                            ruleName = "SpecialGachaRule_1",
                            weight1Star = 0f, weight2Star = 0f, weight3Star = 0f, weight4Star = 100f,
                            setBaseWeights = new[] { 50f, 70f, 130f, 250f, 250f, 250f },
                            finishStrength = 0f, finishCurve = 2f, newCardMultiplier = 1f,
                        },
                        new CardGachaSlotRule
                        {
                            ruleName = "SpecialGachaRule_2",
                            weight1Star = 42f, weight2Star = 30f, weight3Star = 21.9f, weight4Star = 6.0f,
                            setBaseWeights = new[] { 50f, 70f, 130f, 250f, 250f, 250f },
                            finishStrength = 2.0f, finishCurve = 2f, newCardMultiplier = 4.0f,
                        },
                        new CardGachaSlotRule
                        {
                            ruleName = "SpecialGachaRule_3",
                            weight1Star = 0f, weight2Star = 0f, weight3Star = 88.2f, weight4Star = 11.8f,
                            setBaseWeights = new[] { 50f, 70f, 130f, 250f, 250f, 250f },
                            finishStrength = 2.0f, finishCurve = 2f, newCardMultiplier = 4.0f,
                        },
                        new CardGachaSlotRule
                        {
                            ruleName = "SpecialGachaRule_4_5",
                            weight1Star = 0f, weight2Star = 58.8f, weight3Star = 29.4f, weight4Star = 11.8f,
                            setBaseWeights = new[] { 50f, 70f, 130f, 250f, 250f, 250f },
                            finishStrength = 2.0f, finishCurve = 2f, newCardMultiplier = 4.0f,
                        },
                        new CardGachaSlotRule
                        {
                            ruleName = "SpecialGachaRule_4_5",
                            weight1Star = 0f, weight2Star = 58.8f, weight3Star = 29.4f, weight4Star = 11.8f,
                            setBaseWeights = new[] { 50f, 70f, 130f, 250f, 250f, 250f },
                            finishStrength = 2.0f, finishCurve = 2f, newCardMultiplier = 4.0f,
                        },
                    }
                },
            };
        }

        // EN: Retrieves the NormalGachaRule parameter block for the specified collection ID (0 to 5).
        // VI: Lấy khối thông số NormalGachaRule cho bộ sưu tập theo mã collectionId (0 đến 5).
        public CardSetGachaParam GetSetGachaParam(int collectionId)
        {
            EnsureDefaultNormalGachaRule();
            for (int i = 0; i < normalGachaSetParams.Count; i++)
            {
                if (normalGachaSetParams[i].collectionId == collectionId)
                {
                    return normalGachaSetParams[i];
                }
            }
            return normalGachaSetParams[Mathf.Clamp(collectionId, 0, normalGachaSetParams.Count - 1)];
        }

        // EN: Returns the token cost for the next spin based on how many slots have already been claimed on the current board (50 -> 600).
        // VI: Trả về giá vé (token) cho lượt quay tiếp theo dựa trên số ô đã nhận trên bảng hiện tại (50 -> 600).
        public int GetSpinCostForStep(int claimedSlotsOnBoard)
        {
            EnsureDefaultSpinSlots();
            if (spinStepCosts != null && spinStepCosts.Count > 0)
            {
                int idx = Mathf.Clamp(claimedSlotsOnBoard, 0, spinStepCosts.Count - 1);
                return Mathf.Max(1, spinStepCosts[idx]);
            }
            return Mathf.Max(1, (claimedSlotsOnBoard + 1) * spinCostTickets);
        }

        // EN: Returns the 12-slot spin board configuration for the given board index (0 to 3, clamping to the repeating final board).
        // VI: Trả về cấu hình bảng vòng quay 12 ô cho chỉ số bảng tương ứng (0 đến 3, giữ ở bảng lặp cuối cùng nếu vượt quá).
        public CardSpinBoardInfo GetSpinBoard(int boardIndex)
        {
            EnsureDefaultSpinBoards();
            if (spinBoards == null || spinBoards.Count == 0) return null;
            int clampedIndex = Mathf.Clamp(boardIndex, 0, spinBoards.Count - 1);
            return spinBoards[clampedIndex];
        }

        // EN: Returns the 12 spin slots for the given board index.
        // VI: Trả về danh sách 12 ô vòng quay của bảng theo chỉ số boardIndex.
        public List<CardSpinSlotInfo> GetSpinBoardSlots(int boardIndex)
        {
            CardSpinBoardInfo board = GetSpinBoard(boardIndex);
            if (board != null && board.slots != null && board.slots.Count == 12)
            {
                return board.slots;
            }
            EnsureDefaultSpinSlots();
            return spinSlots;
        }

        // EN: Returns the spin wheel card icon for the given rarity (reusing icons configured on spinSlots or falling back to BackCard).
        // VI: Trả về biểu tượng thẻ trên bàn quay theo độ hiếm (tái sử dụng icon từ spinSlots hoặc dùng BackCard).
        public Sprite GetSpinCardIcon(CardRarity rarity)
        {
            if (spinSlots != null)
            {
                for (int i = 0; i < spinSlots.Count; i++)
                {
                    if (spinSlots[i] != null &&
                        spinSlots[i].slotType != CardSpinSlotType.Resource &&
                        spinSlots[i].cardRarity == rarity &&
                        spinSlots[i].customIcon != null)
                    {
                        return spinSlots[i].customIcon;
                    }
                }
            }
            return GetBackCardSprite(rarity);
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

