using GamePackages;
using System.Collections;
using TMPro;
using UnityEngine;

namespace FoodPop
{
    public abstract class Booster : MonoBehaviour
    {
        // VI: Loại booster.
        public abstract BoosterType Type { get; }

        // EN: True while the player still owns at least one use of this booster.
        // VI: True khi người chơi còn ít nhất một lượt booster này.
        public bool HasAmount => Amount > 0;

        // EN: True while the current level is below this booster's unlock level (set by RefreshLock).
        // VI: True khi level hiện tại còn nhỏ hơn level mở khóa của booster (đặt bởi RefreshLock).
        public bool IsLocked { get; private set; }

        // EN: The booster icon renderer (used by the unlock intro to fly the icon and aim the trial hand).
        // VI: Renderer icon booster (màn giới thiệu mở khóa dùng để cho icon bay và đặt bàn tay hướng dẫn).
        public SpriteRenderer IconRenderer => icon;

        // EN: Object shown while locked (lock icon and "Lv N"), or null when not assigned.
        // VI: Object hiện khi còn khóa (icon khóa và "Lv N"), hoặc null khi chưa gán.
        public Transform LockRoot => objLock != null ? objLock.transform : null;

        // EN: Object shown once unlocked (icon, plus sign and count), or null when not assigned.
        // VI: Object hiện khi đã mở khóa (icon, dấu cộng và số lượt), hoặc null khi chưa gán.
        public Transform UnlockRoot => objUnlock != null ? objUnlock.transform : null;

        // VI: Renderer icon (lớp con có thể dùng lại sprite của nó).
        protected SpriteRenderer Icon => icon;
        // VI: Số lượt booster người chơi đang có.
        protected int Amount => PlayerData.GetResourceValue(boosterType);

        // EN: Player data holding booster counts (guaranteed to exist at runtime call sites).
        // VI: Dữ liệu người chơi chứa số lượt booster (luôn tồn tại tại nơi gọi lúc chạy).
        private static PlayerDataManager PlayerData => ProfileManager.instance.PlayerDataManager;

        [SerializeField] ResourceType boosterType;
        [SerializeField] BoxCollider2D boxCollider;
        [SerializeField] private SpriteRenderer icon;
        [Tooltip("Object dấu cộng, hiện khi đã hết lượt booster; chạm vào sẽ mở popup mua booster.")]
        [SerializeField] private GameObject objPlus;
        [Tooltip("Object số lượt, hiện khi còn lượt booster.")]
        [SerializeField] private GameObject objNumber;
        [Tooltip("Text hiển thị số lượt booster còn lại.")]
        [SerializeField] private TextMeshProUGUI txtNumber;
        [Tooltip("Độ sáng của icon khi booster đang không dùng được (có booster khác đang chạy).")]
        [Range(0f, 1f)][SerializeField] private float unavailableBrightness = 0.55f;

        [Header("Khóa")]
        [Tooltip("Object hiện khi booster còn khóa: icon khóa và chữ level mở khóa.")]
        [SerializeField] private GameObject objLock;
        [Tooltip("Object hiện khi booster đã mở khóa: icon booster, dấu cộng và số lượt.")]
        [SerializeField] private GameObject objUnlock;
        [Tooltip("Text hiển thị level mở khóa booster, ví dụ \"Lv 5\".")]
        [SerializeField] private TextMeshProUGUI txtUnlockLevel;

        private Coroutine shakeRoutine;
        private Transform shakeTarget;

        // EN: Shows the current count and follows later changes of this booster's resource.
        // VI: Hiện số lượt hiện tại và theo dõi các thay đổi sau đó của tài nguyên booster này.
        private void OnEnable()
        {
            PlayerData.RegisterResourceChange(boosterType, OnAmountChanged);
            RefreshAmount();
        }

        // EN: Stops following the resource and resets the shaken object's rotation when hidden mid-shake.
        // VI: Ngừng theo dõi tài nguyên và khôi phục góc xoay của object đang lắc khi bị ẩn giữa nhịp lắc.
        private void OnDisable()
        {
            PlayerData.RemoveResourceChange(boosterType, OnAmountChanged);
            shakeRoutine = null;
            if (shakeTarget != null)
                shakeTarget.localRotation = Quaternion.identity;
        }

        // EN: Locks the booster when the level number is below its unlock level in BoosterConfig: shows objLock with "Lv N" and hides objUnlock, or the other way round once unlocked.
        // VI: Khóa booster khi số level nhỏ hơn level mở khóa trong BoosterConfig: hiện objLock kèm chữ "Lv N" và ẩn objUnlock, hoặc ngược lại khi đã mở khóa.
        public void RefreshLock(int level)
        {
            int unlockLevel = IngameConfig.Instance.Boosters.GetUnlockLevel(Type);
            IsLocked = level < unlockLevel;
            if (objLock != null)
                objLock.SetActive(IsLocked);
            if (objUnlock != null)
                objUnlock.SetActive(!IsLocked);
            if (txtUnlockLevel != null)
                txtUnlockLevel.text = "Lv " + unlockLevel;
        }

        // EN: Shows the locked or unlocked look without changing IsLocked (the unlock intro shows the lock again before playing the unlock).
        // VI: Hiện dáng khóa hoặc đã mở mà không đổi IsLocked (màn giới thiệu mở khóa cho hiện lại ổ khóa trước khi chạy hiệu ứng mở).
        public void ShowLockVisual(bool locked)
        {
            if (objLock != null)
                objLock.SetActive(locked);
            if (objUnlock != null)
                objUnlock.SetActive(!locked);
        }

