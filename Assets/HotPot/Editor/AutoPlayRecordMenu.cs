using UnityEditor;

namespace FoodPop
{
    // EN: Menu that starts Play Mode with the debug bot playing the level and the recorder capturing the Game view.
    // VI: Menu bật Play Mode với bot debug tự chơi level và bộ quay ghi lại màn hình Game.
    public static class AutoPlayRecordMenu
    {
        // EN: Requests auto play + recording and enters Play Mode.
        // VI: Yêu cầu tự chơi + quay màn hình rồi vào Play Mode.
        [MenuItem("Tools/HotPot/Auto Play And Record")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
                return;
            GameplayRecorder.RequestRecording();
            EditorApplication.EnterPlaymode();
        }
    }
}
