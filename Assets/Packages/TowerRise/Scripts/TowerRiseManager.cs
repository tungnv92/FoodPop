using DG.Tweening;
using Longvm.RewardAnimator;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GamePackages
{
    [Serializable]
    public class TowerRiseManager : ContextData<TowerRiseData>, IFeatureNewDay, IFeatureUpdate
    {
        // EN: Signals persisted UI-owned feature state changes without exposing the model.
        // VI: Báo thay đổi trạng thái tính năng do giao diện yêu cầu sau khi lưu mà không lộ model.
        public event Action StateChanged;

        // EN: Reads isDoneTutorial without exposing mutable feature data.
        // VI: Đọc isDoneTutorial mà không lộ dữ liệu tính năng có thể sửa.
        public bool HasCompletedTutorial => Data != null ? Data.isDoneTutorial : false;

        // EN: Persists isDoneTutorial when first marked by the feature UI.
        // VI: Lưu isDoneTutorial khi giao diện tính năng đánh dấu lần đầu.
        public void CompleteTutorial()
        {
            if (Data == null || Data.isDoneTutorial) return;
            Data.isDoneTutorial = true;
            Save();
            StateChanged?.Invoke();
        }
        // EN: Reads isShowedInfo without exposing mutable feature data.
        // VI: Đọc isShowedInfo mà không lộ dữ liệu tính năng có thể sửa.
        public bool HasShownInfo => Data != null ? Data.isShowedInfo : false;

        // EN: Persists isShowedInfo when first marked by the feature UI.
        // VI: Lưu isShowedInfo khi giao diện tính năng đánh dấu lần đầu.
        public void MarkInfoShown()
        {
            if (Data == null || Data.isShowedInfo) return;
            Data.isShowedInfo = true;
            Save();
            StateChanged?.Invoke();
        }
        // EN: Reads isStarted without exposing mutable feature data.
        // VI: Đọc isStarted mà không lộ dữ liệu tính năng có thể sửa.
        public bool HasStarted => Data != null ? Data.isStarted : false;
        // EN: Reads lastMilestone without exposing mutable feature data.
        // VI: Đọc lastMilestone mà không lộ dữ liệu tính năng có thể sửa.
        public int LastViewedProgress => Data != null ? Data.lastMilestone : 0;
        // EN: Reads currentMilestone without exposing mutable feature data.
        // VI: Đọc currentMilestone mà không lộ dữ liệu tính năng có thể sửa.
        public int CurrentProgress => Data != null ? Data.currentMilestone : 0;
        // EN: Reads totalMilestone without exposing mutable feature data.
        // VI: Đọc totalMilestone mà không lộ dữ liệu tính năng có thể sửa.
        public int TotalProgress => Data != null ? Data.totalMilestone : 0;

        // EN: Marks a reached milestone claimed before its rewards are presented, without exposing the saved list.
        // VI: Đánh dấu đã nhận mốc hoàn thành trước khi trình diễn thưởng mà không lộ danh sách lưu.
        public void MarkMilestoneClaimed(int index)
        {
            if (Data == null || index < 0 || index >= GetMileStoneCount() || !IsMilestoneReached(index) || IsMilestoneClaimed(index)) return;
            Data.claimedIndexes.Add(index);
            Save();
            StateChanged?.Invoke();
        }

        // EN: Coordinates Instance using encapsulated player and feature APIs instead of accessing live save models.
        // VI: Điều phối Instance qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
        public static TowerRiseManager Instance => ProfileManager.instance.GameFeatureContext != null
            ? ProfileManager.instance.GameFeatureContext.TowerRiseManager
            : null;

        public static TowerRiseManager Ins => Instance;

        public TowerRiseConfig TowerRiseConfig;
        public TowerRiseConfig towerRiseConfig
        {
            get => TowerRiseConfig;
            set => TowerRiseConfig = value;
        }

        // EN: Keeps the mutable feature model private while public callers use value queries.
        // VI: Giữ model tính năng có thể sửa ở phạm vi riêng, nơi gọi public dùng truy vấn giá trị.
        private TowerRiseData towerRiseData => Data;

        public int currentMilestone;
        public int currentStepinMilestone;
        public bool isWin;
        public bool isFail;
        public int failAnimFromStep;

        public int lastCompletedMilestoneIndex = -1;
        public bool pendingIntroduceScroll;
        public bool isClaimingRewards;

        public int UnlockLevel => TowerRiseConfig != null ? TowerRiseConfig.LevelActiveFeature : 85;
        public int unlockLevelIndex => UnlockLevel;

        // EN: Checks the unlock level through read-only player manager queries.
        // VI: Kiểm tra level mở khóa qua truy vấn chỉ đọc của manager người chơi.
        // EN: Checks the unlock level through read-only player manager queries.
        // VI: Kiểm tra level mở khóa qua truy vấn chỉ đọc của manager người chơi.
        public bool IsLevelUnlocked => ProfileManager.instance.PlayerDataManager.IsInitialized &&
                                       ProfileManager.instance.PlayerDataManager.GetCurrentLevel() >= UnlockLevel;

        public bool isActive => IsLevelUnlocked && (TowerRiseConfig == null || TowerRiseConfig.isFeatureEnabled);
        public bool IsEnabled => TowerRiseConfig != null && TowerRiseConfig.isFeatureEnabled;

        // EN: Binds the saved model through internal initialization and initializes this feature's state.
        // VI: Gắn model lưu qua khởi tạo nội bộ và khởi tạo trạng thái của tính năng này.
        internal override void SetContext(TowerRiseData data)
        {
            base.SetContext(data ?? throw new System.ArgumentNullException(nameof(data)));

            MigrateLastViewedData();
            EnsureTotalMilestone();
            bool isDataChanged = false;
            NormalizeProgressData(ref isDataChanged);

            if (towerRiseData.isQuitLevelLastSession)
            {
                OnFailLevel();
                towerRiseData.isQuitLevelLastSession = false;
                isDataChanged = true;
            }

            if (isDataChanged)
            {
                Save();
            }

            RefreshCurrentMilestoneFromData();
            CheckExpire();
        }

        public void OnNewDay()
        {
            CheckExpire();
        }

        public void OnUpdate()
        {
            CheckExpire();
            TryAutoCompleteFinishedEventIfNoRewards();
        }

        // EN: Coordinates GetCurrentTime using encapsulated player and feature APIs instead of accessing live save models.
        // VI: Điều phối GetCurrentTime qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
        public DateTime GetCurrentTime()
        {
            if (ProfileManager.instance.TimeManager != null)
            {
                return ProfileManager.instance.TimeManager.GetNow();
            }
            return DateTime.Now;
        }

        public void OnInitt()
        {
            towerRiseConfig?.AssignIndexes();
            RefreshCurrentMilestoneFromData();
        }

        public void OnLoadLevel()
        {
            isWin = false;
            isFail = false;
            failAnimFromStep = 0;
        }

        public int GetMileStoneCount()
        {
            return towerRiseConfig != null ? towerRiseConfig.GetMileStoneCount() : 0;
        }

        public int GetConfigTotalWinAmount()
        {
            return towerRiseConfig != null ? towerRiseConfig.GetTotalWinAmount() : 0;
        }

        void EnsureTotalMilestone()
        {
            if (towerRiseData == null || !towerRiseData.isStarted) return;

            int configTotal = GetConfigTotalWinAmount();
            if (configTotal <= 0) return;

            if (towerRiseData.totalMilestone <= 0)
                towerRiseData.totalMilestone = configTotal;
        }

        void NormalizeProgressData(ref bool isDataChanged)
        {
            if (towerRiseData == null) return;

            if (towerRiseData.totalMilestone > 0 && towerRiseData.currentMilestone > towerRiseData.totalMilestone)
            {
                towerRiseData.currentMilestone = towerRiseData.totalMilestone;
                isDataChanged = true;
            }

            if (towerRiseData.lastMilestone > towerRiseData.currentMilestone)
            {
                towerRiseData.lastMilestone = towerRiseData.currentMilestone;
                isDataChanged = true;
            }
        }

        public int GetReachedMilestoneIndex()
        {
            return GetReachedMilestoneIndexFromProgress(GetTotalCurrentWins());
        }

        public int GetReachedMilestoneIndexFromProgress(int progress)
        {
            if (towerRiseConfig == null || towerRiseData == null) return 0;

            int count = GetMileStoneCount();
            if (count <= 0) return 0;
            if (IsCompleteAllMileStones()) return count;

            for (int i = 0; i < count; i++)
            {
                if (progress < towerRiseConfig.GetCumulativeWinAmount(i))
                    return i;
            }

            return count;
        }

        int GetBaseProgressForMilestoneIndex(int milestoneIndex)
        {
            if (towerRiseConfig == null || milestoneIndex <= 0) return 0;
            return towerRiseConfig.GetCumulativeWinAmount(milestoneIndex - 1);
        }

        public void RefreshCurrentMilestoneFromData()
        {
            if (towerRiseData == null || towerRiseConfig == null)
            {
                currentMilestone = 0;
                currentStepinMilestone = 0;
                return;
            }

            int totalProgress = towerRiseData.currentMilestone;
            currentMilestone = GetReachedMilestoneIndexFromProgress(totalProgress);
            currentStepinMilestone = totalProgress - GetBaseProgressForMilestoneIndex(currentMilestone);
        }

        bool HasStreakInCurrentSegment()
        {
            if (towerRiseData == null) return false;
            return towerRiseData.currentMilestone > GetBaseProgressForMilestoneIndex(GetReachedMilestoneIndex());
        }

        public TowerRiseMileStoneInfo GetNextMilestoneInfo()
        {
            if (towerRiseConfig == null || IsCompleteAllMileStones()) return null;
            return towerRiseConfig.GetMileStoneInfo(GetReachedMilestoneIndex());
        }

        public TowerRiseMileStoneInfo GetMilestoneInfo(int index)
        {
            return towerRiseConfig?.GetMileStoneInfo(index);
        }

        public int GetNextMilestoneWinAmount()
        {
            return GetNextMilestoneInfo()?.winAmount ?? 0;
        }

        public int GetCurrentSegmentWinAmount()
        {
            if (IsCompleteAllMileStones())
            {
                int count = GetMileStoneCount();
                if (count <= 0) return 0;
                return GetMilestoneInfo(count - 1)?.winAmount ?? 0;
            }
            return GetNextMilestoneWinAmount();
        }

        public int GetSegmentWinAmountForMilestoneIndex(int milestoneIndex)
        {
            return GetMilestoneInfo(milestoneIndex)?.winAmount ?? 0;
        }

        public int GetStepInMilestoneFromProgress(int progress)
        {
            int milestoneIndex = GetReachedMilestoneIndexFromProgress(progress);
            return progress - GetBaseProgressForMilestoneIndex(milestoneIndex);
        }

        public int GetLastViewedStepInMilestone()
        {
            if (towerRiseData == null) return 0;
            return GetStepInMilestoneFromProgress(towerRiseData.lastMilestone);
        }

        public bool IsWinAnimCompletingMilestone()
        {
            return isWin && currentStepinMilestone == 0 && lastCompletedMilestoneIndex >= 0;
        }

        public int GetWinAnimFromStep()
        {
            if (currentStepinMilestone > 0) return currentStepinMilestone - 1;

            if (lastCompletedMilestoneIndex >= 0)
            {
                int amount = GetSegmentWinAmountForMilestoneIndex(lastCompletedMilestoneIndex);
                if (amount > 0) return amount - 1;
            }

            return 0;
        }

        public int GetWinAnimSegmentAmount()
        {
            if (IsWinAnimCompletingMilestone())
                return GetSegmentWinAmountForMilestoneIndex(lastCompletedMilestoneIndex);
            return GetCurrentSegmentWinAmount();
        }

        public int GetEventDisplayStep()
        {
            if (isWin) return GetWinAnimFromStep();
            if (isFail) return failAnimFromStep;
            return GetLastViewedStepInMilestone();
        }

        public bool ShouldPlayEventWinAnim()
        {
            if (!IsEventStarted() || towerRiseData == null) return false;
            return isWin || towerRiseData.lastMilestone < towerRiseData.currentMilestone;
        }

        public bool ShouldPlayEventFailAnim()
        {
            if (!IsEventStarted() || towerRiseData == null) return false;
            return isFail || towerRiseData.lastMilestone > towerRiseData.currentMilestone;
        }

        public bool IsCompleteAllMileStones()
        {
            if (towerRiseData == null) return false;
            return towerRiseData.totalMilestone > 0
                && towerRiseData.currentMilestone >= towerRiseData.totalMilestone;
        }

        public bool IsMilestoneReached(int index)
        {
            return GetReachedMilestoneIndex() > index;
        }

        public bool IsMilestoneClaimed(int index)
        {
            return towerRiseData != null && towerRiseData.claimedIndexes.Contains(index);
        }

        public bool IsMilestoneClaimable(int index)
        {
            return IsMilestoneReached(index) && !IsMilestoneClaimed(index);
        }

        public bool IsMilestoneReachedAtProgress(int index, float totalProgress)
        {
            if (towerRiseConfig == null || index < 0 || index >= GetMileStoneCount()) return false;
            return totalProgress >= towerRiseConfig.GetCumulativeWinAmount(index);
        }

        public bool IsMilestoneClaimableAtProgress(int index, float totalProgress)
        {
            return IsMilestoneReachedAtProgress(index, totalProgress) && !IsMilestoneClaimed(index);
        }

        public bool IsEventStarted()
        {
            if (IsEventFinishedPending()) return false;
            return isActive && towerRiseData != null && towerRiseData.isStarted && IsEventScheduleActive();
        }

        public bool IsEventFinishedPending()
        {
            return towerRiseData != null && towerRiseData.isEventFinishedPending;
        }

        public bool HasFinishedRewardsToClaim()
        {
            return IsEventFinishedPending() && HasPendingFinishedClaimMilestones();
        }

        bool HasPendingFinishedClaimMilestones()
        {
            return towerRiseData?.finishedPendingClaimMilestoneIndexes != null
                && towerRiseData.finishedPendingClaimMilestoneIndexes.Count > 0;
        }

        public int GetFinishedPendingClaimCount()
        {
            if (towerRiseData?.finishedPendingClaimMilestoneIndexes == null) return 0;
            return towerRiseData.finishedPendingClaimMilestoneIndexes.Count;
        }

        public bool IsEventScheduleActive()
        {
            DayOfWeek day = GetCurrentTime().DayOfWeek;
            return day == DayOfWeek.Tuesday
                || day == DayOfWeek.Wednesday
                || day == DayOfWeek.Thursday;
        }

        public bool ShouldShowHomeButton()
        {
            if (!isActive) return false;
            if (IsEventFinishedPending()) return HasFinishedRewardsToClaim();
            if (!IsEventScheduleActive())
                return towerRiseData != null && towerRiseData.isStarted && HasUnclaimedReachedMilestones();
            if (HasUnclaimedReachedMilestones()) return true;
            return !IsCompleteAllMileStones();
        }

        bool HasUnclaimedReachedMilestones()
        {
            if (towerRiseData == null || !towerRiseData.isStarted) return false;

            int count = GetMileStoneCount();
            for (int i = 0; i < count && i < GetReachedMilestoneIndex(); i++)
            {
                if (!IsMilestoneClaimed(i)) return true;
            }

            return false;
        }

        public bool ShouldShowHomeLock()
        {
            return IsEventScheduleActive() && !isActive;
        }

        public void StartEvent()
        {
            if (towerRiseData == null) return;

            towerRiseData.isStarted = true;
            towerRiseData.totalMilestone = GetConfigTotalWinAmount();
            towerRiseData.currentMilestone = 0;
            towerRiseData.lastMilestone = 0;
            pendingIntroduceScroll = true;
            Save();
            RefreshCurrentMilestoneFromData();
        }

        public void OnWinLevel()
        {
            isWin = true;
            lastCompletedMilestoneIndex = -1;
            if (!IsEventStarted()) return;
            if (IsCompleteAllMileStones()) return;
            if (towerRiseData.totalMilestone <= 0) return;

            bool hadStaleFailReplay = towerRiseData.lastMilestone > towerRiseData.currentMilestone;

            int oldReached = GetReachedMilestoneIndex();
            towerRiseData.currentMilestone = Mathf.Min(
                towerRiseData.currentMilestone + 1,
                towerRiseData.totalMilestone);

            if (hadStaleFailReplay)
                towerRiseData.lastMilestone = towerRiseData.currentMilestone - 1;

            int newReached = GetReachedMilestoneIndex();
            if (newReached > oldReached)
                lastCompletedMilestoneIndex = oldReached;

            Save();
            RefreshCurrentMilestoneFromData();
        }

        public void OnFailLevel()
        {
            if (!IsEventStarted()) return;
            if (!HasStreakInCurrentSegment()) return;

            isFail = true;
            failAnimFromStep = currentStepinMilestone;

            towerRiseData.currentMilestone = GetBaseProgressForMilestoneIndex(GetReachedMilestoneIndex());
            Save();
            RefreshCurrentMilestoneFromData();
        }

        public bool ShouldWarningWhenLose()
        {
            return IsEventStarted() && HasStreakInCurrentSegment() && !IsCompleteAllMileStones();
        }

        public void ClearLastCompletedMilestoneIndex()
        {
            lastCompletedMilestoneIndex = -1;
        }

        public int GetPlayerPlatformIndex()
        {
            int count = GetMileStoneCount();
            if (count <= 0) return 0;
            if (IsCompleteAllMileStones()) return count;
            return Mathf.Clamp(currentMilestone, 0, count - 1);
        }

        public static int MapRewardToAnimatorType(ResourceType type)
        {
            return type switch
            {
                ResourceType.Coin => 0,             // Coin
                ResourceType.UnlimitedLives => 1,   // UnlimitedLife
                ResourceType.BoosterClean => 2,   // Booster_1
                ResourceType.BoosterShuffle => 3,   // Booster_2
                ResourceType.BoosterPickup => 4,    // Booster_3
                ResourceType.BoosterAutoComplete => 5, // Booster_4
                _ => 0
            };
        }

        // EN: Claims the reward for the specified TowerRise milestone index and plays the reward animation if available.
        // VI: Nhận phần thưởng cho chỉ số mốc TowerRise chỉ định và phát hiệu ứng phần thưởng nếu có.
        public void ClaimMilestone(int index, Action onComplete = null)
        {
            if (!IsMilestoneClaimable(index))
            {
                onComplete?.Invoke();
                return;
            }

            var mileStoneInfo = GetMilestoneInfo(index);
            towerRiseData.claimedIndexes.Add(index);
            Save();

            if (mileStoneInfo == null || mileStoneInfo.rewards == null || mileStoneInfo.rewards.Count == 0)
            {
                if (IsEventFinishedPending())
                    OnFinishedMilestoneClaimed(index);
                onComplete?.Invoke();
                return;
            }

            RewardAnimator rwAnimator = UIManager.Ins != null && UIManager.Ins.GetPanel() != null
                ? UIManager.Ins.GetPanel().RewardAnimator
                : null;

            //if (AudioManager.Ins != null)
            {
            //    AudioManager.Ins.PlaySound(SoundType.CollectReward);
            }

            if (rwAnimator != null)
            {
                rwAnimator.Init(ChestType.None);
                foreach (var reward in mileStoneInfo.rewards)
                {
                    var r = reward;
                    rwAnimator.Add(MapRewardToAnimatorType(r.type), r.value, onReceiveReward: () =>
                    {
                        ClaimRewardResource(r, "TowerRise");
                    });
                }
                rwAnimator.OnComplete(() =>
                {
                    if (IsEventFinishedPending())
                        OnFinishedMilestoneClaimed(index);
                    onComplete?.Invoke();
                });
                rwAnimator.Play();
            }
            else
            {
                foreach (var reward in mileStoneInfo.rewards)
                {
                    ClaimRewardResource(reward, "TowerRise");
                }

                if (IsEventFinishedPending())
                    OnFinishedMilestoneClaimed(index);
                onComplete?.Invoke();
            }
        }

        // EN: Grants supported rewards through dedicated APIs and warns about unknown resource types.
        // VI: Cấp thưởng hợp lệ qua API chuyên biệt và cảnh báo loại tài nguyên không xác định.
        // EN: Coordinates ClaimRewardResource using encapsulated player and feature APIs instead of accessing live save models.
        // VI: Điều phối ClaimRewardResource qua API người chơi và tính năng đã đóng gói thay vì truy cập model lưu đang dùng.
        public static void ClaimRewardResource(ResourceGame reward, string placement = "TowerRise")
        {
            if (reward == null) return;
            var pdm = ProfileManager.instance.PlayerDataManager;

            switch (reward.type)
            {
                case ResourceType.Coin:
                    pdm.OnAddCoin(reward.value, placement);
                    break;
                case ResourceType.UnlimitedLives:
                    pdm.AddUnlimitedLiveTime(reward.value, placement);
                    break;
                case ResourceType.BoosterClean:
                case ResourceType.BoosterShuffle:
                case ResourceType.BoosterPickup:
                case ResourceType.BoosterAutoComplete:
                    pdm.OnAddBooster(reward.type, reward.value, placement);
                    break;
                default:
                    Debug.LogWarning($"Unsupported reward resource: {reward.type}");
                    break;
            }
        }

        public void OnFinishedMilestoneClaimed(int index, bool autoCompleteWhenEmpty = true)
        {
            if (!IsEventFinishedPending()) return;

            RemoveFinishedPendingClaimMilestone(index);
            RefreshHomeTowerRise();

            if (UIManager.Ins != null && UIManager.Ins.IsPopupOpen(PopupId.TowerRise))
                UIManager.Ins.GetPopup<TowerRise>(PopupId.TowerRise)?.RefreshFinishedPresentation();

            if (autoCompleteWhenEmpty && !HasFinishedRewardsToClaim())
                CompleteFinishedEventAfterClaims();
        }

        void RemoveFinishedPendingClaimMilestone(int index)
        {
            if (towerRiseData?.finishedPendingClaimMilestoneIndexes == null) return;

            towerRiseData.finishedPendingClaimMilestoneIndexes.Remove(index);
            Save();
        }

        void PruneFinishedPendingClaims()
        {
            if (towerRiseData?.finishedPendingClaimMilestoneIndexes == null) return;

            for (int i = towerRiseData.finishedPendingClaimMilestoneIndexes.Count - 1; i >= 0; i--)
            {
                int milestoneIndex = towerRiseData.finishedPendingClaimMilestoneIndexes[i];
                if (IsMilestoneClaimed(milestoneIndex))
                    towerRiseData.finishedPendingClaimMilestoneIndexes.RemoveAt(i);
            }

            Save();
        }

        void PrepareFinishedPendingClaims()
        {
            towerRiseData.finishedPendingClaimMilestoneIndexes ??= new List<int>();
            towerRiseData.finishedPendingClaimMilestoneIndexes.Clear();

            int count = GetMileStoneCount();
            for (int i = 0; i < count && i < GetReachedMilestoneIndex(); i++)
            {
                if (IsMilestoneClaimed(i)) continue;

                var mileStoneInfo = GetMilestoneInfo(i);
                if (mileStoneInfo?.rewards == null || mileStoneInfo.rewards.Count == 0)
                {
                    if (!towerRiseData.claimedIndexes.Contains(i))
                        towerRiseData.claimedIndexes.Add(i);
                    continue;
                }

                towerRiseData.finishedPendingClaimMilestoneIndexes.Add(i);
            }

            Save();
        }

        public void MarkEventFinished()
        {
            if (towerRiseData == null || towerRiseData.isEventFinishedPending) return;
            if (!towerRiseData.isStarted) return;

            PrepareFinishedPendingClaims();

            isWin = false;
            isFail = false;
            failAnimFromStep = 0;
            lastCompletedMilestoneIndex = -1;

            if (!HasPendingFinishedClaimMilestones())
            {
                ResetEvent();
                return;
            }

            towerRiseData.isEventFinishedPending = true;
            Save();

            NotifyEventFinished();
            RefreshHomeTowerRise();
        }

        public void ResetEvent()
        {
            if (towerRiseData == null) return;

            towerRiseData.unclaimedRewardInfos.Clear();
            towerRiseData.finishedPendingClaimMilestoneIndexes?.Clear();
            towerRiseData.isEventFinishedPending = false;

            ClearDataNewWeek();
            InitNewExpire();

            isWin = false;
            isFail = false;
            failAnimFromStep = 0;
            lastCompletedMilestoneIndex = -1;

            RefreshHomeTowerRise();
        }

        public void BeginFinishedClaimChain(Action onComplete = null)
        {
            if (!IsEventFinishedPending() || !HasFinishedRewardsToClaim())
            {
                onComplete?.Invoke();
                return;
            }

            PruneFinishedPendingClaims();
            if (!HasFinishedRewardsToClaim())
            {
                CompleteFinishedEventAfterClaims();
                onComplete?.Invoke();
                return;
            }

            var pendingMilestones = new List<int>(towerRiseData.finishedPendingClaimMilestoneIndexes);
            var rewards = BuildMergedFinishedRewards(pendingMilestones, out int skinMilestoneIndex);

            foreach (int milestoneIndex in pendingMilestones)
            {
                if (!towerRiseData.claimedIndexes.Contains(milestoneIndex))
                    towerRiseData.claimedIndexes.Add(milestoneIndex);
            }
            towerRiseData.finishedPendingClaimMilestoneIndexes.Clear();
            Save();
            RefreshHomeTowerRise();

            if (rewards.Count == 0)
            {
                CompleteFinishedEventAfterClaims();
                onComplete?.Invoke();
                return;
            }

            var claimBox = UIManager.Ins.OpenPopup<TowerRiseClaimBox>(PopupId.TowerRiseClaimBox);
            if (claimBox != null)
            {
                claimBox.OnInitt(rewards, skinMilestoneIndex);
                claimBox.OnClaimComplete = () =>
                {
                    CompleteFinishedEventAfterClaims();
                    onComplete?.Invoke();
                };
            }
            else
            {
                foreach (var reward in rewards)
                {
                    ClaimRewardResource(reward, "TowerRiseFinished");
                }
                CompleteFinishedEventAfterClaims();
                onComplete?.Invoke();
            }
        }

        List<ResourceGame> BuildMergedFinishedRewards(List<int> milestoneIndexes, out int skinMilestoneIndex)
        {
            skinMilestoneIndex = 0;
            var mergedAmounts = new Dictionary<ResourceType, int>();

            foreach (int milestoneIndex in milestoneIndexes)
            {
                skinMilestoneIndex = Mathf.Max(skinMilestoneIndex, milestoneIndex);

                var mileStoneInfo = GetMilestoneInfo(milestoneIndex);
                if (mileStoneInfo?.rewards == null) continue;

                foreach (var rewardInfo in mileStoneInfo.rewards)
                {
                    if (mergedAmounts.ContainsKey(rewardInfo.type))
                        mergedAmounts[rewardInfo.type] += rewardInfo.value;
                    else
                        mergedAmounts[rewardInfo.type] = rewardInfo.value;
                }
            }

            var rewards = new List<ResourceGame>();
            foreach (var pair in mergedAmounts)
                rewards.Add(new ResourceGame { type = pair.Key, value = pair.Value });

            return rewards;
        }

        void CompleteFinishedEventAfterClaims()
        {
            if (UIManager.Ins != null && UIManager.Ins.IsPopupOpen(PopupId.TowerRise))
                UIManager.Ins.ClosePopup(PopupId.TowerRise);

            ResetEvent();
        }

        public void TryAutoCompleteFinishedEventIfNoRewards()
        {
            if (!IsEventFinishedPending() || HasFinishedRewardsToClaim()) return;
            if (UIManager.Ins == null) return;
            if (UIManager.Ins.IsPopupOpen(PopupId.TowerRise)) return;

            CompleteFinishedEventAfterClaims();
        }

        void NotifyEventFinished()
        {
            if (UIManager.Ins == null || !UIManager.Ins.IsPopupOpen(PopupId.TowerRise)) return;
            UIManager.Ins.GetPopup<TowerRise>(PopupId.TowerRise)?.OnEventFinished();
        }

        public void RefreshHomeTowerRise()
        {
            TryAutoCompleteFinishedEventIfNoRewards();

            var homeBtn = UnityEngine.Object.FindFirstObjectByType<TowerRiseButtonHome>();
            homeBtn?.RefreshState();
        }

        public void InitNewExpire()
        {
            if (towerRiseData == null) return;
            DateTime expireTime = GetEventPeriodEnd(GetCurrentTime());
            towerRiseData.expire = expireTime.ToString("o");
            Save();
        }

        static DateTime GetEventPeriodEnd(DateTime now)
        {
            DateTime today = now.Date;
            int daysUntilThursday = ((int)DayOfWeek.Thursday - (int)today.DayOfWeek + 7) % 7;
            DateTime end = today.AddDays(daysUntilThursday).AddHours(23).AddMinutes(59).AddSeconds(59);
            if (daysUntilThursday == 0 && now > end)
                end = end.AddDays(7);
            return end;
        }

        public void CheckExpire()
        {
            if (towerRiseData == null) return;
            if (towerRiseData.isEventFinishedPending) return;

            if (DateTime.TryParse(towerRiseData.expire, out DateTime expire))
            {
                DateTime now = GetCurrentTime();
                if (!towerRiseData.isStarted && !towerRiseData.isEventFinishedPending
                    && expire.Date > GetEventPeriodEnd(now).Date.AddDays(1))
                {
                    InitNewExpire();
                    DateTime.TryParse(towerRiseData.expire, out expire);
                }

                if (now > expire)
                {
                    if (towerRiseData.isStarted)
                        MarkEventFinished();
                    else
                        InitNewExpire();
                }
            }
            else
            {
                ClearAllData();
                InitNewExpire();
            }
        }

        private int GetDaysUntilTuesday(DayOfWeek currentDay)
        {
            return currentDay switch
            {
                DayOfWeek.Tuesday => 0,
                DayOfWeek.Wednesday => 6,
                DayOfWeek.Thursday => 5,
                DayOfWeek.Friday => 4,
                DayOfWeek.Saturday => 3,
                DayOfWeek.Sunday => 2,
                DayOfWeek.Monday => 1,
                _ => 0
            };
        }

        public string GetCountDownString()
        {
            if (!isActive) return "---";
            if (IsEventFinishedPending()) return "Finished!";

            DateTime now = GetCurrentTime();

            if (!IsEventScheduleActive())
            {
                int daysUntilTuesday = GetDaysUntilTuesday(now.DayOfWeek);
                if (daysUntilTuesday == 0) daysUntilTuesday = 7;
                DateTime nextStart = now.Date.AddDays(daysUntilTuesday);
                TimeSpan untilStart = nextStart - now;
                if (untilStart.TotalSeconds <= 0) return "---";
                if (untilStart.Days > 0)
                    return $"{untilStart.Days}d {untilStart.Hours:D2}h";
                if (untilStart.Hours > 0)
                    return $"{untilStart.Hours}h {untilStart.Minutes:D2}m";
                return $"{untilStart.Minutes}:{untilStart.Seconds:D2}";
            }

            if (DateTime.TryParse(towerRiseData.expire, out DateTime expire))
            {
                if (!towerRiseData.isStarted)
                    expire = GetEventPeriodEnd(now);
                else if (IsEventScheduleActive() && expire.Date > GetEventPeriodEnd(now).Date.AddDays(1))
                    expire = GetEventPeriodEnd(now);

                TimeSpan timeRemaining = expire - now;
                if (timeRemaining.TotalSeconds <= 0) return "---";

                if (timeRemaining.Days > 0)
                {
                    return $"{timeRemaining.Days}d {timeRemaining.Hours:D2}h";
                }
                if (timeRemaining.Hours > 0)
                {
                    return $"{timeRemaining.Hours}h {timeRemaining.Minutes:D2}m";
                }
                return $"{timeRemaining.Minutes}:{timeRemaining.Seconds:D2}";
            }

            return "---";
        }

        public int GetTotalCurrentWins()
        {
            return towerRiseData != null ? towerRiseData.currentMilestone : 0;
        }

        public void SnapLastEventDisplayToCurrent()
        {
            SnapLastMilestoneToCurrent();
        }

        void SnapLastMilestoneToCurrent()
        {
            if (towerRiseData == null) return;

            towerRiseData.lastMilestone = towerRiseData.currentMilestone;
            Save();
        }

        void MigrateLastViewedData()
        {
            if (towerRiseData == null || !towerRiseData.isStarted) return;

            bool hasProgress = towerRiseData.currentMilestone > 0;
            bool lastEmpty = towerRiseData.lastMilestone == 0;

            if (hasProgress && lastEmpty)
                SnapLastMilestoneToCurrent();
        }

        public int LastViewedReachedMilestoneIndex =>
            towerRiseData != null ? GetReachedMilestoneIndexFromProgress(towerRiseData.lastMilestone) : 0;

        public int GetEventDisplayPlatformIndex()
        {
            int count = GetMileStoneCount();
            if (count <= 0) return 0;
            if (IsWinAnimCompletingMilestone())
                return Mathf.Clamp(lastCompletedMilestoneIndex, 0, count - 1);
            if (isFail || isWin)
                return Mathf.Clamp(currentMilestone, 0, count - 1);
            if (IsCompleteAllMileStones()) return count;
            return Mathf.Clamp(LastViewedReachedMilestoneIndex, 0, count - 1);
        }

        public int GetNotiNumber()
        {
            if (towerRiseData == null || !ShouldShowHomeButton()) return 0;

            if (IsEventFinishedPending())
                return GetFinishedPendingClaimCount();

            int res = 0;

            if (!towerRiseData.isStarted) return res;

            int count = GetMileStoneCount();
            for (int i = 0; i < count && i < GetReachedMilestoneIndex(); i++)
            {
                if (!IsMilestoneClaimed(i)) res += 1;
            }

            return res;
        }

        public bool IsNoti()
        {
            if (towerRiseData == null || !ShouldShowHomeButton()) return false;
            if (GetNotiNumber() > 0) return false;

            if (IsEventFinishedPending()) return HasFinishedRewardsToClaim();

            return ShouldShowIntroduce();
        }

        public bool ShouldShowIntroduce()
        {
            return isActive && IsEventScheduleActive() && towerRiseData != null && !towerRiseData.isStarted;
        }

        [Button]
        public void ClearDataNewWeek()
        {
            towerRiseData?.ClearDataNewWeek();
            Save();
            RefreshCurrentMilestoneFromData();
        }

        [Button]
        public void ClearAllData()
        {
            towerRiseData?.ClearAllData();
            Save();
            RefreshCurrentMilestoneFromData();
        }

        [Button]
        public void CheatAddWin()
        {
            OnWinLevel();
            var homeBtn = UnityEngine.Object.FindFirstObjectByType<TowerRiseButtonHome>();
            homeBtn?.RunAnim();
        }

        [Button]
        public void CheatAddConsecutiveWins(int cheatConsecutiveWinCount = 1)
        {
            if (cheatConsecutiveWinCount <= 0) return;

            int winsAdded = 0;
            for (int i = 0; i < cheatConsecutiveWinCount; i++)
            {
                if (!IsEventStarted() || IsCompleteAllMileStones()) break;

                OnWinLevel();
                winsAdded++;
            }

            if (winsAdded <= 0) return;

            RefreshHomeTowerRise();
            var homeBtn = UnityEngine.Object.FindFirstObjectByType<TowerRiseButtonHome>();
            homeBtn?.RunAnim();
        }

        [Button]
        public void CheatFail()
        {
            OnFailLevel();
            var homeBtn = UnityEngine.Object.FindFirstObjectByType<TowerRiseButtonHome>();
            homeBtn?.RunAnim();
        }

        [Button]
        public void CheatSetMilestone(int milestoneIndex)
        {
            if (towerRiseData == null) return;
            int count = GetMileStoneCount();
            milestoneIndex = Mathf.Clamp(milestoneIndex, 0, count);
            towerRiseData.currentMilestone = GetBaseProgressForMilestoneIndex(milestoneIndex);
            if (towerRiseData.totalMilestone <= 0)
                towerRiseData.totalMilestone = GetConfigTotalWinAmount();
            Save();
            RefreshCurrentMilestoneFromData();
        }

        [Button]
        public void CheatNewWeek()
        {
            if (towerRiseData == null) return;
            towerRiseData.expire = GetCurrentTime().AddDays(-2).ToString("o");
            CheckExpire();
        }
    }
}
