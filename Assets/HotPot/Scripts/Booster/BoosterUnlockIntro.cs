using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FoodPop
{
    // EN: Introduces a booster on the level where it unlocks (once per booster): pauses the level, opens the "new booster" popup, flies the icon from the popup onto its button while the lock fades, pops the button, grants the unlock gift, then runs a guided try (dim screen with a hole over the button, Spine hand and "Tap to try!"; only that button reacts and the try spends one use like a normal tap).
    // VI: Giới thiệu booster ở level nó được mở khóa (mỗi booster một lần): tạm dừng màn chơi, mở popup "booster mới", cho icon bay từ popup xuống nút trong lúc ổ khóa mờ đi, nút nảy lên, tặng lượt mở khóa, rồi chạy lượt dùng thử có hướng dẫn (màn hình tối có lỗ sáng ở nút, bàn tay Spine và "Tap to try!"; chỉ nút đó phản hồi và lượt thử trừ một lượt như chạm thường).
    [DisallowMultipleComponent]
    public sealed class BoosterUnlockIntro : MonoBehaviour
    {
        private const string SeenKeyPrefix = "FoodPop.BoosterIntro.";

        [Header("Tham chiếu")]
        [Tooltip("Controller booster; để trống thì tự tìm trên cùng object.")]
        [SerializeField] private BoosterController controller;
        [Tooltip("Điều khiển level; để trống thì tự tìm trong scene.")]
        [SerializeField] private LevelController levelController;

        [Header("Nhịp hiện popup và icon bay")]
        [Tooltip("Thời gian icon bay từ popup xuống nút booster, tính bằng giây.")]
        [SerializeField, Min(0.05f)] private float flyDuration = 0.65f;
        [Tooltip("Độ cao vòng cung khi icon bay, tính bằng đơn vị world.")]
        [SerializeField] private float flyArc = 1.5f;
        [Tooltip("Thời gian nút booster nảy lên sau khi icon đáp xuống, tính bằng giây.")]
        [SerializeField, Min(0.05f)] private float popDuration = 0.42f;
        [Tooltip("Thời gian chờ sau khi màn chơi đã dừng rồi mới mở popup giới thiệu, để người chơi kịp nhìn bàn chơi (giây, không phụ thuộc tốc độ game).")]
        [SerializeField, Min(0f)] private float popupDelay = 0.8f;

        [Header("Hướng dẫn dùng thử")]
        [Tooltip("Prefab lớp phủ dùng thử (Prefabs/UI/Booster/BoosterTrialOverlay): canvas vẽ trên mọi UI, tấm đen che kín màn hình chừa lỗ sáng ở nút, bàn tay Spine và dòng nhắc.")]
        [SerializeField] private BoosterTrialOverlay trialOverlayPrefab;
        [Tooltip("Mức phập phồng của nút trong lúc chờ người chơi chạm (0.06 = 6%).")]
        [SerializeField, Range(0f, 0.3f)] private float pulseScale = 0.06f;
        [Tooltip("Thứ tự vẽ của icon bay từ popup xuống nút.")]
        [SerializeField] private int sortingOrder = 940;

        private Coroutine routine;
        private BoosterTrialOverlay overlay;
        private SpriteRenderer flyer;
        private Booster activeBooster;
        private Vector3 lockRestScale = Vector3.one;
        private Vector3 unlockRestScale = Vector3.one;
        private bool ownsPause;
        private float pausedTimeScale = 1f;

        // EN: Fills references left empty in the Inspector.
        // VI: Tự gán các tham chiếu còn trống trong Inspector.
        private void Awake()
        {
            ResolveReferences();
        }

        // EN: Starts following level loads.
        // VI: Bắt đầu theo dõi lúc nạp level.
        private void OnEnable()
        {
            if (levelController != null)
                levelController.LevelLoaded += OnLevelLoaded;
        }

        // EN: Stops following level loads and cleans up an intro that is still running.
        // VI: Ngừng theo dõi lúc nạp level và dọn màn giới thiệu đang chạy dở.
        private void OnDisable()
        {
            if (levelController != null)
                levelController.LevelLoaded -= OnLevelLoaded;
            StopIntro();
        }

        // EN: Forgets which boosters were already introduced so the intros play again (testing helper).
        // VI: Xóa ghi nhớ các booster đã giới thiệu để màn giới thiệu chạy lại (dùng khi thử nghiệm).
        [ContextMenu("Test/Xóa cờ đã xem giới thiệu booster")]
        public void ClearSeenFlags()
        {
            foreach (BoosterType type in System.Enum.GetValues(typeof(BoosterType)))
                PlayerPrefs.DeleteKey(SeenKeyPrefix + type);
            PlayerPrefs.Save();
        }

        // EN: Uses the BoosterController on this object and the LevelController in the scene when not assigned.
        // VI: Dùng BoosterController trên cùng object và LevelController trong scene khi chưa được gán.
        private void ResolveReferences()
        {
            if (controller == null)
                controller = GetComponent<BoosterController>();
            if (levelController == null)
                levelController = FindFirstObjectByType<LevelController>();
        }

        // EN: Collects the boosters that unlock on this level (level 2 or later; boosters open from level 1 are available from the start and get no intro) and were never introduced, then plays their intros in order.
        // VI: Gom các booster mở khóa ở level này (từ level 2 trở đi; booster mở từ level 1 có sẵn ngay từ đầu nên không giới thiệu) và chưa từng được giới thiệu, rồi lần lượt chạy màn giới thiệu.
        private void OnLevelLoaded(LevelConfig config)
        {
            StopIntro();
            if (controller == null || config == null || config.levelNumber <= 1)
                return;
            BoosterConfig boosters = IngameConfig.Instance.Boosters;
            var due = new List<Booster>();
            foreach (Booster booster in controller.GetComponentsInChildren<Booster>(true))
                if (booster != null && boosters.GetUnlockLevel(booster.Type) == config.levelNumber && !IsSeen(booster.Type))
                    due.Add(booster);
            if (due.Count > 0)
                routine = StartCoroutine(RunIntro(due));
        }

        // EN: Cancels a running intro: removes the overlay and flying icon, closes the popup, restores the button look and gives back the pause.
        // VI: Hủy màn giới thiệu đang chạy: bỏ lớp phủ và icon bay, đóng popup, trả lại dáng nút và nhả tạm dừng.
        private void StopIntro()
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = null;
            if (controller != null)
                controller.EndTrial();
            if (overlay != null)
                Destroy(overlay.gameObject);
            overlay = null;
            if (flyer != null)
                Destroy(flyer.gameObject);
            flyer = null;
            if (activeBooster != null)
            {
                if (activeBooster.LockRoot != null) activeBooster.LockRoot.localScale = lockRestScale;
                if (activeBooster.UnlockRoot != null) activeBooster.UnlockRoot.localScale = unlockRestScale;
                activeBooster.ShowLockVisual(activeBooster.IsLocked);
                activeBooster = null;
            }
            if (UIManager.Instance != null && UIManager.Instance.IsUICanvasOpened<CanvasBoosterIntro>())
                UIManager.Instance.CloseUI<CanvasBoosterIntro>();
            ReleasePause();
        }

        // EN: Runs popup, flight, the one-time unlock gift (BoosterConfig unlockGift uses, granted together with the seen flag) and guided trial for each due booster, pausing the level around each one.
        // VI: Chạy popup, chuyến bay, quà mở khóa một lần (số lượt unlockGift trong BoosterConfig, cộng cùng lúc ghi cờ đã xem) và lượt dùng thử cho từng booster đến lượt, tạm dừng màn chơi quanh mỗi booster.
        private IEnumerator RunIntro(List<Booster> due)
        {
            yield return null;
            foreach (Booster booster in due)
            {
                while (controller.IsRunning)
                    yield return null;
                Pause();
                activeBooster = booster;
                if (booster.LockRoot != null) lockRestScale = booster.LockRoot.localScale;
                if (booster.UnlockRoot != null) unlockRestScale = booster.UnlockRoot.localScale;
                booster.ShowLockVisual(true);
                yield return new WaitForSecondsRealtime(popupDelay);
                yield return ShowPopupAndFly(booster);
                MarkSeen(booster.Type);
                booster.Grant(IngameConfig.Instance.Boosters.GetUnlockGift(booster.Type), "unlock_intro");
                yield return RunTrial(booster);
                activeBooster = null;
            }
            ReleasePause();
            routine = null;
        }

        // EN: Pauses the level (once) and remembers the speed to restore.
        // VI: Tạm dừng màn chơi (một lần) và ghi nhớ tốc độ để khôi phục.
        private void Pause()
        {
            if (ownsPause)
                return;
            pausedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            ownsPause = true;
        }

        // EN: Opens the popup, waits for "Got it", then flies the icon from the popup onto the booster button.
        // VI: Mở popup, chờ bấm "Got it", rồi cho icon bay từ popup xuống nút booster.
        private IEnumerator ShowPopupAndFly(Booster booster)
        {
            BoosterConfig config = IngameConfig.Instance.Boosters;
            Sprite icon = booster.IconRenderer != null ? booster.IconRenderer.sprite : null;
            bool pressed = false;
            CanvasBoosterIntro popup = UIManager.Instance != null
                ? UIManager.Instance.ShowBoosterIntro(icon, config.GetName(booster.Type), config.GetInfo(booster.Type), () => pressed = true)
                : null;
            if (popup == null)
            {
                booster.ShowLockVisual(false);
                yield break;
            }
            while (!pressed)
                yield return null;
            bool hasRect = popup.TryGetIconScreenRect(out Vector2 center, out float heightPixels);
            popup.HideIcon();
            UIManager.Instance.CloseUI<CanvasBoosterIntro>();
            yield return FlyIcon(booster, hasRect, center, heightPixels);
        }

        // EN: Flies a copy of the icon along an arc from its popup position to the button icon, shrinking the lock away on the way, then shows the unlocked button and pops it.
        // VI: Cho bản sao icon bay theo vòng cung từ vị trí trên popup tới icon của nút, thu nhỏ ổ khóa trên đường bay, rồi hiện nút đã mở và cho nút nảy lên.
        private IEnumerator FlyIcon(Booster booster, bool hasRect, Vector2 screenCenter, float heightPixels)
        {
            SpriteRenderer target = booster.IconRenderer;
            Camera cam = Camera.main;
            if (target != null && target.sprite != null && cam != null && hasRect)
            {
                Transform targetTransform = target.transform;
                Vector3 end = targetTransform.position;
                float spriteHeight = Mathf.Max(0.001f, target.sprite.bounds.size.y);
                float endHeight = spriteHeight * Mathf.Abs(targetTransform.lossyScale.y);
                Vector3 start = cam.ScreenToWorldPoint(new Vector3(screenCenter.x, screenCenter.y, Mathf.Abs(end.z - cam.transform.position.z)));
                start.z = end.z;
                float worldPerPixel = cam.orthographic ? cam.orthographicSize * 2f / Mathf.Max(1, Screen.height) : endHeight / Mathf.Max(1f, heightPixels);
                float startHeight = heightPixels * worldPerPixel;
                Vector3 control = (start + end) * 0.5f + Vector3.up * flyArc;

                flyer = new GameObject("Booster intro icon").AddComponent<SpriteRenderer>();
                flyer.sprite = target.sprite;
                flyer.sortingLayerID = target.sortingLayerID;
                flyer.sortingOrder = sortingOrder;
                Transform lockRoot = booster.LockRoot;
                for (float elapsed = 0f; elapsed < flyDuration; elapsed += Time.unscaledDeltaTime)
                {
                    float t = Mathf.Clamp01(elapsed / flyDuration);
                    float e = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                    flyer.transform.position = (1f - e) * (1f - e) * start + 2f * (1f - e) * e * control + e * e * end;
                    flyer.transform.localScale = Vector3.one * (Mathf.Lerp(startHeight, endHeight, e) / spriteHeight);
                    flyer.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Mathf.PI * e) * -14f);
                    if (lockRoot != null)
                        lockRoot.localScale = lockRestScale * (1f - Mathf.Clamp01((t - 0.45f) / 0.35f));
                    yield return null;
                }
                Destroy(flyer.gameObject);
                flyer = null;
                if (lockRoot != null)
                    lockRoot.localScale = lockRestScale;
            }
            booster.ShowLockVisual(false);
            yield return Pop(booster.UnlockRoot);
        }

        // EN: Squash-and-stretch pop of the unlocked button (0.86 → 1.16 → 0.96 → 1).
        // VI: Cho nút đã mở nảy lên kiểu bóp giãn (0.86 → 1.16 → 0.96 → 1).
        private IEnumerator Pop(Transform target)
        {
            if (target == null)
                yield break;
            for (float elapsed = 0f; elapsed < popDuration; elapsed += Time.unscaledDeltaTime)
            {
                float t = elapsed / popDuration;
                float s = t < 0.45f ? Mathf.Lerp(0.86f, 1.16f, t / 0.45f)
                        : t < 0.75f ? Mathf.Lerp(1.16f, 0.96f, (t - 0.45f) / 0.3f)
                        : Mathf.Lerp(0.96f, 1f, (t - 0.75f) / 0.25f);
                target.localScale = unlockRestScale * s;
                yield return null;
            }
            target.localScale = unlockRestScale;
        }

        // EN: Shows the dim overlay, hand and prompt, lets only the booster button react, then unpauses and runs the booster like a normal tap (spends one use).
        // VI: Hiện lớp tối, bàn tay và dòng nhắc, chỉ cho nút booster phản hồi, rồi bỏ tạm dừng và chạy booster như chạm thường (trừ một lượt).
        private IEnumerator RunTrial(Booster booster)
        {
            BuildOverlay(booster);
            bool tapped = false;
            controller.BeginTrial(booster, () => tapped = true);
            Transform pulse = booster.UnlockRoot;
            while (!tapped)
            {
                if (pulse != null)
                    pulse.localScale = unlockRestScale * (1f + pulseScale * (0.5f - 0.5f * Mathf.Cos(Time.unscaledTime * Mathf.PI * 2f / 0.9f)));
                yield return null;
            }
            if (pulse != null)
                pulse.localScale = unlockRestScale;
            if (overlay != null)
                Destroy(overlay.gameObject);
            overlay = null;
            ReleasePause();
            controller.TryActivate(booster);
        }

        // EN: Spawns the trial overlay prefab (its own canvas above every UI, black panels covering the whole screen except a round hole fitted to the button background, Spine hand and "Tap to try!" text from BoosterConfig).
        // VI: Tạo lớp phủ dùng thử từ prefab (canvas riêng trên mọi UI, tấm đen che kín màn hình trừ lỗ tròn khớp nền nút, bàn tay Spine và dòng "Tap to try!" lấy từ BoosterConfig).
        private void BuildOverlay(Booster booster)
        {
            if (trialOverlayPrefab == null)
            {
                Debug.LogWarning("[BoosterUnlockIntro] Chưa gán prefab lớp phủ dùng thử.", this);
                return;
            }
            overlay = Instantiate(trialOverlayPrefab);
            overlay.Show(booster.GetComponent<SpriteRenderer>().bounds, Camera.main, IngameConfig.Instance.Boosters.GetShortDes(BoosterConfig.ShortDesType.TryPrompt));
        }

        // EN: Restores the speed saved by Pause, if this component paused the level.
        // VI: Khôi phục tốc độ đã lưu khi Pause, nếu chính component này đã tạm dừng màn chơi.
        private void ReleasePause()
        {
            if (!ownsPause)
                return;
            ownsPause = false;
            Time.timeScale = pausedTimeScale;
        }

        // EN: True when the booster's intro was already shown on this device.
        // VI: True khi màn giới thiệu của booster đã hiện trên thiết bị này.
        private static bool IsSeen(BoosterType type)
        {
            return PlayerPrefs.GetInt(SeenKeyPrefix + type, 0) == 1;
        }

        // EN: Remembers that the booster's intro was shown.
        // VI: Ghi nhớ màn giới thiệu của booster đã hiện.
        private static void MarkSeen(BoosterType type)
        {
            PlayerPrefs.SetInt(SeenKeyPrefix + type, 1);
            PlayerPrefs.Save();
        }
    }
}
