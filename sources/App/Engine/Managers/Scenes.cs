namespace Engine.Managers; 

public static class Scenes
{
    private static readonly Stack<Scene> _scenes = new();
    private static bool _shouldPopScene = false;
    private static Scene? _sceneToPop = null;
    private static Scene? _sceneToPushAfterPop = null;
    
    public static Action OnScenePopped;
    public static Action OnScenePushed;

    public static void PushScene(Scene scene)
    {
        _scenes.Push(scene);
        OnScenePushed?.Invoke();
    }

    public static void PushOnNewScene(Scene scene)
    {
        _scenes.Pop();
        PushScene(scene);
    }
    
    public static void PopScene()
    {
        _shouldPopScene = true;
        _sceneToPop = _scenes.Count > 0 ? _scenes.Peek() : null;
        
        OnScenePopped?.Invoke();
    }

    public static void ReplaceScene(Scene scene)
    {
        _sceneToPushAfterPop = scene;
        PopScene();
    }
    
    private static void ProcessPop()
    {
        if (_shouldPopScene && _sceneToPop != null)
        {
            if (_scenes.Count > 0 && ReferenceEquals(_scenes.Peek(), _sceneToPop))
            {
                _scenes.Pop()?.RootDispose();
            }

            _shouldPopScene = false;
            _sceneToPop = null;

            if (_sceneToPushAfterPop != null)
            {
                _scenes.Push(_sceneToPushAfterPop);
                _sceneToPushAfterPop = null;
                OnScenePushed?.Invoke();
            }
        }
    }
    
    public static Scene? PeekScene() 
    {
        if (_scenes.Count == 0) return null;
        return _scenes.Peek();
    }
    
    public static void Update(float deltaTime)
    {
        _scenes.Peek()?.RootUpdate(deltaTime);
        ProcessPop();
    }
    
    public static void Draw() => _scenes.Peek()?.RootDraw();
    
    public static void Dispose()
    {
        while (_scenes.Count > 0)
            PopScene();
        ProcessPop();
    }
    
    public static bool HasPreviousScene() => _scenes.Count > 1;
}