using System.Collections;
using System.IO;
using UnityEngine;

namespace FoodPop
{
    // EN: Debug recorder: captures every rendered frame of the Game view at a fixed frame rate into one MJPEG file (Recordings/ next to Assets), stops when the level is finished or the time limit is reached.
    // VI: Bộ quay debug: chụp mọi frame của màn hình Game với tốc độ khung hình cố định vào một file MJPEG (thư mục Recordings/ cạnh Assets), dừng khi xong level hoặc hết thời gian giới hạn.
    public class GameplayRecorder : MonoBehaviour
    {
        [Tooltip("Số khung hình mỗi giây của video (thời gian game được khóa theo giá trị này).")]
        [Range(10, 60)] [SerializeField] private int frameRate = 30;
        [Tooltip("Thời lượng tối đa (giây game) của video.")]
        [Min(1f)] [SerializeField] private float maxDuration = 150f;
        [Tooltip("Thời gian (giây) quay thêm sau khi phục vụ xong mọi khách.")]
        [Min(0f)] [SerializeField] private float tailDuration = 2.5f;
        [Tooltip("Chất lượng JPEG của từng khung hình (1-100).")]
        [Range(1, 100)] [SerializeField] private int jpegQuality = 80;
        [Tooltip("Chiều rộng (pixel) của video; chiều cao tính theo tỉ lệ màn hình.")]
        [Min(64)] [SerializeField] private int outputWidth = 540;

        private FileStream output;
        private float finishedAt = -1f;
        private RenderTexture scaled;
        private Texture2D readback;

        // EN: Full path of the MJPEG file being written.
        // VI: Đường dẫn đầy đủ của file MJPEG đang ghi.
        public static string OutputPath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Recordings", "autoplay.mjpg");

        // EN: Locks game time to the frame rate, opens the output file and starts capturing.
        // VI: Khóa thời gian game theo tốc độ khung hình, mở file ghi và bắt đầu chụp.
        private void Start()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
            output = new FileStream(OutputPath, FileMode.Create, FileAccess.Write);
            Time.captureFramerate = frameRate;
            StartCoroutine(Capture());
        }

        // EN: Appends one JPEG per rendered frame until the level ends (plus a short tail) or the time limit passes.
        // VI: Ghi thêm một ảnh JPEG cho mỗi frame cho tới khi hết level (cộng thêm một đoạn ngắn) hoặc quá thời gian giới hạn.
        private IEnumerator Capture()
        {
            WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
            while (!ShouldStop())
            {
                yield return endOfFrame;
                byte[] jpg = CaptureScaledFrame();
                output.Write(jpg, 0, jpg.Length);
            }
            Finish();
        }

        // EN: Grabs the Game view, shrinks it to outputWidth (keeping the aspect ratio) and encodes it as JPEG, reusing the same buffers every frame.
        // VI: Chụp màn hình Game, thu nhỏ về outputWidth (giữ tỉ lệ) và mã hóa JPEG, dùng lại cùng bộ đệm cho mọi frame.
        private byte[] CaptureScaledFrame()
        {
            Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture();
            int width = Mathf.Min(outputWidth, frame.width) / 2 * 2;
            int height = Mathf.RoundToInt(frame.height * (float)width / frame.width) / 2 * 2;
            if (scaled == null || scaled.width != width || scaled.height != height)
            {
                if (scaled != null)
                    scaled.Release();
                scaled = new RenderTexture(width, height, 0);
                readback = new Texture2D(width, height, TextureFormat.RGB24, false);
            }
            Graphics.Blit(frame, scaled);
            Destroy(frame);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = scaled;
            readback.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = previous;
            return readback.EncodeToJPG(jpegQuality);
        }

        // EN: True when the time limit is reached or the tail after the last customer has been recorded.
        // VI: True khi hết thời gian giới hạn hoặc đã quay xong đoạn sau khách cuối cùng.
        private bool ShouldStop()
        {
            if (Time.time >= maxDuration)
                return true;
            CustomerManager customers = CustomerManager.Instance;
            if (finishedAt < 0f && customers != null && customers.AllServed)
                finishedAt = Time.time;
            return finishedAt >= 0f && Time.time - finishedAt >= tailDuration;
        }

        // EN: Closes the file, restores normal time and leaves Play Mode in the editor.
        // VI: Đóng file, trả thời gian về bình thường và thoát Play Mode trong editor.
        private void Finish()
        {
            output?.Dispose();
            output = null;
            Time.captureFramerate = 0;
            Debug.Log($"[GameplayRecorder] Đã lưu video tại {OutputPath}");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#endif
        }

        // EN: Makes sure the file is closed if Play Mode stops early.
        // VI: Bảo đảm file được đóng nếu Play Mode dừng sớm.
        private void OnDestroy()
        {
            output?.Dispose();
            if (scaled != null)
                scaled.Release();
            Time.captureFramerate = 0;
        }

#if UNITY_EDITOR
        private const string RequestKey = "FoodPop.AutoPlayRecord";

        // EN: Editor-only: when the record menu requested it, adds the bot and the recorder after the scene loads.
        // VI: Chỉ trong editor: khi menu quay yêu cầu, thêm bot và bộ quay sau khi scene được nạp.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootFromMenu()
        {
            if (!UnityEditor.SessionState.GetBool(RequestKey, false))
                return;
            UnityEditor.SessionState.SetBool(RequestKey, false);
            GameObject go = new GameObject("AutoPlayRecorder");
            go.AddComponent<AutoPlayer>();
            go.AddComponent<GameplayRecorder>();
        }

        // EN: Editor-only: asks the next Play Mode session to auto play and record.
        // VI: Chỉ trong editor: yêu cầu lần Play tiếp theo tự chơi và quay màn hình.
        public static void RequestRecording()
        {
            UnityEditor.SessionState.SetBool(RequestKey, true);
        }
#endif
    }
}
