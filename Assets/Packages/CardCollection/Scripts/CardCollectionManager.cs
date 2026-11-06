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

        public int SpinCostTickets
        {
            get
            {
                if (config == null) return 50;
                int claimedOnBoard = Data != null && Data.claimedSpinSlots != null ? Data.claimedSpinSlots.Count : 0;
                return config.GetSpinCostForStep(claimedOnBoard);
            }
        }

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

        // EN: Handles level win events; unlocks the event at level 40 and awards 1 card using NormalGachaRule on a first-try win.
        // VI: Xử lý khi thắng màn chơi; mở khóa sự kiện khi thắng màn 40 và thưởng 1 thẻ theo NormalGachaRule nếu thắng ngay lần thử đầu.
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

            if (!isActive || !isFirstTry || Data == null || config == null)
            {
                return null;
            }

            config.EnsureDefaultSetup();
            int rolledCardId = RollCardByGachaRule(config.normalGachaRule);
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

        // EN: Resolves a single card ID using the CardPackResolver algorithm (Hard Guarantee Pity -> Weighted Rarity -> Dynamic Set Weight [base * Early * Finish * Miss] -> Weighted Card [newCardMultiplier]).
        // VI: Chọn một mã thẻ theo thuật toán CardPackResolver (Bảo hiểm cứng Pity -> Quay độ hiếm -> Trọng số bộ động [base * Early * Finish * Miss] -> Trọng số thẻ [newCardMultiplier]).
        public int RollCardByGachaRule(CardGachaSlotRule rule)
        {
            if (config == null) return 0;
            config.EnsureDefaultSetup();
            Data?.EnsureSetGachaProgressCount();

            rule ??= config.normalGachaRule;

            if (rule.enableHardGuarantee && TryRollHardGuaranteedCard(rule, out int guaranteedCardId))
            {
                return guaranteedCardId;
            }

            CardRarity rolledRarity = RollRarityFromGachaRule(rule);
            int rolledSetId = RollCollectionForRarity(rule, rolledRarity);
            return RollCardInCollectionAndRarity(rolledSetId, rolledRarity, GetRuleNewCardMultiplier(rule, rolledSetId));
        }

        // EN: Checks whether any incomplete set has reached its hard guarantee threshold N (draws without a new card >= N) and forces a new card from that set.
        // VI: Kiểm tra xem có bộ sưu tập chưa hoàn thành nào đạt ngưỡng bảo hiểm cứng N (số lượt không ra thẻ mới >= N) để ép ra thẻ mới của bộ đó hay không.
        private bool TryRollHardGuaranteedCard(CardGachaSlotRule rule, out int guaranteedCardId)
        {
            guaranteedCardId = -1;
            if (Data == null) return false;
            Data.EnsureSetGachaProgressCount();

            for (int setId = 0; setId < TOTAL_COLLECTIONS; setId++)
            {
                CardSetGachaParam setParam = config.GetSetGachaParam(setId);
                if (setParam == null || setParam.guaranteeDraws <= 0) continue;

                int drawsWithoutNew = Data.setDrawsWithoutNewCard[setId];
                if (drawsWithoutNew < setParam.guaranteeDraws) continue;

                List<CardCollectionCardInfo> unownedAllowedCards = GetUnownedCardsInSetForRule(setId, rule);
                if (unownedAllowedCards.Count == 0) continue;

                guaranteedCardId = RollWeightedCardFromCandidates(unownedAllowedCards, rule);
                return guaranteedCardId >= 0;
            }

            return false;
        }

        // EN: Returns all unowned cards in the specified collection whose rarity has a positive weight in the given gacha rule.
        // VI: Trả về danh sách các thẻ chưa sở hữu trong bộ sưu tập chỉ định có độ hiếm được phép rơi trong quy tắc gacha.
        private List<CardCollectionCardInfo> GetUnownedCardsInSetForRule(int collectionId, CardGachaSlotRule rule)
        {
            List<CardCollectionCardInfo> list = new();
            List<CardCollectionCardInfo> setCards = GetCardsInCollection(collectionId);
            for (int i = 0; i < setCards.Count; i++)
            {
                CardCollectionCardInfo c = setCards[i];
                if ((Data == null || !Data.HasCard(c.cardId)) && rule.GetRarityWeight(c.rarity) > 0f)
                {
                    list.Add(c);
                }
            }
            return list;
        }

        // EN: Picks one card from the candidate list weighted by its rarity weight in the rule.
        // VI: Chọn một thẻ từ danh sách ứng viên dựa theo trọng số độ hiếm trong quy tắc.
        private int RollWeightedCardFromCandidates(List<CardCollectionCardInfo> candidates, CardGachaSlotRule rule)
        {
            if (candidates == null || candidates.Count == 0) return -1;

            float totalWeight = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                totalWeight += Mathf.Max(0.01f, rule.GetRarityWeight(candidates[i].rarity));
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += Mathf.Max(0.01f, rule.GetRarityWeight(candidates[i].rarity));
                if (roll <= cumulative)
                {
                    return candidates[i].cardId;
                }
            }

            return candidates[candidates.Count - 1].cardId;
        }

        // EN: Rolls a card rarity (1 to 4 stars) according to the rarity weights in the given gacha rule.
        // VI: Quay độ hiếm thẻ (1 đến 4 sao) theo trọng số độ hiếm trong quy tắc gacha được truyền vào.
        private CardRarity RollRarityFromGachaRule(CardGachaSlotRule rule)
        {
            CardRarity[] rarities = { CardRarity.OneStar, CardRarity.TwoStar, CardRarity.ThreeStar, CardRarity.FourStar };
            float totalWeight = 0f;
            for (int i = 0; i < rarities.Length; i++)
            {
                totalWeight += rule.GetRarityWeight(rarities[i]);
            }

            if (totalWeight <= 0f) return CardRarity.OneStar;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < rarities.Length; i++)
            {
                float w = rule.GetRarityWeight(rarities[i]);
                if (w <= 0f) continue;
                cumulative += w;
                if (roll <= cumulative)
                {
                    return rarities[i];
                }
            }

            return CardRarity.OneStar;
        }

        // EN: Rolls a collection ID (0..5) among sets that contain at least one card of the rolled rarity, using dynamic weight = base * Early * Finish * Miss.
        // VI: Quay chọn bộ sưu tập (0..5) trong các bộ có chứa thẻ thuộc độ hiếm đã quay, dùng công thức trọng số động = base * Early * Finish * Miss.
        private int RollCollectionForRarity(CardGachaSlotRule rule, CardRarity rarity)
        {
            List<int> eligibleSets = new(TOTAL_COLLECTIONS);
            List<float> setWeights = new(TOTAL_COLLECTIONS);
            float totalWeight = 0f;

            for (int setId = 0; setId < TOTAL_COLLECTIONS; setId++)
            {
                if (!DoesSetHaveCardOfRarity(setId, rarity)) continue;

                float w = ComputeDynamicSetWeight(setId, rule);
                eligibleSets.Add(setId);
                setWeights.Add(w);
                totalWeight += w;
            }

            if (eligibleSets.Count == 0) return 0;
            if (totalWeight <= 0f) return eligibleSets[UnityEngine.Random.Range(0, eligibleSets.Count)];

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < eligibleSets.Count; i++)
            {
                cumulative += setWeights[i];
                if (roll <= cumulative)
                {
                    return eligibleSets[i];
                }
            }

            return eligibleSets[eligibleSets.Count - 1];
        }

        // EN: Checks whether the specified collection contains at least one card of the given rarity.
        // VI: Kiểm tra xem bộ sưu tập chỉ định có chứa ít nhất một thẻ thuộc độ hiếm tương ứng hay không.
        private bool DoesSetHaveCardOfRarity(int collectionId, CardRarity rarity)
        {
            List<CardCollectionCardInfo> setCards = GetCardsInCollection(collectionId);
            for (int i = 0; i < setCards.Count; i++)
            {
                if (setCards[i].rarity == rarity) return true;
            }
            return false;
        }

        // EN: Computes the dynamic gacha weight for a collection: base * Early * Finish * Miss (or base only if the set is already complete for allowed rarities).
        // VI: Tính trọng số gacha động cho một bộ sưu tập: base * Early * Finish * Miss (hoặc chỉ lấy base nếu bộ đã đủ thẻ trong các độ hiếm cho phép).
        private float ComputeDynamicSetWeight(int collectionId, CardGachaSlotRule rule)
        {
            CardSetGachaParam setParam = config.GetSetGachaParam(collectionId);
            float baseWeight = rule.useNormalSetParams && setParam != null
                ? setParam.baseWeight
                : (rule.setBaseWeights != null && collectionId < rule.setBaseWeights.Length
                    ? rule.setBaseWeights[collectionId]
                    : 100f);

            float p = GetSetCompletionRatioForRule(collectionId, rule);
            if (p >= 1f)
            {
                return Mathf.Max(0f, baseWeight);
            }

            float earlyStrength = rule.useNormalSetParams && setParam != null ? setParam.earlyStrength : 0f;
            float finishStrength = rule.useNormalSetParams && setParam != null ? setParam.finishStrength : rule.finishStrength;
            float finishCurve = rule.useNormalSetParams && setParam != null ? setParam.finishCurve : rule.finishCurve;
            float missPerDraw = rule.useNormalSetParams && setParam != null ? setParam.missPerDraw : 0f;

            int drawsSinceLastNew = (Data != null && Data.setDrawsWithoutNewCard != null && collectionId < Data.setDrawsWithoutNewCard.Count)
                ? Mathf.Max(0, Data.setDrawsWithoutNewCard[collectionId])
                : 0;

            float earlyBoost = 1f + (1f - p) * Mathf.Max(0f, earlyStrength);
            float finishBoost = 1f + Mathf.Pow(Mathf.Clamp01(p), Mathf.Max(0.1f, finishCurve)) * Mathf.Max(0f, finishStrength);
            float missBoost = 1f + Mathf.Max(0f, missPerDraw) * drawsSinceLastNew;

            return Mathf.Max(0f, baseWeight) * earlyBoost * finishBoost * missBoost;
        }

        // EN: Calculates the completion ratio p = owned / total for cards in the collection whose rarity is allowed by the given rule.
        // VI: Tính tỉ lệ hoàn thành p = số thẻ đã có / tổng số thẻ của bộ sưu tập trong các độ hiếm mà quy tắc cho phép.
        private float GetSetCompletionRatioForRule(int collectionId, CardGachaSlotRule rule)
        {
            List<CardCollectionCardInfo> setCards = GetCardsInCollection(collectionId);
            int totalEligible = 0;
            int ownedEligible = 0;

            for (int i = 0; i < setCards.Count; i++)
            {
                if (rule.GetRarityWeight(setCards[i].rarity) <= 0f) continue;
                totalEligible++;
                if (Data != null && Data.HasCard(setCards[i].cardId))
                {
                    ownedEligible++;
                }
            }

            if (totalEligible <= 0) return 1f;
            return (float)ownedEligible / totalEligible;
        }

        // EN: Returns the new-card weight multiplier for the given rule and collection.
        // VI: Trả về hệ số nhân trọng số ưu tiên thẻ mới cho quy tắc và bộ sưu tập tương ứng.
        private float GetRuleNewCardMultiplier(CardGachaSlotRule rule, int collectionId)
        {
            if (rule.useNormalSetParams)
            {
                CardSetGachaParam setParam = config.GetSetGachaParam(collectionId);
                if (setParam != null) return Mathf.Max(1f, setParam.newCardMultiplier);
            }
            return Mathf.Max(1f, rule.newCardMultiplier);
        }

        // EN: Selects a card within the specified collection and rarity, weighting unowned cards by newCardMultiplier (defaultWeight 1 * newCardMultiplier).
        // VI: Chọn một thẻ trong bộ sưu tập và độ hiếm chỉ định, nhân trọng số ưu tiên newCardMultiplier cho các thẻ chưa sở hữu.
        private int RollCardInCollectionAndRarity(int collectionId, CardRarity rarity, float newCardMultiplier)
        {
            List<CardCollectionCardInfo> candidates = new();
            List<CardCollectionCardInfo> setCards = GetCardsInCollection(collectionId);
            for (int i = 0; i < setCards.Count; i++)
            {
                if (setCards[i].rarity == rarity)
                {
                    candidates.Add(setCards[i]);
                }
            }

            if (candidates.Count == 0)
            {
                return RollCardIdForRarity(rarity);
            }

            float totalWeight = 0f;
            float[] weights = new float[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                bool isOwned = Data != null && Data.HasCard(candidates[i].cardId);
                weights[i] = isOwned ? 1f : Mathf.Max(1f, newCardMultiplier);
                totalWeight += weights[i];
            }

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < candidates.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative)
                {
                    return candidates[i].cardId;
                }
            }

            return candidates[candidates.Count - 1].cardId;
        }

        // EN: Rolls a card rarity using NormalGachaRule weights.
        // VI: Quay độ hiếm của thẻ dựa trên trọng số NormalGachaRule.
        public CardRarity RollCardRarityWithPity()
        {
            if (config == null) return CardRarity.OneStar;
            config.EnsureDefaultSetup();
            return RollRarityFromGachaRule(config.normalGachaRule);
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

                cumulative += Mathf.Max(0f, config.rarityConfigs[i].dropWeight);
                if (roll <= cumulative)
                {
                    return rc.rarity;
                }
            }

            return minRarity;
        }

        // EN: Selects a card ID of the given rarity, applying preference toward unowned cards.
        // VI: Chọn mã thẻ theo độ hiếm chỉ định, áp dụng ưu tiên cho các thẻ người chơi chưa sở hữu.
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

        // EN: Grants the specified card to the player, updates per-set gacha pity counters, or converts duplicate cards into Spin Tokens (1★=10, 2★=30, 3★=80, 4★=200).
        // VI: Trao thẻ chỉ định cho người chơi, cập nhật bộ đếm bảo hiểm gacha từng bộ, hoặc quy đổi thẻ trùng thành Token quay thưởng (1★=10, 2★=30, 3★=80, 4★=200).
        public CardDropResult GrantCard(int cardId)
        {
            if (Data == null || config == null) return null;
            CardCollectionCardInfo cardInfo = config.GetCardInfo(cardId);
            if (cardInfo == null) return null;

            bool wasOwnedBeforeSetCheck = IsCollectionCompleted(cardInfo.collectionId);
            int totalBefore = GetTotalOwnedCardsCount();

            bool isNewCard = Data.AddCard(cardId);
            UpdateSetGachaProgressOnDraw(cardInfo.collectionId, isNewCard);

            int ticketsEarned = 0;
            if (!isNewCard)
            {
                CardRarityDropConfig rarityCfg = config.GetRarityConfig(cardInfo.rarity);
                ticketsEarned = rarityCfg != null ? Mathf.Max(1, rarityCfg.duplicateTicketReward) : 10;
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

        // EN: Updates the draws-since-last-new-card counter for all 6 collections after a card draw, resetting the counter to 0 for the collection that received a new card.
        // VI: Cập nhật số lượt bốc chưa ra thẻ mới cho cả 6 bộ sưu tập sau mỗi lần nhận thẻ, đặt lại về 0 cho bộ sưu tập vừa nhận được thẻ mới.
        private void UpdateSetGachaProgressOnDraw(int drawnCollectionId, bool isNewCard)
        {
            if (Data == null) return;
            Data.EnsureSetGachaProgressCount();

            for (int setId = 0; setId < TOTAL_COLLECTIONS; setId++)
            {
                if (!IsCollectionCompleted(setId))
                {
                    Data.setDrawsWithoutNewCard[setId]++;
                }
            }

            if (isNewCard && drawnCollectionId >= 0 && drawnCollectionId < TOTAL_COLLECTIONS)
            {
                Data.setDrawsWithoutNewCard[drawnCollectionId] = 0;
            }
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
            Data.claimedSpinSlots ??= new List<int>();
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

        // EN: Returns the active Lucky Spin board configuration based on the player's current board index.
        // VI: Trả về cấu hình bàn quay Lucky Spin đang hoạt động dựa trên chỉ số bàn hiện tại của người chơi.
        public CardSpinBoardInfo GetCurrentSpinBoard()
        {
            if (config == null) return null;
            config.EnsureDefaultSetup();
            int boardIdx = Data != null ? Data.currentSpinBoardIndex : 0;
            return config.GetSpinBoard(boardIdx);
        }

        // EN: Returns the 12 slot configurations for the player's currently active Lucky Spin board.
        // VI: Trả về 12 cấu hình ô cho bàn quay Lucky Spin đang hoạt động của người chơi.
        public List<CardSpinSlotInfo> GetCurrentSpinSlots()
        {
            if (config == null) return null;
            config.EnsureDefaultSetup();
            int boardIdx = Data != null ? Data.currentSpinBoardIndex : 0;
            return config.GetSpinBoardSlots(boardIdx);
        }

        // EN: Advances to the next spin board (or resets repeating Board 4) when all 12 slots on the current board have been claimed.
        // VI: Chuyển sang bàn quay tiếp theo (hoặc làm mới Bàn 4 lặp lại) khi cả 12 ô trên bàn hiện tại đã được nhận hết.
        public bool AdvanceSpinBoardIfCompleted()
        {
            if (Data == null || config == null) return false;
            config.EnsureDefaultSetup();

            List<CardSpinSlotInfo> currentSlots = GetCurrentSpinSlots();
            int totalSlots = currentSlots != null ? currentSlots.Count : 0;
            if (totalSlots <= 0) return false;

            for (int i = 0; i < totalSlots; i++)
            {
                if (!Data.IsSpinSlotClaimed(i))
                {
                    return false;
                }
            }

            Data.ResetSpinClaimedSlots();
            Data.currentSpinSetCount = 0;

            int maxBoardIdx = config.spinBoards != null && config.spinBoards.Count > 0
                ? config.spinBoards.Count - 1
                : 0;
            if (Data.currentSpinBoardIndex < maxBoardIdx)
            {
                Data.currentSpinBoardIndex++;
            }

            Save();
            OnDataChanged?.Invoke();
            return true;
        }

        // EN: Checks whether the specified spin slot index has already been rewarded and disabled on the current board.
        // VI: Kiểm tra xem ô vòng quay tại chỉ số chỉ định đã được nhận thưởng và vô hiệu hóa trên bàn hiện tại hay chưa.
        public bool IsSpinSlotClaimed(int slotIndex)
        {
            return Data != null && Data.IsSpinSlotClaimed(slotIndex);
        }

        // EN: Checks whether at least one slot on the current spin board has not yet been rewarded.
        // VI: Kiểm tra xem trên bàn quay hiện tại còn ít nhất một ô phần thưởng chưa được nhận hay không.
        public bool HasAvailableSpinSlot()
        {
            if (Data == null || config == null) return false;
            config.EnsureDefaultSetup();
            AdvanceSpinBoardIfCompleted();

            List<CardSpinSlotInfo> currentSlots = GetCurrentSpinSlots();
            int totalSlots = currentSlots != null ? currentSlots.Count : 0;
            for (int i = 0; i < totalSlots; i++)
            {
                if (!Data.IsSpinSlotClaimed(i))
                {
                    return true;
                }
            }
            return false;
        }

        // EN: Checks whether the player has enough duplicate tickets and an available slot to perform a spin.
        // VI: Kiểm tra xem người chơi có đủ số vé trùng lặp và còn ô trống chưa nhận để thực hiện một lượt quay hay không.
        public bool CanSpin()
        {
            return Data != null && Data.ticketAmount >= SpinCostTickets && HasAvailableSpinSlot();
        }

        // EN: Spends tickets to spin the 12-slot board using subPool 1->5 priority, marks the landed slot as disabled, and grants its reward.
        // VI: Tiêu vé để quay bàn 12 ô theo thứ tự ưu tiên subPool 1->5, đánh dấu ô trúng thưởng bị vô hiệu hóa và trao phần thưởng của ô đó.
        public bool TryExecuteSpin(
            out int landedSlotIndex,
            out CardSpinSlotInfo landedSlot,
            out CardDropResult spinCardDrop)
        {
            landedSlotIndex = 0;
            landedSlot = null;
            spinCardDrop = null;

            if (!CanSpin() || config == null) return false;
            config.EnsureDefaultSetup();

            List<CardSpinSlotInfo> currentSlots = GetCurrentSpinSlots();
            if (currentSlots == null || currentSlots.Count == 0) return false;

            int spinCost = SpinCostTickets;
            Data.ticketAmount -= spinCost;
            landedSlotIndex = RollSpinSlotIndex(currentSlots);
            landedSlot = currentSlots[landedSlotIndex];
            Data.MarkSpinSlotClaimed(landedSlotIndex);

            if (landedSlot.slotType == CardSpinSlotType.Resource)
            {
                GrantResourceReward(landedSlot.resourceReward, "CardCollection_Spin");
            }
            else if (landedSlot.slotType == CardSpinSlotType.SpecificCard && landedSlot.specificCardId >= 0)
            {
                spinCardDrop = GrantCard(landedSlot.specificCardId);
            }
            else
            {
                int cardId = RollCardIdForRarity(landedSlot.cardRarity);
                spinCardDrop = GrantCard(cardId);
            }

            Data.currentSpinSetCount = Data.claimedSpinSlots.Count;
            Data.totalSpinCount++;

            Save();
            OnDataChanged?.Invoke();
            return true;
        }

        // EN: Performs a weighted random roll across unclaimed slots in the lowest remaining subPool (1->5) on the active spin board.
        // VI: Thực hiện quay ngẫu nhiên có trọng số trên các ô chưa nhận thuộc subPool thấp nhất còn lại (1->5) trên bàn quay hiện tại.
        private int RollSpinSlotIndex(List<CardSpinSlotInfo> currentSlots)
        {
            if (currentSlots == null || currentSlots.Count == 0) return 0;

            int minSubPool = int.MaxValue;
            for (int i = 0; i < currentSlots.Count; i++)
            {
                if (IsSpinSlotClaimed(i)) continue;
                int pool = Mathf.Max(1, currentSlots[i].subPool);
                if (pool < minSubPool)
                {
                    minSubPool = pool;
                }
            }

            if (minSubPool == int.MaxValue) return 0;

            List<int> availableIndices = new();
            float totalWeight = 0f;
            for (int i = 0; i < currentSlots.Count; i++)
            {
                if (IsSpinSlotClaimed(i)) continue;
                int pool = Mathf.Max(1, currentSlots[i].subPool);
                if (pool != minSubPool) continue;

                availableIndices.Add(i);
                totalWeight += Mathf.Max(0f, currentSlots[i].weight);
            }

            if (availableIndices.Count == 0) return 0;
            if (totalWeight <= 0f) return availableIndices[UnityEngine.Random.Range(0, availableIndices.Count)];

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < availableIndices.Count; i++)
            {
                int slotIdx = availableIndices[i];
                cumulative += Mathf.Max(0f, currentSlots[slotIdx].weight);
                if (roll <= cumulative)
                {
                    return slotIdx;
                }
            }

            return availableIndices[availableIndices.Count - 1];
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

        // EN: Opens a card pack based on its per-slot Gacha rules (or fallback rarity rules) and returns the granted cards.
        // VI: Mở gói thẻ dựa trên quy tắc Gacha của từng khe thẻ (hoặc quy tắc độ hiếm dự phòng) và trả về danh sách các thẻ nhận được.
        public List<CardDropResult> OpenCardPackInfo(CardPackShopInfo packInfo)
        {
            if (packInfo == null) return null;
            if (config != null) config.EnsureDefaultSetup();

            if (packInfo.slotRules != null && packInfo.slotRules.Count > 0)
            {
                List<CardDropResult> slotResults = new(packInfo.slotRules.Count);
                for (int i = 0; i < packInfo.slotRules.Count; i++)
                {
                    int cardId = RollCardByGachaRule(packInfo.slotRules[i]);
                    CardDropResult res = GrantCard(cardId);
                    if (res != null) slotResults.Add(res);
                }
                return slotResults;
            }

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
                int cardId = RollCardByGachaRule(config != null ? config.normalGachaRule : null);
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

