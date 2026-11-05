using System;
using System.Collections.Generic;

namespace GamePackages
{
    [Serializable]
    public class CardDropResult
    {
        public int cardId;
        public int collectionId;
        public CardRarity rarity;
        public bool isDuplicate;
        public int ticketsConverted;
        public bool completedCollection;
        public bool completedAllCollections;
    }

    [Serializable]
    public class CardCollectionData
    {
        public bool isUnlocked;
        public string unlockTimeIso = "";
        public string expireTimeIso = "";
        public bool isShowedInfo;
        public bool isDoneTutorial;
        public bool isClaimedGrandReward;
        public int ticketAmount;
        public int currentSpinSetCount;
        public int totalSpinCount;
        public int firstTryWinsWithoutTier3;
        public int firstTryWinsWithoutTier4;
        public int pendingDroppedCardId = -1;
        public bool pendingDroppedCardWasDuplicate;
        public int pendingDroppedTicketsEarned;
        public List<int> ownedCardIds = new();
        public List<int> claimedCollectionIds = new();
        public List<int> claimedSpinSlots = new();

        // EN: Initializes a new CardCollectionData instance with default values.
        // VI: Khởi tạo đối tượng CardCollectionData mới với các giá trị mặc định.
        public CardCollectionData()
        {
            Reset();
        }

        // EN: Resets all event progress, timers, owned cards, and spin counters to their initial states.
        // VI: Đặt lại toàn bộ tiến độ sự kiện, bộ đếm thời gian, thẻ sở hữu và lượt quay về trạng thái ban đầu.
        public void Reset()
        {
            isUnlocked = false;
            unlockTimeIso = "";
            expireTimeIso = "";
            isShowedInfo = false;
            isDoneTutorial = false;
            isClaimedGrandReward = false;
            ticketAmount = 0;
            currentSpinSetCount = 0;
            totalSpinCount = 0;
            firstTryWinsWithoutTier3 = 0;
            firstTryWinsWithoutTier4 = 0;
            pendingDroppedCardId = -1;
            pendingDroppedCardWasDuplicate = false;
            pendingDroppedTicketsEarned = 0;
            ownedCardIds.Clear();
            claimedCollectionIds.Clear();
            claimedSpinSlots.Clear();
        }

        // EN: Starts a new 14-day event cycle with the given start and expiration timestamps while preserving tutorial completion.
        // VI: Bắt đầu chu kỳ sự kiện 14 ngày mới với mốc thời gian bắt đầu và kết thúc được truyền vào, giữ nguyên trạng thái hướng dẫn.
        public void StartNewCycle(DateTime unlockTime, DateTime expireTime)
        {
            isUnlocked = true;
            unlockTimeIso = unlockTime.ToString("O");
            expireTimeIso = expireTime.ToString("O");
            isShowedInfo = false;
            isClaimedGrandReward = false;
            ticketAmount = 0;
            currentSpinSetCount = 0;
            totalSpinCount = 0;
            firstTryWinsWithoutTier3 = 0;
            firstTryWinsWithoutTier4 = 0;
            pendingDroppedCardId = -1;
            pendingDroppedCardWasDuplicate = false;
            pendingDroppedTicketsEarned = 0;
            ownedCardIds.Clear();
            claimedCollectionIds.Clear();
            claimedSpinSlots.Clear();
        }

        // EN: Checks whether the player already owns the card with the given ID.
        // VI: Kiểm tra xem người chơi đã sở hữu thẻ có ID chỉ định hay chưa.
        public bool HasCard(int cardId)
        {
            return ownedCardIds != null && ownedCardIds.Contains(cardId);
        }

        // EN: Adds the card ID to the player's collection if not already owned and returns true when newly added.
        // VI: Thêm ID thẻ vào bộ sưu tập của người chơi nếu chưa sở hữu và trả về true khi thẻ là thẻ mới.
        public bool AddCard(int cardId)
        {
            ownedCardIds ??= new List<int>();
            if (ownedCardIds.Contains(cardId))
            {
                return false;
            }
            ownedCardIds.Add(cardId);
            return true;
        }

        // EN: Checks whether the completion reward for the given collection index has already been claimed.
        // VI: Kiểm tra xem phần thưởng hoàn thành của bộ sưu tập theo chỉ số đã được nhận hay chưa.
        public bool IsCollectionClaimed(int collectionId)
        {
            return claimedCollectionIds != null && claimedCollectionIds.Contains(collectionId);
        }

        // EN: Checks whether the spin slot at the given index has already been rewarded and disabled.
        // VI: Kiểm tra xem ô vòng quay tại chỉ số chỉ định đã được nhận thưởng và vô hiệu hóa hay chưa.
        public bool IsSpinSlotClaimed(int slotIndex)
        {
            return claimedSpinSlots != null && claimedSpinSlots.Contains(slotIndex);
        }

        // EN: Marks the spin slot at the given index as rewarded so it will be disabled and skipped in subsequent spins.
        // VI: Đánh dấu ô vòng quay tại chỉ số chỉ định là đã nhận thưởng để vô hiệu hóa và bỏ qua trong các lượt quay tiếp theo.
        public void MarkSpinSlotClaimed(int slotIndex)
        {
            claimedSpinSlots ??= new List<int>();
            if (!claimedSpinSlots.Contains(slotIndex))
            {
                claimedSpinSlots.Add(slotIndex);
            }
        }

        // EN: Clears any pending card drop notification stored from the last level win.
        // VI: Xóa thông tin thẻ rơi đang chờ hiển thị từ lần thắng màn trước đó.
        public void ClearPendingCardDrop()
        {
            pendingDroppedCardId = -1;
            pendingDroppedCardWasDuplicate = false;
            pendingDroppedTicketsEarned = 0;
        }

        // EN: Persists the current player data through ProfileManager.
        // VI: Lưu dữ liệu người chơi hiện tại thông qua ProfileManager.
        public void SaveData()
        {
            ProfileManager.instance?.SaveData();
        }
    }
}

