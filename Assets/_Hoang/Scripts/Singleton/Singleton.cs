public class Singleton<T> where T : Singleton<T>, new()
{
    static T ins;
    public static T Ins => ins ??= new();

    public static bool HasIns => ins != null;
}
