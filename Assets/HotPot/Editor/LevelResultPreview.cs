using System.IO;
using FoodPop;
using UnityEditor;
using UnityEngine;

// EN: Renders the production result canvas into preview images without saving or replacing the open scene.
// VI: Dựng canvas kết quả thật thành ảnh xem thử mà không lưu hoặc thay thế scene đang mở.
internal static class LevelResultPreview
{
    // EN: Keeps generated previews outside Unity assets in the workspace output folder.
    // VI: Đặt ảnh xem thử sinh ra ngoài asset Unity trong thư mục output của workspace.
    private static string OutputFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "../output/LevelResultUI"));

    // EN: Services an explicit preview request after script import so a disconnected editor bridge is not required.
    // VI: Xử lý yêu cầu xem thử đã tạo sau khi nhập script để không phụ thuộc cầu nối editor bị ngắt.
    [InitializeOnLoadMethod]
    private static void ScheduleRequestedPreview()
    {
        if (File.Exists(Path.Combine(OutputFolder, "render.request")))
            EditorApplication.update += RenderRequestedPreview;
    }

    // EN: Waits for editor compilation to finish, then consumes the request and renders both outcomes once.
    // VI: Chờ editor biên dịch xong rồi nhận yêu cầu và dựng hai kết quả đúng một lần.
    private static void RenderRequestedPreview()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        EditorApplication.update -= RenderRequestedPreview;
        string request = Path.Combine(OutputFolder, "render.request");
        if (!File.Exists(request)) return;
        File.Delete(request);
        CaptureBoth();
    }

    // EN: Renders the three concrete popup classes through an isolated UIManager without changing the scene manager.
    // VI: Dựng ba lớp popup cụ thể qua UIManager riêng mà không thay đổi manager của scene.
    [MenuItem("Tools/HotPot/Preview Win And Lose UI")]
    public static void CaptureBoth()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[LevelResultPreview] Hãy thoát chế độ chơi trước khi dựng ảnh xem thử.");
            return;
        }
        Directory.CreateDirectory(OutputFolder);
        var root = new GameObject("Temporary Result Preview", typeof(RectTransform));
        root.hideFlags = HideFlags.HideAndDontSave;
        var cameraRoot = new GameObject("Temporary Result Camera", typeof(Camera));
        cameraRoot.hideFlags = HideFlags.HideAndDontSave;
        Camera camera = cameraRoot.GetComponent<Camera>();
        Camera source = Camera.main;
        if (source != null)
        {
            camera.CopyFrom(source);
            camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
        }
        else
        {
            camera.orthographic = true;
            camera.orthographicSize = 10f;
            camera.transform.position = new Vector3(0, 0, -10);
            camera.backgroundColor = new Color32(42, 62, 51, 255);
        }
        var target = new RenderTexture(720, 1280, 24);
        RenderTexture previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            FoodPop.UIManager panel = root.AddComponent<FoodPop.UIManager>();
            LevelResultPrefabSetup.ConfigureManager(panel);
            CaptureState(panel, camera, target, true);
            CaptureState(panel, camera, target, false);
            CaptureState(panel, camera, target, false, true);
            Debug.Log("[LevelResultPreview] Đã xuất demo thắng/thua/hồi sinh: " + OutputFolder);
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(cameraRoot);
        }
    }

    // EN: Captures one concrete canvas opened by UIManager at mobile resolution using the gameplay prefab.
    // VI: Chụp một canvas cụ thể được UIManager mở ở độ phân giải điện thoại bằng prefab gameplay.
    private static void CaptureState(FoodPop.UIManager panel, Camera camera, RenderTexture target, bool won, bool revive = false)
    {
        if (revive) panel.ShowRevive(null, null, true, false);
        else if (won) panel.ShowWin(null, true, false);
        else panel.ShowLose(null, false);
        Canvas canvas = panel.ActiveCanvas;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = Mathf.Max(camera.nearClipPlane + 0.5f, 1f);
        Canvas.ForceUpdateCanvases();
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        try
        {
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(OutputFolder, revive ? "revive.png" : won ? "win.png" : "lose.png"), image.EncodeToPNG());
        }
        finally
        {
            Object.DestroyImmediate(image);
        }
    }
}
