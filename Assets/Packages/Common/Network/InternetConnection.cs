using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace GamePackages
{
    [DefaultExecutionOrder(10000)]
    public sealed class InternetConnection : MonoBehaviour
    {
        private const string TimeUrl = "https://www.google.com/generate_204";
        // EN: Allows play only after real verification or an explicit TimeManager.cheatTime bypass.
        // VI: Chỉ cho chơi sau xác thực thật hoặc khi chủ động bật TimeManager.cheatTime.
        public bool IsReady => clock.cheatTime || (connected && !wasCheating);

        [Tooltip("Lớp phủ chặn thao tác khi chưa xác thực Internet.")]
        [SerializeField] private GameObject blocker;
        [Tooltip("Nội dung trạng thái kết nối.")]
        [SerializeField] private TMP_Text messageLabel;
        [Tooltip("Nút kiểm tra kết nối và đồng bộ lại giờ.")]
        [SerializeField] private Button retryButton;
        [Tooltip("Chữ trên nút thử lại.")]
        [SerializeField] private TMP_Text retryLabel;
        [Tooltip("Khoảng cách giữa hai lần xác thực Internet khi đang chơi, tính bằng giây thực.")]
        [SerializeField, Min(1f)] private float checkInterval = 10f;

        private TimeManager clock;
        private UnityWebRequest request;
        private Coroutine verification;
        private bool connected;
        private bool wasCheating;
        private bool ownsPause;
        private float previousTimeScale;
        private double nextCheck;

        // EN: Binds the authored retry button without depending on either scene UIManager.
        // VI: Nối nút thử lại đã thiết kế mà không phụ thuộc UIManager của hai hệ scene.
        private void OnEnable()
        {
            retryButton.onClick.AddListener(Retry);
        }

        // EN: Applies runtime TimeManager.cheatTime changes immediately; turning cheatTime off requires a fresh Internet verification.
        // VI: Áp dụng thay đổi TimeManager.cheatTime runtime ngay; tắt cheatTime yêu cầu xác thực Internet mới.
        private void Update()
        {
            bool cheating = clock.cheatTime;
            if (cheating == wasCheating) return;
            wasCheating = cheating;
            CancelVerification();
            connected = false;
            SetBlocked(!cheating, !cheating);
            if (!cheating) Retry();
        }

        // EN: Keeps gameplay frozen and remembers an underlying popup's resume speed if it finishes while disconnected.
        // VI: Giữ gameplay dừng và ghi tốc độ tiếp tục của popup bên dưới nếu popup kết thúc lúc mất mạng.
        private void LateUpdate()
        {
            if (!ownsPause) return;
            if (Time.timeScale > 0f) previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        // EN: Requires a fresh server check when returning from the background, unless the clock's cheatTime is enabled, even when Wi-Fi appears available.
        // VI: Yêu cầu kiểm tra máy chủ mới khi trở lại từ nền, trừ khi bật cheatTime của đồng hồ, kể cả khi Wi-Fi có vẻ vẫn đang kết nối.
        private void OnApplicationPause(bool paused)
        {
            if (paused || clock == null || clock.cheatTime) return;
            CancelVerification();
            connected = false;
            SetBlocked(true, true);
            Retry();
        }

        // EN: Cancels owned requests and listeners, releasing only the pause taken by this overlay.
        // VI: Hủy request và listener sở hữu, chỉ nhả pause do lớp phủ này giữ.
        private void OnDisable()
        {
            retryButton.onClick.RemoveListener(Retry);
            StopAllCoroutines();
            CancelVerification();
            ReleasePause();
        }

        // EN: Starts the persistent connection gate using its clock's cheatTime, without depending on ProfileManager.
        // VI: Khởi động màn chặn kết nối xuyên scene theo cheatTime của đồng hồ, không phụ thuộc ProfileManager.
        public void Initialize(TimeManager timeManager)
        {
            clock = timeManager;
            wasCheating = clock.cheatTime;
            SetBlocked(!wasCheating, !wasCheating);
            if (!wasCheating) Retry();
            StartCoroutine(MonitorConnection());
        }

        // EN: Starts one explicit verification; the clock's cheatTime bypasses it and repeated presses cannot overlap requests.
        // VI: Bắt đầu một lần xác thực chủ động; cheatTime của đồng hồ bỏ xác thực và nhấn lặp không tạo request chồng nhau.
        public void Retry()
        {
            if (verification != null || clock.cheatTime) return;
            if (!connected) SetBlocked(true, true);
            verification = StartCoroutine(VerifyConnection());
        }

        // EN: Detects interface loss once per real second and verifies Internet periodically; the clock's cheatTime bypasses checks and disconnected players retry explicitly.
        // VI: Phát hiện mất kết nối thiết bị mỗi giây thực và định kỳ xác thực Internet; cheatTime của đồng hồ bỏ kiểm tra và người chơi mất mạng chủ động thử lại.
        private IEnumerator MonitorConnection()
        {
            while (true)
            {
                bool cheating = clock.cheatTime;
                if (!cheating && connected)
                {
                    if (Application.internetReachability == NetworkReachability.NotReachable)
                    {
                        CancelVerification();
                        connected = false;
                        SetBlocked(true, false);
                    }
                    else if (Time.realtimeSinceStartupAsDouble >= nextCheck) Retry();
                }
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        // EN: Verifies HTTPS and its UTC Date header together; failure blocks play without substituting device time.
        // VI: Xác thực HTTPS cùng header Date UTC; thất bại chặn chơi và không thay bằng giờ máy.
        private IEnumerator VerifyConnection()
        {
            request = UnityWebRequest.Head(TimeUrl);
            request.timeout = 3;
            request.SetRequestHeader("Cache-Control", "no-cache");
            yield return request.SendWebRequest();
            DateTimeOffset serverTime = default;
            bool success = request.result == UnityWebRequest.Result.Success &&
                DateTimeOffset.TryParse(request.GetResponseHeader("Date"), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out serverTime);
            if (success) clock.Synchronize(serverTime.UtcDateTime);
            request.Dispose();
            request = null;
            verification = null;
            connected = success;
            nextCheck = Time.realtimeSinceStartupAsDouble + checkInterval;
            SetBlocked(!success, false);
        }

        // EN: Stops and disposes verification when mode changes, connection disappears or the owner is disabled.
        // VI: Dừng và giải phóng xác thực khi đổi chế độ, mất kết nối hoặc owner bị tắt.
        private void CancelVerification()
        {
            if (verification != null) StopCoroutine(verification);
            verification = null;
            if (request == null) return;
            request.Abort();
            request.Dispose();
            request = null;
        }

        // EN: Shows a non-dismissible overlay above both UI systems and preserves the pause state beneath it.
        // VI: Hiện lớp phủ không thể tự đóng phía trên cả hai hệ UI và giữ trạng thái pause bên dưới.
        private void SetBlocked(bool blocked, bool checking)
        {
            blocker.SetActive(blocked);
            if (blocked)
            {
                if (!ownsPause)
                {
                    previousTimeScale = Time.timeScale;
                    ownsPause = true;
                }
                Time.timeScale = 0f;
                EventSystem.current?.SetSelectedGameObject(retryButton.gameObject);
                messageLabel.text = checking ? "Checking Internet connection…" : "Internet connection required.\nPlease reconnect to continue playing.";
                retryLabel.text = checking ? "Checking…" : "Try again";
                retryButton.interactable = !checking;
            }
            else ReleasePause();
        }

        // EN: Restores the exact gameplay speed from before the gate, including an existing pause.
        // VI: Khôi phục đúng tốc độ gameplay trước màn chặn, kể cả trạng thái đang pause.
        private void ReleasePause()
        {
            if (!ownsPause) return;
            Time.timeScale = previousTimeScale;
            ownsPause = false;
        }
    }
}
