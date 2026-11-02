using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

public class OpenSceneEditor
{
    const string PathToScenesFolder = "Assets/HotPot/Scenes/";

    [MenuItem("Open Scene/Load #1", priority = 1)]
    public static void OpenLoad()
    {
        OpenScene("Load");
    }

    [MenuItem("Open Scene/Load #1", true, priority = 1)]
    public static bool OpenLoadValidate()
    {
        return OpenSceneValidate("Load");
    }

    [MenuItem("Open Scene/Home #2", priority = 2)]
    public static void OpenHome()
    {
        OpenScene("Home");
    }

    [MenuItem("Open Scene/Home #2", true, priority = 2)]
    public static bool OpenHomeValidate()
    {
        return OpenSceneValidate("Home");
    }

    [MenuItem("Open Scene/Game #3", priority = 3)]
    public static void OpenGame()
    {
        OpenScene("Game");
    }

    [MenuItem("Open Scene/Game #3", true, priority = 3)]
    public static bool OpenGameValidate()
    {
        return OpenSceneValidate("Game");
    }

    [MenuItem("Open Scene/CreateMap #4", priority = 100)]
    public static void OpenMAP()
    {
        OpenScene("CreateMap");
    }

    [MenuItem("Open Scene/CreateMap #4", true, priority = 100)]
    public static bool OpenMAPValidate()
    {
        return OpenSceneValidate("CreateMap");
    }

    [MenuItem("Open Scene/Galactic #5", priority = 4)]
    public static void OpenGalactic()
    {
        OpenScene("Galactic");
    }

    [MenuItem("Open Scene/Galactic #5", true, priority = 4)]
    public static bool OpenGalacticValidate()
    {
        return OpenSceneValidate("Galactic");
    }

    static void OpenScene(string sceneName)
    {
        //if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        //{
        //    EditorSceneManager.OpenScene(PathToScenesFolder + sceneName + ".unity");
        //}

        var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (activeScene.isDirty)
        {
            EditorSceneManager.SaveScene(activeScene);
        }
        EditorSceneManager.OpenScene(PathToScenesFolder + sceneName + ".unity");
    }

    static bool OpenSceneValidate(string sceneName)
    {
        return File.Exists(PathToScenesFolder + sceneName + ".unity");
    }
}