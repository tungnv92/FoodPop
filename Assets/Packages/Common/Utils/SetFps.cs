

using UnityEngine;
/// <summary>
/// Utility component to set the framerate of the game to a
/// target value.
/// </summary>
public class SetFps : MonoBehaviour {
    public int Fps = 60;
    float deltaTime = 0.0f;

    private void Start() {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Fps;
       
    }
    //void Update()
    //{
    //    deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    //}
    //void OnGUI()
    //{
    //    GUIStyle style = new GUIStyle();
    //    style.fontSize = 50;                 // tăng size chữ
    //    style.normal.textColor = Color.white;
    //    GUI.Label(new Rect(10, 10, 300, 100), "FPS: " + (1f / deltaTime).ToString("0"),style);
    //}
}
