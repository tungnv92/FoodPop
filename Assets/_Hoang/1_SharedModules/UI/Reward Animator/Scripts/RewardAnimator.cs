using Sirenix.OdinInspector;
//using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Longvm.RewardAnimator
{
    public class RewardAnimator : MonoBehaviour
    {
        [SerializeField] bool isAutoTrigger;
        [SerializeField] bool isDisplayBg;
        [SerializeField] float autoTriggerDelay = 2;
        [SerializeField] BackgroundAnimation backgroundAnimation;
        [SerializeReference] RewardArranger rewardArranger;
        [SerializeField] RewardAnimation[] rewardAnimations;
        [Header("Chest")]
        ChestType chestType;
        //[SerializeField] SkeletonGraphic[] chestSpines;

        bool isTriggered = false;
        int working;
        WaitUntil waitUntilTriggered;
        List<RectTransform> activeRts = new();
        List<RewardAnimation> activeAnimations = new();

        public WaitUntil WaitUntilComplete { get; private set; }

        event Action onComplete;

        public void SetDestination(int typeId, Transform destination) => rewardAnimations[typeId].SetDestination(destination);

        [Button]
        public void Init(ChestType chestType = ChestType.None)
        {
            isTriggered = false;
            working = 0;
            activeRts.Clear();
            activeAnimations.Clear();
            isDisplayBg = chestType != ChestType.None;

            //foreach (var chest in chestSpines)
            //    chest.gameObject.SetActive(false);

            //if (chestType != ChestType.None)
            //{
            //    chestSpines[(int)chestType / 10].gameObject.SetActive(true);
            //}

            this.chestType = chestType;

            if (waitUntilTriggered == null) waitUntilTriggered = new WaitUntil(() => isTriggered);
            if (WaitUntilComplete == null) WaitUntilComplete = new WaitUntil(() => working == 0);
        }

        [Button(ButtonStyle.FoldoutButton)]
        public void Add(int typeId, int amount, Action onReceiveReward = null, Action onAnimCompleted = null)
        {
            if (typeId < 0) return;

            RewardAnimation animation = rewardAnimations[typeId];
            if (!activeAnimations.Contains(animation))
            {
                activeAnimations.Add(animation);
                activeRts.Add(animation.MyRt);
                animation.Init(amount);
                animation.OnReceiveReward(onReceiveReward);
                animation.OnComplete(Complete);
                working++;
            }
            else
            {
                animation.Add(amount);
            }
            

            void Complete()
            {
                working--;
                animation.gameObject.SetActive(false);
                onAnimCompleted?.Invoke();
            }
        }

        [Button]
        public void Play() => StartCoroutine(PlayCoroutine());
        IEnumerator PlayCoroutine()
        {
            if (isDisplayBg)
            {
                backgroundAnimation.gameObject.SetActive(true);
            }
            else       
            {
                isAutoTrigger = true;
            }

            UIManager.Ins.EnableBlocker(isAutoTrigger);     // If not display background, need enable blocker to block user input
            backgroundAnimation.Init();

            yield return null;
            backgroundAnimation.Open();
            rewardArranger = (activeRts.Count <= 5) ? new Circle() : new GridArranger();

            rewardArranger.Init(activeRts);
            yield return null;
            if (chestType != ChestType.None)
            {
                //ChangeChestSkin(chestSpines[(int)chestType / 10], chestType.ToString());
                //AudioManager.Ins.PlaySound(SoundType.OpenChest);
                yield return new WaitForSeconds(1.5f);
            }
            rewardArranger.Play();
            yield return rewardArranger.WaitUntilComplete;
            //AudioManager.Ins.PlaySound(SoundType.CoinShow);

            if (isAutoTrigger) AutoTrigger();
            yield return waitUntilTriggered;
            backgroundAnimation.Close();
            //if (chestType != ChestType.None) chestSpines[(int)chestType / 10].gameObject.SetActive(false);
            for (int i = 0; i < activeAnimations.Count; i++)
            {
                activeAnimations[i].Play();
            }
            yield return WaitUntilComplete;
            backgroundAnimation.gameObject.SetActive(false);
            UIManager.Ins.EnableBlocker(false);
            onComplete?.Invoke();
            onComplete = null;
        }

        public void OnComplete(Action onComplete) => this.onComplete = onComplete;

        public void Trigger() => isTriggered = true;

        void AutoTrigger() => StartCoroutine(AutoTriggerCoroutine());
        IEnumerator AutoTriggerCoroutine()
        {
            yield return WaitFor.Seconds(autoTriggerDelay);
            Trigger();
        }

        //public void ChangeChestSkin(SkeletonGraphic skeletonGraphic, string skinName)
        //{
        //    var skeleton = skeletonGraphic.Skeleton;

        //    if (skinName == "animchest1" || skinName == "bonus_bank" || skinName == "winstreak" || skinName == "mission_control") skinName = "default";
        //    skeleton.SetSkin(skinName);
        //    skeleton.SetSlotsToSetupPose();

        //    // Reset animation to start frame
        //    var state = skeletonGraphic.AnimationState;
        //    var current = state.GetCurrent(0);

        //    if (current != null)
        //    {
        //        state.SetAnimation(0, current.Animation.Name, current.Loop);
        //    }

        //    skeletonGraphic.AnimationState.Apply(skeleton);
        //    skeletonGraphic.SetVerticesDirty();
        //}

        #region UI EVENTS
        public void OnClick()
        {
            Trigger();
        }
        #endregion
    }

    public enum RewardType
    {
        None = -1,
        Coin = 0,
        UnlimitedLife,
        Booster_1 = 2,
        Booster_2,
        Booster_3,
        Booster_4,
    }

    public enum ChestType
    {
        None = -1,
        animchest1 = 0,
        // Winstreak Chests
        winstreak = 10,
        // Space Mission Chests
        mission_control = 20,
        // Master Pas Chests
        chest0 = 30,
        chest1,
        chest3,
        chest4,
        bonus_bank = 40,
    }
}
