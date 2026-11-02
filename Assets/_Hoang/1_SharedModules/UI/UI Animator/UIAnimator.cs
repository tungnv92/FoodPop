using System;
using System.Collections;
using UnityEngine;

namespace Longvm.UIAnimator
{
    public class UIAnimator : MonoBehaviour
    {
        [SerializeField] bool interactableOnPlay;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeReference] Animation[] animations;

        bool isComplete;
        WaitUntil waitUtilComplete;
        public WaitUntil WaitUntilComplete
        {
            get
            {
                if (waitUtilComplete == null)
                {
                    waitUtilComplete = new WaitUntil(() => isComplete);
                }
                return waitUtilComplete;
            }
        }

        private void OnValidate()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        public void InitAndPlay()
        {
            StartCoroutine(InitAndPlayCoroutine());
        }

        IEnumerator InitAndPlayCoroutine()
        {
            Init();
            yield return null;
            Play();
        }

        public void Init()
        {
            if (animations.IsNullOrEmpty())
            {
                Debug.Log("UI Anim: Animations null or empty");
                return;
            }
            for (int i = 0; i < animations.Length; i++)
            {
                if (animations[i] == null) continue;
                animations[i].Init();
            }
        }

        public void Play(Action onComplete = null)
        {
            StartCoroutine(PlayCoroutine(onComplete));
        }

        IEnumerator PlayCoroutine(Action onComplete)
        {
            isComplete = false;
            Interactable(interactableOnPlay);
            if (animations.IsNullOrEmpty())
            {
                isComplete = true;
                onComplete?.Invoke();
                yield break;
            }
            for (int i = 0; i < animations.Length; i++)
            {
                if (animations[i] == null) continue;
                if (animations[i] is Interval)
                {
                    yield return WaitFor.Seconds(animations[i].Duration);
                }
                else
                {
                    animations[i].Play();
                    if (i == animations.Length - 1)
                    {
                        yield return WaitFor.Seconds(animations[i].Duration);
                    }
                }
            }
            isComplete = true;
            Interactable(true);
            onComplete?.Invoke();
        }

        void Interactable(bool interactable)
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = interactable;
            }
        }
    }
}
