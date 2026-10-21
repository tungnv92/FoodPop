using System.Collections;
using System.IO;
using UnityEngine;

namespace FoodPop
{
    // EN: Debug recorder: captures every rendered frame of the Game view at a fixed frame rate into one MJPEG file (Recordings/ next to Assets), stops when the level is finished or the time limit is reached.
    // VI: Bộ quay debug: chụp mọi frame của màn hình Game với tốc độ khung hình cố định vào một file MJPEG (thư mục Recordings/ cạnh Assets), dừng khi xong level hoặc hết thời gian giới hạn.
    public class GameplayRecorder : MonoBehaviour
    {
        [Tooltip("Số khung hình mỗi giây của video; game vẫn chạy đúng tốc độ thật.")]
        [Range(10, 60)] [SerializeField] private int frameRate = 30;
        [Tooltip("Thời lượng tối đa (giây game) của video.")]
        [Min(1f)] [SerializeField] private float maxDuration = 150f;
        [Tooltip("Thời gian (giây) quay thêm sau khi phục vụ xong mọi khách.")]
        [Min(0f)] [SerializeField] private float tailDuration = 2.5f;
        [Tooltip("Chất lượng JPEG của từng khung hình (1-100).")]
        [Range(1, 100)] [SerializeField] private int jpegQuality = 92;
        [Tooltip("Chiều rộng (pixel) của video; chiều cao tính theo tỉ lệ màn hình.")]
        [Min(64)] [SerializeField] private int outputWidth = 1080;

        private FileStream output;
        private float finishedAt = -1f;
        private RenderTexture scaled;
        private Texture2D readback;

        // EN: Full path of the MJPEG file being written.
        // VI: Đường dẫn đầy đủ của file MJPEG đang ghi.
        public static string OutputPath => Path.Combine(Path.GetDirectoryName(Application.dataPath), "Recordings", "autoplay.mjpg");

        private const long PartLimitBytes = 300L * 1024 * 1024;
        private int partIndex;

        // EN: Path of one part file; the recording is split into parts of at most ~300 MB so each can be copied easily (join them in order).
        // VI: Đường dẫn của một phần file; bản quay được chia thành các phần tối đa ~300 MB để dễ sao chép (nối lại theo thứ tự).
        private static string PartPath(int index) => Path.Combine(Path.GetDirectoryName(OutputPath), $"autoplay_{index:000}.mjpg");

        // EN: Deletes old recordings, then opens the first part file.
        // VI: Xóa các bản quay cũ rồi mở file phần đầu tiên.
        private void OpenFirstPart()
        {
            string folder = Path.GetDirectoryName(OutputPath);
            Directory.CreateDirectory(folder);
            foreach (string old in Directory.GetFiles(folder, "autoplay*.mjpg"))
                File.Delete(old);
            partIndex = 0;
            output = new FileStream(PartPath(partIndex), FileMode.Create, FileAccess.Write);
        }

        // EN: Writes one JPEG frame, starting a new part file when the current one is full.
        // VI: Ghi một khung JPEG, mở file phần mới khi file hiện tại đã đầy.
        private void WriteFrame(byte[] jpg)
        {
            if (output.Length + jpg.Length > PartLimitBytes)
            {
                output.Dispose();
                partIndex++;
                output = new FileStream(PartPath(partIndex), FileMode.Create, FileAccess.Write);
            }
            output.Write(jpg, 0, jpg.Length);
        }

        // EN: Opens the output file and starts capturing; the game runs at its real speed (game time is not locked).
        // VI: Mở file ghi và bắt đầu chụp; game chạy đúng tốc độ thật (không khóa thời gian game).
        private void Start()
        {
            OpenFirstPart();
            StartCoroutine(Capture());
        }

        // EN: Samples the Game view on a real-time clock at frameRate until the level ends (plus a short tail) or the time limit passes: each video frame slot that has elapsed gets the latest image (repeated when rendering is slower than frameRate, skipped when faster), so the video plays at the game's real speed.
        // VI: Lấy mẫu màn hình Game theo đồng hồ thời gian thật với tốc độ frameRate cho tới khi hết level (cộng thêm một đoạn ngắn) hoặc quá thời gian giới hạn: mỗi khe khung hình đã trôi qua nhận ảnh mới nhất (lặp lại khi render chậm hơn frameRate, bỏ bớt khi nhanh hơn), để video chạy đúng tốc độ thật của game.
        private IEnumerator Capture()
        {
            WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();
            float startTime = Time.realtimeSinceStartup;
            long written = 0;
            while (!ShouldStop())
            {
                yield return endOfFrame;
                long due = (long)((Time.realtimeSinceStartup - startTime) * frameRate) + 1;
                if (due <= written)
                    continue;
                byte[] jpg = CaptureScaledFrame();
                for (; written < due; written++)
                    WriteFrame(jpg);
            }
            Finish();
        }

        // EN: Grabs the Game view (rendered larger when the view is smaller than outputWidth, so small Game windows still give sharp frames), scales it to outputWidth (keeping the aspect ratio) and encodes it as JPEG, reusing the same buffers every frame.
        // VI: Chụp màn hình Game (render to hơn khi cửa sổ Game nhỏ hơn outputWidth để khung hình vẫn nét), co về outputWidth (giữ tỉ lệ) và mã hóa JPEG, dùng lại cùng bộ đệm cho mọi frame.
        private byte[] CaptureScaledFrame()
        {
            int superSize = Mathf.Clamp(Mathf.CeilToInt(outputWidth / (float)Mathf.Max(1, Screen.width)), 1, 4);
            Texture2D frame = ScreenCapture.CaptureScreenshotAsTexture(superSize);
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

        // EN: Closes the file and leaves Play Mode in the editor.
        // VI: Đóng file và thoát Play Mode trong editor.
        private void Finish()
        {
            output?.Dispose();
            output = null;
            Debug.Log($"[GameplayRecorder] Đã lưu video ({partIndex + 1} phần) tại {Path.GetDirectoryName(OutputPath)}");
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
