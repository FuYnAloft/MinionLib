namespace MinionLib.Component.Core;

public static class DelegateRegistry
{
    private static readonly Dictionary<(string name, Type type), Delegate> Delegates = new();
    
    public static void Register<T>(string name, T del) where T : Delegate
    {
        StringIdPool.Register(name);
        Delegates[(name, typeof(T))] = del;
    }

    public static T? Get<T>(string name) where T : Delegate
    {
        if (Delegates.TryGetValue((name, typeof(T)), out var del))
        {
            return (T)del;
        }

        return null;
    }
}
