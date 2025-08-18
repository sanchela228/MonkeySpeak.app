namespace Graphics;

public class Animator
{
    private class AnimationTask
    {
        public Action<float> Action;
        public Action OnComplete;
        public bool HasCompletedEventFired = false;
        public float Progress;
        public float Duration;
        public float Delay;
        public bool Removable;
        public bool IsCompleted;
        public bool Mirror;
        public bool Repeat;
        public bool IsPlayingForward = true;
    }
    
    private readonly List<AnimationTask> _tasks = new();
    
    ~Animator() => _tasks.Clear();
    
    public void Task(Action<float> action, Action onComplete = null, float duration = 1f, float delay = 0f, bool removable = false, 
        bool mirror = false, bool repeat = false)
    {
        _tasks.Add(new AnimationTask
        {
            Action = action,
            OnComplete = onComplete,
            HasCompletedEventFired = false,
            Progress = 0f,
            Duration = duration,
            Delay = delay,
            Removable = removable,
            IsCompleted = false,
            Mirror = mirror,
            Repeat = repeat,
            IsPlayingForward = true,
        });
    }
    
    public void Update(float deltaTime)
    {
        for (int i = _tasks.Count - 1; i >= 0; i--)
        {
            var task = _tasks[i];

            if (task.IsCompleted)
            {
                if (!task.HasCompletedEventFired && task.OnComplete != null)
                {
                    task.OnComplete();
                    task.HasCompletedEventFired = true;
                }
                
                continue;
            }

            if (task.Delay > 0f)
            {
                task.Delay -= deltaTime;
                continue;
            }

            float deltaProgress = deltaTime / task.Duration;
            task.Progress += task.IsPlayingForward ? deltaProgress : -deltaProgress;
            
            if (task.IsPlayingForward && task.Progress >= 1f)
            {
                if (task.Mirror)
                {
                    task.Progress = 1f;
                    task.IsPlayingForward = false;
                }
                else if (task.Repeat)
                {
                    task.Progress = 0f;
                }
                else
                {
                    task.IsCompleted = true;
                    
                    if (task.Removable) _tasks.RemoveAt(i);
                }
            }
            else if (!task.IsPlayingForward && task.Progress <= 0f)
            {
                if (task.Repeat)
                {
                    task.Progress = 0f;
                    task.IsPlayingForward = true;
                }
                else
                {
                    task.IsCompleted = true;
                    if (task.Removable) _tasks.RemoveAt(i);
                }
            }
        }
    }

    public void Draw()
    {
        foreach (var task in _tasks)
        {
            if (task.Delay > 0f) continue;
            task.Action(task.Progress);
        }
    }
}