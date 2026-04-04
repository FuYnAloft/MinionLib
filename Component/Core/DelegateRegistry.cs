namespace MinionLib.Component.Core;

public static class DelegateRegistry
{
    private static readonly Dictionary<string, Delegate> Delegates = new();
    
    public static void Register<T>(string key, T del) where T : Delegate
    {
        Delegates[key] = del;
    }

    public static T? Get<T>(string key) where T : Delegate
    {
        if (Delegates.TryGetValue(key, out var del) && del is T typed)
        {
            return typed;
        }

        return null;
    }
}