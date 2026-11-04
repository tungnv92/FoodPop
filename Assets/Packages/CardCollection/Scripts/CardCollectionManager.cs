using System;
using System.Collections.Generic;
using System.Globalization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GamePackages
{
    [Serializable]
    public class CardCollectionManager : ContextData<CardCollectionData>, IFeatureNewDay, IFeatureUpdate
    {
        public static CardCollectionManager Instance =>
            ProfileManager.instance != null && ProfileManager.instance.GameFeatureContext != null
                ? ProfileManager.instance.GameFeatureContext.CardCollectionManager
                : null;

        public static CardCollectionManager Ins => Instance;

        public const int TOTAL_COLLECTIONS = 6;
        public const int CARDS_PER_COLLECTION = 9;
        public const int TOTAL_CARDS = TOTAL_COLLECTIONS * CARDS_PER_COLLECTION;

        public CardCollectionConfig CardCollectionConfig;
        private CardCollectionConfig _fallbackRuntimeConfig;

        public CardCollectionConfig config
        {
            get
            {
                if (CardCollectionConfig == null)
                {
                    if (ProfileManager.instance != null && ProfileManager.instance.CardCollectionConfig != null)
                    {
                        CardCollectionConfig = ProfileManager.instance.CardCollectionConfig;
                    }
                    else
                    {
                        CardCollectionConfig = Resources.Load<CardCollectionConfig>("CardCollectionConfig");
                    }

                    if (CardCollectionConfig == null)
                    {
                        if (_fallbackRuntimeConfig == null)
                        {
                            _fallbackRuntimeConfig = ScriptableObject.CreateInstance<CardCollectionConfig>();
                            _fallbackRuntimeConfig.EnsureDefaultSetup();
                        }
                        return _fallbackRuntimeConfig;
                    }
                }
                return CardCollectionConfig;
            }
            set => CardCollectionConfig = value;
        }

        public new CardCollectionData Data =>
            base.Data ?? (ProfileManager.instance != null && ProfileManager.instance.PlayerData != null
                ? ProfileManager.instance.PlayerData.CardCollectionData
                : null);

        public event Action OnDataChanged;

        [Tooltip("Bật chế độ gian lận kích hoạt sự kiện ngay lập tức để kiểm thử.")]
        public bool isCheatForceActive;

        public int UnlockLevel => config != null ? config.unlockLevelIndex : 40;

        public int EventDurationDays => config != null ? Mathf.Max(1, config.eventDurationDays) : 14;

        public int SpinCostTickets => config != null ? Mathf.Max(1, config.spinCostTickets) : 50;

        public int SpinSetTargetCount => config != null ? Mathf.Max(1, config.spinSetTargetCount) : 12;

        // EN: Checks whether the player has won level 40 (currentLevel > UnlockLevel) or already unlocked the event.
        // VI: Kiểm tra xem người chơi đã thắng màn 40 (currentLevel > UnlockLevel) hoặc đã mở khóa sự kiện hay chưa.
        public bool IsLevelUnlocked =>
            isCheatForceActive ||
            (Data != null && Data.isUnlocked) ||
            (ProfileManager.instance != null &&
             ProfileManager.instance.PlayerData != null &&
             ProfileManager.instance.PlayerData.currentLevel > UnlockLevel);

        // EN: Checks whether the event is currently active (enabled, unlocked, and within the 14-day window).
        // VI: Kiểm tra xem sự kiện hiện đang hoạt động hay không (được bật, đã mở khóa và còn trong thời hạn 14 ngày).
        public bool isActive =>
            (config == null || config.isFeatureEnabled) &&
            IsLevelUnlocked &&
            !IsExpired();

        // EN: Binds the persistent CardCollectionData instance and initializes or validates the event cycle.
        // VI: Gắn dữ liệu CardCollectionData lưu trữ và khởi tạo hoặc kiểm tra chu kỳ sự kiện.
        public override void SetContext(CardCollectionData data)
        {
            if (data == null)
            {
                data = new CardCollectionData();
                if (ProfileManager.instance != null && ProfileManager.instance.PlayerData != null)
                {
                    ProfileManager.instance.PlayerData.CardCollectionData = data;
                }
            }
            base.SetContext(data);
            CheckUnlockAndCycle();
            AutoClaimCompletedCollectionsIfNeeded();
        }

        // EN: Re-validates unlock state and expiration when a new day starts.
        // VI: Kiểm tra lại trạng thái mở khóa và thời hạn khi bước sang ngày mới.
        public void OnNewDay()
        {
            CheckUnlockAndCycle();
        }

        // EN: Periodically checks whether the player has reached the unlock level.
        // VI: Kiểm tra định kỳ xem người chơi đã đạt cấp độ mở khóa sự kiện hay chưa.
        public void OnUpdate()
        {
            if (Data != null && !Data.isUnlocked && IsLevelUnlocked)
            {
                CheckUnlockAndCycle();
            }
        }

        // EN: Returns the current game time from TimeManager or DateTime.Now as fallback.
        // VI: Trả về thời gian hiện tại của game từ TimeManager hoặc DateTime.Now làm dự phòng.
        public DateTime GetCurrentTime()
        {
            if (ProfileManager.instance != null && ProfileManager.instance.TimeManager != null)
            {
                DateTime now = ProfileManager.instance.TimeManager.GetNow();
                if (now != default) return now;
            }
            return DateTime.Now;
        }

        // EN: Unlocks the 14-day event cycle when the player meets the level requirement for the first time.
        // VI: Mở khóa chu kỳ sự kiện 14 ngày khi người chơi đạt đủ điều kiện cấp độ lần đầu tiên.
        public void CheckUnlockAndCycle(bool forceUnlockNow = false)
        {
            if (Data == null) return;

            if ((forceUnlockNow || IsLevelUnlocked) && (!Data.isUnlocked || string.IsNullOrEmpty(Data.expireTimeIso)))
            {
                DateTime now = GetCurrentTime();
                DateTime expire = now.Date.AddDays(EventDurationDays).AddSeconds(-1);
                if (expire <= now)
                {
                    expire = now.AddDays(EventDurationDays);
                }
                Data.StartNewCycle(now, expire);
                Save();
                OnDataChanged?.Invoke();
            }
        }

        // EN: Parses and returns the event expiration timestamp, or DateTime.MaxValue if not yet initialized.
        // VI: Phân tích và trả về mốc thời gian hết hạn sự kiện, hoặc DateTime.MaxValue nếu chưa khởi tạo.
        public DateTime GetExpireTime()
        {
            if (Data != null && !string.IsNullOrEmpty(Data.expireTimeIso))
            {
                if (DateTime.TryParse(Data.expireTimeIso, null, DateTimeStyles.RoundtripKind, out DateTime parsed))
                {
                    return parsed;
                }
            }
            return GetCurrentTime().AddDays(EventDurationDays);
        }

        // EN: Returns the remaining time until the 14-day event expires.
        // VI: Trả về khoảng thời gian còn lại cho đến khi sự kiện 14 ngày kết thúc.
        public TimeSpan GetRemainingTime()
        {
            if (Data == null || !Data.isUnlocked) return TimeSpan.FromDays(EventDurationDays);
            TimeSpan remaining = GetExpireTime() - GetCurrentTime();
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }

        // EN: Checks whether the 14-day event window has expired.
        // VI: Kiểm tra xem thời hạn 14 ngày của sự kiện đã kết thúc hay chưa.
        public bool IsExpired()
        {
            if (isCheatForceActive) return false;
            if (Data == null || !Data.isUnlocked) return false;
            return GetRemainingTime().TotalSeconds <= 0;
        }

        // EN: Formats the remaining event time for UI display (e.g. "13d 19h" or "05h 30m").
        // VI: Định dạng thời gian còn lại của sự kiện để hiển thị lên giao diện (ví dụ "13d 19h" hoặc "05h 30m").
        public string GetCountDownString()
        {
            TimeSpan remaining = GetRemainingTime();
            if (remaining.TotalSeconds <= 0) return "Ended";
            if (remaining.TotalDays >= 1)
            {
                return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
            }
            if (remaining.TotalHours >= 1)
            {
                return $"{remaining.Hours:00}h {remaining.Minutes:00}m";
            }
            return $"{remaining.Minutes:00}m {remaining.Seconds:00}s";
        }

        // EN: Returns true when there is an actionable notification (enough tickets to spin, unclaimed set, or grand prize).
        // VI: Trả về true khi có thông báo cần chú ý (đủ vé để quay, có bộ sưu tập hoặc giải thưởng lớn chưa nhận).
        public bool IsNoti()
        {
            if (!isActive || Data == null) return false;
            if (CanSpin()) return true;
            if (CanClaimGrandReward()) return true;
            for (int i = 0; i < TOTAL_COLLECTIONS; i++)
            {
                if (CanClaimCollectionReward(i)) return true;
            }
            return false;
        }

        #region CARD PROGRESS & QUERIES

        // EN: Returns the total number of unique cards collected across all 6 collections (0 to 54).
        // VI: Trả về tổng số thẻ độc nhất đã thu thập được trên cả 6 bộ sưu tập (0 đến 54).
        public int GetTotalOwnedCardsCount()
        {
            if (Data == null || Data.ownedCardIds == null) return 0;
            return Mathf.Clamp(Data.ownedCardIds.Count, 0, TOTAL_CARDS);
        }

        // EN: Returns the number of unique cards collected within the specified collection (0 to 9).
        // VI: Trả về số lượng thẻ đã thu thập được trong bộ sưu tập chỉ định (0 đến 9).
        public int GetOwnedCountInCollection(int collectionId)
        {
            if (Data == null || Data.ownedCardIds == null || config == null) return 0;
            int count = 0;
            List<CardCollectionCardInfo> setCards = GetCardsInCollection(collectionId);
            for (int i = 0; i < setCards.Count; i++)
            {
                if (Data.HasCard(setCards[i].cardId))
                {
                    count++;
                }
            }
            return count;
        }

        // EN: Returns the list of 9 card definitions belonging to the specified collection index (0 to 5).
        // VI: Trả về danh sách 9 định nghĩa thẻ thuộc bộ sưu tập theo chỉ số chỉ định (0 đến 5).
        public List<CardCollectionCardInfo> GetCardsInCollection(int collectionId)
        {
            List<CardCollectionCardInfo> result = new(CARDS_PER_COLLECTION);
            if (config == null) return result;
            config.EnsureDefaultSetup();

            for (int i = 0; i < config.cards.Count; i++)
            {
                if (config.cards[i].collectionId == collectionId)
                {
                    result.Add(config.cards[i]);
                }
            }
            return result;
        }

        // EN: Checks whether all 9 cards in the specified collection have been collected.
        // VI: Kiểm tra xem toàn bộ 9 thẻ trong bộ sưu tập chỉ định đã được thu thập đủ hay chưa.
        public bool IsCollectionCompleted(int collectionId)
        {
            return GetOwnedCountInCollection(collectionId) >= CARDS_PER_COLLECTION;
        }

        // EN: Checks whether the specified collection is complete and its reward has not yet been claimed.
        // VI: Kiểm tra xem bộ sưu tập chỉ định đã hoàn thành và phần thưởng của nó chưa được nhận hay không.
        public bool CanClaimCollectionReward(int collectionId)
        {
            if (Data == null) return false;
            return IsCollectionCompleted(collectionId) && !Data.IsCollectionClaimed(collectionId);
        }

        // EN: Checks whether all 54 cards are collected and the Grand Reward has not yet been claimed.
        // VI: Kiểm tra xem đã thu thập đủ 54 thẻ và phần thưởng lớn (Grand Reward) chưa được nhận hay không.
        public bool CanClaimGrandReward()
        {
            if (Data == null) return false;
            return GetTotalOwnedCardsCount() >= TOTAL_CARDS && !Data.isClaimedGrandReward;
        }

        #endregion

        #region CARD DROP & RARITY ALGORITHM

        // EN: Handles level win events; unlocks the event at level 40 and awards 1 random card on a first-try win.
        // VI: Xử lý khi thắng màn chơi; mở khóa sự kiện khi thắng màn 40 và thưởng 1 thẻ ngẫu nhiên nếu thắng ngay lần thử đầu.
        public CardDropResult OnLevelWin(bool isFirstTry)
        {
            if (ProfileManager.instance != null && ProfileManager.instance.PlayerData != null)
            {
                int currentLevel = ProfileManager.instance.PlayerData.currentLevel;
                if (currentLevel >= UnlockLevel && (Data == null || !Data.isUnlocked))
                {
                    CheckUnlockAndCycle(true);
                }
            }

            if (!isActive || !isFirstTry || Data == null)
            {
                return null;
            }

            CardRarity rolledRarity = RollCardRarityWithPity();
            int rolledCardId = RollCardIdForRarity(rolledRarity);
            CardDropResult dropResult = GrantCard(rolledCardId);

            if (dropResult != null)
            {
                Data.pendingDroppedCardId = dropResult.cardId;
                Data.pendingDroppedCardWasDuplicate = dropResult.isDuplicate;
                Data.pendingDroppedTicketsEarned = dropResult.ticketsConverted;
                Save();
            }

            return dropResult;
        }

        // EN: Rolls a card rarity using base drop weights combined with pity counters for 3-star and 4-star cards.
        // VI: Quay độ hiếm của thẻ dựa trên trọng số cơ bản kết hợp bộ đếm bảo hiểm cho thẻ 3 sao và 4 sao.
        public CardRarity RollCardRarityWithPity()
        {
            if (Data == null || config == null) return CardRarity.OneStar;
            config.EnsureDefaultSetup();

            Data.firstTryWinsWithoutTier3++;
            Data.firstTryWinsWithoutTier4++;

            CardRarity selectedRarity;
            if (config.pityWinsForFourStar > 0 && Data.firstTryWinsWithoutTier4 >= config.pityWinsForFourStar)
            {
                selectedRarity = CardRarity.FourStar;
            }
            else if (config.pityWinsForThreeStar > 0 && Data.firstTryWinsWithoutTier3 >= config.pityWinsForThreeStar)
            {
                selectedRarity = CardRarity.ThreeStar;
            }
            else
            {
                selectedRarity = RollWeightedRarity(CardRarity.OneStar);
            }

            if (selectedRarity == CardRarity.FourStar)
            {
                Data.firstTryWinsWithoutTier4 = 0;
                Data.firstTryWinsWithoutTier3 = 0;
            }
            else if (selectedRarity == CardRarity.ThreeStar)
            {
                Data.firstTryWinsWithoutTier3 = 0;
            }

            return selectedRarity;
        }

        // EN: Performs a weighted random roll across configured rarities at or above the specified minimum rarity.
        // VI: Thực hiện quay ngẫu nhiên có trọng số giữa các độ hiếm từ mức tối thiểu được chỉ định trở lên.
        public CardRarity RollWeightedRarity(CardRarity minRarity = CardRarity.OneStar)
        {
            if (config == null) return minRarity;
            config.EnsureDefaultSetup();

            float totalWeight = 0f;
            for (int i = 0; i < config.rarityConfigs.Count; i++)
            {
                if (config.rarityConfigs[i].rarity >= minRarity)
                {
                    totalWeight += Mathf.Max(0f, config.rarityConfigs[i].dropWeight);
                }
            }

            if (totalWeight <= 0f) return minRarity;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < config.rarityConfigs.Count; i++)
            {
                var rc = config.rarityConfigs[i];
                if (rc.rarity < minRarity) continue;

                cumulative += Mathf.Max(0f, rc.dropWeight);
                if (roll <= cumulative)
                {
                    return rc.rarity;
                }
            }

            return minRarity;
        }

        // EN: Selects a card ID of the given rarity, applying the configured preference toward unowned cards.
        // VI: Chọn mã thẻ theo độ hiếm chỉ định, áp dụng tỉ lệ ưu tiên cho các thẻ người chơi chưa sở hữu.
        public int RollCardIdForRarity(CardRarity rarity)
        {
            if (config == null) return 0;
            config.EnsureDefaultSetup();

            List<int> allMatching = new();
            List<int> unownedMatching = new();

            for (int i = 0; i < config.cards.Count; i++)
            {
                CardCollectionCardInfo card = config.cards[i];
                if (card.rarity == rarity)
                {
                    allMatching.Add(card.cardId);
                    if (Data == null || !Data.HasCard(card.cardId))
                    {
                        unownedMatching.Add(card.cardId);
                    }
                }
            }

            if (allMatching.Count == 0)
            {
                return UnityEngine.Random.Range(0, TOTAL_CARDS);
            }

            if (unownedMatching.Count > 0 && UnityEngine.Random.value <= config.unownedCardPreferenceChance)
            {
                return unownedMatching[UnityEngine.Random.Range(0, unownedMatching.Count)];
            }

            return allMatching[UnityEngine.Random.Range(0, allMatching.Count)];
        }

        // EN: Grants the specified card to the player, or converts it into Spin Tickets if it is a duplicate.
        // VI: Trao thẻ chỉ định cho người chơi, hoặc chuyển đổi thành Vé quay nếu thẻ đó đã được sở hữu trước đó.
        public CardDropResult GrantCard(int cardId)
        {
            if (Data == null || config == null) return null;
            CardCollectionCardInfo cardInfo = config.GetCardInfo(cardId);
            if (cardInfo == null) return null;

            bool wasOwnedBeforeSetCheck = IsCollectionCompleted(cardInfo.collectionId);
            int totalBefore = GetTotalOwnedCardsCount();

            bool isNewCard = Data.AddCard(cardId);
            int ticketsEarned = 0;

            if (!isNewCard)
            {
                CardRarityDropConfig rarityCfg = config.GetRarityConfig(cardInfo.rarity);
                ticketsEarned = rarityCfg != null ? Mathf.Max(1, rarityCfg.duplicateTicketReward) : 5;
                Data.ticketAmount += ticketsEarned;
            }

            bool completedSetNow = !wasOwnedBeforeSetCheck && IsCollectionCompleted(cardInfo.collectionId);
            bool completedAllNow = totalBefore < TOTAL_CARDS && GetTotalOwnedCardsCount() >= TOTAL_CARDS;

            CardDropResult result = new CardDropResult
            {
                cardId = cardInfo.cardId,
                collectionId = cardInfo.collectionId,
                rarity = cardInfo.rarity,
                isDuplicate = !isNewCard,
                ticketsConverted = ticketsEarned,
                completedCollection = completedSetNow,
                completedAllCollections = completedAllNow,
            };

            Save();
            OnDataChanged?.Invoke();
            return result;
        }

        #endregion

        #region COLLECTION & GRAND REWARD CLAIMING

        // EN: Claims the reward bundle for a completed 9-card collection and saves progress.
        // VI: Nhận gói phần thưởng khi hoàn thành bộ sưu tập 9 thẻ và lưu tiến độ.
        public List<ResourceGame> ClaimCollectionReward(int collectionId)
        {
            if (!CanClaimCollectionReward(collectionId)) return null;

            CardCollectionSetInfo setInfo = config.GetCollectionInfo(collectionId);
            if (setInfo == null) return null;

            Data.claimedCollectionIds.Add(collectionId);
            Save();

            if (setInfo.rewards != null)
            {
                for (int i = 0; i < setInfo.rewards.Count; i++)
                {
                    GrantResourceReward(setInfo.rewards[i], $"CardCollection_Set_{collectionId + 1}");
                }
            }

            OnDataChanged?.Invoke();
            return setInfo.rewards;
        }

        // EN: Claims the Grand Reward once all 54 cards have been collected, including coins, boosters, and avatar frame.
        // VI: Nhận phần thưởng lớn (Grand Reward) khi thu thập đủ 54 thẻ, bao gồm xu, booster và khung avatar.
        public List<ResourceGame> ClaimGrandReward()
        {
            if (!CanClaimGrandReward()) return null;

            Data.isClaimedGrandReward = true;

            if (config.grandRewardFrameId >= 0 &&
                ProfileManager.instance != null &&
                ProfileManager.instance.PlayerData != null)
            {
                ProfileManager.instance.PlayerData.frame_avatar_id = config.grandRewardFrameId;
            }

            if (config.grandRewardResources != null)
            {
                for (int i = 0; i < config.grandRewardResources.Count; i++)
                {
                    GrantResourceReward(config.grandRewardResources[i], "CardCollection_GrandReward");
                }
            }

            Save();
            OnDataChanged?.Invoke();
            return config.grandRewardResources;
        }

        // EN: Automatically checks if any collection rewards need validation on startup.
        // VI: Tự động kiểm tra tính hợp lệ của dữ liệu các bộ sưu tập khi khởi động.
        private void AutoClaimCompletedCollectionsIfNeeded()
        {
            if (Data == null) return;
            Data.ownedCardIds ??= new List<int>();
            Data.claimedCollectionIds ??= new List<int>();
        }

        // EN: Grants a single ResourceGame reward through PlayerDataManager.
        // VI: Cộng phần thưởng ResourceGame thông qua PlayerDataManager.
        public void GrantResourceReward(ResourceGame reward, string placement = "CardCollection")
        {
            if (reward == null || ProfileManager.instance == null || ProfileManager.instance.PlayerDataManager == null)
            {
                return;
            }

            PlayerDataManager pdm = ProfileManager.instance.PlayerDataManager;
            switch (reward.type)
            {
                case ResourceType.Coin:
                    pdm.OnAddCoin(reward.value, placement);
                    break;
                case ResourceType.UnlimitedLives:
                    pdm.AddUnlimitedLiveTime(reward.value, placement);
                    break;
                case ResourceType.BoosterAddSlot:
                case ResourceType.BoosterShuffle:
                case ResourceType.BoosterPickup:
                case ResourceType.BoosterSuperBear:
                    pdm.OnAddBooster(reward.type, reward.value, placement);
                    break;
                default:
                    pdm.OnAddResource(reward.type, reward.value, placement);
                    break;
            }
        }

        #endregion

        #region LUCKY SPIN

        // EN: Checks whether the player has enough duplicate tickets to perform a spin.
        // VI: Kiểm tra xem người chơi có đủ số vé trùng lặp để thực hiện một lượt quay hay không.
        public bool CanSpin()
        {
            return Data != null && Data.ticketAmount >= SpinCostTickets;
        }

        // EN: Spends tickets to spin the 12-slot board, grants the landed slot reward, and checks the 12-spin set bonus.
        // VI: Tiêu vé để quay bàn 12 ô, trao phần thưởng tại ô trúng và kiểm tra thưởng mốc 12 lượt quay.
        public bool TryExecuteSpin(
            out int landedSlotIndex,
            out CardSpinSlotInfo landedSlot,
            out CardDropResult spinCardDrop,
            out CardDropResult setBonusCardDrop)
        {
            landedSlotIndex = 0;
            landedSlot = null;
            spinCardDrop = null;
            setBonusCardDrop = null;

            if (!CanSpin() || config == null) return false;
            config.EnsureDefaultSetup();

            Data.ticketAmount -= SpinCostTickets;
            landedSlotIndex = RollSpinSlotIndex();
            landedSlot = config.spinSlots[landedSlotIndex];

            if (landedSlot.slotType == CardSpinSlotType.Resource)
            {
                GrantResourceReward(landedSlot.resourceReward, "CardCollection_Spin");
            }
            else
            {
                int cardId = RollCardIdForRarity(landedSlot.cardRarity);
                spinCardDrop = GrantCard(cardId);
            }

            Data.currentSpinSetCount++;
            Data.totalSpinCount++;

            if (Data.currentSpinSetCount >= SpinSetTargetCount)
            {
                Data.currentSpinSetCount = 0;
                int bonusCardId = RollCardIdForRarity(config.spinSetBonusCardRarity);
                setBonusCardDrop = GrantCard(bonusCardId);
            }

            Save();
            OnDataChanged?.Invoke();
            return true;
        }

        // EN: Performs a weighted random roll across the 12 slots on the spin board.
        // VI: Thực hiện quay ngẫu nhiên có trọng số trên 12 ô của bàn quay.
        private int RollSpinSlotIndex()
        {
            if (config == null || config.spinSlots == null || config.spinSlots.Count == 0) return 0;

            float totalWeight = 0f;
            for (int i = 0; i < config.spinSlots.Count; i++)
            {
                totalWeight += Mathf.Max(0f, config.spinSlots[i].weight);
            }

            if (totalWeight <= 0f) return UnityEngine.Random.Range(0, config.spinSlots.Count);

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < config.spinSlots.Count; i++)
            {
                cumulative += Mathf.Max(0f, config.spinSlots[i].weight);
                if (roll <= cumulative)
                {
                    return i;
                }
            }

            return 0;
        }

        #endregion

        #region IAP CARD PACK OPENING

        // EN: Opens the specified shop card pack by index (0..3) and grants all rolled cards.
        // VI: Mở gói thẻ trong cửa hàng theo chỉ số (0..3) và trao toàn bộ các thẻ mở được.
        public List<CardDropResult> OpenCardPack(int packIndex)
        {
            if (config == null) return null;
            config.EnsureDefaultSetup();
            if (packIndex < 0 || packIndex >= config.shopPacks.Count) return null;

            CardPackShopInfo packInfo = config.shopPacks[packIndex];
            return OpenCardPackInfo(packInfo);
        }

        // EN: Opens a card pack based on its CardPackShopInfo rules (guaranteed rarity cards + normal rolls).
        // VI: Mở gói thẻ dựa trên quy tắc CardPackShopInfo (các thẻ đảm bảo độ hiếm + các thẻ quay thường).
        public List<CardDropResult> OpenCardPackInfo(CardPackShopInfo packInfo)
        {
            if (packInfo == null) return null;
            List<CardDropResult> results = new(packInfo.totalCardsCount);

            int guaranteed = Mathf.Clamp(packInfo.guaranteedCount, 0, packInfo.totalCardsCount);
            for (int i = 0; i < guaranteed; i++)
            {
                CardRarity rarity = RollWeightedRarity(packInfo.guaranteedRarity);
                int cardId = RollCardIdForRarity(rarity);
                CardDropResult res = GrantCard(cardId);
                if (res != null) results.Add(res);
            }

            for (int i = guaranteed; i < packInfo.totalCardsCount; i++)
            {
                CardRarity rarity = RollWeightedRarity(CardRarity.OneStar);
                int cardId = RollCardIdForRarity(rarity);
                CardDropResult res = GrantCard(cardId);
                if (res != null) results.Add(res);
            }

            return results;
        }

        #endregion

        #region CHEAT / DEBUG HELPERS

        // EN: Cheat helper to unlock the Card Collection event immediately for testing.
        // VI: Hàm hỗ trợ kiểm thử giúp mở khóa sự kiện sưu tập thẻ ngay lập tức.
        [Button]
        public void CheatUnlockEvent()
        {
            isCheatForceActive = true;
            CheckUnlockAndCycle(true);
        }

        // EN: Cheat helper to grant 1 random card using the normal first-try level win rules.
        // VI: Hàm hỗ trợ kiểm thử giúp nhận 1 thẻ ngẫu nhiên theo quy tắc thắng màn lần đầu.
        [Button]
        public void CheatDropRandomCard()
        {
            CheckUnlockAndCycle(true);
            OnLevelWin(true);
        }

        // EN: Cheat helper to add Spin Tickets directly for testing the Spin popup.
        // VI: Hàm hỗ trợ kiểm thử giúp cộng thêm Vé quay trực tiếp để kiểm tra popup Vòng quay.
        [Button]
        public void CheatAddTickets(int amount = 100)
        {
            if (Data == null) return;
            Data.ticketAmount += Mathf.Max(0, amount);
            Save();
            OnDataChanged?.Invoke();
        }

        // EN: Cheat helper to collect all 9 cards in the specified collection index (0 to 5).
        // VI: Hàm hỗ trợ kiểm thử giúp thu thập đủ 9 thẻ trong bộ sưu tập chỉ định (0 đến 5).
        [Button]
        public void CheatCompleteCollection(int collectionId = 0)
        {
            List<CardCollectionCardInfo> setCards = GetCardsInCollection(collectionId);
            for (int i = 0; i < setCards.Count; i++)
            {
                GrantCard(setCards[i].cardId);
            }
        }

        // EN: Cheat helper to collect all 54 cards across all 6 collections.
        // VI: Hàm hỗ trợ kiểm thử giúp thu thập đủ toàn bộ 54 thẻ của cả 6 bộ sưu tập.
        [Button]
        public void CheatCollectAllCards()
        {
            for (int i = 0; i < TOTAL_CARDS; i++)
            {
                GrantCard(i);
            }
        }

        // EN: Cheat helper to clear all Card Collection progress and reset the event.
        // VI: Hàm hỗ trợ kiểm thử giúp xóa toàn bộ tiến độ sưu tập thẻ và đặt lại sự kiện.
        [Button]
        public void ClearAllData()
        {
            if (Data != null)
            {
                Data.Reset();
                Save();
                OnDataChanged?.Invoke();
            }
        }

        #endregion
    }
}

