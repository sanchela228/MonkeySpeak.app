using System.Collections.Concurrent;
using App.System.Services;

namespace Engine;

public static class MainThreadDispatcher
{
    private static readonly ConcurrentQueue<Action> _queue = new();

    public static void Post(Action action)
    {
        if (action != null) _queue.Enqueue(action);
    }

    public static void ExecutePending()
    {
        while (_queue.TryDequeue(out var action))
        {
            try { action(); }
            catch (Exception ex) { Logger.Error($"[Dispatcher] {ex.Message}"); }
        }
    }
}