        // EN: Reads the current count and updates the display.
        // VI: Đọc số lượt hiện tại và cập nhật phần hiển thị.
        public void RefreshAmount()
        {
            ShowAmount(Amount);
        }

        // EN: Dims unavailable booster icons directly and restores brightness when available.
        // VI: Làm mờ trực tiếp icon booster không khả dụng và khôi phục độ sáng khi dùng được.
        public void SetAvailable(bool available)
        {
            float value = available ? 1f : unavailableBrightness;
            if (icon != null)
                icon.color = new Color(value, value, value, icon.color.a);
        }

        // EN: True when the world point is inside this booster's BoxCollider2D (only while the collider is enabled and its object is visible); finds the collider on the icon or the booster when none is assigned.
        // VI: True khi điểm world nằm trong BoxCollider2D của booster (chỉ khi collider đang bật và object đang hiện); tự tìm collider trên icon hoặc trên booster nếu chưa gán.
        public bool Contains(Vector2 worldPoint)
        {
            BoxCollider2D area = HitArea;
            return area != null && area.enabled && area.gameObject.activeInHierarchy && area.OverlapPoint(worldPoint);
        }

        // EN: Spends one use of this booster through OnRemoveBooster (records usage statistics); returns false and changes nothing when no use is left. The display updates through the resource-change callback.
        // VI: Trừ một lượt booster qua OnRemoveBooster (ghi thống kê sử dụng); trả false và không đổi gì khi đã hết lượt. Phần hiển thị tự cập nhật qua callback đổi tài nguyên.
        public bool Consume()
        {
            return PlayerData.OnRemoveBooster(boosterType, 1, "ingame");
        }

        // EN: Adds uses of this booster through OnAddBooster (records the earn); the display updates through the resource-change callback.
        // VI: Cộng lượt booster qua OnAddBooster (ghi thống kê nhận); phần hiển thị tự cập nhật qua callback đổi tài nguyên.
        public void Grant(int amount, string placement)
        {
            PlayerData.OnAddBooster(boosterType, amount, placement);
        }

        // EN: Asks the UI to open the buy-booster popup for this booster (used when it has no use left).
        // VI: Yêu cầu UI mở popup mua booster cho booster này (dùng khi đã hết lượt).
        public void RequestBuy()
        {
            UIManager.Instance?.ShowBuyBooster(Type);
        }

        // EN: Shakes the lock while the booster is locked, otherwise the icon, sideways without a press animation.
        // VI: Lắc ngang icon khóa khi booster còn khóa, ngược lại lắc icon booster, không chạy hiệu ứng nhấn.
        public void PlayDenied()
        {
            Transform target = IsLocked && objLock != null ? objLock.transform : icon != null ? icon.transform : null;
            if (target == null || !isActiveAndEnabled)
                return;
            if (shakeRoutine != null)
                StopCoroutine(shakeRoutine);
            if (shakeTarget != null)
                shakeTarget.localRotation = Quaternion.identity;
            shakeTarget = target;
            shakeRoutine = StartCoroutine(Shake(target));
        }

        // EN: Resource-change callback: refreshes the count display.
        // VI: Callback khi tài nguyên đổi: cập nhật phần hiển thị số lượt.
        private void OnAmountChanged(int value)
        {
            ShowAmount(value);
        }

        // EN: Shows objPlus when no use is left; otherwise shows objNumber with the count in txtNumber.
        // VI: Hiện objPlus khi đã hết lượt; ngược lại hiện objNumber kèm số lượt trong txtNumber.
        private void ShowAmount(int value)
        {
            bool hasAmount = value > 0;
            if (objPlus != null)
                objPlus.SetActive(!hasAmount);
            if (objNumber != null)
                objNumber.SetActive(hasAmount);
            if (txtNumber != null && hasAmount)
                txtNumber.text = value.ToString();
        }

        // EN: The click area: the assigned collider, otherwise the BoxCollider2D found on the icon or on this booster; when there is none, a trigger BoxCollider2D is added to the icon (Unity sizes it to the sprite).
        // VI: Vùng chạm: collider đã gán, nếu không thì BoxCollider2D tìm thấy trên icon hoặc trên booster này; nếu không có thì thêm một BoxCollider2D trigger vào icon (Unity tự khớp cỡ theo sprite).
        private BoxCollider2D HitArea
        {
            get
            {
                if (boxCollider == null)
                    boxCollider = icon != null ? icon.GetComponent<BoxCollider2D>() : null;
                if (boxCollider == null)
                    boxCollider = GetComponent<BoxCollider2D>();
                if (boxCollider == null && icon != null)
                {
                    boxCollider = icon.gameObject.AddComponent<BoxCollider2D>();
                    boxCollider.isTrigger = true;
                }
                return boxCollider;
            }
        }

        // EN: Quick left-right tilt shake of the given object.
        // VI: Lắc nghiêng trái phải nhanh object đã cho.
        private IEnumerator Shake(Transform target)
        {
            const float Duration = 0.3f;
            for (float time = 0f; time < Duration; time += Time.deltaTime)
            {
                float t = time / Duration;
                target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 6f) * 12f * (1f - t));
                yield return null;
            }
            target.localRotation = Quaternion.identity;
            shakeRoutine = null;
        }
    }
}
